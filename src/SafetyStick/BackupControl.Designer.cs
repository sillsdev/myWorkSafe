namespace myWorkSafe
{
	partial class BackupControl
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
				if(_driveDetector !=null)
				{
					_driveDetector.Dispose();
					_driveDetector = null;
				}
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BackupControl));
			this._safeToRemoveLabel = new System.Windows.Forms.Label();
			this.cancelButton = new System.Windows.Forms.Button();
			this.backupNowButton = new System.Windows.Forms.Button();
			this.closeButton = new System.Windows.Forms.Button();
			this.imageList1 = new System.Windows.Forms.ImageList(this.components);
			this.listView1 = new System.Windows.Forms.ListView();
			this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
			this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
			this.syncProgressBar = new System.Windows.Forms.ProgressBar();
			this._status = new System.Windows.Forms.Label();
			this._probablyRemovedPhysicallyTimer = new System.Windows.Forms.Timer(this.components);
			this._mediaStatusIndicator = new myWorkSafe.MediaStatus();
			this._startTimer = new System.Windows.Forms.Timer(this.components);
			this.label1 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// _safeToRemoveLabel
			// 
			this._safeToRemoveLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this._safeToRemoveLabel.AutoSize = true;
			this._safeToRemoveLabel.Font = new System.Drawing.Font("Segoe UI Semibold", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this._safeToRemoveLabel.ForeColor = System.Drawing.Color.Teal;
			this._safeToRemoveLabel.Location = new System.Drawing.Point(113, 221);
			this._safeToRemoveLabel.MaximumSize = new System.Drawing.Size(356, 0);
			this._safeToRemoveLabel.Name = "_safeToRemoveLabel";
			this._safeToRemoveLabel.Size = new System.Drawing.Size(330, 20);
			this._safeToRemoveLabel.TabIndex = 17;
			this._safeToRemoveLabel.Text = "It is now safe to remove the USB Memory Stick.";
			this._safeToRemoveLabel.Visible = false;
			// 
			// cancelButton
			// 
			this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.cancelButton.Location = new System.Drawing.Point(266, 310);
			this.cancelButton.Name = "cancelButton";
			this.cancelButton.Size = new System.Drawing.Size(75, 23);
			this.cancelButton.TabIndex = 16;
			this.cancelButton.Text = "&Cancel";
			this.cancelButton.UseVisualStyleBackColor = true;
			this.cancelButton.Click += new System.EventHandler(this.OnCancelClick);
			// 
			// backupNowButton
			// 
			this.backupNowButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.backupNowButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.backupNowButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.backupNowButton.Location = new System.Drawing.Point(364, 244);
			this.backupNowButton.Name = "backupNowButton";
			this.backupNowButton.Size = new System.Drawing.Size(145, 89);
			this.backupNowButton.TabIndex = 9;
			this.backupNowButton.Text = "Backup Now\r\n";
			this.backupNowButton.UseVisualStyleBackColor = true;
			this.backupNowButton.Click += new System.EventHandler(this.backupNowButton_Click);
			// 
			// closeButton
			// 
			this.closeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.closeButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.closeButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.closeButton.Location = new System.Drawing.Point(376, 277);
			this.closeButton.Name = "closeButton";
			this.closeButton.Size = new System.Drawing.Size(134, 56);
			this.closeButton.TabIndex = 13;
			this.closeButton.Text = "&Close";
			this.closeButton.UseVisualStyleBackColor = true;
			// 
			// imageList1
			// 
			this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
			this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
			this.imageList1.Images.SetKeyName(0, "");
			this.imageList1.Images.SetKeyName(1, "DeleteMessageBoxImage.png");
			// 
			// listView1
			// 
			this.listView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.listView1.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
			this.listView1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.listView1.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
			this.listView1.Location = new System.Drawing.Point(117, 51);
			this.listView1.Name = "listView1";
			this.listView1.Size = new System.Drawing.Size(393, 167);
			this.listView1.SmallImageList = this.imageList1;
			this.listView1.TabIndex = 12;
			this.listView1.UseCompatibleStateImageBehavior = false;
			this.listView1.View = System.Windows.Forms.View.Details;
			// 
			// columnHeader1
			// 
			this.columnHeader1.Text = "Name";
			this.columnHeader1.Width = 200;
			// 
			// columnHeader2
			// 
			this.columnHeader2.Text = "Size";
			this.columnHeader2.Width = 189;
			// 
			// syncProgressBar
			// 
			this.syncProgressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.syncProgressBar.Location = new System.Drawing.Point(121, 35);
			this.syncProgressBar.Name = "syncProgressBar";
			this.syncProgressBar.Size = new System.Drawing.Size(388, 10);
			this.syncProgressBar.TabIndex = 10;
			this.syncProgressBar.Visible = false;
			// 
			// _status
			// 
			this._status.AutoSize = true;
			this._status.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this._status.Location = new System.Drawing.Point(118, 10);
			this._status.Name = "_status";
			this._status.Size = new System.Drawing.Size(114, 17);
			this._status.TabIndex = 15;
			this._status.Text = "this will be status";
			// 
			// _probablyRemovedPhysicallyTimer
			// 
			this._probablyRemovedPhysicallyTimer.Interval = 1000;
			this._probablyRemovedPhysicallyTimer.Tick += new System.EventHandler(this._probablyRemovedPhysicallyTimer_Tick);
			// 
			// _mediaStatusIndicator
			// 
			this._mediaStatusIndicator.BackColor = System.Drawing.Color.Transparent;
			this._mediaStatusIndicator.DeviceSizeInKiloBytes = ((long)(131072));
			this._mediaStatusIndicator.DriveLabel = "Q:\\\\";
			this._mediaStatusIndicator.ExistingFillPercentage = 50;
			this._mediaStatusIndicator.Location = new System.Drawing.Point(3, 10);
			this._mediaStatusIndicator.Name = "_mediaStatusIndicator";
			this._mediaStatusIndicator.PendingFillPercentage = 25;
			this._mediaStatusIndicator.Size = new System.Drawing.Size(100, 323);
			this._mediaStatusIndicator.TabIndex = 11;
			// 
			// _startGatheringInfo
			// 
			this._startTimer.Interval = 500;
			this._startTimer.Tick += new System.EventHandler(this.OnStartTick);
			// 
			// label1
			// 
			this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.label1.AutoSize = true;
			this.label1.ForeColor = System.Drawing.Color.DarkRed;
			this.label1.Location = new System.Drawing.Point(361, 12);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(152, 13);
			this.label1.TabIndex = 18;
			this.label1.Text = "Do not rely on this test version.";
			// 
			// BackupControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.label1);
			this.Controls.Add(this._safeToRemoveLabel);
			this.Controls.Add(this.cancelButton);
			this.Controls.Add(this._mediaStatusIndicator);
			this.Controls.Add(this.backupNowButton);
			this.Controls.Add(this.closeButton);
			this.Controls.Add(this.listView1);
			this.Controls.Add(this.syncProgressBar);
			this.Controls.Add(this._status);
			this.Name = "BackupControl";
			this.Size = new System.Drawing.Size(535, 348);
			this.Load += new System.EventHandler(this.BackupControl_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label _safeToRemoveLabel;
		private System.Windows.Forms.Button cancelButton;
		private MediaStatus _mediaStatusIndicator;
		private System.Windows.Forms.Button backupNowButton;
		private System.Windows.Forms.Button closeButton;
		private System.Windows.Forms.ImageList imageList1;
		private System.Windows.Forms.ListView listView1;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private System.Windows.Forms.ProgressBar syncProgressBar;
		private System.Windows.Forms.Label _status;
		private System.Windows.Forms.Timer _probablyRemovedPhysicallyTimer;
		private System.Windows.Forms.Timer _startTimer;
		private System.Windows.Forms.Label label1;
	}
}
