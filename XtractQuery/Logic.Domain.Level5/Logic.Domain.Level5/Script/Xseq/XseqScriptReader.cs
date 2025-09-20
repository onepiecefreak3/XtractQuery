using Logic.Domain.Kuriimu2.KomponentAdapter.Contract;
using Logic.Domain.Level5.Contract.Script.DataClasses;
using Logic.Domain.Level5.Contract.Script.Xseq;
using Logic.Domain.Level5.Contract.Script.Xseq.DataClasses;
using Logic.Domain.Level5.Script.Xseq.InternalContract;

namespace Logic.Domain.Level5.Script.Xseq;

internal class XseqScriptReader : ScriptReader<XseqFunction, XseqJump, XseqInstruction, XseqArgument>, IXseqScriptReader
{
    private readonly IBinaryFactory _binaryFactory;
    private readonly Dictionary<ushort, HashSet<string>> _functionCache;
    private readonly Dictionary<ushort, HashSet<string>> _jumpCache;

    public XseqScriptReader(IXseqScriptDecompressor decompressor, IXseqScriptEntrySizeProvider entrySizeProvider,
        IBinaryFactory binaryFactory)
        : base(decompressor, entrySizeProvider, binaryFactory)
    {
        _binaryFactory = binaryFactory;
        _functionCache = new Dictionary<ushort, HashSet<string>>();
        _jumpCache = new Dictionary<ushort, HashSet<string>>();
    }

    public override IReadOnlyList<XseqFunction> ReadFunctions(Stream functionStream, int entryCount, PointerLength length)
    {
        using IBinaryReaderX br = _binaryFactory.CreateReader(functionStream, true);

        var result = new XseqFunction[entryCount];

        for (var i = 0; i < entryCount; i++)
        {
            long nameOffset;
            switch (length)
            {
                case PointerLength.Int:
                    nameOffset = br.ReadInt32();
                    break;

                case PointerLength.Long:
                    nameOffset = br.ReadInt64();
                    break;

                default:
                    throw new InvalidOperationException($"Unknown pointer length {length}.");
            }

            result[i] = new XseqFunction
            {
                nameOffset = nameOffset,
                crc16 = br.ReadUInt16(),
                instructionOffset = br.ReadInt16(),
                instructionEndOffset = br.ReadInt16(),
                jumpOffset = br.ReadInt16(),
                jumpCount = br.ReadInt16(),
                localCount = br.ReadInt16(),
                objectCount = br.ReadInt16(),
                parameterCount = br.ReadInt16()
            };
        }

        return result;
    }

    public override IReadOnlyList<XseqJump> ReadJumps(Stream jumpStream, int entryCount, PointerLength length)
    {
        using IBinaryReaderX br = _binaryFactory.CreateReader(jumpStream, true);

        var result = new XseqJump[entryCount];

        for (var i = 0; i < entryCount; i++)
        {
            switch (length)
            {
                case PointerLength.Int:
                    result[i] = new XseqJump
                    {
                        nameOffset = br.ReadInt32(),
                        crc16 = br.ReadUInt16(),
                        instructionIndex = br.ReadInt16()
                    };
                    break;

                case PointerLength.Long:
                    result[i] = new XseqJump
                    {
                        nameOffset = br.ReadInt64(),
                        crc16 = br.ReadUInt16(),
                        instructionIndex = br.ReadInt16()
                    };
                    br.BaseStream.Position += 4;
                    break;

                default:
                    throw new InvalidOperationException($"Unknown pointer length {length}.");
            }
        }

        return result;
    }

    public override IReadOnlyList<XseqInstruction> ReadInstructions(Stream instructionStream, int entryCount, PointerLength length)
    {
        using IBinaryReaderX br = _binaryFactory.CreateReader(instructionStream, true);

        var result = new XseqInstruction[entryCount];

        for (var i = 0; i < entryCount; i++)
        {
            result[i] = new XseqInstruction
            {
                argOffset = br.ReadInt16(),
                argCount = br.ReadInt16(),
                returnParameter = br.ReadInt16(),
                instructionType = br.ReadInt16()
            };

            switch (length)
            {
                case PointerLength.Int:
                    br.BaseStream.Position += 4;
                    break;

                case PointerLength.Long:
                    br.BaseStream.Position += 8;
                    break;

                default:
                    throw new InvalidOperationException($"Unknown pointer length {length}.");
            }
        }

        return result;
    }

    public override IReadOnlyList<XseqArgument> ReadArguments(Stream argumentStream, int entryCount, PointerLength length)
    {
        using IBinaryReaderX br = _binaryFactory.CreateReader(argumentStream, true);

        var result = new XseqArgument[entryCount];

        for (var i = 0; i < entryCount; i++)
        {
            switch (length)
            {
                case PointerLength.Int:
                    result[i] = new XseqArgument
                    {
                        type = br.ReadInt32(),
                        value = br.ReadUInt32()
                    };
                    break;

                case PointerLength.Long:
                    int type = br.ReadInt32();
                    br.BaseStream.Position += 4;
                    uint value = br.ReadUInt32();
                    br.BaseStream.Position += 4;

                    result[i] = new XseqArgument
                    {
                        type = type,
                        value = value
                    };
                    break;

                default:
                    throw new InvalidOperationException($"Unknown pointer length {length}.");
            }
        }

        return result;
    }

    protected override ScriptFunction CreateFunction(XseqFunction function, IBinaryReaderX? stringReader)
    {
        var name = string.Empty;
        if (stringReader != null)
        {
            stringReader.BaseStream.Position = function.nameOffset;
            name = stringReader.ReadCStringSJIS();

            if (!_functionCache.TryGetValue(function.crc16, out HashSet<string>? functionNames))
                _functionCache[function.crc16] = functionNames = new HashSet<string>();

            functionNames.Add(name);
        }

        return new ScriptFunction
        {
            Name = name,

            JumpIndex = function.jumpOffset,
            JumpCount = function.jumpCount,

            InstructionIndex = function.instructionOffset,
            InstructionCount = (short)(function.instructionEndOffset - function.instructionOffset),

            ParameterCount = function.parameterCount,

            LocalCount = function.localCount,
            ObjectCount = function.objectCount
        };
    }

    protected override ScriptJump CreateJump(XseqJump jump, IBinaryReaderX? stringReader)
    {
        var name = string.Empty;
        if (stringReader != null)
        {
            stringReader.BaseStream.Position = jump.nameOffset;
            name = stringReader.ReadCStringSJIS();

            if (!_jumpCache.TryGetValue(jump.crc16, out HashSet<string>? jumpNames))
                _jumpCache[jump.crc16] = jumpNames = new HashSet<string>();

            jumpNames.Add(name);
        }

        return new ScriptJump
        {
            Name = name,

            InstructionIndex = jump.instructionIndex
        };
    }

    protected override ScriptInstruction CreateInstruction(XseqInstruction instruction)
    {
        return new ScriptInstruction
        {
            ArgumentIndex = instruction.argOffset,
            ArgumentCount = instruction.argCount,

            Type = instruction.instructionType,

            ReturnParameter = instruction.returnParameter
        };
    }

    protected override ScriptArgument CreateArgument(XseqArgument argument, int instructionType, int argumentIndex, IBinaryReaderX? stringReader)
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
                        if (_jumpCache.TryGetValue((ushort)argument.value, out names))
                            value = names.First();
                        break;

                    case 31:
                        if (_jumpCache.TryGetValue((ushort)argument.value, out names))
                            value = names.First();
                        break;

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
                    stringReader.BaseStream.Position = argument.value;

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

    protected override IEnumerable<XseqFunction> OrderFunctions(IReadOnlyList<XseqFunction> functions)
    {
        return functions.OrderBy(x => x.instructionOffset).ThenBy(x => x.instructionEndOffset).ThenBy(x => x.crc16);
    }
}