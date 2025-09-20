using System.Runtime.Serialization;

namespace Logic.Domain.Kuriimu2.KomponentAdapter.Contract.Exceptions;

internal class BinaryTypeReaderException : Exception
{
    public BinaryTypeReaderException()
    {
    }

    public BinaryTypeReaderException(string message) : base(message)
    {
    }

    public BinaryTypeReaderException(string message, Exception inner) : base(message, inner)
    {
    }

    protected BinaryTypeReaderException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}