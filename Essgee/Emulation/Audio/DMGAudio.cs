using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Essgee.Exceptions;
using Essgee.EventArguments;
using Essgee.Utilities;

using static Essgee.Emulation.Utilities;

namespace Essgee.Emulation.Audio
{
	public class DMGAudio : IAudio
	{
		protected const int numChannels = 4;

		//

		protected List<short> sampleBuffer;
		public virtual event EventHandler<EnqueueSamplesEventArgs> EnqueueSamples;
		public virtual void OnEnqueueSamples(EnqueueSamplesEventArgs e) { EnqueueSamples?.Invoke(this, e); }

		protected int sampleRate, numOutputChannels;

		//

		double clockRate, refreshRate;
		int samplesPerFrame, cyclesPerFrame, cyclesPerSample;
		[StateRequired]
		int sampleCycleCount, frameCycleCount;

		public SoundEnableState SoundEnableStates { get; set; }

		public DMGAudio()
		{
			sampleBuffer = new List<short>();

			//

			samplesPerFrame = cyclesPerFrame = cyclesPerSample = -1;
		}

		public void SetSampleRate(int rate)
		{
			sampleRate = rate;
			ConfigureTimings();
		}

		public void SetOutputChannels(int channels)
		{
			numOutputChannels = channels;
			ConfigureTimings();
		}

		public void SetClockRate(double clock)
		{
			clockRate = clock;
			ConfigureTimings();
		}

		public void SetRefreshRate(double refresh)
		{
			refreshRate = refresh;
			ConfigureTimings();
		}

		private void ConfigureTimings()
		{
			samplesPerFrame = (int)(sampleRate / refreshRate);
			cyclesPerFrame = (int)(clockRate / refreshRate);
			cyclesPerSample = (cyclesPerFrame / samplesPerFrame);

			FlushSamples();
		}

		public virtual void Startup()
		{
			Reset();

			if (samplesPerFrame == -1) throw new EmulationException("GB PSG: Timings not configured, invalid samples per frame");
			if (cyclesPerFrame == -1) throw new EmulationException("GB PSG: Timings not configured, invalid cycles per frame");
			if (cyclesPerSample == -1) throw new EmulationException("GB PSG: Timings not configured, invalid cycles per sample");
		}

		public virtual void Shutdown()
		{
			//
		}

		public virtual void Reset()
		{
			FlushSamples();

			//

			sampleCycleCount = frameCycleCount = 0;
		}

		public void Step(int clockCyclesInStep)
		{
			//
		}

		protected virtual void GenerateSample()
		{
			for (int i = 0; i < numOutputChannels; i++)
			{
				/* Generate samples */
				var ch1 = (short)0;
				var ch2 = (short)0;
				var ch3 = (short)0;
				var ch4 = (short)0;

				/* Mix samples */
				var mixed = 0;
				if (true) mixed += ch1;
				if (true) mixed += ch2;
				if (true) mixed += ch3;
				if (true) mixed += ch4;
				mixed /= numChannels;

				sampleBuffer.Add((short)mixed);
			}
		}

		public void FlushSamples()
		{
			sampleBuffer.Clear();
		}

		public virtual byte ReadPort(byte port)
		{
			return 0;
		}

		public virtual void WritePort(byte port, byte data)
		{
			//
		}
	}
}
