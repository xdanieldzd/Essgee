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

		/* Input/output mode */
		int operatingModeGroupA;    /* Port A & upper port C */
		int operatingModeGroupB;    /* Port B & lower port C */
		bool isPortAInput, isPortCUInput, isPortBInput, isPortCLInput;

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
				case 0x00:
					/* Port A */
					if (!isPortAInput)
					{
						switch (operatingModeGroupA)
						{
							case 0:
								/* Simple I/O */
								PortAOutput = value;
								break;

							default:
								throw new Exception($"i8255: Unimplemented operating mode {operatingModeGroupA} writing Port A");
						}
					}
					break;

				case 0x01:
					/* Port B */
					if (!isPortBInput)
					{
						switch (operatingModeGroupB)
						{
							case 0:
								/* Simple I/O */
								PortBOutput = value;
								break;

							default:
								throw new Exception($"i8255: Unimplemented operating mode {operatingModeGroupB} writing Port B");
						}
					}
					break;

				case 0x02:
					/* Port C */
					if (!isPortCUInput)
					{
						switch (operatingModeGroupA)
						{
							case 0:
								/* Simple I/O */
								PortCOutput &= 0x0F;
								PortCOutput |= (byte)(value & 0xF0);
								break;

							default:
								throw new Exception($"i8255: Unimplemented operating mode {operatingModeGroupA} writing Port C");
						}
					}
					if (!isPortCLInput)
					{
						switch (operatingModeGroupB)
						{
							case 0:
								/* Simple I/O */
								PortCOutput &= 0xF0;
								PortCOutput |= (byte)(value & 0x0F);
								break;

							default:
								throw new Exception($"i8255: Unimplemented operating mode {operatingModeGroupB} writing Port C");
						}
					}
					break;

				case 0x03:
					/* Control port */
					if ((value & 0x80) == 0x80)
					{
						/* Input/output mode */
						operatingModeGroupA = ((value >> 5) & 0x03);
						isPortAInput = ((value & 0x10) == 0x10);
						isPortCUInput = ((value & 0x08) == 0x08);
						operatingModeGroupB = ((value >> 2) & 0x01);
						isPortBInput = ((value & 0x02) == 0x02);
						isPortCLInput = ((value & 0x01) == 0x01);
					}
					else
					{
						/* Bit set/reset mode */
						byte mask = (byte)(1 << ((value >> 1) & 0x07));
						if ((value & 0x01) == 0x01)
							PortCOutput |= mask;
						else
							PortCOutput &= (byte)~mask;
					}
					break;

				default:
					throw new Exception($"i8255: Unsupported write to port 0x{port:X2}, value 0x{value:X2}");
			}

			//System.IO.File.AppendAllText(@"D:\Temp\Essgee\ppi.txt", $"Port {port:X2}, value {value:X2}\n");
		}

		public byte ReadPort(byte port)
		{
			switch (port & 0x03)
			{
				case 0x00:
					/* Port A */
					if (isPortAInput)
					{
						switch (operatingModeGroupA)
						{
							case 0:
								/* Simple I/O */
								return PortAInput;

							default:
								throw new Exception($"i8255: Unimplemented operating mode {operatingModeGroupA} reading Port A");
						}
					}
					// TODO: verify
					return 0x00;

				case 0x01:
					/* Port B */
					if (isPortBInput)
					{
						switch (operatingModeGroupB)
						{
							case 0:
								/* Simple I/O */
								return PortBInput;

							default:
								throw new Exception($"i8255: Unimplemented operating mode {operatingModeGroupB} reading Port B");
						}
					}
					// TODO: verify
					return 0x00;

				case 0x02:
					/* Port C */
					var value = (byte)0x00; // TODO: verify
					if (isPortCUInput)
					{
						switch (operatingModeGroupA)
						{
							case 0:
								/* Simple I/O */
								value |= (byte)(PortCInput & 0xF0);
								break;

							default:
								throw new Exception($"i8255: Unimplemented operating mode {operatingModeGroupA} reading Port C[U]");
						}
					}
					if (isPortCLInput)
					{
						switch (operatingModeGroupB)
						{
							case 0:
								/* Simple I/O */
								value |= (byte)(PortCInput & 0x0F);
								break;

							default:
								throw new Exception($"i8255: Unimplemented operating mode {operatingModeGroupB} reading Port C[L]");
						}
					}
					return value;

				case 0x03:
					/* Cannot read control port */
					return 0xFF;

				default:
					throw new Exception($"i8255: Unsupported read from port 0x{port:X2}");
			}
		}
	}
}
