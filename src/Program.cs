using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Palaso.UsbDrive;

namespace SafeStick
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
			var preExistingDrives = new List<UsbDriveInfo>();
		//	preExistingDrives.AddRange(UsbDriveInfo.GetDrives());

			var path = @"c:\dev\temp\safeStick";

			//todo remove
//			if (Directory.Exists(path))
//				Directory.Delete(path, true);

			new Form1(path).ShowDialog();

			while (false) 
			{
				var foundDrives = UsbDriveInfo.GetDrives();
				foreach (var info in foundDrives)
				{
					if(preExistingDrives.All(d=>d.RootDirectory.ToString() != info.RootDirectory.ToString()) && info.IsReady)
					{
						using(var form =new Form1(path))
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
	}
}
