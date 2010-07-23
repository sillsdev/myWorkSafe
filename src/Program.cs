using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Palaso.UsbDrive;

namespace SafetyStick
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			SetUpErrorHandling();

			DoTestRun();

			var preExistingDrives = new List<UsbDriveInfo>();
			preExistingDrives.AddRange(UsbDriveInfo.GetDrives());

		
			while (true) 
			{
				var foundDrives = UsbDriveInfo.GetDrives();
				foreach (var info in foundDrives)
				{
					if(preExistingDrives.All(d=>d.RootDirectory.ToString() != info.RootDirectory.ToString()) && info.IsReady)
					{
						long totalSpaceInKilobytes = (long) (info.TotalSize/1024);
						long freeSpaceInKilobytes =(long) (info.AvailableFreeSpace/1024);
						string destinationDeviceRoot = info.RootDirectory.ToString();
						using(var form =new MainWindow(destinationDeviceRoot, freeSpaceInKilobytes, totalSpaceInKilobytes))
						{
							form.ShowDialog();
							break;
						}
					}
				}
				preExistingDrives = foundDrives;
				Thread.Sleep(2000);
			}
		}

		private static void DoTestRun()
		{
			var destinationDeviceRoot = @"c:\dev\temp\SafetyStick";

//			if (Directory.Exists(path))
//				Directory.Delete(path, true);

			//		var info = new DriveInfo(Path.GetPathRoot(destinationPath));

			var totalSpaceInKilobytes = 90 * 1024;// (int)(100.0 * info.AvailableFreeSpace / info.TotalSize);
			var freeSpaceInKilobytes = 80*1024;
			new MainWindow(destinationDeviceRoot, freeSpaceInKilobytes, totalSpaceInKilobytes).ShowDialog();
		}

		private static void SetUpErrorHandling()
		{
			Palaso.Reporting.ErrorReport.AddProperty("EmailAddress", "hattonjohn@gmail.com");
			Palaso.Reporting.ErrorReport.AddStandardProperties();
			Palaso.Reporting.ExceptionHandler.Init();
		}
	}
}
