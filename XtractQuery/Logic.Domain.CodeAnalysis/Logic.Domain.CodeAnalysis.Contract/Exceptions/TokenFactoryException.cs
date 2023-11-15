using System.Runtime.Serialization;

namespace Logic.Domain.CodeAnalysis.Contract.Exceptions
{
    [Serializable]
    public class TokenFactoryException : Exception
    {
        public TokenFactoryException()
        {
        }

        public TokenFactoryException(string message) : base(message)
        {
        }

        public TokenFactoryException(string message, Exception inner) : base(message, inner)
        {
        }

        protected TokenFactoryException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
