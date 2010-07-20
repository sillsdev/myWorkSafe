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
			FillPercentage = 50;
		}

		public int FillPercentage { get; set; }

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			var stickWidth = 81;
			var connectorHeight = 70;

			e.Graphics.DrawImage(pictureBox1.Image, pictureBox1.Bounds);
			var height =(FillPercentage/100.0)* (pictureBox1.Image.Height-(connectorHeight+95));
			e.Graphics.FillRectangle(Brushes.White, pictureBox1.Left+6, pictureBox1.Top + connectorHeight, stickWidth-10, (int) height);

		}
	}
}
