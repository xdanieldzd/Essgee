using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Essgee.Emulation.Peripherals
{
	interface IPeripheral
	{
		void Startup();
		void Shutdown();
		void Reset();

		byte ReadPort(byte port);
		void WritePort(byte port, byte value);
	}
}
