using System.Runtime.Serialization;

namespace Logic.Domain.Level5.Script.Xseq.InternalContract.Exceptions;

public class XseqScriptCompressorException : Exception
{
    public XseqScriptCompressorException()
    {
    }

    public XseqScriptCompressorException(string message) : base(message)
    {
    }

    public XseqScriptCompressorException(string message, Exception inner) : base(message, inner)
    {
    }

    protected XseqScriptCompressorException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}