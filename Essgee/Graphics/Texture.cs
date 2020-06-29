using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

using OpenTK.Graphics.OpenGL;

using GlPixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;
using GdiPixelFormat = System.Drawing.Imaging.PixelFormat;

namespace Essgee.Graphics
{
	public class Texture : IDisposable
	{
		static readonly Dictionary<GdiPixelFormat, PixelFormat> gdiPixelFormatMap = new Dictionary<GdiPixelFormat, PixelFormat>()
		{
			{ GdiPixelFormat.Format32bppArgb, PixelFormat.Rgba8888 },
			{ GdiPixelFormat.Format24bppRgb, PixelFormat.Rgb888 }
		};

		static readonly Dictionary<PixelFormat, (PixelInternalFormat, GlPixelFormat, int)> glPixelFormatMap = new Dictionary<PixelFormat, (PixelInternalFormat, GlPixelFormat, int)>()
		{
			{ PixelFormat.Rgba8888, (PixelInternalFormat.Rgba8, GlPixelFormat.Bgra, 4) },
			{ PixelFormat.Rgb888, (PixelInternalFormat.Rgb8, GlPixelFormat.Bgr, 3) }
		};

		readonly static int maxTextureSize;

		int textureHandle;

		public int Width { get; private set; }
		public int Height { get; private set; }

		PixelInternalFormat pixelInternalFormat;
		GlPixelFormat glPixelFormat;
		int bytesPerPixel, dataSize;
		byte[] currentData;

		TextureMinFilter minFilter;
		TextureMagFilter magFilter;
		TextureWrapMode wrapMode;

		bool disposed = false;

		static Texture()
		{
			maxTextureSize = GL.GetInteger(GetPName.MaxTextureSize);
		}

		public Texture(int width, int height, PixelFormat pixelFormat, FilterMode filter = FilterMode.Linear, WrapMode wrap = WrapMode.Repeat)
		{
			InitializeRaw(width, height, pixelFormat, filter, wrap);
		}

		public Texture(Bitmap image, FilterMode filter = FilterMode.Linear, WrapMode wrap = WrapMode.Repeat)
		{
			if (!gdiPixelFormatMap.ContainsKey(image.PixelFormat))
				throw new ArgumentException($"Unsupported pixel format {image.PixelFormat}", nameof(image));

			InitializeRaw(image.Width, image.Height, gdiPixelFormatMap[image.PixelFormat], filter, wrap);

			var bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, image.PixelFormat);
			var imageData = new byte[bitmapData.Height * bitmapData.Stride];
			Marshal.Copy(bitmapData.Scan0, imageData, 0, imageData.Length);
			SetData(imageData);
			image.UnlockBits(bitmapData);
		}

		~Texture()
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
				if (GL.IsTexture(textureHandle))
					GL.DeleteTexture(textureHandle);
			}

			disposed = true;
		}

		private void InitializeRaw(int width, int height, PixelFormat pixelFormat, FilterMode filter, WrapMode wrap)
		{
			if (width <= 0 || width > maxTextureSize) throw new ArgumentOutOfRangeException(nameof(width), $"Invalid width {width}");
			Width = width;

			if (height <= 0 || height > maxTextureSize) throw new ArgumentOutOfRangeException(nameof(height), $"Invalid height {height}");
			Height = height;

			if (!glPixelFormatMap.ContainsKey(pixelFormat)) throw new ArgumentException($"Unsupported pixel format {pixelFormat}", nameof(pixelFormat));
			(pixelInternalFormat, glPixelFormat, bytesPerPixel) = glPixelFormatMap[pixelFormat];

			dataSize = (width * height * bytesPerPixel);

			switch (filter)
			{
				case FilterMode.Linear:
					minFilter = TextureMinFilter.Linear;
					magFilter = TextureMagFilter.Linear;
					break;

				case FilterMode.Nearest:
					minFilter = TextureMinFilter.Nearest;
					magFilter = TextureMagFilter.Nearest;
					break;

				default:
					throw new ArgumentException("Invalid filter mode", nameof(filter));
			}

			switch (wrap)
			{
				case WrapMode.Repeat: wrapMode = TextureWrapMode.Repeat; break;
				case WrapMode.Border: wrapMode = TextureWrapMode.ClampToBorder; break;
				case WrapMode.Edge: wrapMode = TextureWrapMode.ClampToEdge; break;
				case WrapMode.Mirror: wrapMode = TextureWrapMode.MirroredRepeat; break;
				default: throw new ArgumentException("Invalid wrap mode", nameof(wrap));
			}

			GenerateHandles();
			InitializeTexture();
		}

		private void GenerateHandles()
		{
			textureHandle = GL.GenTexture();
		}

		private void InitializeTexture()
		{
			GL.BindTexture(TextureTarget.Texture2D, textureHandle);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)minFilter);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)magFilter);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)wrapMode);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)wrapMode);
			if (bytesPerPixel != 4) GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
			GL.TexImage2D(TextureTarget.Texture2D, 0, pixelInternalFormat, Width, Height, 0, glPixelFormat, PixelType.UnsignedByte, IntPtr.Zero);
			GL.BindTexture(TextureTarget.Texture2D, 0);
		}

		public void SetData(byte[] data)
		{
			if (data == null) throw new ArgumentNullException(nameof(data), "Image data is null");
			if (data.Length != dataSize) throw new ArgumentException($"Image data size mismatch; excepted {dataSize} bytes, got {data.Length} bytes", nameof(data));

			GL.BindTexture(TextureTarget.Texture2D, textureHandle);
			GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, Width, Height, glPixelFormat, PixelType.UnsignedByte, (currentData = data));
		}

		public byte[] GetData()
		{
			return currentData;
		}

		public void ClearData()
		{
			var emptyData = new byte[dataSize];
			SetData(emptyData);
		}

		public void Activate()
		{
			Activate(TextureUnit.Texture0);
		}

		public void Activate(TextureUnit textureUnit)
		{
			if (textureHandle == -1) throw new InvalidOperationException("Invalid texture handle");
			GL.ActiveTexture(textureUnit);
			GL.BindTexture(TextureTarget.Texture2D, textureHandle);
		}

		public void Deactivate()
		{
			Deactivate(TextureUnit.Texture0);
		}

		public void Deactivate(TextureUnit textureUnit)
		{
			GL.ActiveTexture(textureUnit);
			GL.BindTexture(TextureTarget.Texture2D, 0);
		}
	}
}
