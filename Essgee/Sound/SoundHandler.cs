using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

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

		// TODO properly implement saving wav files
		bool saveWav = false;
		WaveHeader waveHeader;
		FormatChunk formatChunk;
		DataChunk dataChunk;

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

			// TODO
			if (saveWav)
			{
				waveHeader = new WaveHeader();
				formatChunk = new FormatChunk(sampleRate, numChannels);
				dataChunk = new DataChunk();
				waveHeader.FileLength += formatChunk.Length();
			}
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

			// TODO
			if (saveWav)
			{
				using (FileStream file = new FileStream(@"D:\Temp\Essgee\out.wav", FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
				{
					file.Write(waveHeader.GetBytes(), 0, (int)waveHeader.Length());
					file.Write(formatChunk.GetBytes(), 0, (int)formatChunk.Length());
					file.Write(dataChunk.GetBytes(), 0, (int)dataChunk.Length());
				}
			}
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

			// TODO
			if (saveWav)
			{
				dataChunk.AddSampleData(e.MixedSamples);
				waveHeader.FileLength += (uint)e.MixedSamples.Length;
			}
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

		class WaveHeader
		{
			const string fileTypeId = "RIFF";
			const string mediaTypeId = "WAVE";

			public string FileTypeId { get; private set; }
			public uint FileLength { get; set; }
			public string MediaTypeId { get; private set; }

			public WaveHeader()
			{
				FileTypeId = fileTypeId;
				MediaTypeId = mediaTypeId;
				FileLength = 4;     /* Minimum size is always 4 bytes */
			}

			public byte[] GetBytes()
			{
				List<byte> chunkData = new List<byte>();

				chunkData.AddRange(Encoding.ASCII.GetBytes(FileTypeId));
				chunkData.AddRange(BitConverter.GetBytes(FileLength));
				chunkData.AddRange(Encoding.ASCII.GetBytes(MediaTypeId));

				return chunkData.ToArray();
			}

			public uint Length()
			{
				return (uint)GetBytes().Length;
			}
		}

		class FormatChunk
		{
			const string chunkId = "fmt ";

			ushort bitsPerSample, channels;
			uint frequency;

			public string ChunkId { get; private set; }
			public uint ChunkSize { get; private set; }
			public ushort FormatTag { get; private set; }

			public ushort Channels
			{
				get { return channels; }
				set { channels = value; RecalcBlockSizes(); }
			}

			public uint Frequency
			{
				get { return frequency; }
				set { frequency = value; RecalcBlockSizes(); }
			}

			public uint AverageBytesPerSec { get; private set; }
			public ushort BlockAlign { get; private set; }

			public ushort BitsPerSample
			{
				get { return bitsPerSample; }
				set { bitsPerSample = value; RecalcBlockSizes(); }
			}

			public FormatChunk()
			{
				ChunkId = chunkId;
				ChunkSize = 16;
				FormatTag = 1;          /* MS PCM (Uncompressed wave file) */
				Channels = 2;           /* Default to stereo */
				Frequency = 44100;      /* Default to 44100hz */
				BitsPerSample = 16;     /* Default to 16bits */
				RecalcBlockSizes();
			}

			public FormatChunk(int frequency, int channels) : this()
			{
				Channels = (ushort)channels;
				Frequency = (ushort)frequency;
				RecalcBlockSizes();
			}

			private void RecalcBlockSizes()
			{
				BlockAlign = (ushort)(channels * (bitsPerSample / 8));
				AverageBytesPerSec = frequency * BlockAlign;
			}

			public byte[] GetBytes()
			{
				List<byte> chunkBytes = new List<byte>();

				chunkBytes.AddRange(Encoding.ASCII.GetBytes(ChunkId));
				chunkBytes.AddRange(BitConverter.GetBytes(ChunkSize));
				chunkBytes.AddRange(BitConverter.GetBytes(FormatTag));
				chunkBytes.AddRange(BitConverter.GetBytes(Channels));
				chunkBytes.AddRange(BitConverter.GetBytes(Frequency));
				chunkBytes.AddRange(BitConverter.GetBytes(AverageBytesPerSec));
				chunkBytes.AddRange(BitConverter.GetBytes(BlockAlign));
				chunkBytes.AddRange(BitConverter.GetBytes(BitsPerSample));

				return chunkBytes.ToArray();
			}

			public uint Length()
			{
				return (uint)GetBytes().Length;
			}
		}

		class DataChunk
		{
			const string chunkId = "data";

			public string ChunkId { get; private set; }
			public uint ChunkSize { get; set; }
			public List<short> WaveData { get; private set; }

			public DataChunk()
			{
				ChunkId = chunkId;
				ChunkSize = 0;
				WaveData = new List<short>();
			}

			public byte[] GetBytes()
			{
				List<byte> chunkBytes = new List<byte>();

				chunkBytes.AddRange(Encoding.ASCII.GetBytes(ChunkId));
				chunkBytes.AddRange(BitConverter.GetBytes(ChunkSize));
				byte[] bufferBytes = new byte[WaveData.Count * 2];
				Buffer.BlockCopy(WaveData.ToArray(), 0, bufferBytes, 0, bufferBytes.Length);
				chunkBytes.AddRange(bufferBytes.ToList());

				return chunkBytes.ToArray();
			}

			public uint Length()
			{
				return (uint)GetBytes().Length;
			}

			public void AddSampleData(short[] stereoBuffer)
			{
				WaveData.AddRange(stereoBuffer);

				ChunkSize += (uint)(stereoBuffer.Length * 2);
			}
		}
	}
}
