using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using Essgee.Emulation;
using Essgee.Emulation.Configuration;
using Essgee.Utilities;

namespace Essgee
{
	public class Configuration
	{
		public const int RecentFilesCapacity = 15;
		public const string DefaultShaderName = "Basic";

		public bool LimitFps { get; set; }
		public bool ShowFps { get; set; }
		public bool Mute { get; set; }
		public float Volume { get; set; }
		public int ScreenSize { get; set; }
		[JsonConverter(typeof(StringEnumConverter))]
		public ScreenSizeMode ScreenSizeMode { get; set; }
		public string LastShader { get; set; }

		public List<string> RecentFiles { get; set; }

		[JsonConverter(typeof(InterfaceDictionaryConverter<IConfiguration>))]
		public Dictionary<string, IConfiguration> Machines { get; set; }

		public Configuration()
		{
			LimitFps = true;
			ShowFps = false;
			Mute = false;
			Volume = 1.0f;
			ScreenSize = 2;
			ScreenSizeMode = ScreenSizeMode.Scale;
			LastShader = DefaultShaderName;

			RecentFiles = new List<string>(RecentFilesCapacity);

			Machines = new Dictionary<string, IConfiguration>();
			foreach (var machineConfigType in Assembly.GetExecutingAssembly().GetTypes().Where(x => typeof(IConfiguration).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract))
				Machines.Add(machineConfigType.Name, (IConfiguration)Activator.CreateInstance(machineConfigType));
		}
	}
}
