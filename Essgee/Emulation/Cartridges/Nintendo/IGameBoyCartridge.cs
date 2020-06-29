using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Essgee.Emulation.Cartridges.Nintendo
{
	public interface IGameBoyCartridge : ICartridge
	{
		void SetCartridgeConfig(bool battery, bool rtc, bool rumble);
	}
}
