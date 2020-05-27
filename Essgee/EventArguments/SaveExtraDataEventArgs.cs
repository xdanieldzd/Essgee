using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Essgee.EventArguments
{
	public class SaveExtraDataEventArgs : EventArgs
	{
		public ExtraDataTypes DataType { get; private set; }
		public ExtraDataOptions Options { get; private set; }

		public string Description { get; private set; }
		public object Data { get; private set; }

		public SaveExtraDataEventArgs(ExtraDataTypes type, ExtraDataOptions option, string desc, object data)
		{
			DataType = type;
			Options = option;
			Description = desc;
			Data = data;
		}
	}
}
