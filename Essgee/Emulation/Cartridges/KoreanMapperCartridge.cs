using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Essgee.Exceptions;

namespace Essgee.Emulation.Cartridges
{
	public class KoreanMapperCartridge : ICartridge
	{
		byte[] romData;
		byte bankMask, pagingRegister;

		public KoreanMapperCartridge(int romSize, int ramSize)
		{
			pagingRegister = 0x02;

			romData = new byte[romSize];
		}

		public void LoadRom(byte[] data)
		{
			Buffer.BlockCopy(data, 0, romData, 0, Math.Min(data.Length, romData.Length));

			var romSizeRounded = 1;
			while (romSizeRounded < romData.Length) romSizeRounded <<= 1;

			bankMask = (byte)((romSizeRounded >> 14) - 1);
		}

		public void LoadRam(byte[] data)
		{
			//
		}

		public void SetState(Dictionary<string, dynamic> state)
		{
			throw new EmulationException($"Savestates not implemented for {GetType().Name}");
		}

		public Dictionary<string, dynamic> GetState()
		{
			throw new EmulationException($"Savestates not implemented for {GetType().Name}");
		}

		public byte[] GetRomData()
		{
			return romData;
		}

		public byte[] GetRamData()
		{
			return null;
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
			return 0xBFFF;
		}

		public byte Read(ushort address)
		{
			switch (address & 0xC000)
			{
				case 0x0000:
					return romData[address & 0x3FFF];

				case 0x4000:
					return romData[(0x01 << 14) | (address & 0x3FFF)];

				case 0x8000:
					return romData[((pagingRegister << 14) | (address & 0x3FFF))];

				default:
					throw new EmulationException(string.Format("Korean mapper: Cannot read from cartridge address 0x{0:X4}", address));
			}
		}

		public void Write(ushort address, byte value)
		{
			switch (address)
			{
				case 0xA000:
					pagingRegister = (byte)(value & bankMask);
					break;
			}
		}
	}
}
