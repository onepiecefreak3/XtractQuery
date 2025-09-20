using System.Runtime.Serialization;

namespace Logic.Domain.Level5.Cryptography.InternalContract.Exceptions;

public class ChecksumFactoryException : Exception
{
    public ChecksumFactoryException()
    {
    }

    public ChecksumFactoryException(string message) : base(message)
    {
    }

    public ChecksumFactoryException(string message, Exception inner) : base(message, inner)
    {
    }

    protected ChecksumFactoryException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}