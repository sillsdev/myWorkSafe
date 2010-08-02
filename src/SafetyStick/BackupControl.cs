using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Dolinay;
using myWorkSafe.Usb;
using System.IO;
using Timer = System.Threading.Timer;

namespace myWorkSafe
{
	public partial class BackupControl : UserControl
	{
		enum State {Preparing, BackingUp, CouldNotEject, SafelyEjected, ExpectingPhysicalRemoval}

		private State CurrentState;

		private readonly string _destinationDeviceRoot;
		private readonly long _availableFreeSpaceInKilobytes;
		private readonly long _totalSpaceOfDeviceInKilobytes;
		Synchronizer _synchronizer;
		private BackgroundWorker _preparationWorker;
		private BackgroundWorker _backupWorker;
		private List<FileSource> _groups;

		public BackupControl(string destinationDeviceRoot, long availableFreeSpaceInKilobytes, long totalSpaceOfDeviceInKilobytes)
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
				new WeSayFiles(), 
				new OtherFiles(), 
				new OtherDesktopFiles(),
				new WindowsLiveMail(),
				new ThunderbirdMail(),
				new MyPictures(),
				new MyMusic(),
				new MyVideos(),
			};


			var driveDetector = new DriveDetector();

			//TODO: use this instead of polling in the main program
			//driveDetector.DeviceArrived += new DriveDetectorEventHandler(OnDriveArrived);

			//TODO: see if DeviceRemoved could be used instaead of DeviceSomethingHappened
			//driveDetector.DeviceRemoved += new DriveDetectorEventHandler(OnDriveRemoved);
			driveDetector.DeviceSomethingHappened += new DriveDetectorEventHandler(OnDriveSomething);
			//driveDetector.QueryRemove += new DriveDetectorEventHandler(OnQueryRemove);

			_synchronizer = new Synchronizer(destinationDeviceRoot, _groups, availableFreeSpaceInKilobytes);
			_synchronizer.GroupProgress +=new Action(OnSynchronizer_GroupProgress);

			_mediaStatusIndicator.DriveLabel = destinationDeviceRoot;

			_mediaStatusIndicator.ExistingFillPercentage = (int)(100.0*availableFreeSpaceInKilobytes / totalSpaceOfDeviceInKilobytes);
			
			//until we know how much we're going to fill up
			_mediaStatusIndicator.PendingFillPercentage = _mediaStatusIndicator.ExistingFillPercentage;
			_mediaStatusIndicator.DeviceSizeInKiloBytes = totalSpaceOfDeviceInKilobytes;

			closeButton.Visible = false;
			cancelButton.Visible = true;

			listView1.Visible = true;
			Cursor = Cursors.WaitCursor;

			_status.Text = "Looking at what files have changed...";
			_preparationWorker = new BackgroundWorker();
			_preparationWorker.DoWork+= OnPreparationWorker_DoWork;
			_preparationWorker.WorkerSupportsCancellation = true;
			_preparationWorker.RunWorkerCompleted += OnPreparationCompleted;
		}

		private void OnDriveSomething(object sender, DriveDetectorEventArgs e)
		{

			//we can't actually get the info for what the drive used to be... ///if (e.Drive == _destinationDeviceRoot)
			
			//if we previously ejected, then this message means it was pulled out, so we can close
			if(CurrentState== State.ExpectingPhysicalRemoval && !Directory.Exists(_destinationDeviceRoot))
			{
				OnCancelClick(this, null);
				CloseNow();

			}
		}

		private void SetWindowText()
		{
			var ver = Assembly.GetExecutingAssembly().GetName().Version;
			Text = string.Format("{0}, build {1}.{2}.{3}", Assembly.GetExecutingAssembly().GetName().Name, ver.Major, ver.Minor, ver.Build);
		}

		private void OnSynchronizer_GroupProgress()
		{
			InvokeIfRequired(() =>listView1.Items.Clear());
			foreach (var group in _groups)
			{
				var item = new ListViewItem(group.Name);
				switch(group.Disposition)
				{
					case FileSource.DispositionChoice.Hide:
						continue;
						break;
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
					case FileSource.DispositionChoice.NotEnoughRoom:
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

			if (_synchronizer.TotalFilesThatWillBeBackedUpThatWillBeCopied > 0)
			{
				_status.Text = string.Format("Will back up {0} files ({1})",
				                             _synchronizer.TotalFilesThatWillBeBackedUpThatWillBeCopied,
				                             MediaStatus.GetStringForStorageSize(_synchronizer.ApprovedChangeInKB));
			}
			else
			{
				_status.Text = "No files need to be backed up";
				closeButton.Visible = true;
				backupNowButton.Visible = false;
			}

			_mediaStatusIndicator.PendingFillPercentage = (int)(100.0 * (_availableFreeSpaceInKilobytes - _synchronizer.ApprovedChangeInKB) / _totalSpaceOfDeviceInKilobytes);
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
			InvokeIfRequired(()=>
			                 	{
									if (_synchronizer.FilesCopiedThusFar >= syncProgressBar.Minimum
										&& _synchronizer.FilesCopiedThusFar <= syncProgressBar.Maximum)
										syncProgressBar.Value = _synchronizer.FilesCopiedThusFar;
								});
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
			_status.Text = "Copying files...";
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
			_status.Text = "Finished";
			syncProgressBar.Visible = false;
			cancelButton.Visible = false;
			closeButton.Visible = true;
			cancelButton.Visible = false;
			closeButton.Focus();
			Cursor = Cursors.Default;

			AttemptEjectInAMoment();
		}

		private void AttemptEjectInAMoment()
		{
			var timer = new System.Windows.Forms.Timer();
			timer.Tick += ((o, args) =>
			                                  	{
			                                  		((System.Windows.Forms.Timer) o).Enabled = false;
			                                  		TryToEjectUsb();
			                                  	});
			timer.Interval = 100;
			timer.Enabled = true;
		}

		private void TryToEjectUsb()
		{
			if (AttemptUsbEject())
			{
				using (var player = new SoundPlayer(Properties.Resources.finished))
				{
					player.Play();
				}
				CurrentState = State.SafelyEjected;
				//ok, now wait a second. 
				_probablyRemovedPhysicallyTimer.Enabled = true;
				//then, next time a usb device is removed, we'll assume
				//it was this one and quit. (it might be possible to get more firm info, 
				//but this device level is a murky world...
			}
			else
			{
				CurrentState = State.CouldNotEject;
			}
		}


		private bool AttemptUsbEject()
		{
			try
			{
				using (var x = new VolumeDeviceClass())
				{
					var device = x.Devices.FirstOrDefault(d =>
					                                      	{
					                                      		Volume v = d as Volume;
					                                      		if (v == null || v.LogicalDrive==null)
					                                      			return false;
					                                      		return _destinationDeviceRoot.StartsWith(v.LogicalDrive);
					                                      	});
					string ejectionResult="---";
					if (device != null)
					{
						Thread.Sleep(1000);//wait for our actions to die down
						ejectionResult = device.Eject(false);
					}
					if (ejectionResult != null)
					{
						Thread.Sleep(2000);//wait for our actions to die down
						ejectionResult = device.Eject(false);
						if (ejectionResult != null)
						{
							SystemSounds.Asterisk.Play();
							_safeToRemoveLabel.Text = ejectionResult;
						}
					}
					_safeToRemoveLabel.Visible = true;
					return ejectionResult==null;
				}
			}
			catch(Exception err)
			{
				Palaso.Reporting.ErrorReport.NotifyUserOfProblem(err, "Could not eject the usb memory stick.");
				return false;
			}


		}

		void _backupWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			CurrentState = State.BackingUp;
			_synchronizer.DoSynchronization();		
		}

		private void OnCancelClick(object sender, EventArgs e)
		{
			if (_preparationWorker != null)
			{
				_status.Text = "Cancelling...";
				_preparationWorker.RunWorkerCompleted +=new RunWorkerCompletedEventHandler((x,y)=>Application.Exit());
				_preparationWorker.CancelAsync();
			}
			if(_backupWorker !=null)
			{
				_status.Text = "Cancelling...";
				syncProgressBar.Visible = false;
				_backupWorker.CancelAsync();
			}
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


		public event Action CloseNow;

		private void BackupControl_Load(object sender, EventArgs e)
		{
			CurrentState = State.Preparing;
			_preparationWorker.RunWorkerAsync();
		}

		private void _probablyRemovedPhysicallyTimer_Tick(object sender, EventArgs e)
		{
			//Ok, since this fired, so some time has passed since we unmounted the usb device.
			//Now enter the stage where, the next time a usb device is removed, we'll assume
			//it was this the device we were backing up to, and quit. (it might be possible to get more firm info, 
			//but this device level is a murky world...

			CurrentState = State.ExpectingPhysicalRemoval;
			_probablyRemovedPhysicallyTimer.Enabled = false;
		}
	}
}

	

