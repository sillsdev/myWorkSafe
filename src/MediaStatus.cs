using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SafetyStick
{
	public partial class MediaStatus : UserControl
	{
		public MediaStatus()
		{
			InitializeComponent();
			//DrawMode =  DrawMode.OwnerDrawFixed;
			ExistingFillPercentage = 50;
			PendingFillPercentage = 25;
			DeviceSizeInKiloBytes = 128*1024;
		}

		public long DeviceSizeInKiloBytes { get; set; }

		public int ExistingFillPercentage { get; set; }

		private int _pendingFillPercentage;
		public int PendingFillPercentage
		{
			get { return _pendingFillPercentage; }
			set 
			{
				_pendingFillPercentage = value; 
				Invalidate();
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			var stickWidth = 81;
			var connectorHeight = 70;

			_deviceSizeLabel.Text = GetStringForStorageSize(DeviceSizeInKiloBytes);
			e.Graphics.DrawImage(pictureBox1.Image, pictureBox1.Bounds);
			var freeSpaceHeight =(ExistingFillPercentage/100.0)* (pictureBox1.Image.Height-(connectorHeight+95));
			e.Graphics.FillRectangle(Brushes.LightBlue, pictureBox1.Left+6, pictureBox1.Top + connectorHeight, stickWidth-10, (int) freeSpaceHeight);

			var newSpaceHeight = (PendingFillPercentage / 100.0) * (pictureBox1.Image.Height - (connectorHeight + 95));
			e.Graphics.FillRectangle(Brushes.White, pictureBox1.Left + 6, pictureBox1.Top + connectorHeight, stickWidth - 10, (int)newSpaceHeight);

		}

		public static string GetStringForStorageSize(long kiloBytes)
		{
			var megs = Math.Ceiling(kiloBytes / 1024.0);
			var gigs = megs / 1024.0;
			if (gigs < 1)
			{
				return megs.ToString("##.#") + "Meg";
			}
			return gigs.ToString("##.#") + "Gig";

		}
	}
}
