using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Synchronization;
using Microsoft.Synchronization.Files;

namespace myWorkSafe
{
	public class Synchronizer
	{       
		private int _totalFilesThatWillBeBackedUp;
		private int _files;
		public string DestinationRootForThisUser;
		private readonly IEnumerable<FileSource> _groups;
		private readonly long _totalAvailableOnDeviceInKilobytes;
		private SyncOrchestrator _agent;
		private FileSource _currentSource;
//		public long PredictedChangeInKiloBytes;
		private bool _cancelRequested;
		public long ApprovedChangeInKB;


		public Synchronizer(string destinationDeviceRoot, IEnumerable<FileSource> groups, long totalAvailableOnDeviceInKilobytes)
		{
			_groups = groups;
			_totalAvailableOnDeviceInKilobytes = totalAvailableOnDeviceInKilobytes;
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

		public void GatherInformation()
		{
			/* enhance: we could try to deal with the situation where a lower-priority group
			 * is hogging space from a newly enlarged higher-priority group. We'd have to
			 * scan ahead, perhaps first collecting the current on-backup sizes of each.
			 * 
			 * Then, as we go through the groups, we could keep going so long as deleting
			 * some lower-priority group would allow us to keep going.
			 */
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
				_currentSource = group;//used by callbacks
				group.ClearStatistics();
				if (!group.GetIsRelevantOnThisMachine())
				{
					group.Disposition = FileSource.DispositionChoice.Hide;
					continue;
				}

				if (limitHasBeenReached)
				{
					//don't even investigate.
					//NB: there might actually be enough room, if this group is smaller
					//than the first one which was too big. Or algorithm doesn't try
					//to fit it in.
					group.Disposition = FileSource.DispositionChoice.NotEnoughRoom;
					InvokeGroupProgress();
					continue;
				}
				group.Disposition = FileSource.DispositionChoice.Calculating;
				InvokeGroupProgress();


				string destinationSubFolder = group.GetDestinationSubFolder(DestinationRootForThisUser);

				//nb: it seems so far that the persisant metat data file thing is counter productive,
				//since we aren't sync'ing, really, we're just copying from computer-->key.  We could
				//imagine that file helping speed things up, but I can't yet see that it does, but I
				//have seen it cause things to not be copied over (in circumnstances which might make
				//sense in some syncing contexts).

				group.SourceTempMetaFile = Path.GetTempFileName();
				group.DestTempMetaFile = Path.GetTempFileName();
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

					sourceProvider.DetectingChanges += (x, y) => InvokeProgress();//just to detect cancel

					PreviewOrSynchronizeCore(destinationProvider, sourceProvider);
				}

				var groupsChangeInKB = (long)Math.Ceiling(group.NetChangeInBytes / 1024.0);
				//is there room to fit in this whole group?
				if (ApprovedChangeInKB + groupsChangeInKB < _totalAvailableOnDeviceInKilobytes)
				{
					ApprovedChangeInKB = groupsChangeInKB;
					_totalFilesThatWillBeBackedUp += group.UpdateFileCount + group.NewFileCount;
					group.Disposition = FileSource.DispositionChoice.WillBeBackedUp;
				}
				else
				{
					limitHasBeenReached = true;	//nb: remove if/when we go to the system below of deleting
					group.Disposition = FileSource.DispositionChoice.NotEnoughRoom;
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

			try
			{
				foreach (var group in _groups)
				{
					if (_cancelRequested)
					{
						break;
					}
					_currentSource = group; //used by callbacks
					if (group.Disposition == FileSource.DispositionChoice.NotEnoughRoom)
						continue;

					group.ClearStatistics();
					group.Disposition = FileSource.DispositionChoice.Synchronizing;
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
						destinationProvider.ApplyingChange += (OnDestinationChange);
						PreviewOrSynchronizeCore(destinationProvider, sourceProvider);
					}

					group.Disposition = FileSource.DispositionChoice.WasBackedUp;

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

		private void OnDestinationPreviewChange(object x, ApplyingChangeEventArgs y)
		{
			if(ShouldSkip(y))
			{
				y.SkipChange = true;
				return;
			}
			if(y.CurrentFileData !=null)
			{
				_currentSource.NetChangeInBytes -= y.CurrentFileData.Size;
			//	PredictedSpaceInKiloBytes -= y.CurrentFileData.Size/1024;
				//next the new size will be added back, below
			}
			switch(y.ChangeType)
			{
				case ChangeType.Create:
					_currentSource.NewFileCount++;
					_currentSource.NetChangeInBytes += y.NewFileData.Size;
					//PredictedSpaceInKiloBytes += y.NewFileData.Size/1024;
					break;
				case ChangeType.Update:
					_currentSource.UpdateFileCount++;
					_currentSource.NetChangeInBytes += y.NewFileData.Size;
					//PredictedSpaceInKiloBytes += y.NewFileData.Size/1024;
					break;
				case ChangeType.Delete:
					_currentSource.DeleteFileCount++;
					break;
			}
			InvokeProgress();
		}

		private bool ShouldSkip(ApplyingChangeEventArgs args)
		{
			//the built-in directory system is lame, you can't just specify the name of the directory
			//this changes the behavior to do just that
			if (args.NewFileData!=null && args.NewFileData.IsDirectory)
				return _currentSource.ShouldSkipDirectory(args.NewFileData);

			if (args.CurrentFileData != null && args.CurrentFileData.IsDirectory)
				return _currentSource.ShouldSkipDirectory(args.CurrentFileData);

			if (args.NewFileData != null)
				return _currentSource.ShouldSkip(args.NewFileData.RelativePath);

			if (args.CurrentFileData != null)
				return _currentSource.ShouldSkip(args.CurrentFileData.RelativePath);

			return false;
		}

		private void OnDestinationChange(object x, ApplyingChangeEventArgs y)
		{
			if (ShouldSkip(y))
			{
				y.SkipChange = true;
				return;
			}

			Debug.Assert(y == null || y.NewFileData == null || !y.NewFileData.Name.Contains("extensions"));

			_files++;
			InvokeProgress();

			switch (y.ChangeType)
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
		public event Func<bool> FileProgress;

		public event Action GroupProgress;

		public void InvokeProgress()
		{
			Func<bool> handler = FileProgress;
			if (handler != null)
			{
				if(handler())
				{
					_cancelRequested = true;
					_agent.Cancel();
				}
			}
		}
	}
}
