using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using myWorkSafe.Properties;
using Palaso.UsbDrive;

namespace myWorkSafe
{
	static class Program
	{
		private static bool _exitNow = false;
	
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			SetUpErrorHandling();

            var trayMenu = new ContextMenu();
            trayMenu.MenuItems.Add("Exit", OnExit);

			using (var trayIcon = new NotifyIcon())
			{
				trayIcon.Text = Application.ProductName;
				trayIcon.Icon = Resources.application;
				trayIcon.BalloonTipText = "myWorkSafe ready for usb memory stick.";

				trayIcon.ContextMenu = trayMenu;
				trayIcon.Visible = true;


			//	DoTestRun();

				var preExistingDrives = new List<UsbDriveInfo>();
				preExistingDrives.AddRange(UsbDriveInfo.GetDrives());

				while (!_exitNow)
				{
					List<UsbDriveInfo> foundDrives = GetFoundDrives();
					foreach (var info in foundDrives)
					{
						if (GetIsADeviceWeShouldTryToBackupTo(preExistingDrives, info))
						{
							try
							{
								long totalSpaceInKilobytes = (long) (info.TotalSize/1024);
								long freeSpaceInKilobytes = (long) (info.AvailableFreeSpace/1024);
								string destinationDeviceRoot = info.RootDirectory.ToString();
								var backupControl = new BackupControl(destinationDeviceRoot, freeSpaceInKilobytes, totalSpaceInKilobytes);
								using (var form = new MainWindow(backupControl))
								{
									form.ShowDialog();
									break;
								}
							}
							catch (Exception error)
							{
								Palaso.Reporting.ErrorReport.NotifyUserOfProblem(error, "Sorry, something went wrong.");
							}
						}
					}
					preExistingDrives = foundDrives;
					if (!_exitNow)
						Thread.Sleep(500);
					if (!_exitNow)
						Thread.Sleep(500);
					if (!_exitNow)
						Thread.Sleep(500);
					if (!_exitNow)
						Thread.Sleep(500);					
				}
			}
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


		private static bool GetIsADeviceWeShouldTryToBackupTo(List<UsbDriveInfo> preExistingDrives, UsbDriveInfo info)
		{
			//some of these drives may not exist anymore (e.g., we just backed up and ejected one),
			//which will give an error which we catch. But we soon regenerate this list, so it's not going
			//to mean more than one exception
			try
			{
				return preExistingDrives.All(d => d.RootDirectory.ToString() != info.RootDirectory.ToString())
				       && info.IsReady;
			}
			catch (Exception error)
			{
				return false;
			}
		}

		private static void OnExit(object sender, EventArgs e)
	        {
		 		//Application.Exit();
		 		_exitNow = true;
	        }

		private static void DoTestRun()
		{
			var destinationDeviceRoot = @"c:\dev\temp\SafetyStick";

//			if (Directory.Exists(path))
//				Directory.Delete(path, true);

			//		var info = new DriveInfo(Path.GetPathRoot(destinationPath));

			var totalSpaceInKilobytes = 900 * 1024;// (int)(100.0 * info.AvailableFreeSpace / info.TotalSize);
			var freeSpaceInKilobytes = 800*1024;
			var backupControl = new BackupControl(destinationDeviceRoot, freeSpaceInKilobytes, totalSpaceInKilobytes);
			new MainWindow(backupControl).ShowDialog();
		}

		private static void SetUpErrorHandling()
		{
			Palaso.Reporting.ErrorReport.AddProperty("EmailAddress", "hattonjohn@gmail.com");
			Palaso.Reporting.ErrorReport.AddStandardProperties();
			Palaso.Reporting.ExceptionHandler.Init();
		}


	}
}
