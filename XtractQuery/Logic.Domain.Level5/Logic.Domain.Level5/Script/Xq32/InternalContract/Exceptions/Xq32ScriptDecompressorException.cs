using System.Runtime.Serialization;

namespace Logic.Domain.Level5.Script.Xq32.InternalContract.Exceptions
{
    public class Xq32ScriptDecompressorException : Exception
    {
        public Xq32ScriptDecompressorException()
        {
        }

        public Xq32ScriptDecompressorException(string message) : base(message)
        {
        }

        public Xq32ScriptDecompressorException(string message, Exception inner) : base(message, inner)
        {
        }

        protected Xq32ScriptDecompressorException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
