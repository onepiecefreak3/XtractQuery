using System.Runtime.Serialization;

namespace Logic.Domain.CodeAnalysis.Contract.Level5.Exceptions;

public class Level5ScriptParserException:Exception
{
    public int Line { get; }
    public int Column { get; }

    public Level5ScriptParserException()
    {
    }

    public Level5ScriptParserException(string message) : base(message)
    {
    }

    public Level5ScriptParserException(string message, Exception inner) : base(message, inner)
    {
    }

    public Level5ScriptParserException(string message, int line, int column) : base(message)
    {
        Line = line;
        Column = column;
    }

    protected Level5ScriptParserException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}