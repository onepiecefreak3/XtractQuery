using System.Runtime.Serialization;

namespace Logic.Domain.Kuriimu2.KompressionAdapter.InternalContract.Exceptions;

internal class Level5RleCompressionException:Exception
{
    public Level5RleCompressionException()
    {
    }

    public Level5RleCompressionException(string message) : base(message)
    {
    }

    public Level5RleCompressionException(string message, Exception inner) : base(message, inner)
    {
    }

    protected Level5RleCompressionException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}