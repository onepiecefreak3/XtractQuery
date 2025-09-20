using System.Runtime.Serialization;

namespace Logic.Domain.Kuriimu2.KryptographyAdapter.Contract.Exceptions;

public class CrcChecksumFactoryException : Exception
{
    public CrcChecksumFactoryException()
    {
    }

    public CrcChecksumFactoryException(string message) : base(message)
    {
    }

    public CrcChecksumFactoryException(string message, Exception inner) : base(message, inner)
    {
    }

    protected CrcChecksumFactoryException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}