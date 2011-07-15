using System.Windows.Forms;

namespace myWorkSafe
{
    partial class DriveCommandsAndSettingsControl
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
            this._driveLabel = new System.Windows.Forms.Label();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this._label = new System.Windows.Forms.Label();
            this._backupNowButton = new System.Windows.Forms.Button();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::myWorkSafe.Properties.Resources.usbstick;
            this.pictureBox1.Location = new System.Drawing.Point(3, 3);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(71, 93);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // _driveLabel
            // 
            this._driveLabel.AutoSize = true;
            this._driveLabel.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this._driveLabel.Location = new System.Drawing.Point(3, 0);
            this._driveLabel.Name = "_driveLabel";
            this._driveLabel.Padding = new System.Windows.Forms.Padding(0, 0, 0, 15);
            this._driveLabel.Size = new System.Drawing.Size(57, 36);
            this._driveLabel.TabIndex = 1;
            this._driveLabel.Text = "label1";
            // 
            // comboBox1
            // 
            this.comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox1.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Items.AddRange(new object[] {
            "Always Backup",
            "Never Backup",
            "Ask Each Time",
            "Unknown"});
            this.comboBox1.Location = new System.Drawing.Point(3, 59);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(192, 25);
            this.comboBox1.TabIndex = 2;
            // 
            // _label
            // 
            this._label.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._label.Font = new System.Drawing.Font("Segoe UI", 10F);
            this._label.Location = new System.Drawing.Point(3, 36);
            this._label.Name = "_label";
            this._label.Size = new System.Drawing.Size(316, 20);
            this._label.TabIndex = 4;
            this._label.Text = "What should happen when this drive is inserted?";
            // 
            // _backupNowButton
            // 
            this._backupNowButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._backupNowButton.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this._backupNowButton.Location = new System.Drawing.Point(161, 98);
            this._backupNowButton.Name = "_backupNowButton";
            this._backupNowButton.Size = new System.Drawing.Size(158, 37);
            this._backupNowButton.TabIndex = 5;
            this._backupNowButton.Text = "Backup Now";
            this._backupNowButton.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this._label, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.comboBox1, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this._driveLabel, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this._backupNowButton, 0, 3);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(80, 3);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 4;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(322, 138);
            this.tableLayoutPanel1.TabIndex = 6;
            // 
            // DriveCommandsAndSettingsControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.pictureBox1);
            this.Name = "DriveCommandsAndSettingsControl";
            this.Size = new System.Drawing.Size(405, 144);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label _driveLabel;
        private System.Windows.Forms.ComboBox comboBox1;
        private Label _label;
        private System.Windows.Forms.Button _backupNowButton;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
    }
}
