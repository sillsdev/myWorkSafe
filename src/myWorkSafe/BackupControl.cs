﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Dolinay;
using myWorkSafe.Groups;
using myWorkSafe.Usb;
using System.IO;
using SIL.IO;
using SIL.Progress;
using SIL.Reporting;


namespace myWorkSafe
{
	public partial class BackupControl : UserControl
	{
		enum State {Preparing, BackingUp, CouldNotEject, SafelyEjected, ExpectingPhysicalRemoval,
			ReadyToBackup,
			Succeeded,
			ErrorEncountered,
			Cancelled
		}

		private State CurrentState;

		private readonly string _destinationDeviceRoot;
		private readonly long _availableFreeSpaceInKilobytes;
		private readonly long _totalSpaceOfDeviceInKilobytes;
		MirrorController _controller;
		private BackgroundWorker _preparationWorker;
		private BackgroundWorker _backupWorker;
		private List<FileGroup> _groups;
		private DriveDetector _driveDetector;

		public BackupControl(string destinationDeviceRoot, long availableFreeSpaceInKilobytes, long totalSpaceOfDeviceInKilobytes, IProgress progress)
		{
			Progress = progress;
			//Font = SystemFonts.MessageBoxFont;
			_destinationDeviceRoot = destinationDeviceRoot;
			_availableFreeSpaceInKilobytes = availableFreeSpaceInKilobytes;
			_totalSpaceOfDeviceInKilobytes = totalSpaceOfDeviceInKilobytes;
			InitializeComponent();
			SetWindowText();
			listView1.Visible = false;
			backupNowButton.Visible = false;
	
			string destinationFolderPath = GetDestinationFolderPath(destinationDeviceRoot);
			if (!Directory.Exists(destinationFolderPath))
				Directory.CreateDirectory(destinationFolderPath);

			ReadInGroups(destinationFolderPath);

			DoPreview = false;
			AutoStart = true;

			syncProgressBar.Style = ProgressBarStyle.Marquee; //until we have an estimate

			_driveDetector = new DriveDetector();

			//TODO: use this instead of polling in the main program
			//driveDetector.DeviceArrived += new DriveDetectorEventHandler(OnDriveArrived);

			//TODO: see if DeviceRemoved could be used instaead of DeviceSomethingHappened
            _driveDetector.DeviceRemoved += new DriveDetectorEventHandler(OnDriveSomething);
			//_driveDetector.DeviceSomethingHappened += new DriveDetectorEventHandler(OnDriveSomething);
			//driveDetector.QueryRemove += new DriveDetectorEventHandler(OnQueryRemove);


			//_controller = new Synchronizer(destinationFolderPath, _groups, availableFreeSpaceInKilobytes, Progress);
			_controller = new MirrorController(destinationFolderPath, _groups, availableFreeSpaceInKilobytes, Progress);
			_controller.GroupProgress +=new Action(OnSynchronizer_GroupProgress);

			_mediaStatusIndicator.DriveLabel = destinationDeviceRoot;

			_mediaStatusIndicator.ExistingFillPercentage = 100-(int)(100.0*availableFreeSpaceInKilobytes / totalSpaceOfDeviceInKilobytes);
			
			//until we know how much we're going to fill up
			_mediaStatusIndicator.PendingFillPercentage = _mediaStatusIndicator.UnknownFillPercentage;
			_mediaStatusIndicator.DeviceSizeInKiloBytes = totalSpaceOfDeviceInKilobytes;

			closeButton.Visible = false;
			cancelButton.Visible = true;
			_status.Text = "";
			listView1.Visible = true;
			Cursor = Cursors.WaitCursor;

			_preparationWorker = new BackgroundWorker();
			_preparationWorker.DoWork += OnPreparationWorker_DoWork;
			_preparationWorker.WorkerSupportsCancellation = true;
			_preparationWorker.RunWorkerCompleted += OnPreparationCompleted;
		}

		/// <summary>
		/// We want to name the root of the backup in a way that allows multiple
		/// team members to use the same key if necessary, and to help support
		/// staff identify whose backup they are looking at. Ideally, the computer
		/// name would have the language name.
		/// </summary>
		public static string GetDestinationFolderPath(string destinationDeviceRoot)
		{
			var id = string.Format("myWorkSafe-{0}-{1}", System.Environment.UserName, System.Environment.MachineName);
			var path = Path.Combine(destinationDeviceRoot, id);
			return path;
		}

		public bool DoPreview { get; set; }

		public bool AutoStart { get; set; }

		public IProgress Progress { get; set; }

		private void ReadInGroups(string destinationFolderPath)
		{
			var path = FileLocator.GetFileDistributedWithApplication("myWorkSafe.ini");
			_groups = new List<FileGroup>();
			var factoryGroupsReader = new GroupIniFileReader(path);
			factoryGroupsReader.CreateGroups(_groups);

			//look for a user-override file
			var dir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			dir = Path.Combine(dir, "myWorkSafe");
			if(!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);//so it's easier for people to find

			}
			var ini = Path.Combine(dir, "myWorkSafe.ini");
			if (File.Exists(ini))
			{
				Progress.WriteMessage("Found settings override in users's application data.");
				var customGroupsReader = new GroupIniFileReader(ini);
				customGroupsReader.CreateGroups(_groups);
			}
			else
			{
				File.WriteAllText(ini, "#Enter your own or modified groups here, to customize myWorkSafe behavior");
			}

			//look for override file on the drive itself
			var iniPath = Path.Combine(destinationFolderPath, "myWorkSafe.ini");
			if (File.Exists(iniPath))
			{
				Progress.WriteMessage("Found settings override on USB device.");
				var customGroupsReader = new GroupIniFileReader(iniPath);
				customGroupsReader.CreateGroups(_groups);
			}
		}


		private void OnDriveSomething(object sender, DriveDetectorEventArgs e)
		{
            //I think the idea here was to auto-quit if the usb is removed, but...

		    return;//this was leading to a crash on XP becuase the log was going to a window that had been closed (I think)
			//we can't actually get the info for what the drive used to be... ///if (e.Drive == _destinationDeviceRoot)
            Debug.Fail("In release build, would be closing.");
			switch (CurrentState)
			{
				case State.Preparing:
				case State.BackingUp:
			        Progress.WriteError("Got Drive event ({0}) while preparing or backing up. Cancelling.", e.Drive);
					OnCancelClick(this, null);
					CloseNow();
					break;
				case State.CouldNotEject:
				case State.Cancelled:
				case State.SafelyEjected:
				case State.ExpectingPhysicalRemoval:
				case State.ReadyToBackup:
				case State.Succeeded:
				case State.ErrorEncountered:
			        Progress.WriteError("Got Drive event ({0}) while in state {1}. Closing.", e.Drive, CurrentState.ToString());
					CloseNow();
					break;
				default:
					throw new ArgumentOutOfRangeException();
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
					case FileGroup.DispositionChoice.Hide:
						continue;
						break;
					case FileGroup.DispositionChoice.Waiting:
						break;
					case FileGroup.DispositionChoice.Calculating:
						item.SubItems.Add("Calculating...");
						break;
					case FileGroup.DispositionChoice.Synchronizing:
						item.SubItems.Add("Mirroring...");//LocalizationManager.LocalizeString("MirroringStatus","Mirroring...","What is shown while the copying is going on", "BackupControl",LocalizationCategory.Label,LocalizationPriority.High));
						break;
					case FileGroup.DispositionChoice.WillBeBackedUp:
						//item.ImageIndex = 0;
						item.SubItems.Add(group.UpdateFileCount + group.NewFileCount +" files to backup.");
						break;
					case FileGroup.DispositionChoice.NotEnoughRoom:
						//item.ImageIndex = 1;
						item.SubItems.Add("Not enough room.");
						item.ForeColor = Color.DarkRed;
						break;
					case FileGroup.DispositionChoice.Cancelled:
						item.SubItems.Add("Cancelled.");
						item.ForeColor = Color.DarkRed;
						break;
					case FileGroup.DispositionChoice.WasBackedUp:
						item.ImageIndex = 0;
						item.SubItems.Add("Done");
						break;
				}
				InvokeIfRequired(() => listView1.Items.Add(item));
			}
		}

		void ChangeState(State state)
		{
			CurrentState = state;
			switch(CurrentState)
			{
				case State.Preparing:
					_status.Text = "Looking at what files have changed...";
					backupNowButton.Visible = false;
					syncProgressBar.Visible = false;
					cancelButton.Visible = true;
					break;
				case State.ReadyToBackup:
					cancelButton.Visible = false;
					Cursor = Cursors.Default;
					listView1.Visible = true;
					backupNowButton.Visible = true;
					break;
				case State.BackingUp:
					_status.Text = "Copying files...";
					_updateMediaStatusTimer.Enabled = true;
					backupNowButton.Visible = false;
					syncProgressBar.Visible = true;
					cancelButton.Visible = true;
					break;
				case State.Succeeded:
					_status.Text = "Finished";
					_updateMediaStatusTimer.Enabled = false;
					syncProgressBar.Visible = false;
					closeButton.Visible = true;
					cancelButton.Visible = false;
					closeButton.Focus();
					Cursor = Cursors.Default;
                    using (var player = new SoundPlayer(Properties.Resources.finished))
                    {
                        player.Play();
                    }
					break;
				case State.Cancelled:
					_status.Text = "Cancelled";
					_updateMediaStatusTimer.Enabled = false;
					syncProgressBar.Visible = false;
					closeButton.Visible = true;
					cancelButton.Visible = false;
					closeButton.Focus();
					Cursor = Cursors.Default;
					break;
				case State.ErrorEncountered:
					_status.Text = "See the Log for error information.";
					_status.ForeColor = Color.Red;
					_updateMediaStatusTimer.Enabled = false;
					syncProgressBar.Visible = false;
					closeButton.Visible = true;
					cancelButton.Visible = false;
					closeButton.Focus();
					Cursor = Cursors.Default;
					using (var player = new SoundPlayer(Properties.Resources.error))
					{
						player.Play();
					}
					break;
		
				case State.CouldNotEject:
					break;
				case State.SafelyEjected:
					break;
				case State.ExpectingPhysicalRemoval:
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		void OnPreparationCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			syncProgressBar.Style = ProgressBarStyle.Blocks;//now that we have an estimate
			ChangeState(State.ReadyToBackup);
			syncProgressBar.Maximum = 0;
			foreach (var group in _groups)
			{
				if(group.Disposition == FileGroup.DispositionChoice.WillBeBackedUp)
					syncProgressBar.Maximum += group.UpdateFileCount + group.NewFileCount;
			}
			syncProgressBar.Minimum = 0;

			if (_controller.TotalFilesThatWillBeBackedUpThatWillBeCopied > 0)
			{
				_status.Text = string.Format("Will back up {0} files ({1})",
				                             _controller.TotalFilesThatWillBeBackedUpThatWillBeCopied,
				                             MediaStatus.GetStringForStorageSize(_controller.ApprovedChangeInKB));
			}
			else
			{
				_status.Text = "No files need to be backed up";
				closeButton.Visible = true;
				backupNowButton.Visible = false;
			}

			_mediaStatusIndicator.PendingFillPercentage = (int)(100.0 * (_availableFreeSpaceInKilobytes - _controller.ApprovedChangeInKB) / _totalSpaceOfDeviceInKilobytes);
		}



		void OnPreparationWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			_controller.FileProgress += GetIsCancellationPending;
			_controller.GatherPreview();
			_controller.FileProgress -= GetIsCancellationPending;
		
		}

		bool GetIsCancellationPending(MirrorEventArgs unused)
		{
			return _preparationWorker.CancellationPending || (_backupWorker!=null &&_backupWorker.CancellationPending);
				
		}

		/// <summary>
		/// returns true if we want to cancel
		/// </summary>
		/// <returns></returns>
        public bool OnFileProgress(MirrorEventArgs args)
		{
			InvokeIfRequired(()=>
			                 	{
									if (_controller.FilesCopiedThusFar >= syncProgressBar.Minimum
										&& _controller.FilesCopiedThusFar <= syncProgressBar.Maximum)                                       
									{
										syncProgressBar.Value = _controller.FilesCopiedThusFar;
									}
									var max = 40;
									string display="";
			                 		var parts = args.Path.Split(new char[] {Path.DirectorySeparatorChar});
									//in case the neame itself is too long...
									if (parts.Length == 0)
										return;
									if (parts[parts.Length -1].Length > max)
									{
										var length = args.Path.Length;
										var start = length > max ? length - max : 0;
										length = length > max ? max : length;
										display = args.Path.Substring(start, length);
									}
									else for (int i = parts.Length-1; i >= 0 ; i--)
			                 		{
										var potential = parts[i] + "/" + display;
										if (potential.Length > max)
											break;
			                 			display = potential;
			                 		}
			                 	    display = display.TrimEnd(new char []{'/'});

                                    switch(args.PendingAction)
                                    {
                                        case MirrorAction.Skip:
			                 	            _status.Text = string.Format("Skipping {0}", display);
                                            break;
                                        case MirrorAction.Create:
                                            if (args.Situation == MirrorSituation.DirectoryMissing)
                                            {
                                                break; // don't make it look like we're copying a directory which we may later delete (because it ends up empty), which is then confusing "Why does it keep copying that thing over and over?"
                                            }
			                 	            _status.Text = string.Format("Copying {0}", display);
                                            break;
                                        case MirrorAction.Delete:
			                 	            _status.Text = string.Format("Processing {0}", display); //nb "deleting" would scare people
                                            break;
                                        case MirrorAction.DoNothing:
                                            _status.Text = string.Format("Checking {0}", display);  //another euphemism
                                            break;
                                        case MirrorAction.Update:
			                 	            _status.Text = string.Format("Updating {0}", display);
                                            break;
                                        default:
                                            break;
                                    }

								});
			return GetIsCancellationPending(args);
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
			StartBackup();
		}

		private void StartBackup()
		{
			ChangeState(State.BackingUp);
			_controller.FileProgress += OnFileProgress;
			_backupWorker = new BackgroundWorker();
			_backupWorker.DoWork += new DoWorkEventHandler(_backupWorker_DoWork);
			_backupWorker.WorkerSupportsCancellation = true;
			_backupWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OnBackupWorkerCompleted);
			_backupWorker.RunWorkerAsync();
		}

		void OnBackupWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
            try //In Oct-2011 I got a typically vague null value crash report in this method.  
                //Added the check to ParentForm above, and am wrapping it in a catch block
		    {
		        if (Progress.ErrorEncountered)
		        {
		            ChangeState(State.ErrorEncountered);
		        }
		        else
		        {
		            ChangeState(State.Succeeded);

		            if (ParentForm == null || !((MainWindow) ParentForm).IsActiveWindow)
		            {
		                using (var dlg = new AllDonePopup())
		                {
		                    dlg.ShowDialog();
		                    CloseNow.Invoke();
		                }
		            }

		            //AttemptEjectInAMoment();
		        }
		    }
            catch(Exception error) 
            {
#if DEBUG
                throw error;    // nothing in here is worth talking to the user about
#endif
            }
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
				ChangeState(State.SafelyEjected);
				//ok, now wait a second. 
				_probablyRemovedPhysicallyTimer.Enabled = true;
				//then, next time a usb device is removed, we'll assume
				//it was this one and quit. (it might be possible to get more firm info, 
				//but this device level is a murky world...
			}
			else
			{
				ChangeState(State.CouldNotEject);
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
				SIL.Reporting.ErrorReport.NotifyUserOfProblem(err, "Could not eject the usb memory stick.");
				return false;
			}


		}

		void _backupWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			_controller.Run();		
		}

		private void OnCancelClick(object sender, EventArgs e)
		{
			if (_preparationWorker != null)
			{
				_status.Text = "Cancelling...";
				_preparationWorker.RunWorkerCompleted +=new RunWorkerCompletedEventHandler((x,y)=>ChangeState(State.Cancelled));
				_preparationWorker.CancelAsync();
			}
			if(_backupWorker !=null)
			{
				_status.Text = "Cancelling...";
				syncProgressBar.Visible = false;
				_backupWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler((x, y) => ChangeState(State.Cancelled));
				_backupWorker.CancelAsync();
			}
            UsageReporter.SendEvent("Backup","Command","CancelBackup","",0);
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
			//if we start too soon, sometime the progress events are called before the window
			//is created and ready to receive them.
			_startTimer.Enabled = true;
		}

		private void _probablyRemovedPhysicallyTimer_Tick(object sender, EventArgs e)
		{
			//Ok, since this fired, so some time has passed since we unmounted the usb device.
			//Now enter the stage where, the next time a usb device is removed, we'll assume
			//it was this the device we were backing up to, and quit. (it might be possible to get more firm info, 
			//but this device level is a murky world...

			ChangeState(State.ExpectingPhysicalRemoval);
			_probablyRemovedPhysicallyTimer.Enabled = false;
		}

		private void OnStartTick(object sender, EventArgs e)
		{
			_startTimer.Enabled = false;
			if (DoPreview)
			{
				ChangeState(State.Preparing);
				_preparationWorker.RunWorkerAsync();
			}
			else
			{
				if(AutoStart)
				{
					StartBackup();
				}
				else
				{
					ChangeState(State.ReadyToBackup);
				}
			}
		}

		private void _updateMediaStatusTimer_Tick(object sender, EventArgs e)
		{
			var info =DriveInfo.GetDrives().FirstOrDefault(d => d.RootDirectory.Name == _destinationDeviceRoot);
			if (info == null)
				return;
			_mediaStatusIndicator.ExistingFillPercentage = 100-(int)(100.0 * info.AvailableFreeSpace / info.TotalSize);
		}
	}
}

	

