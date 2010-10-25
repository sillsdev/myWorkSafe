using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Dolinay;
using Localization;
using myWorkSafe.Properties;
using Palaso.Reporting;
using Palaso.UsbDrive;


namespace myWorkSafe
{
	static class Program
	{
		private static string _pendingDriveArrival;

		[STAThread]
		static void Main(string[] args)
		{
			bool createdNew;
			using (new Mutex(true, "myWorkSafe", out createdNew))
			{
				if (createdNew)
				{
					RunCore(args);
				}
			}

		}

		private static void RunCore(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			SetUpErrorHandling();

			LocalizationManager.Enabled = true;
			LocalizationManager.Initialize();
			//LocalizationManager.UILangId = "fr";

			var trayMenu = new ContextMenu();
			trayMenu.MenuItems.Add("Exit", OnExit);

			if(args.Length == 1 && args[0].Trim()=="afterInstall")
			{
				var info = new InfoWindow();
				info.Text = "myWorkSafe Installed";
				info.ShowDialog();
			}

			using(var detector = new DriveDetector())
			using (var trayIcon = new NotifyIcon())
			{
				trayIcon.Text = Application.ProductName;
				trayIcon.Icon = Resources.application;
				trayIcon.BalloonTipText = "myWorkSafe ready for usb memory stick.";
                          
				trayIcon.ContextMenu = trayMenu;
				trayIcon.Visible = true;
				trayIcon.MouseClick += new MouseEventHandler(trayIcon_MouseClick);

				detector.DeviceArrived += new DriveDetectorEventHandler(OnDeviceArrived);
				Application.Idle += new EventHandler(Application_Idle);
				Application.Run();
			}
		}

		static void trayIcon_MouseClick(object sender, MouseEventArgs e)
		{
			if(DialogResult.Abort == new InfoWindow().ShowDialog())
				Application.Exit();
		}

		static void Application_Idle(object sender, EventArgs e)
		{
			if (_pendingDriveArrival != null)
			{
				try
				{
					var driveLetter = _pendingDriveArrival;
					_pendingDriveArrival = null;
					HandleDeviceArrived(driveLetter);
				}
				catch (Exception error)
				{
					Palaso.Reporting.ErrorReport.NotifyUserOfProblem(error, "Sorry, something went wrong.");
				}
			}						
		}

		static void OnDeviceArrived(object sender, DriveDetectorEventArgs e)
		{
			///messes things up to handle it right now... wait and do it in our normal app loop
			_pendingDriveArrival = e.Drive;			
		}

		static void HandleDeviceArrived(string driveLetter)
		{
			MultiProgress progress= new MultiProgress(new IProgress[]{});
			List<UsbDriveInfo> foundDrives = GetFoundDrives();
			var drive = foundDrives.FirstOrDefault(d => d.RootDirectory.ToString() == driveLetter);
			if (drive == null || !drive.IsReady)
				return;
			try
			{
				if (!IsAKnownBackupDrive(drive))
				{
					if(DialogResult.Yes == new NewDrivePopup().ShowDialog())
					{
						Directory.CreateDirectory(BackupControl.GetDestinationFolderPath(drive.RootDirectory.ToString()));
					}
				}
				//based on that popup, it might now pass this test:
				if (IsAKnownBackupDrive(drive))
				{
					long totalSpaceInKilobytes = (long) (drive.TotalSize/1024);
					long freeSpaceInKilobytes = (long) (drive.AvailableFreeSpace/1024);
					string destinationDeviceRoot = drive.RootDirectory.ToString();
					var backupControl = new BackupControl(destinationDeviceRoot, freeSpaceInKilobytes, totalSpaceInKilobytes, progress);
                    UsageReporter.ReportLaunchesAsync(); 
                    using (var form = new MainWindow(backupControl, progress))
					{
						form.ShowDialog();
					}
				}
			}
			catch (Exception error)
			{
				Palaso.Reporting.ErrorReport.NotifyUserOfProblem(error, "Sorry, something went wrong.");
			}
		}

		private static bool IsAKnownBackupDrive(UsbDriveInfo drive)
		{
			return Directory.Exists(BackupControl.GetDestinationFolderPath(drive.RootDirectory.ToString()));
		}

		private static List<UsbDriveInfo> GetFoundDrives()
		{
			var foundDrives = new List<UsbDriveInfo>();
			try
			{
				foundDrives = UsbDriveInfo.GetDrives();
			}
			catch
			{
				//swallow, just return the empty set of drives
			}
			return foundDrives;
		}


		private static void OnExit(object sender, EventArgs e)
	        {
		 		Application.Exit();
	        }

		private static void DoTestRun()
		{
			MultiProgress progress = new MultiProgress(new IProgress[] { });
			var destinationDeviceRoot = @"c:\dev\temp\SafetyStick";

//			if (Directory.Exists(path))
//				Directory.Delete(path, true);

			//		var info = new DriveInfo(Path.GetPathRoot(destinationPath));

			var totalSpaceInKilobytes = 900 * 1024;// (int)(100.0 * info.AvailableFreeSpace / info.TotalSize);
			var freeSpaceInKilobytes = 800*1024;
			var backupControl = new BackupControl(destinationDeviceRoot, freeSpaceInKilobytes, totalSpaceInKilobytes, progress);
			backupControl.DoPreview = false;
			backupControl.AutoStart = true;

			new MainWindow(backupControl, progress).ShowDialog();
		}

		private static void SetUpErrorHandling()
		{
            ErrorReport.EmailAddress = "hide@gmail.org".Replace("hide", "hattonjohn");
			Palaso.Reporting.ErrorReport.AddStandardProperties();
			Palaso.Reporting.ExceptionHandler.Init();

            //Note: we're going to do this when the app window opens, rather than at start up.  UsageReporter.ReportLaunchesAsync();
		}
	}
}
