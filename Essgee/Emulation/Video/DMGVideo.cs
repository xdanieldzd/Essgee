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

using static Essgee.Emulation.Machines.GameBoy;

namespace Essgee.Emulation.Video
{
	public class DMGVideo : IVideo
	{
		readonly LR35902.MemoryReadDelegate memoryReadDelegate;
		readonly RequestInterruptDelegate requestInterruptDelegate;

		public virtual (int X, int Y, int Width, int Height) Viewport => (0, 0, 160, 144);

		public virtual event EventHandler<SizeScreenEventArgs> SizeScreen;
		public virtual void OnSizeScreen(SizeScreenEventArgs e) { SizeScreen?.Invoke(this, e); }

		public virtual event EventHandler<RenderScreenEventArgs> RenderScreen;
		public virtual void OnRenderScreen(RenderScreenEventArgs e) { RenderScreen?.Invoke(this, e); }

		public virtual event EventHandler<EventArgs> EndOfScanline;
		public virtual void OnEndOfScanline(EventArgs e) { EndOfScanline?.Invoke(this, e); }

		readonly int[] cyclesPerMode = new int[] { 204, 456, 80, 172 };

		//

		protected double clockRate, refreshRate;

		//

		[StateRequired]
		protected byte[] vram, oam;

		// FF40
		protected bool lcdEnable, wndMapSelect, wndEnable, bgWndTileSelect, bgMapSelect, objSize, objEnable, bgEnable;

		// FF41
		protected bool lycLyInterrupt, m2OamInterrupt, m1VBlankInterrupt, m0HBlankInterrupt, coincidenceFlag;
		protected byte modeNumber;

		// FF42
		protected byte scrollY;
		// FF43
		protected byte scrollX;

		// FF44
		protected byte currentScanline;
		// FF45
		protected byte lyCompare;

		// FF46
		protected byte oamDmaStart;

		// FF47
		protected byte bgPalette;
		// FF48
		protected byte obPalette0;
		// FF49
		protected byte obPalette1;

		// FF4A
		protected byte windowY;
		// FF4B
		protected byte windowX;

		//

		int oamDmaCurrentSource, oamDmaBytesLeft;

		readonly byte[][] colorValuesBgr = new byte[][]
		{
			/*              B     G     R */
			new byte[] { 0xF8, 0xF8, 0xF8 },	/* White */
			new byte[] { 0x7C, 0x7C, 0x7C },	/* Light gray */
			new byte[] { 0x3E, 0x3E, 0x3E },	/* Dark gray */
			new byte[] { 0x1F, 0x1F, 0x1F },	/* Black */
		};

		protected const byte screenUsageEmpty = 0;
		protected const byte screenUsageBackground = (1 << 0);
		protected const byte screenUsageSprite = (1 << 1);

		// 0000 -- empty
		// 0001 -- background
		// nn02 -- sprite (nn==sprite number)
		protected ushort[] screenUsage;

		protected int cycleCount;
		protected byte[] outputFramebuffer;

		protected int clockCyclesPerLine;

		//

		public DMGVideo(LR35902.MemoryReadDelegate memoryRead, RequestInterruptDelegate requestInterrupt)
		{
			vram = new byte[0x2000];
			oam = new byte[0xA0];

			//

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
			lcdEnable = wndMapSelect = wndEnable = bgWndTileSelect = bgMapSelect = objSize = objEnable = bgEnable = false;

			lycLyInterrupt = m2OamInterrupt = m1VBlankInterrupt = m0HBlankInterrupt = coincidenceFlag = false;
			modeNumber = 0;

			scrollY = scrollX = 0;

			currentScanline = 0;
			lyCompare = 0;

			oamDmaStart = 0;

			bgPalette = 0;
			obPalette0 = 0;
			obPalette1 = 0;

			windowY = windowX = 0;

			oamDmaCurrentSource = oamDmaBytesLeft = 0;

			ClearScreenUsage();

			cycleCount = 0;
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
			clockCyclesPerLine = (int)Math.Round((clockRate / refreshRate) / 153);

			/* Create arrays */
			screenUsage = new ushort[160 * 144];
			outputFramebuffer = new byte[(160 * 144) * 4];
		}

		public virtual void Step(int clockCyclesInStep)
		{
			// http://imrannazar.com/GameBoy-Emulation-in-JavaScript:-GPU-Timings

			// TODO verify and stuff, etc etc etc......

			cycleCount += clockCyclesInStep;

			if (oamDmaBytesLeft > 0)
			{
				for (var i = 0; i < clockCyclesInStep / 4; i++)
				{
					oam[0xA0 - oamDmaBytesLeft] = memoryReadDelegate((ushort)oamDmaCurrentSource++);
					oamDmaBytesLeft--;
					if (oamDmaBytesLeft <= 0) break;
				}
				return;
			}

			switch (modeNumber)
			{
				// Mode 2 (OAM search)
				case 2:
					if (cycleCount >= cyclesPerMode[modeNumber])
					{
						if (m2OamInterrupt)
							requestInterruptDelegate(InterruptSource.LCDCStatus);

						coincidenceFlag = (currentScanline == lyCompare);

						if (lycLyInterrupt && coincidenceFlag)
							requestInterruptDelegate(InterruptSource.LCDCStatus);

						cycleCount = 0;
						modeNumber = 3;
					}
					break;

				// Mode 3 (Data transfer to LCD)
				case 3:
					if (cycleCount >= cyclesPerMode[modeNumber])
					{
						cycleCount = 0;
						modeNumber = 0;

						RenderLine(currentScanline);
					}
					break;

				// Mode 0 (H-blank)
				case 0:
					if (cycleCount >= cyclesPerMode[modeNumber])
					{
						OnEndOfScanline(EventArgs.Empty);

						if (m0HBlankInterrupt)
							requestInterruptDelegate(InterruptSource.LCDCStatus);

						coincidenceFlag = (currentScanline == lyCompare);

						cycleCount = 0;
						currentScanline++;

						if (lycLyInterrupt && coincidenceFlag)
							requestInterruptDelegate(InterruptSource.LCDCStatus);

						if (currentScanline == 144)
						{
							requestInterruptDelegate(InterruptSource.VBlank);

							//System.IO.File.WriteAllBytes(@"D:\Temp\Essgee\vram.gb", vram);

							modeNumber = 1;
							OnRenderScreen(new RenderScreenEventArgs(160, 144, outputFramebuffer.Clone() as byte[]));
						}
						else
						{
							modeNumber = 2;
						}
					}
					break;

				// Mode 1 (V-blank)
				case 1:
					if (cycleCount >= cyclesPerMode[modeNumber])
					{
						if (lycLyInterrupt && coincidenceFlag)
							requestInterruptDelegate(InterruptSource.LCDCStatus);

						if (m1VBlankInterrupt)
							requestInterruptDelegate(InterruptSource.LCDCStatus);

						cycleCount = 0;
						currentScanline++;

						if (currentScanline > 153)
						{
							modeNumber = 2;
							currentScanline = 0;

							ClearScreenUsage();
						}
					}
					break;
			}
		}

		protected virtual void RenderLine(int y)
		{
			if (lcdEnable)
			{
				var tileBase = (ushort)(bgWndTileSelect ? 0x0000 : 0x0800);

				if (bgEnable)
				{
					var mapBase = (ushort)(bgMapSelect ? 0x1C00 : 0x1800);
					RenderBackground(y, mapBase, tileBase, false);
				}
				else
					SetLine(y, 0xFF, 0xFF, 0xFF);

				if (wndEnable)
				{
					var mapBase = (ushort)(wndMapSelect ? 0x1C00 : 0x1800);
					RenderBackground(y, mapBase, tileBase, true);
				}

				if (objEnable)
				{
					RenderSprites(y);
				}
			}
			else
				SetLine(y, 0xFF, 0xFF, 0xFF);
		}

		protected virtual void RenderBackground(int y, ushort mapBase, ushort tileBase, bool isWindow)
		{
			if (isWindow && y < windowY) return;

			var yTransformed = (byte)(isWindow ? (y - windowY) : (scrollY + y));

			for (var x = (isWindow ? (windowX - 7) : 0); x < 160; x++)
			{
				var xTransformed = (byte)(isWindow ? x : (scrollX + x));

				var mapAddress = mapBase + ((yTransformed >> 3) << 5) + (xTransformed >> 3);
				var tileNumber = vram[mapAddress];

				if (!bgWndTileSelect)
					tileNumber = (byte)(tileNumber ^ 0x80);

				var tileAddress = tileBase + (tileNumber << 4) + ((yTransformed & 7) << 1);

				var ba = (vram[tileAddress + 0] >> (7 - (xTransformed % 8))) & 0b1;
				var bb = (vram[tileAddress + 1] >> (7 - (xTransformed % 8))) & 0b1;
				var c = (byte)((bb << 1) | ba);

				if (c != 0)
					SetScreenUsageFlag(y, x, screenUsageBackground);

				SetPixel(y, x, (byte)((bgPalette >> (c << 1)) & 0x03));
			}
		}

		protected virtual void RenderSprites(int y)
		{
			var objHeight = (objSize ? 16 : 8);
			var numObjDisplayed = 0;

			// TODO more stuffs, priority, etc

			for (var i = 0; i < 40 * 4; i += 4)
			{
				var objY = oam[i + 0] - 16;

				if ((y < objY) || y >= (objY + objHeight)) continue;

				var objX = oam[i + 1] - 8;
				var objTileNumber = oam[i + 2];
				var objAttributes = oam[i + 3];

				var objPrioAboveBg = ((objAttributes >> 7) & 0b1) != 0b1;
				var objFlipY = ((objAttributes >> 6) & 0b1) == 0b1;
				var objFlipX = ((objAttributes >> 5) & 0b1) == 0b1;
				var objPalNumber = ((objAttributes >> 4) & 0b1);

				if (objHeight == 16)
				{
					if (y < (objY + 8)) objTileNumber &= 0xFE;
					else objTileNumber |= 0x01;
				}

				for (var x = 0; x < 8; x++)
				{
					if ((objX + x) < 0 || (objX + x) >= 160) continue;
					if (!objPrioAboveBg && IsScreenUsageFlagSet(y, (objX + x), screenUsageBackground)) continue;

					var xShift = objFlipX ? (x % 8) : (7 - (x % 8));
					var yShift = objFlipY ? (7 - ((y - objY) % 8)) : ((y - objY) % 8);

					var tileAddress = (objTileNumber << 4) + (yShift << 1);

					var pal = (objPalNumber == 0 ? obPalette0 : obPalette1);
					var ba = (vram[tileAddress + 0] >> xShift) & 0b1;
					var bb = (vram[tileAddress + 1] >> xShift) & 0b1;
					var c = (byte)((bb << 1) | ba);

					if (c != 0)
					{
						SetScreenUsageFlag(y, (objX + x), (ushort)(screenUsageSprite | ((i / 4) << 8)));
						SetPixel(y, (objX + x), (byte)((pal >> (c << 1)) & 0x03));
						//SetPixel(y, (objX + x), 0, 0, objTileNumber);
					}
				}

				numObjDisplayed++;
				if (numObjDisplayed >= 10) break;
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

		protected virtual void WriteColorToFramebuffer(byte b, byte g, byte r, int address)
		{
			outputFramebuffer[address + 0] = b;
			outputFramebuffer[address + 1] = g;
			outputFramebuffer[address + 2] = r;
			outputFramebuffer[address + 3] = 0xFF;
		}

		protected virtual void WriteColorToFramebuffer(byte c, int address)
		{
			outputFramebuffer[address + 0] = colorValuesBgr[c & 0x03][0];
			outputFramebuffer[address + 1] = colorValuesBgr[c & 0x03][1];
			outputFramebuffer[address + 2] = colorValuesBgr[c & 0x03][2];
			outputFramebuffer[address + 3] = 0xFF;
		}

		protected virtual void ClearScreenUsage()
		{
			for (int i = 0; i < screenUsage.Length; i++)
				screenUsage[i] = screenUsageEmpty;
		}

		protected ushort GetScreenUsageFlag(int y, int x)
		{
			return screenUsage[(y * 160) + (x % 160)];
		}

		protected bool IsScreenUsageFlagSet(int y, int x, ushort flag)
		{
			return ((GetScreenUsageFlag(y, x) & (flag & 0x00FF)) == (flag & 0x00FF));
		}

		protected void SetScreenUsageFlag(int y, int x, ushort flag)
		{
			screenUsage[(y * 160) + (x % 160)] |= flag;
		}

		protected void ClearScreenUsageFlag(int y, int x, ushort flag)
		{
			screenUsage[(y * 160) + (x % 160)] &= (ushort)~flag;
		}

		//

		public virtual byte ReadVram(ushort address)
		{
			return vram[address & (vram.Length - 1)];
		}

		public virtual void WriteVram(ushort address, byte value)
		{
			vram[address & (vram.Length - 1)] = value;
		}

		public virtual byte ReadOam(ushort address)
		{
			return oam[address - 0xFE00];
		}

		public virtual void WriteOam(ushort address, byte value)
		{
			oam[address - 0xFE00] = value;
		}

		public virtual byte ReadPort(byte port)
		{
			switch (port)
			{
				case 0x40:
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
					return (byte)(
						(lycLyInterrupt ? (1 << 6) : 0) |
						(m2OamInterrupt ? (1 << 5) : 0) |
						(m1VBlankInterrupt ? (1 << 4) : 0) |
						(m0HBlankInterrupt ? (1 << 3) : 0) |
						(coincidenceFlag ? (1 << 2) : 0) |
						((modeNumber & 0b11) << 1));

				case 0x42:
					return scrollY;

				case 0x43:
					return scrollX;

				case 0x44:
					return currentScanline;

				case 0x45:
					return lyCompare;

				case 0x47:
					return bgPalette;

				case 0x48:
					return obPalette0;

				case 0x49:
					return obPalette1;

				case 0x4A:
					return windowY;

				case 0x4B:
					return windowX;
			}

			return 0;
		}

		public virtual void WritePort(byte port, byte value)
		{
			switch (port)
			{
				case 0x40:
					lcdEnable = ((value >> 7) & 0b1) == 0b1;
					wndMapSelect = ((value >> 6) & 0b1) == 0b1;
					wndEnable = ((value >> 5) & 0b1) == 0b1;
					bgWndTileSelect = ((value >> 4) & 0b1) == 0b1;
					bgMapSelect = ((value >> 3) & 0b1) == 0b1;
					objSize = ((value >> 2) & 0b1) == 0b1;
					objEnable = ((value >> 1) & 0b1) == 0b1;
					bgEnable = ((value >> 0) & 0b1) == 0b1;
					break;

				case 0x41:
					lycLyInterrupt = ((value >> 6) & 0b1) == 0b1;
					m2OamInterrupt = ((value >> 5) & 0b1) == 0b1;
					m1VBlankInterrupt = ((value >> 4) & 0b1) == 0b1;
					m0HBlankInterrupt = ((value >> 3) & 0b1) == 0b1;
					break;

				case 0x42:
					scrollY = value;
					break;

				case 0x43:
					scrollX = value;
					break;

				case 0x45:
					lyCompare = value;
					break;

				case 0x46:
					oamDmaStart = value;
					oamDmaCurrentSource = oamDmaStart << 8;
					oamDmaBytesLeft = 0xA0;
					break;

				case 0x47:
					bgPalette = value;
					break;

				case 0x48:
					obPalette0 = value;
					break;

				case 0x49:
					obPalette1 = value;
					break;

				case 0x4A:
					windowY = value;
					break;

				case 0x4B:
					windowX = value;
					break;
			}
		}
	}
}
