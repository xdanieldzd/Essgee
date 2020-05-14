using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Essgee.EventArguments;

namespace Essgee.Emulation.Audio
{
	interface IAudio
	{
		event EventHandler<EnqueueSamplesEventArgs> EnqueueSamples;
		void OnEnqueueSamples(EnqueueSamplesEventArgs e);

		void Startup();
		void Shutdown();
		void Reset();
		void Step(int clockCyclesInStep);

		void SetSampleRate(int rate);
		void SetOutputChannels(int channels);
		void SetClockRate(double clock);
		void SetRefreshRate(double refresh);
	}
}
