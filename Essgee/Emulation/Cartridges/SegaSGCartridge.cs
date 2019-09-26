using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Essgee.Emulation.Cartridges
{
	public class SegaSGCartridge : ICartridge
	{
		byte[] romData, ramData;
		int romMask, ramMask;

		public SegaSGCartridge(int romSize, int ramSize)
		{
			romData = new byte[romSize];
			ramData = new byte[ramSize];

			romMask = 1;
			while (romMask < romSize) romMask <<= 1;
			romMask -= 1;

			ramMask = (ramSize - 1);
		}

		public void LoadRom(byte[] data)
		{
			Buffer.BlockCopy(data, 0, romData, 0, Math.Min(data.Length, romData.Length));
		}

		public void LoadRam(byte[] data)
		{
			Buffer.BlockCopy(data, 0, ramData, 0, Math.Min(data.Length, ramData.Length));
		}

		public void SetState(Dictionary<string, dynamic> state)
		{
			romMask = state[nameof(romMask)];
			ramMask = state[nameof(ramMask)];
		}

		public Dictionary<string, dynamic> GetState()
		{
			return new Dictionary<string, dynamic>
			{
				[nameof(romMask)] = romMask,
				[nameof(ramMask)] = ramMask
			};
		}

		public byte[] GetRomData()
		{
			return romData;
		}

		public byte[] GetRamData()
		{
			return ramData;
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
			return (ushort)((romData.Length + ramData.Length) - 1);
		}

		public byte Read(ushort address)
		{
			if (ramData.Length > 0)
			{
				if (address < (romMask + 1))
					return romData[address & romMask];
				else
					return ramData[address & ramMask];
			}
			else
			{
				return romData[address & romMask];
			}
		}

		public void Write(ushort address, byte value)
		{
			if (ramData.Length > 0 && address >= (romMask + 1))
				ramData[address & ramMask] = value;
		}
	}
}
