using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

using OpenTK;

using Essgee.Extensions;

namespace Essgee.Graphics.Shaders
{
	public class ShaderBundle : IDisposable
	{
		public const int MaxNumSourceSamplers = 3;

		readonly static string manifestFilename = "Manifest.json";
		readonly static string fragmentFilename = "Fragment.glsl";

		readonly static string glslUniformSourceSamplers = "source";

		readonly static string glslUniformProjection = "projection";
		readonly static string glslUniformModelview = "modelview";
		readonly static string glslUniformTextureSize = "textureSize";
		readonly static string glslUniformInputViewport = "inputViewport";
		readonly static string glslUniformOutputViewport = "outputViewport";

		readonly static string glslVersion = "#version 300 es\n";
		readonly static string glslESPrecision = "precision mediump float; precision mediump int;\n";
		readonly static string glslMainStart = "void main(void){";
		readonly static string glslMainEnd = "}\n";

		readonly static string defaultVertexUniforms = $"uniform mat4 {glslUniformProjection}; uniform mat4 {glslUniformModelview}; uniform vec2 {glslUniformTextureSize}; uniform vec4 {glslUniformInputViewport};\n";
		readonly static string defaultVertexOuts = "out vec4 vertColor; out vec2 vertTexCoord;\n";
		readonly static string defaultVertexMain = $"vertColor = inColor; gl_Position = {glslUniformProjection} * {glslUniformModelview} * vec4(inPosition.x, inPosition.y, inPosition.z, 1.0);\n";

		// TODO: kinda ugly... but seems to work fine?
		readonly static string defaultVertexTexCoord =
			$"vertTexCoord = vec2(" +
			$"(inTexCoord.x == 0.0 ? ({glslUniformInputViewport}.x + 0.5) / {glslUniformTextureSize}.x : ({glslUniformInputViewport}.z + {glslUniformInputViewport}.x - 0.5) / {glslUniformTextureSize}.x)," +
			$"(inTexCoord.y == 0.0 ? ({glslUniformInputViewport}.y + 0.5) / {glslUniformTextureSize}.y : ({glslUniformInputViewport}.w + {glslUniformInputViewport}.y - 0.5) / {glslUniformTextureSize}.y));\n";

		readonly static string defaultFragmentUniforms = $"uniform sampler2D {glslUniformSourceSamplers}[{MaxNumSourceSamplers}]; uniform vec2 {glslUniformTextureSize}; uniform vec4 {glslUniformInputViewport}; uniform vec4 {glslUniformOutputViewport};";
		readonly static string defaultFragmentIns = "in vec4 vertColor; in vec2 vertTexCoord;\n";
		readonly static string defaultFragmentOuts = "out vec4 fragColor;\n";

		readonly List<VertexElement> vertexElements;
		readonly int vertexStructSize;

		readonly string vertexPreamble, vertexMain, fragmentPreamble;
		string manifestJson, fragmentMain;

		public BundleManifest Manifest { get; private set; }

		GLSLShader internalShader;

		bool disposed = false;

		public ShaderBundle(Type vertexType)
		{
			(vertexElements, vertexStructSize) = VertexBuffer.DeconstructVertexLayout(vertexType);

			var vertexPreambleBuilder = new StringBuilder();
			vertexPreambleBuilder.Append(glslVersion);
			vertexPreambleBuilder.Append(glslESPrecision);
			vertexPreambleBuilder.Append(defaultVertexUniforms);
			vertexPreambleBuilder.Append(VertexBuffer.GetShaderPreamble(vertexElements));
			vertexPreambleBuilder.Append(defaultVertexOuts);
			vertexPreamble = vertexPreambleBuilder.ToString();

			var vertexMainBuilder = new StringBuilder();
			vertexMainBuilder.Append(glslMainStart);
			vertexMainBuilder.Append(defaultVertexMain);
			vertexMainBuilder.Append(defaultVertexTexCoord);
			vertexMainBuilder.Append(glslMainEnd);
			vertexMain = vertexMainBuilder.ToString();

			var fragmentPreambleBuilder = new StringBuilder();
			fragmentPreambleBuilder.Append(glslVersion);
			fragmentPreambleBuilder.Append(glslESPrecision);
			fragmentPreambleBuilder.Append(defaultFragmentUniforms);
			fragmentPreambleBuilder.Append(defaultFragmentIns);
			fragmentPreambleBuilder.Append(defaultFragmentOuts);
			fragmentPreamble = fragmentPreambleBuilder.ToString();
		}

		public ShaderBundle(string shaderName, Type vertexType) : this(vertexType)
		{
			LoadBundle(shaderName);
		}

		~ShaderBundle()
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
				internalShader.Dispose();

			disposed = true;
		}

		public void LoadBundle(string shaderName)
		{
			/* Try loading embedded shader first... */
			var shaderEmbeddedPath = $"{Application.ProductName}.Assets.Shaders.{shaderName}";
			var shaderEmbeddedManifestFile = $"{shaderEmbeddedPath}.{manifestFilename}";
			if (Assembly.GetExecutingAssembly().IsEmbeddedResourceAvailable(shaderEmbeddedManifestFile))
			{
				manifestJson = Assembly.GetExecutingAssembly().ReadEmbeddedTextFile(shaderEmbeddedManifestFile);
				fragmentMain = Assembly.GetExecutingAssembly().ReadEmbeddedTextFile($"{shaderEmbeddedPath}.{fragmentFilename}");
			}
			/* If embedded shader wasn't found, try loading from assets directory... */
			else
			{
				var bundlePath = Path.Combine(Program.ShaderPath, shaderName);

				if (!Directory.Exists(bundlePath)) throw new DirectoryNotFoundException($"Shader {shaderName} not found");

				var manifestPath = Path.Combine(bundlePath, manifestFilename);
				if (!File.Exists(manifestPath)) throw new FileNotFoundException($"Manifest {manifestFilename} not found in {bundlePath}");

				manifestJson = File.ReadAllText(manifestPath);

				var fragmentPath = Path.Combine(bundlePath, fragmentFilename);
				if (!File.Exists(fragmentPath)) throw new FileNotFoundException($"Fragment shader {fragmentFilename} not found in {bundlePath}");

				using (var reader = new StreamReader(new FileStream(fragmentPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
				{
					fragmentMain = reader.ReadToEnd();
				}
			}

			/* Now initialize GLSL shader using manifest and fragment code */
			InitializeBundle(manifestJson, fragmentMain);
		}

		private void InitializeBundle(string manifestJson, string fragmentMain)
		{
			Manifest = manifestJson.DeserializeObject<BundleManifest>();
			if (Manifest.Samplers > MaxNumSourceSamplers)
			{
				// TODO: give user a warning or something?
				Manifest.Samplers = MaxNumSourceSamplers;
			}

			internalShader = new GLSLShader();
			internalShader.SetVertexShaderCode(vertexPreamble, vertexMain);
			internalShader.SetFragmentShaderCode(fragmentPreamble, fragmentMain);
			internalShader.LinkProgram();

			internalShader.SetUniformMatrix(glslUniformModelview, false, Matrix4.Identity);
			internalShader.SetUniform(glslUniformTextureSize, Vector2.One);
			internalShader.SetUniform(glslUniformInputViewport, Vector4.One);
			internalShader.SetUniform(glslUniformOutputViewport, Vector4.One);

			for (int i = 0; i < MaxNumSourceSamplers; i++)
				internalShader.SetUniform($"{glslUniformSourceSamplers}[{i}]", i);
		}

		public void SetProjectionMatrix(Matrix4 mat4)
		{
			internalShader.SetUniformMatrix(glslUniformProjection, false, mat4);
		}

		public void SetModelviewMatrix(Matrix4 mat4)
		{
			internalShader.SetUniformMatrix(glslUniformModelview, false, mat4);
		}

		public void SetTextureSize(Vector2 vec2)
		{
			internalShader.SetUniform(glslUniformTextureSize, vec2);
		}

		public void SetInputViewport(Vector4 vec4)
		{
			internalShader.SetUniform(glslUniformInputViewport, vec4);
		}

		public void SetOutputViewport(Vector4 vec4)
		{
			internalShader.SetUniform(glslUniformOutputViewport, vec4);
		}

		public void Activate()
		{
			internalShader.Activate();
		}
	}
}
