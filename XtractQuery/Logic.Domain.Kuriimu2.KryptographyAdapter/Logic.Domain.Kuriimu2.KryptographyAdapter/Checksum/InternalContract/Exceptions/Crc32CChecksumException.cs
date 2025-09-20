using System.Runtime.Serialization;

namespace Logic.Domain.Kuriimu2.KryptographyAdapter.Checksum.InternalContract.Exceptions;

public class Crc32CChecksumException : Exception
{
    public Crc32CChecksumException()
    {
    }

    public Crc32CChecksumException(string message) : base(message)
    {
    }

    public Crc32CChecksumException(string message, Exception inner) : base(message, inner)
    {
    }

    protected Crc32CChecksumException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}