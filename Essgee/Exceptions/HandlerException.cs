using System;

namespace Essgee.Exceptions
{
	public class HandlerException : Exception
	{
		public HandlerException() : base() { }
		public HandlerException(string message) : base(message) { }
		public HandlerException(string message, Exception innerException) : base(message, innerException) { }
		public HandlerException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}
}
