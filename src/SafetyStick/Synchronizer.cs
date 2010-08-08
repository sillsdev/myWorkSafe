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
		private int _errorCountSinceLastSuccess;
		private bool _gotIOExceptionProbablyDiskFull;
		private const int MaxErrorsBeforeAbort=10;


		public Synchronizer(string destinationFolderPath, IEnumerable<FileGroup> groups, long totalAvailableOnDeviceInKilobytes, IProgress progress)
		{
			_groups = groups;
			_totalAvailableOnDeviceInKilobytes = totalAvailableOnDeviceInKilobytes;
			Guard.AgainstNull(progress,"progress");
			_progress = progress;
			_agent = new SyncOrchestrator();
			DestinationRootForThisUser = destinationFolderPath;
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
				{
					_progress.WriteMessage("Skipping preview of group {0}", group.Name);
					continue;
				}
				_progress.WriteMessage("Beginning preview of group {0}", group.Name);

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
			//nb: a value of FileSyncOptions.RecycleDeletedFiles is implicated in a ton of 
			//ConflictLoserWriteError + "PathTooLong" errors I was getting on a flash drive 
			//	(even though it wasn't full)
			//until I reformated it.  Since small Flash drives don't have a recycle bin, I don't
			//know what the semantics are for this anyhow
			//var options = FileSyncOptions.RecycleDeletedFiles;
			var options = FileSyncOptions.None ;

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
					{
						_progress.WriteMessage("Skipping group {0}", group.Name);
						continue;
					}
					if(_gotIOExceptionProbablyDiskFull)
					{
						group.Disposition = FileGroup.DispositionChoice.NotEnoughRoom;
						continue;
					}

					_progress.WriteMessage("Beginning group {0}", group.Name);


					if (group.Disposition == FileGroup.DispositionChoice.NotEnoughRoom)
						continue;

					group.ClearStatistics();
					group.Disposition = FileGroup.DispositionChoice.Synchronizing;
					InvokeGroupProgress();
					string tempDirectory = Path.GetDirectoryName(group.SourceTempMetaFile);

					try
					{
						using (var sourceProvider = new FileSyncProvider(group.SourceGuid, group.RootFolder, group.Filter, options,
																		 tempDirectory,
																		 Path.GetFileName(group.SourceTempMetaFile),
																		 tempDirectory,
																		 tempDirectory))
						{
							string destinationSubFolder = group.GetDestinationSubFolder(DestinationRootForThisUser);
							using (
								var destinationProvider = new FileSyncProvider(group.DestGuid,
																			   destinationSubFolder,
																			   group.Filter, options,
																			   tempDirectory,
																			   Path.GetFileName(group.SourceTempMetaFile),
																			   tempDirectory,
																			   tempDirectory))
							{
								_progress.WriteVerbose("[{0}] Source={1}", group.Name, group.RootFolder);
								_progress.WriteVerbose("[{0}] Destination={1}", group.Name, destinationSubFolder);

								destinationProvider.PreviewMode = false;
								destinationProvider.SkippedChange += OnDestinationSkippedChange;
								destinationProvider.ApplyingChange += OnDestinationApplyingChange;

								PreviewOrSynchronizeCore(destinationProvider, sourceProvider);
							}

						}
						group.Disposition = FileGroup.DispositionChoice.WasBackedUp;
					}
					catch (IOException error)
					{
						_gotIOExceptionProbablyDiskFull = true;
						//enhance: we could clarify that it was partially backed up
						_currentGroup.Disposition = FileGroup.DispositionChoice.NotEnoughRoom;
						_progress.WriteWarning(error.Message);
					}

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

		void OnDestinationSkippedChange(object sender, SkippedChangeEventArgs e)
		{
			if(e.SkipReason == SkipReason.ApplicationRequest)
			{
				_progress.WriteMessage("Skipping '{0}  {1}'", e.NewFilePath ?? "", e.CurrentFilePath ?? "");
				return;
			}

			if(e.Exception.Message.Contains("space")
				|| e.Exception.Message.Contains("full")
				|| e.Exception.GetType() == typeof(System.IO.IOException))
			{
				_progress.WriteError(e.SkipReason.ToString());
				_progress.WriteError(e.Exception.Message);
				_gotIOExceptionProbablyDiskFull = true;
				_agent.Cancel();//will just end this group, not close the window
				return;
			}
			//ConflictLoserWriteError. This reason will be raised if a change is skipped because an attempt to recycle a losing file fails.
//			var path = e.CurrentFilePath == null ? e.NewFilePath : e.CurrentFilePath;
			try
			{
				_progress.WriteError("File Skipped ['{0}'/'{1}']. Reason={2}. Exception Follows:", e.NewFilePath ?? "", e.CurrentFilePath ?? "", e.SkipReason);
				if(e.Exception !=null)
					_progress.WriteException(e.Exception);

				_errorCountSinceLastSuccess++;
				if(_errorCountSinceLastSuccess > MaxErrorsBeforeAbort)
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
			if(ShouldSkip("Preview", (FileSyncProvider)provider,args))
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
					_progress.WriteVerbose("[{0}] Preview Create {1}", _currentGroup.Name, GetPathFromArgs(args));
					_currentGroup.NewFileCount++;
					_currentGroup.NetChangeInBytes += args.NewFileData.Size;
					_alreadyAccountedFor.Add(args.NewFileData.RelativePath);
					break;
				case ChangeType.Update:
					_progress.WriteVerbose("[{0}] Preview Update {1}", _currentGroup.Name, GetPathFromArgs(args));
					_currentGroup.UpdateFileCount++;
					_currentGroup.NetChangeInBytes += args.NewFileData.Size;
					_alreadyAccountedFor.Add(args.CurrentFileData.RelativePath);
					break;
				case ChangeType.Delete:
					_progress.WriteVerbose("[{0}] Preview Delete {1}", _currentGroup.Name, GetPathFromArgs(args));
					_alreadyAccountedFor.Add(args.CurrentFileData.RelativePath);
					_currentGroup.DeleteFileCount++;
					break;
			}
			InvokeProgress(args);
		}

		private bool ShouldSkip(string mode, FileSyncProvider provider,ApplyingChangeEventArgs args)
		{
			//the built-in directory system is lame, you can't just specify the name of the directory
			//this changes the behavior to do just that
			if (args.NewFileData!=null && args.NewFileData.IsDirectory)
			{
				if(_currentGroup.ShouldSkipSubDirectory(args.NewFileData))
				{
					_progress.WriteVerbose("{0} [{1}] Skipping Folder {2}",mode, _currentGroup.Name, GetPathFromArgs(args));
					return true;
				}
				return false;
			}

			if (args.CurrentFileData != null && args.CurrentFileData.IsDirectory)
			{
				if (_currentGroup.ShouldSkipSubDirectory(args.CurrentFileData))
				{
					_progress.WriteVerbose("{0} [{1}] Skipping Folder {2}",mode, _currentGroup.Name, GetPathFromArgs(args));
					return true;
				}
				return false;
			}

			if (args.NewFileData != null)
			{
				if (_alreadyAccountedFor.Contains(args.NewFileData.RelativePath))
				{
					_progress.WriteVerbose("[{0}] Skipping new file because it was already backed up by a previous group:  {1}", _currentGroup.NewFileCount, GetPathFromArgs(args));
					return true;
				}
				if( _currentGroup.ShouldSkipFile(args.NewFileData.RelativePath))
				{
					_progress.WriteVerbose("[{0}] Skipping new file: {1}", _currentGroup.Name,GetPathFromArgs(args));
					return true;
				}
				return false;
			}

			if (args.CurrentFileData != null)
			{
				if(_alreadyAccountedFor.Contains(args.CurrentFileData.RelativePath) )
				{
					_progress.WriteVerbose("Skipping current file because it was already backed up by a previous group: " + args.CurrentFileData.RelativePath + "  " +
					                       args.CurrentFileData.Name);
					return true;
					
				}
				if(_currentGroup.ShouldSkipFile(args.CurrentFileData.RelativePath))
				{
					_progress.WriteVerbose("Skipping current file: " + args.CurrentFileData.RelativePath + "  " +
					                       args.CurrentFileData.Name);
					return true;
				}
			}

			return false;
		}

		private void OnDestinationApplyingChange(object provider, ApplyingChangeEventArgs args)
		{
			if (ShouldSkip("Backup", (FileSyncProvider)provider, args))
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
					_progress.WriteVerbose("[{0}] Creating {1}",_currentGroup.Name, GetPathFromArgs(args)); 
					break;
				case ChangeType.Update:
					_progress.WriteVerbose("[{0}] Updating {1}", _currentGroup.Name, GetPathFromArgs(args));
					break;
				case ChangeType.Delete:
					_progress.WriteVerbose("[{0}] Deleting {1}", _currentGroup.Name, GetPathFromArgs(args));
					break;
			}

			//review: does this really mean success?
			_errorCountSinceLastSuccess = 0;
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
				if(FileProgress(GetPathFromArgs(args)))
				{
					_cancelRequested = true;
					_agent.Cancel();
				}
			}
		}

		private string GetPathFromArgs(EventArgs args)
		{
			FileData newFileData=null;
			FileData currentFileData=null;
			ApplyingChangeEventArgs a = args as ApplyingChangeEventArgs;
			if(a!=null)
			{
				newFileData = a.NewFileData;
				currentFileData = a.CurrentFileData;
			}

			//enhance: when updating a file, both new and current have the same file path contents
			//this algorithm my by simpler: if current isn't null, use it, else new.

			string path=string.Empty;
			if(newFileData !=null)
			{
				path = newFileData.RelativePath;
			}
			else if (currentFileData != null)
			{
				path = currentFileData.RelativePath;
			}
			return path;
		}
	}
}
