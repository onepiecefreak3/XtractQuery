using System.Runtime.Serialization;

namespace Logic.Domain.Level5.Contract.Script.Xq32.Exceptions
{
    public class Xq32ScriptWriterException : Exception
    {
        public Xq32ScriptWriterException()
        {
        }

        public Xq32ScriptWriterException(string message) : base(message)
        {
        }

        public Xq32ScriptWriterException(string message, Exception inner) : base(message, inner)
        {
        }

        protected Xq32ScriptWriterException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
