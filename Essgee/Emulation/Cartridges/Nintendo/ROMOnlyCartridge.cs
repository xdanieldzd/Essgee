using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Essgee.Exceptions;
using Essgee.Utilities;

namespace Essgee.Emulation.Cartridges.Nintendo
{
	public class ROMOnlyCartridge : ICartridge
	{
		byte[] romData;

		public ROMOnlyCartridge(int romSize, int ramSize)
		{
			romData = new byte[romSize];
		}

		public void LoadRom(byte[] data)
		{
			Buffer.BlockCopy(data, 0, romData, 0, Math.Min(data.Length, romData.Length));
		}

		public void LoadRam(byte[] data)
		{
			/* Not supported */
		}

		public byte[] GetRomData()
		{
			return romData;
		}

		public byte[] GetRamData()
		{
			return new byte[0];
		}

		public bool IsRamSaveNeeded()
		{
			return false;
		}

		public ushort GetLowerBound()
		{
			return 0x0000;
		}

		public ushort GetUpperBound()
		{
			return 0x7FFF;
		}

		public void Step(int clockCyclesInStep)
		{
			/* Nothing to do */
		}

		public byte Read(ushort address)
		{
			return romData[address & (romData.Length - 1)];
		}

		public void Write(ushort address, byte value)
		{
			/* Not supported */
		}
	}
}
