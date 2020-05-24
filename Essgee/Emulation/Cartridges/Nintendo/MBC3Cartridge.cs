using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Essgee.Exceptions;
using Essgee.Utilities;

namespace Essgee.Emulation.Cartridges.Nintendo
{
	public class MBC3Cartridge : ICartridge
	{
		byte[] romData, ramData;
		bool hasCartRam;

		byte romBank, ramBank;
		bool ramEnable;

		byte[] rtcRegisters;
		bool rtcSelected;
		byte rtcRegSelected;

		public MBC3Cartridge(int romSize, int ramSize)
		{
			romData = new byte[romSize];
			ramData = new byte[ramSize];

			romBank = 1;
			ramBank = 0;

			ramEnable = false;

			rtcRegisters = new byte[0x05];
			rtcSelected = false;
			rtcRegSelected = 0;

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
				if (ramEnable)
				{
					if (!rtcSelected)
					{
						return ramData[(ramBank << 13) | (address & 0x1FFF)];
					}
					else
					{
						//TODO rtc registers
						return 0;
					}
				}
				else
					return 0;
			}
			else
				return 0;
		}

		public void Write(ushort address, byte value)
		{
			if (address >= 0x0000 && address <= 0x1FFF)
			{
				ramEnable = (value & 0x0F) == 0x0A;
			}
			else if (address >= 0x2000 && address <= 0x3FFF)
			{
				romBank = (byte)((romBank & 0x80) | (value & 0x7F));
				romBank &= (byte)((romData.Length >> 14) - 1);
				if (romBank == 0x00) romBank = 0x01;
			}
			else if (address >= 0x4000 && address <= 0x5FFF)
			{
				if (value >= 0x00 && value <= 0x07)
				{
					rtcSelected = false;
					ramBank = (byte)(value & 0x03);
				}
				else if (value >= 0x08 && value <= 0x0C)
				{
					rtcSelected = true;
					rtcRegSelected = (byte)(value >> 3);
				}
			}
			else if (address >= 0x6000 && address <= 0x7FFF)
			{
				//TODO latch clock data
			}
			else if (address >= 0xA000 && address <= 0xBFFF)
			{
				if (ramEnable)
				{
					if (!rtcSelected)
					{
						ramData[(ramBank << 13) | (address & 0x1FFF)] = value;
						hasCartRam = true;
					}
					else
					{
						//TODO rtc registers
					}
				}
			}
		}
	}
}
