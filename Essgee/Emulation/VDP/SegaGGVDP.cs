using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Essgee.Utilities;

using static Essgee.Emulation.Utilities;

namespace Essgee.Emulation.VDP
{
	/* Sega 315-5378, Game Gear */
	public class SegaGGVDP : SegaSMSVDP
	{
		protected override int numTotalScanlines => NumTotalScanlinesNtsc;

		public override (int X, int Y, int Width, int Height) Viewport => (85, 64, 160, 144);
		// y -- ((ScreenHeight / 2) - (144 / 2)) ????

		[StateRequired]
		ushort cramLatch;

		public SegaGGVDP() : base()
		{
			cram = new byte[0x40];
		}

		public override void Reset()
		{
			base.Reset();

			cramLatch = 0x0000;
		}

		protected override void WriteColorToFramebuffer(int palette, int color, int address)
		{
			int cramAddress = ((palette * 32) + (color * 2));
			WriteColorToFramebuffer((ushort)(cram[cramAddress + 1] << 8 | cram[cramAddress]), address);
		}

		protected override void WriteColorToFramebuffer(ushort colorValue, int address)
		{
			RGB444toBGRA8888(colorValue, ref outputFramebuffer, address);
		}

		protected override void WriteDataPort(byte value)
		{
			isSecondControlWrite = false;

			readBuffer = value;

			switch (codeRegister)
			{
				case 0x00:
				case 0x01:
				case 0x02:
					vram[addressRegister] = value;
					break;
				case 0x03:
					if ((addressRegister & 0x0001) != 0)
					{
						cramLatch = (ushort)((cramLatch & 0x00FF) | (value << 8));
						cram[(addressRegister & 0x003E) | 0x0000] = (byte)((cramLatch >> 0) & 0xFF);
						cram[(addressRegister & 0x003E) | 0x0001] = (byte)((cramLatch >> 8) & 0xFF);
					}
					else
						cramLatch = (ushort)((cramLatch & 0xFF00) | (value << 0));
					break;
			}

			addressRegister++;
		}
	}
}
