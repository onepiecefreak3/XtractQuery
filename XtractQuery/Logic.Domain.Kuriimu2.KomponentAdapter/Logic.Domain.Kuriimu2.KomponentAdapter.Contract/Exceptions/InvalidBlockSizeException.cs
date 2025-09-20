using System.Runtime.Serialization;

namespace Logic.Domain.Kuriimu2.KomponentAdapter.Contract.Exceptions;

public class InvalidBlockSizeException:Exception
{
    public InvalidBlockSizeException(int blockSize): this($"The given BlockSize {blockSize} is not supported.")
    {
    }

    public InvalidBlockSizeException(string message) : base(message)
    {
    }

    public InvalidBlockSizeException(string message, Exception inner) : base(message, inner)
    {
    }

    protected InvalidBlockSizeException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}