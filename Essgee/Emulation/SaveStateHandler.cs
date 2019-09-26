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

		public static Dictionary<string, dynamic> Load(Stream stream)
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

				// Read and check CRC32
				var crc32 = reader.ReadUInt32();
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

		public static void Save(Stream stream, Dictionary<string, dynamic> state)
		{
			using (var writer = new BinaryWriter(new MemoryStream()))
			{
				// Write version string
				var versionBytes = Encoding.ASCII.GetBytes(ExpectedVersion);
				writer.Write(versionBytes);

				// Write filesize placeholder
				var filesizePosition = writer.BaseStream.Position;
				writer.Write(uint.MaxValue);

				// Write CRC32 placeholder
				var crc32Position = writer.BaseStream.Position;
				writer.Write(uint.MaxValue);

				// Write state data
				var binaryFormatter = new BinaryFormatter();
				binaryFormatter.Serialize(writer.BaseStream, state);
				//WriteStateData(writer, state);

				// Write filesize
				var lastOffset = writer.BaseStream.Position;
				writer.BaseStream.Position = filesizePosition;
				writer.Write((uint)writer.BaseStream.Length);
				writer.BaseStream.Position = lastOffset;

				// Write CRC32
				lastOffset = writer.BaseStream.Position;

				writer.BaseStream.Position = 0;
				var crc32 = Crc32.Calculate(writer.BaseStream, 0x10, (int)writer.BaseStream.Length - 0x10);

				writer.BaseStream.Position = crc32Position;
				writer.Write(crc32);
				writer.BaseStream.Position = lastOffset;

				// Copy to file
				writer.BaseStream.Position = 0;
				writer.BaseStream.CopyTo(stream);
			}
		}
	}
}
