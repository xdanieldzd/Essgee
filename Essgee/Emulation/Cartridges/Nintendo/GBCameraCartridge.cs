using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Drawing;

namespace Essgee.Emulation.Cartridges.Nintendo
{
	// TODO actually make somewhat accurate via https://github.com/AntonioND/gbcam-rev-engineer/blob/master/doc/gb_camera_doc_v1_1_1.pdf

	public class GBCameraCartridge : ICartridge
	{
		float[] outputGainTable =
		{
			14.0f, 15.5f, 17.0f, 18.5f, 20.0f, 21.5f, 23.0f, 24.5f,
			26.0f, 29.0f, 32.0f, 35.0f, 38.0f, 41.0f, 45.5f, 51.5f
		};

		public enum ImageSources
		{
			[Description("Random Noise")]
			Noise,
			[Description("Image File")]
			File
		}

		Random random;
		ImageSources imageSourceType;
		Bitmap scaledImage;
		byte[,] sourceBuffer;
		byte[,,] tileBuffer;

		byte[] romData, ramData;
		bool hasCartRam;

		byte romBank, ramBank;
		bool ramEnable;

		byte[] camRegisters;
		bool camSelected;

		public GBCameraCartridge(int romSize, int ramSize)
		{
			random = new Random();
			imageSourceType = ImageSources.Noise;
			scaledImage = new Bitmap(128, 112);
			sourceBuffer = new byte[128, 112];
			tileBuffer = new byte[14, 16, 16];

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

		public void SetImageSource(ImageSources source, string filename)
		{
			imageSourceType = source;

			if (imageSourceType == ImageSources.File)
			{
				using (var tempImage = new Bitmap(filename))
				{
					using (var g = System.Drawing.Graphics.FromImage(scaledImage))
					{
						var ratio = Math.Min(tempImage.Width / (float)scaledImage.Width, tempImage.Height / (float)scaledImage.Height);
						var srcWidth = (int)(scaledImage.Width * ratio);
						var srcHeight = (int)(scaledImage.Height * ratio);
						var srcX = (tempImage.Width - srcWidth) / 2;
						var srcY = (tempImage.Height - srcHeight) / 2;
						var scaledRect = new Rectangle(0, 0, scaledImage.Width, scaledImage.Height);

						g.FillRectangle(Brushes.White, scaledRect);
						g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
						g.DrawImage(tempImage, scaledRect, new Rectangle(srcX, srcY, srcWidth, srcHeight), GraphicsUnit.Pixel);
					}
				}
			}
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
								GenerateImage();
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

		private int Clamp(int value, int min, int max)
		{
			if (value < min) value = min;
			else if (value > max) value = max;
			return value;
		}

		private int Scale(int value, int min, int max, int minScaled, int maxScaled)
		{
			return (int)(minScaled + (float)(value - min) / (max - min) * (maxScaled - minScaled));
		}

		private void GenerateImage()
		{
			// TODO rewrite as per above pdf, chp 4

			var brightnessMultiplier = (((camRegisters[0x02] << 8 | camRegisters[0x03]) / 65535.0f) * (outputGainTable[camRegisters[0x01] & 0x1F] * 8.0f));

			/* Clear tile buffer */
			for (var x = 0; x < 14; x++)
				for (var y = 0; y < 16; y++)
					for (var z = 0; z < 16; z++)
						tileBuffer[x, y, z] = 0x00;

			/* Generate source data */
			for (var x = 0; x < scaledImage.Width; x++)
			{
				for (var y = 0; y < scaledImage.Height; y++)
				{
					switch (imageSourceType)
					{
						case ImageSources.Noise:
							sourceBuffer[x, y] = (byte)random.Next((int)brightnessMultiplier);
							break;

						case ImageSources.File:
							sourceBuffer[x, y] = (byte)(Clamp((int)(scaledImage.GetPixel(x, y).GetBrightness() * brightnessMultiplier), 0, 250) + random.Next(5));
							break;
					}
				}
			}

			/* Convert source data to GB tiles */
			for (var x = 0; x < scaledImage.Width; x++)
			{
				for (var y = 0; y < scaledImage.Height; y++)
				{
					var sensorValue = sourceBuffer[x, y];
					var matrixOffset = 0x06 + ((y % 4) * 12) + ((x % 4) * 3);

					var c = (byte)0;
					if (sensorValue < camRegisters[matrixOffset + 0]) c = 3;
					else if (sensorValue < camRegisters[matrixOffset + 1]) c = 2;
					else if (sensorValue < camRegisters[matrixOffset + 2]) c = 1;
					else c = 0;

					if ((c & 1) != 0) tileBuffer[y >> 3, x >> 3, ((y & 7) * 2) + 0] |= (byte)(1 << (7 - (7 & x)));
					if ((c & 2) != 0) tileBuffer[y >> 3, x >> 3, ((y & 7) * 2) + 1] |= (byte)(1 << (7 - (7 & x)));
				}
			}

			/* Copy tiles to cartridge RAM */
			int outputOffset = 0x100;
			for (var x = 0; x < 14; x++)
				for (var y = 0; y < 16; y++)
					for (var z = 0; z < 16; z++)
						ramData[outputOffset++] = tileBuffer[x, y, z];
		}
	}
}
