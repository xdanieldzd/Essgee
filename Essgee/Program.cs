using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace Essgee
{
	static class Program
	{
		const string jsonConfigFileName = "Config.json";
		const string saveDataDirectoryName = "Saves";
		const string screenshotDirectoryName = "Screenshots";

		readonly static string programDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), Application.ProductName);
		readonly static string programConfigPath = Path.Combine(programDataDirectory, jsonConfigFileName);

		public static Configuration Configuration { get; set; }

		public static string ShaderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Shaders");
		public static string SaveDataPath = Path.Combine(programDataDirectory, saveDataDirectoryName);
		public static string ScreenshotPath = Path.Combine(programDataDirectory, screenshotDirectoryName);

		[STAThread]
		static void Main()
		{
			System.Threading.Thread.CurrentThread.CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

			Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

			LoadConfiguration();

			if (!Directory.Exists(SaveDataPath))
				Directory.CreateDirectory(SaveDataPath);

			if (!Directory.Exists(ScreenshotPath))
				Directory.CreateDirectory(ScreenshotPath);

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
		}

		public static void SaveConfiguration()
		{
			Configuration.SerializeToFile(programConfigPath);
		}
	}
}
