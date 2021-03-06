﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Globalization;

using OpenTK.Graphics.OpenGL;

using Essgee.Debugging;
using Essgee.Graphics;
using Essgee.Sound;
using Essgee.Emulation;
using Essgee.Emulation.Machines;
using Essgee.EventArguments;
using Essgee.Exceptions;
using Essgee.Extensions;
using Essgee.Metadata;
using Essgee.Utilities;
using Essgee.Utilities.XInput;

namespace Essgee
{
	public partial class MainForm : Form
	{
		readonly static int maxScreenSizeFactor = 5;
		readonly static int maxSampleRateFactor = 3;
		readonly static int maxSaveStateCount = 8;

		object uiLock = new object();

		// https://stackoverflow.com/a/21319086
		private bool cursorShown = true;
		public bool CursorShown
		{
			get { return cursorShown; }
			set
			{
				if (value == cursorShown) return;

				if (value) Cursor.Show();
				else Cursor.Hide();

				cursorShown = value;
			}
		}

		OnScreenDisplayHandler onScreenDisplayHandler;

		GraphicsHandler graphicsHandler;
		SoundHandler soundHandler;
		GameMetadataHandler gameMetadataHandler;

		GameMetadata lastGameMetadata;

		EmulatorHandler emulatorHandler;

		SoundDebuggerForm soundDebuggerForm;

		bool lastUserPauseState;
		(int x, int y, int width, int height) currentViewport;
		double currentPixelAspectRatio;
		byte[] lastFramebufferData;
		(int width, int height) lastFramebufferSize;

		IMessageFilter altMessageFilter;
		bool fullScreen;

		List<Keys> keysDown;
		MouseButtons mouseButtonsDown;
		(int x, int y) mousePosition;

		public MainForm()
		{
			InitializeComponent();

			if (!Program.AppEnvironment.DebugMode)
			{
				AppDomain.CurrentDomain.UnhandledException += (s, e) =>
				{
					var ex = (e.ExceptionObject as Exception);
					ex.Data.Add("Thread", System.Threading.Thread.CurrentThread.Name);
					ex.Data.Add("IsUnhandled", true);
					ExceptionHandler(ex);
				};
			}

			currentViewport = (0, 0, 128, 128);
			currentPixelAspectRatio = 4.0 / 3.0;

			SizeAndPositionWindow();
			SetWindowTitleAndStatus();

			SetFileFilters();

			CreateRecentFilesMenu();
			CreatePowerOnMenu();
			CreateShaderMenu();
			CreateScreenSizeMenu();
			CreateSizeModeMenu();
			CreateSampleRateMenu();

			automaticPauseToolStripMenuItem.DataBindings.Add(nameof(automaticPauseToolStripMenuItem.Checked), Program.Configuration, nameof(Program.Configuration.AutoPause), false, DataSourceUpdateMode.OnPropertyChanged);

			limitFPSToolStripMenuItem.DataBindings.Add(nameof(limitFPSToolStripMenuItem.Checked), Program.Configuration, nameof(Program.Configuration.LimitFps), false, DataSourceUpdateMode.OnPropertyChanged);
			limitFPSToolStripMenuItem.CheckedChanged += (s, e) => { emulatorHandler?.SetFpsLimiting(Program.Configuration.LimitFps); };

			showFPSToolStripMenuItem.DataBindings.Add(nameof(showFPSToolStripMenuItem.Checked), Program.Configuration, nameof(Program.Configuration.ShowFps), false, DataSourceUpdateMode.OnPropertyChanged);

			muteToolStripMenuItem.DataBindings.Add(nameof(muteToolStripMenuItem.Checked), Program.Configuration, nameof(Program.Configuration.Mute), false, DataSourceUpdateMode.OnPropertyChanged);
			muteToolStripMenuItem.CheckedChanged += (s, e) => { soundHandler?.SetMute(Program.Configuration.Mute); };

			lowPassFilterToolStripMenuItem.DataBindings.Add(nameof(lowPassFilterToolStripMenuItem.Checked), Program.Configuration, nameof(Program.Configuration.LowPassFilter), false, DataSourceUpdateMode.OnPropertyChanged);
			lowPassFilterToolStripMenuItem.CheckedChanged += (s, e) => { soundHandler?.SetLowPassFilter(Program.Configuration.LowPassFilter); };

			useXInputControllerToolStripMenuItem.DataBindings.Add(nameof(useXInputControllerToolStripMenuItem.Checked), Program.Configuration, nameof(Program.Configuration.EnableXInput), false, DataSourceUpdateMode.OnPropertyChanged);

			enableXInputVibrationToolStripMenuItem.DataBindings.Add(nameof(enableXInputVibrationToolStripMenuItem.Checked), Program.Configuration, nameof(Program.Configuration.EnableRumble), false, DataSourceUpdateMode.OnPropertyChanged);

			foreach (ToolStripMenuItem sizeMenuItem in screenSizeToolStripMenuItem.DropDownItems)
				sizeMenuItem.Click += (s, e) => { Program.Configuration.ScreenSize = (int)(s as ToolStripMenuItem).Tag; SizeAndPositionWindow(); };

			renderControl.LostFocus += (s, e) => { SetTemporaryPause(true); };
			renderControl.GotFocus += (s, e) => { SetTemporaryPause(false); };
			menuStrip.MenuActivate += (s, e) => { SetTemporaryPause(true); };
			menuStrip.MenuDeactivate += (s, e) => { SetTemporaryPause(false); };
			ResizeBegin += (s, e) => { SetTemporaryPause(true); };
			ResizeEnd += (s, e) => { SetTemporaryPause(false); };
			Move += (s, e) => { SetTemporaryPause(true); };

			altMessageFilter = new AltKeyFilter();
			fullScreen = false;

			keysDown = new List<Keys>();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (components != null) components.Dispose();

				if (onScreenDisplayHandler != null) onScreenDisplayHandler.Dispose();
				if (graphicsHandler != null) graphicsHandler.Dispose();
				if (soundHandler != null) soundHandler.Dispose();
			}

			base.Dispose(disposing);
		}

		private void ExceptionHandler(Exception ex)
		{
			this.CheckInvokeMethod(() =>
			{
				if (!Program.AppEnvironment.TemporaryDisableCustomExceptionForm)
				{
					(_, ExceptionResult result, string prefix, string postfix) = ExceptionForm.GetExceptionInfo(ex);

					if (result == ExceptionResult.Continue)
					{
						MessageBox.Show($"{prefix}{ex.InnerException?.Message ?? ex.Message}\n\n{postfix}.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					}
					else
					{
						var exceptionForm = new ExceptionForm(ex) { Owner = this };
						exceptionForm.ShowDialog();

						switch (result)
						{
							case ExceptionResult.StopEmulation:
								SignalStopEmulation();
								break;

							case ExceptionResult.ExitApplication:
								Environment.Exit(-1);
								break;
						}
					}
				}
				else
				{
					var exceptionInfoBuilder = new StringBuilder();
					exceptionInfoBuilder.AppendLine($"Thread: {ex.Data["Thread"] ?? "<unnamed>"}");
					exceptionInfoBuilder.AppendLine($"Function: {ex.TargetSite.ReflectedType.FullName}.{ex.TargetSite.Name}");
					exceptionInfoBuilder.AppendLine($"Exception: {ex.GetType().Name}");
					exceptionInfoBuilder.Append($"Message: {ex.Message}");

					var isUnhandled = Convert.ToBoolean(ex.Data["IsUnhandled"]);

					if (!isUnhandled && ex is CartridgeLoaderException)
					{
						MessageBox.Show($"{ ex.InnerException?.Message ?? ex.Message}\n\nFailed to load cartridge.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					}
					else if (!isUnhandled && ex is EmulationException)
					{
						MessageBox.Show($"An emulation exception has occured!\n\n{exceptionInfoBuilder.ToString()}\n\nEmulation cannot continue and will be terminated.", "Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
						SignalStopEmulation();
					}
					else
					{
						var errorBuilder = new StringBuilder();
						errorBuilder.AppendLine("An unhandled exception has occured!");
						errorBuilder.AppendLine();
						errorBuilder.AppendLine(exceptionInfoBuilder.ToString());
						errorBuilder.AppendLine();
						errorBuilder.AppendLine("Exception occured:");
						errorBuilder.AppendLine($"{ex.StackTrace}");
						errorBuilder.AppendLine();
						errorBuilder.AppendLine("Execution cannot continue and the application will be terminated.");

						MessageBox.Show(errorBuilder.ToString(), "Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);

						Environment.Exit(-1);
					}
				}
			});
		}

		protected override void WndProc(ref Message m)
		{
			base.WndProc(ref m);

			switch (m.Msg)
			{
				// WM_NCLBUTTONDBLCLK -- double-click in nonclient area of window (title bar, frame, etc)
				case 0xA3:
					SetTemporaryPause(false);
					break;

				// WM_SYSCOMMAND -- command in Window/aka system/aka control menu
				case 0x0112:
					if (m.WParam == new IntPtr(0xF030) || m.WParam == new IntPtr(0xF120))
					{
						// SC_MAXIMIZE or SC_RESTORE -- maximize or restore window
						SetTemporaryPause(false);
					}
					else if (m.WParam == new IntPtr(0xF020))
					{
						// SC_MINIMIZE -- minimize window
						SetTemporaryPause(true);
					}
					break;
			}
		}

		private void InitializeHandlers()
		{
			InitializeOSDHandler();
			InitializeGraphicsHandler();
			InitializeSoundHandler();
			InitializeMetadataHandler();
		}

		private void InitializeOSDHandler()
		{
			var osdFontText = Assembly.GetExecutingAssembly().ReadEmbeddedImageFile($"{Application.ProductName}.Assets.OsdFont.png");
			onScreenDisplayHandler = new OnScreenDisplayHandler(osdFontText);

			onScreenDisplayHandler?.EnqueueMessageDebug($"Hello from {GetProductNameAndVersionString(true)}, this is a debug build!\nOSD handler initialized; font bitmap is {osdFontText.Width}x{osdFontText.Height}.");

			if (onScreenDisplayHandler == null) throw new HandlerException("Failed to initialize OSD handler");
		}

		private void InitializeGraphicsHandler()
		{
			graphicsHandler = new GraphicsHandler(onScreenDisplayHandler);
			graphicsHandler?.LoadShaderBundle(Program.Configuration.LastShader);
		}

		private void InitializeSoundHandler()
		{
			soundHandler = new SoundHandler(onScreenDisplayHandler, Program.Configuration.SampleRate, 2, ExceptionHandler);
			soundHandler.SetVolume(Program.Configuration.Volume);
			soundHandler.SetMute(Program.Configuration.Mute);
			soundHandler.SetLowPassFilter(Program.Configuration.LowPassFilter);
			soundHandler.Startup();
		}

		private void InitializeMetadataHandler()
		{
			gameMetadataHandler = new GameMetadataHandler(onScreenDisplayHandler);
		}

		private void InitializeDebuggers()
		{
			soundDebuggerForm = new SoundDebuggerForm() { Location = Program.Configuration.DebugWindows[typeof(SoundDebuggerForm).Name] };
		}

		private void ShutdownDebuggers()
		{
			soundDebuggerForm.Close();
		}

		private void InitializeEmulation(Type machineType)
		{
			if (emulatorHandler != null)
				ShutdownEmulation();

			emulatorHandler = new EmulatorHandler(machineType, ExceptionHandler);
			emulatorHandler.Initialize();

			emulatorHandler.SendLogMessage += EmulatorHandler_SendLogMessage;
			emulatorHandler.EmulationReset += EmulatorHandler_EmulationReset;
			emulatorHandler.RenderScreen += EmulatorHandler_RenderScreen;
			emulatorHandler.SizeScreen += EmulatorHandler_SizeScreen;
			emulatorHandler.ChangeViewport += EmulatorHandler_ChangeViewport;
			emulatorHandler.PollInput += EmulatorHandler_PollInput;
			emulatorHandler.EnqueueSamples += soundHandler.EnqueueSamples;
			emulatorHandler.SaveExtraData += EmulatorHandler_SaveExtraData;
			emulatorHandler.EnableRumble += EmulatorHandler_EnableRumble;
			emulatorHandler.PauseChanged += EmulatorHandler_PauseChanged;

			emulatorHandler.EnqueueSamples += soundDebuggerForm.EnqueueSamples;

			emulatorHandler.SetFpsLimiting(Program.Configuration.LimitFps);

			emulatorHandler.SetConfiguration(Program.Configuration.Machines[machineType.Name]);

			currentPixelAspectRatio = emulatorHandler.Information.PixelAspectRatio;

			pauseToolStripMenuItem.DataBindings.Clear();
			pauseToolStripMenuItem.CheckedChanged += (s, e) =>
			{
				var pauseState = (s as ToolStripMenuItem).Checked;

				emulatorHandler.Pause(pauseState);
				lastUserPauseState = pauseState;
			};

			onScreenDisplayHandler.EnqueueMessageSuccess($"{emulatorHandler.Information.Manufacturer} {emulatorHandler.Information.Model} emulation initialized.");
		}

		private void SetTemporaryPause(bool newTemporaryPauseState)
		{
			if (emulatorHandler == null || !emulatorHandler.IsRunning || !Program.Configuration.AutoPause) return;

			if (newTemporaryPauseState)
				emulatorHandler.Pause(true);
			else if (!lastUserPauseState)
				emulatorHandler.Pause(false);
		}

		private void PowerOnWithoutCartridge(Type machineType)
		{
			if (soundHandler.IsRecording)
				soundHandler.CancelRecording();

			InitializeEmulation(machineType);

			lastGameMetadata = null;

			ApplyConfigOverrides(machineType);

			CreateToggleGraphicsLayersMenu();
			CreateToggleSoundChannelsMenu();

			takeScreenshotToolStripMenuItem.Enabled = pauseToolStripMenuItem.Enabled = resetToolStripMenuItem.Enabled = stopToolStripMenuItem.Enabled = true;
			loadStateToolStripMenuItem.Enabled = saveStateToolStripMenuItem.Enabled = false;
			startRecordingToolStripMenuItem.Enabled = true;
			toggleLayersToolStripMenuItem.Enabled = enableChannelsToolStripMenuItem.Enabled = true;

			emulatorHandler.Startup();

			SizeAndPositionWindow();
			SetWindowTitleAndStatus();

			onScreenDisplayHandler.EnqueueMessage("Power on without cartridge.");
		}

		private void LoadAndRunCartridge(string fileName)
		{
			try
			{
				var (machineType, romData) = CartridgeLoader.Load(fileName, "ROM image");

				if (soundHandler.IsRecording)
					soundHandler.CancelRecording();

				InitializeEmulation(machineType);

				lastGameMetadata = gameMetadataHandler.GetGameMetadata(emulatorHandler.Information.DatFileName, fileName, Crc32.Calculate(romData), romData.Length);

				ApplyConfigOverrides(machineType);

				emulatorHandler.LoadCartridge(romData, lastGameMetadata);

				AddToRecentFiles(fileName);
				CreateRecentFilesMenu();
				CreateLoadSaveStateMenus();
				CreateToggleGraphicsLayersMenu();
				CreateToggleSoundChannelsMenu();

				takeScreenshotToolStripMenuItem.Enabled = pauseToolStripMenuItem.Enabled = resetToolStripMenuItem.Enabled = stopToolStripMenuItem.Enabled = true;
				loadStateToolStripMenuItem.Enabled = saveStateToolStripMenuItem.Enabled = true;
				startRecordingToolStripMenuItem.Enabled = true;
				toggleLayersToolStripMenuItem.Enabled = enableChannelsToolStripMenuItem.Enabled = true;

				emulatorHandler.Startup();

				SizeAndPositionWindow();
				SetWindowTitleAndStatus();

				onScreenDisplayHandler.EnqueueMessage($"Loaded '{lastGameMetadata?.KnownName ?? "unrecognized game"}'.");
			}
			catch (Exception ex) when (!Program.AppEnvironment.DebugMode)
			{
				ExceptionHandler(ex);
			}
		}

		private void ApplyConfigOverrides(Type machineType)
		{
			var forcePowerOnWithoutCart = false;
			var hasTVStandardOverride = false;
			var hasRegionOverride = false;
			var hasDisallowMemoryControlOverride = false;

			var overrideConfig = Program.Configuration.Machines[machineType.Name].CloneObject();

			if (lastGameMetadata == null)
			{
				var property = overrideConfig.GetType().GetProperty("UseBootstrap");
				if (property != null && (bool)property.GetValue(overrideConfig) != true)
				{
					property.SetValue(overrideConfig, true);
					forcePowerOnWithoutCart = true;
				}
			}

			if (lastGameMetadata != null && lastGameMetadata.PreferredTVStandard != TVStandard.Auto)
			{
				var property = overrideConfig.GetType().GetProperty("TVStandard");
				if (property != null)
				{
					property.SetValue(overrideConfig, lastGameMetadata.PreferredTVStandard);
					hasTVStandardOverride = true;
				}
			}

			if (lastGameMetadata != null && lastGameMetadata.PreferredRegion != Emulation.Region.Auto)
			{
				var property = overrideConfig.GetType().GetProperty("Region");
				if (property != null)
				{
					property.SetValue(overrideConfig, lastGameMetadata.PreferredRegion);
					hasRegionOverride = true;
				}
			}

			if (lastGameMetadata != null && lastGameMetadata.AllowMemoryControl != true)
			{
				var propertyMem = overrideConfig.GetType().GetProperty("AllowMemoryControl");
				if (propertyMem != null)
				{
					propertyMem.SetValue(overrideConfig, lastGameMetadata.AllowMemoryControl);
					hasDisallowMemoryControlOverride = true;

					var propertyBoot = overrideConfig.GetType().GetProperty("UseBootstrap");
					if (propertyBoot != null)
					{
						propertyBoot.SetValue(overrideConfig, false);
					}
				}
			}

			if (forcePowerOnWithoutCart)
				onScreenDisplayHandler.EnqueueMessageWarning("Bootstrap ROM is disabled in settings; enabling it for this startup.");

			if (hasTVStandardOverride)
				onScreenDisplayHandler.EnqueueMessageWarning($"Overriding TV standard setting; running game as {lastGameMetadata?.PreferredTVStandard}.");

			if (hasRegionOverride)
				onScreenDisplayHandler.EnqueueMessageWarning($"Overriding region setting; running game as {lastGameMetadata?.PreferredRegion}.");

			if (hasDisallowMemoryControlOverride)
				onScreenDisplayHandler.EnqueueMessageWarning("Game-specific hack: Preventing software from reconfiguring memory control.\nBootstrap ROM has been disabled for this startup due to memory control hack.");

			if (forcePowerOnWithoutCart || hasTVStandardOverride || hasRegionOverride || hasDisallowMemoryControlOverride)
				emulatorHandler.SetConfiguration(overrideConfig);
		}

		private void SignalStopEmulation()
		{
			ShutdownEmulation();

			lastGameMetadata = null;

			takeScreenshotToolStripMenuItem.Enabled = pauseToolStripMenuItem.Enabled = resetToolStripMenuItem.Enabled = stopToolStripMenuItem.Enabled = false;
			loadStateToolStripMenuItem.Enabled = saveStateToolStripMenuItem.Enabled = false;
			startRecordingToolStripMenuItem.Enabled = false;
			toggleLayersToolStripMenuItem.Enabled = enableChannelsToolStripMenuItem.Enabled = false;

			SetWindowTitleAndStatus();
		}

		private void ShutdownEmulation()
		{
			if (emulatorHandler == null) return;

			emulatorHandler.SaveCartridge();

			emulatorHandler.SendLogMessage -= EmulatorHandler_SendLogMessage;
			emulatorHandler.EmulationReset -= EmulatorHandler_EmulationReset;
			emulatorHandler.RenderScreen -= EmulatorHandler_RenderScreen;
			emulatorHandler.SizeScreen -= EmulatorHandler_SizeScreen;
			emulatorHandler.ChangeViewport -= EmulatorHandler_ChangeViewport;
			emulatorHandler.PollInput -= EmulatorHandler_PollInput;
			emulatorHandler.EnqueueSamples -= soundHandler.EnqueueSamples;
			emulatorHandler.SaveExtraData -= EmulatorHandler_SaveExtraData;
			emulatorHandler.EnableRumble -= EmulatorHandler_EnableRumble;
			emulatorHandler.PauseChanged -= EmulatorHandler_PauseChanged;

			emulatorHandler.EnqueueSamples -= soundDebuggerForm.EnqueueSamples;

			emulatorHandler.Shutdown();
			while (emulatorHandler.IsRunning) { }

			emulatorHandler = null;
			GC.Collect();

			graphicsHandler?.FlushTextures();

			onScreenDisplayHandler.EnqueueMessage("Emulation stopped.");
		}

		private void SetFileFilters()
		{
			var filters = new List<string>();
			var extensionsList = new List<string>();

			foreach (var machineType in Assembly.GetExecutingAssembly().GetTypes().Where(x => typeof(IMachine).IsAssignableFrom(x) && !x.IsInterface).OrderBy(x => x.GetCustomAttribute<MachineIndexAttribute>()?.Index))
			{
				if (machineType == null) continue;

				var instance = (IMachine)Activator.CreateInstance(machineType);
				var (filterExtension, filterDescription) = instance.FileFilter;

				extensionsList.Add($"*{filterExtension}");

				var currentFilter = $"*{filterExtension};*.zip";
				filters.Add($"{filterDescription} ({currentFilter})|{currentFilter}");
			}
			extensionsList.Add("*.zip");

			var allExtensionsFilter = string.Join(";", extensionsList);
			filters.Insert(0, $"All Supported ROMs ({allExtensionsFilter})|{allExtensionsFilter}");
			filters.Add("All Files (*.*)|*.*");

			ofdOpenROM.Filter = string.Join("|", filters);
		}

		private void AddToRecentFiles(string fileName)
		{
			if (Program.Configuration.RecentFiles.Contains(fileName))
			{
				var index = Program.Configuration.RecentFiles.IndexOf(fileName);
				var newList = new List<string>(Configuration.RecentFilesCapacity) { fileName };
				newList.AddRange(Program.Configuration.RecentFiles.Where(x => x != fileName));
				Program.Configuration.RecentFiles = newList;
			}
			else
			{
				Program.Configuration.RecentFiles.Insert(0, fileName);
				if (Program.Configuration.RecentFiles.Count > Configuration.RecentFilesCapacity)
					Program.Configuration.RecentFiles.RemoveAt(Program.Configuration.RecentFiles.Count - 1);
			}
		}

		private void CreateRecentFilesMenu()
		{
			recentFilesToolStripMenuItem.DropDownItems.Clear();

			var clearMenuItem = new ToolStripMenuItem("&Clear List...");
			clearMenuItem.Click += (s, e) =>
			{
				Program.Configuration.RecentFiles.Clear();
				CreateRecentFilesMenu();
			};
			recentFilesToolStripMenuItem.DropDownItems.Add(clearMenuItem);
			recentFilesToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());

			for (int i = 0; i < Configuration.RecentFilesCapacity; i++)
			{
				var file = (i < Program.Configuration.RecentFiles.Count ? Program.Configuration.RecentFiles[i] : null);
				var menuItem = new ToolStripMenuItem(file != null ? file.Replace("&", "&&") : "-")
				{
					Enabled = (file != null),
					Tag = file
				};
				menuItem.Click += (s, e) =>
				{
					if ((s as ToolStripMenuItem).Tag is string fileName)
						LoadAndRunCartridge(fileName);
				};
				recentFilesToolStripMenuItem.DropDownItems.Add(menuItem);
			}
		}

		private void CreatePowerOnMenu()
		{
			powerOnToolStripMenuItem.DropDownItems.Clear();

			foreach (var machineType in Assembly.GetExecutingAssembly().GetTypes().Where(x => typeof(IMachine).IsAssignableFrom(x) && !x.IsInterface).OrderBy(x => x.GetCustomAttribute<MachineIndexAttribute>()?.Index))
			{
				if (machineType == null) continue;

				var instance = (IMachine)Activator.CreateInstance(machineType);
				if (!instance.HasBootstrap) continue;

				var configuration = Program.Configuration.Machines[machineType.Name];
				var bootstrapPathProperty = configuration.GetType().GetProperties().Where(x => x.GetCustomAttribute<IsBootstrapRomPathAttribute>() != null).FirstOrDefault();

				if (bootstrapPathProperty?.GetValue(configuration) is string bootstrapPath)
				{
					var menuItem = new ToolStripMenuItem(instance.ModelName) { Tag = machineType, Enabled = File.Exists(bootstrapPath) };
					menuItem.Click += (s, e) =>
					{
						if ((s as ToolStripMenuItem).Tag is Type machine)
						{
							PowerOnWithoutCartridge(machine);
						}
					};
					powerOnToolStripMenuItem.DropDownItems.Add(menuItem);
				}
			}
		}

		private void CreateLoadSaveStateMenus()
		{
			loadStateToolStripMenuItem.DropDownItems.Clear();
			saveStateToolStripMenuItem.DropDownItems.Clear();

			for (int i = 1; i <= maxSaveStateCount; i++)
			{
				var stateFileInfo = new FileInfo(emulatorHandler.GetSaveStateFilename(i));

				var loadMenuItem = new ToolStripMenuItem();
				var saveMenuItem = new ToolStripMenuItem();

				if (lastGameMetadata != null)
				{
					if (!stateFileInfo.Exists)
					{
						loadMenuItem.Text = $"{i}: -";
						loadMenuItem.Enabled = false;

						saveMenuItem.Text = $"{i}: -";
						saveMenuItem.Enabled = true;
					}
					else
					{
						loadMenuItem.Text = $"{i}: {stateFileInfo.LastWriteTime}";
						loadMenuItem.Tag = i;
						loadMenuItem.Click += (s, e) =>
						{
							if ((s as ToolStripMenuItem).Tag is int stateNumber)
							{
								SetTemporaryPause(true);
								emulatorHandler.LoadState(stateNumber);
								SetTemporaryPause(false);

								onScreenDisplayHandler.EnqueueMessage($"State {stateNumber} loaded.");
							}
						};

						saveMenuItem.Text = $"{i}: {stateFileInfo.LastWriteTime}";
					}

					saveMenuItem.Tag = i;
					saveMenuItem.Click += (s, e) =>
					{
						if ((s as ToolStripMenuItem).Tag is int stateNumber)
						{
							SetTemporaryPause(true);
							emulatorHandler.SaveState(stateNumber);
							SetTemporaryPause(false);

							while (emulatorHandler.IsHandlingSaveState) { Application.DoEvents(); }
							CreateLoadSaveStateMenus();

							onScreenDisplayHandler.EnqueueMessage($"State {stateNumber} saved.");
						}
					};

					loadStateToolStripMenuItem.DropDownItems.Add(loadMenuItem);
					saveStateToolStripMenuItem.DropDownItems.Add(saveMenuItem);
				}
			}
		}

		private void CreateShaderMenu()
		{
			shadersToolStripMenuItem.DropDownItems.Clear();

			var shaders = new List<string>() { Configuration.DefaultShaderName };
			shaders.AddRange(new DirectoryInfo(Program.ShaderPath).EnumerateDirectories().Select(x => x.Name));

			foreach (var shaderName in shaders)
			{
				var menuItem = new ToolStripMenuItem(shaderName)
				{
					Checked = (shaderName == Program.Configuration.LastShader),
					Tag = shaderName
				};
				menuItem.Click += (s, e) =>
				{
					if ((s as ToolStripMenuItem).Tag is string shader)
					{
						graphicsHandler?.LoadShaderBundle(shader);

						Program.Configuration.LastShader = shader;
						foreach (ToolStripMenuItem shaderMenuItem in shadersToolStripMenuItem.DropDownItems)
							shaderMenuItem.Checked = (shaderMenuItem.Tag as string) == Program.Configuration.LastShader;
					}
				};
				shadersToolStripMenuItem.DropDownItems.Add(menuItem);
			}
		}

		private void CreateScreenSizeMenu()
		{
			screenSizeToolStripMenuItem.DropDownItems.Clear();

			for (int i = 2; i <= maxScreenSizeFactor; i++)
			{
				var menuItem = new ToolStripMenuItem($"{i}x")
				{
					Checked = (Program.Configuration.ScreenSize == i),
					Tag = i
				};
				menuItem.Click += (s, e) =>
				{
					if ((s as ToolStripMenuItem).Tag is int screenSizeFactor)
					{
						Program.Configuration.ScreenSize = screenSizeFactor;
						SizeAndPositionWindow();

						foreach (ToolStripMenuItem screenSizeMenuItem in screenSizeToolStripMenuItem.DropDownItems)
							screenSizeMenuItem.Checked = (int)screenSizeMenuItem.Tag == Program.Configuration.ScreenSize;
					}
				};
				screenSizeToolStripMenuItem.DropDownItems.Add(menuItem);
			}
		}

		private void CreateSizeModeMenu()
		{
			sizeModeToolStripMenuItem.DropDownItems.Clear();

			foreach (var sizeMode in Enum.GetValues(typeof(ScreenSizeMode)))
			{
				var menuItem = new ToolStripMenuItem($"{sizeMode}")
				{
					Checked = (Program.Configuration.ScreenSizeMode == (ScreenSizeMode)sizeMode),
					Tag = sizeMode
				};
				menuItem.Click += (s, e) =>
				{
					if ((s as ToolStripMenuItem).Tag is object screenSizeMode && Enum.IsDefined(typeof(ScreenSizeMode), screenSizeMode))
					{
						Program.Configuration.ScreenSizeMode = (ScreenSizeMode)screenSizeMode;
						graphicsHandler?.Resize(renderControl.ClientRectangle, new Size((int)(currentViewport.width * currentPixelAspectRatio), currentViewport.height));

						foreach (ToolStripMenuItem sizeModeMenuItem in sizeModeToolStripMenuItem.DropDownItems)
							sizeModeMenuItem.Checked = (ScreenSizeMode)sizeModeMenuItem.Tag == Program.Configuration.ScreenSizeMode;
					}
				};
				sizeModeToolStripMenuItem.DropDownItems.Add(menuItem);
			}
		}

		private void CreateToggleGraphicsLayersMenu()
		{
			toggleLayersToolStripMenuItem.DropDownItems.Clear();

			foreach (var layer in emulatorHandler.Information.RuntimeOptions.Where(x => x.Name.StartsWith("GraphicsLayersShow")))
			{
				var menuItem = new ToolStripMenuItem(layer.Description)
				{
					Checked = (bool)emulatorHandler.GetRuntimeOption(layer.Name),
					Tag = layer.Name
				};
				menuItem.Click += (s, e) =>
				{
					if ((s as ToolStripMenuItem).Tag is string layerOptionName)
					{
						emulatorHandler.SetRuntimeOption(layerOptionName, !(s as ToolStripMenuItem).Checked);

						foreach (ToolStripMenuItem toggleLayersMenuItem in toggleLayersToolStripMenuItem.DropDownItems)
						{
							if (toggleLayersMenuItem.Tag is string layerOptionNameCheck)
								toggleLayersMenuItem.Checked = (bool)emulatorHandler.GetRuntimeOption(layerOptionNameCheck);
						}
					}
				};
				toggleLayersToolStripMenuItem.DropDownItems.Add(menuItem);
			}
		}

		private void CreateSampleRateMenu()
		{
			sampleRateToolStripMenuItem.DropDownItems.Clear();

			for (int i = 0; i < maxSampleRateFactor; i++)
			{
				var sampleRate = 11025 << i;
				var menuItem = new ToolStripMenuItem($"{sampleRate} Hz")
				{
					Checked = (Program.Configuration.SampleRate == sampleRate),
					Tag = sampleRate
				};
				menuItem.Click += (s, e) =>
				{
					if ((s as ToolStripMenuItem).Tag is int rate)
					{
						Program.Configuration.SampleRate = rate;

						if (soundHandler != null)
						{
							if (emulatorHandler != null) emulatorHandler.EnqueueSamples -= soundHandler.EnqueueSamples;
							soundHandler?.ClearSampleBuffer();
							soundHandler?.Shutdown();
						}

						InitializeSoundHandler();

						if (emulatorHandler != null)
						{
							var machineType = emulatorHandler.GetMachineType();
							emulatorHandler.SetConfiguration(Program.Configuration.Machines[machineType.Name]);

							emulatorHandler.EnqueueSamples += soundHandler.EnqueueSamples;
						}

						foreach (ToolStripMenuItem sampleRateMenuItem in sampleRateToolStripMenuItem.DropDownItems)
							sampleRateMenuItem.Checked = (int)sampleRateMenuItem.Tag == Program.Configuration.SampleRate;
					}
				};
				sampleRateToolStripMenuItem.DropDownItems.Add(menuItem);
			}
		}

		private void CreateToggleSoundChannelsMenu()
		{
			enableChannelsToolStripMenuItem.DropDownItems.Clear();

			foreach (var channel in emulatorHandler.Information.RuntimeOptions.Where(x => x.Name.StartsWith("AudioEnable")))
			{
				var menuItem = new ToolStripMenuItem(channel.Description)
				{
					Checked = (bool)emulatorHandler.GetRuntimeOption(channel.Name),
					Tag = channel.Name
				};
				menuItem.Click += (s, e) =>
				{
					if ((s as ToolStripMenuItem).Tag is string channelOptionName)
					{
						emulatorHandler.SetRuntimeOption(channelOptionName, !(s as ToolStripMenuItem).Checked);

						foreach (ToolStripMenuItem toggleChannelsMenuItem in enableChannelsToolStripMenuItem.DropDownItems)
						{
							if (toggleChannelsMenuItem.Tag is string channelOptionNameCheck)
								toggleChannelsMenuItem.Checked = (bool)emulatorHandler.GetRuntimeOption(channelOptionNameCheck);
						}
					}
				};
				enableChannelsToolStripMenuItem.DropDownItems.Add(menuItem);
			}
		}

		private string GetProductNameAndVersionString(bool appendBuildName)
		{
			var titleStringBuilder = new StringBuilder();

			var version = new Version(Application.ProductVersion);
			var versionMinor = (version.Minor != 0 ? $".{version.Minor}" : string.Empty);
			titleStringBuilder.Append($"{Application.ProductName} v{version.Major:D3}{versionMinor}");

			if (appendBuildName)
				titleStringBuilder.Append($" ({Program.BuildName})");

			return titleStringBuilder.ToString();
		}

		private void SetWindowTitleAndStatus()
		{
			var titleStringBuilder = new StringBuilder();

			titleStringBuilder.Append(GetProductNameAndVersionString(Program.AppEnvironment.DebugMode));

			if (emulatorHandler != null)
			{
				if (emulatorHandler.IsCartridgeLoaded)
					titleStringBuilder.Append($" - [{Path.GetFileName(Program.Configuration.RecentFiles.First())}]");

				var statusStringBuilder = new StringBuilder();
				statusStringBuilder.Append($"Emulating {emulatorHandler.Information.Manufacturer} {emulatorHandler.Information.Model}");
				statusStringBuilder.Append(", ");

				if (emulatorHandler.IsCartridgeLoaded)
					statusStringBuilder.Append($"playing {lastGameMetadata?.KnownName.Replace("&", "&&") ?? "unrecognized game"}");
				else
					statusStringBuilder.Append("powered on without cartridge");

				tsslStatus.Text = statusStringBuilder.ToString();
				tsslEmulationStatus.Text = (emulatorHandler.IsRunning ? (emulatorHandler.IsPaused ? "Paused" : "Running") : "Stopped");
			}
			else
			{
				tsslStatus.Text = "Ready";
				tsslEmulationStatus.Text = "Stopped";
			}

			Text = titleStringBuilder.ToString();
		}

		private void SizeAndPositionWindow()
		{
			SuspendLayout();

			if (Program.Configuration.ScreenSize < 0 || Program.Configuration.ScreenSize > maxScreenSizeFactor)
				Program.Configuration.ScreenSize = 1;

			if (!fullScreen)
			{
				if (WindowState == FormWindowState.Maximized)
					WindowState = FormWindowState.Normal;

				FormBorderStyle = FormBorderStyle.Sizable;
				TopMost = false;

				menuStrip.Visible = statusStrip.Visible = true;
				menuStrip.Enabled = statusStrip.Enabled = true;

				CursorShown = true;

				ClientSize = new Size(
					(int)((currentViewport.width * currentPixelAspectRatio) * Program.Configuration.ScreenSize),
					(currentViewport.height * Program.Configuration.ScreenSize) + (menuStrip.Height + statusStrip.Height)
					);

				// https://stackoverflow.com/a/6837499
				var screen = Screen.FromControl(this);
				var workingArea = screen.WorkingArea;
				Location = new Point()
				{
					X = Math.Max(workingArea.X, workingArea.X + (workingArea.Width - Width) / 2),
					Y = Math.Max(workingArea.Y, workingArea.Y + (workingArea.Height - Height) / 2)
				};

				SetTemporaryPause(false);
				Application.RemoveMessageFilter(altMessageFilter);
			}
			else
			{
				FormBorderStyle = FormBorderStyle.None;
				Bounds = Screen.FromControl(this).Bounds;
				TopMost = true;

				menuStrip.Visible = statusStrip.Visible = false;
				menuStrip.Enabled = statusStrip.Enabled = false;

				CursorShown = false;

				SetTemporaryPause(false);
				Application.AddMessageFilter(altMessageFilter);
			}

			ResumeLayout();
		}

		private void MainForm_Shown(object sender, EventArgs e)
		{
			InitializeDebuggers();
			InitializeHandlers();
		}

		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			SignalStopEmulation();

			soundHandler?.Shutdown();

			ShutdownDebuggers();

			Program.SaveConfiguration();
		}

		private void renderControl_KeyDown(object sender, KeyEventArgs e)
		{
			if (!keysDown.Contains(e.KeyCode))
				keysDown.Add(e.KeyCode);

			if (e.KeyData == Keys.F11 && emulatorHandler != null)
			{
				fullScreen = !fullScreen;
				SizeAndPositionWindow();

				var stateString = (fullScreen ? "enabled" : "disabled");
				onScreenDisplayHandler.EnqueueMessage($"Fullscreen {stateString}.");
			}
		}

		private void renderControl_KeyUp(object sender, KeyEventArgs e)
		{
			if (keysDown.Contains(e.KeyCode))
				keysDown.Remove(e.KeyCode);
		}

		private void renderControl_MouseMove(object sender, MouseEventArgs e)
		{
			mousePosition = (e.X, e.Y);
			mouseButtonsDown = e.Button;
		}

		private void renderControl_MouseDown(object sender, MouseEventArgs e)
		{
			mouseButtonsDown |= e.Button;
		}

		private void renderControl_MouseUp(object sender, MouseEventArgs e)
		{
			mouseButtonsDown &= ~e.Button;
		}

		private void renderControl_Render(object sender, EventArgs e)
		{
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

			if (emulatorHandler != null)
			{
				if (Program.Configuration.ShowFps)
					onScreenDisplayHandler.SendString($"{emulatorHandler.FramesPerSecond} FPS", -8, -8);

				var debugInfos = emulatorHandler.GetDebugInformation();
				DrawInputDisplay(debugInfos);

				// TODO: move elsewhere?
				soundHandler.MaxQueueLength = (int)(emulatorHandler.FramesPerSecond / emulatorHandler.Information.RefreshRate) + 1;
			}

			graphicsHandler.Render();
		}

		private void DrawInputDisplay(Dictionary<string, dynamic> debugInfo)
		{
			if (!debugInfo.ContainsKey("InputStates") || !debugInfo.ContainsKey("InputOsdIcons")) return;

			var inputStates = (debugInfo["InputStates"] as Dictionary<string, bool>).ToList();
			var inputOsdIcons = (debugInfo["InputOsdIcons"] as Dictionary<string, (string str, int xPos, int yPos)>).ToList();

			var normColor = OpenTK.Graphics.Color4.White;
			var pressColor = OpenTK.Graphics.Color4.Red;

			foreach (var label in inputOsdIcons.Where(x => !inputStates.Any(y => y.Key == x.Key)))
			{
				var (str, xPos, yPos) = label.Value;
				onScreenDisplayHandler.SendString(str, 4 + xPos, 4 + yPos, normColor);
			}

			foreach (var input in inputStates)
			{
				var (str, xPos, yPos) = inputOsdIcons.FirstOrDefault(x => x.Key == input.Key).Value;
				onScreenDisplayHandler.SendString(str, 4 + xPos, 4 + yPos, input.Value ? pressColor : normColor);
			}
		}

		private void renderControl_Resize(object sender, EventArgs e)
		{
			graphicsHandler.Resize(renderControl.ClientRectangle, new Size((int)(currentViewport.width * currentPixelAspectRatio), currentViewport.height));
		}

		private void EmulatorHandler_SendLogMessage(object sender, SendLogMessageEventArgs e)
		{
			this.CheckInvokeMethod(delegate () { onScreenDisplayHandler.EnqueueMessageCore($"{emulatorHandler.Information.Model}: {e.Message}"); });
		}

		private void EmulatorHandler_EmulationReset(object sender, EventArgs e)
		{
			this.CheckInvokeMethod(delegate () { onScreenDisplayHandler.EnqueueMessage("Emulation reset."); });
		}

		private void EmulatorHandler_RenderScreen(object sender, RenderScreenEventArgs e)
		{
			this.CheckInvokeMethod(delegate ()
			{
				if (e.Width != lastFramebufferSize.width || e.Height != lastFramebufferSize.height)
				{
					lastFramebufferSize = (e.Width, e.Height);
					graphicsHandler?.SetTextureSize(e.Width, e.Height);
				}
				lastFramebufferData = e.FrameData;
				graphicsHandler?.SetTextureData(e.FrameData);

				// TODO: create emulation "EndOfFrame" event for this?
				ControllerManager.Update();
			});
		}

		private void EmulatorHandler_SizeScreen(object sender, SizeScreenEventArgs e)
		{
			this.CheckInvokeMethod(delegate ()
			{
				lastFramebufferSize = (e.Width, e.Height);
				graphicsHandler?.SetTextureSize(e.Width, e.Height);
			});
		}

		private void EmulatorHandler_ChangeViewport(object sender, ChangeViewportEventArgs e)
		{
			this.CheckInvokeMethod(delegate ()
			{
				graphicsHandler?.SetScreenViewport(currentViewport = e.Viewport);
				SizeAndPositionWindow();
			});
		}

		private void EmulatorHandler_PollInput(object sender, PollInputEventArgs e)
		{
			// TODO: rare, random, weird argument exceptions on e.Keyboard assignment; does this lock help??
			lock (uiLock)
			{
				e.Keyboard = new List<Keys>(keysDown);
				e.MouseButtons = mouseButtonsDown;

				var vx = (currentViewport.x - 50);
				var dvx = renderControl.ClientSize.Width / (currentViewport.width - (double)vx);
				var dvy = renderControl.ClientSize.Height / (currentViewport.height - (double)currentViewport.y);
				e.MousePosition = ((int)(mousePosition.x / dvx) - vx, (int)(mousePosition.y / dvy) - currentViewport.y);

				if (Program.Configuration.EnableXInput)
					e.ControllerState = ControllerManager.GetController(0).GetControllerState();
			}
		}

		private void EmulatorHandler_SaveExtraData(object sender, SaveExtraDataEventArgs e)
		{
			/* Extract options etc. */
			var includeDateTime = e.Options.HasFlag(ExtraDataOptions.IncludeDateTime);
			var allowOverwrite = e.Options.HasFlag(ExtraDataOptions.AllowOverwrite);

			var extension = string.Empty;
			switch (e.DataType)
			{
				case ExtraDataTypes.Image: extension = "png"; break;
				case ExtraDataTypes.Raw: extension = "bin"; break;
				default: throw new EmulationException($"Unknown extra data type {e.DataType}");
			}

			/* Generate filename/path */
			var filePrefix = $"{Path.GetFileNameWithoutExtension(lastGameMetadata.FileName)} ({e.Description}{(includeDateTime ? $" {DateTime.Now:yyyy-MM-dd HH-mm-ss})" : ")")}";
			var filePath = Path.Combine(Program.ExtraDataPath, $"{filePrefix}.{extension}");
			if (!allowOverwrite)
			{
				var existingFiles = Directory.EnumerateFiles(Program.ExtraDataPath, $"{filePrefix}*{extension}");
				if (existingFiles.Contains(filePath))
					for (int i = 2; existingFiles.Contains(filePath = Path.Combine(Program.ExtraDataPath, $"{filePrefix} ({i}).{extension}")); i++) { }
			}

			/* Handle data */
			if (e.Data is Bitmap image)
			{
				/* Images, ex. GB Printer printouts */
				image.Save(filePath);
			}
			else if (e.Data is byte[] raw)
			{
				/* Raw bytes */
				using (var file = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
				{
					file.Write(raw, 0, raw.Length);
				}
			}
		}

		private void EmulatorHandler_EnableRumble(object sender, EventArgs e)
		{
			if (Program.Configuration.EnableXInput && Program.Configuration.EnableRumble)
				ControllerManager.GetController(0).Vibrate(0.0f, 0.5f, TimeSpan.FromSeconds(0.1f));
		}

		private void EmulatorHandler_PauseChanged(object sender, EventArgs e)
		{
			SetWindowTitleAndStatus();

			if (emulatorHandler.IsPaused)
				soundHandler?.ClearSampleBuffer();
		}

		private void openROMToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var lastFile = (Program.Configuration.RecentFiles.Count > 0 ? Program.Configuration.RecentFiles.First() : string.Empty);
			if (lastFile != string.Empty)
			{
				ofdOpenROM.FileName = Path.GetFileName(lastFile);
				ofdOpenROM.InitialDirectory = Path.GetDirectoryName(lastFile);
			}

			if (ofdOpenROM.ShowDialog() == DialogResult.OK)
				LoadAndRunCartridge(ofdOpenROM.FileName);
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}

		private void resetToolStripMenuItem_Click(object sender, EventArgs e)
		{
			emulatorHandler?.Reset();
		}

		private void stopToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SignalStopEmulation();
		}

		private void startRecordingToolStripMenuItem_Click(object sender, EventArgs e)
		{
			soundHandler?.BeginRecording();
			stopRecordingToolStripMenuItem.Enabled = true;
			(sender as ToolStripMenuItem).Enabled = false;
		}

		private void stopRecordingToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (sfdSaveWavRecording.ShowDialog() == DialogResult.OK)
			{
				soundHandler?.SaveRecording(sfdSaveWavRecording.FileName);
				startRecordingToolStripMenuItem.Enabled = true;
				(sender as ToolStripMenuItem).Enabled = false;
			}
		}

		private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SetTemporaryPause(true);

			var settingsForm = new SettingsForm(Program.Configuration.Machines);
			if (settingsForm.ShowDialog() == DialogResult.OK)
			{
				Program.Configuration.Machines = settingsForm.Configurations;

				if (emulatorHandler != null)
				{
					var machineType = emulatorHandler.GetMachineType();
					emulatorHandler.SetConfiguration(Program.Configuration.Machines[machineType.Name]);
					ApplyConfigOverrides(machineType);
				}

				CreatePowerOnMenu();
			}

			Program.SaveConfiguration();

			SetTemporaryPause(false);
		}

		private void takeScreenshotToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SetTemporaryPause(true);

			using (var bitmap = new Bitmap(lastFramebufferSize.width, lastFramebufferSize.height))
			{
				var screenshotPrefix = $"{Path.GetFileNameWithoutExtension(lastGameMetadata.FileName)} ({DateTime.Now:yyyy-MM-dd HH-mm-ss})";
				var newScreenshotPath = Path.Combine(Program.ScreenshotPath, $"{screenshotPrefix}.png");
				var existingShots = Directory.EnumerateFiles(Program.ScreenshotPath, $"{screenshotPrefix}*.png");
				if (existingShots.Contains(newScreenshotPath))
					for (int i = 2; existingShots.Contains(newScreenshotPath = Path.Combine(Program.ScreenshotPath, $"{screenshotPrefix} (Shot {i}).png")); i++) { }

				var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
				var dataSize = Math.Min(bitmapData.Stride * bitmapData.Height, lastFramebufferData.Length);
				Marshal.Copy(lastFramebufferData, 0, bitmapData.Scan0, dataSize);
				bitmap.UnlockBits(bitmapData);

				using (var croppedBitmap = new Bitmap(currentViewport.width, currentViewport.height))
				{
					using (var g = System.Drawing.Graphics.FromImage(croppedBitmap))
					{
						g.DrawImage(bitmap, 0, 0, new Rectangle(currentViewport.x, currentViewport.y, currentViewport.width, currentViewport.height), GraphicsUnit.Pixel);
					}
					croppedBitmap.Save(newScreenshotPath);
				}

				onScreenDisplayHandler.EnqueueMessageSuccess("Screenshot saved.");
			}

			SetTemporaryPause(false);
		}

		private void soundDebuggerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			soundDebuggerForm.Show();
			soundDebuggerForm.BringToFront();
		}

		private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var productNameAndVersion = GetProductNameAndVersionString(true);

			var description = Assembly.GetExecutingAssembly().GetAttribute<AssemblyDescriptionAttribute>().Description;
			var copyright = Assembly.GetExecutingAssembly().GetAttribute<AssemblyCopyrightAttribute>().Copyright;

			var buildTimeZone = (TimeZoneInfo)BuildInformation.Properties["BuildTimeZone"];
			var buildDateTime = TimeZoneInfo.ConvertTimeFromUtc((DateTime)BuildInformation.Properties["BuildDate"], buildTimeZone);
			var buildTimeOffset = buildTimeZone.GetUtcOffset(buildDateTime);
			var buildDateTimeString = $"{buildDateTime} (UTC{(buildTimeOffset >= TimeSpan.Zero ? "+" : "-")}{Math.Abs(buildTimeOffset.Hours):D2}:{Math.Abs(buildTimeOffset.Minutes):D2})";

			var aboutBuilder = new StringBuilder();
			aboutBuilder.AppendLine($"{productNameAndVersion} - {CultureInfo.CurrentCulture.TextInfo.ToTitleCase(description)}");
			aboutBuilder.AppendLine();
			aboutBuilder.AppendLine($"{copyright}");
			aboutBuilder.AppendLine();
			aboutBuilder.AppendLine($"{buildDateTimeString} on {Program.BuildMachineInfo}");

			if (Program.AppEnvironment.DebugMode)
			{
				aboutBuilder.AppendLine();
				aboutBuilder.AppendLine("(I'm MR.CHRONO. Now on DEBUG...)");
			}

			MessageBox.Show(aboutBuilder.ToString(), $"About {Application.ProductName}", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
	}
}
