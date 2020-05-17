using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Essgee.Emulation.Configuration;
using Essgee.Emulation.CPU;
using Essgee.Emulation.Video;
using Essgee.Emulation.Audio;
using Essgee.Emulation.Cartridges;
using Essgee.Emulation.Cartridges.Nintendo;
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

		public string ManufacturerName => "Nintendo";
		public string ModelName => "Game Boy";
		public string DatFilename => "Nintendo - Game Boy.dat";
		public (string Extension, string Description) FileFilter => (".gb", "Game Boy ROMs");
		public bool HasBootstrap => true;
		public double RefreshRate => refreshRate;

		byte[] bootstrap;
		ICartridge cartridge;
		byte[] wram, hram;
		byte ie;
		LR35902 cpu;
		DMGVideo video;
		DMGAudio audio;

		[Flags]
		public enum InterruptSource : byte
		{
			VBlank = 0,
			LCDCStatus = 1,
			TimerOverflow = 2,
			SerialIO = 3,
			Keypad = 4
		}

		public delegate void RequestInterruptDelegate(InterruptSource source);

		// FF00
		byte joypadRegister;

		// FF01
		byte serialData;
		// FF02
		bool serialShiftClock, serialClockSpeed, serialTransferStartFlag;

		// FF04
		byte divider;

		// FF05
		byte timerCounter;

		// FF06
		byte timerModulo;

		// FF07
		bool timerRunning;
		byte timerInputClock;

		// FF0F
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

		JoypadInputs inputsPressed;

		int dividerCycles, timerCycles;
		int currentMasterClockCyclesInFrame, totalMasterClockCyclesInFrame;

		Configuration.GameBoy configuration;

		public GameBoy() { }

		public void Initialize()
		{
			bootstrap = null;
			cartridge = null;

			wram = new byte[wramSize];
			hram = new byte[hramSize];
			cpu = new LR35902(ReadMemory, WriteMemory);
			video = new DMGVideo(RequestInterrupt);
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

			inputsPressed = 0;

			dividerCycles = timerCycles = 0;

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

			serialData = 0;
			serialShiftClock = serialClockSpeed = serialTransferStartFlag = false;

			divider = 0;

			timerCounter = 0;

			timerModulo = 0;

			timerRunning = false;
			timerInputClock = 0;

			irqVBlank = irqLCDCStatus = irqTimerOverflow = irqSerialIO = irqKeypad = false;

			bootstrapDisabled = !configuration.UseBootstrap;

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

					// TODO more mbcs and stuffs

					default:
						mapperType = typeof(ROMOnlyCartridge);
						break;
				}
			}

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

			cartridge = (ICartridge)Activator.CreateInstance(mapperType, new object[] { romSize, ramSize });
			cartridge.LoadRom(romData);
			cartridge.LoadRam(ramData);
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

			HandleInterrupts();

			audio.Step(cyclesRounded);

			currentMasterClockCyclesInFrame += cyclesRounded;
		}

		private void RequestInterrupt(InterruptSource source)
		{
			WriteMemory(0xFF0F, (byte)(ReadMemory(0xFF0F) | (1 << (byte)source)));
		}

		private void HandleInterrupts()
		{
			var intEnable = ReadMemory(0xFFFF);
			var intFlag = ReadMemory(0xFF0F);

			if (HandleInterrupt(InterruptSource.VBlank, intEnable, intFlag)) return;
			if (HandleInterrupt(InterruptSource.LCDCStatus, intEnable, intFlag)) return;
			if (HandleInterrupt(InterruptSource.TimerOverflow, intEnable, intFlag)) return;
			if (HandleInterrupt(InterruptSource.SerialIO, intEnable, intFlag)) return;
			if (HandleInterrupt(InterruptSource.Keypad, intEnable, intFlag)) return;
		}

		private bool HandleInterrupt(InterruptSource source, byte intEnable, byte intFlag)
		{
			var sourceBit = (byte)(1 << (byte)source);

			var execute = ((intEnable & sourceBit) == sourceBit) && ((intFlag & sourceBit) == sourceBit);
			if (execute)
			{
				cpu.RequestInterrupt((ushort)(0x0040 + (8 * (int)source)));
				WriteMemory(0xFF0F, (byte)(intFlag & ~sourceBit));

				//Program.Logger?.WriteLine($"--- Interrupt {source}");
			}

			return execute;
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
					RequestInterrupt(InterruptSource.TimerOverflow);
				}
				timerCycles = 0;
			}
		}

		private void HandleDivider(int clockCyclesInStep)
		{
			dividerCycles += clockCyclesInStep;
			if (dividerCycles >= 256)
			{
				dividerCycles = 0;
				divider++;
			}
		}

		private void ParseInput(PollInputEventArgs eventArgs)
		{
			var keysDown = eventArgs.Keyboard/*.Append(System.Windows.Forms.Keys.Space)*/;

			inputsPressed = 0;

			if (keysDown.Contains(configuration.ControlsRight)) inputsPressed |= JoypadInputs.Right;
			if (keysDown.Contains(configuration.ControlsLeft)) inputsPressed |= JoypadInputs.Left;
			if (keysDown.Contains(configuration.ControlsUp)) inputsPressed |= JoypadInputs.Up;
			if (keysDown.Contains(configuration.ControlsDown)) inputsPressed |= JoypadInputs.Down;
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
						if ((joypadRegister & 0x30) == 0x20)
							return (byte)((joypadRegister & 0x00) | (((byte)inputsPressed & 0x0F) ^ 0x0F));
						else if ((joypadRegister & 0x30) == 0x10)
							return (byte)((joypadRegister & 0x00) | ((((byte)inputsPressed & 0xF0) >> 4) ^ 0x0F));
						else
							return joypadRegister;

					case 0xFF01:
						return serialData;

					case 0xFF02:
						return (byte)(
							(serialShiftClock ? (1 << 0) : 0) |
							(serialClockSpeed ? (1 << 1) : 0) |
							(serialTransferStartFlag ? (1 << 7) : 0));

					case 0xFF04:
						return divider;

					case 0xFF05:
						return timerCounter;

					case 0xFF06:
						return timerModulo;

					case 0xFF07:
						return (byte)((timerRunning ? (1 << 2) : 0) | (timerInputClock & 0b11));

					case 0xFF0F:
						return (byte)(
							(irqVBlank ? (1 << 0) : 0) |
							(irqLCDCStatus ? (1 << 1) : 0) |
							(irqTimerOverflow ? (1 << 2) : 0) |
							(irqSerialIO ? (1 << 3) : 0) |
							(irqKeypad ? (1 << 4) : 0));

					case 0xFF50:
						return (byte)(bootstrapDisabled ? 0x01 : 0x00);

					default:
						throw new NotImplementedException();
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
						joypadRegister = (byte)((joypadRegister & 0xCF) | (value & 0x30));
						break;

					case 0xFF01:
						serialData = value;
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

					case 0xFF02:
						serialShiftClock = (value & (1 << 0)) != 0;
						serialClockSpeed = (value & (1 << 1)) != 0;
						serialTransferStartFlag = (value & (1 << 7)) != 0;
						break;

					case 0xFF0F:
						irqVBlank = (value & (1 << 0)) != 0;
						irqLCDCStatus = (value & (1 << 1)) != 0;
						irqTimerOverflow = (value & (1 << 2)) != 0;
						irqSerialIO = (value & (1 << 3)) != 0;
						irqKeypad = (value & (1 << 4)) != 0;
						break;

					case 0xFF50:
						bootstrapDisabled = (value != 0x00 ? true : false);
						break;
				}
			}
		}
	}
}
