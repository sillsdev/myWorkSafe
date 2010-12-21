using System;
using System.Drawing;
using System.Windows.Forms;

namespace myWorkSafe
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

		public string DriveLabel
		{
			get
			{
				return _driveLetter.Text;
			}
			set
			{
				_driveLetter.Text = value;
			}
		}

		public long DeviceSizeInKiloBytes { get; set; }
		private int _existingFillPercentage;
		public int ExistingFillPercentage
		{
			get { return _existingFillPercentage; }
			set
			{
				_existingFillPercentage = value;
				Invalidate();
			}
		}

		private int _pendingFillPercentage;
		public int UnknownFillPercentage= -1;

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
			double freeFraction = (1.0 - (ExistingFillPercentage/100.0));
			var freeSpaceHeight = freeFraction*(pictureBox1.Image.Height - (connectorHeight + 95));

            if (PendingFillPercentage == UnknownFillPercentage)
			{
                {
                    e.Graphics.FillRectangle(Brushes.White, pictureBox1.Left + 6, pictureBox1.Top + connectorHeight,
                                             stickWidth - 10,
                                             (int) freeSpaceHeight);
                }

                if (ExistingFillPercentage > 90)
                {
                     e.Graphics.FillRectangle(Brushes.Red, pictureBox1.Left + 6, (int) (pictureBox1.Top + connectorHeight + freeSpaceHeight),
                                             stickWidth - 10,
                                             (int) (pictureBox1.Image.Height - (pictureBox1.Top +connectorHeight + freeSpaceHeight+ 90)));              
                }
			}
			else
			{
				e.Graphics.FillRectangle(Brushes.LightBlue, pictureBox1.Left + 6, pictureBox1.Top + connectorHeight, stickWidth - 10,
				                         (int) freeSpaceHeight);

				var newSpaceHeight = (PendingFillPercentage/100.0)*(pictureBox1.Image.Height - (connectorHeight + 95));
				e.Graphics.FillRectangle(Brushes.White, pictureBox1.Left + 6, pictureBox1.Top + connectorHeight, stickWidth - 10,
				                         (int) newSpaceHeight);
			}
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
