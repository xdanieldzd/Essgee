using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Essgee.Emulation.Configuration
{
	[ElementPriority(1)]
	public class SC3000 : IConfiguration
	{
		[DropDownControl("General", "TV Standard", typeof(TVStandard))]
		[JsonConverter(typeof(StringEnumConverter))]
		public TVStandard TVStandard { get; set; }

		[DropDownControl("General", "Reset Button", typeof(Keys), Keys.F11)]
		[JsonConverter(typeof(StringEnumConverter))]
		public Keys InputReset { get; set; }

		[DropDownControl("General", "Change Input Mode", typeof(Keys), Keys.F11, Tooltip = "Selects which PC keyboard key is used to switch between SC-3000 keyboard and controller input.")]
		[JsonConverter(typeof(StringEnumConverter))]
		public Keys InputChangeMode { get; set; }

		[DropDownControl("General", "Play Tape", typeof(Keys), Keys.F11, Tooltip = "Note that tape emulation is currently non-functional.")]
		[JsonConverter(typeof(StringEnumConverter))]
		public Keys InputPlayTape { get; set; }

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

		public SC3000()
		{
			TVStandard = TVStandard.NTSC;

			InputReset = Keys.F12;
			InputChangeMode = Keys.F1;
			InputPlayTape = Keys.F2;

			Joypad1Up = Keys.Up;
			Joypad1Down = Keys.Down;
			Joypad1Left = Keys.Left;
			Joypad1Right = Keys.Right;
			Joypad1Button1 = Keys.A;
			Joypad1Button2 = Keys.S;

			Joypad2Up = Keys.NumPad8;
			Joypad2Down = Keys.NumPad2;
			Joypad2Left = Keys.NumPad4;
			Joypad2Right = Keys.NumPad6;
			Joypad2Button1 = Keys.NumPad1;
			Joypad2Button2 = Keys.NumPad3;
		}
	}
}
