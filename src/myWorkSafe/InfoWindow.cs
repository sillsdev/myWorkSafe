using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using Palaso.IO;
using Palaso.UsbDrive;

namespace myWorkSafe
{
	public partial class InfoWindow : Form
	{
		public InfoWindow()
		{
			InitializeComponent();
            SetWindowText();
		}

		private void InfoWindow_Load(object sender, EventArgs e)
		{
			Activate(); //bring to front
			var path = FileLocator.GetFileDistributedWithApplication( "about.htm");
			_webBrowser.Navigate(path);

		    flowLayoutPanel1.Controls.Clear();
		    var control = new DriveCommandsAndSettingsControl("x: foo");
            flowLayoutPanel1.Controls.Add(control);

		}

        private void SetWindowText()
        {
            var ver = Assembly.GetExecutingAssembly().GetName().Version;
            Text = string.Format("{0}, build {1}.{2}.{3}", Assembly.GetExecutingAssembly().GetName().Name, ver.Major, ver.Minor, ver.Build);
        }

		private void OnExitClick(object sender, EventArgs e)
		{
			DialogResult = System.Windows.Forms.DialogResult.Abort;
			Close();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			DialogResult = System.Windows.Forms.DialogResult.OK;
			Close();
		}

		private void _webBrowser_Navigating(object sender, WebBrowserNavigatingEventArgs e)
		{
			if (_webBrowser.Document != null)
			{
				e.Cancel = true; //we don't want to navigate away from here, we'll launch a browser instead
				Process.Start(e.Url.AbsoluteUri);
			}

		}

		private void _webBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
		{

		}
	}
}
