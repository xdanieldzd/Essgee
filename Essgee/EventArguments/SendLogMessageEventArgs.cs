using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Essgee.EventArguments
{
	public class SendLogMessageEventArgs : EventArgs
	{
		public string Message { get; private set; }

		public SendLogMessageEventArgs(string message)
		{
			Message = message;
		}
	}
}
