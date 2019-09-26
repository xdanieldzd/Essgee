using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Essgee.Exceptions;
using Essgee.EventArguments;

namespace Essgee.Emulation.PSG
{
	public class SN76489 : IPSG
	{
		/* http://www.smspower.org/Development/SN76489 */
		/* Differences in various system's PSGs: http://forums.nesdev.com/viewtopic.php?p=190216#p190216 */

		const int numChannels = 4, numToneChannels = 3, noiseChannelIndex = 3;

		/* Noise generation constants */
		protected virtual ushort noiseLfsrMask => 0x7FFF;
		protected virtual ushort noiseTappedBits => 0x0003;     /* Bits 0 and 1 */
		protected virtual int noiseBitShift => 14;

		/* Sample generation & event handling */
		protected List<short> sampleBuffer;
		public virtual event EventHandler<EnqueueSamplesEventArgs> EnqueueSamples;
		public virtual void OnEnqueueSamples(EnqueueSamplesEventArgs e) { EnqueueSamples?.Invoke(this, e); }

		/* Audio output variables */
		protected int sampleRate, numOutputChannels;

		/* Channel registers */
		protected ushort[] volumeRegisters;     /* Channels 0-3: 4 bits */
		protected ushort[] toneRegisters;       /* Channels 0-2 (tone): 10 bits; channel 3 (noise): 3 bits */

		/* Channel counters */
		protected ushort[] channelCounters;     /* 10-bit counters */
		protected bool[] channelOutput;

		/* Volume attenuation table */
		protected short[] volumeTable;          /* 2dB change per volume register step */

		/* Latched channel/type */
		byte latchedChannel, latchedType;

		/* Linear-feedback shift register, for noise generation */
		protected ushort noiseLfsr;             /* 15-bit */

		/* Timing variables */
		double clockRate, refreshRate;
		int samplesPerFrame, cyclesPerFrame, cyclesPerSample;
		int sampleCycleCount, frameCycleCount, dividerCount;

		public SN76489(int sampleRate, int numOutputChannels)
		{
			this.sampleRate = sampleRate;
			this.numOutputChannels = numOutputChannels;

			sampleBuffer = new List<short>();

			volumeRegisters = new ushort[numChannels];
			toneRegisters = new ushort[numChannels];

			channelCounters = new ushort[numChannels];
			channelOutput = new bool[numChannels];

			/* https://gitlab.com/higan/higan/blob/master/higan/ms/psg/psg.cpp */
			volumeTable = new short[16];
			for (int i = 0; i < volumeTable.Length; i++)
				volumeTable[i] = (short)(0x2000 * Math.Pow(2.0, i * -2.0 / 6.0) + 0.5);
			volumeTable[15] = 0;

			samplesPerFrame = cyclesPerFrame = cyclesPerSample = -1;
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

			if (samplesPerFrame == -1) throw new EmulationException("SN76489: Timings not configured, invalid samples per frame");
			if (cyclesPerFrame == -1) throw new EmulationException("SN76489: Timings not configured, invalid cycles per frame");
			if (cyclesPerSample == -1) throw new EmulationException("SN76489: Timings not configured, invalid cycles per sample");
		}

		public virtual void Shutdown()
		{
			//
		}

		public virtual void Reset()
		{
			FlushSamples();

			latchedChannel = latchedType = 0x00;
			noiseLfsr = 0x4000;

			for (int i = 0; i < numChannels; i++)
			{
				volumeRegisters[i] = 0x000F;
				toneRegisters[i] = 0x0000;
			}

			sampleCycleCount = frameCycleCount = dividerCount = 0;
		}

		public void SetState(Dictionary<string, dynamic> state)
		{
			volumeRegisters = state[nameof(volumeRegisters)];
			toneRegisters = state[nameof(toneRegisters)];

			channelCounters = state[nameof(channelCounters)];
			channelOutput = state[nameof(channelOutput)];

			latchedChannel = state[nameof(latchedChannel)];
			latchedType = state[nameof(latchedType)];

			noiseLfsr = state[nameof(noiseLfsr)];

			sampleCycleCount = state[nameof(sampleCycleCount)];
			frameCycleCount = state[nameof(frameCycleCount)];
			dividerCount = state[nameof(dividerCount)];
		}

		public Dictionary<string, dynamic> GetState()
		{
			return new Dictionary<string, dynamic>
			{
				[nameof(volumeRegisters)] = volumeRegisters,
				[nameof(toneRegisters)] = toneRegisters,

				[nameof(channelCounters)] = channelCounters,
				[nameof(channelOutput)] = channelOutput,

				[nameof(latchedChannel)] = latchedChannel,
				[nameof(latchedType)] = latchedType,

				[nameof(noiseLfsr)] = noiseLfsr,

				[nameof(sampleCycleCount)] = sampleCycleCount,
				[nameof(frameCycleCount)] = frameCycleCount,
				[nameof(dividerCount)] = dividerCount
			};
		}

		public void Step(int clockCyclesInStep)
		{
			sampleCycleCount += clockCyclesInStep;
			frameCycleCount += clockCyclesInStep;

			for (int i = 0; i < clockCyclesInStep; i++)
			{
				dividerCount++;
				if (dividerCount == 16)
				{
					for (int ch = 0; ch < numToneChannels; ch++)
						StepToneChannel(ch);
					StepNoiseChannel();

					dividerCount = 0;
				}
			}

			if (sampleCycleCount >= cyclesPerSample)
			{
				GenerateSample();

				sampleCycleCount -= cyclesPerSample;
			}

			if (frameCycleCount >= cyclesPerFrame)
			{
				OnEnqueueSamples(new EnqueueSamplesEventArgs(sampleBuffer.ToArray()));
				sampleBuffer.Clear();

				frameCycleCount -= cyclesPerFrame;
				sampleCycleCount = frameCycleCount;
			}
		}

		private void StepToneChannel(int ch)
		{
			/* Check for counter underflow */
			if ((channelCounters[ch] & 0x03FF) > 0)
				channelCounters[ch]--;

			/* Counter underflowed, reload and flip output bit, then generate sample */
			if ((channelCounters[ch] & 0x03FF) == 0)
			{
				channelCounters[ch] = (ushort)(toneRegisters[ch] & 0x03FF);
				channelOutput[ch] = !channelOutput[ch];
			}
		}

		private void StepNoiseChannel()
		{
			int chN = noiseChannelIndex;
			{
				/* Check for counter underflow */
				if ((channelCounters[chN] & 0x03FF) > 0)
					channelCounters[chN]--;

				/* Counter underflowed, reload and flip output bit */
				if ((channelCounters[chN] & 0x03FF) == 0)
				{
					switch (toneRegisters[chN] & 0x3)
					{
						case 0x0: channelCounters[chN] = 0x10; break;
						case 0x1: channelCounters[chN] = 0x20; break;
						case 0x2: channelCounters[chN] = 0x40; break;
						case 0x3: channelCounters[chN] = (ushort)(toneRegisters[2] & 0x03FF); break;
					}
					channelOutput[chN] = !channelOutput[chN];

					if (channelOutput[chN])
					{
						/* Check noise type, then generate sample */
						bool isWhiteNoise = (((toneRegisters[chN] >> 2) & 0x1) == 0x1);

						ushort newLfsrBit = (ushort)((isWhiteNoise ? CheckParity((ushort)(noiseLfsr & noiseTappedBits)) : (noiseLfsr & 0x01)) << noiseBitShift);

						noiseLfsr = (ushort)((newLfsrBit | (noiseLfsr >> 1)) & noiseLfsrMask);
					}
				}
			}
		}

		protected virtual void GenerateSample()
		{
			for (int i = 0; i < numOutputChannels; i++)
				sampleBuffer.Add(GetMixedSample());
		}

		private short GetMixedSample()
		{
			/* Mix samples together */
			/* TODO: verify mixing/multiplication; set to 1.0/0.0 for Populous voice samples, glitched with 1.0/-1.0 */
			short mixed = 0;
			mixed += (short)(volumeTable[volumeRegisters[0]] * ((toneRegisters[0] < 2 ? true : channelOutput[0]) ? 1.0 : 0.0));
			mixed += (short)(volumeTable[volumeRegisters[1]] * ((toneRegisters[1] < 2 ? true : channelOutput[1]) ? 1.0 : 0.0));
			mixed += (short)(volumeTable[volumeRegisters[2]] * ((toneRegisters[2] < 2 ? true : channelOutput[2]) ? 1.0 : 0.0));
			mixed += (short)(volumeTable[volumeRegisters[3]] * (noiseLfsr & 0x1));
			return mixed;
		}

		public void FlushSamples()
		{
			sampleBuffer.Clear();
		}

		private ushort CheckParity(ushort val)
		{
			val ^= (ushort)(val >> 8);
			val ^= (ushort)(val >> 4);
			val ^= (ushort)(val >> 2);
			val ^= (ushort)(val >> 1);
			return (ushort)(val & 0x1);
		}

		public virtual byte ReadPort(byte port)
		{
			throw new EmulationException("SN76489: Cannot read ports");
		}

		public virtual void WritePort(byte port, byte data)
		{
			if (BitUtilities.IsBitSet(data, 7))
			{
				/* LATCH/DATA byte; get channel (0-3) and type (0 is tone/noise, 1 is volume) */
				latchedChannel = (byte)((data >> 5) & 0x03);
				latchedType = (byte)((data >> 4) & 0x01);

				/* Mask off non-data bits */
				data &= 0x0F;

				/* If target is channel 3 noise (3 bits), mask off highest bit */
				if (latchedChannel == 3 && latchedType == 0)
					data &= 0x07;

				/* Write to register */
				if (latchedType == 0)
				{
					/* Data is tone/noise */
					toneRegisters[latchedChannel] = (ushort)((toneRegisters[latchedChannel] & 0x03F0) | data);
				}
				else
				{
					/* Data is volume */
					volumeRegisters[latchedChannel] = data;
				}
			}
			else
			{
				/* DATA byte; mask off non-data bits */
				data &= 0x3F;

				/* Write to register */
				if (latchedType == 0)
				{
					/* Data is tone/noise */
					if (latchedChannel == 3)
					{
						/* Target is channel 3 noise, mask off excess bits and write to low bits of register */
						toneRegisters[latchedChannel] = (ushort)(data & 0x07);
					}
					else
					{
						/* Target is not channel 3 noise, write to high bits of register */
						toneRegisters[latchedChannel] = (ushort)((toneRegisters[latchedChannel] & 0x000F) | (data << 4));
					}
				}
				else
				{
					/* Data is volume; mask off excess bits and write to low bits of register */
					volumeRegisters[latchedChannel] = (ushort)(data & 0x0F);
				}
			}
		}
	}
}
