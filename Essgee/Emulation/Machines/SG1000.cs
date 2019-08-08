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
using Essgee.Emulation.Peripherals;
using Essgee.EventArguments;
using Essgee.Utilities;

namespace Essgee.Emulation.Machines
{
	[MachineIndex(0)]
	public class SG1000 : IMachine
	{
		const double masterClockNtsc = 10738635;
		const double masterClockPal = 10640684;
		const double refreshRateNtsc = 59.922743;
		const double refreshRatePal = 49.701459;

		const int ramSize = 1 * 1024;

		double masterClock;
		double vdpClock, psgClock;

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

		public string ManufacturerName => "Sega";
		public string ModelName => "SG-1000";
		public string DatFilename => "Sega - SG-1000.dat";
		public (string Extension, string Description) FileFilter => (".sg", "SG-1000 ROMs");
		public bool HasBootstrap => false;
		public double RefreshRate { get; private set; }

		public GraphicsEnableState GraphicsEnableStates
		{
			get { return vdp.GraphicsEnableStates; }
			set { vdp.GraphicsEnableStates = value; }
		}

		ICartridge cartridge;
		byte[] wram;
		ICPU cpu;
		IVDP vdp;
		IPSG psg;
		Intel8255 ppi;

		[Flags]
		enum PortAInputs : byte
		{
			P1Up = (1 << 0),
			P1Down = (1 << 1),
			P1Left = (1 << 2),
			P1Right = (1 << 3),
			P1Button1 = (1 << 4),
			P1Button2 = (1 << 5),
			P2Up = (1 << 6),
			P2Down = (1 << 7),
		}

		[Flags]
		enum PortBInputs : byte
		{
			P2Left = (1 << 0),
			P2Right = (1 << 1),
			P2Button1 = (1 << 2),
			P2Button2 = (1 << 3),
		}

		PortAInputs portAInputsPressed;
		PortBInputs portBInputsPressed;

		bool pauseButtonPressed, pauseButtonToggle;

		int currentMasterClockCyclesInFrame, totalMasterClockCyclesInFrame;

		Configuration.SG1000 configuration;

		public SG1000() { }

		public void Initialize()
		{
			cartridge = null;
			cpu = new Z80A(ReadMemory, WriteMemory, ReadPort, WritePort);
			wram = new byte[ramSize];
			vdp = new TMS99xxA();
			psg = new SN76489(44100, 2);
			ppi = new Intel8255();
		}

		public void SetConfiguration(IConfiguration config)
		{
			configuration = (Configuration.SG1000)config;

			ReconfigureSystem();
		}

		private void ReconfigureSystem()
		{
			if (configuration.TVStandard == TVStandard.NTSC)
			{
				masterClock = masterClockNtsc;
				RefreshRate = refreshRateNtsc;
			}
			else
			{
				masterClock = masterClockPal;
				RefreshRate = refreshRatePal;
			}

			vdpClock = (masterClock / 1.0);
			psgClock = (masterClock / 3.0);

			vdp?.SetClockRate(vdpClock);
			vdp?.SetRefreshRate(RefreshRate);

			psg?.SetClockRate(psgClock);
			psg?.SetRefreshRate(RefreshRate);

			currentMasterClockCyclesInFrame = 0;
			totalMasterClockCyclesInFrame = (int)Math.Round(masterClock / RefreshRate);

			OnChangeViewport(new ChangeViewportEventArgs(vdp.Viewport));
		}

		public void Startup()
		{
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
			ppi.Reset();

			portAInputsPressed = 0;
			portBInputsPressed = 0;
			pauseButtonPressed = pauseButtonToggle = false;

			OnEmulationReset(EventArgs.Empty);
		}

		public void Shutdown()
		{
			cpu?.Shutdown();
			vdp?.Shutdown();
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
				mapperType = typeof(SegaSGCartridge);

			cartridge = (ICartridge)Activator.CreateInstance(mapperType, new object[] { romData.Length, ramData.Length });
			cartridge.LoadRom(romData);
			cartridge.LoadRam(ramData);
		}

		public byte[] GetCartridgeRam()
		{
			return cartridge.GetRamData();
		}

		public bool IsCartridgeRamSaveNeeded()
		{
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

			vdp.Step((int)Math.Round(currentMasterClockCycles));

			if (pauseButtonPressed)
			{
				pauseButtonPressed = false;
				cpu.SetInterruptLine(InterruptType.NonMaskable, InterruptState.Assert);
			}

			cpu.SetInterruptLine(InterruptType.Maskable, vdp.InterruptLine);

			psg.Step((int)Math.Round(currentCpuClockCycles));

			currentMasterClockCyclesInFrame += (int)Math.Round(currentMasterClockCycles);
		}

		private void ParseInput(PollInputEventArgs eventArgs)
		{
			/* Get variables */
			var keysDown = eventArgs.Keyboard;

			/* Handle Pause button */
			var pausePressed = keysDown.Contains(configuration.InputPause);
			var pauseButtonHeld = (pauseButtonToggle && pausePressed);
			if (pausePressed)
			{
				if (!pauseButtonHeld) pauseButtonPressed = true;
				pauseButtonToggle = true;
			}
			else if (pauseButtonToggle)
				pauseButtonToggle = false;

			/* Handle controllers */
			portAInputsPressed = 0;
			portBInputsPressed = 0;

			if (keysDown.Contains(configuration.Joypad1Up)) portAInputsPressed |= PortAInputs.P1Up;
			if (keysDown.Contains(configuration.Joypad1Down)) portAInputsPressed |= PortAInputs.P1Down;
			if (keysDown.Contains(configuration.Joypad1Left)) portAInputsPressed |= PortAInputs.P1Left;
			if (keysDown.Contains(configuration.Joypad1Right)) portAInputsPressed |= PortAInputs.P1Right;
			if (keysDown.Contains(configuration.Joypad1Button1)) portAInputsPressed |= PortAInputs.P1Button1;
			if (keysDown.Contains(configuration.Joypad1Button2)) portAInputsPressed |= PortAInputs.P1Button2;

			if (keysDown.Contains(configuration.Joypad2Up)) portAInputsPressed |= PortAInputs.P2Up;
			if (keysDown.Contains(configuration.Joypad2Down)) portAInputsPressed |= PortAInputs.P2Down;
			if (keysDown.Contains(configuration.Joypad2Left)) portBInputsPressed |= PortBInputs.P2Left;
			if (keysDown.Contains(configuration.Joypad2Right)) portBInputsPressed |= PortBInputs.P2Right;
			if (keysDown.Contains(configuration.Joypad2Button1)) portBInputsPressed |= PortBInputs.P2Button1;
			if (keysDown.Contains(configuration.Joypad2Button2)) portBInputsPressed |= PortBInputs.P2Button2;
		}

		private void UpdateInput()
		{
			byte portA = 0xFF, portB = 0xFF;
			if ((ppi.PortCOutput & 0x07) == 0x07)
			{
				portA &= (byte)~portAInputsPressed;
				portB &= (byte)~portBInputsPressed;
			}
			ppi.PortAInput = portA;
			ppi.PortBInput = (byte)((ppi.PortBInput & 0xF0) | (portB & 0x0F));
		}

		private byte ReadMemory(ushort address)
		{
			if (address >= 0x0000 && address <= 0xBFFF)
			{
				return (cartridge != null ? cartridge.Read(address) : (byte)0x00);
			}
			else if (address >= 0xC000 && address <= 0xFFFF)
			{
				return wram[address & (ramSize - 1)];
			}

			/* Cannot read from address, return 0 */
			return 0x00;
		}

		private void WriteMemory(ushort address, byte value)
		{
			if (address >= 0x0000 && address <= 0xBFFF)
			{
				cartridge?.Write(address, value);
			}
			else if (address >= 0xC000 && address <= 0xFFFF)
			{
				wram[address & (ramSize - 1)] = value;
			}
		}

		private byte ReadPort(byte port)
		{
			switch (port & 0xC0)
			{
				case 0x80:
					return vdp.ReadPort(port);

				case 0xC0:
					UpdateInput();
					return ppi.ReadPort(port);

				default:
					// TODO: handle properly
					return 0x00;
			}
		}

		public void WritePort(byte port, byte value)
		{
			switch (port & 0xC0)
			{
				case 0x40: psg.WritePort(port, value); break;
				case 0x80: vdp.WritePort(port, value); break;
				case 0xC0: ppi.WritePort(port, value); break;
				default: break; // TODO: handle properly
			}
		}
	}
}
