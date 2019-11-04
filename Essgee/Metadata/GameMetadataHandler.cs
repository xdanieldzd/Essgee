using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Serialization;
using System.ComponentModel;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using Essgee.Emulation;
using Essgee.Exceptions;
using Essgee.Extensions;
using Essgee.Graphics;
using Essgee.Utilities;

namespace Essgee.Metadata
{
	public class GameMetadataHandler
	{
		readonly static string datDirectoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "No-Intro");
		readonly static string metadataDatabaseFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "MetadataDatabase.json");

		readonly Dictionary<string, DatFile> datFiles;
		readonly List<CartridgeJSON> cartMetadataDatabase;

		OnScreenDisplayHandler onScreenDisplayHandler;

		public int NumKnownSystems { get { return datFiles.Count; } }
		public int NumKnownGames { get { return datFiles.Sum(x => x.Value.Game.Count()); } }

		public GameMetadataHandler(OnScreenDisplayHandler osdHandler)
		{
			onScreenDisplayHandler = osdHandler;

			XmlRootAttribute root;
			XmlSerializer serializer;

			/* Read No-Intro .dat files */
			datFiles = new Dictionary<string, DatFile>();
			foreach (var file in Directory.EnumerateFiles(datDirectoryPath, "*.dat"))
			{
				root = new XmlRootAttribute("datafile") { IsNullable = true };
				serializer = new XmlSerializer(typeof(DatFile), root);
				using (FileStream stream = new FileStream(Path.Combine(datDirectoryPath, file), FileMode.Open))
				{
					datFiles.Add(Path.GetFileName(file), (DatFile)serializer.Deserialize(stream));
				}
			}

			/* Read cartridge metadata database */
			cartMetadataDatabase = metadataDatabaseFilePath.DeserializeFromFile<List<CartridgeJSON>>();

			onScreenDisplayHandler.EnqueueMessageSuccess($"Metadata initialized; {NumKnownGames} game(s) known across {NumKnownSystems} system(s).");
		}

		public GameMetadata GetGameMetadata(string datFilename, string romFilename, uint romCrc32, int romSize)
		{
			/* Sanity checks */
			if (!datFiles.ContainsKey(datFilename)) throw new HandlerException("Requested .dat file not found");

			/* Get information from No-Intro .dat */
			var datFile = datFiles[datFilename];
			var crcString = string.Format("{0:X8}", romCrc32);
			var sizeString = string.Format("{0:D}", romSize);
			var gameInfo = datFile.Game.FirstOrDefault(x => x.Rom.Any(y => y.Crc == crcString && y.Size == sizeString));

			/* Get information from cartridge metadata database */
			var cartridgeInfo = cartMetadataDatabase.FirstOrDefault(x => x.Crc32 == romCrc32 && x.RomSize == romSize);

			/* Create game metadata */
			var gameMetadata = new GameMetadata()
			{
				FileName = Path.GetFileName(romFilename),
				KnownName = (gameInfo?.Name ?? "unrecognized game"),
				RomCrc32 = romCrc32,
				RomSize = romSize
			};

			if (cartridgeInfo != null)
			{
				gameMetadata.RamSize = cartridgeInfo.RamSize;
				gameMetadata.MapperType = cartridgeInfo.Mapper;
				gameMetadata.HasNonVolatileRam = cartridgeInfo.HasNonVolatileRam;
				gameMetadata.PreferredTVStandard = cartridgeInfo.PreferredTVStandard;
				gameMetadata.PreferredRegion = cartridgeInfo.PreferredRegion;
				gameMetadata.AllowMemoryControl = cartridgeInfo.AllowMemoryControl;
			}

			return gameMetadata;
		}

		public class CartridgeJSON
		{
			[JsonProperty(Required = Required.Always), JsonConverter(typeof(HexadecimalJsonConverter))]
			public uint Crc32 { get; set; } = 0xFFFFFFFF;

			[JsonProperty(Required = Required.Always)]
			public int RomSize { get; set; } = 0;

			[JsonProperty(Required = Required.Default), DefaultValue(0)]
			public int RamSize { get; set; } = 0;

			[JsonProperty(Required = Required.Default), JsonConverter(typeof(TypeNameJsonConverter), "Essgee.Emulation.Cartridges"), DefaultValue(null)]
			public Type Mapper { get; set; } = null;

			[JsonProperty(Required = Required.Default), DefaultValue(false)]
			public bool HasNonVolatileRam { get; set; } = false;

			[JsonProperty(Required = Required.Default), JsonConverter(typeof(StringEnumConverter)), DefaultValue(TVStandard.Auto)]
			public TVStandard PreferredTVStandard { get; set; } = TVStandard.Auto;

			[JsonProperty(Required = Required.Default), JsonConverter(typeof(StringEnumConverter)), DefaultValue(Region.Auto)]
			public Region PreferredRegion { get; set; } = Region.Auto;

			[JsonProperty(Required = Required.Default), DefaultValue(true)]
			public bool AllowMemoryControl { get; set; } = true;
		}

		public class DatHeader
		{
			[XmlElement("name")]
			public string Name { get; set; }
			[XmlElement("description")]
			public string Description { get; set; }
			[XmlElement("category")]
			public string Category { get; set; }
			[XmlElement("version")]
			public string Version { get; set; }
			[XmlElement("date")]
			public string Date { get; set; }
			[XmlElement("author")]
			public string Author { get; set; }
			[XmlElement("email")]
			public string Email { get; set; }
			[XmlElement("homepage")]
			public string Homepage { get; set; }
			[XmlElement("url")]
			public string Url { get; set; }
			[XmlElement("comment")]
			public string Comment { get; set; }
		}

		public class DatRelease
		{
			[XmlAttribute("name")]
			public string Name { get; set; }
			[XmlAttribute("region")]
			public string Region { get; set; }
			[XmlAttribute("language")]
			public string Language { get; set; }
			[XmlAttribute("date")]
			public string Date { get; set; }
			[XmlAttribute("default")]
			public string Default { get; set; }
		}

		public class DatBiosSet
		{
			[XmlAttribute("name")]
			public string Name { get; set; }
			[XmlAttribute("description")]
			public string Description { get; set; }
			[XmlAttribute("default")]
			public string Default { get; set; }
		}

		public class DatRom
		{
			[XmlAttribute("name")]
			public string Name { get; set; }
			[XmlAttribute("size")]
			public string Size { get; set; }
			[XmlAttribute("crc")]
			public string Crc { get; set; }
			[XmlAttribute("sha1")]
			public string Sha1 { get; set; }
			[XmlAttribute("md5")]
			public string Md5 { get; set; }
			[XmlAttribute("merge")]
			public string Merge { get; set; }
			[XmlAttribute("status")]
			public string Status { get; set; }
			[XmlAttribute("date")]
			public string Date { get; set; }
		}

		public class DatDisk
		{
			[XmlAttribute("name")]
			public string Name { get; set; }
			[XmlAttribute("sha1")]
			public string Sha1 { get; set; }
			[XmlAttribute("md5")]
			public string Md5 { get; set; }
			[XmlAttribute("merge")]
			public string Merge { get; set; }
			[XmlAttribute("status")]
			public string Status { get; set; }
		}

		public class DatSample
		{
			[XmlAttribute("name")]
			public string Name { get; set; }
		}

		public class DatArchive
		{
			[XmlAttribute("name")]
			public string Name { get; set; }
		}

		public class DatGame
		{
			[XmlAttribute("name")]
			public string Name { get; set; }
			[XmlAttribute("sourcefile")]
			public string SourceFile { get; set; }
			[XmlAttribute("isbios")]
			public string IsBios { get; set; }
			[XmlAttribute("cloneof")]
			public string CloneOf { get; set; }
			[XmlAttribute("romof")]
			public string RomOf { get; set; }
			[XmlAttribute("sampleof")]
			public string SampleOf { get; set; }
			[XmlAttribute("board")]
			public string Board { get; set; }
			[XmlAttribute("rebuildto")]
			public string RebuildTo { get; set; }

			[XmlElement("year")]
			public string Year { get; set; }
			[XmlElement("manufacturer")]
			public string Manufacturer { get; set; }

			[XmlElement("release")]
			public DatRelease[] Release { get; set; }

			[XmlElement("biosset")]
			public DatBiosSet[] BiosSet { get; set; }

			[XmlElement("rom")]
			public DatRom[] Rom { get; set; }

			[XmlElement("disk")]
			public DatDisk[] Disk { get; set; }

			[XmlElement("sample")]
			public DatSample[] Sample { get; set; }

			[XmlElement("archive")]
			public DatArchive[] Archive { get; set; }
		}

		[Serializable()]
		public class DatFile
		{
			[XmlElement("header")]
			public DatHeader Header { get; set; }

			[XmlElement("game")]
			public DatGame[] Game { get; set; }
		}
	}
}
