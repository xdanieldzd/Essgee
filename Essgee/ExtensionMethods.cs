using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using System.Drawing;
using System.IO.Compression;

using Newtonsoft.Json;

using Essgee.Emulation.Machines;

namespace Essgee
{
	public static class ExtensionMethods
	{
		readonly static Dictionary<string, Type> fileExtensionSystemDictionary = new Dictionary<string, Type>()
		{
			{ ".sg", typeof(SG1000) },
			{ ".sc", typeof(SC3000) },
			{ ".sms", typeof(MasterSystem) },
			{ ".gg", typeof(GameGear) }
		};

		public static T GetAttribute<T>(this ICustomAttributeProvider assembly, bool inherit = false) where T : Attribute
		{
			return assembly.GetCustomAttributes(typeof(T), inherit).OfType<T>().FirstOrDefault();
		}

		public static void SerializeToFile(this object obj, string jsonFileName)
		{
			SerializeToFile(obj, jsonFileName, new JsonSerializerSettings());
		}

		public static void SerializeToFile(this object obj, string jsonFileName, JsonSerializerSettings serializerSettings)
		{
			using (var writer = new StreamWriter(jsonFileName))
			{
				writer.Write(JsonConvert.SerializeObject(obj, Formatting.Indented, serializerSettings));
			}
		}

		public static T DeserializeFromFile<T>(this string jsonFileName)
		{
			using (var reader = new StreamReader(jsonFileName))
			{
				return (T)JsonConvert.DeserializeObject(reader.ReadToEnd(), typeof(T), new JsonSerializerSettings() { Formatting = Formatting.Indented });
			}
		}

		public static T DeserializeObject<T>(this string jsonString)
		{
			return (T)JsonConvert.DeserializeObject(jsonString, typeof(T), new JsonSerializerSettings() { Formatting = Formatting.Indented });
		}

		public static (Type, byte[]) TryLoadCartridge(this string fileName)
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

		public static bool IsEmbeddedResourceAvailable(this Assembly assembly, string resourceName)
		{
			using (var stream = assembly.GetManifestResourceStream(resourceName))
				return (stream != null);
		}

		public static string ReadEmbeddedTextFile(this Assembly assembly, string resourceName)
		{
			using (var stream = assembly.GetManifestResourceStream(resourceName))
			using (var reader = new StreamReader(stream))
				return reader.ReadToEnd();
		}

		public static Bitmap ReadEmbeddedImageFile(this Assembly assembly, string resourceName)
		{
			using (var stream = assembly.GetManifestResourceStream(resourceName))
				return new Bitmap(stream);
		}

		// https://www.c-sharpcorner.com/UploadFile/ff2f08/deep-copy-of-object-in-C-Sharp/
		public static T CloneObject<T>(this T source)
		{
			var type = source.GetType();
			var target = (T)Activator.CreateInstance(source.GetType());

			foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
			{
				if (property.CanWrite)
				{
					if (property.PropertyType.IsValueType || property.PropertyType.IsEnum || property.PropertyType.Equals(typeof(string)))
						property.SetValue(target, property.GetValue(source, null), null);
					else
					{
						object objPropertyValue = property.GetValue(source, null);
						if (objPropertyValue == null)
							property.SetValue(target, null, null);
						else
							property.SetValue(target, objPropertyValue.CloneObject(), null);
					}
				}
			}
			return target;
		}
	}
}
