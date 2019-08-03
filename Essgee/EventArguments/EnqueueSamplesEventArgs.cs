using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Essgee.EventArguments
{
	public class EnqueueSamplesEventArgs : EventArgs
	{
		public short[] Samples { get; set; }

		public EnqueueSamplesEventArgs(short[] samples)
		{
			Samples = samples;
		}
	}
}
