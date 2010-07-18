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
		private SyncOrchestrator _agent;
		public int TotalFilesThatWillBeCopied {get{ return _totalFiles;}}

		public Synchronizer(string destinationFolder)
		{
			_agent = new SyncOrchestrator();

			DestinationFolder = destinationFolder;

			//todo remove
			if (Directory.Exists(DestinationFolder))
				Directory.Delete(DestinationFolder, true);

			if (!Directory.Exists(DestinationFolder))
				Directory.CreateDirectory(DestinationFolder);
		}

		public int FilesCopiedThusFar	
		{
			get { return _files;}
		}

		public void GatherInformation()
		{
			_files = 0;
			Run(true);
			_totalFiles = _files;
			_files = 0;
		}

		private void Run(bool previewMode)
		{
			var filter = new FileSyncScopeFilter();
			var options = new FileSyncOptions();
			var srcGuid = Guid.NewGuid();
			var destguid = Guid.NewGuid();
			var group = new ParatextFiles();

			using (var sourceProvider = new FileSyncProvider(srcGuid, group.RootFolder, group.Filter, options))
			using (var usbDestinationProvider = new FileSyncProvider(destguid, DestinationFolder, filter, options))
			{
				sourceProvider.DetectingChanges += new EventHandler<DetectingChangesEventArgs>(sourceProvider_DetectingChanges);
				usbDestinationProvider.PreviewMode = previewMode;

				usbDestinationProvider.ApplyingChange += (OnDestinationApplyingChange);


				_agent.LocalProvider = sourceProvider;
				_agent.RemoteProvider = usbDestinationProvider;
				_agent.Direction = SyncDirectionOrder.Upload; // Synchronize source to destination

				
				_agent.Synchronize();
			}
		}

		private void OnDestinationApplyingChange(object x, ApplyingChangeEventArgs y)
		{
			_files++;
			InvokeProgress();
			
		}

		void sourceProvider_DetectingChanges(object sender, DetectingChangesEventArgs e)
		{

		}

		public void DoSynchronization()
		{
			Run(false);
		}

		/// <summary>
		/// Return true if you want to cancel
		/// </summary>
		public event Func<bool> Progress;

		public void InvokeProgress()
		{
			Func<bool> handler = Progress;
			if (handler != null)
			{
				if(handler())
				{
					_agent.Cancel();
				}
			}
		}
	}
}
