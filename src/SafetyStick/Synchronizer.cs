using System;
using System.Collections.Generic;
using System.IO;
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
		private readonly long _maxInKilobytes;
		private SyncOrchestrator _agent;
		private FileSource _currentSource;
		public long PredictedSpaceInKiloBytes;
		private bool _cancelRequested;

	
		public Synchronizer(string destinationDeviceRoot, IEnumerable<FileSource> groups, long maxInKilobytes)
		{
			_groups = groups;
			_maxInKilobytes = maxInKilobytes;
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
			_cancelRequested = false;
			_files = 0;
			PredictedSpaceInKiloBytes = 0;
			_totalFilesThatWillBeBackedUp = 0;
			var options = FileSyncOptions.None;

			foreach (var group in _groups)
			{
				if(_cancelRequested)
				{
					break;
				}
				_currentSource = group;//used by callbacks
				group.ClearStatistics();

				if (PredictedSpaceInKiloBytes >= _maxInKilobytes)
				{
					group.Disposition = FileSource.DispositionChoice.WillBeSkipped;
					InvokeGroupProgress();
					continue;
				}
				group.Disposition = FileSource.DispositionChoice.Calculating;
				InvokeGroupProgress();

				
				using (var sourceProvider = new FileSyncProvider(group.SourceGuid, group.RootFolder, group.Filter, options))
				using (var destinationProvider = new FileSyncProvider(group.DestGuid, group.GetDestinationSubFolder(DestinationRootForThisUser), group.Filter, options))
				{
					destinationProvider.PreviewMode = true;
					destinationProvider.ApplyingChange += (OnDestinationPreviewChange);

					sourceProvider.DetectingChanges += (x, y) => InvokeProgress();//just to detect cancel

					PreviewOrSynchronizeCore(destinationProvider, sourceProvider);
				}

				//would this push us over the limit?
				if (PredictedSpaceInKiloBytes >= _maxInKilobytes)
				{
					group.Disposition = FileSource.DispositionChoice.WillBeSkipped;
				} 
				else
				{
					_totalFilesThatWillBeBackedUp += group.UpdateFileCount + group.NewFileCount;
					group.Disposition = FileSource.DispositionChoice.WillBeBackedUp;
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
		
			foreach (var group in _groups)
			{
				if (_cancelRequested)
				{
					break;
				}
				_currentSource = group;//used by callbacks
				if (group.Disposition == FileSource.DispositionChoice.WillBeSkipped)
					continue;

				group.ClearStatistics();
				group.Disposition = FileSource.DispositionChoice.Synchronizing;
				InvokeGroupProgress();
				using (var sourceProvider = new FileSyncProvider(group.SourceGuid, group.RootFolder, group.Filter, options))
				using (var destinationProvider = new FileSyncProvider(group.DestGuid, group.GetDestinationSubFolder(DestinationRootForThisUser), group.Filter, options))
				{
					destinationProvider.PreviewMode = false;
					destinationProvider.ApplyingChange += (OnDestinationChange);
					PreviewOrSynchronizeCore(destinationProvider, sourceProvider);
				}

				group.Disposition = FileSource.DispositionChoice.WasBackedUp;

				if(GroupProgress !=null)
				{
					GroupProgress.Invoke();
				}
			}
			InvokeGroupProgress();
		}

		private void OnDestinationPreviewChange(object x, ApplyingChangeEventArgs y)
		{
			//todo: at the moment, this is never true, there's never a current file, even if the file does exist.
			//Not sure when that is supposed to be available

			if(y.CurrentFileData !=null)
			{
				_currentSource.NetChangeInBytes -= y.CurrentFileData.Size;
				PredictedSpaceInKiloBytes -= y.CurrentFileData.Size/1024;
				//next the new size will be added back, below
			}
			switch(y.ChangeType)
			{
				case ChangeType.Create:
					_currentSource.NewFileCount++;
					_currentSource.NetChangeInBytes += y.NewFileData.Size;
					PredictedSpaceInKiloBytes += y.NewFileData.Size/1024;
					break;
				case ChangeType.Update:
					_currentSource.UpdateFileCount++;
					_currentSource.NetChangeInBytes += y.NewFileData.Size;
					PredictedSpaceInKiloBytes += y.NewFileData.Size/1024;
					break;
				case ChangeType.Delete:
					_currentSource.DeleteFileCount++;
					break;
			}
			InvokeProgress();
		}
		private void OnDestinationChange(object x, ApplyingChangeEventArgs y)
		{
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
