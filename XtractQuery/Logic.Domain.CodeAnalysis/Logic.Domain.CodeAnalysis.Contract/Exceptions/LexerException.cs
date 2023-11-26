using System.Runtime.Serialization;

namespace Logic.Domain.CodeAnalysis.Contract.Exceptions
{
    [Serializable]
    public class LexerException : Exception
    {
        public int Line { get; }
        public int Column { get; }

        public LexerException()
        {
        }

        public LexerException(string message) : base(message)
        {
        }

        public LexerException(string message, Exception inner) : base(message, inner)
        {
        }

        public LexerException(string message, int line, int column) : base(message)
        {
            Line = line;
            Column = column;
        }

        protected LexerException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
