using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Diagnostics;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Essgee.Graphics
{
	[DesignTimeVisible(true), ToolboxItem(true)]
	public class RenderControl : GLControl, IComponent
	{
		public event EventHandler<EventArgs> Render;

		bool isRuntime => (LicenseManager.UsageMode != LicenseUsageMode.Designtime);
		bool isReady => (isRuntime && GraphicsContext.CurrentContext != null);

		DebugProc debugCallback;

		bool wasShown = false;

		public RenderControl() : base(GraphicsMode.Default, 3, 0, GraphicsContextFlags.Default)
		{
			if (!isRuntime) return;

			Application.Idle += ((s, e) => { if (isReady) Invalidate(); });
		}

		protected override bool IsInputKey(Keys keyData)
		{
			switch (keyData)
			{
				case Keys.Right:
				case Keys.Left:
				case Keys.Up:
				case Keys.Down:
				case (Keys.Shift | Keys.Right):
				case (Keys.Shift | Keys.Left):
				case (Keys.Shift | Keys.Up):
				case (Keys.Shift | Keys.Down):
					return true;

				default:
					return base.IsInputKey(keyData);
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			if (!isReady)
			{
				e.Graphics.Clear(BackColor);
				using (Pen pen = new Pen(Color.Red, 3.0f))
				{
					e.Graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
					e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
					e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
					e.Graphics.DrawLine(pen, Point.Empty, new Point(ClientRectangle.Right, ClientRectangle.Bottom));
					e.Graphics.DrawLine(pen, new Point(0, ClientRectangle.Bottom), new Point(ClientRectangle.Right, 0));
				}
				return;
			}

			if (!wasShown)
			{
				OnResize(EventArgs.Empty);
				wasShown = true;
			}

			OnRender(EventArgs.Empty);

			SwapBuffers();
		}

		protected virtual void OnRender(EventArgs e)
		{
			if (!isReady) return;
			Render?.Invoke(this, e);
		}

		protected override void OnLoad(EventArgs e)
		{
			if (!isReady) return;

			if (Program.AppEnvironment.EnableOpenGLDebug)
			{
				debugCallback = new DebugProc(GLDebugCallback);
				GL.DebugMessageCallback(debugCallback, IntPtr.Zero);
			}

			GL.ClearColor(BackColor);
			base.OnLoad(e);
		}

		protected override void OnResize(EventArgs e)
		{
			if (!isReady) return;
			base.OnResize(e);
		}

		private void GLDebugCallback(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam)
		{
			Debug.Print($"{(type == DebugType.DebugTypeError ? "GL ERROR!" : "GL callback")} - source={source}, type={type}, severity={severity}, message={Marshal.PtrToStringAnsi(message)}");
		}
	}
}
