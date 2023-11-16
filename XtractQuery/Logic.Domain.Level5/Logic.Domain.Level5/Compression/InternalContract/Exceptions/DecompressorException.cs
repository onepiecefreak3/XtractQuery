using System.Runtime.Serialization;

namespace Logic.Domain.Level5.Compression.InternalContract.Exceptions
{
    public class DecompressorException : Exception
    {
        public DecompressorException()
        {
        }

        public DecompressorException(string message) : base(message)
        {
        }

        public DecompressorException(string message, Exception inner) : base(message, inner)
        {
        }

        protected DecompressorException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
