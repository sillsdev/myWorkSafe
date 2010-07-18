using System;
using System.Collections.Generic;
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


			new Form1(@"c:\dev\temp\safeStick").ShowDialog();

			while (false) 
			{
				var foundDrives = UsbDriveInfo.GetDrives();
				foreach (var info in foundDrives)
				{
					if(preExistingDrives.All(d=>d.RootDirectory.ToString() != info.RootDirectory.ToString()) && info.IsReady)
					{
						using(var form =new Form1(@"c:\dev\temp\safeStick"))
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
