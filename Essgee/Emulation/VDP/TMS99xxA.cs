using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Essgee.Emulation.VDP
{
	/* Texas Instruments TMS99xxA family */
	public class TMS99xxA
	{
		public const int NumTotalScanlinesPal = 313;
		public const int NumTotalScanlinesNtsc = 262;

		public const int NumActiveScanlines = 192;
		public const int NumActivePixelsPerScanline = 256;

		public readonly int NumTotalPixelsPerScanline = 342;
		public virtual int NumTotalScanlines => (isPalChip ? NumTotalScanlinesPal : NumTotalScanlinesNtsc);

		protected int numVisibleScanlines;
		protected int topBlankingSize, topBorderSize, verticalActiveDisplaySize, bottomBorderSize, bottomBlankingSize, verticalSyncSize;
		protected int scanlineTopBlanking, scanlineTopBorder, scanlineActiveDisplay, scanlineBottomBorder, scanlineBottomBlanking, scanlineVerticalSync;

		protected int leftBlanking1Size, colorBurstSize, leftBlanking2Size, leftBorderSize, horizontalActiveDisplaySize, rightBorderSize, rightBlankingSize, horizontalSyncSize;
		protected int pixelLeftBlanking1, pixelColorBurst, pixelLeftBlanking2, pixelLeftBorder, pixelActiveDisplay, pixelRightBorder, pixelRightBlanking, pixelHorizontalSync;

		protected int numVisiblePixels;

		public virtual (int X, int Y, int Width, int Height) Viewport => (pixelLeftBorder, scanlineTopBorder, numVisiblePixels, numVisibleScanlines);

		protected const int NumSprites = 32;
		protected const int NumSpritesPerLine = 4;

		protected double clockRate, refreshRate;
		protected bool isPalChip;

		protected byte[] registers, vram;
		protected (int Number, int Y, int X, int Pattern, int Attribute)[][] spriteBuffer;

		protected ushort vramMask16k => 0x3FFF;
		protected ushort vramMask4k => 0x0FFF;

		protected bool isSecondControlWrite;
		protected ushort controlWord;
		protected byte readBuffer;

		protected byte codeRegister => (byte)((controlWord >> 14) & 0x03);
		protected ushort addressRegister
		{
			get { return (ushort)(controlWord & 0x3FFF); }
			set { controlWord = (ushort)((controlWord & 0xC000) | (value & 0x3FFF)); }
		}

		[Flags]
		protected enum StatusFlags : byte
		{
			None = 0,
			SpriteCollision = (1 << 5),
			SpriteOverflow = (1 << 6),
			FrameInterruptPending = (1 << 7)
		}
		protected StatusFlags statusFlags;
		protected bool isSpriteCollision
		{
			get { return ((statusFlags & StatusFlags.SpriteCollision) == StatusFlags.SpriteCollision); }
			set { statusFlags = ((statusFlags & ~StatusFlags.SpriteCollision) | (value ? StatusFlags.SpriteCollision : StatusFlags.None)); }
		}
		protected bool isSpriteOverflow
		{
			get { return ((statusFlags & StatusFlags.SpriteOverflow) == StatusFlags.SpriteOverflow); }
			set { statusFlags = ((statusFlags & ~StatusFlags.SpriteOverflow) | (value ? StatusFlags.SpriteOverflow : StatusFlags.None)); }
		}
		protected bool isFrameInterruptPending
		{
			get { return ((statusFlags & StatusFlags.FrameInterruptPending) == StatusFlags.FrameInterruptPending); }
			set { statusFlags = ((statusFlags & ~StatusFlags.FrameInterruptPending) | (value ? StatusFlags.FrameInterruptPending : StatusFlags.None)); }
		}
		protected bool isFrameInterruptEnabled => BitUtilities.IsBitSet(registers[0x01], 5);

		public InterruptState InterruptLine { get; set; }

		protected int currentScanline;

		protected bool isDisplayBlanked => !BitUtilities.IsBitSet(registers[0x01], 6);

		protected bool is16kVRAMEnabled => BitUtilities.IsBitSet(registers[0x01], 7);

		protected bool isBitM1Set => BitUtilities.IsBitSet(registers[0x01], 4);
		protected bool isBitM2Set => BitUtilities.IsBitSet(registers[0x00], 1);
		protected bool isBitM3Set => BitUtilities.IsBitSet(registers[0x01], 3);

		protected virtual bool isModeGraphics1 => !(isBitM1Set || isBitM2Set || isBitM3Set);
		protected virtual bool isModeText => (isBitM1Set && !(isBitM2Set || isBitM3Set));
		protected virtual bool isModeGraphics2 => (isBitM2Set && !(isBitM1Set || isBitM3Set));
		protected virtual bool isModeMulticolor => (isBitM3Set && !(isBitM1Set || isBitM2Set));

		protected bool isLargeSprites => BitUtilities.IsBitSet(registers[0x01], 1);
		protected bool isZoomedSprites => BitUtilities.IsBitSet(registers[0x01], 0);

		protected virtual ushort nametableBaseAddress => (ushort)((registers[0x02] & 0x0F) << 10);
		protected virtual ushort spriteAttribTableBaseAddress => (ushort)((registers[0x05] & 0x7F) << 7);
		protected virtual ushort spritePatternGenBaseAddress => (ushort)((registers[0x06] & 0x07) << 11);

		protected byte backgroundColor => (byte)(registers[0x07] & 0x0F);
		protected byte textColor => (byte)((registers[0x07] >> 4) & 0x0F);

		/* http://www.smspower.org/Development/Palette */
		readonly byte[][] colorValuesBgra = new byte[][]
		{
			/*              B     G     R     A */
			new byte[] { 0x00, 0x00, 0x00, 0xFF },  /* Transparent */
			new byte[] { 0x00, 0x00, 0x00, 0xFF },  /* Black */
			new byte[] { 0x3B, 0xB7, 0x47, 0xFF },  /* Medium green */
			new byte[] { 0x6F, 0xCF, 0x7C, 0xFF },  /* Light green */
			new byte[] { 0xFF, 0x4E, 0x5D, 0xFF },  /* Dark blue */
			new byte[] { 0xFF, 0x72, 0x80, 0xFF },  /* Light blue */
			new byte[] { 0x47, 0x62, 0xB6, 0xFF },  /* Dark red */
			new byte[] { 0xED, 0xC8, 0x5D, 0xFF },  /* Cyan */
			new byte[] { 0x48, 0x6B, 0xD7, 0xFF },  /* Medium red */
			new byte[] { 0x6C, 0x8F, 0xFB, 0xFF },  /* Light red */
			new byte[] { 0x41, 0xCD, 0xC3, 0xFF },  /* Dark yellow */
			new byte[] { 0x76, 0xDA, 0xD3, 0xFF },  /* Light yellow */
			new byte[] { 0x2F, 0x9F, 0x3E, 0xFF },  /* Dark green */
			new byte[] { 0xC7, 0x64, 0xB6, 0xFF },  /* Magenta */
			new byte[] { 0xCC, 0xCC, 0xCC, 0xFF },  /* Gray */
			new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }   /* White */
		};

		protected const byte screenUsageEmpty = 0;
		protected const byte screenUsageSprite = (1 << 0);
		protected const byte screenUsageBackground = (1 << 1);
		protected byte[] screenUsage;

		protected int cycleCount;
		protected byte[] outputFramebuffer;

		protected int clockCyclesPerLine;

		public bool EnableBackgrounds { get; set; }
		public bool EnableSprites { get; set; }

		public byte[] OutputFramebuffer => outputFramebuffer.Clone() as byte[];

		public TMS99xxA()
		{
			registers = new byte[0x08];
			vram = new byte[0x4000];

			spriteBuffer = new (int Number, int Y, int X, int Pattern, int Attribute)[NumActiveScanlines][];
			for (int i = 0; i < spriteBuffer.Length; i++) spriteBuffer[i] = new (int Number, int Y, int X, int Pattern, int Attribute)[NumSpritesPerLine];

			EnableBackgrounds = true;
			EnableSprites = true;
		}

		public virtual void Startup()
		{
			Reset();

			Debug.Assert(clockRate != 0.0, "Clock rate is zero", "{0} clock rate is not configured", GetType().FullName);
			Debug.Assert(refreshRate != 0.0, "Refresh rate is zero", "{0} refresh rate is not configured", GetType().FullName);
		}

		public virtual void Reset()
		{
			for (int i = 0; i < registers.Length; i++) registers[i] = 0;
			for (int i = 0; i < vram.Length; i++) vram[i] = 0;

			for (int i = 0; i < spriteBuffer.Length; i++)
				for (int j = 0; j < spriteBuffer[i].Length; j++)
					spriteBuffer[i][j] = (-1, 0, 0, 0, 0);

			isSecondControlWrite = false;
			controlWord = 0x0000;
			readBuffer = 0;

			statusFlags = StatusFlags.None;

			// TODO/FIXME: begin on random scanline (i.e. Vcounter for SMS/GG) on reset, http://www.smspower.org/forums/post62735#62735
			currentScanline = new Random().Next(scanlineTopBorder, scanlineBottomBlanking);

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
			isPalChip = (refreshRate <= 50.0);

			ReconfigureTimings();
		}

		protected virtual void ReconfigureTimings()
		{
			/* Calculate cycles/line */
			clockCyclesPerLine = (int)Math.Round((clockRate / refreshRate) / NumTotalScanlines);

			/* Create arrays */
			screenUsage = new byte[NumTotalPixelsPerScanline * NumTotalScanlines];
			outputFramebuffer = new byte[(NumTotalPixelsPerScanline * NumTotalScanlines) * 4];

			/* Scanline parameters */
			if (!isPalChip)
			{
				topBlankingSize = 13;
				topBorderSize = 27;
				verticalActiveDisplaySize = 192;
				bottomBorderSize = 24;
				bottomBlankingSize = 3;
				verticalSyncSize = 3;
			}
			else
			{
				topBlankingSize = 13;
				topBorderSize = 54;
				verticalActiveDisplaySize = 192;
				bottomBorderSize = 48;
				bottomBlankingSize = 3;
				verticalSyncSize = 3;
			}

			scanlineTopBlanking = 0;
			scanlineTopBorder = (scanlineTopBlanking + topBlankingSize);
			scanlineActiveDisplay = (scanlineTopBorder + topBorderSize);
			scanlineBottomBorder = (scanlineActiveDisplay + verticalActiveDisplaySize);
			scanlineBottomBlanking = (scanlineBottomBorder + bottomBorderSize);
			scanlineVerticalSync = (scanlineBottomBlanking + bottomBlankingSize);

			numVisibleScanlines = (topBorderSize + verticalActiveDisplaySize + bottomBorderSize);

			/* Pixel parameters */
			leftBlanking1Size = 2;
			colorBurstSize = 14;
			leftBlanking2Size = 8;
			leftBorderSize = 13;
			horizontalActiveDisplaySize = 256;
			rightBorderSize = 15;
			rightBlankingSize = 8;
			horizontalSyncSize = 26;

			pixelLeftBlanking1 = 0;
			pixelColorBurst = (pixelLeftBlanking1 + leftBlanking1Size);
			pixelLeftBlanking2 = (pixelColorBurst + colorBurstSize);
			pixelLeftBorder = (pixelLeftBlanking2 + leftBlanking2Size);
			pixelActiveDisplay = (pixelLeftBorder + leftBorderSize);
			pixelRightBorder = (pixelActiveDisplay + horizontalActiveDisplaySize);
			pixelRightBlanking = (pixelRightBorder + rightBorderSize);
			pixelHorizontalSync = (pixelRightBlanking + rightBlankingSize);

			numVisiblePixels = (leftBorderSize + horizontalActiveDisplaySize + rightBorderSize);
		}

		public virtual bool Step(int clockCyclesInStep)
		{
			bool drawScreen = false;

			InterruptLine = ((isFrameInterruptEnabled && isFrameInterruptPending) ? InterruptState.Assert : InterruptState.Clear);

			cycleCount += clockCyclesInStep;

			if (cycleCount >= clockCyclesPerLine)
			{
				CheckSpriteOverflow(currentScanline);

				RenderLine(currentScanline);

				if (currentScanline == (scanlineBottomBorder + 1))
					isFrameInterruptPending = true;

				currentScanline++;
				if (currentScanline == NumTotalScanlines)
				{
					currentScanline = 0;
					ClearScreenUsage();

					drawScreen = true;
				}

				ParseSpriteTable(currentScanline);

				cycleCount -= clockCyclesPerLine;
				if (cycleCount <= -clockCyclesPerLine) cycleCount = 0;
			}

			return drawScreen;
		}

		protected virtual void ClearScreenUsage()
		{
			for (int i = 0; i < screenUsage.Length; i++)
				screenUsage[i] = screenUsageEmpty;
		}

		protected virtual void RenderLine(int y)
		{
			RenderBorders(y);

			if (y >= scanlineTopBlanking && y < scanlineTopBorder) SetLine(y, 0x10, 0x10, 0x10);
			else if (y >= scanlineTopBorder && y < scanlineActiveDisplay) SetLine(y, backgroundColor);
			else if (y >= scanlineActiveDisplay && y < scanlineBottomBorder)
			{
				if (EnableBackgrounds)
				{
					if (isModeGraphics1)
						RenderLineGraphics1Background(y);
					else if (isModeGraphics2)
						RenderLineGraphics2Background(y);
					else if (isModeMulticolor)
						throw new Exception("TMS99xxA: Multicolor screenmode not implemented");
					else if (isModeText)
						RenderLineTextBackground(y);
				}
				else
					SetLine(y, 0x00, 0x00, 0x00);

				if (EnableSprites)
				{
					if (!isModeText)
						RenderLineSprites(y);
				}
			}
			else if (y >= scanlineBottomBorder && y < scanlineBottomBlanking) SetLine(y, backgroundColor);
			else if (y >= scanlineBottomBlanking && y < scanlineVerticalSync) SetLine(y, 0x10, 0x10, 0x10);
			else if (y >= scanlineVerticalSync && y < NumTotalScanlines) SetLine(y, 0x00, 0x00, 0x00);
		}

		protected virtual void RenderBorders(int y)
		{
			for (int x = pixelLeftBlanking1; x < pixelColorBurst; x++) SetPixel(y, x, backgroundColor);
			for (int x = pixelColorBurst; x < pixelLeftBlanking2; x++) SetPixel(y, x, 0x00, 0x20, 0x40);
			for (int x = pixelLeftBlanking2; x < pixelLeftBorder; x++) SetPixel(y, x, 0x10, 0x10, 0x10);
			for (int x = pixelLeftBorder; x < pixelActiveDisplay; x++) SetPixel(y, x, backgroundColor);
			for (int x = pixelRightBorder; x < pixelRightBlanking; x++) SetPixel(y, x, backgroundColor);
			for (int x = pixelRightBlanking; x < pixelHorizontalSync; x++) SetPixel(y, x, 0x10, 0x10, 0x10);
			for (int x = pixelHorizontalSync; x < NumTotalPixelsPerScanline; x++) SetPixel(y, x, 0x00, 0x00, 0x00);
		}

		protected void SetLine(int y, ushort colorValue)
		{
			for (int x = 0; x < NumTotalPixelsPerScanline; x++)
				SetPixel(y, x, colorValue);
		}

		protected void SetLine(int y, byte b, byte g, byte r)
		{
			for (int x = 0; x < NumTotalPixelsPerScanline; x++)
				SetPixel(y, x, b, g, r);
		}

		protected void SetPixel(int y, int x, ushort colorValue)
		{
			WriteColorToFramebuffer(colorValue, ((y * NumTotalPixelsPerScanline) + (x % NumTotalPixelsPerScanline)) * 4);
		}

		protected void SetPixel(int y, int x, byte b, byte g, byte r)
		{
			WriteColorToFramebuffer(b, g, r, ((y * NumTotalPixelsPerScanline) + (x % NumTotalPixelsPerScanline)) * 4);
		}

		protected byte GetScreenUsageFlag(int y, int x)
		{
			return screenUsage[(y * NumTotalPixelsPerScanline) + (x % NumTotalPixelsPerScanline)];
		}

		protected bool IsScreenUsageFlagSet(int y, int x, byte flag)
		{
			return ((GetScreenUsageFlag(y, x) & flag) == flag);
		}

		protected void SetScreenUsageFlag(int y, int x, byte flag)
		{
			screenUsage[(y * NumTotalPixelsPerScanline) + (x % NumTotalPixelsPerScanline)] |= flag;
		}

		protected void ClearScreenUsageFlag(int y, int x, byte flag)
		{
			screenUsage[(y * NumTotalPixelsPerScanline) + (x % NumTotalPixelsPerScanline)] &= (byte)~flag;
		}

		protected void RenderLineGraphics1Background(int y)
		{
			/* Determine coordinates in active display */
			int activeDisplayY = (y - scanlineActiveDisplay);

			int numColumns = 32;
			ushort patternGeneratorBaseAddress = (ushort)((registers[0x04] & 0x07) << 11);
			ushort colorTableBaseAddress = (ushort)(registers[0x03] << 6);

			for (int column = 0; column < numColumns; column++)
			{
				/* Calculate nametable address, fetch character number */
				ushort nametableAddress = (ushort)(nametableBaseAddress + ((activeDisplayY / 8) * numColumns) + column);
				byte characterNumber = ReadVram(nametableAddress);

				/* Fetch pixel and color data for current pixel line (1 byte, 8 pixels) */
				byte pixelLineData = ReadVram((ushort)(patternGeneratorBaseAddress + (characterNumber * 8) + (activeDisplayY % 8)));
				byte pixelLineColor = ReadVram((ushort)(colorTableBaseAddress + (characterNumber / 8)));

				/* Extract background and foreground color indices */
				byte[] colorIndicesBackgroundForeground = new byte[2];
				colorIndicesBackgroundForeground[0] = (byte)(pixelLineColor & 0x0F);
				colorIndicesBackgroundForeground[1] = (byte)(pixelLineColor >> 4);

				for (int pixel = 0; pixel < 8; pixel++)
				{
					/* Fetch color index for current pixel (bit clear means background, bit set means foreground color) */
					byte c = colorIndicesBackgroundForeground[((pixelLineData >> (7 - pixel)) & 0x01)];
					/* Color index 0 is transparent, use background color */
					if (c == 0 || isDisplayBlanked) c = backgroundColor;

					/* Record screen usage, write to framebuffer */
					int x = pixelActiveDisplay + (column * 8) + pixel;
					if (GetScreenUsageFlag(y, x) == screenUsageEmpty)
					{
						SetPixel(y, x, c);
						SetScreenUsageFlag(y, x, screenUsageBackground);
					}
				}
			}
		}

		protected void RenderLineGraphics2Background(int y)
		{
			/* Determine coordinates in active display */
			int activeDisplayY = (y - scanlineActiveDisplay);

			int numColumns = (NumActivePixelsPerScanline / 8);

			/* Calculate some base addresses */
			ushort patternGeneratorBaseAddress = (ushort)((registers[0x04] & 0x04) << 11);
			ushort colorTableBaseAddress = (ushort)((registers[0x03] & 0x80) << 6);

			for (int column = 0; column < numColumns; column++)
			{
				/* Calculate nametable address */
				ushort nametableAddress = (ushort)(nametableBaseAddress + ((activeDisplayY / 8) * numColumns) + column);

				/* Calculate character number and masks */
				ushort characterNumber = (ushort)(((activeDisplayY / 64) << 8) | ReadVram(nametableAddress));
				ushort characterNumberDataMask = (ushort)(((registers[0x04] & 0x03) << 8) | 0xFF);
				ushort characterNumberColorMask = (ushort)(((registers[0x03] & 0x7F) << 3) | 0x07);

				/* Fetch pixel and color data for current pixel line (1 byte, 8 pixels) */
				byte pixelLineData = ReadVram((ushort)(patternGeneratorBaseAddress + ((characterNumber & characterNumberDataMask) * 8) + (activeDisplayY % 8)));
				byte pixelLineColor = ReadVram((ushort)(colorTableBaseAddress + ((characterNumber & characterNumberColorMask) * 8) + (activeDisplayY % 8)));

				/* Extract background and foreground color indices */
				byte[] colorIndicesBackgroundForeground = new byte[2];
				colorIndicesBackgroundForeground[0] = (byte)(pixelLineColor & 0x0F);
				colorIndicesBackgroundForeground[1] = (byte)(pixelLineColor >> 4);

				for (int pixel = 0; pixel < 8; pixel++)
				{
					/* Fetch color index for current pixel (bit clear means background, bit set means foreground color) */
					byte c = colorIndicesBackgroundForeground[((pixelLineData >> (7 - pixel)) & 0x01)];
					/* Color index 0 is transparent, use background color */
					if (c == 0 || isDisplayBlanked) c = backgroundColor;

					/* Record screen usage, write to framebuffer */
					int x = pixelActiveDisplay + (column * 8) + pixel;
					if (GetScreenUsageFlag(y, x) == screenUsageEmpty)
					{
						SetPixel(y, x, c);
						SetScreenUsageFlag(y, x, screenUsageBackground);
					}
				}
			}
		}

		protected void RenderLineTextBackground(int y)
		{
			/* Determine coordinates in active display */
			int activeDisplayY = (y - scanlineActiveDisplay);

			int numColumns = 40;
			ushort patternGeneratorBaseAddress = (ushort)((registers[0x04] & 0x07) << 11);

			/* Get background and text color indices */
			byte[] colorIndicesBackgroundForeground = new byte[2];
			colorIndicesBackgroundForeground[0] = backgroundColor;
			colorIndicesBackgroundForeground[1] = textColor;

			/* Draw left and right 8px borders */
			for (int pixel = 0; pixel < 8; pixel++)
			{
				int x = pixelActiveDisplay + pixel;
				SetPixel(y, x + 0, backgroundColor);
				SetPixel(y, x + 8 + (numColumns * 6), backgroundColor);
			}

			/* Draw text columns */
			for (int column = 0; column < numColumns; column++)
			{
				/* Calculate nametable address, fetch character number */
				ushort nametableAddress = (ushort)(nametableBaseAddress + ((activeDisplayY / 8) * numColumns) + column);
				byte characterNumber = ReadVram(nametableAddress);

				/* Fetch pixel data for current pixel line (1 byte, 8 pixels) */
				byte pixelLineData = ReadVram((ushort)(patternGeneratorBaseAddress + (characterNumber * 8) + (activeDisplayY % 8)));

				for (int pixel = 0; pixel < 6; pixel++)
				{
					/* Fetch color index for current pixel (bit clear means background, bit set means text color) */
					byte c = (isDisplayBlanked ? backgroundColor : colorIndicesBackgroundForeground[((pixelLineData >> (7 - pixel)) & 0x01)]);

					/* Record screen usage, write to framebuffer */
					int x = pixelActiveDisplay + 8 + (column * 6) + pixel;
					if (GetScreenUsageFlag(y, x) == screenUsageEmpty)
					{
						SetPixel(y, x, c);
						SetScreenUsageFlag(y, x, screenUsageBackground);
					}
				}
			}
		}

		protected virtual void WriteSpriteNumberToStatus(int spriteNumber)
		{
			statusFlags &= (StatusFlags.FrameInterruptPending | StatusFlags.SpriteOverflow | StatusFlags.SpriteCollision);
			statusFlags |= (StatusFlags)spriteNumber;
		}

		protected virtual void CheckSpriteOverflow(int y)
		{
			/* Ensure current scanline is within active display */
			if (y >= scanlineActiveDisplay && y < scanlineBottomBorder)
			{
				int activeDisplayY = (y - scanlineActiveDisplay);

				/* If last sprite in buffer is valid, sprite overflow occured */
				int lastSpriteInBuffer = spriteBuffer[activeDisplayY][NumSpritesPerLine - 1].Number;
				if (lastSpriteInBuffer != -1)
				{
					isSpriteOverflow = true;

					/* Store sprite number in status register */
					WriteSpriteNumberToStatus(lastSpriteInBuffer);
				}
			}
		}

		protected virtual void ParseSpriteTable(int y)
		{
			if (y < scanlineActiveDisplay || y >= scanlineBottomBorder) return;

			/* Determine coordinates in active display */
			int activeDisplayY = (y - scanlineActiveDisplay);

			/* Clear sprite list for current line */
			for (int i = 0; i < spriteBuffer[activeDisplayY].Length; i++) spriteBuffer[activeDisplayY][i] = (-1, 0, 0, 0, 0);

			/* Determine sprite size & get zoomed sprites adjustment */
			int zoomShift = (isZoomedSprites ? 1 : 0);
			int spriteHeight = ((isLargeSprites ? 16 : 8) << zoomShift);

			int numValidSprites = 0;
			for (int sprite = 0; sprite < NumSprites; sprite++)
			{
				int yCoordinate = ReadVram((ushort)(spriteAttribTableBaseAddress + (sprite * 4)));

				/* Ignore following if Y coord is 208 */
				if (yCoordinate == 208)
				{
					/* Store first "illegal sprite" number in status register */
					WriteSpriteNumberToStatus(sprite);
					return;
				}

				/* Modify Y coord as needed */
				yCoordinate++;
				if (yCoordinate > NumActiveScanlines + 32) yCoordinate -= 256;

				/* Ignore this sprite if on incorrect lines */
				if (activeDisplayY < yCoordinate || activeDisplayY >= (yCoordinate + spriteHeight)) continue;

				/* Check if maximum number of sprites per line is reached */
				numValidSprites++;
				if (numValidSprites > NumSpritesPerLine) return;

				/* Mark sprite for rendering */
				int xCoordinate = ReadVram((ushort)(spriteAttribTableBaseAddress + (sprite * 4) + 1));
				int patternNumber = ReadVram((ushort)(spriteAttribTableBaseAddress + (sprite * 4) + 2));
				int attributes = ReadVram((ushort)(spriteAttribTableBaseAddress + (sprite * 4) + 3));

				spriteBuffer[activeDisplayY][numValidSprites - 1] = (sprite, yCoordinate, xCoordinate, patternNumber, attributes);
			}

			/* Because we didn't bow out before already, store total number of sprites in status register */
			WriteSpriteNumberToStatus(NumSprites - 1);
		}

		protected void RenderLineSprites(int y)
		{
			if (y < scanlineActiveDisplay || y >= scanlineBottomBorder) return;

			/* Determine coordinates in active display */
			int activeDisplayY = (y - scanlineActiveDisplay);

			/* Determine sprite size & get zoomed sprites adjustment */
			int spriteSize = (isLargeSprites ? 16 : 8);
			int zoomShift = (isZoomedSprites ? 1 : 0);
			int numSpritePixels = (spriteSize << zoomShift);

			foreach (var sprite in spriteBuffer[activeDisplayY])
			{
				if (sprite.Number == -1) continue;

				if (!isDisplayBlanked)
				{
					int yCoordinate = sprite.Y;
					int xCoordinate = sprite.X;
					int patternNumber = sprite.Pattern;
					int attributes = sprite.Attribute;

					/* Fetch sprite information, extract attributes */
					bool earlyClock = ((attributes & 0x80) == 0x80);
					int spriteColor = (attributes & 0x0F);

					/* Adjust according to registers/attributes */
					if (earlyClock) xCoordinate -= 32;
					if (isLargeSprites) patternNumber &= 0xFC;

					for (int pixel = 0; pixel < numSpritePixels; pixel++)
					{
						/* Check if sprite is outside active display, else continue to next sprite */
						if ((xCoordinate + pixel) < 0 || (xCoordinate + pixel) >= NumActivePixelsPerScanline) continue;

						/* Determine coordinate inside sprite */
						int inSpriteXCoord = (pixel >> zoomShift) % spriteSize;
						int inSpriteYCoord = ((activeDisplayY - yCoordinate) >> zoomShift) % spriteSize;

						/* Calculate address and fetch pixel data */
						ushort spritePatternAddress = spritePatternGenBaseAddress;
						spritePatternAddress += (ushort)(patternNumber << 3);
						spritePatternAddress += (ushort)inSpriteYCoord;
						if (inSpriteXCoord >= 8) spritePatternAddress += 16;

						byte pixelLineData = ReadVram(spritePatternAddress);

						/* Check if pixel from pattern needs to be drawn, else continue to next sprite */
						if (((pixelLineData >> (7 - (inSpriteXCoord % 8))) & 0x01) == 0x00) continue;

						int x = pixelActiveDisplay + (xCoordinate + pixel);
						if (IsScreenUsageFlagSet(y, x, screenUsageSprite))
						{
							/* If sprite was already at this location, set sprite collision flag */
							isSpriteCollision = true;
						}
						else
						{
							/* If color isn't transparent, draw pixel to framebuffer */
							if (spriteColor != 0)
								SetPixel(y, x, (ushort)spriteColor);
						}

						/* Note that there is a sprite here regardless */
						SetScreenUsageFlag(y, x, screenUsageSprite);
					}
				}
			}
		}

		protected virtual void WriteColorToFramebuffer(byte b, byte g, byte r, int address)
		{
			outputFramebuffer[address + 0] = b;
			outputFramebuffer[address + 1] = g;
			outputFramebuffer[address + 2] = r;
			outputFramebuffer[address + 3] = 0xFF;
		}

		protected virtual void WriteColorToFramebuffer(ushort colorValue, int address)
		{
			outputFramebuffer[address + 0] = colorValuesBgra[colorValue & 0x0F][0];
			outputFramebuffer[address + 1] = colorValuesBgra[colorValue & 0x0F][1];
			outputFramebuffer[address + 2] = colorValuesBgra[colorValue & 0x0F][2];
			outputFramebuffer[address + 3] = colorValuesBgra[colorValue & 0x0F][3];
		}

		protected virtual byte ReadVram(ushort address)
		{
			if (is16kVRAMEnabled)
				return vram[address & vramMask16k];
			else
				return vram[address & vramMask4k];
		}

		protected virtual void WriteVram(ushort address, byte value)
		{
			if (is16kVRAMEnabled)
				vram[address & vramMask16k] = value;
			else
				vram[address & vramMask4k] = value;
		}

		protected virtual byte ReadDataPort()
		{
			isSecondControlWrite = false;
			statusFlags = StatusFlags.None;

			byte data = readBuffer;
			readBuffer = ReadVram(addressRegister);
			addressRegister++;

			return data;
		}

		protected virtual byte ReadControlPort()
		{
			byte statusCurrent = (byte)statusFlags;

			statusFlags = StatusFlags.None;
			isSecondControlWrite = false;

			InterruptLine = InterruptState.Clear;

			return statusCurrent;
		}

		public virtual byte ReadPort(byte port)
		{
			if ((port & 0x01) == 0x00)
				return ReadDataPort();
			else
				return ReadControlPort();
		}

		protected virtual void WriteDataPort(byte value)
		{
			isSecondControlWrite = false;

			readBuffer = value;
			WriteVram(addressRegister, value);
			addressRegister++;
		}

		protected virtual void WriteControlPort(byte value)
		{
			if (!isSecondControlWrite)
				controlWord = (ushort)((controlWord & 0xFF00) | (value << 0));
			else
			{
				controlWord = (ushort)((controlWord & 0x00FF) | (value << 8));

				switch (codeRegister)
				{
					case 0x00: readBuffer = ReadVram(addressRegister); addressRegister++; break;
					case 0x01: break;
					case 0x02: WriteRegister((byte)((controlWord >> 8) & 0x0F), (byte)(controlWord & 0x00FF)); break;
					case 0x03: break;
				}
			}

			isSecondControlWrite = !isSecondControlWrite;
		}

		public virtual void WritePort(byte port, byte value)
		{
			if ((port & 0x01) == 0x00)
				WriteDataPort(value);
			else
				WriteControlPort(value);
		}

		protected virtual void WriteRegister(byte register, byte value)
		{
			// TODO: confirm register mirroring
			registers[register & 0x07] = value;
		}
	}
}
