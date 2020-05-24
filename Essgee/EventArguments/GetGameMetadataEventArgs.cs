using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Essgee.Metadata;

namespace Essgee.EventArguments
{
	public class GetGameMetadataEventArgs
	{
		public GameMetadata Metadata { get; set; }

		public GetGameMetadataEventArgs()
		{
			Metadata = null;
		}
	}
}
