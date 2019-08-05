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
using Essgee.Utilities;

namespace Essgee.Emulation.Machines
{
	[MachineIndex(3)]
	public class GameGear : IMachine
	{
		const double masterClock = 10738635;
		const double refreshRate = 59.922743;

		const int ramSize = 1 * 8192;

		double vdpClock, psgClock;

		public event EventHandler<SendLogMessageEventArgs> SendLogMessage;
		protected virtual void OnSendLogMessage(SendLogMessageEventArgs e) { SendLogMessage?.Invoke(this, e); }

		public event EventHandler<EventArgs> EmulationReset;
		protected virtual void OnEmulationReset(EventArgs e) { EmulationReset?.Invoke(this, e); }

		public event EventHandler<RenderScreenEventArgs> RenderScreen;
		protected virtual void OnRenderScreen(RenderScreenEventArgs e) { RenderScreen?.Invoke(this, e); }

		public event EventHandler<SizeScreenEventArgs> SizeScreen;
		protected virtual void OnSizeScreen(SizeScreenEventArgs e) { SizeScreen?.Invoke(this, e); }

		public event EventHandler<ChangeViewportEventArgs> ChangeViewport;
		protected virtual void OnChangeViewport(ChangeViewportEventArgs e) { ChangeViewport?.Invoke(this, e); }

		public event EventHandler<PollInputEventArgs> PollInput;
		protected virtual void OnPollInput(PollInputEventArgs e) { PollInput?.Invoke(this, e); }

		public event EventHandler<EnqueueSamplesEventArgs> EnqueueSamples;
		protected virtual void OnEnqueueSamples(EnqueueSamplesEventArgs e) { EnqueueSamples?.Invoke(this, e); }

		public string ManufacturerName => "Sega";
		public string ModelName => "Game Gear";
		public string DatFilename => "Sega - Game Gear.dat";
		public (string Extension, string Description) FileFilter => (".gg", "Game Gear ROMs");
		public bool HasBootstrap => true;
		public double RefreshRate => refreshRate;

		ICartridge bootstrap, cartridge;
		byte[] wram;
		Z80A cpu;
		SegaGGVDP vdp;
		SegaGGPSG psg;

		[Flags]
		enum IOPortABInputs : byte
		{
			PortAUp = (1 << 0),
			PortADown = (1 << 1),
			PortALeft = (1 << 2),
			PortARight = (1 << 3),
			PortATL = (1 << 4),
			PortATR = (1 << 5),
			PortBUp = (1 << 6),
			PortBDown = (1 << 7)
		}

		[Flags]
		enum IOPortBMiscInputs : byte
		{
			PortBLeft = (1 << 0),
			PortBRight = (1 << 1),
			PortBTL = (1 << 2),
			PortBTR = (1 << 3),
			Reset = (1 << 4),
			CartSlotCONTPin = (1 << 5),
			PortATH = (1 << 6),
			PortBTH = (1 << 7)
		}

		[Flags]
		enum IOPortCInputs : byte
		{
			Start = (1 << 7)
		}

		IOPortABInputs portAInputsPressed;
		IOPortBMiscInputs portBInputsPressed;
		IOPortCInputs portCInputsPressed;

		byte portMemoryControl, portIoControl, hCounterLatched, portIoAB, portIoBMisc;
		byte portIoC, portParallelData, portDataDirNMI, portTxBuffer, portRxBuffer, portSerialControl;

		bool isWorkRamEnabled { get { return !BitUtilities.IsBitSet(portMemoryControl, 4); } }
		bool isBootstrapRomEnabled { get { return !BitUtilities.IsBitSet(portMemoryControl, 3); } }

		int currentMasterClockCyclesInFrame, totalMasterClockCyclesInFrame;

		Configuration.GameGear configuration;

		public GameGear() { }

		public void Initialize()
		{
			bootstrap = null;
			cartridge = null;

			cpu = new Z80A(ReadMemory, WriteMemory, ReadPort, WritePort);
			wram = new byte[ramSize];
			vdp = new SegaGGVDP();
			psg = new SegaGGPSG(44100, 2, (s, e) => { OnEnqueueSamples(e); });
		}

		public void SetConfiguration(IConfiguration config)
		{
			configuration = (Configuration.GameGear)config;

			ReconfigureSystem();
		}

		private void ReconfigureSystem()
		{
			vdpClock = (masterClock / 1.0);
			psgClock = (masterClock / 3.0);

			vdp?.SetClockRate(vdpClock);
			vdp?.SetRefreshRate(refreshRate);

			psg?.SetClockRate(psgClock);
			psg?.SetRefreshRate(refreshRate);

			currentMasterClockCyclesInFrame = 0;
			totalMasterClockCyclesInFrame = (int)Math.Round(masterClock / refreshRate);

			OnSizeScreen(new SizeScreenEventArgs(vdp.NumTotalPixelsPerScanline, vdp.NumTotalScanlines));
			OnChangeViewport(new ChangeViewportEventArgs(vdp.Viewport));
		}

		private void LoadBootstrap()
		{
			if (configuration.UseBootstrap && configuration.BootstrapRom != string.Empty)
			{
				var (type, bootstrapRomData) = configuration.BootstrapRom.TryLoadCartridge();
				bootstrap = new SegaMapperCartridge(bootstrapRomData.Length, 0);
				bootstrap.LoadRom(bootstrapRomData);
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
			cpu.SetStackPointer(0xDFF0);
			vdp.Reset();
			psg.Reset();

			portAInputsPressed = 0;
			portBInputsPressed = 0;
			portCInputsPressed = 0;

			portMemoryControl = (byte)(bootstrap != null ? 0xA3 : 0x00);
			portIoControl = 0x0F;
			hCounterLatched = 0x00;
			portIoAB = portIoBMisc = 0xFF;

			portIoC = (byte)(0x80 | (configuration.Region == Region.Export ? 0x40 : 0x00));
			portParallelData = 0x00;
			portDataDirNMI = 0xFF;
			portTxBuffer = 0x00;
			portRxBuffer = 0xFF;
			portSerialControl = 0x00;
			psg.WriteStereoControl(0xFF);

			OnEmulationReset(EventArgs.Empty);
		}

		public void Shutdown()
		{
			psg?.Shutdown();
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
				mapperType = typeof(SegaMapperCartridge);
			if (ramData.Length == 0)
				ramData = new byte[32768];

			cartridge = (ICartridge)Activator.CreateInstance(mapperType, new object[] { romData.Length, ramData.Length });
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
			PollInputEventArgs pollInputEventArgs = new PollInputEventArgs();
			PollInput?.Invoke(this, pollInputEventArgs);
			ParseInput(pollInputEventArgs);

			while (currentMasterClockCyclesInFrame < totalMasterClockCyclesInFrame)
				RunStep();

			currentMasterClockCyclesInFrame -= totalMasterClockCyclesInFrame;
		}

		public void RunStep()
		{
			double currentCpuClockCycles = 0.0;
			currentCpuClockCycles += cpu.Step();

			double currentMasterClockCycles = (currentCpuClockCycles * 3.0);

			if (vdp.Step((int)Math.Round(currentMasterClockCycles)))
				OnRenderScreen(new RenderScreenEventArgs(vdp.NumTotalPixelsPerScanline, vdp.NumTotalScanlines, vdp.OutputFramebuffer));

			cpu.SetInterruptLine(vdp.InterruptLine);

			psg.Step((int)Math.Round(currentCpuClockCycles));

			currentMasterClockCyclesInFrame += (int)Math.Round(currentMasterClockCycles);
		}

		private void ParseInput(PollInputEventArgs eventArgs)
		{
			var keysDown = eventArgs.Keyboard;

			portAInputsPressed = 0;
			portBInputsPressed = 0;
			portCInputsPressed = 0;

			if (keysDown.Contains(configuration.ControlsUp)) portAInputsPressed |= IOPortABInputs.PortAUp;
			if (keysDown.Contains(configuration.ControlsDown)) portAInputsPressed |= IOPortABInputs.PortADown;
			if (keysDown.Contains(configuration.ControlsLeft)) portAInputsPressed |= IOPortABInputs.PortALeft;
			if (keysDown.Contains(configuration.ControlsRight)) portAInputsPressed |= IOPortABInputs.PortARight;
			if (keysDown.Contains(configuration.ControlsButton1)) portAInputsPressed |= IOPortABInputs.PortATL;
			if (keysDown.Contains(configuration.ControlsButton2)) portAInputsPressed |= IOPortABInputs.PortATR;
			if (keysDown.Contains(configuration.ControlsStart)) portCInputsPressed |= IOPortCInputs.Start;

			portIoAB |= (byte)(IOPortABInputs.PortAUp | IOPortABInputs.PortADown | IOPortABInputs.PortALeft | IOPortABInputs.PortARight | IOPortABInputs.PortATL | IOPortABInputs.PortATR | IOPortABInputs.PortBUp | IOPortABInputs.PortBDown);
			portIoBMisc |= (byte)(IOPortBMiscInputs.PortBLeft | IOPortBMiscInputs.PortBRight | IOPortBMiscInputs.PortBTL | IOPortBMiscInputs.PortBTR | IOPortBMiscInputs.Reset | IOPortBMiscInputs.CartSlotCONTPin | IOPortBMiscInputs.PortATH | IOPortBMiscInputs.PortBTH);
			portIoC |= (byte)IOPortCInputs.Start;

			portIoAB &= (byte)~portAInputsPressed;
			portIoBMisc &= (byte)~portBInputsPressed;
			portIoC &= (byte)~portCInputsPressed;
		}

		private byte ReadMemory(ushort address)
		{
			if (address >= 0x0000 && address <= 0xBFFF)
			{
				if (address <= 0x0400 && isBootstrapRomEnabled && bootstrap != null)
					return bootstrap.Read(address);

				if (cartridge != null)
					return cartridge.Read(address);
			}
			else if (address >= 0xC000 && address <= 0xFFFF)
			{
				if (isWorkRamEnabled)
					return wram[address & (ramSize - 1)];
			}

			/* Cannot read from address, return 0 */
			return 0x00;
		}

		private void WriteMemory(ushort address, byte value)
		{
			if (isBootstrapRomEnabled) bootstrap?.Write(address, value);
			cartridge?.Write(address, value);

			if (isWorkRamEnabled && address >= 0xC000 && address <= 0xFFFF)
				wram[address & (ramSize - 1)] = value;
		}

		private byte ReadPort(byte port)
		{
			var maskedPort = (byte)(port & 0xC1);

			switch (maskedPort & 0xF0)
			{
				case 0x00:
					/* GG-specific ports */
					switch (port)
					{
						case 0x00: return (byte)((portIoC & 0xBF) | (configuration.Region == Region.Export ? 0x40 : 0x00));
						case 0x01: return portParallelData;
						case 0x02: return portDataDirNMI;
						case 0x03: return portTxBuffer;
						case 0x04: return portRxBuffer;
						case 0x05: return portSerialControl;
						case 0x06: return 0xFF;
					}
					return 0xFF;

				case 0x40:
					/* Counters */
					if ((maskedPort & 0x01) == 0)
						return vdp.ReadVCounter();          /* V counter */
					else
						return hCounterLatched;             /* H counter */

				case 0x80:
					return vdp.ReadPort(maskedPort);        /* VDP ports */

				case 0xC0:
					if (port == 0xC0 || port == 0xDC)
						return portIoAB;                    /* IO port A/B register */
					else if (port == 0xC1 || port == 0xDD)
						return portIoBMisc;                 /* IO port B/misc register */
					else
						return 0xFF;

				default:
					// TODO: handle properly
					return 0x00;
			}
		}

		public void WritePort(byte port, byte value)
		{
			var maskedPort = (byte)(port & 0xC1);

			switch (maskedPort & 0xF0)
			{
				case 0x00:
					switch (port)
					{
						case 0x00: /* Read-only */ break;
						case 0x01: portParallelData = value; break;
						case 0x02: portDataDirNMI = value; break;
						case 0x03: portTxBuffer = value; break;
						case 0x04: /* Read-only? */; break;
						case 0x05: portSerialControl = (byte)(value & 0xF8); break;
						case 0x06: psg.WriteStereoControl(value); break;
						default:
							/* System stuff */
							if ((maskedPort & 0x01) == 0)
								portMemoryControl = value;  /* Memory control */
							else
							{
								/* I/O control */
								if ((portIoControl & 0x0A) == 0x00 && ((value & 0x02) == 0x02 || (value & 0x08) == 0x08))
									hCounterLatched = vdp.ReadHCounter();
								portIoControl = value;
							}
							break;
					}
					break;

				case 0x40:
					/* PSG */
					psg.WritePort(maskedPort, value);
					break;

				case 0x80:
					/* VDP */
					vdp.WritePort(maskedPort, value);
					break;

				case 0xC0:
					/* No effect */
					break;

				default:
					// TODO: handle properly
					break;
			}
		}
	}
}
