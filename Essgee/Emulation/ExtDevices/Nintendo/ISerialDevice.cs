using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Essgee.EventArguments;

namespace Essgee.Emulation.ExtDevices.Nintendo
{
	public interface ISerialDevice
	{
		event EventHandler<SaveExtraDataEventArgs> SaveExtraData;

		void Initialize();
		void Shutdown();

		byte ExchangeBit(int left, byte data);
	}
}
