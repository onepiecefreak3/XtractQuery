using System.Runtime.Serialization;

namespace Logic.Domain.Kuriimu2.KompressionAdapter.Contract.Exceptions;

[Serializable]
public class CompressionFactoryException : Exception
{
    public CompressionFactoryException()
    {
    }

    public CompressionFactoryException(string message) : base(message)
    {
    }

    public CompressionFactoryException(string message, Exception inner) : base(message, inner)
    {
    }

    protected CompressionFactoryException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}