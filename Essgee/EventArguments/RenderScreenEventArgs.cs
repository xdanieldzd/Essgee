using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Essgee.EventArguments
{
	public class RenderScreenEventArgs : EventArgs
	{
		public int Width { get; private set; }
		public int Height { get; private set; }
		public byte[] FrameData { get; private set; }

		public RenderScreenEventArgs(int width, int height, byte[] data)
		{
			Width = width;
			Height = height;
			FrameData = data;
		}
	}
}
