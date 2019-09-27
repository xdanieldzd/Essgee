using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Reflection;

using Essgee.Emulation.Configuration;

namespace Essgee
{
	static class Program
	{
		public static class AppEnvironment
		{
#if DEBUG
			public static readonly bool DebugMode = true;
#else
			public static readonly bool DebugMode = false;
#endif
			public static readonly bool EnableCustomUnhandledExceptionHandler = true;

			public static readonly bool EnableSuperSlowCPULogger = false;

			public static readonly bool EnableOpenGLDebug = false;
		}

		const string jsonConfigFileName = "Config.json";
		const string saveDataDirectoryName = "Saves";
		const string screenshotDirectoryName = "Screenshots";
		const string saveStateDirectoryName = "Savestates";

		readonly static string programDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), Application.ProductName);
		readonly static string programConfigPath = Path.Combine(programDataDirectory, jsonConfigFileName);

		public static Configuration Configuration { get; set; }

		public static string ShaderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Shaders");
		public static string SaveDataPath = Path.Combine(programDataDirectory, saveDataDirectoryName);
		public static string ScreenshotPath = Path.Combine(programDataDirectory, screenshotDirectoryName);
		public static string SaveStatePath = Path.Combine(programDataDirectory, saveStateDirectoryName);

		[STAThread]
		static void Main()
		{
			System.Threading.Thread.CurrentThread.CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

			Application.SetUnhandledExceptionMode(AppEnvironment.EnableCustomUnhandledExceptionHandler ? UnhandledExceptionMode.ThrowException : UnhandledExceptionMode.CatchException);

			LoadConfiguration();

			if (!Directory.Exists(SaveDataPath))
				Directory.CreateDirectory(SaveDataPath);

			if (!Directory.Exists(ScreenshotPath))
				Directory.CreateDirectory(ScreenshotPath);

			if (!Directory.Exists(SaveStatePath))
				Directory.CreateDirectory(SaveStatePath);

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new MainForm());
		}

		private static void LoadConfiguration()
		{
			Directory.CreateDirectory(programDataDirectory);

			if (!File.Exists(programConfigPath) || (Configuration = programConfigPath.DeserializeFromFile<Configuration>()) == null)
			{
				Configuration = new Configuration();
				Configuration.SerializeToFile(programConfigPath);
			}

			foreach (var machineConfigType in Assembly.GetExecutingAssembly().GetTypes().Where(x => typeof(IConfiguration).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract))
			{
				if (!Configuration.Machines.ContainsKey(machineConfigType.Name))
					Configuration.Machines.Add(machineConfigType.Name, (IConfiguration)Activator.CreateInstance(machineConfigType));
			}
		}

		public static void SaveConfiguration()
		{
			Configuration.SerializeToFile(programConfigPath);
		}
	}
}
