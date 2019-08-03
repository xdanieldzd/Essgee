using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Essgee.Graphics
{
	public enum FilterMode
	{
		Linear,
		Nearest
	}

	public enum WrapMode
	{
		Repeat,
		Edge,
		Border,
		Mirror
	}

	public enum PixelFormat
	{
		Rgba8888,
		Rgb888
	}
}
