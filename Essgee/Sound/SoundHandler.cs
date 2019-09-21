using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using OpenTK.Audio;
using OpenTK.Audio.OpenAL;

using Essgee.EventArguments;
using Essgee.Graphics;

namespace Essgee.Sound
{
	public class SoundHandler : IDisposable
	{
		const int numBuffers = 4;

		readonly Action<Exception> exceptionHandler;

		OnScreenDisplayHandler onScreenDisplayHandler;

		public int SampleFrequency { get; private set; }
		public int NumChannels { get; private set; }

		AudioContext context;
		int source;
		EffectsExtension effects;
		int filter;
		int[] buffers;
		Queue<short[]> sampleQueue;

		bool muted;
		float volume;

		Thread audioThread;
		volatile bool audioThreadRunning;

		bool disposed = false;

		public SoundHandler(OnScreenDisplayHandler osdHandler, int sampleFrequency, int numChannels, Action<Exception> exceptionHandler = null)
		{
			this.exceptionHandler = exceptionHandler;

			onScreenDisplayHandler = osdHandler;

			SampleFrequency = sampleFrequency;
			NumChannels = numChannels;

			source = -1;
			filter = -1;
			buffers = new int[numBuffers];
			sampleQueue = new Queue<short[]>();

			InitializeOpenAL();
			InitializeFilters();

			onScreenDisplayHandler.EnqueueMessageSuccess($"Sound initialized; {SampleFrequency} Hz, {NumChannels} channel(s).");
		}

		~SoundHandler()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposed)
				return;

			if (disposing)
			{
				Shutdown();
			}

			context.Dispose();

			disposed = true;
		}

		private void InitializeOpenAL()
		{
			context = new AudioContext();
			source = AL.GenSource();
		}

		private void InitializeFilters()
		{
			effects = new EffectsExtension();
			filter = effects.GenFilter();
			effects.Filter(filter, EfxFilteri.FilterType, (int)EfxFilterType.Lowpass);
			effects.Filter(filter, EfxFilterf.LowpassGain, 0.9f);
			effects.Filter(filter, EfxFilterf.LowpassGainHF, 0.75f);
			AL.Source(source, ALSourcei.EfxDirectFilter, filter);
		}

		public void Startup()
		{
			audioThreadRunning = true;

			buffers = AL.GenBuffers(numBuffers);
			for (int i = 0; i < buffers.Length; i++)
				GenerateBuffer(buffers[i]);
			AL.SourcePlay(source);

			audioThread = new Thread(ThreadMainLoop) { Name = "EssgeeAudio", Priority = ThreadPriority.AboveNormal, IsBackground = true };
			audioThread.Start();
		}

		public void Shutdown()
		{
			audioThreadRunning = false;

			audioThread?.Join();

			foreach (var buffer in buffers.Where(x => AL.IsBuffer(x)))
				AL.DeleteBuffer(buffer);

			sampleQueue.Clear();
		}

		private void ThreadMainLoop()
		{
			try
			{
				while (true)
				{
					if (!audioThreadRunning)
						break;

					AL.GetSource(source, ALGetSourcei.BuffersProcessed, out int buffersProcessed);
					while (buffersProcessed-- > 0)
					{
						int buffer = AL.SourceUnqueueBuffer(source);
						if (buffer != 0)
							GenerateBuffer(buffer);
					}

					AL.GetSource(source, ALGetSourcei.SourceState, out int state);
					if ((ALSourceState)state != ALSourceState.Playing)
						AL.SourcePlay(source);
				}
			}
			catch (Exception ex) when (!Program.AppEnvironment.DebugMode)
			{
				ex.Data.Add("Thread", Thread.CurrentThread.Name);
				exceptionHandler(ex);
			}
		}

		public void SetVolume(float value)
		{
			AL.Source(source, ALSourcef.Gain, volume = value);
		}

		public void SetMute(bool mute)
		{
			AL.Source(source, ALSourcef.Gain, (muted = mute) ? 0.0f : volume);
		}

		public void EnqueueSamples(object sender, EnqueueSamplesEventArgs e)
		{
			if (sampleQueue.Count > 4)
				sampleQueue.Clear();

			sampleQueue.Enqueue(e.Samples);
		}

		public void ClearSampleBuffer()
		{
			sampleQueue.Clear();
		}

		private void GenerateBuffer(int buffer)
		{
			var samples = (sampleQueue.Count > 0 ? sampleQueue.Dequeue() : new short[512]);
			if (samples != null)
			{
				AL.BufferData(buffer, ALFormat.Stereo16, samples, samples.Length * sizeof(short), SampleFrequency);
				AL.SourceQueueBuffer(source, buffer);
			}
		}
	}
}
