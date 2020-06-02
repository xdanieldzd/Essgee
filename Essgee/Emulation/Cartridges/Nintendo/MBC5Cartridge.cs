using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Essgee.Exceptions;
using Essgee.Utilities;

namespace Essgee.Emulation.Cartridges.Nintendo
{
	// TODO: rumble?

	public class MBC5Cartridge : ICartridge
	{
		byte[] romData, ramData;
		bool hasCartRam;

		ushort romBank;
		byte ramBank;
		bool ramEnable;

		public MBC5Cartridge(int romSize, int ramSize)
		{
			romData = new byte[romSize];
			ramData = new byte[ramSize];

			romBank = 1;
			ramBank = 0;

			ramEnable = false;

			hasCartRam = false;
		}

		public void LoadRom(byte[] data)
		{
			Buffer.BlockCopy(data, 0, romData, 0, Math.Min(data.Length, romData.Length));
		}

		public void LoadRam(byte[] data)
		{
			Buffer.BlockCopy(data, 0, ramData, 0, Math.Min(data.Length, ramData.Length));
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
			return hasCartRam;
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
			if (address >= 0x0000 && address <= 0x3FFF)
			{
				return romData[address & 0x3FFF];
			}
			else if (address >= 0x4000 && address <= 0x7FFF)
			{
				return romData[(romBank << 14) | (address & 0x3FFF)];
			}
			else if (address >= 0xA000 && address <= 0xBFFF)
			{
				if (ramEnable && ramData.Length != 0)
					return ramData[(ramBank << 13) | (address & 0x1FFF)];
				else
					return 0xFF;
			}
			else
				return 0xFF;
		}

		public void Write(ushort address, byte value)
		{
			if (address >= 0x0000 && address <= 0x1FFF)
			{
				ramEnable = (value & 0x0F) == 0x0A;
			}
			else if (address >= 0x2000 && address <= 0x2FFF)
			{
				romBank = (byte)((romBank & 0x0100) | value);
				romBank &= (byte)((romData.Length >> 14) - 1);
				//if ((romBank & 0x01FF) == 0x00) romBank |= 0x01;    //TODO: verify
			}
			else if (address >= 0x3000 && address <= 0x3FFF)
			{
				romBank = (byte)((romBank & 0x00FF) | ((value & 0x01) << 8));
				romBank &= (byte)((romData.Length >> 14) - 1);
				//if ((romBank & 0x01FF) == 0x00) romBank |= 0x01;    //TODO: verify
			}
			else if (address >= 0x4000 && address <= 0x5FFF)
			{
				ramBank = (byte)(value & 0x0F);
			}
			else if (address >= 0xA000 && address <= 0xBFFF)
			{
				if (ramEnable && ramData.Length != 0)
				{
					ramData[(ramBank << 13) | (address & 0x1FFF)] = value;
					hasCartRam = true;
				}
			}
		}
	}
}
