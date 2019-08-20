using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using System.IO.Compression;

using Essgee.Emulation.Machines;
using Essgee.Utilities;

namespace Essgee.Emulation
{
	public static class CartridgeLoader
	{
		static Dictionary<string, Type> fileExtensionSystemDictionary;

		static CartridgeLoader()
		{
			fileExtensionSystemDictionary = new Dictionary<string, Type>();
			foreach (var machineType in Assembly.GetExecutingAssembly().GetTypes().Where(x => typeof(IMachine).IsAssignableFrom(x) && !x.IsInterface).OrderBy(x => x.GetCustomAttribute<MachineIndexAttribute>()?.Index))
			{
				if (machineType == null) continue;

				var instance = (IMachine)Activator.CreateInstance(machineType);
				fileExtensionSystemDictionary.Add(instance.FileFilter.Extension, machineType);
			}
		}

		public static (Type, byte[]) Load(string fileName)
		{
			Type machineType = null;

			byte[] romData = null;

			var fileExtension = Path.GetExtension(fileName);
			if (fileExtension == ".zip")
			{
				using (var zip = ZipFile.Open(fileName, ZipArchiveMode.Read))
				{
					foreach (var entry in zip.Entries)
					{
						var entryExtension = Path.GetExtension(entry.Name);
						if (fileExtensionSystemDictionary.ContainsKey(entryExtension))
						{
							machineType = fileExtensionSystemDictionary[entryExtension];
							using (var stream = entry.Open())
							{
								romData = new byte[entry.Length];
								stream.Read(romData, 0, romData.Length);
							}
							break;
						}
					}
				}
			}
			else if (fileExtensionSystemDictionary.ContainsKey(fileExtension))
			{
				machineType = fileExtensionSystemDictionary[fileExtension];
				romData = File.ReadAllBytes(fileName);
			}

			if (machineType == null)
				throw new Exception("File not recognized");

			if (romData == null)
				throw new Exception("File failed to load");

			return (machineType, romData);
		}
	}
}
