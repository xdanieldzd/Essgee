using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Essgee.Emulation.Cartridges
{
	public interface ICartridge
	{
		void LoadRom(byte[] data);
		void LoadRam(byte[] data);

		byte[] GetRomData();
		byte[] GetRamData();
		bool IsRamSaveNeeded();

		ushort GetLowerBound();
		ushort GetUpperBound();

		void Step(int clockCyclesInStep);

		byte Read(ushort address);
		void Write(ushort address, byte value);
	}
}
