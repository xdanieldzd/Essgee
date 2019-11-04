using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using System.Drawing;

namespace Essgee.Extensions
{
	public static class AssemblyExtensionMethods
	{
		public static T GetAttribute<T>(this ICustomAttributeProvider assembly, bool inherit = false) where T : Attribute
		{
			return assembly.GetCustomAttributes(typeof(T), inherit).OfType<T>().FirstOrDefault();
		}

		public static bool IsEmbeddedResourceAvailable(this Assembly assembly, string resourceName)
		{
			using (var stream = assembly.GetManifestResourceStream(resourceName))
				return (stream != null);
		}

		public static string ReadEmbeddedTextFile(this Assembly assembly, string resourceName)
		{
			using (var reader = new StreamReader(assembly.GetManifestResourceStream(resourceName)))
				return reader.ReadToEnd();
		}

		public static Bitmap ReadEmbeddedImageFile(this Assembly assembly, string resourceName)
		{
			using (var stream = assembly.GetManifestResourceStream(resourceName))
				return new Bitmap(stream);
		}
	}
}
