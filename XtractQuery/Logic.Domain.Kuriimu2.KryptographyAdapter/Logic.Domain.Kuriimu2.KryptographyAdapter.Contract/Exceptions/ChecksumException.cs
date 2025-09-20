using System.Runtime.Serialization;

namespace Logic.Domain.Kuriimu2.KryptographyAdapter.Contract.Exceptions;

public class ChecksumException : Exception
{
    public ChecksumException()
    {
    }

    public ChecksumException(string message) : base(message)
    {
    }

    public ChecksumException(string message, Exception inner) : base(message, inner)
    {
    }

    protected ChecksumException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}