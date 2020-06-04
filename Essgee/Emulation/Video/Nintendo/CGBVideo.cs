using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using Essgee.Emulation.CPU;
using Essgee.Exceptions;
using Essgee.EventArguments;
using Essgee.Utilities;

using static Essgee.Emulation.CPU.SM83;
using static Essgee.Emulation.Utilities;

namespace Essgee.Emulation.Video.Nintendo
{
	public class CGBVideo : DMGVideo
	{
		// FF4F - VBK
		byte vramBank;

		// FF51 - HDMA1
		byte dmaSourceHi;

		// FF52 - HDMA2
		byte dmaSourceLo;

		// FF53 - HDMA3
		byte dmaDestinationHi;

		// FF54 - HDMA4
		byte dmaDestinationLo;

		// FF55 - HDMA5
		byte dmaTransferLength;
		bool dmaTransferType;   // 0=General purpose, 1=Hblank

		// FF68 - BCPS
		byte bgPaletteIndex;
		bool bgPaletteAutoIncrement;

		// FF69 - BCPD

		// FF6A - OCPS
		byte objPaletteIndex;
		bool objPaletteAutoIncrement;

		// FF6B - OCPD

		//

		byte[] bgPaletteData, objPaletteData;

		protected const byte screenUsageBackgroundHighPriority = (1 << 3);

		ushort dmaSourceAddress
		{
			get { return (ushort)((dmaSourceHi << 8) | dmaSourceLo); }
			set
			{
				dmaSourceHi = (byte)((value >> 8) & 0xFF);
				dmaSourceLo = (byte)((value >> 0) & 0xFF);
			}
		}
		ushort dmaDestinationAddress
		{
			get { return (ushort)((dmaDestinationHi << 8) | dmaDestinationLo); }
			set
			{
				dmaDestinationHi = (byte)((value >> 8) & 0xFF);
				dmaDestinationLo = (byte)((value >> 0) & 0xFF);
			}
		}

		public CGBVideo(MemoryReadDelegate memoryRead, RequestInterruptDelegate requestInterrupt) : base(memoryRead, requestInterrupt)
		{
			vram = new byte[0x4000];

			//

			bgPaletteData = new byte[64];
			objPaletteData = new byte[64];
		}

		public override void Reset()
		{
			base.Reset();

			vramBank = 0;

			for (var i = 0; i < bgPaletteData.Length; i += 2)
			{
				bgPaletteData[i + 0] = 0x7F;
				bgPaletteData[i + 1] = 0xFF;
			}
			for (var i = 0; i < objPaletteData.Length; i += 2)
			{
				objPaletteData[i + 0] = 0x7F;
				objPaletteData[i + 1] = 0xFF;
			}
		}

		//

		protected override void RenderBackground(int y, int x)
		{
			var tileBase = (ushort)(bgWndTileSelect ? 0x0000 : 0x0800);
			var mapBase = (ushort)(bgMapSelect ? 0x1C00 : 0x1800);

			var yTransformed = (byte)(scrollY + y);
			var xTransformed = (byte)(scrollX + x);

			var mapAddress = mapBase + ((yTransformed >> 3) << 5) + (xTransformed >> 3);
			var tileNumber = vram[mapAddress];

			var tileAttribs = vram[0x2000 | mapAddress];
			var tileBgPalette = tileAttribs & 0b111;
			var tileVramBank = (tileAttribs >> 3) & 0b1;
			var tileHorizontalFlip = ((tileAttribs >> 5) & 0b1) == 0b1;
			var tileVerticalFlip = ((tileAttribs >> 6) & 0b1) == 0b1;
			var tileBgHasPriority = ((tileAttribs >> 7) & 0b1) == 0b1;

			var xShift = tileHorizontalFlip ? (xTransformed % 8) : (7 - (xTransformed % 8));
			var yShift = tileVerticalFlip ? (7 - (yTransformed & 7)) : (yTransformed & 7);

			if (!bgWndTileSelect)
				tileNumber = (byte)(tileNumber ^ 0x80);

			var tileAddress = (tileVramBank << 13) + tileBase + (tileNumber << 4) + (yShift << 1);

			var ba = (vram[tileAddress + 0] >> xShift) & 0b1;
			var bb = (vram[tileAddress + 1] >> xShift) & 0b1;
			var c = (byte)((bb << 1) | ba);

			if (c != 0)
				SetScreenUsageFlag(y, x, tileBgHasPriority ? screenUsageBackgroundHighPriority : screenUsageBackground);

			var paletteAddress = (tileBgPalette << 3) + ((c & 0b11) << 1);
			SetPixel(y, x, (ushort)((bgPaletteData[paletteAddress + 1] << 8) | bgPaletteData[paletteAddress + 0]));
		}

		protected override void RenderWindow(int y, int x)
		{
			var tileBase = (ushort)(bgWndTileSelect ? 0x0000 : 0x0800);
			var mapBase = (ushort)(wndMapSelect ? 0x1C00 : 0x1800);

			if (y < windowY) return;
			if (x < (windowX - 7)) return;

			var yTransformed = (byte)(y - windowY);
			var xTransformed = (byte)((7 - windowX) + x);

			var mapAddress = mapBase + ((yTransformed >> 3) << 5) + (xTransformed >> 3);
			var tileNumber = vram[mapAddress];

			var tileAttribs = vram[0x2000 | mapAddress];
			var tileBgPalette = tileAttribs & 0b111;
			var tileVramBank = (tileAttribs >> 3) & 0b1;
			var tileHorizontalFlip = ((tileAttribs >> 5) & 0b1) == 0b1;
			var tileVerticalFlip = ((tileAttribs >> 6) & 0b1) == 0b1;
			var tileBgHasPriority = ((tileAttribs >> 7) & 0b1) == 0b1;

			var xShift = tileHorizontalFlip ? (xTransformed % 8) : (7 - (xTransformed % 8));
			var yShift = tileVerticalFlip ? (7 - (yTransformed & 7)) : (yTransformed & 7);

			if (!bgWndTileSelect)
				tileNumber = (byte)(tileNumber ^ 0x80);

			var tileAddress = (tileVramBank << 13) + tileBase + (tileNumber << 4) + (yShift << 1);

			var ba = (vram[tileAddress + 0] >> xShift) & 0b1;
			var bb = (vram[tileAddress + 1] >> xShift) & 0b1;
			var c = (byte)((bb << 1) | ba);

			if (c != 0)
				SetScreenUsageFlag(y, x, tileBgHasPriority ? screenUsageBackgroundHighPriority : screenUsageWindow);    // TODO correct?

			var paletteAddress = (tileBgPalette << 3) + ((c & 0b11) << 1);
			SetPixel(y, x, (ushort)((bgPaletteData[paletteAddress + 1] << 8) | bgPaletteData[paletteAddress + 0]));
		}

		protected override void RenderSprites(int y, int x)
		{
			// TODO: more GBC sprite specifics! (priority etc)

			var objHeight = (objSize ? 16 : 8);
			var numObjDisplayed = 0;

			// Iterate over sprite slots
			for (var i = (byte)0; i < numOamSlots; i++)
			{
				// Get sprite Y coord & if sprite is not on current scanline, continue to next slot
				var objY = (short)(oam[(i * 4) + 0] - 16);
				if (y < objY || y >= (objY + objHeight)) continue;

				// Get sprite X coord, tile number & attributes
				var objX = (byte)(oam[(i * 4) + 1] - 8);
				var objTileNumber = oam[(i * 4) + 2];
				var objAttributes = oam[(i * 4) + 3];

				// Extract attributes
				var objPrioAboveBg = ((objAttributes >> 7) & 0b1) != 0b1;
				var objFlipY = ((objAttributes >> 6) & 0b1) == 0b1;
				var objFlipX = ((objAttributes >> 5) & 0b1) == 0b1;
				var objVramBank = (objAttributes >> 3) & 0b1;
				var objPalNumber = (objAttributes >> 0) & 0b111;

				// Iterate over pixels
				for (var px = 0; px < 8; px++)
				{
					// If sprite pixel X coord does not equal current rendering X coord, continue to next pixel
					if (x != (byte)(objX + px)) continue;

					// If sprite of lower X coord already exists -or- sprite of same X coord BUT lower slot exists, continue to next pixel
					var prevObjX = GetSpriteUsageCoord(y, x);
					var prevObjSlot = GetSpriteUsageSlot(y, x);
					if (prevObjX < objX || (prevObjX == objX && prevObjSlot < i)) continue;

					// If priority BG was already drawn /or/ sprite priority is not above background -and- BG/window pixel was already drawn, continue to next pixel
					if (IsScreenUsageFlagSet(y, x, screenUsageBackgroundHighPriority) ||
						(!objPrioAboveBg && (IsScreenUsageFlagSet(y, x, screenUsageBackground) || IsScreenUsageFlagSet(y, x, screenUsageWindow)))) continue;

					// Calculate tile address
					var xShift = objFlipX ? (px % 8) : (7 - (px % 8));
					var yShift = objFlipY ? (7 - ((y - objY) % 8)) : ((y - objY) % 8);
					if (objSize)
					{
						objTileNumber &= 0xFE;
						if ((objFlipY && y < (objY + 8)) || (!objFlipY && y >= (objY + 8)))
							objTileNumber |= 0x01;
					}
					var tileAddress = (objVramBank << 13) + (objTileNumber << 4) + (yShift << 1);

					// Get palette & bitplanes
					var ba = (vram[tileAddress + 0] >> xShift) & 0b1;
					var bb = (vram[tileAddress + 1] >> xShift) & 0b1;

					// Combine to color index, draw if color is not 0
					var c = (byte)((bb << 1) | ba);
					if (c != 0)
					{
						SetScreenUsageFlag(y, x, screenUsageSprite);
						SetSpriteUsage(y, x, objX, i);

						var paletteAddress = (objPalNumber << 3) + ((c & 0b11) << 1);
						SetPixel(y, x, (ushort)((objPaletteData[paletteAddress + 1] << 8) | objPaletteData[paletteAddress + 0]));
					}
				}

				// If sprites per line limit was exceeded, stop drawing sprites
				if (numObjDisplayed++ >= 10) break;
			}
		}

		protected void SetPixel(int y, int x, ushort c)
		{
			WriteColorToFramebuffer(c, ((y * 160) + (x % 160)) * 4);
		}

		private void WriteColorToFramebuffer(ushort c, int address)
		{
			RGB555toBGRA8888(c, ref outputFramebuffer, address);
		}

		//

		public override byte ReadVram(ushort address)
		{
			return base.ReadVram((ushort)(vramBank << 13 | address));
		}

		public override void WriteVram(ushort address, byte value)
		{
			base.WriteVram((ushort)(vramBank << 13 | address), value);
		}

		public override byte ReadPort(byte port)
		{
			switch (port)
			{
				case 0x4F:
					// VBK
					return (byte)(
						0xFE |
						(vramBank & 0b1));

				case 0x55:
					// HDMA5
					return (byte)(
						(dmaTransferLength & 0x7F) |
						(dmaTransferType ? (1 << 7) : 0));

				case 0x68:
					// BCPS
					return (byte)(
						(bgPaletteIndex & 0x3F) |
						(bgPaletteAutoIncrement ? (1 << 7) : 0));

				case 0x69:
					// BCPD
					if (modeNumber != 3) return bgPaletteData[bgPaletteIndex];
					else return 0xFF;

				case 0x6A:
					// OCPS
					return (byte)(
						(objPaletteIndex & 0x3F) |
						(objPaletteAutoIncrement ? (1 << 7) : 0));

				case 0x6B:
					// OCPD
					if (modeNumber != 3) return objPaletteData[objPaletteIndex];
					else return 0xFF;

				default:
					return base.ReadPort(port);
			}
		}

		public override void WritePort(byte port, byte value)
		{
			switch (port)
			{
				case 0x4F:
					vramBank = (byte)(value & 0b1);
					break;

				case 0x51:
					// HDMA1
					dmaSourceHi = value;
					break;

				case 0x52:
					// HDMA2
					dmaSourceLo = (byte)(value & 0xF0);
					break;

				case 0x53:
					// HDMA3
					dmaDestinationHi = (byte)(value & 0x1F);
					break;

				case 0x54:
					// HDMA4
					dmaDestinationLo = (byte)(value & 0xF0);
					break;

				case 0x55:
					// HDMA5
					dmaTransferLength = (byte)(value & 0x7F);
					dmaTransferType = ((value >> 7) & 0b1) == 0b1;

					// TODO make Hblank DMA run

					if (!dmaTransferType)
					{
						// General purpose DMA
						// TODO accuracy?
						for (int i = 0; i < (dmaTransferLength << 4); i++)
							vram[(vramBank << 13) | dmaDestinationAddress++] = memoryReadDelegate(dmaSourceAddress++);
						dmaTransferLength = 0xFF;
					}
					break;

				case 0x68:
					// BCPS
					bgPaletteIndex = (byte)(value & 0x3F);
					bgPaletteAutoIncrement = ((value >> 7) & 0b1) == 0b1;
					break;

				case 0x69:
					// BCPD
					if (modeNumber != 3) bgPaletteData[bgPaletteIndex] = value;
					if (bgPaletteAutoIncrement)
					{
						bgPaletteIndex++;
						bgPaletteIndex &= 0x3F;
					}
					break;

				case 0x6A:
					// OCPS
					objPaletteIndex = (byte)(value & 0x3F);
					objPaletteAutoIncrement = ((value >> 7) & 0b1) == 0b1;
					break;

				case 0x6B:
					// OCPD
					if (modeNumber != 3) objPaletteData[objPaletteIndex] = value;
					if (objPaletteAutoIncrement)
					{
						objPaletteIndex++;
						objPaletteIndex &= 0x3F;
					}
					break;

				default:
					base.WritePort(port, value);
					break;
			}
		}
	}
}
