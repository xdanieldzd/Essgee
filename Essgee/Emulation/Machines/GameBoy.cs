using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Essgee.Emulation.Configuration;
using Essgee.Emulation.CPU;
using Essgee.Emulation.VDP;
using Essgee.Emulation.PSG;
using Essgee.Emulation.Cartridges;
using Essgee.EventArguments;
using Essgee.Exceptions;
using Essgee.Utilities;

namespace Essgee.Emulation.Machines
{
	[MachineIndex(5)]
	public class GameBoy : IMachine
	{
		const double masterClock = 4194304;
		const double refreshRate = 59.73;

		const int ramSize = 8 * 1024;

		//

		public event EventHandler<SendLogMessageEventArgs> SendLogMessage;
		protected virtual void OnSendLogMessage(SendLogMessageEventArgs e) { SendLogMessage?.Invoke(this, e); }

		public event EventHandler<EventArgs> EmulationReset;
		protected virtual void OnEmulationReset(EventArgs e) { EmulationReset?.Invoke(this, e); }

		public event EventHandler<RenderScreenEventArgs> RenderScreen
		{
			add { vdp.RenderScreen += value; }
			remove { vdp.RenderScreen -= value; }
		}

		public event EventHandler<SizeScreenEventArgs> SizeScreen
		{
			add { vdp.SizeScreen += value; }
			remove { vdp.SizeScreen -= value; }
		}

		public event EventHandler<ChangeViewportEventArgs> ChangeViewport;
		protected virtual void OnChangeViewport(ChangeViewportEventArgs e) { ChangeViewport?.Invoke(this, e); }

		public event EventHandler<PollInputEventArgs> PollInput;
		protected virtual void OnPollInput(PollInputEventArgs e) { PollInput?.Invoke(this, e); }

		public event EventHandler<EnqueueSamplesEventArgs> EnqueueSamples
		{
			add { psg.EnqueueSamples += value; }
			remove { psg.EnqueueSamples -= value; }
		}

		public string ManufacturerName => "Nintendo";
		public string ModelName => "Game Boy";
		public string DatFilename => "Nintendo - Game Boy.dat";
		public (string Extension, string Description) FileFilter => (".gb", "Game Boy ROMs");
		public bool HasBootstrap => true;
		public double RefreshRate => refreshRate;

		public GraphicsEnableState GraphicsEnableStates
		{
			get { return vdp.GraphicsEnableStates; }
			set { vdp.GraphicsEnableStates = value; }
		}

		public SoundEnableState SoundEnableStates
		{
			get { return psg.SoundEnableStates; }
			set { psg.SoundEnableStates = value; }
		}

		byte[] bootstrap;
		ICartridge cartridge;
		byte[] wram;
		ICPU cpu;
		IVDP vdp;
		IPSG psg;

		//

		int currentMasterClockCyclesInFrame, totalMasterClockCyclesInFrame;

		Configuration.GameBoy configuration;

		public GameBoy() { }

		public void Initialize()
		{
			bootstrap = null;
			cartridge = null;

			wram = new byte[ramSize];
			cpu = new LR35902(ReadMemory, WriteMemory);
			vdp = new GameBoyVDP();
			psg = new GameBoyPSG();

			vdp.EndOfScanline += (s, e) =>
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

		private void ReconfigureSystem()
		{
			vdp?.SetClockRate(masterClock);
			vdp?.SetRefreshRate(refreshRate);
			vdp?.SetRevision(0);

			psg?.SetSampleRate(Program.Configuration.SampleRate);
			psg?.SetOutputChannels(2);
			psg?.SetClockRate(masterClock);
			psg?.SetRefreshRate(refreshRate);

			currentMasterClockCyclesInFrame = 0;
			totalMasterClockCyclesInFrame = (int)Math.Round(masterClock / refreshRate);

			OnChangeViewport(new ChangeViewportEventArgs(vdp.Viewport));
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
			vdp.Startup();
			psg.Startup();
		}

		public void Reset()
		{
			cpu.Reset();
			vdp.Reset();
			psg.Reset();

			if (configuration.UseBootstrap)
			{
				cpu.SetProgramCounter(0x0000);
				cpu.SetStackPointer(0x0000);
			}
			else
			{
				cpu.SetProgramCounter(0x0100);
				cpu.SetStackPointer(0xFFFE);
			}

			OnEmulationReset(EventArgs.Empty);
		}

		public void Shutdown()
		{
			cpu?.Shutdown();
			vdp?.Shutdown();
			psg?.Shutdown();
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
				mapperType = typeof(ColecoCartridge);

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

			vdp.Step((int)Math.Round(currentCpuClockCycles));

			cpu.SetInterruptLine(InterruptType.Maskable, vdp.InterruptLine);
			//TODO other interrupt sources

			psg.Step((int)Math.Round(currentCpuClockCycles));

			currentMasterClockCyclesInFrame += (int)Math.Round(currentCpuClockCycles);
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
				if (configuration.UseBootstrap && address < 0x0100 && true /* bootstrap enabled */)
					return bootstrap[address & 0x00FF];
				else
					return (cartridge != null ? cartridge.Read(address) : (byte)0x00);
			}
			else if (address >= 0x8000 && address <= 0x9FFF)
			{
				//vram
			}
			else if (address >= 0xA000 && address <= 0xBFFF)
			{
				//cartram
			}
			else if (address >= 0xC000 && address <= 0xFDFF)
			{
				return wram[address & (ramSize - 1)];
			}
			else if (address >= 0xFE00 && address <= 0xFE9F)
			{
				//oam
			}
			else if (address >= 0xFF00 && address <= 0xFF7F)
			{
				//io
			}
			else if (address >= 0xFF80 && address <= 0xFFFE)
			{
				//hram
			}
			else if (address == 0xFFFF)
			{
				//ie
			}

			/* Cannot read from address, return 0 */
			return 0x00;
		}

		private void WriteMemory(ushort address, byte value)
		{
			if (address >= 0x0000 && address <= 0x7FFF)
			{
				cartridge?.Write(address, value);
			}
			else if (address >= 0x8000 && address <= 0x9FFF)
			{
				//vram
			}
			else if (address >= 0xA000 && address <= 0xBFFF)
			{
				//cartram
			}
			else if (address >= 0xC000 && address <= 0xFDFF)
			{
				wram[address & (ramSize - 1)] = value;
			}
			else if (address >= 0xFE00 && address <= 0xFE9F)
			{
				//oam
			}
			else if (address >= 0xFF00 && address <= 0xFF7F)
			{
				//io
			}
			else if (address >= 0xFF80 && address <= 0xFFFE)
			{
				//hram
			}
			else if (address == 0xFFFF)
			{
				//ie
			}
		}
	}
}
