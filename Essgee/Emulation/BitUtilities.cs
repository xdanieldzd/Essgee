using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Essgee.Emulation
{
	public static class BitUtilities
	{
		public static bool IsBitSet(byte value, int bit)
		{
			return ((value & (1 << bit)) != 0);
		}
	}
}
