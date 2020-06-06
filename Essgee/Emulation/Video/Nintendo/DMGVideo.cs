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

namespace Essgee.Emulation.Video.Nintendo
{
	public class DMGVideo : IVideo
	{
		protected const int numOamSlots = 40;

		protected const int mode2Boundary = 80;
		protected const int mode3Boundary = mode2Boundary + 168;

		protected Action[] modeFunctions;

		protected readonly MemoryReadDelegate memoryReadDelegate;
		protected readonly RequestInterruptDelegate requestInterruptDelegate;

		public virtual (int X, int Y, int Width, int Height) Viewport => (0, 0, 160, 144);

		public virtual event EventHandler<SizeScreenEventArgs> SizeScreen;
		public virtual void OnSizeScreen(SizeScreenEventArgs e) { SizeScreen?.Invoke(this, e); }

		public virtual event EventHandler<RenderScreenEventArgs> RenderScreen;
		public virtual void OnRenderScreen(RenderScreenEventArgs e) { RenderScreen?.Invoke(this, e); }

		public virtual event EventHandler<EventArgs> EndOfScanline;
		public virtual void OnEndOfScanline(EventArgs e) { EndOfScanline?.Invoke(this, e); }

		//

		protected double clockRate, refreshRate;

		//

		[StateRequired]
		protected byte[,] vram;
		[StateRequired]
		protected byte[] oam;

		// FF40 - LCDC
		protected bool lcdEnable, wndMapSelect, wndEnable, bgWndTileSelect, bgMapSelect, objSize, objEnable, bgEnable;

		// FF41 - STAT
		protected bool lycLyInterrupt, m2OamInterrupt, m1VBlankInterrupt, m0HBlankInterrupt, coincidenceFlag;
		protected byte modeNumber;

		// FF42 - SCY
		protected byte scrollY;
		// FF43 - SCX
		protected byte scrollX;

		// FF44 - LY
		protected byte ly;
		// FF45 - LYC
		protected byte lyCompare;

		// FF46 - DMA
		protected byte oamDmaStart;

		// FF47 - BGP
		protected byte bgPalette;
		// FF48 - OBP0
		protected byte obPalette0;
		// FF49 - OBP1
		protected byte obPalette1;

		// FF4A - WY
		protected byte windowY;
		// FF4B - WX
		protected byte windowX;

		//

		protected int numSpritesOnLine, skipFrames;
		protected bool statIrqSignal, vBlankReady;

		readonly byte[][] colorValuesBgr = new byte[][]
		{
			/*              B     G     R */
			new byte[] { 0xF8, 0xF8, 0xF8 },	/* White */
			new byte[] { 0x9B, 0x9B, 0x9B },	/* Light gray */
			new byte[] { 0x3E, 0x3E, 0x3E },	/* Dark gray */
			new byte[] { 0x1F, 0x1F, 0x1F },	/* Black */
		};

		protected const byte screenUsageEmpty = 0;
		protected const byte screenUsageBackground = (1 << 0);
		protected const byte screenUsageWindow = (1 << 1);
		protected const byte screenUsageSprite = (1 << 2);
		protected byte[] screenUsage;

		protected const ushort spriteUsageEmpty = 0xFFFF;
		protected ushort[] spriteUsage;

		protected int cycleCount, cyclePenaltyMode3, currentScanline;
		protected byte[] outputFramebuffer;

		protected int clockCyclesPerLine;

		//

		public DMGVideo(MemoryReadDelegate memoryRead, RequestInterruptDelegate requestInterrupt)
		{
			vram = new byte[1, 0x2000];
			oam = new byte[0xA0];

			//

			modeFunctions = new Action[] { StepHBlank, StepVBlank, StepOAMSearch, StepLCDTransfer };

			memoryReadDelegate = memoryRead;
			requestInterruptDelegate = requestInterrupt;
		}

		public virtual void Startup()
		{
			Reset();

			if (memoryReadDelegate == null) throw new EmulationException("DMGVideo: Memory read delegate is null");
			if (requestInterruptDelegate == null) throw new EmulationException("DMGVideo: Request interrupt delegate is null");

			Debug.Assert(clockRate != 0.0, "Clock rate is zero", "{0} clock rate is not configured", GetType().FullName);
			Debug.Assert(refreshRate != 0.0, "Refresh rate is zero", "{0} refresh rate is not configured", GetType().FullName);
		}

		public virtual void Shutdown()
		{
			//
		}

		public virtual void Reset()
		{
			for (var i = 0; i < vram.GetLength(0); i++) vram[0, i] = 0;
			for (var i = 0; i < oam.Length; i++) oam[i] = 0;

			for (var i = (byte)0x40; i < 0x4C; i++)
			{
				// skip OAM dma
				if (i != 0x46) WritePort(i, 0x00);
			}

			numSpritesOnLine = skipFrames = 0;
			statIrqSignal = vBlankReady = false;

			ClearScreenUsage();
			ClearSpriteUsage();

			cycleCount = cyclePenaltyMode3 = currentScanline = 0;
		}

		public void SetClockRate(double clock)
		{
			clockRate = clock;

			ReconfigureTimings();
		}

		public void SetRefreshRate(double refresh)
		{
			refreshRate = refresh;

			ReconfigureTimings();
		}

		public virtual void SetRevision(int rev)
		{
			Debug.Assert(rev == 0, "Invalid revision", "{0} revision is invalid; only rev 0 is valid", GetType().FullName);
		}

		protected virtual void ReconfigureTimings()
		{
			/* Calculate cycles/line */
			clockCyclesPerLine = (int)Math.Round((clockRate / refreshRate) / 154);

			/* Create arrays */
			screenUsage = new byte[160 * 144];
			spriteUsage = new ushort[160 * 144];
			outputFramebuffer = new byte[(160 * 144) * 4];

			for (var y = 0; y < 144; y++)
				SetLine(y, 0xFF, 0xFF, 0xFF);
		}

		public virtual void Step(int clockCyclesInStep)
		{
			for (var c = 0; c < clockCyclesInStep; c++)
			{
				if (lcdEnable)
				{
					/* LCD enabled, handle LCD modes */
					modeFunctions[modeNumber]();
				}
				else
				{
					/* LCD disabled */
					modeNumber = 0;
					cycleCount = 0;

					currentScanline = 0;
					ly = 0;
				}
			}
		}

		protected virtual void StepVBlank()
		{
			// TODO: *should* be 4, but Altered Space hangs w/ any value lower than 8?
			if (cycleCount == 8 && vBlankReady)
			{
				requestInterruptDelegate(InterruptSource.VBlank);
				vBlankReady = false;
			}

			/* V-blank */
			cycleCount++;
			if (cycleCount == clockCyclesPerLine) EndVBlank();
		}

		protected virtual void EndVBlank()
		{
			/* End of scanline reached */
			OnEndOfScanline(EventArgs.Empty);
			currentScanline++;
			ly = (byte)currentScanline;

			/* Check for & request STAT interrupts */
			CheckAndRequestStatInterupt();

			if (currentScanline == 153)
			{
				// TODO: specific cycle this happens?

				/* LY reports as 0 on line 153 */
				ly = 0;

				CheckAndRequestStatInterupt();
			}
			else if (currentScanline == 154)
			{
				/* End of V-blank reached */
				modeNumber = 2;
				currentScanline = 0;
				ly = 0;

				CheckAndRequestStatInterupt();

				ClearScreenUsage();
				ClearSpriteUsage();
			}

			cycleCount = 0;
		}

		protected virtual void StepOAMSearch()
		{
			/* OAM search */

			/* Get object Y coord */
			var objIndex = cycleCount >> 1;
			var objY = oam[(objIndex << 2) + 0] - 16;

			/* Check if object is on current scanline & maximum number of objects was not exceeded, then increment counter */
			if (currentScanline >= objY && currentScanline < (objY + (objSize ? 16 : 8)) && numSpritesOnLine < 10)
				numSpritesOnLine++;

			/* Increment cycle count & check for next LCD mode */
			cycleCount++;
			if (cycleCount == mode2Boundary) EndOAMSearch();
		}

		protected virtual void EndOAMSearch()
		{
			modeNumber = 3;
			CheckAndRequestStatInterupt();

			cyclePenaltyMode3 += 8 * numSpritesOnLine;      // TODO: more details
		}

		protected virtual void StepLCDTransfer()
		{
			/* Data transfer to LCD */

			/* Render pixels */
			RenderPixel(currentScanline, cycleCount - mode2Boundary);

			/* Increment cycle count & check for next LCD mode */
			cycleCount++;
			if (cycleCount == mode3Boundary + cyclePenaltyMode3) EndLCDTransfer();
		}

		protected virtual void EndLCDTransfer()
		{
			modeNumber = 0;
			CheckAndRequestStatInterupt();
		}

		protected virtual void StepHBlank()
		{
			/* H-blank */

			/* Increment cycle count & check for next LCD mode */
			cycleCount++;
			if (cycleCount == clockCyclesPerLine) EndHBlank();
		}

		protected virtual void EndHBlank()
		{
			/* End of scanline reached */
			OnEndOfScanline(EventArgs.Empty);
			currentScanline++;
			ly = (byte)currentScanline;

			CheckAndRequestStatInterupt();

			numSpritesOnLine = 0;
			cyclePenaltyMode3 = scrollX % 8;

			if (currentScanline == 144)
			{
				modeNumber = 1;
				CheckAndRequestStatInterupt();

				/* Reached V-blank, request V-blank interrupt */
				vBlankReady = true;

				if (skipFrames > 0) skipFrames--;

				/* Submit screen for rendering */
				OnRenderScreen(new RenderScreenEventArgs(160, 144, outputFramebuffer.Clone() as byte[]));
			}
			else
			{
				modeNumber = 2;
				CheckAndRequestStatInterupt();
			}

			cycleCount = 0;
		}

		protected void CheckAndRequestStatInterupt()
		{
			if (!lcdEnable) return;

			var oldSignal = statIrqSignal;
			statIrqSignal = false;

			if (modeNumber == 0 && m0HBlankInterrupt) statIrqSignal = true;
			if (modeNumber == 1 && m1VBlankInterrupt) statIrqSignal = true;
			if (modeNumber == 2 && m2OamInterrupt) statIrqSignal = true;

			coincidenceFlag = (ly == lyCompare);
			if (coincidenceFlag && lycLyInterrupt) statIrqSignal = true;

			if (!oldSignal && statIrqSignal)
				requestInterruptDelegate(InterruptSource.LCDCStatus);
		}

		protected virtual void RenderPixel(int y, int x)
		{
			if (x < 0 || x >= 160 || y < 0 || y >= 144) return;

			if (skipFrames > 0)
			{
				SetPixel(y, x, 0xFF, 0xFF, 0xFF);
				return;
			}

			if (bgEnable)
				RenderBackground(y, x);
			else
				SetPixel(y, x, 0xFF, 0xFF, 0xFF);

			if (wndEnable) RenderWindow(y, x);
			if (objEnable) RenderSprites(y, x);
		}

		protected virtual void RenderBackground(int y, int x)
		{
			var tileBase = (ushort)(bgWndTileSelect ? 0x0000 : 0x0800);
			var mapBase = (ushort)(bgMapSelect ? 0x1C00 : 0x1800);

			var yTransformed = (byte)(scrollY + y);
			var xTransformed = (byte)(scrollX + x);

			var mapAddress = mapBase + ((yTransformed >> 3) << 5) + (xTransformed >> 3);
			var tileNumber = vram[0, mapAddress];

			if (!bgWndTileSelect)
				tileNumber = (byte)(tileNumber ^ 0x80);

			var tileAddress = tileBase + (tileNumber << 4) + ((yTransformed & 7) << 1);

			var ba = (vram[0, tileAddress + 0] >> (7 - (xTransformed % 8))) & 0b1;
			var bb = (vram[0, tileAddress + 1] >> (7 - (xTransformed % 8))) & 0b1;
			var c = (byte)((bb << 1) | ba);

			if (c != 0)
				SetScreenUsageFlag(y, x, screenUsageBackground);

			SetPixel(y, x, (byte)((bgPalette >> (c << 1)) & 0x03));
		}

		protected virtual void RenderWindow(int y, int x)
		{
			var tileBase = (ushort)(bgWndTileSelect ? 0x0000 : 0x0800);
			var mapBase = (ushort)(wndMapSelect ? 0x1C00 : 0x1800);

			if (y < windowY) return;
			if (x < (windowX - 7)) return;

			var yTransformed = (byte)(y - windowY);
			var xTransformed = (byte)((7 - windowX) + x);

			var mapAddress = mapBase + ((yTransformed >> 3) << 5) + (xTransformed >> 3);
			var tileNumber = vram[0, mapAddress];

			if (!bgWndTileSelect)
				tileNumber = (byte)(tileNumber ^ 0x80);

			var tileAddress = tileBase + (tileNumber << 4) + ((yTransformed & 7) << 1);

			var ba = (vram[0, tileAddress + 0] >> (7 - (xTransformed % 8))) & 0b1;
			var bb = (vram[0, tileAddress + 1] >> (7 - (xTransformed % 8))) & 0b1;
			var c = (byte)((bb << 1) | ba);

			if (c != 0)
				SetScreenUsageFlag(y, x, screenUsageWindow);

			SetPixel(y, x, (byte)((bgPalette >> (c << 1)) & 0x03));
		}

		protected virtual void RenderSprites(int y, int x)
		{
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
				var objPalNumber = ((objAttributes >> 4) & 0b1);

				// Iterate over pixels
				for (var px = 0; px < 8; px++)
				{
					// If sprite pixel X coord does not equal current rendering X coord, continue to next pixel
					if (x != (byte)(objX + px)) continue;

					// If sprite of lower X coord already exists -or- sprite of same X coord BUT lower slot exists, continue to next pixel
					var prevObjX = GetSpriteUsageCoord(y, x);
					var prevObjSlot = GetSpriteUsageSlot(y, x);
					if (prevObjX < objX || (prevObjX == objX && prevObjSlot < i)) continue;

					// If sprite priority is not above background -and- BG/window pixel was already drawn, continue to next pixel
					if (!objPrioAboveBg &&
						(IsScreenUsageFlagSet(y, x, screenUsageBackground) || IsScreenUsageFlagSet(y, x, screenUsageWindow))) continue;

					// Calculate tile address
					var xShift = objFlipX ? (px % 8) : (7 - (px % 8));
					var yShift = objFlipY ? (7 - ((y - objY) % 8)) : ((y - objY) % 8);
					if (objSize)
					{
						objTileNumber &= 0xFE;
						if ((objFlipY && y < (objY + 8)) || (!objFlipY && y >= (objY + 8)))
							objTileNumber |= 0x01;
					}
					var tileAddress = (objTileNumber << 4) + (yShift << 1);

					// Get palette & bitplanes
					var pal = (objPalNumber == 0 ? obPalette0 : obPalette1);
					var ba = (vram[0, tileAddress + 0] >> xShift) & 0b1;
					var bb = (vram[0, tileAddress + 1] >> xShift) & 0b1;

					// Combine to color index, draw if color is not 0
					var c = (byte)((bb << 1) | ba);
					if (c != 0)
					{
						SetScreenUsageFlag(y, x, screenUsageSprite);
						SetSpriteUsage(y, x, objX, i);
						SetPixel(y, x, (byte)((pal >> (c << 1)) & 0x03));
					}
				}

				// If sprites per line limit was exceeded, stop drawing sprites
				if (numObjDisplayed++ >= 10) break;
			}
		}

		protected void SetLine(int y, byte c)
		{
			for (int x = 0; x < 160; x++)
				SetPixel(y, x, c);
		}

		protected void SetLine(int y, byte b, byte g, byte r)
		{
			for (int x = 0; x < 160; x++)
				SetPixel(y, x, b, g, r);
		}

		protected void SetPixel(int y, int x, byte c)
		{
			WriteColorToFramebuffer(c, ((y * 160) + (x % 160)) * 4);
		}

		protected void SetPixel(int y, int x, byte b, byte g, byte r)
		{
			WriteColorToFramebuffer(b, g, r, ((y * 160) + (x % 160)) * 4);
		}

		protected virtual void WriteColorToFramebuffer(byte c, int address)
		{
			outputFramebuffer[address + 0] = colorValuesBgr[c & 0x03][0];
			outputFramebuffer[address + 1] = colorValuesBgr[c & 0x03][1];
			outputFramebuffer[address + 2] = colorValuesBgr[c & 0x03][2];
			outputFramebuffer[address + 3] = 0xFF;
		}

		protected virtual void WriteColorToFramebuffer(byte b, byte g, byte r, int address)
		{
			outputFramebuffer[address + 0] = b;
			outputFramebuffer[address + 1] = g;
			outputFramebuffer[address + 2] = r;
			outputFramebuffer[address + 3] = 0xFF;
		}

		protected virtual void ClearScreenUsage()
		{
			for (var i = 0; i < screenUsage.Length; i++)
				screenUsage[i] = screenUsageEmpty;
		}

		protected virtual void ClearSpriteUsage()
		{
			for (var i = 0; i < spriteUsage.Length; i++)
				spriteUsage[i] = spriteUsageEmpty;
		}

		protected ushort GetScreenUsageFlag(int y, int x)
		{
			return screenUsage[(y * 160) + (x % 160)];
		}

		protected bool IsScreenUsageFlagSet(int y, int x, byte flag)
		{
			return ((GetScreenUsageFlag(y, x) & flag) == flag);
		}

		protected void SetScreenUsageFlag(int y, int x, byte flag)
		{
			screenUsage[(y * 160) + (x % 160)] |= flag;
		}

		protected void ClearScreenUsageFlag(int y, int x, byte flag)
		{
			screenUsage[(y * 160) + (x % 160)] &= (byte)~flag;
		}

		protected void SetSpriteUsage(int y, int x, byte objX, byte objSlot)
		{
			spriteUsage[(y * 160) + (x % 160)] = (ushort)((objX << 8) | objSlot);
		}

		protected byte GetSpriteUsageCoord(int y, int x)
		{
			return (byte)((spriteUsage[(y * 160) + (x % 160)] >> 8) & 0xFF);
		}

		protected byte GetSpriteUsageSlot(int y, int x)
		{
			return (byte)((spriteUsage[(y * 160) + (x % 160)] >> 0) & 0xFF);
		}

		//

		public virtual byte ReadVram(ushort address)
		{
			if (modeNumber != 3)
				return vram[0, address & (vram.Length - 1)];
			else
				return 0xFF;
		}

		public virtual void WriteVram(ushort address, byte value)
		{
			if (modeNumber != 3)
				vram[0, address & (vram.Length - 1)] = value;
		}

		public virtual byte ReadOam(ushort address)
		{
			if (modeNumber != 2 && modeNumber != 3)
				return oam[address - 0xFE00];
			else
				return 0xFF;
		}

		public virtual void WriteOam(ushort address, byte value)
		{
			if (modeNumber != 2 && modeNumber != 3)
				oam[address - 0xFE00] = value;
		}

		public virtual byte ReadPort(byte port)
		{
			switch (port)
			{
				case 0x40:
					// LCDC
					return (byte)(
						(lcdEnable ? (1 << 7) : 0) |
						(wndMapSelect ? (1 << 6) : 0) |
						(wndEnable ? (1 << 5) : 0) |
						(bgWndTileSelect ? (1 << 4) : 0) |
						(bgMapSelect ? (1 << 3) : 0) |
						(objSize ? (1 << 2) : 0) |
						(objEnable ? (1 << 1) : 0) |
						(bgEnable ? (1 << 0) : 0));

				case 0x41:
					// STAT
					return (byte)(
						0x80 |
						(lycLyInterrupt ? (1 << 6) : 0) |
						(m2OamInterrupt ? (1 << 5) : 0) |
						(m1VBlankInterrupt ? (1 << 4) : 0) |
						(m0HBlankInterrupt ? (1 << 3) : 0) |
						(coincidenceFlag ? (1 << 2) : 0) |
						((modeNumber & 0b11) << 0));

				case 0x42:
					// SCY
					return scrollY;

				case 0x43:
					// SCX
					return scrollX;

				case 0x44:
					// LY
					return ly;

				case 0x45:
					// LYC
					return lyCompare;

				case 0x46:
					// DMA
					return oamDmaStart;

				case 0x47:
					// BGP
					return bgPalette;

				case 0x48:
					// OBP0
					return obPalette0;

				case 0x49:
					// OBP1
					return obPalette1;

				case 0x4A:
					// WY
					return windowY;

				case 0x4B:
					//WX
					return windowX;

				default:
					return 0xFF;
			}
		}

		public virtual void WritePort(byte port, byte value)
		{
			switch (port)
			{
				case 0x40:
					// LCDC
					{
						var newLcdEnable = ((value >> 7) & 0b1) == 0b1;
						if (lcdEnable != newLcdEnable)
						{
							modeNumber = 2;
							currentScanline = 0;
							ly = 0;

							CheckAndRequestStatInterupt();

							if (newLcdEnable)
								skipFrames = 4;
						}

						lcdEnable = newLcdEnable;
						wndMapSelect = ((value >> 6) & 0b1) == 0b1;
						wndEnable = ((value >> 5) & 0b1) == 0b1;
						bgWndTileSelect = ((value >> 4) & 0b1) == 0b1;
						bgMapSelect = ((value >> 3) & 0b1) == 0b1;
						objSize = ((value >> 2) & 0b1) == 0b1;
						objEnable = ((value >> 1) & 0b1) == 0b1;
						bgEnable = ((value >> 0) & 0b1) == 0b1;
					}
					break;

				case 0x41:
					// STAT
					lycLyInterrupt = ((value >> 6) & 0b1) == 0b1;
					m2OamInterrupt = ((value >> 5) & 0b1) == 0b1;
					m1VBlankInterrupt = ((value >> 4) & 0b1) == 0b1;
					m0HBlankInterrupt = ((value >> 3) & 0b1) == 0b1;

					CheckAndRequestStatInterupt();

					// TODO: correct?
					if (lcdEnable && modeNumber == 1 && currentScanline != 0)
						requestInterruptDelegate(InterruptSource.LCDCStatus);
					break;

				case 0x42:
					// SCY
					scrollY = value;
					break;

				case 0x43:
					// SCX
					scrollX = value;
					break;

				case 0x44:
					// LY
					break;

				case 0x45:
					// LYC
					lyCompare = value;
					CheckAndRequestStatInterupt();
					break;

				case 0x46:
					// DMA
					oamDmaStart = value;
					for (int src = 0, dst = oamDmaStart << 8; src < 0xA0; src++, dst++)
						oam[src] = memoryReadDelegate((ushort)dst);
					break;

				case 0x47:
					// BGP
					bgPalette = value;
					break;

				case 0x48:
					// OBP0
					obPalette0 = value;
					break;

				case 0x49:
					// OBP1
					obPalette1 = value;
					break;

				case 0x4A:
					// WY
					windowY = value;
					break;

				case 0x4B:
					// WX
					windowX = value;
					break;
			}
		}
	}
}
