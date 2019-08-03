using System;
using System.IO;

namespace Essgee.Utilities
{
	public static class Crc32
	{
		static readonly uint[] crcTable;
		static readonly uint crcPolynomial = 0xEDB88320;
		static readonly uint crcSeed = 0xFFFFFFFF;

		static Crc32()
		{
			crcTable = new uint[256];

			for (int i = 0; i < 256; i++)
			{
				uint entry = (uint)i;
				for (int j = 0; j < 8; j++)
				{
					if ((entry & 0x00000001) == 0x00000001)
						entry = (entry >> 1) ^ crcPolynomial;
					else
						entry = (entry >> 1);
				}
				crcTable[i] = entry;
			}
		}

		public static uint Calculate(FileInfo fileInfo)
		{
			return Calculate(fileInfo, 0, (int)fileInfo.Length);
		}

		public static uint Calculate(FileInfo fileInfo, int start, int length)
		{
			if (start >= fileInfo.Length) throw new Crc32Exception("Start offset is greater than file size");
			if (length > fileInfo.Length) throw new Crc32Exception("Length is greater than file size");
			if ((start + length) > fileInfo.Length) throw new Crc32Exception("End offset is greater than file size");

			uint crc = 0;
			using (FileStream file = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			{
				byte[] data = new byte[length];
				file.Read(data, start, length);
				crc = Calculate(data, 0, data.Length);
			}
			return crc;
		}

		public static uint Calculate(byte[] data)
		{
			return Calculate(data, 0, data.Length);
		}

		public static uint Calculate(byte[] data, int start, int length)
		{
			if (start >= data.Length) throw new Crc32Exception("Start offset is greater than array size");
			if (length > data.Length) throw new Crc32Exception("Length is greater than array size");
			if ((start + length) > data.Length) throw new Crc32Exception("End offset is greater than array size");

			uint crc = crcSeed;
			for (int i = start; i < (start + length); i++)
				crc = ((crc >> 8) ^ crcTable[data[i] ^ (crc & 0x000000FF)]);
			return ~crc;
		}
	}

	public class Crc32Exception : Exception
	{
		public Crc32Exception() : base() { }
		public Crc32Exception(string message) : base(message) { }
		public Crc32Exception(string message, Exception innerException) : base(message, innerException) { }
		public Crc32Exception(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}
}
