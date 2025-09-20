using System.Runtime.Serialization;

namespace Logic.Domain.Level5.Script.Xseq.InternalContract.Exceptions;

public class XseqScriptDecompressorException : Exception
{
    public XseqScriptDecompressorException()
    {
    }

    public XseqScriptDecompressorException(string message) : base(message)
    {
    }

    public XseqScriptDecompressorException(string message, Exception inner) : base(message, inner)
    {
    }

    protected XseqScriptDecompressorException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}