using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.IO;

using Essgee.Emulation.Configuration;
using Essgee.Emulation.Machines;
using Essgee.EventArguments;
using Essgee.Metadata;

namespace Essgee.Emulation
{
	public class EmulatorHandler
	{
		IMachine emulator;

		Thread emulationThread;
		volatile bool emulationThreadRunning;

		volatile bool limitFps;
		volatile bool emulationThreadPaused;

		volatile bool configChangeRequested;
		IConfiguration newConfiguration;

		public event EventHandler<SendLogMessageEventArgs> SendLogMessage
		{
			add { emulator.SendLogMessage += value; }
			remove { emulator.SendLogMessage -= value; }
		}

		public event EventHandler<EventArgs> EmulationReset
		{
			add { emulator.EmulationReset += value; }
			remove { emulator.EmulationReset -= value; }
		}

		public event EventHandler<RenderScreenEventArgs> RenderScreen
		{
			add { emulator.RenderScreen += value; }
			remove { emulator.RenderScreen -= value; }
		}

		public event EventHandler<SizeScreenEventArgs> SizeScreen
		{
			add { emulator.SizeScreen += value; }
			remove { emulator.SizeScreen -= value; }
		}

		public event EventHandler<ChangeViewportEventArgs> ChangeViewport
		{
			add { emulator.ChangeViewport += value; }
			remove { emulator.ChangeViewport -= value; }
		}

		public event EventHandler<PollInputEventArgs> PollInput
		{
			add { emulator.PollInput += value; }
			remove { emulator.PollInput -= value; }
		}

		public event EventHandler<EnqueueSamplesEventArgs> EnqueueSamples
		{
			add { emulator.EnqueueSamples += value; }
			remove { emulator.EnqueueSamples -= value; }
		}

		GameMetadata currentGameMetadata;

		public bool IsCartridgeLoaded { get; private set; }

		public bool IsRunning => emulationThreadRunning;
		public bool IsPaused
		{
			get { return emulationThreadPaused; }
			set { emulationThreadPaused = value; }
		}

		public (string Manufacturer, string Model, string DatFileName) Information => (emulator.ManufacturerName, emulator.ModelName, emulator.DatFilename);

		public EmulatorHandler(Type type)
		{
			emulator = (IMachine)Activator.CreateInstance(type);
		}

		public void SetConfiguration(IConfiguration config)
		{
			if (emulationThreadRunning)
			{
				configChangeRequested = true;
				newConfiguration = config;
			}
			else
				emulator.SetConfiguration(config);
		}

		public void Initialize()
		{
			emulator.Initialize();
		}

		public void Startup()
		{
			emulationThreadRunning = true;
			emulationThreadPaused = false;

			emulator.Startup();
			emulator.Reset();

			emulationThread = new Thread(ThreadMainLoop) { Name = "EssgeeEmulation", Priority = ThreadPriority.AboveNormal };
			emulationThread.Start();
		}

		public void Reset()
		{
			emulator.Reset();
		}

		public void Shutdown()
		{
			emulationThreadRunning = false;

			emulationThread?.Join();

			emulator.Shutdown();
		}

		public void Load(byte[] romData, GameMetadata gameMetadata)
		{
			currentGameMetadata = gameMetadata;

			byte[] ramData = new byte[currentGameMetadata.RamSize];

			var savePath = Path.Combine(Program.SaveDataPath, Path.ChangeExtension(currentGameMetadata.FileName, "sav"));
			if (File.Exists(savePath))
				ramData = File.ReadAllBytes(savePath);

			emulator.Load(romData, ramData, currentGameMetadata.MapperType);

			IsCartridgeLoaded = true;
		}

		public void Save()
		{
			if (currentGameMetadata == null) return;

			var cartRamSaveNeeded = emulator.IsCartridgeRamSaveNeeded();
			if ((cartRamSaveNeeded && currentGameMetadata.MapperType != null && currentGameMetadata.HasNonVolatileRam) ||
				cartRamSaveNeeded)
			{
				var ramData = emulator.GetCartridgeRam();
				var savePath = Path.Combine(Program.SaveDataPath, Path.ChangeExtension(currentGameMetadata.FileName, "sav"));
				File.WriteAllBytes(savePath, ramData);
			}
		}

		public Dictionary<string, dynamic> GetDebugInformation()
		{
			return emulator.GetDebugInformation();
		}

		public Type GetMachineType()
		{
			return emulator.GetType();
		}

		public void SetFpsLimiting(bool value)
		{
			limitFps = value;
		}

		public int FramesPerSecond { get; private set; }

		private void ThreadMainLoop()
		{
			// TODO: rework fps limiter/counter - AGAIN - because the counter is inaccurate at sampleTimespan=0.25 and the limiter CAN cause sound crackling at sampleTimespan>0.25
			// try this maybe? https://stackoverflow.com/a/34839411

			var stopWatch = Stopwatch.StartNew();

			TimeSpan accumulatedTime = TimeSpan.Zero, lastStartTime = TimeSpan.Zero, lastEndTime = TimeSpan.Zero;

			var frameCounter = 0;
			var sampleTimespan = TimeSpan.FromSeconds(0.5);

			while (true)
			{
				if (!emulationThreadRunning)
					break;

				var refreshRate = emulator.RefreshRate;
				var targetElapsedTime = TimeSpan.FromTicks((long)Math.Round(TimeSpan.TicksPerSecond / refreshRate));

				var startTime = stopWatch.Elapsed;

				if (!emulationThreadPaused)
				{
					if (limitFps)
					{
						var elapsedTime = (startTime - lastStartTime);
						lastStartTime = startTime;

						if (elapsedTime < targetElapsedTime)
						{
							accumulatedTime += elapsedTime;

							while (accumulatedTime >= targetElapsedTime)
							{
								emulator.RunFrame();
								frameCounter++;

								accumulatedTime -= targetElapsedTime;
							}
						}
					}
					else
					{
						emulator.RunFrame();
						frameCounter++;
					}

					if ((stopWatch.Elapsed - lastEndTime) >= sampleTimespan)
					{
						FramesPerSecond = (int)((frameCounter * 1000.0) / sampleTimespan.TotalMilliseconds);
						frameCounter = 0;
						lastEndTime = stopWatch.Elapsed;
					}
				}
				else
				{
					lastEndTime = stopWatch.Elapsed;
				}

				if (configChangeRequested)
				{
					emulator.SetConfiguration(newConfiguration);
					configChangeRequested = false;
				}
			}
		}
	}
}
