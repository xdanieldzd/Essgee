﻿using System;
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

		public int SampleRate { get; private set; }
		public int NumChannels { get; private set; }

		public int MaxQueueLength { get; set; }

		AudioContext context;
		int source;
		EffectsExtension effects;
		int filter;
		int[] buffers;

		Queue<short[]> sampleQueue;
		short[] lastSamples;

		bool muted;
		float volume;

		Thread audioThread;
		volatile bool audioThreadRunning;

		bool disposed = false;

		public SoundHandler(OnScreenDisplayHandler osdHandler, int sampleRate, int numChannels, Action<Exception> exceptionHandler = null)
		{
			this.exceptionHandler = exceptionHandler;

			onScreenDisplayHandler = osdHandler;

			SampleRate = sampleRate;
			NumChannels = numChannels;

			MaxQueueLength = 2;

			source = -1;
			filter = -1;
			buffers = new int[numBuffers];

			sampleQueue = new Queue<short[]>();
			lastSamples = new short[512];

			InitializeOpenAL();
			InitializeFilters();

			onScreenDisplayHandler.EnqueueMessageSuccess($"Sound initialized; {SampleRate} Hz, {NumChannels} channel(s).");
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

		public void SetLowPassFilter(bool enable)
		{
			AL.Source(source, ALSourcei.EfxDirectFilter, (enable ? filter : 0));
		}

		public void EnqueueSamples(object sender, EnqueueSamplesEventArgs e)
		{
			if (sampleQueue.Count > MaxQueueLength)
			{
				var samplesToDrop = (sampleQueue.Count - MaxQueueLength);
				onScreenDisplayHandler.EnqueueMessageDebug($"({GetType().Name}/{DateTime.Now.Second:D2}s) Sample queue overflow; dropping {samplesToDrop} of {sampleQueue.Count} samples.");
				for (int i = 0; i < samplesToDrop; i++) sampleQueue.Dequeue();
			}

			sampleQueue.Enqueue(e.MixedSamples.ToArray());
		}

		public void ClearSampleBuffer()
		{
			sampleQueue.Clear();
			for (int i = 0; i < lastSamples.Length; i++)
				lastSamples[i] = 0;
		}

		private void GenerateBuffer(int buffer)
		{
			if (sampleQueue.Count > 0)
				lastSamples = sampleQueue.Dequeue();

			AL.BufferData(buffer, ALFormat.Stereo16, lastSamples, lastSamples.Length * sizeof(short), SampleRate);
			AL.SourceQueueBuffer(source, buffer);
		}
	}
}
