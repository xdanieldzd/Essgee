using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

using Essgee.Emulation.Configuration;
using Essgee.Emulation.CPU;
using Essgee.Emulation.Video;
using Essgee.Emulation.Audio;
using Essgee.Emulation.Cartridges;
using Essgee.Emulation.Cartridges.Nintendo;
using Essgee.Emulation.Peripherals.Serial;
using Essgee.EventArguments;
using Essgee.Exceptions;
using Essgee.Utilities;

namespace Essgee.Emulation.Machines
{
	[MachineIndex(5)]
	public class GameBoy : IMachine
	{
		const double masterClock = 4194304;
		const double refreshRate = 59.727500569606;

		const int wramSize = 8 * 1024;
		const int hramSize = 0x7F;

		readonly int[] timerValues = { 1024, 16, 64, 256 };

		//

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

		public event EventHandler<GetGameMetadataEventArgs> GetGameMetadata;
		protected virtual void OnGetGameMetadata(GetGameMetadataEventArgs e) { GetGameMetadata?.Invoke(this, e); }

		public string ManufacturerName => "Nintendo";
		public string ModelName => "Game Boy";
		public string DatFilename => "Nintendo - Game Boy.dat";
		public (string Extension, string Description) FileFilter => (".gb;.gbc", "Game Boy ROMs");
		public bool HasBootstrap => true;
		public double RefreshRate => refreshRate;
		public double PixelAspectRatio => 1.0;

		byte[] bootstrap;
		ICartridge cartridge;
		byte[] wram, hram;
		byte ie;
		SM83 cpu;
		DMGVideo video;
		DMGAudio audio;
		ISerialDevice serialDevice;

		// FF00 - P1/JOYP
		byte joypadRegister;

		// FF01 - SB
		byte serialData;
		// FF02 - SC
		bool serialUseInternalClock, serialTransferInProgress;

		// FF04 - DIV
		byte divider;

		// FF05 - TIMA
		byte timerCounter;

		// FF06 - TMA
		byte timerModulo;

		// FF07 - TAC
		bool timerRunning;
		byte timerInputClock;

		// FF0F - IF
		bool irqVBlank, irqLCDCStatus, irqTimerOverflow, irqSerialIO, irqKeypad;

		// FF50
		bool bootstrapDisabled;

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

		public enum SerialDevices
		{
			[Description("None")]
			None = 0,
			[Description("Game Boy Printer")]
			GBPrinter = 1
		}

		JoypadInputs inputsPressed;

		int serialBitsCounter;
		int dividerCycles, timerCycles, serialCycles;

		int currentMasterClockCyclesInFrame, totalMasterClockCyclesInFrame;

		Configuration.GameBoy configuration;

		public GameBoy() { }

		public void Initialize()
		{
			bootstrap = null;
			cartridge = null;

			wram = new byte[wramSize];
			hram = new byte[hramSize];
			cpu = new SM83(ReadMemory, WriteMemory);
			video = new DMGVideo(ReadMemory, cpu.RequestInterrupt);
			audio = new DMGAudio();

			video.EndOfScanline += (s, e) =>
			{
				PollInputEventArgs pollInputEventArgs = new PollInputEventArgs();
				OnPollInput(pollInputEventArgs);
				ParseInput(pollInputEventArgs);
			};
		}

		public void SetConfiguration(IConfiguration config)
		{
			configuration = (Configuration.GameBoy)config;

			ReconfigureSystem();
		}

		public void SetRuntimeOption(string name, object value)
		{
			//TODO layer toggling etc
		}

		private void ReconfigureSystem()
		{
			video?.SetClockRate(masterClock);
			video?.SetRefreshRate(refreshRate);
			video?.SetRevision(0);

			audio?.SetSampleRate(Program.Configuration.SampleRate);
			audio?.SetOutputChannels(2);
			audio?.SetClockRate(masterClock);
			audio?.SetRefreshRate(refreshRate);

			if (cartridge is GBCameraCartridge camCartridge)
				camCartridge.SetImageSource(configuration.CameraSource, configuration.CameraImageFile);

			currentMasterClockCyclesInFrame = 0;
			totalMasterClockCyclesInFrame = (int)Math.Round(masterClock / refreshRate);

			OnChangeViewport(new ChangeViewportEventArgs(video.Viewport));
		}

		private void LoadBootstrap()
		{
			if (configuration.UseBootstrap)
			{
				var (type, bootstrapRomData) = CartridgeLoader.Load(configuration.BootstrapRom, "Game Boy Bootstrap");
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

			var getGameMetadataEventArgs = new GetGameMetadataEventArgs();
			OnGetGameMetadata(getGameMetadataEventArgs);

			switch (configuration.SerialDevice)
			{
				case SerialDevices.None:
					serialDevice = new DummyDevice();
					break;

				case SerialDevices.GBPrinter:
					serialDevice = new GBPrinter(getGameMetadataEventArgs?.Metadata?.FileName);
					break;

				default:
					throw new EmulationException($"Unknown serial device {configuration.SerialDevice} selected");
			}
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
				cpu.SetRegisterAF(0x01B0);
				cpu.SetRegisterBC(0x0013);
				cpu.SetRegisterDE(0x00D8);
				cpu.SetRegisterHL(0x014D);
			}

			joypadRegister = 0x0F;

			serialData = 0xFF;
			serialUseInternalClock = serialTransferInProgress = false;

			divider = 0;

			timerCounter = 0;

			timerModulo = 0;

			timerRunning = false;
			timerInputClock = 0;

			irqVBlank = irqLCDCStatus = irqTimerOverflow = irqSerialIO = irqKeypad = false;

			bootstrapDisabled = !configuration.UseBootstrap;

			inputsPressed = 0;

			serialBitsCounter = 0;
			dividerCycles = timerCycles = serialCycles = 0;

			OnEmulationReset(EventArgs.Empty);
		}

		public void Shutdown()
		{
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

				default: ramSize = 0; break;
			}
			if (mapperType == null)
			{
				switch (romData[0x0147])
				{
					case 0x00:
						mapperType = typeof(ROMOnlyCartridge);
						break;

					case 0x01:
					case 0x02:
					case 0x03:
						mapperType = typeof(MBC1Cartridge);
						break;

					case 0x05:
					case 0x06:
						mapperType = typeof(MBC2Cartridge);
						ramSize = 0x100;    /* MBC2 internal RAM, 512*4b == 256 bytes */
						break;

					case 0x0F:
					case 0x10:
					case 0x11:
					case 0x12:
					case 0x13:
						mapperType = typeof(MBC3Cartridge);
						break;

					case 0xFC:
						mapperType = typeof(GBCameraCartridge);
						ramSize = 128 * 1024;   // TODO not specified in header??
						break;

					// TODO more mbcs and stuffs

					default:
						throw new EmulationException($"Unimplemented cartridge type 0x{romData[0x0147]:X2}");
				}
			}

			cartridge = (ICartridge)Activator.CreateInstance(mapperType, new object[] { romSize, ramSize });
			cartridge.LoadRom(romData);
			cartridge.LoadRam(ramData);

			if (cartridge is GBCameraCartridge camCartridge)
				camCartridge.SetImageSource(configuration.CameraSource, configuration.CameraImageFile);
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

			currentMasterClockCyclesInFrame -= totalMasterClockCyclesInFrame;
		}

		public void RunStep()
		{
			double currentCpuClockCycles = 0.0;
			currentCpuClockCycles += cpu.Step();

			var cyclesRounded = (int)Math.Round(currentCpuClockCycles);

			video.Step(cyclesRounded);

			HandleTimer(cyclesRounded);
			HandleDivider(cyclesRounded);
			HandleSerialIO(cyclesRounded);

			audio.Step(cyclesRounded);

			cartridge.Step(cyclesRounded);

			currentMasterClockCyclesInFrame += cyclesRounded;
		}

		private void HandleTimer(int clockCyclesInStep)
		{
			if (!timerRunning) return;

			timerCycles += clockCyclesInStep;
			if (timerCycles >= timerValues[timerInputClock])
			{
				timerCounter++;
				if (timerCounter == 0)
				{
					timerCounter = timerModulo;
					cpu.RequestInterrupt(SM83.InterruptSource.TimerOverflow);
				}
				timerCycles -= timerValues[timerInputClock];
			}
		}

		private void HandleDivider(int clockCyclesInStep)
		{
			dividerCycles += clockCyclesInStep;
			if (dividerCycles >= 256)
			{
				divider++;
				dividerCycles -= 256;
			}
		}

		private void HandleSerialIO(int clockCyclesInStep)
		{
			if (serialTransferInProgress)
			{
				serialCycles += clockCyclesInStep;
				if (serialCycles >= 512)
				{
					serialBitsCounter++;
					if (serialBitsCounter == 8)
					{
						/* If using internal clock... */
						if (serialUseInternalClock)
							serialData = serialDevice.DoSlaveTransfer(serialData);

						/* If other devices provides clock... */
						else if (serialDevice.ProvidesClock())
							serialData = serialDevice.DoMasterTransfer(serialData);

						cpu.RequestInterrupt(SM83.InterruptSource.SerialIO);

						serialTransferInProgress = false;

						serialBitsCounter = 0;
					}
					serialCycles -= 512;
				}
			}
		}

		private void ParseInput(PollInputEventArgs eventArgs)
		{
			var keysDown = eventArgs.Keyboard/*.Append(System.Windows.Forms.Keys.Space)*/;

			inputsPressed = 0;

			if (keysDown.Contains(configuration.ControlsRight) && !keysDown.Contains(configuration.ControlsLeft)) inputsPressed |= JoypadInputs.Right;
			if (keysDown.Contains(configuration.ControlsLeft) && !keysDown.Contains(configuration.ControlsRight)) inputsPressed |= JoypadInputs.Left;
			if (keysDown.Contains(configuration.ControlsUp) && !keysDown.Contains(configuration.ControlsDown)) inputsPressed |= JoypadInputs.Up;
			if (keysDown.Contains(configuration.ControlsDown) && !keysDown.Contains(configuration.ControlsUp)) inputsPressed |= JoypadInputs.Down;
			if (keysDown.Contains(configuration.ControlsA)) inputsPressed |= JoypadInputs.A;
			if (keysDown.Contains(configuration.ControlsB)) inputsPressed |= JoypadInputs.B;
			if (keysDown.Contains(configuration.ControlsSelect)) inputsPressed |= JoypadInputs.Select;
			if (keysDown.Contains(configuration.ControlsStart)) inputsPressed |= JoypadInputs.Start;
		}

		private byte ReadMemory(ushort address)
		{
			if (address >= 0x0000 && address <= 0x7FFF)
			{
				if (configuration.UseBootstrap && address < 0x0100 && !bootstrapDisabled)
					return bootstrap[address & 0x00FF];
				else
					return (cartridge != null ? cartridge.Read(address) : (byte)0x00);
			}
			else if (address >= 0x8000 && address <= 0x9FFF)
			{
				return video.ReadVram(address);
			}
			else if (address >= 0xA000 && address <= 0xBFFF)
			{
				return (cartridge != null ? cartridge.Read(address) : (byte)0x00);
			}
			else if (address >= 0xC000 && address <= 0xFDFF)
			{
				return wram[address & (wramSize - 1)];
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
			if ((address & 0xFFF0) == 0xFF40)
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

					case 0xFF50:
						// Bootstrap disable
						return (byte)(
							0xFE |
							(bootstrapDisabled ? (1 << 0) : 0));

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
				wram[address & (wramSize - 1)] = value;
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
			if ((address & 0xFFF0) == 0xFF40)
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
						serialTransferInProgress = (value & (1 << 7)) != 0;

						if (serialTransferInProgress) serialCycles = 0;
						serialBitsCounter = 0;
						break;

					case 0xFF04:
						divider = value;
						break;

					case 0xFF05:
						timerCounter = value;
						break;

					case 0xFF06:
						timerModulo = value;
						break;

					case 0xFF07:
						timerRunning = (value & (1 << 2)) != 0;
						timerInputClock = (byte)(value & 0b11);
						break;

					case 0xFF0F:
						irqVBlank = (value & (1 << 0)) != 0;
						irqLCDCStatus = (value & (1 << 1)) != 0;
						irqTimerOverflow = (value & (1 << 2)) != 0;
						irqSerialIO = (value & (1 << 3)) != 0;
						irqKeypad = (value & (1 << 4)) != 0;
						break;

					case 0xFF50:
						if (!bootstrapDisabled)
							bootstrapDisabled = (value & (1 << 0)) != 0;
						break;
				}
			}
		}
	}
}
