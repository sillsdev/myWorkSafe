using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Synchronization;
using Microsoft.Synchronization.Files;
using myWorkSafe.Groups;
using Palaso.Code;

namespace myWorkSafe
{
	public class Synchronizer
	{       
		private int _totalFilesThatWillBeBackedUp;
		private int _files;
		public string DestinationRootForThisUser;
		private readonly IEnumerable<FileGroup> _groups;
		private readonly long _totalAvailableOnDeviceInKilobytes;
		private readonly IProgress _progress;
		private SyncOrchestrator _agent;
		private FileGroup _currentGroup;
		private bool _cancelRequested;
		public long ApprovedChangeInKB;
		private HashSet<string> _alreadyAccountedFor;
		private int _errorCount;
		private const int MaxErrorsBeforeAbort=10;


		public Synchronizer(string destinationDeviceRoot, IEnumerable<FileGroup> groups, long totalAvailableOnDeviceInKilobytes, IProgress progress)
		{
			_groups = groups;
			_totalAvailableOnDeviceInKilobytes = totalAvailableOnDeviceInKilobytes;
			Guard.AgainstNull(progress,"progress");
			_progress = progress;
			SetDestinationFolderPath(destinationDeviceRoot);
			_agent = new SyncOrchestrator();
		}

		/// <summary>
		/// We want to name the root of the backup in a way that allows multiple
		/// team members to use the same key if necessary, and to help support
		/// staff identify whose backup they are looking at. Ideally, the computer
		/// name would have the language name.
		/// </summary>
		private void SetDestinationFolderPath(string destinationDeviceRoot)
		{
			var id = string.Format("{0}-{1}", System.Environment.UserName,  System.Environment.MachineName);           
			DestinationRootForThisUser = Path.Combine(destinationDeviceRoot, id);
			if (!Directory.Exists(DestinationRootForThisUser))
				Directory.CreateDirectory(DestinationRootForThisUser);
		}

		public int FilesCopiedThusFar	
		{
			get { return _files;}
		}

		public int TotalFilesThatWillBeBackedUpThatWillBeCopied
		{
			get
			{
				return _totalFilesThatWillBeBackedUp;
			}
		}

		public void GatherPreview()
		{
			/* enhance: we could try to deal with the situation where a lower-priority group
			 * is hogging space from a newly enlarged higher-priority group. We'd have to
			 * scan ahead, perhaps first collecting the current on-backup sizes of each.
			 * 
			 * Then, as we go through the groups, we could keep going so long as deleting
			 * some lower-priority group would allow us to keep going.
			 */
			_alreadyAccountedFor = new HashSet<string>();
			_cancelRequested = false;
			_files = 0;
			ApprovedChangeInKB = 0;
			//PredictedChangeInKiloBytes = 0;
			_totalFilesThatWillBeBackedUp = 0;
			var options = FileSyncOptions.None;
			var limitHasBeenReached = false;

			foreach (var group in _groups)
			{
				if(_cancelRequested)
				{
					break;
				}
				_currentGroup = group;//used by callbacks
				group.ClearStatistics();

				if(group.Disposition == FileGroup.DispositionChoice.Hide)
					continue;

				Debug.Assert(!string.IsNullOrEmpty(group.RootFolder), "should have been weeded out already");
				Debug.Assert(Directory.Exists(group.RootFolder), "should have been weeded out already");


				if (limitHasBeenReached)
				{
					//don't even investigate.
					//NB: there might actually be enough room, if this group is smaller
					//than the first one which was too big. Or algorithm doesn't try
					//to fit it in.
					group.Disposition = FileGroup.DispositionChoice.NotEnoughRoom;
					InvokeGroupProgress();
					continue;
				}
				group.Disposition = FileGroup.DispositionChoice.Calculating;
				InvokeGroupProgress();


				string destinationSubFolder = group.GetDestinationSubFolder(DestinationRootForThisUser);

				//nb: it seems so far that the persisant metat data file thing is counter productive,
				//since we aren't sync'ing, really, we're just copying from computer-->key.  We could
				//imagine that file helping speed things up, but I can't yet see that it does, but I
				//have seen it cause things to not be copied over (in circumnstances which might make
				//sense in some syncing contexts).

				string tempDirectory = Path.GetDirectoryName(group.SourceTempMetaFile);
				using (var sourceProvider = new FileSyncProvider(group.SourceGuid, group.RootFolder, group.Filter, options,
					tempDirectory,
					Path.GetFileName(group.SourceTempMetaFile),
					tempDirectory,
					tempDirectory)) 
				using (var destinationProvider = new FileSyncProvider(group.DestGuid, destinationSubFolder, group.Filter, options,
					tempDirectory,
					Path.GetFileName(group.DestTempMetaFile),
					tempDirectory,
					tempDirectory
				))
				{
					destinationProvider.PreviewMode = true;
					destinationProvider.ApplyingChange += (OnDestinationPreviewChange);

					sourceProvider.DetectingChanges += (x, y) => InvokeProgress(y);//just to detect cancel

					PreviewOrSynchronizeCore(destinationProvider, sourceProvider);
				}

				var groupsChangeInKB = (long)Math.Ceiling(group.NetChangeInBytes / 1024.0);
				//is there room to fit in this whole group?
				if (ApprovedChangeInKB + groupsChangeInKB < _totalAvailableOnDeviceInKilobytes)
				{
					ApprovedChangeInKB = groupsChangeInKB;
					_totalFilesThatWillBeBackedUp += group.UpdateFileCount + group.NewFileCount;
					group.Disposition = FileGroup.DispositionChoice.WillBeBackedUp;
				}
				else
				{
					limitHasBeenReached = true;	//nb: remove if/when we go to the system below of deleting
					group.Disposition = FileGroup.DispositionChoice.NotEnoughRoom;
				} 
			}
			InvokeGroupProgress();		
			_files = 0;
		}

		private void PreviewOrSynchronizeCore(FileSyncProvider destinationProvider, FileSyncProvider sourceProvider)
		{
			_agent.LocalProvider = sourceProvider;
			_agent.RemoteProvider = destinationProvider;
			_agent.Direction = SyncDirectionOrder.Upload; // Synchronize source to destination

			try
			{
				_agent.Synchronize();
			}
			catch (Microsoft.Synchronization.SyncAbortedException err)
			{
				//swallow                        
			}
		}

		private void InvokeGroupProgress()
		{
			if (GroupProgress != null)
			{
				GroupProgress.Invoke();
			}
		}

		public void DoSynchronization()
		{
			_cancelRequested = false;
			var options = FileSyncOptions.RecycleDeletedFiles;
			_alreadyAccountedFor = new HashSet<string>();

			try
			{
				foreach (var group in _groups)
				{
					if (_cancelRequested)
					{
						break;
					}
					_currentGroup = group; //used by callbacks

					if (group.Disposition == FileGroup.DispositionChoice.Hide)
						continue;


					if (group.Disposition == FileGroup.DispositionChoice.NotEnoughRoom)
						continue;

					group.ClearStatistics();
					group.Disposition = FileGroup.DispositionChoice.Synchronizing;
					InvokeGroupProgress();
					string tempDirectory = Path.GetDirectoryName(group.SourceTempMetaFile);

					using (var sourceProvider = new FileSyncProvider(group.SourceGuid, group.RootFolder, group.Filter, options,
																	 tempDirectory,
																	 Path.GetFileName(group.SourceTempMetaFile),
																	 tempDirectory,
																	 tempDirectory))
					using (
						var destinationProvider = new FileSyncProvider(group.DestGuid,
																	   group.GetDestinationSubFolder(DestinationRootForThisUser),
																	   group.Filter, options,
																	   tempDirectory,
																	   Path.GetFileName(group.SourceTempMetaFile),
																	   tempDirectory,
																	   tempDirectory))
					{
						destinationProvider.PreviewMode = false;
						destinationProvider.SkippedChange += new EventHandler<SkippedChangeEventArgs>(destinationProvider_SkippedChange);
						destinationProvider.ApplyingChange += (OnDestinationChange);
						PreviewOrSynchronizeCore(destinationProvider, sourceProvider);
					}

					group.Disposition = FileGroup.DispositionChoice.WasBackedUp;

					if (GroupProgress != null)
					{
						GroupProgress.Invoke();
					}
				}
				InvokeGroupProgress();
			}
			catch (Exception error)
			{
				Palaso.Reporting.ErrorReport.NotifyUserOfProblem(error, "Sorry, something didn't work.");
			}
			finally
			{
				CleanupTempFiles();
			}
		}

		void destinationProvider_SkippedChange(object sender, SkippedChangeEventArgs e)
		{
			//ConflictLoserWriteError. This reason will be raised if a change is skipped because an attempt to recycle a losing file fails.
//			var path = e.CurrentFilePath == null ? e.NewFilePath : e.CurrentFilePath;
			try
			{
				_progress.WriteError("File Skipped ['{0}'/'{1}']. Reason={2}. Exception Follows:", e.NewFilePath ?? "", e.CurrentFilePath ?? "", e.SkipReason);
				if(e.Exception !=null)
					_progress.WriteException(e.Exception);

				_errorCount++;
				if(_errorCount > MaxErrorsBeforeAbort)
				{
					_progress.WriteError("Error count exceeded limit. Will abort.");
					_cancelRequested = true;
					_agent.Cancel();
				}
			}
			catch (Exception error)
			{
				try
				{
					_progress.WriteException(error);
				}
				catch (Exception progressException)
				{ 
					Palaso.Reporting.ErrorReport.ReportFatalException(progressException);
				}
			}
		}

		private void CleanupTempFiles()
		{
			foreach (var group in _groups)
			{
				if(File.Exists(group.SourceTempMetaFile))
				{
					File.Delete(group.SourceTempMetaFile);
				}
				if (File.Exists(group.DestTempMetaFile))
				{
					File.Delete(group.DestTempMetaFile);
				}
			}
		}

		private void OnDestinationPreviewChange(object provider, ApplyingChangeEventArgs args)
		{
			if(ShouldSkip((FileSyncProvider)provider,args))
			{
				args.SkipChange = true;
				return;
			}
			if(args.CurrentFileData !=null)
			{
				_currentGroup.NetChangeInBytes -= args.CurrentFileData.Size;
				//below, we'll add back the new size, giving us the correct net change
			}
			string rootDirectoryPath = ((FileSyncProvider)provider).RootDirectoryPath;
			switch (args.ChangeType)
			{
				case ChangeType.Create:
					_currentGroup.NewFileCount++;
					_currentGroup.NetChangeInBytes += args.NewFileData.Size;
					_alreadyAccountedFor.Add(Path.Combine(rootDirectoryPath, args.NewFileData.RelativePath));
					break;
				case ChangeType.Update:
					_currentGroup.UpdateFileCount++;
					_currentGroup.NetChangeInBytes += args.NewFileData.Size;
					_alreadyAccountedFor.Add(Path.Combine(rootDirectoryPath, args.CurrentFileData.RelativePath));
					break;
				case ChangeType.Delete:
					_alreadyAccountedFor.Add(Path.Combine(rootDirectoryPath, args.CurrentFileData.RelativePath));
					_currentGroup.DeleteFileCount++;
					break;
			}
			InvokeProgress(args);
		}

		private bool ShouldSkip(FileSyncProvider provider,ApplyingChangeEventArgs args)
		{
			//the built-in directory system is lame, you can't just specify the name of the directory
			//this changes the behavior to do just that
			if (args.NewFileData!=null && args.NewFileData.IsDirectory)
				return _currentGroup.ShouldSkipSubDirectory(args.NewFileData);

			if (args.CurrentFileData != null && args.CurrentFileData.IsDirectory)
				return _currentGroup.ShouldSkipSubDirectory(args.CurrentFileData);

			string rootDirectoryPath = provider.RootDirectoryPath; 
			if (args.NewFileData != null)
				return _alreadyAccountedFor.Contains(Path.Combine(rootDirectoryPath, args.NewFileData.RelativePath)) 
						||_currentGroup.ShouldSkipFile(args.NewFileData.RelativePath);

			if (args.CurrentFileData != null)
				return _alreadyAccountedFor.Contains(Path.Combine(rootDirectoryPath, args.CurrentFileData.RelativePath)) 
						|| _currentGroup.ShouldSkipFile(args.CurrentFileData.RelativePath);

			return false;
		}

		private void OnDestinationChange(object provider, ApplyingChangeEventArgs args)
		{
			if(args.NewFileData != null)
				Debug.WriteLine(args.NewFileData.RelativePath+"  "+args.NewFileData.Name);

			if (ShouldSkip((FileSyncProvider)provider, args))
			{
				args.SkipChange = true;
				return;
			}

			Debug.Assert(args == null || args.NewFileData == null || !args.NewFileData.Name.Contains("extensions"));

			_files++;
			InvokeProgress(args);

			switch (args.ChangeType)
			{
				case ChangeType.Create:
					break;
				case ChangeType.Update:
					break;
				case ChangeType.Delete:
					break;
			}
		}

		/// <summary>
		/// Return true if you want to cancel
		/// </summary>
		public event Func<string, bool> FileProgress;

		public event Action GroupProgress;

		public void InvokeProgress(EventArgs args)
		{
			//TODO this needs work!
			if (FileProgress != null)
			{
				FileData newFileData=null;
				FileData currentFileData=null;
				ApplyingChangeEventArgs a = args as ApplyingChangeEventArgs;
				if(a!=null)
				{
					newFileData = a.NewFileData;
					currentFileData = a.CurrentFileData;
				}

				string path=string.Empty;
				if(newFileData !=null)
				{
					path = Path.Combine(newFileData.RelativePath, newFileData.Name);
					Debug.Assert(currentFileData ==null);//test this assumption
				}
				else if (currentFileData != null)
				{
					path = Path.Combine(currentFileData.RelativePath, currentFileData.Name);
				}

				if(FileProgress(path))
				{
					_cancelRequested = true;
					_agent.Cancel();
				}
			}
		}
	}
}
