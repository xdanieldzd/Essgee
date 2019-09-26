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

		void SetState(Dictionary<string, dynamic> state);
		Dictionary<string, dynamic> GetState();

		byte ReadPort(byte port);
		void WritePort(byte port, byte value);
	}
}
