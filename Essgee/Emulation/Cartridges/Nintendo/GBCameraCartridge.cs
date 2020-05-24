using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace Essgee.Emulation.Cartridges.Nintendo
{
	// TODO actually make somewhat accurate via https://github.com/AntonioND/gbcam-rev-engineer/blob/master/doc/gb_camera_doc_v1_1_1.pdf

	public class GBCameraCartridge : ICartridge
	{
		public enum ImageSources
		{
			[Description("Random Noise")]
			Noise,
			[Description("Image File")]
			File
		}

		Random random;
		ImageSources imageSource;

		byte[] romData, ramData;
		bool hasCartRam;

		byte romBank, ramBank;
		bool ramEnable;

		byte[] camRegisters;
		bool camSelected;

		public GBCameraCartridge(int romSize, int ramSize)
		{
			random = new Random();

			romData = new byte[romSize];
			ramData = new byte[ramSize];

			romBank = 1;
			ramBank = 0;

			ramEnable = false;

			camRegisters = new byte[0x80];  // 0x36 used
			camSelected = false;

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

		public void SetImageSource(ImageSources source)
		{
			imageSource = source;
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
				if (!camSelected)
				{
					if ((camRegisters[0x00] & 0b1) == 0)
						return ramData[(ramBank << 13) | (address & 0x1FFF)];
					else
						return 0;
				}
				else
				{
					var reg = (byte)(address & 0x7F);
					if (reg == 0x00)
						return (byte)(camRegisters[reg] & 0x07);
					else
						return 0;
				}
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
				romBank = (byte)((romBank & 0xC0) | (value & 0x3F));
				romBank &= (byte)((romData.Length >> 14) - 1);
				if ((romBank & 0x3F) == 0x00) romBank |= 0x01;
			}
			else if (address >= 0x4000 && address <= 0x5FFF)
			{
				if ((value & 0x10) != 0)
				{
					camSelected = true;
				}
				else
				{
					camSelected = false;
					ramBank = (byte)(value & 0x0F);
				}
			}
			else if (address >= 0xA000 && address <= 0xBFFF)
			{
				if (!camSelected)
				{
					if (ramEnable)
					{
						ramData[(ramBank << 13) | (address & 0x1FFF)] = value;
						hasCartRam = true;
					}
				}
				else
				{
					var reg = (byte)(address & 0x7F);
					switch (reg)
					{
						case 0x00:
							if ((value & 0b1) != 0)
							{
								switch (imageSource)
								{
									case ImageSources.Noise:
										for (int i = 0; i < 14 * 16 * 16; i++)
											ramData[0x0100 + i] = (byte)random.Next(255);
										break;

									case ImageSources.File:
										//
										break;
								}
								value &= 0xFE;
							}
							break;

						default:
							//
							break;
					}

					camRegisters[reg] = value;
				}
			}
		}
	}
}
