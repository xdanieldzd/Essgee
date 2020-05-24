using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Essgee.Emulation.Peripherals.Serial
{
	public class DummyDevice : ISerialDevice
	{
		public bool ProvidesClock() { return false; }
		public byte DoSlaveTransfer(byte data) { return 0xFF; }
		public byte DoMasterTransfer(byte data) { return 0xFF; }
	}
}
