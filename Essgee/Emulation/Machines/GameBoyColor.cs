using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

using Essgee.Emulation.Configuration;
using Essgee.Emulation.CPU;
using Essgee.Emulation.Video.Nintendo;
using Essgee.Emulation.Audio;
using Essgee.Emulation.Cartridges.Nintendo;
using Essgee.Emulation.ExtDevices.Nintendo;
using Essgee.EventArguments;
using Essgee.Exceptions;
using Essgee.Utilities;

namespace Essgee.Emulation.Machines
{
	// TODO: make GB and GBC share ex. ROM loading routine?

	[MachineIndex(6)]
	public class GameBoyColor : IMachine
	{
		const double masterClock = 4194304;
		const double refreshRate = 59.727500569606;

		const int wramSize = 8 * 1024;
		const int hramSize = 0x7F;

		const int serialCycleCountNormal = 512;
		const int serialCycleCountFast = 16;

		public event EventHandler<SendLogMessageEventArgs> SendLogMessage;
		protected virtual void OnSendLogMessage(SendLogMessageEventArgs e) { SendLogMessage?.Invoke(this, e); }

		public event EventHandler<EventArgs> EmulationReset;
		protected virtual void OnEmulationReset(EventArgs e) { EmulationReset?.Invoke(this, e); }

		public event EventHandler<RenderScreenEventArgs> RenderScreen
		{
			add { video.RenderScreen += value; }
			remove { video.RenderScreen -= value; }
		}

		public event EventHandler<SizeScreenEventArgs> SizeScreen
		{
			add { video.SizeScreen += value; }
			remove { video.SizeScreen -= value; }
		}

		public event EventHandler<ChangeViewportEventArgs> ChangeViewport;
		protected virtual void OnChangeViewport(ChangeViewportEventArgs e) { ChangeViewport?.Invoke(this, e); }

		public event EventHandler<PollInputEventArgs> PollInput;
		protected virtual void OnPollInput(PollInputEventArgs e) { PollInput?.Invoke(this, e); }

		public event EventHandler<EnqueueSamplesEventArgs> EnqueueSamples
		{
			add { audio.EnqueueSamples += value; }
			remove { audio.EnqueueSamples -= value; }
		}

		public event EventHandler<SaveExtraDataEventArgs> SaveExtraData;
		protected virtual void OnSaveExtraData(SaveExtraDataEventArgs e) { SaveExtraData?.Invoke(this, e); }

		public event EventHandler<EventArgs> EnableRumble;
		protected virtual void OnEnableRumble(EventArgs e) { EnableRumble?.Invoke(this, EventArgs.Empty); }

		public string ManufacturerName => "Nintendo";
		public string ModelName => "Game Boy Color";
		public string DatFilename => "Nintendo - Game Boy Color.dat";
		public (string Extension, string Description) FileFilter => (".gbc", "Game Boy Color ROMs");
		public bool HasBootstrap => true;
		public double RefreshRate => refreshRate;
		public double PixelAspectRatio => 1.0;

		byte[] bootstrap;
		IGameBoyCartridge cartridge;
		byte[,] wram;
		byte[] hram;
		byte ie;
		SM83CGB cpu;
		CGBVideo video;
		CGBAudio audio;
		ISerialDevice serialDevice;

		// FF00 - P1/JOYP
		byte joypadRegister;

		// FF01 - SB
		byte serialData;
		// FF02 - SC
		bool serialUseInternalClock, serialFastClockSpeed, serialTransferInProgress;

		// FF04 - DIV
		byte divider;
		// FF05 - TIMA
		byte timerCounter;
		//
		ushort clockCycleCount;

		// FF06 - TMA
		byte timerModulo;

		// FF07 - TAC
		bool timerRunning;
		byte timerInputClock;
		//
		bool timerOverflow, timerLoading;

		// FF0F - IF
		bool irqVBlank, irqLCDCStatus, irqTimerOverflow, irqSerialIO, irqKeypad;

		// FF4C

		// FF4D - KEY1
		bool speedIsDouble, speedSwitchPending;

		// FF50
		bool bootstrapDisabled;

		// FF56 - RP
		bool irWriteData, irReadData;
		byte irReadEnable;

		// FF70 - SVBK
		byte wramBank;

		[Flags]
		enum JoypadInputs : byte
		{
			Right = (1 << 0),
			Left = (1 << 1),
			Up = (1 << 2),
			Down = (1 << 3),
			A = (1 << 4),
			B = (1 << 5),
			Select = (1 << 6),
			Start = (1 << 7)
		}

		JoypadInputs inputsPressed;

		int serialBitsCounter;
		int timerCycles, serialCycles;

		int currentMasterClockCyclesInFrame, totalMasterClockCyclesInFrame;

		Configuration.GameBoyColor configuration;

		public GameBoyColor() { }

		public void Initialize()
		{
			bootstrap = null;
			cartridge = null;

			wram = new byte[8, wramSize];
			hram = new byte[hramSize];
			cpu = new SM83CGB(ReadMemory, WriteMemory);
			video = new CGBVideo(ReadMemory, cpu.RequestInterrupt);
			audio = new CGBAudio();

			video.EndOfScanline += (s, e) =>
			{
				PollInputEventArgs pollInputEventArgs = new PollInputEventArgs();
				OnPollInput(pollInputEventArgs);
				ParseInput(pollInputEventArgs);
			};
		}

		public void SetConfiguration(IConfiguration config)
		{
			configuration = (Configuration.GameBoyColor)config;

			ReconfigureSystem();
		}

		public void SetRuntimeOption(string name, object value)
		{
			//TODO layer toggling etc
		}

		private void ReconfigureSystem()
		{
			/* Video */
			video?.SetClockRate(masterClock);
			video?.SetRefreshRate(refreshRate);
			video?.SetRevision(0);

			/* Audio */
			audio?.SetSampleRate(Program.Configuration.SampleRate);
			audio?.SetOutputChannels(2);
			audio?.SetClockRate(masterClock);
			audio?.SetRefreshRate(refreshRate);

			/* Cartridge */
			if (cartridge is GBCameraCartridge camCartridge)
				camCartridge.SetImageSource(configuration.CameraSource, configuration.CameraImageFile);

			/* Serial */
			if (serialDevice != null)
			{
				serialDevice.SaveExtraData -= SaveExtraData;
				serialDevice.Shutdown();
			}

			serialDevice = (ISerialDevice)Activator.CreateInstance(configuration.SerialDevice);
			serialDevice.SaveExtraData += SaveExtraData;
			serialDevice.Initialize();

			/* Misc timing */
			currentMasterClockCyclesInFrame = 0;
			totalMasterClockCyclesInFrame = (int)Math.Round(masterClock / refreshRate);

			/* Announce viewport */
			OnChangeViewport(new ChangeViewportEventArgs(video.Viewport));
		}

		private void LoadBootstrap()
		{
			if (configuration.UseBootstrap)
			{
				var (type, bootstrapRomData) = CartridgeLoader.Load(configuration.BootstrapRom, "Game Boy Color Bootstrap");
				bootstrap = new byte[bootstrapRomData.Length];
				Buffer.BlockCopy(bootstrapRomData, 0, bootstrap, 0, bootstrap.Length);
			}
		}

		public void Startup()
		{
			LoadBootstrap();

			cpu.Startup();
			video.Startup();
			audio.Startup();
		}

		public void Reset()
		{
			cpu.Reset();
			video.Reset();
			audio.Reset();

			if (configuration.UseBootstrap)
			{
				cpu.SetProgramCounter(0x0000);
				cpu.SetStackPointer(0x0000);
			}
			else
			{
				cpu.SetProgramCounter(0x0100);
				cpu.SetStackPointer(0xFFFE);
				cpu.SetRegisterAF(0x11B0);
				cpu.SetRegisterBC(0x0000);
				cpu.SetRegisterDE(0xFF56);
				cpu.SetRegisterHL(0x000D);

				video.WritePort(0x40, 0x91);
				video.WritePort(0x42, 0x00);
				video.WritePort(0x43, 0x00);
				video.WritePort(0x45, 0x00);
				video.WritePort(0x47, 0xFC);
				video.WritePort(0x48, 0xFF);
				video.WritePort(0x49, 0xFF);
				video.WritePort(0x4A, 0x00);
				video.WritePort(0x4B, 0x00);
			}

			joypadRegister = 0x0F;

			serialData = 0xFF;
			serialUseInternalClock = serialFastClockSpeed = serialTransferInProgress = false;

			timerCounter = 0;
			clockCycleCount = 0;

			timerModulo = 0;

			timerRunning = false;
			timerInputClock = 0;

			timerOverflow = timerLoading = false;

			irqVBlank = irqLCDCStatus = irqTimerOverflow = irqSerialIO = irqKeypad = false;

			bootstrapDisabled = !configuration.UseBootstrap;

			wramBank = 0x01;

			inputsPressed = 0;

			serialBitsCounter = 0;
			timerCycles = serialCycles = 0;

			OnEmulationReset(EventArgs.Empty);
		}

		public void Shutdown()
		{
			if (serialDevice != null)
			{
				serialDevice.SaveExtraData -= SaveExtraData;
				serialDevice.Shutdown();
			}

			if (cartridge is MBC5Cartridge mbc5Cartridge)
				mbc5Cartridge.EnableRumble -= EnableRumble;

			cpu?.Shutdown();
			video?.Shutdown();
			audio?.Shutdown();
		}

		public void SetState(Dictionary<string, dynamic> state)
		{
			throw new NotImplementedException();
		}

		public Dictionary<string, dynamic> GetState()
		{
			throw new NotImplementedException();
		}

		public Dictionary<string, dynamic> GetDebugInformation()
		{
			var dict = new Dictionary<string, dynamic>
			{
				{ "CyclesInFrame", currentMasterClockCyclesInFrame },
			};

			return dict;
		}

		public void Load(byte[] romData, byte[] ramData, Type mapperType)
		{
			var romSize = -1;
			switch (romData[0x0148])
			{
				case 0x00: romSize = 32 * 1024; break;
				case 0x01: romSize = 64 * 1024; break;
				case 0x02: romSize = 128 * 1024; break;
				case 0x03: romSize = 256 * 1024; break;
				case 0x04: romSize = 512 * 1024; break;
				case 0x05: romSize = 1024 * 1024; break;
				case 0x06: romSize = 2048 * 1024; break;
				case 0x07: romSize = 4096 * 1024; break;
				case 0x08: romSize = 8192 * 1024; break;
				case 0x52: romSize = 1152 * 1024; break;
				case 0x53: romSize = 1280 * 1024; break;
				case 0x54: romSize = 1536 * 1024; break;

				default: romSize = romData.Length; break;
			}

			var ramSize = -1;
			switch (romData[0x0149])
			{
				case 0x00: ramSize = 0 * 1024; break;
				case 0x01: ramSize = 2 * 1024; break;
				case 0x02: ramSize = 8 * 1024; break;
				case 0x03: ramSize = 32 * 1024; break;
				case 0x04: ramSize = 128 * 1024; break;
				case 0x05: ramSize = 64 * 1024; break;

				default: ramSize = 0; break;
			}

			/* NOTES:
			 *  MBC2 internal RAM is not given in header, 512*4b == 256 bytes 
			 *  GB Camera internal RAM ~seems~ to not be given in header? 128 kbytes
			 */

			var mapperTypeFromHeader = typeof(NoMapperCartridge);
			var hasBattery = false;
			var hasRtc = false;
			var hasRumble = false;
			switch (romData[0x0147])
			{
				case 0x00: mapperType = typeof(NoMapperCartridge); break;
				case 0x01: mapperType = typeof(MBC1Cartridge); break;
				case 0x02: mapperType = typeof(MBC1Cartridge); break;
				case 0x03: mapperType = typeof(MBC1Cartridge); hasBattery = true; break;
				case 0x05: mapperType = typeof(MBC2Cartridge); ramSize = 0x100; break;
				case 0x06: mapperType = typeof(MBC2Cartridge); ramSize = 0x100; hasBattery = true; break;
				case 0x08: mapperType = typeof(NoMapperCartridge); break;
				case 0x09: mapperType = typeof(NoMapperCartridge); hasBattery = true; break;
				// 0B-0D, MMM01
				case 0x0F: mapperType = typeof(MBC3Cartridge); hasBattery = true; hasRtc = true; break;
				case 0x10: mapperType = typeof(MBC3Cartridge); hasBattery = true; hasRtc = true; break;
				case 0x11: mapperType = typeof(MBC3Cartridge); break;
				case 0x12: mapperType = typeof(MBC3Cartridge); break;
				case 0x13: mapperType = typeof(MBC3Cartridge); hasBattery = true; break;
				case 0x19: mapperType = typeof(MBC5Cartridge); break;
				case 0x1A: mapperType = typeof(MBC5Cartridge); break;
				case 0x1B: mapperType = typeof(MBC5Cartridge); hasBattery = true; break;
				case 0x1C: mapperType = typeof(MBC5Cartridge); hasRumble = true; break;
				case 0x1D: mapperType = typeof(MBC5Cartridge); hasRumble = true; break;
				case 0x1E: mapperType = typeof(MBC5Cartridge); hasBattery = true; hasRumble = true; break;
				// 20, MBC6
				// 22, MBC7
				case 0xFC: mapperType = typeof(GBCameraCartridge); ramSize = 128 * 1024; break;
				// FD, BANDAI TAMA5
				// FE, HuC3
				// FF, HuC1

				default: throw new EmulationException($"Unimplemented cartridge type 0x{romData[0x0147]:X2}");
			}

			if (mapperType == null)
				mapperType = mapperTypeFromHeader;

			if (romSize != romData.Length)
			{
				var romSizePadded = 1;
				while (romSizePadded < romData.Length) romSizePadded <<= 1;
				romSize = Math.Max(romSizePadded, romData.Length);
			}

			cartridge = (IGameBoyCartridge)Activator.CreateInstance(mapperType, new object[] { romSize, ramSize });
			cartridge.LoadRom(romData);
			cartridge.LoadRam(ramData);
			cartridge.SetCartridgeConfig(hasBattery, hasRtc, hasRumble);

			if (cartridge is GBCameraCartridge camCartridge)
				camCartridge.SetImageSource(configuration.CameraSource, configuration.CameraImageFile);

			if (cartridge is MBC5Cartridge mbc5Cartridge)
				mbc5Cartridge.EnableRumble += EnableRumble;
		}

		public byte[] GetCartridgeRam()
		{
			return cartridge?.GetRamData();
		}

		public bool IsCartridgeRamSaveNeeded()
		{
			if (cartridge == null) return false;
			return cartridge.IsRamSaveNeeded();
		}

		public virtual void RunFrame()
		{
			while (currentMasterClockCyclesInFrame < totalMasterClockCyclesInFrame)
				RunStep();

			currentMasterClockCyclesInFrame = 0;
		}

		public void RunStep()
		{
			// TODO: verify if current handling of CGB double speed mode is correct! seems to work but is probably wrong??
			// NOTES:
			//  https://github.com/LIJI32/GBVideoPlayer/blob/master/How%20It%20Works.md#hblank-and-sub-hblank-tricks
			//  https://gbdev.io/pandocs/#ff4d-key1-cgb-mode-only-prepare-speed-switch

			var clockCyclesInStep = RunCpuStep();
			var cycleLength = cpu.IsDoubleSpeed ? 2 : 4;

			video.IsDoubleSpeed = cpu.IsDoubleSpeed;

			for (var s = 0; s < clockCyclesInStep / 4; s++)
			{
				HandleTimerOverflow();
				UpdateCycleCounter((ushort)(clockCycleCount + 4));

				HandleSerialIO(4);

				video.Step(cycleLength);
				audio.Step(cycleLength);
				cartridge?.Step(cycleLength);

				currentMasterClockCyclesInFrame += cycleLength;
			}
		}

		private int RunCpuStep()
		{
			if (video.GDMAWaitCycles > 0)
			{
				var cycleLength = cpu.IsDoubleSpeed ? 2 : 4;
				video.GDMAWaitCycles -= cycleLength;
				return cycleLength;
			}
			else
				return cpu.Step();
		}

		private void IncrementTimer()
		{
			timerCounter++;
			if (timerCounter == 0) timerOverflow = true;
		}

		private bool GetTimerBit(byte value, ushort cycles)
		{
			switch (value & 0b11)
			{
				case 0: return (cycles & (1 << 9)) != 0;
				case 1: return (cycles & (1 << 3)) != 0;
				case 2: return (cycles & (1 << 5)) != 0;
				case 3: return (cycles & (1 << 7)) != 0;
				default: throw new EmulationException("Unhandled timer state");
			}
		}

		private void UpdateCycleCounter(ushort value)
		{
			if (timerRunning)
			{
				if (!GetTimerBit(timerInputClock, value) && GetTimerBit(timerInputClock, clockCycleCount))
					IncrementTimer();
			}

			clockCycleCount = value;
			divider = (byte)(clockCycleCount >> 8);
		}

		private void HandleTimerOverflow()
		{
			timerLoading = false;

			if (timerOverflow)
			{
				cpu.RequestInterrupt(SM83.InterruptSource.TimerOverflow);
				timerOverflow = false;

				timerCounter = timerModulo;
				timerLoading = true;
			}
		}

		private void HandleSerialIO(int clockCyclesInStep)
		{
			var cycleCount = (serialFastClockSpeed ? serialCycleCountFast : serialCycleCountNormal) >> (cpu.IsDoubleSpeed ? 1 : 0);

			if (serialTransferInProgress)
			{
				if (serialUseInternalClock)
				{
					/* If using internal clock... */

					serialCycles += clockCyclesInStep;
					if (serialCycles >= cycleCount)
					{
						serialBitsCounter++;
						if (serialBitsCounter == 8)
						{
							serialData = serialDevice.DoSlaveTransfer(serialData);

							cpu.RequestInterrupt(SM83.InterruptSource.SerialIO);
							serialTransferInProgress = false;
							serialBitsCounter = 0;
						}
						serialCycles -= cycleCount;
					}
				}
				else if (serialDevice.ProvidesClock)
				{
					/* If other devices provides clock... */

					serialCycles += clockCyclesInStep;
					if (serialCycles >= cycleCount)
					{
						serialBitsCounter++;
						if (serialBitsCounter == 8)
						{
							serialData = serialDevice.DoMasterTransfer(serialData);

							cpu.RequestInterrupt(SM83.InterruptSource.SerialIO);
							serialTransferInProgress = false;
							serialBitsCounter = 0;
						}
						serialCycles -= cycleCount;
					}
				}
			}
		}

		private void ParseInput(PollInputEventArgs eventArgs)
		{
			inputsPressed = 0;

			if ((eventArgs.Keyboard.Contains(configuration.ControlsRight) && !eventArgs.Keyboard.Contains(configuration.ControlsLeft)) ||
				eventArgs.ControllerState.IsAnyRightDirectionPressed() && !eventArgs.ControllerState.IsAnyLeftDirectionPressed())
				inputsPressed |= JoypadInputs.Right;

			if ((eventArgs.Keyboard.Contains(configuration.ControlsLeft) && !eventArgs.Keyboard.Contains(configuration.ControlsRight)) ||
				eventArgs.ControllerState.IsAnyLeftDirectionPressed() && !eventArgs.ControllerState.IsAnyRightDirectionPressed())
				inputsPressed |= JoypadInputs.Left;

			if ((eventArgs.Keyboard.Contains(configuration.ControlsUp) && !eventArgs.Keyboard.Contains(configuration.ControlsDown)) ||
				eventArgs.ControllerState.IsAnyUpDirectionPressed() && !eventArgs.ControllerState.IsAnyDownDirectionPressed())
				inputsPressed |= JoypadInputs.Up;

			if ((eventArgs.Keyboard.Contains(configuration.ControlsDown) && !eventArgs.Keyboard.Contains(configuration.ControlsUp)) ||
				eventArgs.ControllerState.IsAnyDownDirectionPressed() && !eventArgs.ControllerState.IsAnyUpDirectionPressed())
				inputsPressed |= JoypadInputs.Down;

			if (eventArgs.Keyboard.Contains(configuration.ControlsA) || eventArgs.ControllerState.IsAPressed()) inputsPressed |= JoypadInputs.A;
			if (eventArgs.Keyboard.Contains(configuration.ControlsB) || eventArgs.ControllerState.IsXPressed() || eventArgs.ControllerState.IsBPressed()) inputsPressed |= JoypadInputs.B;
			if (eventArgs.Keyboard.Contains(configuration.ControlsSelect) || eventArgs.ControllerState.IsBackPressed()) inputsPressed |= JoypadInputs.Select;
			if (eventArgs.Keyboard.Contains(configuration.ControlsStart) || eventArgs.ControllerState.IsStartPressed()) inputsPressed |= JoypadInputs.Start;
		}

		private byte ReadMemory(ushort address)
		{
			if (address >= 0x0000 && address <= 0x7FFF)
			{
				if (configuration.UseBootstrap && (address <= 0x00FF || (address >= 0x0200 && address <= 0x08FF)) && !bootstrapDisabled)
					return bootstrap[address];
				else
					return (cartridge != null ? cartridge.Read(address) : (byte)0xFF);
			}
			else if (address >= 0x8000 && address <= 0x9FFF)
			{
				return video.ReadVram(address);
			}
			else if (address >= 0xA000 && address <= 0xBFFF)
			{
				return (cartridge != null ? cartridge.Read(address) : (byte)0xFF);
			}
			else if (address >= 0xC000 && address <= 0xFDFF)
			{
				if ((address & 0x1000) == 0)
					return wram[0, address & 0x0FFF];
				else
					return wram[wramBank, address & 0x0FFF];
			}
			else if (address >= 0xFE00 && address <= 0xFE9F)
			{
				return video.ReadOam(address);
			}
			else if (address >= 0xFF00 && address <= 0xFF7F)
			{
				return ReadIo(address);
			}
			else if (address >= 0xFF80 && address <= 0xFFFE)
			{
				return hram[address - 0xFF80];
			}
			else if (address == 0xFFFF)
			{
				return ie;
			}

			/* Cannot read from address, return 0 */
			return 0x00;
		}

		private byte ReadIo(ushort address)
		{
			if (((address & 0xFFF0) == 0xFF40 && address != 0xFF4C && address != 0xFF4D) || (address >= 0xFF51 && address <= 0xFF55) || (address >= 0xFF68 && address <= 0xFF6B))
				return video.ReadPort((byte)(address & 0xFF));
			else if ((address & 0xFFF0) == 0xFF10 || (address & 0xFFF0) == 0xFF20 || (address & 0xFFF0) == 0xFF30)
				return audio.ReadPort((byte)(address & 0xFF));
			else
			{
				switch (address)
				{
					case 0xFF00:
						// P1/JOYP
						return joypadRegister;

					case 0xFF01:
						// SB
						return serialData;

					case 0xFF02:
						// SC
						return (byte)(
							0x7E |
							(serialUseInternalClock ? (1 << 0) : 0) |
							(serialFastClockSpeed ? (1 << 1) : 0) |
							(serialTransferInProgress ? (1 << 7) : 0));

					case 0xFF04:
						// DIV
						return divider;

					case 0xFF05:
						// TIMA
						return timerCounter;

					case 0xFF06:
						// TMA
						return timerModulo;

					case 0xFF07:
						// TAC
						return (byte)(
							0xF8 |
							(timerRunning ? (1 << 2) : 0) |
							(timerInputClock & 0b11));

					case 0xFF0F:
						// IF
						return (byte)(
							0xE0 |
							(irqVBlank ? (1 << 0) : 0) |
							(irqLCDCStatus ? (1 << 1) : 0) |
							(irqTimerOverflow ? (1 << 2) : 0) |
							(irqSerialIO ? (1 << 3) : 0) |
							(irqKeypad ? (1 << 4) : 0));

					case 0xFF4D:
						// KEY1
						return (byte)(
							0x7E |
							(speedSwitchPending ? (1 << 0) : 0) |
							((speedIsDouble = cpu.IsDoubleSpeed) ? (1 << 7) : 0));

					case 0xFF50:
						// Bootstrap disable
						return (byte)(
							0xFE |
							(bootstrapDisabled ? (1 << 0) : 0));

					case 0xFF70:
						// SVBK
						return (byte)(
							0xF8 |
							(wramBank & 0b111));

					default:
						return 0xFF;// throw new NotImplementedException();
				}
			}
		}

		private void WriteMemory(ushort address, byte value)
		{
			if (address >= 0x0000 && address <= 0x7FFF)
			{
				cartridge?.Write(address, value);
			}
			else if (address >= 0x8000 && address <= 0x9FFF)
			{
				video.WriteVram(address, value);
			}
			else if (address >= 0xA000 && address <= 0xBFFF)
			{
				cartridge?.Write(address, value);
			}
			else if (address >= 0xC000 && address <= 0xFDFF)
			{
				if ((address & 0x1000) == 0)
					wram[0, address & 0x0FFF] = value;
				else
					wram[wramBank, address & 0x0FFF] = value;
			}
			else if (address >= 0xFE00 && address <= 0xFE9F)
			{
				video.WriteOam(address, value);
			}
			else if (address >= 0xFF00 && address <= 0xFF7F)
			{
				WriteIo(address, value);
			}
			else if (address >= 0xFF80 && address <= 0xFFFE)
			{
				hram[address - 0xFF80] = value;
			}
			else if (address == 0xFFFF)
			{
				ie = value;
			}
		}

		private void WriteIo(ushort address, byte value)
		{
			if (((address & 0xFFF0) == 0xFF40 && address != 0xFF4C && address != 0xFF4D) || (address >= 0xFF51 && address <= 0xFF55) || (address >= 0xFF68 && address <= 0xFF6B))
				video.WritePort((byte)(address & 0xFF), value);
			else if ((address & 0xFFF0) == 0xFF10 || (address & 0xFFF0) == 0xFF20 || (address & 0xFFF0) == 0xFF30)
				audio.WritePort((byte)(address & 0xFF), value);
			else
			{
				switch (address)
				{
					case 0xFF00:
						joypadRegister = (byte)((joypadRegister & 0xC0) | (value & 0x30));
						if ((joypadRegister & 0x30) == 0x20)
							joypadRegister |= (byte)(((byte)inputsPressed & 0x0F) ^ 0x0F);
						else if ((joypadRegister & 0x30) == 0x10)
							joypadRegister |= (byte)((((byte)inputsPressed & 0xF0) >> 4) ^ 0x0F);
						else
							joypadRegister = 0xFF;
						break;

					case 0xFF01:
						serialData = value;
						break;

					case 0xFF02:
						serialUseInternalClock = (value & (1 << 0)) != 0;
						serialFastClockSpeed = (value & (1 << 1)) != 0;
						serialTransferInProgress = (value & (1 << 7)) != 0;

						if (serialTransferInProgress) serialCycles = 0;
						serialBitsCounter = 0;
						break;

					case 0xFF04:
						UpdateCycleCounter(0);
						break;

					case 0xFF05:
						if (!timerLoading)
						{
							timerCounter = value;
							timerOverflow = false;
						}
						break;

					case 0xFF06:
						timerModulo = value;
						if (timerLoading)
							timerCounter = value;
						break;

					case 0xFF07:
						{
							var newTimerRunning = (value & (1 << 2)) != 0;
							var newTimerInputClock = (byte)(value & 0b11);

							var oldBit = timerRunning && GetTimerBit(timerInputClock, clockCycleCount);
							var newBit = newTimerRunning && GetTimerBit(newTimerInputClock, clockCycleCount);

							if (oldBit && !newBit)
								IncrementTimer();

							timerRunning = newTimerRunning;
							timerInputClock = newTimerInputClock;
						}
						break;

					case 0xFF0F:
						irqVBlank = (value & (1 << 0)) != 0;
						irqLCDCStatus = (value & (1 << 1)) != 0;
						irqTimerOverflow = (value & (1 << 2)) != 0;
						irqSerialIO = (value & (1 << 3)) != 0;
						irqKeypad = (value & (1 << 4)) != 0;
						break;

					case 0xFF4D:
						speedSwitchPending = (value & (1 << 0)) != 0;
						break;

					case 0xFF50:
						if (!bootstrapDisabled)
							bootstrapDisabled = (value & (1 << 0)) != 0;
						break;

					case 0xFF70:
						wramBank = (byte)(value & 0b111);
						if (wramBank == 0x00) wramBank = 0x01;
						break;
				}
			}
		}
	}
}
