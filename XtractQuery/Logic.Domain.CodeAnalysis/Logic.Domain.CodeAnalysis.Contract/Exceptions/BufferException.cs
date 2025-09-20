using System.Runtime.Serialization;

namespace Logic.Domain.CodeAnalysis.Contract.Exceptions;

[Serializable]
public class BufferException : Exception
{
    public BufferException()
    {
    }

    public BufferException(string message) : base(message)
    {
    }

    public BufferException(string message, Exception inner) : base(message, inner)
    {
    }

    protected BufferException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}