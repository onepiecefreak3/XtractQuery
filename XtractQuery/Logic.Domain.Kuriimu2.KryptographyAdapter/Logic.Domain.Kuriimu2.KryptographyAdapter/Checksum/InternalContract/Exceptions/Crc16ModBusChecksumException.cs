using System.Runtime.Serialization;

namespace Logic.Domain.Kuriimu2.KryptographyAdapter.Checksum.InternalContract.Exceptions;

public class Crc16ModBusChecksumException : Exception
{
    public Crc16ModBusChecksumException()
    {
    }

    public Crc16ModBusChecksumException(string message) : base(message)
    {
    }

    public Crc16ModBusChecksumException(string message, Exception inner) : base(message, inner)
    {
    }

    protected Crc16ModBusChecksumException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}