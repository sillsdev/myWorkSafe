﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using myWorkSafe.Groups;
using Palaso.Code;

namespace myWorkSafe
{
	public class MirrorController
	{
		private int _totalFilesThatWillBeBackedUp;
		private int _files;
		public string DestinationRootForThisUser;
		private readonly IEnumerable<FileGroup> _groups;
		private readonly long _totalAvailableOnDeviceInKilobytes;
		private readonly IProgress _progress;
		private MirrorMaker _engine;
		private FileGroup _currentGroup;
		private bool _cancelRequested;
		public long ApprovedChangeInKB;
		private HashSet<string> _alreadyAccountedFor;
		private int _errorCountSinceLastSuccess;
		private bool _gotIOExceptionProbablyDiskFull;
		private const int MaxErrorsBeforeAbort = 10;


		public MirrorController(string destinationFolderPath, IEnumerable<FileGroup> groups, long totalAvailableOnDeviceInKilobytes, IProgress progress)
		{
			_groups = groups;
			_totalAvailableOnDeviceInKilobytes = totalAvailableOnDeviceInKilobytes;
			Guard.AgainstNull(progress,"progress");
			_progress = progress;
			DestinationRootForThisUser = destinationFolderPath;
		}


		public int FilesCopiedThusFar
		{
			get { return _files; }
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
			var limitHasBeenReached = false;

			foreach (var group in _groups)
			{
				if (_cancelRequested)
				{
					break;
				}
				_currentGroup = group; //used by callbacks
				group.ClearStatistics();

				if (group.Disposition == FileGroup.DispositionChoice.Hide)
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
				using (_engine = new MirrorMaker())
				{
					_engine.StartingDirectory += OnStartingDirectory;
					_engine.StartingFile += OnStartingFile;
					_engine.Run(group.RootFolder, destinationSubFolder);

					var groupsChangeInKB = (long) Math.Ceiling(group.NetChangeInBytes/1024.0);
					//is there room to fit in this whole group?
					if (ApprovedChangeInKB + groupsChangeInKB < _totalAvailableOnDeviceInKilobytes)
					{
						ApprovedChangeInKB = groupsChangeInKB;
						_totalFilesThatWillBeBackedUp += group.UpdateFileCount + group.NewFileCount;
						group.Disposition = FileGroup.DispositionChoice.WillBeBackedUp;
					}
					else
					{
						limitHasBeenReached = true; //nb: remove if/when we go to the system below of deleting
						group.Disposition = FileGroup.DispositionChoice.NotEnoughRoom;
					}

					InvokeGroupProgress();
					_files = 0;
				}
				_engine = null;
			}
		}


		void OnStartingFile(object sender, MirrorEventArgs args)
		{
			if (ShouldSkip("Backup", args))
			{
				args.PendingAction = MirrorAction.Skip;
				return;
			}

			_files++;
			InvokeProgress(args);

			switch (args.Situation)
			{
				case MirrorSituation.FileIsSame:
					_alreadyAccountedFor.Add(args.Path);
					break;
				case MirrorSituation.FileMissing:
					_progress.WriteVerbose("[{0}] Creating {1}", _currentGroup.Name, args.Path);
					_alreadyAccountedFor.Add(args.Path);
					break;
				case MirrorSituation.SourceFileOlder:
				case MirrorSituation.SourceFileNewer:
					_progress.WriteVerbose("[{0}] Updating {1}", _currentGroup.Name, args.Path);
					break;
				case MirrorSituation.FileOnDestinationButNotSource:
					if (!_currentGroup.NormallyPropogateDeletions
						&& !args.Path.Contains(".hg")) //always propogate deletions inside the mercurial folder
					{
						args.PendingAction = MirrorAction.Skip;
						_progress.WriteVerbose("[{0}] Because of group policy, will not propagate deletion of {1}", _currentGroup.Name, args.Path);
					}
					else
					{
						args.PendingAction = MirrorAction.Delete;
						_progress.WriteVerbose("[{0}] Deleting {1}", _currentGroup.Name, args.Path);
					}
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			//review: does this really mean success?
			_errorCountSinceLastSuccess = 0;
		}

		void OnStartingDirectory(object sender, MirrorEventArgs args)
		{
			if (ShouldSkip("Backup", args))
			{
				args.PendingAction = MirrorAction.Skip;
				return;
			}

			_files++;//review
			InvokeProgress(args);

			switch (args.Situation)
			{
				case MirrorSituation.DirectoryMissing:
					_progress.WriteVerbose("[{0}] Creating {1}", _currentGroup.Name, args.Path);
					_alreadyAccountedFor.Add(args.Path);
					break;
				case MirrorSituation.DirectoryExists:
					_alreadyAccountedFor.Add(args.Path);
					break;
				case MirrorSituation.DirectoryOnDestinationButNotSource:
					if (!_currentGroup.NormallyPropogateDeletions
						&& !args.Path.Contains(".hg")) //always propogate deletions inside the mercurial folder
					{
						args.PendingAction = MirrorAction.Skip;
						_progress.WriteVerbose("[{0}] Because of group policy, will not propagate deletion of {1}", _currentGroup.Name, args.Path);
					}
					else
					{
						args.PendingAction = MirrorAction.Delete;
						_progress.WriteVerbose("[{0}] Deleting {1}", _currentGroup.Name, args.Path);
					}
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			//review: does this really mean success?
			_errorCountSinceLastSuccess = 0;
		}

		private void InvokeGroupProgress()
		{
			if (GroupProgress != null)
			{
				GroupProgress.Invoke();
			}
		}

		public void Run()
		{
			_cancelRequested = false;

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
					if (_gotIOExceptionProbablyDiskFull)
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

					using (_engine = new MirrorMaker())
					{
						try
						{

							_engine.StartingDirectory += OnStartingDirectory;
							_engine.StartingFile += OnStartingFile;
							_engine.ItemHandlingError += OnItemHandlingError;

							string destinationSubFolder = group.GetDestinationSubFolder(DestinationRootForThisUser);

							_progress.WriteVerbose("[{0}] Source={1}", group.Name, group.RootFolder);
							_progress.WriteVerbose("[{0}] Destination={1}", group.Name, destinationSubFolder);

							//_engine.PreviewMode = false;
							_engine.Run(group.RootFolder, destinationSubFolder);
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
				_engine = null;
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

		void OnItemHandlingError(object sender, ItemHandlingErrorArgs e)
		{
				//todo: make work for non-english
				if(e.Exception.Message.Contains("space")
					|| e.Exception.Message.Contains("full"))
				{
					_progress.WriteError(e.Exception.Message);
					_gotIOExceptionProbablyDiskFull = true;
					_engine.Cancel();//will just end this group, not close the window
					return;
				}
				try
				{
					_progress.WriteError("Error while processing file'{0}'. Reason={1}. Exception Follows:", e.Path, e.Exception.Message);
					if(e.Exception !=null)
						_progress.WriteException(e.Exception);

					_errorCountSinceLastSuccess++;
					if(_errorCountSinceLastSuccess > MaxErrorsBeforeAbort)
					{
						_progress.WriteError("Error count exceeded limit. Will abort.");
						_cancelRequested = true;
						_engine.Cancel();
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
				if (File.Exists(group.SourceTempMetaFile))
				{
					File.Delete(group.SourceTempMetaFile);
				}
				if (File.Exists(group.DestTempMetaFile))
				{
					File.Delete(group.DestTempMetaFile);
				}
			}
		}


		/*
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
					_progress.WriteVerbose("[{0}] Preview Create {1}", _currentGroup.Name, args.Path);
					_currentGroup.NewFileCount++;
					_currentGroup.NetChangeInBytes += args.NewFileData.Size;
					_alreadyAccountedFor.Add(args.NewFileData.RelativePath);
					break;
				case ChangeType.Update:
					_progress.WriteVerbose("[{0}] Preview Update {1}", _currentGroup.Name, args.Path);
					_currentGroup.UpdateFileCount++;
					_currentGroup.NetChangeInBytes += args.NewFileData.Size;
					_alreadyAccountedFor.Add(args.CurrentFileData.RelativePath);
					break;
				case ChangeType.Delete:
					if (!_currentGroup.NormallyPropogateDeletions)
					{
						args.SkipChange = true;
						_progress.WriteVerbose("[{0}] Because of group policy, would not propagate deletion of {1}", _currentGroup.Name, args.Path);
					}
					else
					{
						_progress.WriteVerbose("[{0}] Preview Delete {1}", _currentGroup.Name, args.Path);
						_currentGroup.DeleteFileCount++;
					}
					break;
			}
			InvokeProgress(args);
		}*/

		private bool ShouldSkip(string mode, MirrorEventArgs args)
		{
			if (args.Situation == MirrorSituation.DirectoryMissing)
			{
				if (_currentGroup.ShouldSkipSubDirectory(args.Path))
				{
					_progress.WriteVerbose("{0} [{1}] Skipping Folder {2}", mode, _currentGroup.Name, args.Path);
					return true;
				}
				//TODO: what about if it is not missing, but should be removed ?
				return false;
			}

			if (_alreadyAccountedFor.Contains(args.Path))
			{
				_progress.WriteVerbose("[{0}] Skipping new file because it was already backed up by a previous group:  {1}", _currentGroup.NewFileCount, args.Path);
				return true;
			}
			if (_currentGroup.ShouldSkipFile(args.Path))
			{
				_progress.WriteVerbose("[{0}] Skipping new file: {1}", _currentGroup.Name, args.Path);
				return true;
			}
			return false;
		}


		/// <summary>
		/// Return true if you want to cancel
		/// </summary>
		public event Func<string, bool> FileProgress;

		public event Action GroupProgress;

		public void InvokeProgress(MirrorEventArgs args)
		{
			if (FileProgress != null)
			{
				if (FileProgress(args.Path))
				{
					_cancelRequested = true;
					_engine.Cancel();
				}
			}
		}


	}
}