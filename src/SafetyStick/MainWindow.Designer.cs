namespace myWorkSafe
{
	partial class MainWindow
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainWindow));
			this.tabControl1 = new System.Windows.Forms.TabControl();
			this._backupPage = new System.Windows.Forms.TabPage();
			this._aboutPage = new System.Windows.Forms.TabPage();
			this.tabControl1.SuspendLayout();
			this.SuspendLayout();
			// 
			// tabControl1
			// 
			this.tabControl1.Alignment = System.Windows.Forms.TabAlignment.Bottom;
			this.tabControl1.Controls.Add(this._backupPage);
			this.tabControl1.Controls.Add(this._aboutPage);
			this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabControl1.Location = new System.Drawing.Point(0, 0);
			this.tabControl1.Multiline = true;
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.Size = new System.Drawing.Size(575, 414);
			this.tabControl1.TabIndex = 0;
			this.tabControl1.SelectedIndexChanged += new System.EventHandler(this.tabControl1_SelectedIndexChanged);
			// 
			// _backupPage
			// 
			this._backupPage.Location = new System.Drawing.Point(4, 4);
			this._backupPage.Name = "_backupPage";
			this._backupPage.Padding = new System.Windows.Forms.Padding(3);
			this._backupPage.Size = new System.Drawing.Size(567, 388);
			this._backupPage.TabIndex = 0;
			this._backupPage.Text = "Backup";
			this._backupPage.UseVisualStyleBackColor = true;
			// 
			// _aboutPage
			// 
			this._aboutPage.Location = new System.Drawing.Point(4, 4);
			this._aboutPage.Name = "_aboutPage";
			this._aboutPage.Padding = new System.Windows.Forms.Padding(3);
			this._aboutPage.Size = new System.Drawing.Size(484, 352);
			this._aboutPage.TabIndex = 1;
			this._aboutPage.Text = "About";
			this._aboutPage.UseVisualStyleBackColor = true;
			// 
			// MainWindow
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.SystemColors.Window;
			this.ClientSize = new System.Drawing.Size(575, 414);
			this.Controls.Add(this.tabControl1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "MainWindow";
			this.Text = "myWorkSafe";
			this.tabControl1.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.TabPage _backupPage;
		private System.Windows.Forms.TabPage _aboutPage;

	}
}

