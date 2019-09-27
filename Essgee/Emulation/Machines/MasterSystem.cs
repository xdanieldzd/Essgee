using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
	[MachineIndex(2)]
	public class MasterSystem : IMachine
	{
		const double masterClockNtsc = 10738635;
		const double masterClockPal = 10640684;
		const double refreshRateNtsc = 59.922743;
		const double refreshRatePal = 49.701459;

		const int ramSize = 1 * 8192;

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
		public string ModelName => "Master System";
		public string DatFilename => "Sega - Master System - Mark III.dat";
		public (string Extension, string Description) FileFilter => (".sms", "Master System ROMs");
		public bool HasBootstrap => true;
		public double RefreshRate { get; private set; }

		public GraphicsEnableState GraphicsEnableStates
		{
			get { return vdp.GraphicsEnableStates; }
			set { vdp.GraphicsEnableStates = value; }
		}

		ICartridge bootstrap, cartridge;
		byte[] wram;
		ICPU cpu;
		IVDP vdp;
		IPSG psg;

		InputDevice[] inputDevices;

		[Flags]
		enum ControllerInputs : byte
		{
			Up = 0b00000001,
			Down = 0b00000010,
			Left = 0b00000100,
			Right = 0b00001000,
			TL = 0b00010000,
			TR = 0b00100000,
			TH = 0b01000000
		}
		const byte inputResetButton = 0x10;

		bool lightgunLatched;

		const string pauseInputName = "Pause";
		bool pauseButtonPressed, pauseButtonToggle;

		byte portMemoryControl, portIoControl, hCounterLatched;

		bool isExpansionSlotEnabled { get { return !BitUtilities.IsBitSet(portMemoryControl, 7); } }
		bool isCartridgeSlotEnabled { get { return !BitUtilities.IsBitSet(portMemoryControl, 6); } }
		bool isCardSlotEnabled { get { return !BitUtilities.IsBitSet(portMemoryControl, 5); } }
		bool isWorkRamEnabled { get { return !BitUtilities.IsBitSet(portMemoryControl, 4); } }
		bool isBootstrapRomEnabled { get { return !BitUtilities.IsBitSet(portMemoryControl, 3); } }
		bool isIoChipEnabled { get { return !BitUtilities.IsBitSet(portMemoryControl, 2); } }

		enum IOControlDirection { Output = 0, Input = 1 }
		enum IOControlOutputLevel { Low = 0, High = 1 }
		enum IOControlPort { A = 0, B = 1 }
		enum IOControlPin { TR = 0, TH = 1 };

		IOControlDirection portAPinTRDirection { get { return (IOControlDirection)((portIoControl >> 0) & 0x01); } }
		IOControlDirection portAPinTHDirection { get { return (IOControlDirection)((portIoControl >> 1) & 0x01); } }
		IOControlDirection portBPinTRDirection { get { return (IOControlDirection)((portIoControl >> 2) & 0x01); } }
		IOControlDirection portBPinTHDirection { get { return (IOControlDirection)((portIoControl >> 3) & 0x01); } }
		IOControlOutputLevel portAPinTROutputLevel { get { return (IOControlOutputLevel)((portIoControl >> 4) & 0x01); } }
		IOControlOutputLevel portAPinTHOutputLevel { get { return (IOControlOutputLevel)((portIoControl >> 5) & 0x01); } }
		IOControlOutputLevel portBPinTROutputLevel { get { return (IOControlOutputLevel)((portIoControl >> 6) & 0x01); } }
		IOControlOutputLevel portBPinTHOutputLevel { get { return (IOControlOutputLevel)((portIoControl >> 7) & 0x01); } }

		int currentMasterClockCyclesInFrame, totalMasterClockCyclesInFrame;

		Configuration.MasterSystem configuration;

		IEnumerable<Keys> lastKeysDown;
		MouseButtons lastMouseButtons;
		(int x, int y) lastMousePosition;

		public MasterSystem() { }

		public void Initialize()
		{
			bootstrap = null;
			cartridge = null;

			wram = new byte[ramSize];
			cpu = new Z80A(ReadMemory, WriteMemory, ReadPort, WritePort);
			vdp = new SegaSMSVDP();
			psg = new SegaSMSPSG(44100, 2);

			inputDevices = new InputDevice[2];
			inputDevices[0] = InputDevice.None;
			inputDevices[1] = InputDevice.None;
		}

		public void SetConfiguration(IConfiguration config)
		{
			configuration = (Configuration.MasterSystem)config;

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

			inputDevices[0] = configuration.Joypad1DeviceType;
			inputDevices[1] = configuration.Joypad2DeviceType;
		}

		private void LoadBootstrap()
		{
			if (configuration.UseBootstrap && configuration.BootstrapRom != string.Empty)
			{
				var (type, bootstrapRomData) = CartridgeLoader.Load(configuration.BootstrapRom);
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

			pauseButtonPressed = pauseButtonToggle = false;

			portMemoryControl = (byte)(bootstrap != null ? 0xE3 : 0x00);
			portIoControl = 0x0F;
			hCounterLatched = 0x00;

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
			configuration.TVStandard = state[nameof(configuration.TVStandard)];
			configuration.Region = state[nameof(configuration.Region)];

			SaveStateHandler.PerformSetState(bootstrap, state[nameof(bootstrap)]);
			SaveStateHandler.PerformSetState(cartridge, state[nameof(cartridge)]);
			wram = state[nameof(wram)];
			SaveStateHandler.PerformSetState(cpu, state[nameof(cpu)]);
			SaveStateHandler.PerformSetState(vdp, state[nameof(vdp)]);
			SaveStateHandler.PerformSetState(psg, state[nameof(psg)]);

			inputDevices = state[nameof(inputDevices)];
			lightgunLatched = state[nameof(lightgunLatched)];

			portMemoryControl = state[nameof(portMemoryControl)];
			portIoControl = state[nameof(portIoControl)];
			hCounterLatched = state[nameof(hCounterLatched)];

			ReconfigureSystem();
		}

		public Dictionary<string, dynamic> GetState()
		{
			return new Dictionary<string, dynamic>
			{
				[nameof(configuration.TVStandard)] = configuration.TVStandard,
				[nameof(configuration.Region)] = configuration.Region,

				[nameof(bootstrap)] = SaveStateHandler.PerformGetState(bootstrap),
				[nameof(cartridge)] = SaveStateHandler.PerformGetState(cartridge),
				[nameof(wram)] = wram,
				[nameof(cpu)] = SaveStateHandler.PerformGetState(cpu),
				[nameof(vdp)] = SaveStateHandler.PerformGetState(vdp),
				[nameof(psg)] = SaveStateHandler.PerformGetState(psg),

				[nameof(inputDevices)] = inputDevices,
				[nameof(lightgunLatched)] = lightgunLatched,

				[nameof(portMemoryControl)] = portMemoryControl,
				[nameof(portIoControl)] = portIoControl,
				[nameof(hCounterLatched)] = hCounterLatched
			};
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

			lastKeysDown = pollInputEventArgs.Keyboard;
			lastMouseButtons = pollInputEventArgs.MouseButtons;
			lastMousePosition = pollInputEventArgs.MousePosition;

			HandlePauseButton();

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

		private void HandlePauseButton()
		{
			var pausePressed = lastKeysDown.Contains(configuration.InputPause);
			var pauseButtonHeld = (pauseButtonToggle && pausePressed);
			if (pausePressed)
			{
				if (!pauseButtonHeld) pauseButtonPressed = true;
				pauseButtonToggle = true;
			}
			else if (pauseButtonToggle)
				pauseButtonToggle = false;
		}

		private byte ReadInput(int port)
		{
			var state = (byte)0xFF;

			switch (inputDevices[port])
			{
				case InputDevice.None:
					/* Do nothing */
					break;

				case InputDevice.Controller:
					if (lastKeysDown.Contains((port == 0 ? configuration.Joypad1Up : configuration.Joypad2Up))) state &= (byte)~ControllerInputs.Up;
					if (lastKeysDown.Contains((port == 0 ? configuration.Joypad1Down : configuration.Joypad2Down))) state &= (byte)~ControllerInputs.Down;
					if (lastKeysDown.Contains((port == 0 ? configuration.Joypad1Left : configuration.Joypad2Left))) state &= (byte)~ControllerInputs.Left;
					if (lastKeysDown.Contains((port == 0 ? configuration.Joypad1Right : configuration.Joypad2Right))) state &= (byte)~ControllerInputs.Right;
					if (lastKeysDown.Contains((port == 0 ? configuration.Joypad1Button1 : configuration.Joypad2Button1))) state &= (byte)~ControllerInputs.TL;
					if (lastKeysDown.Contains((port == 0 ? configuration.Joypad1Button2 : configuration.Joypad2Button2))) state &= (byte)~ControllerInputs.TR;
					break;

				case InputDevice.Lightgun:
					if (GetIOControlDirection((port == 0 ? IOControlPort.A : IOControlPort.B), IOControlPin.TH, portIoControl) == IOControlDirection.Input)
					{
						var diffX = Math.Abs(lastMousePosition.x - (vdp.ReadPort(SegaSMSVDP.PortHCounter) << 1));
						var diffY = Math.Abs(lastMousePosition.y - vdp.ReadPort(SegaSMSVDP.PortVCounter));

						if ((diffY <= 5) && (diffX <= 60))
						{
							state &= (byte)~ControllerInputs.TH;
							if (!lightgunLatched)
							{
								hCounterLatched = (byte)(lastMousePosition.x >> 1);
								lightgunLatched = true;
							}
						}
						else
							lightgunLatched = false;
					}

					var mouseButton = (port == 0 ? MouseButtons.Left : MouseButtons.Right);
					if ((lastMouseButtons & mouseButton) == mouseButton)
						state &= (byte)~ControllerInputs.TL;
					break;
			}

			return state;
		}

		private byte ReadResetButton()
		{
			return (!lastKeysDown.Contains(configuration.InputReset) ? inputResetButton : (byte)0x00);
		}

		private IOControlDirection GetIOControlDirection(IOControlPort port, IOControlPin pin, byte data)
		{
			return (IOControlDirection)((data >> (0 | ((byte)port << 1) | (byte)pin)) & 0x01);
		}

		private IOControlOutputLevel GetIOControlOutputLevel(IOControlPort port, IOControlPin pin, byte data)
		{
			return (IOControlOutputLevel)((data >> (4 | ((byte)port << 1) | (byte)pin)) & 0x01);
		}

		private byte ReadIoPort(byte port)
		{
			if ((port & 0x01) == 0)
			{
				/* IO port A/B register */
				var inputCtrlA = ReadInput(0);                              /* Read controller port A */
				var inputCtrlB = ReadInput(1);                              /* Read controller port B */

				if (configuration.Region == Region.Export)
				{
					/* Adjust TR according to direction/level */
					if (GetIOControlDirection(IOControlPort.A, IOControlPin.TR, portIoControl) == IOControlDirection.Output)
					{
						inputCtrlA &= (byte)~ControllerInputs.TR;
						if (GetIOControlOutputLevel(IOControlPort.A, IOControlPin.TR, portIoControl) == IOControlOutputLevel.High)
							inputCtrlA |= (byte)ControllerInputs.TR;
					}
				}

				var portState = (byte)(inputCtrlA & 0x3F);                  /* Controller port A (bits 0-5, into bits 0-5) */
				portState |= (byte)((inputCtrlB & 0x03) << 6);              /* Controller port B (bits 0-1, into bits 6-7) */

				return portState;
			}
			else
			{
				/* IO port B/misc register */
				var inputCtrlA = ReadInput(0);                              /* Read controller port A */
				var inputCtrlB = ReadInput(1);                              /* Read controller port B */

				if (configuration.Region == Region.Export)
				{
					/* Adjust TR and THx according to direction/level */
					if (GetIOControlDirection(IOControlPort.B, IOControlPin.TR, portIoControl) == IOControlDirection.Output)
					{
						inputCtrlB &= (byte)~ControllerInputs.TR;
						if (GetIOControlOutputLevel(IOControlPort.B, IOControlPin.TR, portIoControl) == IOControlOutputLevel.High)
							inputCtrlB |= (byte)ControllerInputs.TR;
					}
					if (GetIOControlDirection(IOControlPort.A, IOControlPin.TH, portIoControl) == IOControlDirection.Output)
					{
						inputCtrlA &= (byte)~ControllerInputs.TH;
						if (GetIOControlOutputLevel(IOControlPort.A, IOControlPin.TH, portIoControl) == IOControlOutputLevel.High)
							inputCtrlA |= (byte)ControllerInputs.TH;
					}
					if (GetIOControlDirection(IOControlPort.B, IOControlPin.TH, portIoControl) == IOControlDirection.Output)
					{
						inputCtrlB &= (byte)~ControllerInputs.TH;
						if (GetIOControlOutputLevel(IOControlPort.B, IOControlPin.TH, portIoControl) == IOControlOutputLevel.High)
							inputCtrlB |= (byte)ControllerInputs.TH;
					}
				}

				var portState = (byte)((inputCtrlB & 0x3F) >> 2);           /* Controller port B (bits 2-5, into bits 0-3) */
				portState |= ReadResetButton();                             /* Reset button (bit 4, into bit 4) */
				portState |= 0b00100000;                                    /* Cartridge slot CONT pin (bit 5, into bit 5) */
				portState |= (byte)(((inputCtrlA >> 6) & 0x01) << 6);       /* Controller port A TH pin (bit 6, into bit 6) */
				portState |= (byte)(((inputCtrlB >> 6) & 0x01) << 7);       /* Controller port B TH pin (bit 6, into bit 7) */

				return portState;
			}
		}

		private byte ReadMemory(ushort address)
		{
			if (address >= 0x0000 && address <= 0xBFFF)
			{
				if (isBootstrapRomEnabled && bootstrap != null)
					return bootstrap.Read(address);

				if (isCartridgeSlotEnabled && cartridge != null)
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
			if (isCartridgeSlotEnabled) cartridge?.Write(address, value);

			if (isWorkRamEnabled && address >= 0xC000 && address <= 0xFFFF)
				wram[address & (ramSize - 1)] = value;
		}

		private byte ReadPort(byte port)
		{
			port = (byte)(port & 0xC1);

			switch (port & 0xF0)
			{
				case 0x00:
					/* Behave like SMS2 */
					return 0xFF;

				case 0x40:
					/* Counters */
					if ((port & 0x01) == 0)
						return vdp.ReadPort(port);      /* V counter */
					else
						return hCounterLatched;         /* H counter */

				case 0x80:
					return vdp.ReadPort(port);          /* VDP ports */

				case 0xC0:
					return ReadIoPort(port);            /* IO ports */

				default:
					// TODO: handle properly
					return 0x00;
			}
		}

		public void WritePort(byte port, byte value)
		{
			port = (byte)(port & 0xC1);

			switch (port & 0xF0)
			{
				case 0x00:
					/* System stuff */
					if ((port & 0x01) == 0)
						portMemoryControl = value;      /* Memory control */
					else
					{
						/* I/O control */
						if ((GetIOControlDirection(IOControlPort.A, IOControlPin.TH, value) == IOControlDirection.Input &&
							GetIOControlOutputLevel(IOControlPort.A, IOControlPin.TH, value) == IOControlOutputLevel.High &&
							GetIOControlOutputLevel(IOControlPort.A, IOControlPin.TH, portIoControl) == IOControlOutputLevel.Low) ||
							(GetIOControlDirection(IOControlPort.B, IOControlPin.TH, value) == IOControlDirection.Input &&
							GetIOControlOutputLevel(IOControlPort.B, IOControlPin.TH, value) == IOControlOutputLevel.High &&
							GetIOControlOutputLevel(IOControlPort.B, IOControlPin.TH, portIoControl) == IOControlOutputLevel.Low))
						{
							/* TH is input and transition is Low->High, latch HCounter */
							hCounterLatched = vdp.ReadPort(SegaSMSVDP.PortHCounter);
						}

						portIoControl = value;
					}
					break;

				case 0x40:
					/* PSG */
					psg.WritePort(port, value);
					break;

				case 0x80:
					/* VDP */
					vdp.WritePort(port, value);
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
