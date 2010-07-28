namespace myWorkSafe
{
	partial class MediaStatus
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this._deviceSizeLabel = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.SuspendLayout();
			// 
			// pictureBox1
			// 
			this.pictureBox1.Image = global::myWorkSafe.Properties.Resources.usbstick;
			this.pictureBox1.Location = new System.Drawing.Point(10, 4);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(83, 320);
			this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
			this.pictureBox1.TabIndex = 0;
			this.pictureBox1.TabStop = false;
			this.pictureBox1.Visible = false;
			// 
			// _deviceSizeLabel
			// 
			this._deviceSizeLabel.AutoSize = true;
			this._deviceSizeLabel.Location = new System.Drawing.Point(29, 30);
			this._deviceSizeLabel.Name = "_deviceSizeLabel";
			this._deviceSizeLabel.Size = new System.Drawing.Size(38, 13);
			this._deviceSizeLabel.TabIndex = 1;
			this._deviceSizeLabel.Text = "99 MB";
			this._deviceSizeLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
			// 
			// MediaStatus
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.Transparent;
			this.Controls.Add(this._deviceSizeLabel);
			this.Controls.Add(this.pictureBox1);
			this.Name = "MediaStatus";
			this.Size = new System.Drawing.Size(105, 318);
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.Label _deviceSizeLabel;

	}
}
