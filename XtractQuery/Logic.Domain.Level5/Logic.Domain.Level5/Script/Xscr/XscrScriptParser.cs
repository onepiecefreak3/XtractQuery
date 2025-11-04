using System.Text;
using Komponent.IO;
using Logic.Domain.Level5.Contract.DataClasses.Script;
using Logic.Domain.Level5.Contract.DataClasses.Script.Xscr;
using Logic.Domain.Level5.Contract.Script.Xscr;

namespace Logic.Domain.Level5.Script.Xscr;

internal class XscrScriptParser(IXscrScriptReader reader) : IXscrScriptParser
{
    private static readonly Encoding SjisEncoding = Encoding.GetEncoding("Shift-JIS");

    public XscrScriptFile Parse(Stream input)
    {
        XscrScriptContainer container = reader.Read(input);

        return Parse(container);
    }

    public XscrScriptFile Parse(XscrCompressionContainer container)
    {
        XscrScriptContainer scriptContainer = reader.Read(container);

        return Parse(scriptContainer);
    }

    public XscrScriptFile Parse(XscrScriptContainer container)
    {
        return new XscrScriptFile
        {
            Instructions = ParseInstructions(container.Instructions),
            Arguments = ParseArguments(container.Arguments, container.Strings)
        };
    }

    public IList<XscrScriptInstruction> ParseInstructions(XscrInstruction[] instructions)
    {
        var result = new XscrScriptInstruction[instructions.Length];

        for (var i = 0; i < result.Length; i++)
        {
            result[i] = new XscrScriptInstruction
            {
                Type = instructions[i].instructionType,
                ArgumentIndex = instructions[i].argOffset,
                ArgumentCount = instructions[i].argCount
            };
        }

        return result;
    }

    public IList<XscrScriptArgument> ParseArguments(XscrArgument[] arguments, ScriptStringTable? stringTable = null)
    {
        using BinaryReaderX? stringReader = stringTable is null ? null : new BinaryReaderX(stringTable.Stream, SjisEncoding, true);
        return ParseArguments(arguments, stringReader, stringTable?.BaseOffset ?? 0);
    }

    private IList<XscrScriptArgument> ParseArguments(XscrArgument[] arguments, BinaryReaderX? stringReader, int stringBaseOffset)
    {
        var result = new XscrScriptArgument[arguments.Length];

        for (var i = 0; i < arguments.Length; i++)
            result[i] = ParseArgument(arguments[i], stringReader, stringBaseOffset);

        return result;
    }

    private XscrScriptArgument ParseArgument(XscrArgument argument, BinaryReaderX? stringReader, int stringBaseOffset)
    {
        int rawType = -1;
        ScriptArgumentType type;
        object value;

        switch (argument.type)
        {
            case 1:
                type = ScriptArgumentType.Int;
                value = (int)argument.value;
                break;

            case 2: // Hashed string
                type = ScriptArgumentType.StringHash;
                value = argument.value;
                break;

            case 3:
                type = ScriptArgumentType.Float;
                value = BitConverter.UInt32BitsToSingle(argument.value);
                break;

            case 4:
                type = ScriptArgumentType.Variable;
                value = argument.value;
                break;

            case 24: // String
            case 25: // Method Name
                if (stringReader != null)
                    stringReader.BaseStream.Position = argument.value - stringBaseOffset;

                if (argument.type != 24)
                    rawType = argument.type;

                type = ScriptArgumentType.String;
                value = stringReader?.ReadNullTerminatedString() ?? string.Empty;
                break;

            default:
                throw new InvalidOperationException($"Unknown argument type {argument.type}.");
        }

        return new XscrScriptArgument
        {
            RawArgumentType = rawType,
            Type = type,
            Value = value
        };
    }
}