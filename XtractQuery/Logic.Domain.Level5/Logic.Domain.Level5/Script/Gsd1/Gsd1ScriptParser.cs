using Logic.Domain.Kuriimu2.KomponentAdapter.Contract;
using Logic.Domain.Level5.Contract.Script.DataClasses;
using Logic.Domain.Level5.Contract.Script.Gsd1;
using Logic.Domain.Level5.Contract.Script.Gsd1.DataClasses;

namespace Logic.Domain.Level5.Script.Gsd1;

internal class Gsd1ScriptParser(IBinaryFactory binaryFactory, IGsd1ScriptReader reader) : IGsd1ScriptParser
{
    public Gsd1ScriptFile Parse(Stream input)
    {
        Gsd1ScriptContainer container = reader.Read(input);
        return Parse(container);
    }

    public Gsd1ScriptFile Parse(Gsd1ScriptContainer container)
    {
        return new Gsd1ScriptFile
        {
            Instructions = ParseInstructions(container.Instructions),
            Arguments = ParseArguments(container.Arguments, container.Strings)
        };
    }

    public IList<Gsd1ScriptInstruction> ParseInstructions(Gsd1Instruction[] instructions)
    {
        var result = new Gsd1ScriptInstruction[instructions.Length];

        for (var i = 0; i < result.Length; i++)
        {
            result[i] = new Gsd1ScriptInstruction
            {
                Type = instructions[i].instructionType,
                ArgumentIndex = instructions[i].argOffset,
                ArgumentCount = instructions[i].argCount
            };
        }

        return result;
    }

    public IList<Gsd1ScriptArgument> ParseArguments(Gsd1Argument[] arguments, ScriptStringTable? strings = null)
    {
        using IBinaryReaderX? stringReader = strings is null ? null : binaryFactory.CreateReader(strings.Stream, true);
        return ParseArguments(arguments, stringReader, strings?.BaseOffset ?? 0);
    }

    private IList<Gsd1ScriptArgument> ParseArguments(Gsd1Argument[] arguments, IBinaryReaderX? stringReader, int stringBaseOffset)
    {
        var result = new Gsd1ScriptArgument[arguments.Length];

        for (var i = 0; i < arguments.Length; i++)
            result[i] = ParseArgument(arguments[i], stringReader, stringBaseOffset);

        return result;
    }

    private Gsd1ScriptArgument ParseArgument(Gsd1Argument argument, IBinaryReaderX? stringReader, int stringBaseOffset)
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
                value = stringReader?.ReadCStringSJIS() ?? string.Empty;
                break;

            default:
                throw new InvalidOperationException($"Unknown argument type {argument.type}.");
        }

        return new Gsd1ScriptArgument
        {
            RawArgumentType = rawType,
            Type = type,
            Value = value
        };
    }
}