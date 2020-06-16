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
	[ElementPriorityAttribute(4)]
	public class ColecoVision : IConfiguration
	{
		[IsBootstrapRomPath]
		[FileBrowserControl("General", "BIOS Path", "ColecoVision BIOS ROM (*.col;*.zip)|*.col;*.zip")]
		public string BiosRom { get; set; }

		[DropDownControl("Controls", "Up", typeof(Keys), Keys.F11)]
		[JsonConverter(typeof(StringEnumConverter))]
		public Keys ControlsUp { get; set; }
		[DropDownControl("Controls", "Down", typeof(Keys), Keys.F11)]
		[JsonConverter(typeof(StringEnumConverter))]
		public Keys ControlsDown { get; set; }
		[DropDownControl("Controls", "Left", typeof(Keys), Keys.F11)]
		[JsonConverter(typeof(StringEnumConverter))]
		public Keys ControlsLeft { get; set; }
		[DropDownControl("Controls", "Right", typeof(Keys), Keys.F11)]
		[JsonConverter(typeof(StringEnumConverter))]
		public Keys ControlsRight { get; set; }
		[DropDownControl("Controls", "Left Button", typeof(Keys), Keys.F11)]
		[JsonConverter(typeof(StringEnumConverter))]
		public Keys ControlsButtonLeft { get; set; }
		[DropDownControl("Controls", "Right Button", typeof(Keys), Keys.F11)]
		[JsonConverter(typeof(StringEnumConverter))]
		public Keys ControlsButtonRight { get; set; }

		[DropDownControl("Controls", "Keypad 1", typeof(Keys), Keys.F11)]
		[JsonConverter(typeof(StringEnumConverter))]
		public Keys ControlsKeypad1 { get; set; }
		[DropDownControl("Controls", "Keypad 2", typeof(Keys), Keys.F11)]
		[JsonConverter(typeof(StringEnumConverter))]
		public Keys ControlsKeypad2 { get; set; }
		[DropDownControl("Controls", "Keypad 3", typeof(Keys), Keys.F11)]
		[JsonConverter(typeof(StringEnumConverter))]
		public Keys ControlsKeypad3 { get; set; }
		[DropDownControl("Controls", "Keypad 4", typeof(Keys), Keys.F11)]
		[JsonConverter(typeof(StringEnumConverter))]
		public Keys ControlsKeypad4 { get; set; }
		[DropDownControl("Controls", "Keypad 5", typeof(Keys), Keys.F11)]
		[JsonConverter(typeof(StringEnumConverter))]
		public Keys ControlsKeypad5 { get; set; }
		[DropDownControl("Controls", "Keypad 6", typeof(Keys), Keys.F11)]
		[JsonConverter(typeof(StringEnumConverter))]
		public Keys ControlsKeypad6 { get; set; }
		[DropDownControl("Controls", "Keypad 7", typeof(Keys), Keys.F11)]
		[JsonConverter(typeof(StringEnumConverter))]
		public Keys ControlsKeypad7 { get; set; }
		[DropDownControl("Controls", "Keypad 8", typeof(Keys), Keys.F11)]
		[JsonConverter(typeof(StringEnumConverter))]
		public Keys ControlsKeypad8 { get; set; }
		[DropDownControl("Controls", "Keypad 9", typeof(Keys), Keys.F11)]
		[JsonConverter(typeof(StringEnumConverter))]
		public Keys ControlsKeypad9 { get; set; }
		[DropDownControl("Controls", "Keypad 0", typeof(Keys), Keys.F11)]
		[JsonConverter(typeof(StringEnumConverter))]
		public Keys ControlsKeypad0 { get; set; }
		[DropDownControl("Controls", "Keypad *", typeof(Keys), Keys.F11)]
		[JsonConverter(typeof(StringEnumConverter))]
		public Keys ControlsKeypadStar { get; set; }
		[DropDownControl("Controls", "Keypad #", typeof(Keys), Keys.F11)]
		[JsonConverter(typeof(StringEnumConverter))]
		public Keys ControlsKeypadNumberSign { get; set; }

		public ColecoVision()
		{
			BiosRom = string.Empty;

			ControlsUp = Keys.Up;
			ControlsDown = Keys.Down;
			ControlsLeft = Keys.Left;
			ControlsRight = Keys.Right;
			ControlsButtonLeft = Keys.A;
			ControlsButtonRight = Keys.S;

			ControlsKeypad1 = Keys.NumPad1;
			ControlsKeypad2 = Keys.NumPad2;
			ControlsKeypad3 = Keys.NumPad3;
			ControlsKeypad4 = Keys.NumPad4;
			ControlsKeypad5 = Keys.NumPad5;
			ControlsKeypad6 = Keys.NumPad6;
			ControlsKeypad7 = Keys.NumPad7;
			ControlsKeypad8 = Keys.NumPad8;
			ControlsKeypad9 = Keys.NumPad9;
			ControlsKeypad0 = Keys.NumPad0;
			ControlsKeypadStar = Keys.Multiply;
			ControlsKeypadNumberSign = Keys.Divide;
		}
	}
}
