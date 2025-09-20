using System.Runtime.Serialization;

namespace Logic.Domain.Kuriimu2.KryptographyAdapter.Checksum.InternalContract.Exceptions;

public class JamCrcChecksumException : Exception
{
    public JamCrcChecksumException()
    {
    }

    public JamCrcChecksumException(string message) : base(message)
    {
    }

    public JamCrcChecksumException(string message, Exception inner) : base(message, inner)
    {
    }

    protected JamCrcChecksumException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}