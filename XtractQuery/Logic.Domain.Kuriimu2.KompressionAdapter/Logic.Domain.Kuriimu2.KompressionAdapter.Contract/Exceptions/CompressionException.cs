using System.Runtime.Serialization;

namespace Logic.Domain.Kuriimu2.KompressionAdapter.Contract.Exceptions;

[Serializable]
public class CompressionException : Exception
{
    public CompressionException()
    {
    }

    public CompressionException(string message) : base(message)
    {
    }

    public CompressionException(string message, Exception inner) : base(message, inner)
    {
    }

    protected CompressionException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}