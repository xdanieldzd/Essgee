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

	public enum VDPTypes
	{
		[Description("Mark III / Master System")]
		Mk3SMS1 = 0,
		[Description("Master System II / Game Gear")]
		SMS2GG = 1
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

	[Flags]
	public enum GraphicsEnableState
	{
		[Description("Backgrounds")]
		Backgrounds = (1 << 0),
		[Description("Sprites")]
		Sprites = (1 << 1),
		[Description("Borders")]
		Borders = (1 << 2),

		[ValueIgnored(true)]
		All = (Backgrounds | Sprites | Borders)
	}

	[Flags]
	public enum SoundEnableState
	{
		[Description("Tone Channel 1")]
		ToneChannel1 = (1 << 0),
		[Description("Tone Channel 2")]
		ToneChannel2 = (1 << 1),
		[Description("Tone Channel 3")]
		ToneChannel3 = (1 << 2),
		[Description("Noise Channel")]
		NoiseChannel = (1 << 3),

		[ValueIgnored(true)]
		All = (ToneChannel1 | ToneChannel2 | ToneChannel3 | NoiseChannel)
	}
}
