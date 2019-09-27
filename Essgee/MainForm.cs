using System;
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

using Essgee.Graphics;
using Essgee.Sound;
using Essgee.Emulation;
using Essgee.Emulation.Machines;
using Essgee.EventArguments;
using Essgee.Exceptions;
using Essgee.Metadata;
using Essgee.Utilities;

namespace Essgee
{
	public partial class MainForm : Form
	{
		readonly static double baseScreenSize = 240.0;
		readonly static double aspectRatio = (4.0 / 3.0);

		readonly static int maxScreenSizeFactor = 3;
		readonly static int maxSaveStateCount = 8;

		readonly static string buildName = $"{BuildInformation.Properties["GitBranch"]}-{BuildInformation.Properties["LatestCommitHash"]}{(BuildInformation.Properties["GitPendingChanges"] ? "-dirty" : string.Empty)}";
		readonly static string buildMachineInfo = $"{BuildInformation.Properties["BuildMachineName"]} ({BuildInformation.Properties["BuildMachineProcessorArchitecture"]}, {BuildInformation.Properties["BuildMachineOSPlatform"]} v{BuildInformation.Properties["BuildMachineOSVersion"]})";

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

		GraphicsEnableState graphicsEnableStates;

		bool lastUserPauseState, lastTemporaryPauseState;
		(int x, int y, int width, int height) currentViewport;
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

			SizeAndPositionWindow();
			SetWindowTitleAndStatus();

			SetFileFilters();

			graphicsEnableStates = (GraphicsEnableState.Backgrounds | GraphicsEnableState.Sprites | GraphicsEnableState.Borders);

			CreateRecentFilesMenu();
			CreatePowerOnMenu();
			CreateShaderMenu();
			CreateScreenSizeMenu();
			CreateSizeModeMenu();
			CreateShowLayersMenu();

			limitFPSToolStripMenuItem.DataBindings.Add(nameof(limitFPSToolStripMenuItem.Checked), Program.Configuration, nameof(Program.Configuration.LimitFps), false, DataSourceUpdateMode.OnPropertyChanged);
			limitFPSToolStripMenuItem.CheckedChanged += (s, e) => { emulatorHandler?.SetFpsLimiting(Program.Configuration.LimitFps); };

			showFPSToolStripMenuItem.DataBindings.Add(nameof(showFPSToolStripMenuItem.Checked), Program.Configuration, nameof(Program.Configuration.ShowFps), false, DataSourceUpdateMode.OnPropertyChanged);

			muteToolStripMenuItem.DataBindings.Add(nameof(muteToolStripMenuItem.Checked), Program.Configuration, nameof(Program.Configuration.Mute), false, DataSourceUpdateMode.OnPropertyChanged);
			muteToolStripMenuItem.CheckedChanged += (s, e) => { soundHandler?.SetMute(Program.Configuration.Mute); };

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

		private void ExceptionHandler(Exception ex)
		{
			CheckInvokeMethod(() =>
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
			var osdFontText = Assembly.GetExecutingAssembly().ReadEmbeddedImageFile($"{Application.ProductName}.Assets.OsdFont.png");
			onScreenDisplayHandler = new OnScreenDisplayHandler(osdFontText);

			if (onScreenDisplayHandler == null) throw new HandlerException("Failed to initialize OSD handler");

			graphicsHandler = new GraphicsHandler(onScreenDisplayHandler);
			graphicsHandler?.LoadShaderBundle(Program.Configuration.LastShader);

			soundHandler = new SoundHandler(onScreenDisplayHandler, 44100, 2, ExceptionHandler);
			soundHandler.SetVolume(Program.Configuration.Volume);
			soundHandler.SetMute(Program.Configuration.Mute);
			soundHandler.Startup();

			gameMetadataHandler = new GameMetadataHandler(onScreenDisplayHandler);
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

			emulatorHandler.SetFpsLimiting(Program.Configuration.LimitFps);
			emulatorHandler.SetGraphicsEnableStates(graphicsEnableStates);

			emulatorHandler.SetConfiguration(Program.Configuration.Machines[machineType.Name]);

			pauseToolStripMenuItem.DataBindings.Clear();
			pauseToolStripMenuItem.DataBindings.Add(nameof(pauseToolStripMenuItem.Checked), emulatorHandler, nameof(emulatorHandler.IsPaused), false, DataSourceUpdateMode.OnPropertyChanged);
			pauseToolStripMenuItem.CheckedChanged += (s, e) => { SetWindowTitleAndStatus(); };

			onScreenDisplayHandler.EnqueueMessageSuccess($"{emulatorHandler.Information.Manufacturer} {emulatorHandler.Information.Model} emulation initialized.");
		}

		private void SetTemporaryPause(bool newTemporaryPauseState)
		{
			if (emulatorHandler == null || !emulatorHandler.IsRunning) return;

			if (!lastTemporaryPauseState && newTemporaryPauseState)
				lastUserPauseState = emulatorHandler.IsPaused;

			if (lastUserPauseState)
			{
				emulatorHandler.IsPaused = lastUserPauseState;
			}
			else
			{
				if (!newTemporaryPauseState)
					soundHandler?.ClearSampleBuffer();

				emulatorHandler.IsPaused = lastTemporaryPauseState = newTemporaryPauseState;
			}
		}

		private void PowerOnWithoutCartridge(Type machineType)
		{
			InitializeEmulation(machineType);

			lastGameMetadata = null;

			ApplyConfigOverrides(machineType);

			takeScreenshotToolStripMenuItem.Enabled = pauseToolStripMenuItem.Enabled = resetToolStripMenuItem.Enabled = stopToolStripMenuItem.Enabled = true;
			loadStateToolStripMenuItem.Enabled = saveStateToolStripMenuItem.Enabled = false;

			emulatorHandler.Startup();

			SizeAndPositionWindow();
			SetWindowTitleAndStatus();

			onScreenDisplayHandler.EnqueueMessage($"Power on without cartridge.");
		}

		private void LoadAndRunCartridge(string fileName)
		{
			try
			{
				var (machineType, romData) = CartridgeLoader.Load(fileName);

				InitializeEmulation(machineType);

				lastGameMetadata = gameMetadataHandler.GetGameMetadata(emulatorHandler.Information.DatFileName, fileName, Crc32.Calculate(romData), romData.Length);

				ApplyConfigOverrides(machineType);

				emulatorHandler.LoadCartridge(romData, lastGameMetadata);

				AddToRecentFiles(fileName);
				CreateRecentFilesMenu();
				CreateLoadSaveStateMenus();

				takeScreenshotToolStripMenuItem.Enabled = pauseToolStripMenuItem.Enabled = resetToolStripMenuItem.Enabled = stopToolStripMenuItem.Enabled = true;
				loadStateToolStripMenuItem.Enabled = saveStateToolStripMenuItem.Enabled = true;

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

			if (forcePowerOnWithoutCart)
				onScreenDisplayHandler.EnqueueMessageWarning($"Bootstrap ROM is disabled in settings; enabling it for this startup.");

			if (hasTVStandardOverride)
				onScreenDisplayHandler.EnqueueMessageWarning($"Overriding TV standard setting; running game as {lastGameMetadata?.PreferredTVStandard}.");

			if (hasRegionOverride)
				onScreenDisplayHandler.EnqueueMessageWarning($"Overriding region setting; running game as {lastGameMetadata?.PreferredRegion}.");

			if (forcePowerOnWithoutCart || hasTVStandardOverride || hasRegionOverride)
				emulatorHandler.SetConfiguration(overrideConfig);
		}

		private void SignalStopEmulation()
		{
			ShutdownEmulation();

			lastGameMetadata = null;

			takeScreenshotToolStripMenuItem.Enabled = pauseToolStripMenuItem.Enabled = resetToolStripMenuItem.Enabled = stopToolStripMenuItem.Enabled = false;
			loadStateToolStripMenuItem.Enabled = saveStateToolStripMenuItem.Enabled = false;

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

							while (emulatorHandler.IsHandlingSaveState) { }
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

			for (int i = 1; i <= maxScreenSizeFactor; i++)
			{
				var screenSize = (i * baseScreenSize);
				var menuItem = new ToolStripMenuItem($"{i}x ({screenSize}p)")
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
						graphicsHandler?.Resize(renderControl.ClientRectangle, new Size((int)(baseScreenSize * aspectRatio), (int)baseScreenSize));

						foreach (ToolStripMenuItem sizeModeMenuItem in sizeModeToolStripMenuItem.DropDownItems)
							sizeModeMenuItem.Checked = (ScreenSizeMode)sizeModeMenuItem.Tag == Program.Configuration.ScreenSizeMode;
					}
				};
				sizeModeToolStripMenuItem.DropDownItems.Add(menuItem);
			}
		}

		private void CreateShowLayersMenu()
		{
			showLayersToolStripMenuItem.DropDownItems.Clear();

			foreach (var layer in Enum.GetValues(typeof(GraphicsEnableState)))
			{
				var ignore = layer.GetType().GetField(layer.ToString())?.GetAttribute<ValueIgnoredAttribute>()?.IsIgnored;
				if (ignore ?? false) continue;

				var desc = layer.GetType().GetField(layer.ToString())?.GetAttribute<DescriptionAttribute>()?.Description ?? layer.ToString();

				var menuItem = new ToolStripMenuItem($"{desc}")
				{
					Checked = ((graphicsEnableStates & (GraphicsEnableState)layer) == (GraphicsEnableState)layer),
					Tag = layer
				};
				menuItem.Click += (s, e) =>
				{
					if ((s as ToolStripMenuItem).Tag is object gfxLayer && Enum.IsDefined(typeof(GraphicsEnableState), gfxLayer))
					{
						var enableState = (GraphicsEnableState)gfxLayer;

						if ((graphicsEnableStates & enableState) == enableState)
							graphicsEnableStates &= ~enableState;
						else
							graphicsEnableStates |= enableState;

						emulatorHandler.SetGraphicsEnableStates(graphicsEnableStates);

						foreach (ToolStripMenuItem showLayersMenuItem in showLayersToolStripMenuItem.DropDownItems)
						{
							if (showLayersMenuItem.Tag is GraphicsEnableState gfxLayerCheck)
								showLayersMenuItem.Checked = (graphicsEnableStates & gfxLayerCheck) == gfxLayerCheck;
						}

					}
				};
				showLayersToolStripMenuItem.DropDownItems.Add(menuItem);
			}
		}

		private void SetWindowTitleAndStatus()
		{
			var titleStringBuilder = new StringBuilder();

			var version = new Version(Application.ProductVersion);
			var versionMinor = (version.Minor != 0 ? $".{version.Minor}" : string.Empty);
			titleStringBuilder.Append($"{Application.ProductName} v{version.Major:D3}{versionMinor}");

			if (Program.AppEnvironment.DebugMode)
				titleStringBuilder.Append($" ({buildName})");

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
					statusStringBuilder.Append($"powered on without cartridge");

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
					(int)((baseScreenSize * aspectRatio) * Program.Configuration.ScreenSize),
					(int)(baseScreenSize * Program.Configuration.ScreenSize) + (menuStrip.Height + statusStrip.Height)
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
			InitializeHandlers();
		}

		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			SignalStopEmulation();

			soundHandler?.Shutdown();

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
			graphicsHandler.Resize(renderControl.ClientRectangle, new Size((int)(baseScreenSize * aspectRatio), (int)baseScreenSize));
		}

		private void CheckInvokeMethod(MethodInvoker methodInvoker)
		{
			if (InvokeRequired) BeginInvoke(methodInvoker);
			else methodInvoker();
		}

		private void EmulatorHandler_SendLogMessage(object sender, SendLogMessageEventArgs e)
		{
			CheckInvokeMethod(delegate () { onScreenDisplayHandler.EnqueueMessageCore($"{emulatorHandler.Information.Model}: {e.Message}"); });
		}

		private void EmulatorHandler_EmulationReset(object sender, EventArgs e)
		{
			CheckInvokeMethod(delegate () { onScreenDisplayHandler.EnqueueMessage("Emulation reset."); });
		}

		private void EmulatorHandler_RenderScreen(object sender, RenderScreenEventArgs e)
		{
			CheckInvokeMethod(delegate ()
			{
				if (e.Width != lastFramebufferSize.width || e.Height != lastFramebufferSize.height)
				{
					lastFramebufferSize = (e.Width, e.Height);
					graphicsHandler?.SetTextureSize(e.Width, e.Height);
				}
				lastFramebufferData = e.FrameData;
				graphicsHandler?.SetTextureData(e.FrameData);
			});
		}

		private void EmulatorHandler_SizeScreen(object sender, SizeScreenEventArgs e)
		{
			CheckInvokeMethod(delegate ()
			{
				lastFramebufferSize = (e.Width, e.Height);
				graphicsHandler?.SetTextureSize(e.Width, e.Height);
			});
		}

		private void EmulatorHandler_ChangeViewport(object sender, ChangeViewportEventArgs e)
		{
			CheckInvokeMethod(delegate ()
			{
				graphicsHandler?.SetScreenViewport(currentViewport = e.Viewport);
				SizeAndPositionWindow();
			});
		}

		private void EmulatorHandler_PollInput(object sender, PollInputEventArgs e)
		{
			e.Keyboard = keysDown;
			e.MouseButtons = mouseButtonsDown;

			var vx = (currentViewport.x - 50);
			var dvx = renderControl.ClientSize.Width / (currentViewport.width - (double)vx);
			var dvy = renderControl.ClientSize.Height / (currentViewport.height - (double)currentViewport.y);
			e.MousePosition = ((int)(mousePosition.x / dvx) - vx, (int)(mousePosition.y / dvy) - currentViewport.y);
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
				var newScreenshotPath = string.Empty;
				var screenshotPrefix = Path.GetFileNameWithoutExtension(lastGameMetadata.FileName);
				var existingShots = Directory.EnumerateFiles(Program.ScreenshotPath, $"{screenshotPrefix}*.png");
				for (int i = 0; existingShots.Contains(newScreenshotPath = Path.Combine(Program.ScreenshotPath, $"{screenshotPrefix} (Shot {i:D3}).png")); i++) { }

				var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
				var dataSize = Math.Min(bitmapData.Stride * bitmapData.Height, lastFramebufferData.Length);
				for (int i = 0, j = 0; i < dataSize; i += 3, j += 4)
				{
					Marshal.Copy(lastFramebufferData, i, bitmapData.Scan0 + j, 3);
					Marshal.WriteByte(bitmapData.Scan0 + j + 3, 0xFF);
				}
				bitmap.UnlockBits(bitmapData);

				using (var croppedBitmap = new Bitmap(currentViewport.width, currentViewport.height))
				{
					using (var g = System.Drawing.Graphics.FromImage(croppedBitmap))
					{
						g.DrawImage(bitmap, 0, 0, new Rectangle(currentViewport.x, currentViewport.y, currentViewport.width, currentViewport.height), GraphicsUnit.Pixel);
					}
					croppedBitmap.Save(newScreenshotPath);
				}
			}

			SetTemporaryPause(false);
		}

		private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var description = Assembly.GetExecutingAssembly().GetAttribute<AssemblyDescriptionAttribute>().Description;
			var copyright = Assembly.GetExecutingAssembly().GetAttribute<AssemblyCopyrightAttribute>().Copyright;
			var version = new Version(Application.ProductVersion);
			var versionMinor = (version.Minor != 0 ? $".{version.Minor}" : string.Empty);

			var buildTimeZone = (TimeZoneInfo)BuildInformation.Properties["BuildTimeZone"];
			var buildDateTime = TimeZoneInfo.ConvertTimeFromUtc((DateTime)BuildInformation.Properties["BuildDate"], buildTimeZone);
			var buildTimeOffset = buildTimeZone.GetUtcOffset(buildDateTime);
			var buildDateTimeString = $"{buildDateTime} (UTC{(buildTimeOffset >= TimeSpan.Zero ? "+" : "-")}{Math.Abs(buildTimeOffset.Hours):D2}:{Math.Abs(buildTimeOffset.Minutes):D2})";

			var aboutBuilder = new StringBuilder();
			aboutBuilder.AppendLine($"{Application.ProductName} v{version.Major:D3}{versionMinor} ({buildName}) - {CultureInfo.CurrentCulture.TextInfo.ToTitleCase(description)}");
			aboutBuilder.AppendLine();
			aboutBuilder.AppendLine($"{copyright}");
			aboutBuilder.AppendLine();
			aboutBuilder.AppendLine($"{buildDateTimeString} on {buildMachineInfo}");

			if (Program.AppEnvironment.DebugMode)
			{
				aboutBuilder.AppendLine();
				aboutBuilder.AppendLine("(I'm MR.CHRONO. Now on DEBUG...)");
			}

			MessageBox.Show(aboutBuilder.ToString(), $"About {Application.ProductName}", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
	}
}
