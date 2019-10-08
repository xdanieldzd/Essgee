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

		public static void RGB222toBGRA8888(int color, ref byte[] buffer, int address)
		{
			byte r = (byte)((color >> 0) & 0x3), g = (byte)((color >> 2) & 0x3), b = (byte)((color >> 4) & 0x3);
			buffer[address + 0] = (byte)((b << 6) | (b << 4) | (b << 2) | b);
			buffer[address + 1] = (byte)((g << 6) | (g << 4) | (g << 2) | g);
			buffer[address + 2] = (byte)((r << 6) | (r << 4) | (r << 2) | r);
			buffer[address + 3] = 0xFF;
		}

		public static void RGB444toBGRA8888(int color, ref byte[] buffer, int address)
		{
			byte r = (byte)((color >> 0) & 0xF), g = (byte)((color >> 4) & 0xF), b = (byte)((color >> 8) & 0xF);
			buffer[address + 0] = (byte)((b << 4) | b);
			buffer[address + 1] = (byte)((g << 4) | g);
			buffer[address + 2] = (byte)((r << 4) | r);
			buffer[address + 3] = 0xFF;
		}
	}
}
