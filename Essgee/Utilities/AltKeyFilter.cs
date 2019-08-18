using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Essgee.Utilities
{
	class AltKeyFilter : IMessageFilter
	{
		public bool PreFilterMessage(ref Message m)
		{
			return (m.Msg == 0x0104 && ((int)m.LParam & 0x20000000) != 0);
		}
	}
}
