using System.Runtime.Serialization;

namespace Logic.Domain.Kuriimu2.KryptographyAdapter.Checksum.InternalContract.Exceptions;

public class Crc16X25ChecksumException : Exception
{
    public Crc16X25ChecksumException()
    {
    }

    public Crc16X25ChecksumException(string message) : base(message)
    {
    }

    public Crc16X25ChecksumException(string message, Exception inner) : base(message, inner)
    {
    }

    protected Crc16X25ChecksumException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}