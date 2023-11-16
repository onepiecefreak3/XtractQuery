using System.Runtime.Serialization;

namespace Logic.Domain.CodeAnalysis.Contract.Exceptions
{
    [Serializable]
    public class LexerException : Exception
    {
        public LexerException()
        {
        }

        public LexerException(string message) : base(message)
        {
        }

        public LexerException(string message, Exception inner) : base(message, inner)
        {
        }

        protected LexerException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
