using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Essgee.Emulation.Peripherals
{
	public class Intel8255
	{
		public byte PortAInput { get; set; }
		public byte PortBInput { get; set; }
		public byte PortCInput { get; set; }
		public byte PortAOutput { get; set; }
		public byte PortBOutput { get; set; }
		public byte PortCOutput { get; set; }

		byte configByte, setResetControlByte;

		int operatingModeGroupA => ((configByte >> 5) & 0x03);
		bool isPortAInput => ((configByte & 0x10) == 0x10);
		bool isPortCUInput => ((configByte & 0x08) == 0x08);
		int operatingModeGroupB => ((configByte >> 2) & 0x01);
		bool isPortBInput => ((configByte & 0x02) == 0x02);
		bool isPortCLInput => ((configByte & 0x01) == 0x01);
		int bitToChange => ((setResetControlByte >> 1) & 0x07);
		bool isSetBitOperation => ((setResetControlByte & 0x01) == 0x01);

		public Intel8255() { }

		public void Reset()
		{
			PortAInput = PortAOutput = 0x00;
			PortBInput = PortBOutput = 0x00;
			PortCInput = PortCOutput = 0x00;

			WritePort(0x03, 0x9B);
		}

		public void WritePort(byte port, byte value)
		{
			switch (port & 0x03)
			{
				case 0x00: PortAOutput = value; break;
				case 0x01: PortBOutput = value; break;
				case 0x02: PortCOutput = value; break;

				case 0x03:
					/* Control port */
					if ((value & 0x80) == 0x80)
					{
						configByte = value;
					}
					else
					{
						setResetControlByte = value;

						byte mask = (byte)(1 << bitToChange);
						if (isSetBitOperation) PortCOutput |= mask;
						else PortCOutput &= (byte)~mask;
					}
					break;

				default: throw new Exception(string.Format("i8255: Unsupported write to port 0x{0:X2}, value 0x{1:X2}", port, value));
			}
		}

		public byte ReadPort(byte port)
		{
			switch (port & 0x03)
			{
				case 0x00: return (isPortAInput ? PortAInput : PortAOutput);
				case 0x01: return (isPortBInput ? PortBInput : PortBOutput);
				case 0x02: return (byte)(((isPortCUInput ? PortCInput : PortCOutput) & 0xF0) | (isPortCLInput ? PortCInput : PortCOutput) & 0x0F);
				case 0x03: return 0xFF; /* Cannot read control port */

				default: throw new Exception(string.Format("i8255: Unsupported read from port 0x{0:X2}", port));
			}
		}
	}
}
