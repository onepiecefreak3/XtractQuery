using System.Runtime.Serialization;

namespace Logic.Domain.Level5.Contract.Script.Xseq.Exceptions
{
    public class XseqScriptReaderException : Exception
    {
        public XseqScriptReaderException()
        {
        }

        public XseqScriptReaderException(string message) : base(message)
        {
        }

        public XseqScriptReaderException(string message, Exception inner) : base(message, inner)
        {
        }

        protected XseqScriptReaderException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
