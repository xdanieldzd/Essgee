using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.ComponentModel;

using Essgee.EventArguments;

namespace Essgee.Emulation.ExtDevices.Nintendo
{
	[Description("Game Boy Printer")]
	[ElementPriority(2)]
	public class GBPrinter : ISerialDevice
	{
		readonly Color[] defaultPalette = new Color[]
		{
			Color.FromArgb(0xF8, 0xF8, 0xF8),
			Color.FromArgb(0x9B, 0x9B, 0x9B),
			Color.FromArgb(0x3E, 0x3E, 0x3E),
			Color.FromArgb(0x1F, 0x1F, 0x1F)
		};

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
		byte marginBefore, marginAfter, palette, exposure;

		int imageHeight;
		int printDelay;

		public event EventHandler<SaveExtraDataEventArgs> SaveExtraData;
		protected virtual void OnSaveExtraData(SaveExtraDataEventArgs e) { SaveExtraData?.Invoke(this, e); }

		public bool ProvidesClock => false;

		public GBPrinter()
		{
			imageData = new List<byte>();
		}

		public void Initialize()
		{
			ResetPacket();

			packetBytesReceived = 0;
			dataBytesLeft = 0;

			status = 0;
			presence = 0;

			marginBefore = marginAfter = 0;
			palette = exposure = 0;

			imageHeight = 0;
			printDelay = 0;
		}

		public void Shutdown()
		{
			//
		}

		private void ResetPacket()
		{
			packet = (0, 0, false, 0, new byte[0], 0);
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
								marginBefore = (byte)((packet.data[1] >> 4) & 0x0F);
								marginAfter = (byte)(packet.data[1] & 0x0F);
								palette = packet.data[2];
								exposure = (byte)(packet.data[3] & 0x7F);

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
									if (printDelay >= 16)   // TODO: figure out actual print duration/timing?
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

		private void PerformPrint()
		{
			if (imageHeight == 0) return;

			/* Create bitmap for "printing" */
			using (var image = new Bitmap(160, imageHeight))
			{
				/* Convert image tiles to pixels */
				for (var y = 0; y < image.Height; y += 8)
				{
					for (var x = 0; x < image.Width; x += 8)
					{
						var tileAddress = ((y / 8) * 0x140) + ((x / 8) * 0x10);

						for (var py = 0; py < 8; py++)
						{
							for (var px = 0; px < 8; px++)
							{
								var ba = (imageData[tileAddress + 0] >> (7 - (px % 8))) & 0b1;
								var bb = (imageData[tileAddress + 1] >> (7 - (px % 8))) & 0b1;
								var c = (byte)((bb << 1) | ba);
								image.SetPixel(x + px, y + py, defaultPalette[(byte)((palette >> (c << 1)) & 0x03)]);
							}

							tileAddress += 2;
						}
					}
				}

				/* Apply approximate exposure (i.e. mess with the brightness a bit) */
				using (var adjustedImage = new Bitmap(image.Width, image.Height))
				{
					using (var g = System.Drawing.Graphics.FromImage(adjustedImage))
					{
						var scale = ((128 - exposure) / 128.0f) + 0.5f;
						var matrix = new float[][]
						{
							new float[] { scale, 0.0f, 0.0f, 0.0f, 0.0f },
							new float[] { 0.0f, scale, 0.0f, 0.0f, 0.0f },
							new float[] { 0.0f, 0.0f, scale, 0.0f, 0.0f },
							new float[] { 0.0f, 0.0f, 0.0f, 1.0f, 0.0f },
							new float[] { 0.0f, 0.0f, 0.0f, 0.0f, 1.0f }
						};

						var imageAttribs = new ImageAttributes();
						imageAttribs.ClearColorMatrix();
						imageAttribs.SetColorMatrix(new ColorMatrix(matrix), ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
						g.DrawImage(image, new Rectangle(0, 0, adjustedImage.Width, adjustedImage.Height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, imageAttribs);

						/* Save the image */
						OnSaveExtraData(new SaveExtraDataEventArgs(ExtraDataTypes.Image, ExtraDataOptions.IncludeDateTime, "Printout", adjustedImage));
					}
				}
			}
		}

		private void DumpPacket()
		{
			if (Program.AppEnvironment.EnableLogger)
			{
				Program.Logger.WriteLine("[Received GB Printer Packet]");
				Program.Logger.WriteLine("- Magic bytes: 0x" + packet.magic.ToString("X4"));
				Program.Logger.WriteLine("- Command: " + packet.command.ToString());
				Program.Logger.WriteLine("- Is data compressed? " + packet.isCompressed.ToString());
				Program.Logger.WriteLine("- Data length: 0x" + packet.dataLen.ToString("X4"));
				if (packet.dataLen != 0)
				{
					Program.Logger.WriteLine("- Data (UNCOMPRESSED):");
					for (int line = 0; line < ((packet.dataLen / 16) == 0 ? 1 : (packet.dataLen / 16)); line++)
					{
						Program.Logger.Write(" - 0x" + (line * 16).ToString("X4") + ": ");
						for (int byteno = 0; byteno < ((packet.dataLen % 16) == 0 ? 0x10 : (packet.dataLen % 16)); byteno++) Program.Logger.Write(packet.data[(line * 16) + byteno].ToString("X2") + " ");
						Program.Logger.WriteLine();
					}
				}
				Program.Logger.WriteLine("- Checksum: 0x" + packet.checksum.ToString("X4"));
				Program.Logger.WriteLine("[Status Returned]");
				Program.Logger.WriteLine("- Presence: " + presence.ToString());
				Program.Logger.WriteLine("- Status: " + status.ToString());
				Program.Logger.WriteLine();
			}
		}
	}
}
