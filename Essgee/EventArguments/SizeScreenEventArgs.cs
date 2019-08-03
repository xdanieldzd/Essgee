using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Essgee.EventArguments
{
	public class SizeScreenEventArgs : EventArgs
	{
		public int Width { get; private set; }
		public int Height { get; private set; }

		public SizeScreenEventArgs(int width, int height)
		{
			Width = width;
			Height = height;
		}
	}
}
