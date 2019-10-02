using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

using Essgee.EventArguments;

namespace Essgee.Sound
{
	public partial class WaveformControl : UserControl
	{
		short[] currentSamples;

		float verticalCenter, verticalScale, horizontalScale;

		public WaveformControl()
		{
			InitializeComponent();

			SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
		}

		public void EnqueueSamples(short[] samples)
		{
			currentSamples = samples;

			horizontalScale = ((currentSamples.Length / 2.0f) / ClientRectangle.Width);
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			verticalCenter = (ClientRectangle.Height / 2.0f);
			verticalScale = (ClientRectangle.Height / (float)short.MaxValue);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.Clear(BackColor);

			e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
			e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
			e.Graphics.SmoothingMode = SmoothingMode.None;

			if (currentSamples == null) return;

			using (var pen = new Pen(ForeColor))
			{
				if (false)
				{
					for (float x = 0; x < ClientRectangle.Width; x += horizontalScale)
					{
						var currSampleIdx = (int)Math.Round(x, MidpointRounding.AwayFromZero);
						var y = verticalCenter - (currentSamples[currSampleIdx] * verticalScale);
						e.Graphics.DrawRectangle(pen, x, y, 1.0f, 1.0f);
					}
				}
				else if (false)
				{
					for (float x = 0; x < ClientRectangle.Width; x += horizontalScale)
					{
						var currSampleIdx = (int)Math.Round(x, MidpointRounding.AwayFromZero);
						var nextSampleIdx = Math.Min((int)Math.Round(x + 1.0f, MidpointRounding.AwayFromZero), currentSamples.Length);
						var y1 = verticalCenter - (currentSamples[currSampleIdx] * verticalScale);
						var y2 = verticalCenter - (currentSamples[nextSampleIdx] * verticalScale);
						e.Graphics.DrawLine(pen, x, y1, x + Math.Min(1.0f, horizontalScale), y2);
					}
				}
				else if (true)
				{
					for (var i = 0; i < currentSamples.Length; i++)
					{
						var x = (ClientRectangle.Width / (float)currentSamples.Length) + (i / 2.0f);

						var y1 = verticalCenter - (currentSamples[i] * verticalScale);
						var y2 = verticalCenter - (currentSamples[((i < currentSamples.Length - 2) ? i + 2 : i)] * verticalScale);

						e.Graphics.DrawLine(pen, x, y1, x + 1, y2);
					}
				}
			}
		}
	}
}
