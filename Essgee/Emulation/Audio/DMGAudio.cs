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
	public partial class DMGAudio : IAudio
	{
		// https://gbdev.gg8.se/wiki/articles/Gameboy_sound_hardware
		// http://emudev.de/gameboy-emulator/bleeding-ears-time-to-add-audio/
		// https://github.com/GhostSonic21/GhostBoy/blob/master/GhostBoy/APU.cpp

		protected const int numChannels = 4;

		Square channel1, channel2;

		// FF1A - NR30
		bool ch3SoundOn;

		// FF1B - NR31
		byte ch3SoundLengthData;

		// FF1C - NR32
		byte ch3OutputLevel;

		// FF1D - NR33
		byte ch3FrequencyLo;

		// FF1E - NR34
		byte ch3FrequencyHi;
		bool ch3CounterConsecutiveSelection, ch3Initial;

		// FF20 - NR41
		byte ch4SoundLengthData;

		// FF21 - NR42
		byte ch4InitialEnvelopeVol, ch4NumEnvelopeSweep;
		bool ch4EnvelopeIncDec;

		// FF22 - NR43
		byte ch4ShiftClockFreq, ch4FreqDivRatio;
		bool ch4CounterStepSelect;

		// FF23 - NR44
		bool ch4CounterConsecutiveSelection, ch4Initial;

		// FF24 - NR50
		byte[] volumeRightLeft;
		bool[] vinEnableRightLeft;

		// FF25 - NR51
		bool[] channel1Enable, channel2Enable, channel3Enable, channel4Enable;

		// FF26 - NR52
		bool isSoundHwEnabled;

		protected int frameSequencerReload, frameSequencerCounter, frameSequencer;

		protected List<short>[] channelSampleBuffer;
		protected List<short> mixedSampleBuffer;
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
			channelSampleBuffer = new List<short>[numChannels];
			for (int i = 0; i < numChannels; i++) channelSampleBuffer[i] = new List<short>();

			mixedSampleBuffer = new List<short>();

			channel1 = new Square(true);
			channel2 = new Square(false);
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

			volumeRightLeft = new byte[numOutputChannels];
			vinEnableRightLeft = new bool[numOutputChannels];

			channel1Enable = new bool[numOutputChannels];
			channel2Enable = new bool[numOutputChannels];
			channel3Enable = new bool[numOutputChannels];
			channel4Enable = new bool[numOutputChannels];

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

			channel1.Reset();
			channel2.Reset();
			//

			frameSequencerReload = (int)(clockRate / 512);
			frameSequencerCounter = frameSequencerReload;
			frameSequencer = 0;

			sampleCycleCount = frameCycleCount = 0;
		}

		public void Step(int clockCyclesInStep)
		{
			if (!isSoundHwEnabled) return;

			sampleCycleCount += clockCyclesInStep;
			frameCycleCount += clockCyclesInStep;

			for (int i = 0; i < clockCyclesInStep; i++)
			{
				frameSequencerCounter--;
				if (frameSequencerCounter == 0)
				{
					frameSequencerCounter = frameSequencerReload;

					switch (frameSequencer)
					{
						case 0:
							channel1.LengthCounterClock();
							channel2.LengthCounterClock();
							//channel3.LengthCounterClock();
							//channel4.LengthCounterClock();
							break;

						case 1:
							break;

						case 2:
							channel1.SweepClock();
							channel1.LengthCounterClock();
							channel2.LengthCounterClock();
							//channel3.LengthCounterClock();
							//channel4.LengthCounterClock();
							break;

						case 3:
							break;

						case 4:
							channel1.LengthCounterClock();
							channel2.LengthCounterClock();
							//channel3.LengthCounterClock();
							//channel4.LengthCounterClock();
							break;

						case 5:
							break;

						case 6:
							channel1.SweepClock();
							channel1.LengthCounterClock();
							channel2.LengthCounterClock();
							//channel3.LengthCounterClock();
							//channel4.LengthCounterClock();
							break;

						case 7:
							channel1.VolumeEnvelopeClock();
							channel2.VolumeEnvelopeClock();
							//channel4.VolumeEnvelopeClock();
							break;
					}

					frameSequencer++;
					if (frameSequencer >= 8)
						frameSequencer = 0;
				}

				channel1.Step();
				channel2.Step();
				//channel3.Step();
				//channel4.Step();
			}

			if (sampleCycleCount >= cyclesPerSample)
			{
				GenerateSample();

				sampleCycleCount -= cyclesPerSample;
			}

			if (mixedSampleBuffer.Count >= (samplesPerFrame * numOutputChannels))
			{
				OnEnqueueSamples(new EnqueueSamplesEventArgs(
					numChannels,
					channelSampleBuffer.Select(x => x.ToArray()).ToArray(),
					new bool[numChannels] { false, false, false, false },
					mixedSampleBuffer.ToArray()));

				FlushSamples();
			}

			if (frameCycleCount >= cyclesPerFrame)
			{
				frameCycleCount -= cyclesPerFrame;
				sampleCycleCount = frameCycleCount;
			}
		}

		protected virtual void GenerateSample()
		{
			for (int i = 0; i < numOutputChannels; i++)
			{
				/* Generate samples */
				var ch1 = (short)(((channel1Enable[i] ? channel1.OutputVolume : 0) * (volumeRightLeft[i] + 1)) << 8);
				var ch2 = (short)(((channel2Enable[i] ? channel2.OutputVolume : 0) * (volumeRightLeft[i] + 1)) << 8);
				var ch3 = (short)0;
				var ch4 = (short)0;

				channelSampleBuffer[0].Add(ch1);
				channelSampleBuffer[1].Add(ch2);
				channelSampleBuffer[2].Add(ch3);
				channelSampleBuffer[3].Add(ch4);

				/* Mix samples */
				var mixed = 0;
				if (true) mixed += ch1;
				if (true) mixed += ch2;
				if (true) mixed += ch3;
				if (true) mixed += ch4;
				mixed /= numChannels;

				mixedSampleBuffer.Add((short)mixed);
			}
		}

		public void FlushSamples()
		{
			for (int i = 0; i < numChannels; i++)
				channelSampleBuffer[i].Clear();

			mixedSampleBuffer.Clear();
		}

		public virtual byte ReadPort(byte port)
		{
			if (port >= 0x10 && port <= 0x14)
				return channel1.ReadPort((byte)(port - 0x10));
			else if (port >= 0x15 && port <= 0x19)
				return channel2.ReadPort((byte)(port - 0x15));
			else // TODO move other channels to classes
				switch (port)
				{
					case 0x1A:
						return (byte)(
							0x7F |
							(ch3SoundOn ? (1 << 7) : 0));

					case 0x1B:
						return ch3SoundLengthData;

					case 0x1C:
						return (byte)(
							0x9F |
							(ch3OutputLevel << 5));

					case 0x1E:
						return (byte)(
							(ch3CounterConsecutiveSelection ? (1 << 6) : 0));

					case 0x20:
						return (byte)(
							0xC0 |
							(ch4SoundLengthData));

					case 0x21:
						return (byte)(
							(ch4InitialEnvelopeVol << 4) |
							(ch4EnvelopeIncDec ? (1 << 3) : 0) |
							(ch4NumEnvelopeSweep << 0));

					case 0x22:
						return (byte)(
							(ch4ShiftClockFreq << 4) |
							(ch4CounterStepSelect ? (1 << 3) : 0) |
							(ch4FreqDivRatio << 0));

					case 0x23:
						return (byte)(
							0x3F |
							(ch4CounterConsecutiveSelection ? (1 << 6) : 0));

					case 0x24:
						return (byte)(
							(vinEnableRightLeft[1] ? (1 << 7) : 0) |
							(volumeRightLeft[1] << 4) |
							(vinEnableRightLeft[0] ? (1 << 3) : 0) |
							(volumeRightLeft[0] << 0));

					case 0x25:
						return (byte)(
							(channel4Enable[1] ? (1 << 7) : 0) |
							(channel3Enable[1] ? (1 << 6) : 0) |
							(channel2Enable[1] ? (1 << 5) : 0) |
							(channel1Enable[1] ? (1 << 4) : 0) |
							(channel4Enable[0] ? (1 << 3) : 0) |
							(channel3Enable[0] ? (1 << 2) : 0) |
							(channel2Enable[0] ? (1 << 1) : 0) |
							(channel1Enable[0] ? (1 << 0) : 0));

					case 0x26:
						return (byte)(
							0x70 |
							(isSoundHwEnabled ? (1 << 7) : 0) |
							(false ? (1 << 3) : 0) |
							(false ? (1 << 2) : 0) |
							(channel2.IsActive ? (1 << 1) : 0) |
							(channel1.IsActive ? (1 << 0) : 0));

					default:
						return 0xFF;
				}
		}

		public virtual void WritePort(byte port, byte value)
		{
			if (port >= 0x10 && port <= 0x14)
				channel1.WritePort((byte)(port - 0x10), value);
			else if (port >= 0x15 && port <= 0x19)
				channel2.WritePort((byte)(port - 0x15), value);
			else // TODO move other channels to classes
				switch (port)
				{
					//

					case 0x24:
						vinEnableRightLeft[1] = ((value >> 7) & 0b1) == 0b1;
						volumeRightLeft[1] = (byte)((value >> 4) & 0b111);
						vinEnableRightLeft[0] = ((value >> 3) & 0b1) == 0b1;
						volumeRightLeft[0] = (byte)((value >> 0) & 0b111);
						break;

					case 0x25:
						channel4Enable[1] = ((value >> 7) & 0b1) == 0b1;
						channel3Enable[1] = ((value >> 6) & 0b1) == 0b1;
						channel2Enable[1] = ((value >> 5) & 0b1) == 0b1;
						channel1Enable[1] = ((value >> 4) & 0b1) == 0b1;
						channel4Enable[0] = ((value >> 3) & 0b1) == 0b1;
						channel3Enable[0] = ((value >> 2) & 0b1) == 0b1;
						channel2Enable[0] = ((value >> 1) & 0b1) == 0b1;
						channel1Enable[0] = ((value >> 0) & 0b1) == 0b1;
						break;

					case 0x26:
						isSoundHwEnabled = ((value >> 7) & 0b1) == 0b1;
						break;

					default:
						break;
				}
		}
	}
}
