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

		// FF10 - NR10
		byte ch1NumSweepShift, ch1SweepTime;
		bool ch1SweepIncDec;

		// FF11 - NR11
		byte ch1WavePatternDuty, ch1SoundLengthData;

		// FF12 - NR12
		byte ch1InitialEnvelopeVol, ch1NumEnvelopeSweep;
		bool ch1EnvelopeIncDec;

		// FF13 - NR13
		byte ch1FrequencyLo;

		// FF14 - NR14
		byte ch1FrequencyHi;
		bool ch1CounterConsecutiveSelection, ch1Initial;

		// FF16 - NR21
		byte ch2WavePatternDuty, ch2SoundLengthData;

		// FF17 - NR22
		byte ch2InitialEnvelopeVol, ch2NumEnvelopeSweep;
		bool ch2EnvelopeIncDec;

		// FF18 - NR23
		byte ch2FrequencyLo;

		// FF19 - NR24
		byte ch2FrequencyHi;
		bool ch2CounterConsecutiveSelection, ch2Initial;

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
		byte so1OutputLevel, so2OutputLevel;
		bool outputVinToSo1, outputVinToSo2;

		// FF25 - NR51
		bool outputCh1ToSo1, outputCh2ToSo1, outputCh3ToSo1, outputCh4ToSo1;
		bool outputCh1ToSo2, outputCh2ToSo2, outputCh3ToSo2, outputCh4ToSo2;

		// FF26 - NR52
		bool ch1OnFlag, ch2OnFlag, ch3OnFlag, ch4OnFlag, allSoundOn;

		protected int frameSequencerCounter, frameSequencer;

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

			ch1OnFlag = ch2OnFlag = ch3OnFlag = ch4OnFlag = false;

			frameSequencerCounter = 8192;
			frameSequencer = 0;

			sampleCycleCount = frameCycleCount = 0;
		}

		public void Step(int clockCyclesInStep)
		{
			sampleCycleCount += clockCyclesInStep;
			frameCycleCount += clockCyclesInStep;

			for (int i = 0; i < clockCyclesInStep; i++)
			{
				// http://emudev.de/gameboy-emulator/bleeding-ears-time-to-add-audio/
				// https://github.com/GhostSonic21/GhostBoy/blob/master/GhostBoy/APU.cpp

				frameSequencerCounter--;
				if (frameSequencerCounter <= 0)
				{
					frameSequencerCounter = 8192;

					switch (frameSequencer)
					{
						case 0:
							// len ctr clock
							break;

						case 1:
							break;

						case 2:
							// len ctr clock
							// sweep clock
							break;

						case 3:
							break;

						case 4:
							// len ctr clock
							break;

						case 5:
							break;

						case 6:
							// len ctr clock
							// sweep clock
							break;

						case 7:
							// vol env clock
							break;
					}

					frameSequencer++;
					if (frameSequencer >= 8)
						frameSequencer = 0;
				}

				//StepChannel1();
				StepChannel2();
				//StepChannel3();
				//StepChannel4();
			}

			if (sampleCycleCount >= cyclesPerSample)
			{
				GenerateSample();

				sampleCycleCount -= cyclesPerSample;
			}

			if (sampleBuffer.Count >= (samplesPerFrame * numOutputChannels))
			{
				/*OnEnqueueSamples(new EnqueueSamplesEventArgs(
					numChannels,
					new short[numChannels][] { new short[0], new short[0], new short[0], new short[0] },
					new bool[numChannels] { false, false, false, false },
					sampleBuffer.ToArray()));
					*/
				FlushSamples();
			}

			if (frameCycleCount >= cyclesPerFrame)
			{
				frameCycleCount -= cyclesPerFrame;
				sampleCycleCount = frameCycleCount;
			}
		}

		//

		private void StepChannel2()
		{
			//
		}

		//

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
			switch (port)
			{
				case 0x10:
					return (byte)(
						0x80 |
						(ch1SweepTime << 4) |
						(ch1SweepIncDec ? (1 << 3) : 0) |
						(ch1NumSweepShift << 0));

				case 0x11:
					return (byte)(
						(ch1WavePatternDuty << 6));

				case 0x12:
					return (byte)(
						(ch1InitialEnvelopeVol << 4) |
						(ch1EnvelopeIncDec ? (1 << 3) : 0) |
						(ch1NumEnvelopeSweep << 0));

				case 0x14:
					return (byte)(
						(ch1CounterConsecutiveSelection ? (1 << 6) : 0));

				case 0x16:
					return (byte)(
						(ch2WavePatternDuty << 6));

				case 0x17:
					return (byte)(
						(ch2InitialEnvelopeVol << 4) |
						(ch2EnvelopeIncDec ? (1 << 3) : 0) |
						(ch2NumEnvelopeSweep << 0));

				case 0x19:
					return (byte)(
						(ch2CounterConsecutiveSelection ? (1 << 6) : 0));

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
						(outputVinToSo2 ? (1 << 7) : 0) |
						(so2OutputLevel << 4) |
						(outputVinToSo1 ? (1 << 3) : 0) |
						(so1OutputLevel << 0));

				case 0x25:
					return (byte)(
						(outputCh4ToSo2 ? (1 << 7) : 0) |
						(outputCh3ToSo2 ? (1 << 6) : 0) |
						(outputCh2ToSo2 ? (1 << 5) : 0) |
						(outputCh1ToSo2 ? (1 << 4) : 0) |
						(outputCh4ToSo1 ? (1 << 3) : 0) |
						(outputCh3ToSo1 ? (1 << 2) : 0) |
						(outputCh2ToSo1 ? (1 << 1) : 0) |
						(outputCh1ToSo1 ? (1 << 0) : 0));

				case 0x26:
					return (byte)(
						0x70 |
						(allSoundOn ? (1 << 7) : 0) |
						(ch4OnFlag ? (1 << 3) : 0) |
						(ch3OnFlag ? (1 << 2) : 0) |
						(ch2OnFlag ? (1 << 1) : 0) |
						(ch1OnFlag ? (1 << 0) : 0));

				default:
					return 0xFF;
			}
		}

		public virtual void WritePort(byte port, byte value)
		{
			switch (port)
			{
				case 0x10:
					ch1SweepTime = (byte)((value >> 4) & 0b111);
					ch1SweepIncDec = ((value >> 3) & 0b1) == 0b1;
					ch1NumSweepShift = (byte)((value >> 0) & 0b111);
					break;

				case 0x11:
					ch1WavePatternDuty = (byte)((value >> 6) & 0b11);
					ch1SoundLengthData = (byte)((value >> 0) & 0b111111);
					break;

				case 0x12:
					ch1InitialEnvelopeVol = (byte)((value >> 4) & 0b1111);
					ch1EnvelopeIncDec = ((value >> 3) & 0b1) == 0b1;
					ch1NumEnvelopeSweep = (byte)((value >> 0) & 0b111);
					break;

				case 0x13:
					ch1FrequencyLo = value;
					break;

				case 0x14:
					ch1Initial = ((value >> 7) & 0b1) == 0b1;
					ch1CounterConsecutiveSelection = ((value >> 6) & 0b1) == 0b1;
					ch1FrequencyHi = (byte)((value >> 0) & 0b111);
					break;

				case 0x16:
					ch2WavePatternDuty = (byte)((value >> 6) & 0b11);
					ch2SoundLengthData = (byte)((value >> 0) & 0b111111);
					break;

				case 0x17:
					ch2InitialEnvelopeVol = (byte)((value >> 4) & 0b1111);
					ch2EnvelopeIncDec = ((value >> 3) & 0b1) == 0b1;
					ch2NumEnvelopeSweep = (byte)((value >> 0) & 0b111);
					break;

				case 0x18:
					ch2FrequencyLo = value;
					break;

				case 0x19:
					ch2Initial = ((value >> 7) & 0b1) == 0b1;
					ch2CounterConsecutiveSelection = ((value >> 6) & 0b1) == 0b1;
					ch2FrequencyHi = (byte)((value >> 0) & 0b111);
					break;

				//

				case 0x24:
					outputVinToSo2 = ((value >> 7) & 0b1) == 0b1;
					so2OutputLevel = (byte)((value >> 4) & 0b111);
					outputVinToSo1 = ((value >> 3) & 0b1) == 0b1;
					so1OutputLevel = (byte)((value >> 0) & 0b111);
					break;

				case 0x25:
					outputCh4ToSo2 = ((value >> 7) & 0b1) == 0b1;
					outputCh3ToSo2 = ((value >> 6) & 0b1) == 0b1;
					outputCh2ToSo2 = ((value >> 5) & 0b1) == 0b1;
					outputCh1ToSo2 = ((value >> 4) & 0b1) == 0b1;
					outputCh4ToSo1 = ((value >> 3) & 0b1) == 0b1;
					outputCh3ToSo1 = ((value >> 2) & 0b1) == 0b1;
					outputCh2ToSo1 = ((value >> 1) & 0b1) == 0b1;
					outputCh1ToSo1 = ((value >> 0) & 0b1) == 0b1;
					break;

				case 0x26:
					allSoundOn = ((value >> 7) & 0b1) == 0b1;
					break;

				default:
					break;
			}
		}
	}
}
