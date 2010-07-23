using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Media;
using System.Reflection;
using System.Windows.Forms;
using UsbEject.Library;

namespace SafetyStick
{
	public partial class MainWindow : Form
	{
		private readonly string _destinationDeviceRoot;
		private readonly long _availableFreeSpaceInKilobytes;
		private readonly long _totalSpaceOfDeviceInKilobytes;
		Synchronizer _synchronizer;
		private BackgroundWorker _preparationWorker;
		private BackgroundWorker _backupWorker;
		private List<FileSource> _groups;

		public MainWindow(string destinationDeviceRoot, long availableFreeSpaceInKilobytes, long totalSpaceOfDeviceInKilobytes)
		{
			//Font = SystemFonts.MessageBoxFont;
			_destinationDeviceRoot = destinationDeviceRoot;
			_availableFreeSpaceInKilobytes = availableFreeSpaceInKilobytes;
			_totalSpaceOfDeviceInKilobytes = totalSpaceOfDeviceInKilobytes;
			InitializeComponent();
			SetWindowText();
			listView1.Visible = false;
			backupNowButton.Visible = false;
			_groups = new List<FileSource>(){
				new ParatextFiles(), 
				//new DevChorusFiles(),
				//new WeSayFiles(), 
				//new OtherFiles(), 
				//new OtherDesktopFiles()
			};

			_synchronizer = new Synchronizer(destinationDeviceRoot, _groups, totalSpaceOfDeviceInKilobytes);
			_synchronizer.GroupProgress +=new Action(OnSynchronizer_GroupProgress);
			
			_mediaStatusIndicator.ExistingFillPercentage = (int)(100.0*availableFreeSpaceInKilobytes / totalSpaceOfDeviceInKilobytes);
			
			//until we know how much we're going to fill up
			_mediaStatusIndicator.PendingFillPercentage = _mediaStatusIndicator.ExistingFillPercentage;
			_mediaStatusIndicator.DeviceSizeInKiloBytes = totalSpaceOfDeviceInKilobytes;

			closeButton.Visible = false;
			cancelButton.Visible = true;

			listView1.Visible = true;
			Cursor = Cursors.WaitCursor;

			_upperStatusLabel.Text = "Looking at what files have changed...";
			_preparationWorker = new BackgroundWorker();
			_preparationWorker.DoWork+= OnPreparationWorker_DoWork;
			_preparationWorker.WorkerSupportsCancellation = true;
			_preparationWorker.RunWorkerAsync();
			_preparationWorker.RunWorkerCompleted += OnPreparationCompleted;
		}

		private void SetWindowText()
		{
			var ver = Assembly.GetExecutingAssembly().GetName().Version;
			Text = string.Format("{0}, build {1}.{2}.{3}", Assembly.GetExecutingAssembly().GetName().Name, ver.Major, ver.Minor, ver.Build);
		}

		private void OnSynchronizer_GroupProgress()
		{
			InvokeIfRequired(() => listView1.Items.Clear());
			foreach (var group in _groups)
			{
				var item = new ListViewItem(group.Name);
				switch(group.Disposition)
				{
					case FileSource.DispositionChoice.Waiting:
						break;
					case FileSource.DispositionChoice.Calculating:
						item.SubItems.Add("Calculating...");
						break;
					case FileSource.DispositionChoice.Synchronizing:
						item.SubItems.Add("Synchronizing...");
						break;
					case FileSource.DispositionChoice.WillBeBackedUp:
						//item.ImageIndex = 0;
						item.SubItems.Add(group.UpdateFileCount + group.NewFileCount +" files to backup.");
						break;
					case FileSource.DispositionChoice.WillBeSkipped:
						item.ImageIndex = 1;
						item.SubItems.Add("Not enough room.");
						break;
					case FileSource.DispositionChoice.WasBackedUp:
						item.ImageIndex = 0;
						item.SubItems.Add("Done");
						break;
				}
				InvokeIfRequired(() => listView1.Items.Add(item));
			}
		}

		void OnPreparationCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			cancelButton.Visible = false;
			Cursor = Cursors.Default;
			listView1.Visible = true;
			backupNowButton.Visible = true;
			syncProgressBar.Maximum = 0;
			foreach (var group in _groups)
			{
				if(group.Disposition == FileSource.DispositionChoice.WillBeBackedUp)
					syncProgressBar.Maximum += group.UpdateFileCount + group.NewFileCount;
			}
			syncProgressBar.Minimum = 0;

			_upperStatusLabel.Visible = false;
			_lowerStatus.Text = string.Format("Will back up {0} files ({1})", _synchronizer.TotalFilesThatWillBeBackedUpThatWillBeCopied, MediaStatus.GetStringForStorageSize(_synchronizer.PredictedSpaceInKiloBytes));
			_lowerStatus.Visible = true;
			_mediaStatusIndicator.PendingFillPercentage = (int)(100.0 * (_availableFreeSpaceInKilobytes - _synchronizer.PredictedSpaceInKiloBytes) / _totalSpaceOfDeviceInKilobytes);
		}



		void OnPreparationWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			_synchronizer.FileProgress += GetIsCancellationPending;
			_synchronizer.GatherInformation();
			_synchronizer.FileProgress -= GetIsCancellationPending;
		
		}

		bool GetIsCancellationPending()
		{
			return _preparationWorker.CancellationPending || (_backupWorker!=null &&_backupWorker.CancellationPending);
				
		}

		/// <summary>
		/// returns true if we want to cancel
		/// </summary>
		/// <returns></returns>
		public bool OnFileProgress()
		{
			InvokeIfRequired(()=>syncProgressBar.Value = _synchronizer.FilesCopiedThusFar);
			return GetIsCancellationPending();
		}

		public void InvokeIfRequired(Action action)
		{
			if (InvokeRequired)
			{
				Invoke(action);
			}
			else
			{
				action();
			}
		}

		private void backupNowButton_Click(object sender, EventArgs e)
		{
			CancelButton = null;
			_lowerStatus.Text = "Copying files...";
			backupNowButton.Visible = false;
			syncProgressBar.Visible = true;
			cancelButton.Visible = true;
			_synchronizer.FileProgress += OnFileProgress;
			_backupWorker = new BackgroundWorker();
			_backupWorker.DoWork += new DoWorkEventHandler(_backupWorker_DoWork);
			_backupWorker.WorkerSupportsCancellation = true;
			_backupWorker.RunWorkerAsync();
			_backupWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OnBackupWorkerCompleted);

		}

		void OnBackupWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			_lowerStatus.Text = "Finished";
			syncProgressBar.Visible = false;
			cancelButton.Visible = false;
			closeButton.Visible = true;
			AcceptButton = closeButton;
			CancelButton = closeButton;
			cancelButton.Visible = false;
			closeButton.Focus();
			Cursor = Cursors.Default;


			try
			{
				using (var x = new UsbEject.Library.VolumeDeviceClass())
				{
					var device = x.Devices.FirstOrDefault(d =>
					                                      	{
					                                      		Volume v = d as Volume;
					                                      		if (v==null)
					                                      			return false;
					                                      		return _destinationDeviceRoot.StartsWith(v.LogicalDrive);
					                                      	});
					if (device != null)
					{
						device.Eject(true);
					}
					else
					{
						_safeToRemoveLabel.Text = "No stick ejected";
					}
					_safeToRemoveLabel.Visible = true;
				}
			}
			catch(Exception err)
			{
				Palaso.Reporting.ErrorReport.NotifyUserOfProblem(err, "Could not eject the usb memory stick.");
			}

			using (var player = new SoundPlayer(Properties.Resources.finished))
			{
				player.Play();
			}
		}

		void _backupWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			_synchronizer.DoSynchronization();		
		}

		private void OnCancelClick(object sender, EventArgs e)
		{
			if (_preparationWorker != null)
			{
				_upperStatusLabel.Text = "Cancelling...";
				_preparationWorker.RunWorkerCompleted +=new RunWorkerCompletedEventHandler((x,y)=>Close());
				_preparationWorker.CancelAsync();
			}
			if(_backupWorker !=null)
			{
				_upperStatusLabel.Text = "Cancelling...";
				syncProgressBar.Visible = false;
				_backupWorker.CancelAsync();
			}
		}

		private void closeButton_Click(object sender, EventArgs e)
		{
			Close();
		}


//
//		void agent_SessionProgress(object sender, SyncStagedProgressEventArgs e)
//		{
//			syncProgressBar.Maximum = (int)e.TotalWork;
//			syncProgressBar.Value = (int)e.CompletedWork;
//		}


		//		private void Form1_Paint(object sender, PaintEventArgs e)
		//		{
		//			using (
		//				LinearGradientBrush brush = new LinearGradientBrush(ClientRectangle,
		//																	Color.FromArgb(255, 255, 255),
		//																	Color.FromArgb(209, 227, 227),
		//																	LinearGradientMode.ForwardDiagonal)
		//				)
		//			{
		//				Blend blend = new Blend();
		//				blend.Positions = new float[] {0, .1f, .35f, .7f, .9f, 1};
		//				blend.Factors = new float[] {0, 0, .5f, .5f, 1, 1};
		//				brush.Blend = blend;
		//
		//				e.Graphics.FillRectangle(brush, e.Graphics.ClipBounds); // 3d effect
		//			}
		//		}
	}
}
