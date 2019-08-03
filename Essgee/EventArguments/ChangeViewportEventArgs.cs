using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Essgee.EventArguments
{
	public class ChangeViewportEventArgs : EventArgs
	{
		public (int X, int Y, int Width, int Height) Viewport { get; private set; }

		public ChangeViewportEventArgs((int, int, int, int) viewport)
		{
			Viewport = viewport;
		}
	}
}
