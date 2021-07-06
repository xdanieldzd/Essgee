# Essgee
Essgee is an emulator for various 8-bit consoles and handhelds, mostly by Sega, supporting the Sega SG-1000, SC-3000 (partially), Mark III/Master System and Game Gear, as well as the Coleco ColecoVision (partially), and Nintendo Game Boy and Game Boy Color (partially). It is written in C# and uses .NET Framework v4.7.1, [OpenTK](https://www.nuget.org/packages/OpenTK) and [OpenTK.GLControl](https://www.nuget.org/packages/OpenTK.GLControl) for graphics and sound output, [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json) for JSON parsing, as well as [MSBuildTasks](https://www.nuget.org/packages/MSBuildTasks) for including build information.

It also improves just enough on [its](https://github.com/xdanieldzd/MasterFudge) [predecessors](https://github.com/xdanieldzd/MasterFudgeMk2) to no longer be called "fudged", hence its new, pun-y name.

### Table of Contents ###
* [Usage](#usage): How to use Essgee
* [Status](#status): Current emulation accuracy, emulated features, etc.
* [Notes](#notes): Issues and other caveats
* [Screenshots](#screenshots)
* [Acknowledgements & Attribution](#acknowledgements--attribution)

## Usage
Run `Essgee.exe` to start the emulator. Open a ROM to run via the __File__ menu, recently opened files can also be found there. The emulator recognizes ROMs for each emulated system by the file extension, so ensure SG-1000 ROMs are suffixed `.sg`, SC-3000 ones `.sc`, Mark III/Master System ones `.sms`, Game Gear ones `.gg`, ColecoVision ones `.col`, Game Boy ones `.gb` and Game Boy Color ones `.gbc`. Zipped files can be loaded as well; in that case, the first file in the archive recognized as a valid ROM will be loaded.

Emulator settings can be changed via the __Options__ menu, each option should be self-explanatory. System-specific settings can be found in the __Settings__ dialog. These include controller configurations, region and TV standard selection, bootstrap ROM paths and similar things.

The emulation can be controlled via the __Emulation__ menu. Once a game is running, it can be paused, reset or stopped from there. This menu also contains the __Power On__ submenu, which allows powering up a system with no cartridge inserted, if a bootstrap ROM image is selected. Systems that do not contain a bootstrap ROM cannot be booted like this. Save states can be loaded and saved from the __Load State__ and __Save State__ submenus; eight slots are available, each slot listing the date and time the respective save state was last written to.

Fullscreen mode can be toggled via the __F11__ key. This is currently hardcoded and thus cannot be configured.

Certain ROMs have configuration overrides, stored in the file `Assets\MetadataDatabase.json`, which are automatically applied when loading such a file. These include, for example, specific mappers (ex. for Codemasters games), region and TV standard overrides, and RAM size settings. These games might otherwise not work or misbehave, hence the need for this file. They're not game-specific hacks, but rather workaround for when the game won't work with ex. the default TV standard specified in the settings, because it was made for 50 Hz PAL systems and does not run correctly at 60 Hz.

The shader __Basic__ is integrated into the emulator, while the others are stored in the `Assets\Shaders` directory. Essgee's shader system is comparatively limited in features and not compatible with shaders from other emulators.

The SC-3000 keyboard is (currently) not configurable; the current layout can be found in the source code, in the file `Essgee\Emulation\Machines\SC3000.cs`, although you'll also need the layout of the SC-3000 keyboard matrix to make sense of it like this. Because the controllers and keyboard are both emulated via the PC keyboard, a toggle has been added to allow either the emulated keyboard or the controllers to be active. Press the input mode key (as configured via the SC-3000 tab in the __Settings__ dialog) to switch between keyboard or controller inputs.

## Status

### CPUs
* __Zilog Z80__: All opcodes implemented, documented and undocumented; the two undocumented flags are (possibly?) not fully supported yet; disassembly features incomplete
* __Sharp SM83__: Core of Game Boy and Game Boy Color CPUs; all opcodes implemented; disassembly features incomplete

### Graphics
* __Texas Instruments TMS9918A__: Scanline-based, not fully accurate and possibly with some bugs here and there; still missing the multicolor graphics mode
  * __Sega 315-5124__ and __315-5246__: Mark III/Master System and Master System II VDPs, TMS9918A with additional graphics mode, line interrupts, etc.; also not fully accurate, also currently emulating a bit of a hybrid of both
  * __Sega 315-5378__: Game Gear VDP based on Master System II VDP, with higher color depth, etc.; also not fully accurate
* __Nintendo DMG__: Original Game Boy graphics system; pixel-based, with certain inaccuracies and possibly some bugs
  * __Nintendo CGB__: Game Boy Color graphics system; similar status as DMG

### Sound
* __Texas Instruments SN76489__: Fully emulated, accuracy is probably not very high, but still sounds decent enough
  * __Sega 315-5246__: Master System II PSG (integrated into VDP chip), SN76489 with minor differences in noise channel; same issues as SN76489
  * __Sega 315-5378__: Game Gear PSG (integrated into VDP) based on Master System II PSG, with stereo output extension; same issues as other PSGs
* __Nintendo DMG__: Game Boy sound system; fully emulated but likely not 100% accurate
  * __Nintendo CGB__: Game Boy Color sound system; similar status as DMG

### Support Chips
* __Intel 8255__: Peripheral interface chip used in the SC-3000; not fully tested nor accurate, enough for controller and keyboard support where applicable

### Media
* Support for various cartridge types, ex. standard Sega mapper, Codemasters mapper, various Korean mappers, various Game Boy MBCs and Game Boy Camera

### External Devices
* Support for Game Boy Printer and partial support for Game Boy Color infrared port, including Pocket Pikachu Color data transfer

### Input Devices
* __SG-1000__: Standard controllers
* __SC-3000__: Standard controllers and integrated keyboard
  * Switch between controller and keyboard input using the Change Input Mode key, defaults to F1
  * Keyboard layout is (currently?) not user-configurable
* __Mark III/Master System__: Standard controllers and/or Light Phaser
  * Light Phaser support is still somewhat rudimentary
* __Game Gear__: Integrated controls
* __ColecoVision__: Standard controllers
* __Game Boy__: Integrated controls
* __Game Boy Color__: Integrated controls

## Notes
* Overall accuracy of the emulation is nowhere near exact, but it is certainly accurate enough to play many games quite well
* Sound output _might_ stutter from time to time, the corresponding sound management code isn't too great
* The framerate limiter and FPS counter are somewhat inaccurate and might contribute to the aforementioned sound stuttering issues
* Pocket Pikachu Color requires data file from [GB Enhanced+](https://github.com/shonumi/gbe-plus) by [shonumi](https://github.com/shonumi)

## Screenshots
* __Girl's Garden__ (SG-1000, using Pseudo-Monitor shader):<br><br>
 ![Screenshot 1](https://raw.githubusercontent.com/xdanieldzd/Essgee/master/Screenshots/SG1000-Garden.png)<br><br>
* __Sega SC-3000 BASIC Level 3__ (SC-3000, using Pseudo-Monitor shader):<br><br>
 ![Screenshot 2](https://raw.githubusercontent.com/xdanieldzd/Essgee/master/Screenshots/SC3000-BasicLv3.png)<br><br>
* __Donkey Kong__ (ColecoVision, using Pseudo-Monitor shader):<br><br>
 ![Screenshot 5](https://raw.githubusercontent.com/xdanieldzd/Essgee/master/Screenshots/CV-Donkey.png)<br><br>
* __Sonic the Hedgehog__ (Master System, using Pseudo-Monitor shader):<br><br>
 ![Screenshot 3](https://raw.githubusercontent.com/xdanieldzd/Essgee/master/Screenshots/SMS-Sonic1.png)<br><br>
* __GG Aleste II / Power Strike II__ (Game Gear, using LCD-Blur shader):<br><br>
 ![Screenshot 4](https://raw.githubusercontent.com/xdanieldzd/Essgee/master/Screenshots/GG-AlesteII.png)<br><br>

## Acknowledgements & Attribution
* Essgee uses [DejaVu](https://dejavu-fonts.github.io) Sans Condensed as its OSD font; see the [DejaVu Fonts License](https://dejavu-fonts.github.io/License.html) for applicable information.
* The XML data files in `Assets\No-Intro` were created by the [No-Intro](http://www.no-intro.org) project; see the [DAT-o-MATIC website](https://datomatic.no-intro.org) for official downloads.
* The file `EssgeeIcon.ico` is derived from "[Sg1000.jpg](https://segaretro.org/File:Sg1000.jpg)" on [Sega Retro](https://segaretro.org), in revision from 16 March 2011 by [Black Squirrel](https://segaretro.org/User:Black_Squirrel), and used under [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). The image was edited to remove the controller and text, then resized for use as the application icon.
