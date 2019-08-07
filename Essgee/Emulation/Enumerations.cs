using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace Essgee.Emulation
{
	public enum TVStandard
	{
		[ValueIgnored(true)]
		Auto = -1,
		[Description("NTSC (60 Hz)")]
		NTSC = 0,
		[Description("PAL (50 Hz)")]
		PAL
	}
	public enum Region
	{
		[ValueIgnored(true)]
		Auto = -1,
		[Description("Domestic (Japan)")]
		Domestic = 0,
		[Description("Export")]
		Export
	}

	public enum InputDevice
	{
		[Description("None")]
		None = 0,
		[Description("Standard Controller")]
		Controller,
		[Description("Light Phaser")]
		Lightgun
	}

	public enum InterruptType
	{
		Maskable,
		NonMaskable
	}

	public enum InterruptState
	{
		Clear,
		Assert
	}
}
