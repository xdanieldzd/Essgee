using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Essgee.Emulation.Cartridges
{
	interface ICartridge
	{
		void LoadRom(byte[] data);
		void LoadRam(byte[] data);

		void SetState(Dictionary<string, dynamic> state);
		Dictionary<string, dynamic> GetState();

		byte[] GetRomData();
		byte[] GetRamData();
		bool IsRamSaveNeeded();

		ushort GetLowerBound();
		ushort GetUpperBound();

		byte Read(ushort address);
		void Write(ushort address, byte value);
	}
}
