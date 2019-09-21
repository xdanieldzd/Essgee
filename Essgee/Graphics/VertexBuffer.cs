using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Reflection;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using Essgee.Exceptions;

namespace Essgee.Graphics
{
	public class VertexBuffer : IDisposable
	{
		static readonly Dictionary<Type, VertexAttribPointerType> pointerTypeTranslator = new Dictionary<Type, VertexAttribPointerType>()
		{
			{ typeof(byte), VertexAttribPointerType.UnsignedByte },
			{ typeof(sbyte), VertexAttribPointerType.Byte },
			{ typeof(ushort), VertexAttribPointerType.UnsignedShort },
			{ typeof(short), VertexAttribPointerType.Short },
			{ typeof(uint), VertexAttribPointerType.UnsignedInt },
			{ typeof(int), VertexAttribPointerType.Int },
			{ typeof(float), VertexAttribPointerType.Float },
			{ typeof(double), VertexAttribPointerType.Double },
			{ typeof(Vector2), VertexAttribPointerType.Float },
			{ typeof(Vector3), VertexAttribPointerType.Float },
			{ typeof(Vector4), VertexAttribPointerType.Float },
			{ typeof(Color4), VertexAttribPointerType.Float }
		};

		static readonly Dictionary<Type, string> glslTypeTranslator = new Dictionary<Type, string>()
		{
			{ typeof(uint), "uint" },
			{ typeof(int), "int" },
			{ typeof(float), "float" },
			{ typeof(double), "double" },
			{ typeof(Vector2), "vec2" },
			{ typeof(Vector3), "vec3" },
			{ typeof(Vector4), "vec4" },
			{ typeof(Color4), "vec4" }
		};

		static readonly Dictionary<Type, DrawElementsType> drawElementsTypeTranslator = new Dictionary<Type, DrawElementsType>()
		{
			{ typeof(byte), DrawElementsType.UnsignedByte },
			{ typeof(ushort), DrawElementsType.UnsignedShort },
			{ typeof(uint), DrawElementsType.UnsignedInt }
		};

		readonly int vaoHandle, vboHandle;
		int numElementsToDraw;

		PrimitiveType primitiveType;

		int elementBufferHandle;
		DrawElementsType drawElementsType;

		List<VertexElement> vertexElements;
		int vertexStructSize;

		bool disposed = false;

		public VertexBuffer()
		{
			vaoHandle = GL.GenVertexArray();
			vboHandle = GL.GenBuffer();
			numElementsToDraw = -1;

			primitiveType = PrimitiveType.Triangles;

			elementBufferHandle = -1;
			drawElementsType = DrawElementsType.UnsignedByte;

			vertexElements = null;
			vertexStructSize = -1;
		}

		~VertexBuffer()
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
				if (GL.IsVertexArray(vaoHandle))
					GL.DeleteVertexArray(vaoHandle);

				if (GL.IsBuffer(vboHandle))
					GL.DeleteBuffer(vboHandle);

				if (GL.IsBuffer(elementBufferHandle))
					GL.DeleteBuffer(elementBufferHandle);
			}

			disposed = true;
		}

		public static (List<VertexElement>, int) DeconstructVertexLayout<T>() where T : struct, IVertexStruct
		{
			return DeconstructVertexLayout(typeof(T));
		}

		public static (List<VertexElement>, int) DeconstructVertexLayout(Type vertexType)
		{
			if (!typeof(IVertexStruct).IsAssignableFrom(vertexType)) throw new Exceptions.GraphicsException("Cannot deconstruct layout of non-vertex type");

			var elements = new List<VertexElement>();
			var structSize = Marshal.SizeOf(vertexType);

			foreach (var field in vertexType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
			{
				var attribs = field.GetCustomAttributes(typeof(VertexElementAttribute), false);
				if (attribs == null || attribs.Length != 1) continue;

				var elementAttribute = (attribs[0] as VertexElementAttribute);

				var numComponents = Marshal.SizeOf(field.FieldType);

				if (field.FieldType.IsValueType && !field.FieldType.IsEnum)
				{
					var structFields = field.FieldType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					if (structFields == null || structFields.Length < 1 || structFields.Length > 4) throw new Exceptions.GraphicsException("Invalid number of fields in struct");
					numComponents = structFields.Length;
				}

				elements.Add(new VertexElement()
				{
					AttributeIndex = elementAttribute.AttributeIndex,
					DataType = field.FieldType,
					NumComponents = numComponents,
					OffsetInVertex = Marshal.OffsetOf(vertexType, field.Name).ToInt32(),
					Name = field.Name
				});
			}

			return (elements, structSize);
		}

		public void SetPrimitiveType(PrimitiveType primType)
		{
			primitiveType = primType;
		}

		public PrimitiveType GetPrimitiveType()
		{
			return primitiveType;
		}

		public void SetVertexData<TVertex>(TVertex[] vertices) where TVertex : struct, IVertexStruct
		{
			(vertexElements, vertexStructSize) = DeconstructVertexLayout<TVertex>();

			GL.BindVertexArray(vaoHandle);

			GL.BindBuffer(BufferTarget.ArrayBuffer, vboHandle);
			GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr(vertexStructSize * vertices.Length), vertices, BufferUsageHint.StaticDraw);

			foreach (var element in vertexElements)
			{
				GL.EnableVertexAttribArray(element.AttributeIndex);
				GL.VertexAttribPointer(element.AttributeIndex, element.NumComponents, GetVertexAttribPointerType(element.DataType), false, vertexStructSize, element.OffsetInVertex);
			}

			numElementsToDraw = vertices.Length;

			GL.BindVertexArray(0);
		}

		public void SetIndices<TIndex>(TIndex[] indices) where TIndex : struct, IConvertible
		{
			drawElementsType = GetDrawElementsType(typeof(TIndex));

			if (elementBufferHandle == -1)
				elementBufferHandle = GL.GenBuffer();

			GL.BindBuffer(BufferTarget.ElementArrayBuffer, elementBufferHandle);
			GL.BufferData(BufferTarget.ElementArrayBuffer, new IntPtr(Marshal.SizeOf(typeof(TIndex)) * indices.Length), indices, BufferUsageHint.StaticDraw);

			numElementsToDraw = indices.Length;
		}

		private VertexAttribPointerType GetVertexAttribPointerType(Type type)
		{
			if (pointerTypeTranslator.ContainsKey(type))
				return pointerTypeTranslator[type];
			else
				throw new ArgumentException("Unimplemented or unsupported vertex attribute pointer type");
		}

		private DrawElementsType GetDrawElementsType(Type type)
		{
			if (drawElementsTypeTranslator.ContainsKey(type))
				return drawElementsTypeTranslator[type];
			else
				throw new ArgumentException("Unsupported draw elements type");
		}

		private static string GetGlslDataType(Type type)
		{
			if (glslTypeTranslator.ContainsKey(type))
				return glslTypeTranslator[type];
			else
				throw new ArgumentException("Unimplemented or unsupported GLSL data type");
		}

		public string GetShaderPreamble(string prefix = "in")
		{
			return GetShaderPreamble(vertexElements, prefix);
		}

		public static string GetShaderPreamble(List<VertexElement> vertexElements, string prefix = "in")
		{
			var stringBuilder = new StringBuilder();
			for (int i = 0; i < vertexElements.Count; i++)
			{
				var element = vertexElements[i];
				stringBuilder.AppendLine($"layout(location = {element.AttributeIndex}) in {GetGlslDataType(element.DataType)} {prefix}{element.Name};");
			}
			return stringBuilder.ToString();
		}

		public void Render()
		{
			GL.BindVertexArray(vaoHandle);

			if (elementBufferHandle != -1)
			{
				GL.BindBuffer(BufferTarget.ElementArrayBuffer, elementBufferHandle);
				GL.DrawElements(primitiveType, numElementsToDraw, drawElementsType, 0);
			}
			else
				GL.DrawArrays(primitiveType, 0, numElementsToDraw);
		}
	}

	public interface IVertexStruct { }

	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
	public class VertexElementAttribute : Attribute
	{
		public int AttributeIndex { get; set; }

		public VertexElementAttribute()
		{
			AttributeIndex = -1;
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct CommonVertex : IVertexStruct
	{
		[VertexElement(AttributeIndex = 0)]
		public Vector3 Position;
		[VertexElement(AttributeIndex = 1)]
		public Vector3 Normal;
		[VertexElement(AttributeIndex = 2)]
		public Color4 Color;
		[VertexElement(AttributeIndex = 3)]
		public Vector2 TexCoord;
	}
}
