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
				using (var sourceProvider = new FileSyncProvider(group.SourceGuid, group.RootFolder, group.Filter, options))
				using (var destinationProvider = new FileSyncProvider(group.DestGuid, destinationSubFolder, group.Filter, options))
				{
					destinationProvider.PreviewMode = true;
					destinationProvider.ApplyingChange += (OnDestinationPreviewChange);

					sourceProvider.DetectingChanges += (x, y) => InvokeProgress();//just to detect cancel

					PreviewOrSynchronizeCore(destinationProvider, sourceProvider);
				}

				//is there room to fit in this whole group?
				if(ApprovedChangeInKB + group.NetChangeInBytes < _totalAvailableOnDeviceInKilobytes)
				{
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
		
			foreach (var group in _groups)
			{
				if (_cancelRequested)
				{
					break;
				}
				_currentSource = group;//used by callbacks
				if (group.Disposition == FileSource.DispositionChoice.NotEnoughRoom)
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
