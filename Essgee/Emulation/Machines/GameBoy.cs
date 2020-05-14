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

		// FF01
		byte serialData;
		// FF02
		bool serialShiftClock, serialClockSpeed, serialTransferStartFlag;

		// FF0F
		bool irqVBlank, irqLCDCStatus, irqTimerOverflow, irqSerialIO, irqKeypad;

		// FF50
		bool bootstrapDisabled;

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
				// TODO autodetect mbcs
				mapperType = typeof(ROMOnlyCartridge);
			}

			cartridge = (ICartridge)Activator.CreateInstance(mapperType, new object[] { romData.Length, 0 });
			cartridge.LoadRom(romData);
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

			video.Step((int)Math.Round(currentCpuClockCycles));

			HandleInterrupts();

			audio.Step((int)Math.Round(currentCpuClockCycles));

			currentMasterClockCyclesInFrame += (int)Math.Round(currentCpuClockCycles);
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
			}

			return execute;
		}

		private void ParseInput(PollInputEventArgs eventArgs)
		{
			var keysDown = eventArgs.Keyboard;

			//
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
			else
			{
				switch (address)
				{
					case 0xFF01:
						return serialData;

					case 0xFF02:
						return (byte)(
							(serialShiftClock ? 0b00000001 : 0) |
							(serialClockSpeed ? 0b00000010 : 0) |
							(serialTransferStartFlag ? 0b10000000 : 0));

					case 0xFF0F:
						return (byte)(
							(irqVBlank ? 0b00000001 : 0) |
							(irqLCDCStatus ? 0b00000010 : 0) |
							(irqTimerOverflow ? 0b00000100 : 0) |
							(irqSerialIO ? 0b00001000 : 0) |
							(irqKeypad ? 0b00010000 : 0));

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

			else
			{
				switch (address)
				{
					case 0xFF01:
						serialData = value;
						break;

					case 0xFF02:
						serialShiftClock = (value & 0b00000001) != 0;
						serialClockSpeed = (value & 0b00000010) != 0;
						serialTransferStartFlag = (value & 0b10000000) != 0;
						break;

					case 0xFF0F:
						irqVBlank = (value & 0b00000001) != 0;
						irqLCDCStatus = (value & 0b00000010) != 0;
						irqTimerOverflow = (value & 0b00000100) != 0;
						irqSerialIO = (value & 0b00001000) != 0;
						irqKeypad = (value & 0b00010000) != 0;
						break;

					case 0xFF50:
						bootstrapDisabled = (value != 0x00 ? true : false);
						break;

					default:
						throw new NotImplementedException();
				}
			}
		}
	}
}
