using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using System.Drawing;
using System.Windows.Forms;

using Newtonsoft.Json;

namespace Essgee
{
	public static class ExtensionMethods
	{
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

		public static void CheckInvokeMethod(this Form form, MethodInvoker methodInvoker)
		{
			if (form.InvokeRequired) form.BeginInvoke(methodInvoker);
			else methodInvoker();
		}
	}
}
