using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;

namespace SafeStick
{
	public partial class Form1 : Form
	{
		Synchronizer _synchronizer;
		private BackgroundWorker _preparationWorker;
		private BackgroundWorker _backupWorker;

		public Form1(string destinationPath)
		{
			InitializeComponent();
			listView1.Visible = false;
			backupNowButton.Visible = false;
			_synchronizer = new Synchronizer(destinationPath);
			var info =new DriveInfo(Path.GetPathRoot(destinationPath));
			mediaStatus1.FillPercentage = (int) (100.0*info.AvailableFreeSpace/info.TotalSize);

			closeButton.Visible = false;
			cancelButton.Visible = true;
                      
                       

			statusLabel.Text = "Looking at what files have changed...";
			_preparationWorker = new BackgroundWorker();
			_preparationWorker.DoWork+=new DoWorkEventHandler(_preparationWorker_DoWork);
			_preparationWorker.WorkerSupportsCancellation = true;
			_preparationWorker.RunWorkerAsync();
			_preparationWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OnPreparationCompleted);
		}

		void OnPreparationCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			cancelButton.Visible = false;
			Cursor = Cursors.Default;
			listView1.Visible = true;
			backupNowButton.Visible = true;
			syncProgressBar.Maximum = _synchronizer.TotalFilesThatWillBeCopied;
			syncProgressBar.Minimum = 0;

			statusLabel.Text = string.Format("{0} files will be backed up:", _synchronizer.TotalFilesThatWillBeCopied);
		}

		void _preparationWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			_synchronizer.GatherInformation();
		
		}

		/// <summary>
		/// returns true if we want to cancel
		/// </summary>
		/// <returns></returns>
		public bool OnProgress()
		{
			InvokeIfRequired(()=>syncProgressBar.Value = _synchronizer.FilesCopiedThusFar);
			return _backupWorker.CancellationPending;
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
			statusLabel.Text = "Copying files to USB Stick...";
			backupNowButton.Visible = false;
			syncProgressBar.Visible = true;
			cancelButton.Visible = true;
			_synchronizer.Progress += OnProgress;
			_backupWorker = new BackgroundWorker();
			_backupWorker.DoWork += new DoWorkEventHandler(_backupWorker_DoWork);
			_backupWorker.WorkerSupportsCancellation = true;
			_backupWorker.RunWorkerAsync();
			_backupWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OnBackupWorkerCompleted);

		}

		void OnBackupWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			statusLabel.Text = "Finished";
			syncProgressBar.Visible = false;
			cancelButton.Visible = false;
			closeButton.Visible = true;
			AcceptButton = closeButton;
			CancelButton = closeButton;
			cancelButton.Visible = false;
			closeButton.Focus();
			Cursor = Cursors.Default;
		}

		void _backupWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			_synchronizer.DoSynchronization();		
		}

		private void OnCancelClick(object sender, EventArgs e)
		{
			if (_preparationWorker != null)
			{
				statusLabel.Text = "Cancelling...";
				_preparationWorker.RunWorkerCompleted +=new RunWorkerCompletedEventHandler((x,y)=>Close());
				_preparationWorker.CancelAsync();
			}
			if(_backupWorker !=null)
			{
				statusLabel.Text = "Cancelling...";
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
