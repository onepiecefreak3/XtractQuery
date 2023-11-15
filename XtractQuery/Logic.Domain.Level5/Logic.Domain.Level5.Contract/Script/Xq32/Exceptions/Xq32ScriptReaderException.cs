using System.Runtime.Serialization;

namespace Logic.Domain.Level5.Contract.Script.Xq32.Exceptions
{
    public class Xq32ScriptReaderException : Exception
    {
        public Xq32ScriptReaderException()
        {
        }

        public Xq32ScriptReaderException(string message) : base(message)
        {
        }

        public Xq32ScriptReaderException(string message, Exception inner) : base(message, inner)
        {
        }

        protected Xq32ScriptReaderException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
