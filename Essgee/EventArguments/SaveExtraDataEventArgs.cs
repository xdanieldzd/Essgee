using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Essgee.EventArguments
{
	public class SaveExtraDataEventArgs : EventArgs
	{
		public string Description { get; private set; }
		public string Extension { get; private set; }
		public bool IncludeDate { get; private set; }
		public object Data { get; private set; }

		public SaveExtraDataEventArgs(string desc, string ext, bool includeDate, object data)
		{
			Description = desc;
			Extension = ext;
			IncludeDate = includeDate;
			Data = data;
		}
	}
}
