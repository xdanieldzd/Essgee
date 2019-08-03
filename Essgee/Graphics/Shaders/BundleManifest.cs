using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Essgee.Graphics.Shaders
{
	public class BundleManifest
	{
		[JsonConverter(typeof(StringEnumConverter))]
		public FilterMode Filter { get; set; }
		[JsonConverter(typeof(StringEnumConverter))]
		public WrapMode Wrap { get; set; }
		public int Samplers { get; set; }

		public BundleManifest()
		{
			Filter = FilterMode.Linear;
			Wrap = WrapMode.Repeat;
			Samplers = 3;
		}
	}
}
