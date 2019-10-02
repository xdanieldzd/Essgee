using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Essgee.EventArguments
{
	public class EnqueueSamplesEventArgs : EventArgs
	{
		public int NumChannels { get; set; }
		public short[][] ChannelSamples { get; set; }
		public bool[] IsChannelMuted { get; set; }
		public short[] MixedSamples { get; set; }

		public EnqueueSamplesEventArgs(int numChannels, short[][] channelSamples, bool[] isMuted, short[] mixedSamples)
		{
			NumChannels = numChannels;
			ChannelSamples = channelSamples;
			IsChannelMuted = isMuted;
			MixedSamples = mixedSamples;
		}
	}
}
