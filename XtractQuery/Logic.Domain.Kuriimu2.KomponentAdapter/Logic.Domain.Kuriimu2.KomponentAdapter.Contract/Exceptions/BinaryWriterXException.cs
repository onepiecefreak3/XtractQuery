using System.Runtime.Serialization;

namespace Logic.Domain.Kuriimu2.KomponentAdapter.Contract.Exceptions;

public class BinaryWriterXException:Exception
{
    public BinaryWriterXException()
    {
    }

    public BinaryWriterXException(string message) : base(message)
    {
    }

    public BinaryWriterXException(string message, Exception inner) : base(message, inner)
    {
    }

    protected BinaryWriterXException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}