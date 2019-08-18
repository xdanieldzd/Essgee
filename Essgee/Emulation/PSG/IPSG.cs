﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Essgee.EventArguments;

namespace Essgee.Emulation.PSG
{
	interface IPSG
	{
		event EventHandler<EnqueueSamplesEventArgs> EnqueueSamples;
		void OnEnqueueSamples(EnqueueSamplesEventArgs e);

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