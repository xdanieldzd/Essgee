using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Essgee.EventArguments
{
	public class RenderScreenEventArgs : EventArgs
	{
		public byte[] FrameData { get; private set; }

		public RenderScreenEventArgs(byte[] data)
		{
			FrameData = data;
		}
	}
}
