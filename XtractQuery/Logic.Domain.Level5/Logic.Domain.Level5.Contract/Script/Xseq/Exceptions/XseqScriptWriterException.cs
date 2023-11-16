using System.Runtime.Serialization;

namespace Logic.Domain.Level5.Contract.Script.Xseq.Exceptions
{
    public class XseqScriptWriterException : Exception
    {
        public XseqScriptWriterException()
        {
        }

        public XseqScriptWriterException(string message) : base(message)
        {
        }

        public XseqScriptWriterException(string message, Exception inner) : base(message, inner)
        {
        }

        protected XseqScriptWriterException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
