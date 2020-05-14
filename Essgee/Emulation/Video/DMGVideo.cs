using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using Essgee.Exceptions;
using Essgee.EventArguments;
using Essgee.Utilities;

using static Essgee.Emulation.Utilities;
using static Essgee.Emulation.Machines.GameBoy;

namespace Essgee.Emulation.Video
{
	public class DMGVideo : IVideo
	{
		readonly RequestInterruptDelegate requestInterruptDelegate;

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
		protected byte[] vram, oam;

		[StateRequired]
		protected int currentScanline;

		[StateRequired]
		protected bool readyVblank;
		//

		[StateRequired]
		protected int cycleCount;
		protected byte[] outputFramebuffer;

		protected int clockCyclesPerLine;

		//

		public DMGVideo(RequestInterruptDelegate requestInterrupt)
		{
			vram = new byte[0x2000];
			oam = new byte[0xA0];

			//

			requestInterruptDelegate = requestInterrupt;
		}

		public virtual void Startup()
		{
			Reset();

			if (requestInterruptDelegate == null) throw new EmulationException("DMGVideo: Request interrupt delegate is null");

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
			/* Calculate cycles/line */
			clockCyclesPerLine = (int)Math.Round((clockRate / refreshRate) / 153);

			/* Create arrays */
			outputFramebuffer = new byte[(160 * 144) * 4];

			//temp
			for (int i = 0; i < outputFramebuffer.Length; i += 4)
			{
				outputFramebuffer[i + 0] = 0xff;
				outputFramebuffer[i + 3] = 0xff;
			}
		}

		public virtual void Step(int clockCyclesInStep)
		{
			cycleCount += clockCyclesInStep;

			if (cycleCount >= clockCyclesPerLine)
			{
				OnEndOfScanline(EventArgs.Empty);

				//render

				currentScanline++;

				if (currentScanline == 144)
					readyVblank = true;

				if (readyVblank)
					requestInterruptDelegate(InterruptSource.VBlank);

				if (currentScanline == 153)
				{
					//temp
					for (int i = 0, j = 0; i < vram.Length; i++, j += 4) outputFramebuffer[j] = vram[i];




					readyVblank = false;

					currentScanline = 0;
					OnRenderScreen(new RenderScreenEventArgs(160, 144, outputFramebuffer.Clone() as byte[]));
				}

				cycleCount -= clockCyclesPerLine;
				if (cycleCount <= -clockCyclesPerLine) cycleCount = 0;
			}
		}

		//

		public virtual byte ReadVram(ushort address)
		{
			return vram[address & (vram.Length - 1)];
		}

		public virtual void WriteVram(ushort address, byte value)
		{
			vram[address & (vram.Length - 1)] = value;
		}

		public virtual byte ReadOam(ushort address)
		{
			return oam[address - 0xFE00];
		}

		public virtual void WriteOam(ushort address, byte value)
		{
			oam[address - 0xFE00] = value;
		}

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
