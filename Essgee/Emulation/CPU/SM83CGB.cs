using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Essgee.Emulation.CPU
{
	public class SM83CGB : SM83
	{
		// TODO: better way of implementing this?
		public bool IsDoubleSpeed { get; private set; }

		public SM83CGB(MemoryReadDelegate memoryRead, MemoryWriteDelegate memoryWrite) : base(memoryRead, memoryWrite) { }

		protected override void Stop()
		{
			pc++;

			// Perform speed switch; get IO register value
			var key1 = memoryReadDelegate(0xFF4D);

			// Is speed switch pending?
			if ((key1 & 0b1) != 0)
			{
				// Clear pending flag
				key1 &= 0xFE;

				if (((key1 >> 7) & 0b1) != 0)
				{
					// Was double speed, now normal speed
					key1 &= 0x7F;
					IsDoubleSpeed = false;
				}
				else
				{
					// Was normal speed, now double speed
					key1 |= 0x80;
					IsDoubleSpeed = true;
				}

				// Write register value
				memoryWriteDelegate(0xFF4D, key1);
			}
		}
	}
}
