using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Reflection;

using OpenTK;
using OpenTK.Graphics;

using Essgee.Graphics.Shaders;

namespace Essgee.Graphics
{
	public class OnScreenDisplayHandler : IDisposable
	{
		readonly static int messageDefaultSeconds = 5;
		readonly static int maxStringListLength = 128;
		readonly static int stringListPurgeSize = 16;

		readonly static string glslUniformProjection = "projection";
		readonly static string glslUniformModelview = "modelview";
		readonly static string glslUniformSourceSampler = "source";
		readonly static string glslUniformTextColor = "textColor";
		readonly static string glslUniformFontSize = "fontSize";
		readonly static string glslUniformCharacterOffset = "characterOffset";

		readonly static string glslVersion = "#version 300 es\n";
		readonly static string glslESPrecision = "precision mediump float; precision mediump int;\n";
		readonly static string glslMainStart = "void main(void){";
		readonly static string glslMainEnd = "}\n";

		readonly static string vertexUniforms = $"uniform mat4 {glslUniformProjection}; uniform mat4 {glslUniformModelview};\n";
		readonly static string vertexOuts = "out vec2 vertTexCoord;\n";
		readonly static string vertexMain = $"vertTexCoord = inTextureCoords; gl_Position = {glslUniformProjection} * {glslUniformModelview} * vec4(inPosition.x, inPosition.y, 0.0, 1.0);\n";

		readonly static string fragmentUniforms = $"uniform sampler2D {glslUniformSourceSampler}; uniform vec4 {glslUniformTextColor}; uniform vec2 {glslUniformFontSize}; uniform vec2 {glslUniformCharacterOffset};\n";
		readonly static string fragmentIns = "in vec2 vertTexCoord;\n";
		readonly static string fragmentOuts = "out vec4 fragColor;\n";

		readonly static string fragmentMain =
			$"vec2 localTexCoord = vec2(({glslUniformCharacterOffset}.x + vertTexCoord.x) / {glslUniformFontSize}.x, ({glslUniformCharacterOffset}.y + vertTexCoord.y) / {glslUniformFontSize}.y);\n" +
			$"vec4 outColor = textColor * texture(source, localTexCoord);\n" +
			"if(outColor.a == 0.0) discard;\n" +
			"fragColor = outColor;\n";

		readonly static ushort[] characterIndices = new ushort[] { 0, 1, 2, 2, 3, 0 };

		readonly static Color4 colorSuccess = new Color4(192, 255, 192, 255);
		readonly static Color4 colorWarning = new Color4(255, 192, 160, 255);
		readonly static Color4 colorError = new Color4(255, 128, 128, 255);
		readonly static Color4 colorCore = new Color4(192, 192, 255, 255);
		readonly static Color4 colorDebug = new Color4(192, 128, 255, 255);

		readonly OnScreenDisplayVertex[] characterVertices;

		readonly List<VertexElement> vertexElements;
		readonly int vertexStructSize;
		readonly VertexBuffer characterVertexBuffer;

		readonly Texture fontTexture;
		readonly float characterSourceSize;
		readonly float characterDefaultWidth;
		readonly GLSLShader shader;

		readonly Dictionary<char, Vector2> characterOffsetDict;
		readonly Dictionary<char, (float start, float width)> characterWidthDict;

		readonly List<OnScreenDisplayMessage> stringList;

		(int X, int Y, int Width, int Height) viewport;

		bool disposed = false;

		public OnScreenDisplayHandler(Bitmap osdFontBitmap)
		{
			characterSourceSize = (osdFontBitmap.Width / 16.0f);
			characterDefaultWidth = characterSourceSize - (2.0f * (characterSourceSize / 8));

			characterOffsetDict = new Dictionary<char, Vector2>();
			characterWidthDict = new Dictionary<char, (float, float)>();
			for (var ch = '\0'; ch < (char)((osdFontBitmap.Width / characterSourceSize) * (osdFontBitmap.Height / characterSourceSize)); ch++)
			{
				float x = (ch % (osdFontBitmap.Width / (int)characterSourceSize)) * characterSourceSize;
				float y = (ch / (osdFontBitmap.Width / (int)characterSourceSize)) * characterSourceSize;

				float width = characterSourceSize;
				float start = 0.0f;
				for (float xc = x + (characterSourceSize - 1); xc >= x; xc--)
				{
					var pixel = osdFontBitmap.GetPixel((int)xc, (int)y);
					if (pixel.R == 0xFF && pixel.G == 0x00 && pixel.B == 0x00 && pixel.A == 0xFF)
					{
						width = (xc - x);
						osdFontBitmap.SetPixel((int)xc, (int)y, Color.Transparent);
					}

					if (pixel.R == 0xFF && pixel.G == 0xFF && pixel.B == 0x00 && pixel.A == 0xFF)
					{
						start = (xc - x);
						osdFontBitmap.SetPixel((int)xc, (int)y, Color.Transparent);
					}
				}

				characterOffsetDict.Add(ch, new Vector2(x, y));
				characterWidthDict.Add(ch, (start, width));
			}

			fontTexture = new Texture(osdFontBitmap, filter: FilterMode.Nearest);

			(vertexElements, vertexStructSize) = VertexBuffer.DeconstructVertexLayout<OnScreenDisplayVertex>();

			characterVertices = new OnScreenDisplayVertex[]
			{
				new OnScreenDisplayVertex() { Position = new Vector2(0.0f,                  characterSourceSize),   TextureCoords = new Vector2(0.0f,                   characterSourceSize) },
				new OnScreenDisplayVertex() { Position = new Vector2(0.0f,                  0.0f),                  TextureCoords = new Vector2(0.0f,                   0.0f) },
				new OnScreenDisplayVertex() { Position = new Vector2(characterSourceSize,   0.0f),                  TextureCoords = new Vector2(characterSourceSize,    0.0f) },
				new OnScreenDisplayVertex() { Position = new Vector2(characterSourceSize,   characterSourceSize),   TextureCoords = new Vector2(characterSourceSize,    characterSourceSize) },
			};

			characterVertexBuffer = new VertexBuffer();
			characterVertexBuffer.SetVertexData(characterVertices);
			characterVertexBuffer.SetIndices(characterIndices);

			shader = new GLSLShader();
			shader.SetVertexShaderCode(glslVersion, glslESPrecision, vertexUniforms, VertexBuffer.GetShaderPreamble(vertexElements), vertexOuts, glslMainStart, vertexMain, glslMainEnd);
			shader.SetFragmentShaderCode(glslVersion, glslESPrecision, fragmentUniforms, fragmentIns, fragmentOuts, glslMainStart, fragmentMain, glslMainEnd);
			shader.LinkProgram();

			shader.SetUniformMatrix(glslUniformProjection, false, Matrix4.Identity);
			shader.SetUniformMatrix(glslUniformModelview, false, Matrix4.Identity);
			shader.SetUniform(glslUniformSourceSampler, 0);
			shader.SetUniform(glslUniformTextColor, Color4.White);
			shader.SetUniform(glslUniformFontSize, new Vector2(fontTexture.Width, fontTexture.Height));
			shader.SetUniform(glslUniformCharacterOffset, Vector2.Zero);

			stringList = new List<OnScreenDisplayMessage>();
		}

		~OnScreenDisplayHandler()
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
			if (disposed) return;

			if (disposing)
			{
				if (fontTexture != null) fontTexture.Dispose();
				if (characterVertexBuffer != null) characterVertexBuffer.Dispose();
				if (shader != null) shader.Dispose();
			}

			disposed = true;
		}

		public void SetViewport(ValueTuple<int, int, int, int> view)
		{
			viewport = view;
		}

		public void SetProjectionMatrix(Matrix4 mat4)
		{
			shader.SetUniformMatrix(glslUniformProjection, false, mat4);
		}

		public void SetModelviewMatrix(Matrix4 mat4)
		{
			shader.SetUniformMatrix(glslUniformModelview, false, mat4);
		}

		public void SendString(string str, int x, int y)
		{
			SendString(str, x, y, Color4.White);
		}

		public void SendString(string str, int x, int y, Color4 color)
		{
			stringList.Add(new OnScreenDisplayMessage()
			{
				X = x,
				Y = y,
				Color = color,
				Text = str,
				ShowUntil = DateTime.Now,
				IsLogEntry = false
			});
		}

		public void EnqueueMessage(string str)
		{
			EnqueueMessage(str, Color4.White);
		}

		public void EnqueueMessage(string str, Color4 color)
		{
			var split = str.Split(new string[] { Environment.NewLine, "\n" }, StringSplitOptions.RemoveEmptyEntries);
			for (var i = 0; i < split.Length; i++)
			{
				stringList.Add(new OnScreenDisplayMessage()
				{
					X = 0,
					Y = 0,
					Color = color,
					Text = split[i],
					ShowUntil = DateTime.Now + TimeSpan.FromTicks(i) + TimeSpan.FromSeconds(messageDefaultSeconds),
					IsLogEntry = true
				});
			}
		}

		public void EnqueueMessageSuccess(string str)
		{
			EnqueueMessage(str, colorSuccess);
		}

		public void EnqueueMessageWarning(string str)
		{
			EnqueueMessage(str, colorWarning);
		}

		public void EnqueueMessageError(string str)
		{
			EnqueueMessage(str, colorError);
		}

		public void EnqueueMessageCore(string str)
		{
			EnqueueMessage(str, colorCore);
		}

		public void EnqueueMessageDebug(string str)
		{
			if (Program.AppEnvironment.DebugMode)
				EnqueueMessage(str, colorDebug);
		}

		public void Render(float deltaTime)
		{
			shader.Activate();
			fontTexture.Activate();

			RenderStrings(deltaTime);
			RenderLogMessages(deltaTime);

			stringList.RemoveAll(x => !x.IsLogEntry && x.ShowUntil < DateTime.Now);
			stringList.RemoveAll(x => x.IsLogEntry && x.Color.A <= 0.0f);

			if (stringList.Count > maxStringListLength)
				stringList.RemoveRange(0, stringListPurgeSize);

			fontTexture.Deactivate();
		}

		private int MeasureString(string @string)
		{
			float width = 0.0f;
			foreach (var ch in @string)
			{
				if (characterWidthDict.ContainsKey(ch))
				{
					width -= characterWidthDict[ch].start;
					width += characterWidthDict[ch].width;
				}
				else
					width += characterDefaultWidth;
			}
			return (int)width;
		}

		private void RenderStrings(float deltaTime)
		{
			foreach (var @string in stringList.ToList().Where(x => !x.IsLogEntry))
			{
				float x = @string.X, y = @string.Y;
				if (x < 0.0f) x = (viewport.Width + x) - MeasureString(@string.Text);
				if (y < 0.0f) y = (viewport.Height + y) - characterSourceSize;
				shader.SetUniform(glslUniformTextColor, @string.Color);
				ParseAndRenderString(@string, ref x, ref y);
			}
		}

		private void RenderLogMessages(float deltaTime)
		{
			var logY = (viewport.Height - (characterSourceSize + (characterSourceSize / 2)));
			foreach (var @string in stringList.ToList().Where(x => x.IsLogEntry).OrderByDescending(x => x.ShowUntil))
			{
				float x = characterSourceSize / 2.0f, y = logY;
				logY -= characterSourceSize;
				shader.SetUniform(glslUniformTextColor, @string.Color);
				ParseAndRenderString(@string, ref x, ref y);

				if ((logY + characterSourceSize) < 0) break;
			}

			var timeNow = DateTime.Now;
			foreach (var @string in stringList.ToList().Where(x => x.IsLogEntry))
			{
				if ((@string.ShowUntil.Ticks - timeNow.Ticks) < TimeSpan.TicksPerSecond)
					@string.Color = new Color4(@string.Color.R, @string.Color.G, @string.Color.B, Math.Max(0.0f, @string.Color.A - (deltaTime / 25.0f)));
			}
		}

		private void ParseAndRenderString(OnScreenDisplayMessage @string, ref float x, ref float y)
		{
			foreach (var c in @string.Text)
			{
				var ch = c;

				if (!characterWidthDict.ContainsKey(ch))
					ch = '\0';

				x -= characterWidthDict[ch].start;

				if (ch == '\n' || ch == '\r')
				{
					x = @string.X;
					y += characterSourceSize;
					continue;
				}
				else if (ch != ' ')
				{
					var osdModelview = Matrix4.Identity;
					osdModelview *= Matrix4.CreateTranslation(x, y, 0.0f);

					shader.SetUniformMatrix(glslUniformModelview, false, osdModelview);
					shader.SetUniform(glslUniformCharacterOffset, characterOffsetDict[ch]);

					characterVertexBuffer.Render();
				}

				x += characterWidthDict[ch].width;
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct OnScreenDisplayVertex : IVertexStruct
		{
			[VertexElement(AttributeIndex = 0)]
			public Vector2 Position;
			[VertexElement(AttributeIndex = 1)]
			public Vector2 TextureCoords;
		}

		public class OnScreenDisplayMessage
		{
			public int X { get; set; }
			public int Y { get; set; }
			public Color4 Color { get; set; }
			public string Text { get; set; }
			public DateTime ShowUntil { get; set; }
			public bool IsLogEntry { get; set; }

			public OnScreenDisplayMessage()
			{
				X = Y = 0;
				Color = Color4.White;
				Text = string.Empty;
				ShowUntil = DateTime.Now;
				IsLogEntry = false;
			}
		}
	}
}
