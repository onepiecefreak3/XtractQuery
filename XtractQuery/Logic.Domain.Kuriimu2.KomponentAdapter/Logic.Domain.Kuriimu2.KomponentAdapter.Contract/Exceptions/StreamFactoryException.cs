using System.Runtime.Serialization;

namespace Logic.Domain.Kuriimu2.KomponentAdapter.Contract.Exceptions;

public class StreamFactoryException : Exception
{
    public StreamFactoryException()
    {
    }

    public StreamFactoryException(string message) : base(message)
    {
    }

    public StreamFactoryException(string message, Exception inner) : base(message, inner)
    {
    }

    protected StreamFactoryException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}