using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.ComponentModel;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using Essgee.Graphics.Shaders;

namespace Essgee.Graphics
{
	public class GraphicsHandler
	{
		static readonly CommonVertex[] vertices = new CommonVertex[]
		{
			new CommonVertex() { Position = new Vector3(0.0f, 1.0f, 0.0f), TexCoord = new Vector2(0.0f, 1.0f), Color = Color4.White },
			new CommonVertex() { Position = new Vector3(0.0f, 0.0f, 0.0f), TexCoord = new Vector2(0.0f, 0.0f), Color = Color4.White },
			new CommonVertex() { Position = new Vector3(1.0f, 0.0f, 0.0f), TexCoord = new Vector2(1.0f, 0.0f), Color = Color4.White },
			new CommonVertex() { Position = new Vector3(1.0f, 1.0f, 0.0f), TexCoord = new Vector2(1.0f, 1.0f), Color = Color4.White }
		};

		static readonly byte[] indices = new byte[] { 0, 1, 2, 2, 3, 0 };

		public string GLRenderer { get; private set; }
		public string GLVersion { get; private set; }

		VertexBuffer vertexBuffer;
		ShaderBundle shaderBundle;

		Texture[] textures;
		int lastTextureUpdate;

		OnScreenDisplayHandler onScreenDisplayHandler;

		(int Width, int Height) textureSize;
		(int X, int Y, int Width, int Height) inputViewport, outputViewport;
		Matrix4 projectionMatrix, modelviewMatrix;

		bool refreshRendererAndShader;

		Stopwatch stopwatch;
		float deltaTime;

		public GraphicsHandler(OnScreenDisplayHandler osdHandler)
		{
			onScreenDisplayHandler = osdHandler;

			if (Program.AppEnvironment.EnableOpenGLDebug)
				GL.Enable(EnableCap.DebugOutput);

			GLRenderer = GL.GetString(StringName.Renderer);
			GLVersion = GL.GetString(StringName.Version);

			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

			vertexBuffer = new VertexBuffer();
			vertexBuffer.SetVertexData(vertices);
			vertexBuffer.SetIndices(indices);

			textures = new Texture[ShaderBundle.MaxNumSourceSamplers];

			lastTextureUpdate = 0;

			projectionMatrix = Matrix4.Identity;
			modelviewMatrix = Matrix4.Identity;

			stopwatch = Stopwatch.StartNew();

			Application.Idle += (s, e) =>
			{
				if (LicenseManager.UsageMode == LicenseUsageMode.Designtime || GraphicsContext.CurrentContext == null) return;

				stopwatch.Stop();
				deltaTime = (float)stopwatch.Elapsed.TotalMilliseconds / 10.0f;
				stopwatch.Reset();
				stopwatch.Start();
			};

			onScreenDisplayHandler.EnqueueMessageSuccess($"Graphics initialized; {GLRenderer}, {GLVersion}.");
		}

		public void LoadShaderBundle(string shaderName)
		{
			var lastShaderFilter = shaderBundle?.Manifest.Filter;
			var lastShaderWrap = shaderBundle?.Manifest.Wrap;

			shaderBundle = new ShaderBundle(shaderName, typeof(CommonVertex));

			if ((lastShaderFilter != null && lastShaderFilter != shaderBundle.Manifest.Filter) || (lastShaderWrap != null && lastShaderWrap != shaderBundle.Manifest.Wrap))
				CreateTextures();

			FlushTextures();

			refreshRendererAndShader = true;

			onScreenDisplayHandler.EnqueueMessage($"Loaded shader '{shaderName}'.");
		}

		public void SetTextureSize(int width, int height)
		{
			textureSize = (width, height);

			CreateTextures();

			refreshRendererAndShader = true;
		}

		private void CreateTextures()
		{
			for (int i = 0; i < textures.Length; i++)
				textures[i] = new Texture(textureSize.Width, textureSize.Height, PixelFormat.Rgb888, shaderBundle.Manifest.Filter, shaderBundle.Manifest.Wrap);
			lastTextureUpdate = 0;
		}

		public void SetTextureData(byte[] data)
		{
			textures[lastTextureUpdate].SetData(data);
			lastTextureUpdate++;
			if (lastTextureUpdate >= shaderBundle.Manifest.Samplers) lastTextureUpdate = 0;
		}

		public void SetScreenViewport((int, int, int, int) viewport)
		{
			inputViewport = viewport;

			lastTextureUpdate = 0;

			refreshRendererAndShader = true;
		}

		public void FlushTextures()
		{
			for (int i = 0; i < shaderBundle.Manifest.Samplers; i++)
				textures[i]?.ClearData();

			lastTextureUpdate = 0;

			refreshRendererAndShader = true;
		}

		public void Resize(Rectangle clientRectangle, Size screenSize)
		{
			outputViewport = (clientRectangle.X, clientRectangle.Y, clientRectangle.Width, clientRectangle.Height);

			GL.Viewport(0, 0, outputViewport.Width, outputViewport.Height);

			projectionMatrix = Matrix4.CreateOrthographicOffCenter(0.0f, outputViewport.Width, outputViewport.Height, 0.0f, -10.0f, 10.0f);

			if (Program.Configuration.ScreenSizeMode == ScreenSizeMode.Scale)
			{
				var multiplier = (float)Math.Min(outputViewport.Width / (double)screenSize.Width, outputViewport.Height / (double)screenSize.Height);
				var adjustedWidth = (screenSize.Width * multiplier);
				var adjustedHeight = (screenSize.Height * multiplier);
				modelviewMatrix = Matrix4.CreateScale(adjustedWidth, adjustedHeight, 1.0f) * Matrix4.CreateTranslation((outputViewport.Width - adjustedWidth) / 2.0f, (outputViewport.Height - adjustedHeight) / 2.0f, 1.0f);
			}
			else
			{
				modelviewMatrix = Matrix4.CreateScale(outputViewport.Width, outputViewport.Height, 1.0f);
			}

			onScreenDisplayHandler.SetViewport(outputViewport);
			onScreenDisplayHandler.SetProjectionMatrix(projectionMatrix);
			onScreenDisplayHandler.SetModelviewMatrix(modelviewMatrix);

			shaderBundle.SetProjectionMatrix(projectionMatrix);
			shaderBundle.SetModelviewMatrix(modelviewMatrix);
		}

		public void Render()
		{
			if (refreshRendererAndShader)
			{
				shaderBundle.SetTextureSize(new Vector2(textureSize.Width, textureSize.Height));
				shaderBundle.SetInputViewport(new Vector4(inputViewport.X, inputViewport.Y, inputViewport.Width, inputViewport.Height));
				shaderBundle.SetOutputViewport(new Vector4(outputViewport.X, outputViewport.Y, outputViewport.Width, outputViewport.Height));
				shaderBundle.SetProjectionMatrix(projectionMatrix);
				shaderBundle.SetModelviewMatrix(modelviewMatrix);

				GC.Collect();

				refreshRendererAndShader = false;
			}

			for (int i = 0; i < shaderBundle.Manifest.Samplers; i++)
				textures[i]?.Activate(TextureUnit.Texture0 + i);

			shaderBundle.Activate();
			vertexBuffer.Render();

			onScreenDisplayHandler.Render(deltaTime);
		}
	}
}
