using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
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

        public Xq32ScriptReader(IXq32ScriptDecompressor decompressor, IXq32ScriptHashStringCache cache, IBinaryFactory binaryFactory)
            : base(decompressor, binaryFactory)
        {
            _binaryFactory = binaryFactory;
            _cache = cache;
        }

        public override IReadOnlyList<Xq32Function> ReadFunctions(Stream functionStream, int entryCount)
        {
            using IBinaryReaderX br = _binaryFactory.CreateReader(functionStream, true);

            var result = new Xq32Function[entryCount];

            for (var i = 0; i < entryCount; i++)
            {
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
            }

            return result;
        }

        public override IReadOnlyList<Xq32Jump> ReadJumps(Stream jumpStream, int entryCount)
        {
            using IBinaryReaderX br = _binaryFactory.CreateReader(jumpStream, true);

            var result = new Xq32Jump[entryCount];

            for (var i = 0; i < entryCount; i++)
            {
                result[i] = new Xq32Jump
                {
                    nameOffset = br.ReadInt32(),
                    crc32 = br.ReadUInt32(),
                    instructionIndex = br.ReadInt32()
                };
            }

            return result;
        }

        public override IReadOnlyList<Xq32Instruction> ReadInstructions(Stream instructionStream, int entryCount)
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
                    instructionType = br.ReadInt16(),
                    zero0 = br.ReadInt32()
                };
            }

            return result;
        }

        public override IReadOnlyList<Xq32Argument> ReadArguments(Stream argumentStream, int entryCount)
        {
            using IBinaryReaderX br = _binaryFactory.CreateReader(argumentStream, true);

            var result = new Xq32Argument[entryCount];

            for (var i = 0; i < entryCount; i++)
            {
                result[i] = new Xq32Argument
                {
                    type = br.ReadInt32(),
                    value = br.ReadUInt32()
                };
            }

            return result;
        }

        IList<ScriptFunction> IXq32ScriptReader.CreateFunctions(IReadOnlyList<Xq32Function> functions, ScriptStringTable? stringTable)
        {
            return CreateFunctions(functions, stringTable);
        }

        IList<ScriptJump> IXq32ScriptReader.CreateJumps(IReadOnlyList<Xq32Jump> jumps, ScriptStringTable? stringTable)
        {
            return CreateJumps(jumps, stringTable);
        }

        IList<ScriptInstruction> IXq32ScriptReader.CreateInstructions(IReadOnlyList<Xq32Instruction> instructions)
        {
            return CreateInstructions(instructions);
        }

        IList<ScriptArgument> IXq32ScriptReader.CreateArguments(IReadOnlyList<Xq32Argument> arguments, ScriptStringTable? stringTable)
        {
            return CreateArguments(arguments, stringTable);
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
