using System.Runtime.Serialization;

namespace Logic.Domain.Kuriimu2.KompressionAdapter.InternalContract.Exceptions;

internal class Level5Huffman8BitCompressionException:Exception
{
    public Level5Huffman8BitCompressionException()
    {
    }

    public Level5Huffman8BitCompressionException(string message) : base(message)
    {
    }

    public Level5Huffman8BitCompressionException(string message, Exception inner) : base(message, inner)
    {
    }

    protected Level5Huffman8BitCompressionException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}