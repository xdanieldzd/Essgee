using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Essgee.EventArguments;

namespace Essgee.Emulation.VDP
{
	interface IVDP
	{
		(int X, int Y, int Width, int Height) Viewport { get; }

		event EventHandler<RenderScreenEventArgs> RenderScreen;
		void OnRenderScreen(RenderScreenEventArgs e);

		event EventHandler<SizeScreenEventArgs> SizeScreen;
		void OnSizeScreen(SizeScreenEventArgs e);

		InterruptState InterruptLine { get; }

		bool EnableBackgrounds { get; set; }
		bool EnableSprites { get; set; }
		bool EnableOffScreen { get; set; }

		void Startup();
		void Shutdown();
		void Reset();
		void Step(int clockCyclesInStep);

		void SetClockRate(double clock);
		void SetRefreshRate(double refresh);

		byte ReadPort(byte port);
		void WritePort(byte port, byte value);
	}
}
