using System.Runtime.Serialization;

namespace Logic.Domain.Kuriimu2.KryptographyAdapter.Checksum.InternalContract.Exceptions;

public class Crc32StandardChecksumException : Exception
{
    public Crc32StandardChecksumException()
    {
    }

    public Crc32StandardChecksumException(string message) : base(message)
    {
    }

    public Crc32StandardChecksumException(string message, Exception inner) : base(message, inner)
    {
    }

    protected Crc32StandardChecksumException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}