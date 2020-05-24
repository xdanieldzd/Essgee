using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Essgee.Emulation.Peripherals.Serial
{
	public interface ISerialDevice
	{
		bool ProvidesClock();
		byte DoSlaveTransfer(byte data);
		byte DoMasterTransfer(byte data);
	}
}
