using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

using Essgee.Exceptions;
using Essgee.Utilities;

namespace Essgee.Emulation
{
	public static class SaveStateHandler
	{
		public static string ExpectedVersion = $"ESGST{new Version(Application.ProductVersion).Major:D3}";

		public static Dictionary<string, dynamic> Load(Stream stream, string machineName)
		{
			stream.Position = 0;

			using (var reader = new BinaryReader(stream))
			{
				// Read and check version string
				var version = Encoding.ASCII.GetString(reader.ReadBytes(ExpectedVersion.Length));
				if (version != ExpectedVersion) throw new EmulationException("Unsupported savestate version");

				// Read and check filesize
				var filesize = reader.ReadUInt32();
				if (filesize != reader.BaseStream.Length) throw new EmulationException("Savestate filesize mismatch");

				// Read CRC32
				var crc32 = reader.ReadUInt32();

				// Read and check machine ID
				var machineId = Encoding.ASCII.GetString(reader.ReadBytes(16));
				if (machineId != GenerateMachineIdString(machineName)) throw new EmulationException("Savestate machine mismatch");

				// Check CRC32
				using (var stateStream = new MemoryStream())
				{
					reader.BaseStream.CopyTo(stateStream);
					stateStream.Position = 0;
					var expectedCrc32 = Crc32.Calculate(stateStream);
					if (crc32 != expectedCrc32) throw new EmulationException("Savestate checksum error");

					// Read state data
					var binaryFormatter = new BinaryFormatter();
					return (binaryFormatter.Deserialize(stateStream) as Dictionary<string, dynamic>);
				}
			}
		}

		public static void Save(Stream stream, string machineName, Dictionary<string, dynamic> state)
		{
			using (var writer = new BinaryWriter(new MemoryStream()))
			{
				// Write version string
				writer.Write(Encoding.ASCII.GetBytes(ExpectedVersion));

				// Write filesize placeholder
				var filesizePosition = writer.BaseStream.Position;
				writer.Write(uint.MaxValue);

				// Write CRC32 placeholder
				var crc32Position = writer.BaseStream.Position;
				writer.Write(uint.MaxValue);

				// Write machine ID
				writer.Write(Encoding.ASCII.GetBytes(GenerateMachineIdString(machineName)));

				// Write state data
				var binaryFormatter = new BinaryFormatter();
				binaryFormatter.Serialize(writer.BaseStream, state);

				// Write filesize
				var lastOffset = writer.BaseStream.Position;
				writer.BaseStream.Position = filesizePosition;
				writer.Write((uint)writer.BaseStream.Length);
				writer.BaseStream.Position = lastOffset;

				// Write CRC32
				lastOffset = writer.BaseStream.Position;

				writer.BaseStream.Position = 0;
				var crc32 = Crc32.Calculate(writer.BaseStream, 0x20, (int)writer.BaseStream.Length - 0x20);

				writer.BaseStream.Position = crc32Position;
				writer.Write(crc32);
				writer.BaseStream.Position = lastOffset;

				// Copy to file
				writer.BaseStream.Position = 0;
				writer.BaseStream.CopyTo(stream);
			}
		}

		private static string GenerateMachineIdString(string machineId)
		{
			return machineId.Substring(0, Math.Min(machineId.Length, 16)).PadRight(16);
		}

		public static void PerformSetState(object obj, Dictionary<string, dynamic> state)
		{
			if (obj != null)
			{
				foreach (var prop in obj.GetType().GetProperties(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Where(x => x.GetCustomAttributes(typeof(StateRequiredAttribute), false).Length != 0))
				{
					prop.SetValue(obj, state[prop.Name]);
				}

				foreach (var field in obj.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Where(x => x.GetCustomAttributes(typeof(StateRequiredAttribute), false).Length != 0))
				{
					field.SetValue(obj, state[field.Name]);
				}
			}
		}

		public static Dictionary<string, dynamic> PerformGetState(object obj)
		{
			var state = new Dictionary<string, dynamic>();

			if (obj != null)
			{
				foreach (var prop in obj.GetType().GetProperties(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Where(x => x.GetCustomAttributes(typeof(StateRequiredAttribute), false).Length != 0))
				{
					state.Add(prop.Name, prop.GetValue(obj));
				}

				foreach (var field in obj.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Where(x => x.GetCustomAttributes(typeof(StateRequiredAttribute), false).Length != 0))
				{
					state.Add(field.Name, field.GetValue(obj));
				}
			}

			return state;
		}
	}
}
