using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using myWorkSafe.Usb;
using Palaso.IO;

namespace myWorkSafe
{
	public partial class MainWindow : Form
	{
		public MainWindow(BackupControl backupControl, MultiProgress progress)
		{
			//Font = SystemFonts.MessageBoxFont;
			InitializeComponent();
			SetWindowText();
			backupControl.Dock = DockStyle.Fill;
			backupControl.CloseNow += () => Close();
			_backupPage.Controls.Add(backupControl);
			progress.Add(_logBox);
		}

		private void SetWindowText()
		{
			var ver = Assembly.GetExecutingAssembly().GetName().Version;
			Text = string.Format("{0}, build {1}.{2}.{3}", Assembly.GetExecutingAssembly().GetName().Name, ver.Major, ver.Minor, ver.Build);
		}
		
		
		private void tabControl1_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			//we delay making one of these until we have to
			if(tabControl1.SelectedIndex == 1 && _aboutPage.Controls.Count ==0)
			{
				var webBrowser = new WebBrowser();
				webBrowser.Dock = DockStyle.Fill;
				_aboutPage.Controls.Add(webBrowser);
				var path =FileLocator.GetFileDistributedWithApplication("about.htm");
				webBrowser.Navigate(path);
			}
		}

		private void MainWindow_Load(object sender, System.EventArgs e)
		{
			//BringToFront();
			Activate();
            _logBox.Show();
		}

		private void _errorWatchTimer_Tick(object sender, System.EventArgs e)
		{
			if(_logBox.ErrorEncountered )
			{
				_errorWatchTimer.Enabled = false;
				_logPage.Text = "Error Log";
				_logPage.ImageIndex = 0;

			}
		}

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Pause && (e.Alt))
            {
                throw new ApplicationException("User-invoked test crash.");
            }
        }


	}
}
