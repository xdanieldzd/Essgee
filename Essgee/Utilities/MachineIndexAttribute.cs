using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Essgee.Utilities
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class MachineIndexAttribute : Attribute
	{
		public int Index { get; private set; }

		public MachineIndexAttribute(int index)
		{
			Index = index;
		}
	}
}
