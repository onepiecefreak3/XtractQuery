using System.Runtime.Serialization;

namespace Logic.Domain.Level5.Script.Xseq.InternalContract.Exceptions;

public class XseqStringTableException : Exception
{
    public XseqStringTableException()
    {
    }

    public XseqStringTableException(string message) : base(message)
    {
    }

    public XseqStringTableException(string message, Exception inner) : base(message, inner)
    {
    }

    protected XseqStringTableException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}