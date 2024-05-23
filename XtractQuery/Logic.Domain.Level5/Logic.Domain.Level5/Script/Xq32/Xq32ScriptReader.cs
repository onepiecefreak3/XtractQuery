using Logic.Domain.Kuriimu2.KomponentAdapter.Contract;
using Logic.Domain.Level5.Contract.Script.DataClasses;
using Logic.Domain.Level5.Contract.Script.Xq32;
using Logic.Domain.Level5.Contract.Script.Xq32.DataClasses;
using Logic.Domain.Level5.Script.Xq32.InternalContract;

namespace Logic.Domain.Level5.Script.Xq32
{
    internal class Xq32ScriptReader : ScriptReader<Xq32Function, Xq32Jump, Xq32Instruction, Xq32Argument>, IXq32ScriptReader
    {
        private readonly IBinaryFactory _binaryFactory;
        private readonly IXq32ScriptHashStringCache _cache;

        public Xq32ScriptReader(IXq32ScriptDecompressor decompressor, IXq32ScriptHashStringCache cache,
            IXq32ScriptEntrySizeProvider entrySizeProvider, IBinaryFactory binaryFactory)
            : base(decompressor, entrySizeProvider, binaryFactory)
        {
            _binaryFactory = binaryFactory;
            _cache = cache;
        }

        public override IReadOnlyList<Xq32Function> ReadFunctions(Stream functionStream, int entryCount, PointerLength length)
        {
            using IBinaryReaderX br = _binaryFactory.CreateReader(functionStream, true);

            var result = new Xq32Function[entryCount];

            for (var i = 0; i < entryCount; i++)
            {
                switch (length)
                {
                    case PointerLength.Int:
                        result[i] = new Xq32Function
                        {
                            nameOffset = br.ReadInt32(),
                            crc32 = br.ReadUInt32(),
                            instructionOffset = br.ReadInt16(),
                            instructionEndOffset = br.ReadInt16(),
                            jumpOffset = br.ReadInt16(),
                            jumpCount = br.ReadInt16(),
                            localCount = br.ReadInt16(),
                            objectCount = br.ReadInt16(),
                            parameterCount = br.ReadInt32()
                        };
                        break;

                    case PointerLength.Long:
                        result[i] = new Xq32Function
                        {
                            nameOffset = br.ReadInt64(),
                            crc32 = br.ReadUInt32(),
                            instructionOffset = br.ReadInt16(),
                            instructionEndOffset = br.ReadInt16(),
                            jumpOffset = br.ReadInt16(),
                            jumpCount = br.ReadInt16(),
                            localCount = br.ReadInt16(),
                            objectCount = br.ReadInt16(),
                            parameterCount = br.ReadInt32()
                        };
                        br.BaseStream.Position += 4;
                        break;

                    default:
                        throw new InvalidOperationException($"Unknown pointer length {length}.");
                }
            }

            return result;
        }

        public override IReadOnlyList<Xq32Jump> ReadJumps(Stream jumpStream, int entryCount, PointerLength length)
        {
            using IBinaryReaderX br = _binaryFactory.CreateReader(jumpStream, true);

            var result = new Xq32Jump[entryCount];

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

                result[i] = new Xq32Jump
                {
                    nameOffset = nameOffset,
                    crc32 = br.ReadUInt32(),
                    instructionIndex = br.ReadInt32()
                };
            }

            return result;
        }

        public override IReadOnlyList<Xq32Instruction> ReadInstructions(Stream instructionStream, int entryCount, PointerLength length)
        {
            using IBinaryReaderX br = _binaryFactory.CreateReader(instructionStream, true);

            var result = new Xq32Instruction[entryCount];

            for (var i = 0; i < entryCount; i++)
            {
                result[i] = new Xq32Instruction
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

        public override IReadOnlyList<Xq32Argument> ReadArguments(Stream argumentStream, int entryCount, PointerLength length)
        {
            using IBinaryReaderX br = _binaryFactory.CreateReader(argumentStream, true);

            var result = new Xq32Argument[entryCount];

            for (var i = 0; i < entryCount; i++)
            {
                switch (length)
                {
                    case PointerLength.Int:
                        result[i] = new Xq32Argument
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

                        result[i] = new Xq32Argument
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

        protected override ScriptFunction CreateFunction(Xq32Function function, IBinaryReaderX? stringReader)
        {
            var name = string.Empty;
            if (stringReader != null)
            {
                stringReader.BaseStream.Position = function.nameOffset;
                name = stringReader.ReadCStringSJIS();

                _cache.Set(function.crc32, name);
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

        protected override ScriptJump CreateJump(Xq32Jump jump, IBinaryReaderX? stringReader)
        {
            var name = string.Empty;
            if (stringReader != null)
            {
                stringReader.BaseStream.Position = jump.nameOffset;
                name = stringReader.ReadCStringSJIS();

                _cache.Set(jump.crc32, name);
            }

            return new ScriptJump
            {
                Name = name,

                InstructionIndex = jump.instructionIndex
            };
        }

        protected override ScriptInstruction CreateInstruction(Xq32Instruction instruction)
        {
            return new ScriptInstruction
            {
                ArgumentIndex = instruction.argOffset,
                ArgumentCount = instruction.argCount,

                Type = instruction.instructionType,

                ReturnParameter = instruction.returnParameter
            };
        }

        protected override ScriptArgument CreateArgument(Xq32Argument argument, IBinaryReaderX? stringReader)
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

                    if (_cache.TryGet(argument.value, out string name))
                        value = name;

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

        protected override IEnumerable<Xq32Function> OrderFunctions(IReadOnlyList<Xq32Function> functions)
        {
            return functions.OrderBy(x => x.instructionOffset).ThenBy(x => x.instructionEndOffset).ThenBy(x => x.crc32);
        }
    }
}
