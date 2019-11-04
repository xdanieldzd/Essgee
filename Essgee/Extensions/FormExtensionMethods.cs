using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Essgee.Extensions
{
	public static class FormExtensionMethods
	{
		public static void CheckInvokeMethod(this Form form, MethodInvoker methodInvoker)
		{
			if (form.InvokeRequired) form.BeginInvoke(methodInvoker);
			else methodInvoker();
		}
	}
}
