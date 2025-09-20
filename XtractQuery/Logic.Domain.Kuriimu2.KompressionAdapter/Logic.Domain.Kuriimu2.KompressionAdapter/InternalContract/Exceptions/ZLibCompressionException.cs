using System.Runtime.Serialization;

namespace Logic.Domain.Kuriimu2.KompressionAdapter.InternalContract.Exceptions;

internal class ZLibCompressionException:Exception
{
    public ZLibCompressionException()
    {
    }

    public ZLibCompressionException(string message) : base(message)
    {
    }

    public ZLibCompressionException(string message, Exception inner) : base(message, inner)
    {
    }

    protected ZLibCompressionException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}