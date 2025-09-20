using System.Runtime.Serialization;

namespace Logic.Domain.Kuriimu2.KomponentAdapter.Contract.Exceptions;

public class BinaryTypeSizeMeasurementException : Exception
{
    public BinaryTypeSizeMeasurementException()
    {
    }

    public BinaryTypeSizeMeasurementException(string message) : base(message)
    {
    }

    public BinaryTypeSizeMeasurementException(string message, Exception inner) : base(message, inner)
    {
    }

    protected BinaryTypeSizeMeasurementException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}