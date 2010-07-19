namespace SafeStick
{
	partial class Form1
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
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Windows.Forms.ListViewItem listViewItem1 = new System.Windows.Forms.ListViewItem(new string[] {
            "Paratext",
            "Will be backed up"}, -1);
			System.Windows.Forms.ListViewItem listViewItem2 = new System.Windows.Forms.ListViewItem("AdaptIt");
			System.Windows.Forms.ListViewItem listViewItem3 = new System.Windows.Forms.ListViewItem(new string[] {
            "WeSay (Not found)"}, -1, System.Drawing.SystemColors.GrayText, System.Drawing.Color.Empty, null);
			System.Windows.Forms.ListViewItem listViewItem4 = new System.Windows.Forms.ListViewItem("Other Documents");
			System.Windows.Forms.ListViewItem listViewItem5 = new System.Windows.Forms.ListViewItem(new string[] {
            "Music (Not Enough Space)"}, -1, System.Drawing.SystemColors.GrayText, System.Drawing.Color.Empty, null);
			System.Windows.Forms.ListViewItem listViewItem6 = new System.Windows.Forms.ListViewItem(new string[] {
            "Video (Not Enough Space)"}, -1, System.Drawing.SystemColors.GrayText, System.Drawing.SystemColors.Window, null);
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
			this.backupNowButton = new System.Windows.Forms.Button();
			this.syncProgressBar = new System.Windows.Forms.ProgressBar();
			this.listView1 = new System.Windows.Forms.ListView();
			this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
			this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
			this.imageList1 = new System.Windows.Forms.ImageList(this.components);
			this.closeButton = new System.Windows.Forms.Button();
			this.statusLabel = new System.Windows.Forms.Label();
			this.cancelButton = new System.Windows.Forms.Button();
			this.mediaStatus1 = new SafeStick.MediaStatus();
			this.SuspendLayout();
			// 
			// backupNowButton
			// 
			this.backupNowButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.backupNowButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.backupNowButton.Location = new System.Drawing.Point(129, 251);
			this.backupNowButton.Name = "backupNowButton";
			this.backupNowButton.Size = new System.Drawing.Size(340, 89);
			this.backupNowButton.TabIndex = 0;
			this.backupNowButton.Text = "Backup Now\r\n(Press Enter)";
			this.backupNowButton.UseVisualStyleBackColor = true;
			this.backupNowButton.Click += new System.EventHandler(this.backupNowButton_Click);
			// 
			// syncProgressBar
			// 
			this.syncProgressBar.Location = new System.Drawing.Point(129, 251);
			this.syncProgressBar.Name = "syncProgressBar";
			this.syncProgressBar.Size = new System.Drawing.Size(340, 31);
			this.syncProgressBar.TabIndex = 2;
			this.syncProgressBar.Visible = false;
			// 
			// listView1
			// 
			this.listView1.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
			this.listView1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.listView1.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
			this.listView1.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
            listViewItem1,
            listViewItem2,
            listViewItem3,
            listViewItem4,
            listViewItem5,
            listViewItem6});
			this.listView1.Location = new System.Drawing.Point(129, 45);
			this.listView1.Name = "listView1";
			this.listView1.Size = new System.Drawing.Size(340, 152);
			this.listView1.SmallImageList = this.imageList1;
			this.listView1.TabIndex = 4;
			this.listView1.UseCompatibleStateImageBehavior = false;
			this.listView1.View = System.Windows.Forms.View.Details;
			// 
			// columnHeader1
			// 
			this.columnHeader1.Text = "Name";
			this.columnHeader1.Width = 150;
			// 
			// columnHeader2
			// 
			this.columnHeader2.Text = "Size";
			this.columnHeader2.Width = 189;
			// 
			// imageList1
			// 
			this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
			this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
			this.imageList1.Images.SetKeyName(0, "");
			// 
			// closeButton
			// 
			this.closeButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.closeButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.closeButton.Location = new System.Drawing.Point(335, 284);
			this.closeButton.Name = "closeButton";
			this.closeButton.Size = new System.Drawing.Size(134, 56);
			this.closeButton.TabIndex = 5;
			this.closeButton.Text = "&Close";
			this.closeButton.UseVisualStyleBackColor = true;
			this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
			// 
			// statusLabel
			// 
			this.statusLabel.AutoSize = true;
			this.statusLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.statusLabel.Location = new System.Drawing.Point(129, 22);
			this.statusLabel.Name = "statusLabel";
			this.statusLabel.Size = new System.Drawing.Size(171, 17);
			this.statusLabel.TabIndex = 6;
			this.statusLabel.Text = "{0} files will be backed up:";
			// 
			// cancelButton
			// 
			this.cancelButton.Location = new System.Drawing.Point(394, 317);
			this.cancelButton.Name = "cancelButton";
			this.cancelButton.Size = new System.Drawing.Size(75, 23);
			this.cancelButton.TabIndex = 7;
			this.cancelButton.Text = "&Cancel";
			this.cancelButton.UseVisualStyleBackColor = true;
			this.cancelButton.Click += new System.EventHandler(this.OnCancelClick);
			// 
			// mediaStatus1
			// 
			this.mediaStatus1.BackColor = System.Drawing.Color.Transparent;
			this.mediaStatus1.FillPercentage = 50;
			this.mediaStatus1.Location = new System.Drawing.Point(10, 17);
			this.mediaStatus1.Name = "mediaStatus1";
			this.mediaStatus1.Size = new System.Drawing.Size(100, 323);
			this.mediaStatus1.TabIndex = 3;
			// 
			// Form1
			// 
			this.AcceptButton = this.backupNowButton;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.SystemColors.Window;
			this.CancelButton = this.closeButton;
			this.ClientSize = new System.Drawing.Size(492, 354);
			this.Controls.Add(this.cancelButton);
			this.Controls.Add(this.statusLabel);
			this.Controls.Add(this.listView1);
			this.Controls.Add(this.mediaStatus1);
			this.Controls.Add(this.syncProgressBar);
			this.Controls.Add(this.backupNowButton);
			this.Controls.Add(this.closeButton);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "Form1";
			this.Text = "SafeStick";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button backupNowButton;
		private System.Windows.Forms.ProgressBar syncProgressBar;
		private MediaStatus mediaStatus1;
		private System.Windows.Forms.ListView listView1;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private System.Windows.Forms.ImageList imageList1;
		private System.Windows.Forms.Button closeButton;
		private System.Windows.Forms.Label statusLabel;
		private System.Windows.Forms.Button cancelButton;
	}
}

