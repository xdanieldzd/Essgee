using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using Essgee.EventArguments;
using Essgee.Utilities;

using static Essgee.Emulation.Utilities;

namespace Essgee.Emulation.VDP
{
	public class GameBoyVDP : IVDP
	{
		//

		public virtual (int X, int Y, int Width, int Height) Viewport => (0, 0, 160, 144);

		public virtual event EventHandler<SizeScreenEventArgs> SizeScreen;
		public virtual void OnSizeScreen(SizeScreenEventArgs e) { SizeScreen?.Invoke(this, e); }

		public virtual event EventHandler<RenderScreenEventArgs> RenderScreen;
		public virtual void OnRenderScreen(RenderScreenEventArgs e) { RenderScreen?.Invoke(this, e); }

		public virtual event EventHandler<EventArgs> EndOfScanline;
		public virtual void OnEndOfScanline(EventArgs e) { EndOfScanline?.Invoke(this, e); }

		//

		protected double clockRate, refreshRate;

		//

		[StateRequired]
		public InterruptState InterruptLine { get; set; }

		[StateRequired]
		protected int currentScanline;

		//

		[StateRequired]
		protected int cycleCount;
		protected byte[] outputFramebuffer;

		protected int clockCyclesPerLine;

		public GraphicsEnableState GraphicsEnableStates { get; set; }

		//

		public GameBoyVDP()
		{
			//

			GraphicsEnableStates = GraphicsEnableState.All;
		}

		public virtual void Startup()
		{
			Reset();

			Debug.Assert(clockRate != 0.0, "Clock rate is zero", "{0} clock rate is not configured", GetType().FullName);
			Debug.Assert(refreshRate != 0.0, "Refresh rate is zero", "{0} refresh rate is not configured", GetType().FullName);
		}

		public virtual void Shutdown()
		{
			//
		}

		public virtual void Reset()
		{
			//

			cycleCount = 0;
		}

		public void SetClockRate(double clock)
		{
			clockRate = clock;

			ReconfigureTimings();
		}

		public void SetRefreshRate(double refresh)
		{
			refreshRate = refresh;

			ReconfigureTimings();
		}

		public virtual void SetRevision(int rev)
		{
			Debug.Assert(rev == 0, "Invalid revision", "{0} revision is invalid; only rev 0 is valid", GetType().FullName);
		}

		protected virtual void ReconfigureTimings()
		{
			//
		}

		public virtual void Step(int clockCyclesInStep)
		{
			//
		}

		//

		public virtual byte ReadPort(byte port)
		{
			return 0;
		}

		public virtual void WritePort(byte port, byte value)
		{
			//
		}
	}
}
