using System;

using Essgee.Utilities;

namespace Essgee.Emulation.PSG
{
	public class SegaGGPSG : SegaSMSPSG
	{
		public const int PortStereoControl = 0x06;

		enum OutputChannel : int { Left = 0, Right = 1 }

		[StateRequired]
		readonly bool[] channel0Enable, channel1Enable, channel2Enable, channel3Enable;

		public SegaGGPSG(int sampleRate, int numOutputChannels) : base(sampleRate, numOutputChannels)
		{
			channel0Enable = new bool[2];
			channel1Enable = new bool[2];
			channel2Enable = new bool[2];
			channel3Enable = new bool[2];
		}

		public override void Reset()
		{
			base.Reset();

			WritePort(PortStereoControl, 0xFF);
		}

		protected override void GenerateSample()
		{
			for (int i = 0; i < numOutputChannels; i++)
				sampleBuffer.Add(GetMixedSample((i % 2) == 0 ? OutputChannel.Left : OutputChannel.Right));
		}

		private short GetMixedSample(OutputChannel channel)
		{
			short mixed = 0;
			if (EnableToneChannel1 && channel0Enable[(int)channel]) mixed += (short)(volumeTable[volumeRegisters[0]] * ((toneRegisters[0] < 2 ? true : channelOutput[0]) ? 0.5 : -0.5));
			if (EnableToneChannel2 && channel1Enable[(int)channel]) mixed += (short)(volumeTable[volumeRegisters[1]] * ((toneRegisters[1] < 2 ? true : channelOutput[1]) ? 0.5 : -0.5));
			if (EnableToneChannel3 && channel2Enable[(int)channel]) mixed += (short)(volumeTable[volumeRegisters[2]] * ((toneRegisters[2] < 2 ? true : channelOutput[2]) ? 0.5 : -0.5));
			if (EnableNoiseChannel && channel3Enable[(int)channel]) mixed += (short)(volumeTable[volumeRegisters[3]] * (noiseLfsr & 0x1));
			return mixed;
		}

		public override void WritePort(byte port, byte data)
		{
			if (port == 0x06)
			{
				/* Stereo control */
				channel0Enable[(int)OutputChannel.Left] = ((data & 0x10) != 0);
				channel0Enable[(int)OutputChannel.Right] = ((data & 0x01) != 0);

				channel1Enable[(int)OutputChannel.Left] = ((data & 0x20) != 0);
				channel1Enable[(int)OutputChannel.Right] = ((data & 0x02) != 0);

				channel2Enable[(int)OutputChannel.Left] = ((data & 0x40) != 0);
				channel2Enable[(int)OutputChannel.Right] = ((data & 0x04) != 0);

				channel3Enable[(int)OutputChannel.Left] = ((data & 0x80) != 0);
				channel3Enable[(int)OutputChannel.Right] = ((data & 0x08) != 0);
			}
			else
				base.WritePort(port, data);
		}
	}
}
