using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Essgee.Emulation.CPU
{
	interface ICPU
	{
		void Startup();
		void Shutdown();
		void Reset();
		int Step();

		void SetState(Dictionary<string, dynamic> state);
		Dictionary<string, dynamic> GetState();

		void SetStackPointer(ushort value);
		void SetInterruptLine(InterruptType type, InterruptState state);
	}
}
