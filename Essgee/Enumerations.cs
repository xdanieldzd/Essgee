using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Essgee
{
	public enum ScreenSizeMode { Stretch, Scale, Integer }

	public enum ExceptionResult { Continue, StopEmulation, ExitApplication }

	public enum ExtraDataTypes { Raw, Image }

	[Flags]
	public enum ExtraDataOptions
	{
		IncludeDateTime = (1 << 0),
		AllowOverwrite = (1 << 1)
	}
}
