using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.IO;
using System.IO.MemoryMappedFiles;

using Essgee.EventArguments;

namespace Essgee.Emulation.ExtDevices.Nintendo
{
	[Description("Game Boy (Link Cable)")]
	[ElementPriority(1)]
	public class GameBoyIPC : ISerialDevice
	{
		// TODO: ensure correct mmf/accessor disposal?

		const string ipcName = "EssgeeGBLink";
		const int ipcLength = 16;

		const int ipcOffsetSerialData = 0;
		//

		public event EventHandler<SaveExtraDataEventArgs> SaveExtraData;
		protected virtual void OnSaveExtraData(SaveExtraDataEventArgs e) { SaveExtraData?.Invoke(this, e); }

		public bool ProvidesClock { get; private set; }

		MemoryMappedFile mmf;
		MemoryMappedViewAccessor accessor;

		bool ipcConnectionExists;

		public GameBoyIPC()
		{
			mmf = null;
			accessor = null;

			ipcConnectionExists = false;
		}

		public void Initialize()
		{
			//
		}

		public void Shutdown()
		{
			if (ipcConnectionExists)
			{
				accessor.Flush();
				accessor.Dispose();
				mmf.Dispose();

				ipcConnectionExists = false;
			}
		}

		private void EstablishIPCConnection()
		{
			if (ipcConnectionExists) return;

			try
			{
				// Try to open existing mapped file; if it exists, assume other instance provides clock
				mmf = MemoryMappedFile.OpenExisting(ipcName);
				ProvidesClock = false;
			}
			catch (FileNotFoundException)
			{
				// Mapped file does not yet exist, create file and assume this instance provides clock
				mmf = MemoryMappedFile.CreateOrOpen(ipcName, ipcLength);
				ProvidesClock = true;
			}
			accessor = mmf.CreateViewAccessor(0, ipcLength, MemoryMappedFileAccess.ReadWrite);

			ipcConnectionExists = true;
		}

		public byte DoSlaveTransfer(byte data)
		{
			if (!ipcConnectionExists) EstablishIPCConnection();

			var serialDataReceived = accessor.ReadByte(ipcOffsetSerialData);
			accessor.Write(ipcOffsetSerialData, data);

			Program.Logger?.WriteLine($"{System.Reflection.MethodBase.GetCurrentMethod().Name}: recv 0x{serialDataReceived:X2}, sent 0x{data:X2}");

			return serialDataReceived;
		}

		public byte DoMasterTransfer(byte data)
		{
			if (!ipcConnectionExists) EstablishIPCConnection();

			var serialDataReceived = accessor.ReadByte(ipcOffsetSerialData);
			accessor.Write(ipcOffsetSerialData, data);

			Program.Logger?.WriteLine($"{System.Reflection.MethodBase.GetCurrentMethod().Name}: recv 0x{serialDataReceived:X2}, sent 0x{data:X2}");

			return serialDataReceived;
		}
	}
}
