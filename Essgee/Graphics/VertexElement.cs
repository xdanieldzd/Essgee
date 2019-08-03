using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Essgee.Graphics
{
	public sealed class VertexElement
	{
		public int AttributeIndex { get; internal set; }
		public Type DataType { get; internal set; }
		public int NumComponents { get; internal set; }
		public int OffsetInVertex { get; internal set; }
		public string Name { get; internal set; }

		public VertexElement()
		{
			AttributeIndex = -1;
			DataType = null;
			NumComponents = -1;
			OffsetInVertex = -1;
			Name = string.Empty;
		}
	}
}
