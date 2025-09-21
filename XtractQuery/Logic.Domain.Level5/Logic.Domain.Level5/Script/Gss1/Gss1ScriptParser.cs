using Logic.Domain.Kuriimu2.KomponentAdapter.Contract;
using Logic.Domain.Level5.Contract.Script.DataClasses;
using Logic.Domain.Level5.Contract.Script.Gss1;
using Logic.Domain.Level5.Contract.Script.Gss1.DataClasses;

namespace Logic.Domain.Level5.Script.Gss1;

internal class Gss1ScriptParser(IBinaryFactory binaryFactory, IGss1ScriptReader reader) : IGss1ScriptParser
{
    private readonly Dictionary<ushort, HashSet<string>> _functionCache = [];
    private readonly Dictionary<ushort, HashSet<string>> _jumpCache = [];

    public Gss1ScriptFile Parse(Stream input)
    {
        Gss1ScriptContainer container = reader.Read(input);
        return Parse(container);
    }

    public Gss1ScriptFile Parse(Gss1ScriptContainer container)
    {
        IList<ScriptInstruction> instructions = ParseInstructions(container.Instructions);

        return new Gss1ScriptFile
        {
            Functions = ParseFunctions(container.Functions, container.Strings),
            Jumps = ParseJumps(container.Jumps, container.Strings),
            Instructions = instructions,
            Arguments = ParseArguments(container.Arguments, instructions.AsReadOnly(), container.Strings)
        };
    }

    public IList<ScriptFunction> ParseFunctions(Gss1Function[] functions, ScriptStringTable? strings = null)
    {
        using IBinaryReaderX? stringReader = strings is null ? null : binaryFactory.CreateReader(strings.Stream, true);
        return ParseFunctions(functions, stringReader, strings?.BaseOffset ?? 0);
    }

    public IList<ScriptJump> ParseJumps(Gss1Jump[] jumps, ScriptStringTable? strings = null)
    {
        using IBinaryReaderX? stringReader = strings is null ? null : binaryFactory.CreateReader(strings.Stream, true);
        return ParseJumps(jumps, stringReader, strings?.BaseOffset ?? 0);
    }

    public IList<ScriptInstruction> ParseInstructions(Gss1Instruction[] instructions)
    {
        var result = new ScriptInstruction[instructions.Length];

        for (var i = 0; i < result.Length; i++)
        {
            result[i] = new ScriptInstruction
            {
                ArgumentIndex = instructions[i].argOffset,
                ArgumentCount = instructions[i].argCount,
                ReturnParameter = instructions[i].returnParameter,
                Type = instructions[i].instructionType
            };
        }

        return result;
    }

    public IList<ScriptArgument> ParseArguments(Gss1Argument[] arguments, IReadOnlyList<ScriptInstruction> instructions, ScriptStringTable? strings = null)
    {
        using IBinaryReaderX? stringReader = strings is null ? null : binaryFactory.CreateReader(strings.Stream, true);
        return ParseArguments(arguments, instructions, stringReader, strings?.BaseOffset ?? 0);
    }

    private IList<ScriptFunction> ParseFunctions(Gss1Function[] functions, IBinaryReaderX? stringReader, int stringBaseOffset)
    {
        var result = new ScriptFunction[functions.Length];

        for (var i = 0; i < result.Length; i++)
        {
            result[i] = new ScriptFunction
            {
                Name = ReadString(stringReader, functions[i].nameOffset - stringBaseOffset, functions[i].crc16, _functionCache) ?? string.Empty,
                InstructionIndex = functions[i].instructionOffset,
                InstructionCount = (short)(functions[i].instructionEndOffset - functions[i].instructionOffset),
                JumpIndex = functions[i].jumpOffset,
                JumpCount = functions[i].jumpCount,
                LocalCount = functions[i].localCount,
                ObjectCount = functions[i].objectCount,
                ParameterCount = functions[i].parameterCount
            };
        }

        return result;
    }

    private IList<ScriptJump> ParseJumps(Gss1Jump[] jumps, IBinaryReaderX? stringReader, int stringBaseOffset)
    {
        var result = new ScriptJump[jumps.Length];

        for (var i = 0; i < result.Length; i++)
        {
            result[i] = new ScriptJump
            {
                Name = ReadString(stringReader, jumps[i].nameOffset - stringBaseOffset, jumps[i].crc16, _jumpCache) ?? string.Empty,
                InstructionIndex = jumps[i].instructionIndex
            };
        }

        return result;
    }

    private IList<ScriptArgument> ParseArguments(Gss1Argument[] arguments, IReadOnlyList<ScriptInstruction> instructions,
        IBinaryReaderX? stringReader, int stringBaseOffset)
    {
        var result = new ScriptArgument[arguments.Length];

        var instructionTypes = new (int, int)[arguments.Length];
        foreach (ScriptInstruction instruction in instructions)
        {
            for (var i = 0; i < instruction.ArgumentCount; i++)
                instructionTypes[instruction.ArgumentIndex + i] = (instruction.Type, i);
        }

        var counter = 0;
        foreach (Gss1Argument argument in arguments)
        {
            (int instructionType, int argumentIndex) = instructionTypes[counter];
            result[counter++] = ParseArgument(argument, instructionType, argumentIndex, stringReader, stringBaseOffset);
        }

        return result;
    }

    private ScriptArgument ParseArgument(Gss1Argument argument, int instructionType, int argumentIndex,
        IBinaryReaderX? stringReader, int stringBaseOffset)
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

                if (argumentIndex != 0)
                {
                    if (_functionCache.TryGetValue((ushort)argument.value, out HashSet<string>? names)
                        || _jumpCache.TryGetValue((ushort)argument.value, out names))
                        value = names.First();
                    break;
                }

                switch (instructionType)
                {
                    case 20:
                        if (_functionCache.TryGetValue((ushort)argument.value, out HashSet<string>? names))
                            value = names.First();
                        break;

                    case 30:
                    case 31:
                    case 33:
                        if (_jumpCache.TryGetValue((ushort)argument.value, out names))
                            value = names.First();
                        break;
                }

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

        return new ScriptArgument
        {
            RawArgumentType = rawType,
            Type = type,
            Value = value
        };
    }

    private static string? ReadString(IBinaryReaderX? stringReader, int offset, ushort hash, Dictionary<ushort, HashSet<string>> cache)
    {
        if (stringReader is null)
            return null;

        stringReader.BaseStream.Position = offset;
        string name = stringReader.ReadCStringSJIS();

        if (!cache.TryGetValue(hash, out HashSet<string>? functionNames))
            cache[hash] = functionNames = [];

        functionNames.Add(name);

        return name;
    }
}