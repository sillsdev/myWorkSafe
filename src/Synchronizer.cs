using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Synchronization;
using Microsoft.Synchronization.Files;

namespace SafeStick
{
	public class Synchronizer
	{       
		private int _totalFiles;
		private int _files;
		public string DestinationFolder;
		private readonly IEnumerable<FileSource> _groups;
		private readonly long _maxInKilobytes;
		private SyncOrchestrator _agent;
		public int TotalFilesThatWillBeCopied {get{ return _totalFiles;}}

		public Synchronizer(string destinationFolder, IEnumerable<FileSource> groups, long maxInKilobytes)
		{
			_agent = new SyncOrchestrator();

			DestinationFolder = destinationFolder;
			_groups = groups;
			_maxInKilobytes = maxInKilobytes;


			if (!Directory.Exists(DestinationFolder))
				Directory.CreateDirectory(DestinationFolder);
		}

		public int FilesCopiedThusFar	
		{
			get { return _files;}
		}

		private FileSource _currentSource;
		private long _totalKiloBytesCalculatedThusFar;

		public void GatherInformation()
		{
			_files = 0;
			_totalKiloBytesCalculatedThusFar = 0;
			var options = FileSyncOptions.None;

			foreach (var group in _groups)
			{
				_currentSource = group;//used by callbacks
				group.ClearStatistics();

				if (_totalKiloBytesCalculatedThusFar >= _maxInKilobytes)
				{
					group.Disposition = FileSource.DispositionChoice.WillBeSkipped;
					InvokeGroupProgress();
					continue;
				}
				group.Disposition = FileSource.DispositionChoice.Calculating;
				InvokeGroupProgress();

				using (var sourceProvider = new FileSyncProvider(group.SourceGuid, group.RootFolder, group.Filter, options))
				using (var destinationProvider = new FileSyncProvider(group.DestGuid, DestinationFolder, group.Filter, options))
				{
					destinationProvider.PreviewMode = true;
					destinationProvider.ApplyingChange += (OnDestinationPreviewChange);
					
					_agent.LocalProvider = sourceProvider;
					_agent.RemoteProvider = destinationProvider;
					_agent.Direction = SyncDirectionOrder.Upload; // Synchronize source to destination
					_agent.Synchronize();
				}
				group.Disposition = FileSource.DispositionChoice.WillBeBackedUp;
			}
			InvokeGroupProgress();

			_totalFiles = _files;
			_files = 0;
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
			var options = FileSyncOptions.RecycleDeletedFiles;
		
			foreach (var group in _groups)
			{
				_currentSource = group;//used by callbacks
				if (group.Disposition == FileSource.DispositionChoice.WillBeSkipped)
					continue;

				group.ClearStatistics();
				group.Disposition = FileSource.DispositionChoice.Synchronizing;
				InvokeGroupProgress();
				using (var sourceProvider = new FileSyncProvider(group.SourceGuid, group.RootFolder, group.Filter, options))
				using (var destinationProvider = new FileSyncProvider(group.DestGuid, DestinationFolder, group.Filter, options))
				{
					destinationProvider.PreviewMode = false;
					destinationProvider.ApplyingChange += (OnDestinationChange);
					_agent.LocalProvider = sourceProvider;
					_agent.RemoteProvider = destinationProvider;
					_agent.Direction = SyncDirectionOrder.Upload; // Synchronize source to destination
					
					_agent.Synchronize();
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
				_totalKiloBytesCalculatedThusFar -= y.CurrentFileData.Size/1024;
				//next the new size will be added back, below
			}
			switch(y.ChangeType)
			{
				case ChangeType.Create:
					_currentSource.NewFileCount++;
					_currentSource.NetChangeInBytes += y.NewFileData.Size;
					_totalKiloBytesCalculatedThusFar += y.NewFileData.Size/1024;
					break;
				case ChangeType.Update:
					_currentSource.UpdateFileCount++;
					_currentSource.NetChangeInBytes += y.NewFileData.Size;
					_totalKiloBytesCalculatedThusFar += y.NewFileData.Size/1024;
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
					_agent.Cancel();
				}
			}
		}

//		private SyncId GetSyncId(string idFilePath)
//		{
//			SyncId replicaId = null;
//
			//Try to read existing ReplicaID
//			if (File.Exists(idFilePath))
//			{
//				using (StreamReader sr = File.OpenText(idFilePath))
//				{
//					string strGuid = sr.ReadLine();
//					if (!string.IsNullOrEmpty(strGuid))
//					{
//						replicaId = new SyncId(new Guid(strGuid));
//					}
//				}
//			}
			//If not exist, Create ReplicaID file
//			if (replicaId == null)
//			{
//				using (FileStream idFile = File.Open(idFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
//				{
//					using (StreamWriter sw = new StreamWriter(idFile))
//					{
//						replicaId = new SyncId(Guid.NewGuid());
//						sw.WriteLine(replicaId.GetGuidId().ToString("D"));
//					}
//				}
//			}
//
//			return replicaId;
//		}
	}
}
