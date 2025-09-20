using System.Runtime.Serialization;

namespace Logic.Domain.Level5.Script.Xq32.InternalContract.Exceptions;

public class Xq32ScriptCompressorException : Exception
{
    public Xq32ScriptCompressorException()
    {
    }

    public Xq32ScriptCompressorException(string message) : base(message)
    {
    }

    public Xq32ScriptCompressorException(string message, Exception inner) : base(message, inner)
    {
    }

    protected Xq32ScriptCompressorException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}