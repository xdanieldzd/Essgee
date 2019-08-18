using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using Essgee.Utilities;

namespace Essgee.Emulation.Configuration
{
	[RootPagePriority(2)]
	public class MasterSystem : IConfiguration
	{
		[DropDownControl("General", "TV Standard", typeof(TVStandard))]
		[JsonConverter(typeof(StringEnumConverter))]
		public TVStandard TVStandard { get; set; }
		[DropDownControl("General", "Region", typeof(Region))]
		[JsonConverter(typeof(StringEnumConverter))]
		public Region Region { get; set; }

		[CheckBoxControl("General", "Use Bootstrap ROM")]
		public bool UseBootstrap { get; set; }
		[IsBootstrapRomPath]
		[FileBrowserControl("General", "Bootstrap Path", "SMS Bootstrap ROM (*.sms;*.zip)|*.sms;*.zip")]
		public string BootstrapRom { get; set; }

		[DropDownControl("General", "Pause Button", typeof(Keys), Keys.F11)]
		[JsonConverter(typeof(StringEnumConverter))]
		public Keys InputPause { get; set; }
		[DropDownControl("General", "Reset Button", typeof(Keys), Keys.F11)]
		[JsonConverter(typeof(StringEnumConverter))]
		public Keys InputReset { get; set; }

		[DropDownControl("Controller Port 1", "Device Type", typeof(InputDevice))]
		[JsonConverter(typeof(StringEnumConverter))]
		public InputDevice Joypad1DeviceType { get; set; }
		[DropDownControl("Controller Port 1", "D-Pad Up", typeof(Keys), Keys.F11)]
		[JsonConverter(typeof(StringEnumConverter))]
		public Keys Joypad1Up { get; set; }
		[DropDownControl("Controller Port 1", "D-Pad Down", typeof(Keys), Keys.F11)]
		[JsonConverter(typeof(StringEnumConverter))]
		public Keys Joypad1Down { get; set; }
		[DropDownControl("Controller Port 1", "D-Pad Left", typeof(Keys), Keys.F11)]
		[JsonConverter(typeof(StringEnumConverter))]
		public Keys Joypad1Left { get; set; }
		[DropDownControl("Controller Port 1", "D-Pad Right", typeof(Keys), Keys.F11)]
		[JsonConverter(typeof(StringEnumConverter))]
		public Keys Joypad1Right { get; set; }
		[DropDownControl("Controller Port 1", "Button 1", typeof(Keys), Keys.F11)]
		[JsonConverter(typeof(StringEnumConverter))]
		public Keys Joypad1Button1 { get; set; }
		[DropDownControl("Controller Port 1", "Button 2", typeof(Keys), Keys.F11)]
		[JsonConverter(typeof(StringEnumConverter))]
		public Keys Joypad1Button2 { get; set; }

		[DropDownControl("Controller Port 2", "Device Type", typeof(InputDevice))]
		[JsonConverter(typeof(StringEnumConverter))]
		public InputDevice Joypad2DeviceType { get; set; }
		[DropDownControl("Controller Port 2", "D-Pad Up", typeof(Keys), Keys.F11)]
		[JsonConverter(typeof(StringEnumConverter))]
		public Keys Joypad2Up { get; set; }
		[DropDownControl("Controller Port 2", "D-Pad Down", typeof(Keys), Keys.F11)]
		[JsonConverter(typeof(StringEnumConverter))]
		public Keys Joypad2Down { get; set; }
		[DropDownControl("Controller Port 2", "D-Pad Left", typeof(Keys), Keys.F11)]
		[JsonConverter(typeof(StringEnumConverter))]
		public Keys Joypad2Left { get; set; }
		[DropDownControl("Controller Port 2", "D-Pad Right", typeof(Keys), Keys.F11)]
		[JsonConverter(typeof(StringEnumConverter))]
		public Keys Joypad2Right { get; set; }
		[DropDownControl("Controller Port 2", "Button 1", typeof(Keys), Keys.F11)]
		[JsonConverter(typeof(StringEnumConverter))]
		public Keys Joypad2Button1 { get; set; }
		[DropDownControl("Controller Port 2", "Button 2", typeof(Keys), Keys.F11)]
		[JsonConverter(typeof(StringEnumConverter))]
		public Keys Joypad2Button2 { get; set; }

		public MasterSystem()
		{
			BootstrapRom = string.Empty;

			TVStandard = TVStandard.NTSC;
			Region = Region.Export;

			InputPause = Keys.Space;
			InputReset = Keys.Back;

			Joypad1DeviceType = InputDevice.Controller;
			Joypad1Up = Keys.Up;
			Joypad1Down = Keys.Down;
			Joypad1Left = Keys.Left;
			Joypad1Right = Keys.Right;
			Joypad1Button1 = Keys.A;
			Joypad1Button2 = Keys.S;

			Joypad2DeviceType = InputDevice.Controller;
			Joypad2Up = Keys.NumPad8;
			Joypad2Down = Keys.NumPad2;
			Joypad2Left = Keys.NumPad4;
			Joypad2Right = Keys.NumPad6;
			Joypad2Button1 = Keys.NumPad1;
			Joypad2Button2 = Keys.NumPad3;
		}
	}
}
