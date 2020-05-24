using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Essgee.Emulation.Peripherals.Serial
{
	public class GBPrinter : ISerialDevice
	{
		Color[] defaultPalette = new Color[]
		{
			Color.FromArgb(248, 248, 248),
			Color.FromArgb(160, 160, 160),
			Color.FromArgb(80, 80, 80),
			Color.FromArgb(0, 0, 0)
		};

		Color[] modifiedPalette = new Color[4];

		enum PrinterCommands : byte
		{
			Initialize = 0x01,
			StartPrinting = 0x02,
			Unknown = 0x03,
			ImageTransfer = 0x04,
			ReadStatus = 0x0F
		}

		[Flags]
		enum PrinterPresenceBits : byte
		{
			Unknown = (1 << 0),
			Present = (1 << 7),
		}

		[Flags]
		enum PrinterStatusBits : byte
		{
			BadChecksum = (1 << 0),
			PrintInProgress = (1 << 1),
			PrintRequested = (1 << 2),
			ReadyToPrint = (1 << 3),
			LowVoltage = (1 << 4),
			Unknown = (1 << 5),
			PaperJam = (1 << 6),
			ThermalProblem = (1 << 7)
		};

		(ushort magic, PrinterCommands command, bool isCompressed, ushort dataLen, byte[] data, ushort checksum) packet;
		int packetBytesReceived;
		int dataBytesLeft;

		PrinterStatusBits status;
		PrinterPresenceBits presence;

		List<byte> imageData;
		int marginBefore, marginAfter;
		byte palette, exposure;

		int imageHeight;
		int printDelay;

		public GBPrinter()
		{
			ResetPacket();

			packetBytesReceived = 0;
			dataBytesLeft = 0;

			status = 0;
			presence = 0;

			imageData = new List<byte>();
			marginBefore = marginAfter = 0;
			palette = exposure = 0;

			imageHeight = 0;
			printDelay = 0;
		}

		private void ResetPacket()
		{
			packet = (0, 0, false, 0, new byte[0], 0);
		}

		public bool ProvidesClock()
		{
			return false;
		}

		public byte DoSlaveTransfer(byte data)
		{
			byte ret = 0;

			if (dataBytesLeft == 0)
			{
				switch (packetBytesReceived)
				{
					case 0x00:
						if (data == 0x88)
						{
							/* First magic byte; reset packet */
							ResetPacket();
							packet.magic |= (ushort)(data << 8);
						}
						break;

					case 1:
						if (data == 0x33)
						{
							/* Second magic byte */
							packet.magic |= data;
							presence = (PrinterPresenceBits.Present | PrinterPresenceBits.Unknown);
						}
						break;

					case 2:
						/* Command byte */
						packet.command = (PrinterCommands)data;
						break;

					case 3:
						/* Compression flag */
						packet.isCompressed = (data & 0x01) != 0;
						break;

					case 4:
						/* Data length LSB */
						packet.dataLen |= data;
						break;

					case 5:
						/* Data length MSB */
						packet.dataLen |= (ushort)(data << 8);
						packet.data = new byte[packet.dataLen];
						dataBytesLeft = packet.dataLen;
						break;

					case 6:
						/* Checksum LSB */
						packet.checksum |= data;
						break;

					case 7:
						/* Checksum MSB */
						packet.checksum |= (ushort)(data << 8);
						break;

					case 8:
						/* Printer presence */
						ret = (byte)presence;
						break;


					case 9:
						/* Printer status */

						/* First, we're done with the packet, so check what we need to do now */
						packet.data = packet.data.Reverse().ToArray();
						switch (packet.command)
						{
							case PrinterCommands.Initialize:
								/* Reset some data */
								status = 0;
								imageData.Clear();
								imageHeight = 0;
								printDelay = 0;
								break;

							case PrinterCommands.ImageTransfer:
								/* Copy packet data for drawing, increase image height & tell GB we're ready to print */
								if (packet.data.Length > 0)
								{
									if (packet.isCompressed)
									{
										/* Decompress RLE first! */
										List<byte> decomp = new List<byte>();
										int ofs = 0, numbytes = 0;
										while (ofs < packet.dataLen)
										{
											if ((packet.data[ofs] & 0x80) != 0)
											{
												/* Compressed */
												numbytes = (packet.data[ofs] & 0x7F) + 2;
												for (int i = 0; i < numbytes; i++) decomp.Add(packet.data[ofs + 1]);
												ofs += 2;
											}
											else
											{
												/* Uncompressed */
												numbytes = (packet.data[ofs] & 0x7F) + 1;
												for (int i = 0; i < numbytes; i++) decomp.Add(packet.data[ofs + 1 + i]);
												ofs += (numbytes + 1);
											}
										}
										packet.data = decomp.ToArray();
										packet.dataLen = (ushort)decomp.Count;
									}

									imageData.AddRange(packet.data);
									imageHeight += (packet.data.Length / 0x28);

									status |= PrinterStatusBits.ReadyToPrint;
								}
								break;

							case PrinterCommands.StartPrinting:
								/* Fetch parameters from packet, tell GB that we're about to print & perform printing */
								marginBefore = (packet.data[1] >> 4);
								marginAfter = (packet.data[1] & 0xF);
								palette = packet.data[2];
								exposure = packet.data[3];

								status &= ~PrinterStatusBits.ReadyToPrint;
								status |= PrinterStatusBits.PrintRequested;
								PerformPrint();
								break;

							case PrinterCommands.ReadStatus:
								if ((status & PrinterStatusBits.PrintRequested) != 0)
								{
									/* If we said printing has been requested, tell the GB it's in progress now */
									status &= ~PrinterStatusBits.PrintRequested;
									status |= PrinterStatusBits.PrintInProgress;
								}
								else if ((status & PrinterStatusBits.PrintInProgress) != 0)
								{
									/* Delay the process a bit... */
									printDelay++;
									if (printDelay >= 8)
									{
										/* If we said printing is in progress, tell the GB we're finished with it */
										status &= ~PrinterStatusBits.PrintInProgress;
										printDelay = 0;
									}
								}
								break;
						}

						/* End of packet */
						DumpPacket();
						packetBytesReceived = 0;

						return (byte)status;
				}

				packetBytesReceived++;
			}
			else
			{
				if (dataBytesLeft > 0)
					packet.data[--dataBytesLeft] = data;
			}

			return ret;
		}

		public byte DoMasterTransfer(byte data)
		{
			/* Not used */
			return 0xFF;
		}

		private int Clamp(int value, int min, int max)
		{
			if (value < min) value = min;
			else if (value > max) value = max;
			return value;
		}

		private void PerformPrint()
		{
			if (imageHeight == 0) return;

			/* Create new palette with changed brightness (APPROXIMATION) */
			sbyte colorModifier = (sbyte)-(exposure - 0x60);
			for (int i = 0; i < modifiedPalette.Length; i++)
			{
				modifiedPalette[i] = Color.FromArgb(
					defaultPalette[i].A,
					Clamp(defaultPalette[i].R + colorModifier, 0, 255),
					Clamp(defaultPalette[i].G + colorModifier, 0, 255),
					Clamp(defaultPalette[i].B + colorModifier, 0, 255));
			}

			/* Create bitmap for "printing" */
			using (var image = new Bitmap(160, imageHeight))
			{
				/* Convert image tiles to pixels */
				for (int y = 0; y < image.Height; y += 8)
				{
					for (int x = 0; x < image.Width; x += 8)
					{
						int tileAddress = ((y / 8) * 0x140) + ((x / 8) * 0x10);

						for (int py = 0; py < 8; py++)
						{
							for (int px = 0; px < 8; px++)
							{
								var ba = (imageData[tileAddress + 0] >> (7 - (px % 8))) & 0b1;
								var bb = (imageData[tileAddress + 1] >> (7 - (px % 8))) & 0b1;
								var c = (byte)((bb << 1) | ba);
								image.SetPixel(x + px, y + py, modifiedPalette[(byte)((palette >> (c << 1)) & 0x03)]);
							}

							tileAddress += 2;
						}
					}
				}

				/* Save the image */


				// TODO don't hardcode paths??
				var i = 0;
				var fn = string.Empty;
				while (System.IO.File.Exists(fn = System.IO.Path.Combine(@"D:\temp\essgee\print\", $"{i}.png"))) i++;
				image.Save(fn);
			}
		}

		private void DumpPacket()
		{
			//
		}
	}
}
