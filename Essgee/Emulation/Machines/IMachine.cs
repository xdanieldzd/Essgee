using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Essgee.EventArguments;
using Essgee.Emulation.Configuration;

namespace Essgee.Emulation.Machines
{
	public interface IMachine
	{
		event EventHandler<SendLogMessageEventArgs> SendLogMessage;
		event EventHandler<EventArgs> EmulationReset;
		event EventHandler<RenderScreenEventArgs> RenderScreen;
		event EventHandler<SizeScreenEventArgs> SizeScreen;
		event EventHandler<ChangeViewportEventArgs> ChangeViewport;
		event EventHandler<PollInputEventArgs> PollInput;
		event EventHandler<EnqueueSamplesEventArgs> EnqueueSamples;

		string ManufacturerName { get; }
		string ModelName { get; }
		string DatFilename { get; }
		double RefreshRate { get; }

		Dictionary<string, dynamic> GetDebugInformation();

		void SetConfiguration(IConfiguration config);

		void Initialize();
		void Startup();
		void Reset();
		void Shutdown();

		void Load(byte[] romData, byte[] ramData, Type mapperType);
		byte[] GetCartridgeRam();
		bool IsCartridgeRamSaveNeeded();

		void RunFrame();
	}
}
