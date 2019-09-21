using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

using OpenTK.Graphics.OpenGL;

using Essgee.Exceptions;

namespace Essgee.Graphics.Shaders
{
	public class GLSLShader : IDisposable
	{
		int vertexShaderObject, fragmentShaderObject, geometryShaderObject;
		List<string> vertexShaderCode, fragmentShaderCode, geometryShaderCode;

		readonly int programObject;

		static readonly string[] uniformSetMethods =
		{
			"Uniform1", "Uniform2", "Uniform3", "Uniform4"
		};

		static readonly string[] uniformSetMethodsMatrix =
		{
			"UniformMatrix2", "UniformMatrix2x3", "UniformMatrix2x4",
			"UniformMatrix3", "UniformMatrix3x2", "UniformMatrix3x4",
			"UniformMatrix4", "UniformMatrix4x2", "UniformMatrix4x3"
		};

		Dictionary<string, int> uniformLocations;
		Dictionary<string, dynamic> uniformData;
		Dictionary<Type, FastInvokeHandler> uniformMethods;

		bool disposed = false;

		public GLSLShader()
		{
			vertexShaderObject = fragmentShaderObject = geometryShaderObject = -1;
			programObject = GL.CreateProgram();

			uniformLocations = new Dictionary<string, int>();
			uniformData = new Dictionary<string, dynamic>();
			uniformMethods = new Dictionary<Type, FastInvokeHandler>();
		}

		~GLSLShader()
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
				DetachDeleteShader(programObject, vertexShaderObject);
				DetachDeleteShader(programObject, fragmentShaderObject);
				DetachDeleteShader(programObject, geometryShaderObject);

				GL.DeleteProgram(programObject);
			}

			disposed = true;
		}

		public void SetVertexShaderCode(params string[] shaderCode)
		{
			DetachDeleteShader(programObject, vertexShaderObject);
			vertexShaderObject = GenerateShader(ShaderType.VertexShader, vertexShaderCode = shaderCode.ToList());
		}

		public void SetFragmentShaderCode(params string[] shaderCode)
		{
			DetachDeleteShader(programObject, fragmentShaderObject);
			fragmentShaderObject = GenerateShader(ShaderType.FragmentShader, fragmentShaderCode = shaderCode.ToList());
		}

		public void SetGeometryShaderCode(params string[] shaderCode)
		{
			DetachDeleteShader(programObject, geometryShaderObject);
			geometryShaderObject = GenerateShader(ShaderType.GeometryShader, geometryShaderCode = shaderCode.ToList());
		}

		private int GenerateShader(ShaderType shaderType, List<string> shaderCode)
		{
			var handle = GL.CreateShader(shaderType);
			GL.ShaderSource(handle, shaderCode.Count, shaderCode.ToArray(), shaderCode.Select(x => x.Length).ToArray());
			GL.CompileShader(handle);

			GL.GetShaderInfoLog(handle, out string infoLog);
			GL.GetShader(handle, ShaderParameter.CompileStatus, out int statusCode);
			if (statusCode != 1)
				throw new GraphicsException($"Shader compile for {shaderType} failed: {infoLog}");

			return handle;
		}

		private void DetachDeleteShader(int programObject, int shaderObject)
		{
			if (shaderObject != -1 && GL.IsShader(shaderObject))
			{
				GL.DetachShader(programObject, shaderObject);
				GL.DeleteShader(shaderObject);
			}
		}

		public void LinkProgram()
		{
			LinkProgram(programObject, new int[] { vertexShaderObject, fragmentShaderObject, geometryShaderObject });
		}

		private void LinkProgram(int programObject, int[] shaderObjects)
		{
			foreach (var shaderObject in shaderObjects.Where(x => x != -1))
				GL.AttachShader(programObject, shaderObject);

			GL.LinkProgram(programObject);
			GL.GetProgramInfoLog(programObject, out string infoLog);
			GL.GetProgram(programObject, GetProgramParameterName.LinkStatus, out int statusCode);
			if (statusCode != 1)
				throw new GraphicsException($"Shader program link failed: {infoLog}");
		}

		public void Activate()
		{
			if (programObject == -1) throw new GraphicsException("Invalid shader program handle");
			GL.UseProgram(programObject);
		}

		public void SetUniform(string name, dynamic data)
		{
			Activate();

			Type type = data.GetType();

			if (!uniformLocations.ContainsKey(name))
				uniformLocations.Add(name, GL.GetUniformLocation(programObject, name));

			uniformData[name] = data;

			if (uniformMethods.ContainsKey(type))
			{
				uniformMethods[type](null, new object[] { uniformLocations[name], data });
			}
			else
			{
				foreach (string methodName in uniformSetMethods)
				{
					Type[] argTypes = new Type[] { typeof(int), type };
					MethodInfo methodInfo = typeof(GL).GetMethod(methodName, argTypes);

					if (methodInfo != null)
					{
						uniformMethods[type] = FastMethodInvoker.GetMethodInvoker(methodInfo);
						uniformMethods[type](null, new object[] { uniformLocations[name], data });
						return;
					}
				}

				throw new GraphicsException("No Uniform method found");
			}
		}

		public void SetUniformMatrix(string name, bool transpose, dynamic data)
		{
			Activate();

			Type type = data.GetType();
			if (!uniformLocations.ContainsKey(name))
				uniformLocations.Add(name, GL.GetUniformLocation(programObject, name));

			uniformData[name] = data;

			if (uniformMethods.ContainsKey(type))
			{
				uniformMethods[type](null, new object[] { uniformLocations[name], transpose, data });
			}
			else
			{
				foreach (string methodName in uniformSetMethodsMatrix)
				{
					Type[] argTypes = new Type[] { typeof(int), typeof(bool), data.GetType().MakeByRefType() };
					MethodInfo methodInfo = typeof(GL).GetMethod(methodName, argTypes);

					if (methodInfo != null)
					{
						uniformMethods[type] = FastMethodInvoker.GetMethodInvoker(methodInfo);
						uniformMethods[type](null, new object[] { uniformLocations[name], transpose, data });
						return;
					}
				}

				throw new GraphicsException("No UniformMatrix method found");
			}
		}

		public dynamic GetUniform(string name)
		{
			if (!uniformData.ContainsKey(name)) throw new ArgumentException();
			return uniformData[name];
		}
	}
}
