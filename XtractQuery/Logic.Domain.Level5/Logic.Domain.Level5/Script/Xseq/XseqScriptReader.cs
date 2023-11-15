using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract;
using Logic.Domain.Level5.Contract.Script;
using Logic.Domain.Level5.Contract.Script.DataClasses;
using Logic.Domain.Level5.Contract.Script.Xseq;
using Logic.Domain.Level5.Contract.Script.Xseq.DataClasses;
using Logic.Domain.Level5.Script.Xseq.InternalContract;

namespace Logic.Domain.Level5.Script.Xseq
{
    internal class XseqScriptReader : ScriptReader<XseqFunction, XseqJump, XseqInstruction, XseqArgument>, IXseqScriptReader
    {
        private readonly IBinaryFactory _binaryFactory;
        private readonly IXseqScriptHashStringCache _cache;

        public XseqScriptReader(IXseqScriptDecompressor decompressor, IXseqScriptHashStringCache cache, IBinaryFactory binaryFactory)
            : base(decompressor, binaryFactory)
        {
            _binaryFactory = binaryFactory;
            _cache = cache;
        }

        public override IReadOnlyList<XseqFunction> ReadFunctions(Stream functionStream, int entryCount)
        {
            using IBinaryReaderX br = _binaryFactory.CreateReader(functionStream, true);

            var result = new XseqFunction[entryCount];

            for (var i = 0; i < entryCount; i++)
            {
                result[i] = new XseqFunction
                {
                    nameOffset = br.ReadInt32(),
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

        public override IReadOnlyList<XseqJump> ReadJumps(Stream jumpStream, int entryCount)
        {
            using IBinaryReaderX br = _binaryFactory.CreateReader(jumpStream, true);

            var result = new XseqJump[entryCount];

            for (var i = 0; i < entryCount; i++)
            {
                result[i] = new XseqJump
                {
                    nameOffset = br.ReadInt32(),
                    crc16 = br.ReadUInt16(),
                    instructionIndex = br.ReadInt16()
                };
            }

            return result;
        }

        public override IReadOnlyList<XseqInstruction> ReadInstructions(Stream instructionStream, int entryCount)
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
                    instructionType = br.ReadInt16(),
                    zero0 = br.ReadInt32()
                };
            }

            return result;
        }

        public override IReadOnlyList<XseqArgument> ReadArguments(Stream argumentStream, int entryCount)
        {
            using IBinaryReaderX br = _binaryFactory.CreateReader(argumentStream, true);

            var result = new XseqArgument[entryCount];

            for (var i = 0; i < entryCount; i++)
            {
                result[i] = new XseqArgument
                {
                    type = br.ReadInt32(),
                    value = br.ReadUInt32()
                };
            }

            return result;
        }

        IList<ScriptFunction> IXseqScriptReader.CreateFunctions(IReadOnlyList<XseqFunction> functions, ScriptStringTable? stringTable)
        {
            return CreateFunctions(functions, stringTable);
        }

        IList<ScriptJump> IXseqScriptReader.ReadJumps(IReadOnlyList<XseqJump> jumps, ScriptStringTable? stringTable)
        {
            return CreateJumps(jumps, stringTable);
        }

        IList<ScriptInstruction> IXseqScriptReader.ReadInstructions(IReadOnlyList<XseqInstruction> instructions)
        {
            return CreateInstructions(instructions);
        }

        IList<ScriptArgument> IXseqScriptReader.ReadArguments(IReadOnlyList<XseqArgument> arguments, ScriptStringTable? stringTable)
        {
            return CreateArguments(arguments, stringTable);
        }

        protected override ScriptFunction CreateFunction(XseqFunction function, IBinaryReaderX? stringReader)
        {
            var name = string.Empty;
            if (stringReader != null)
            {
                stringReader.BaseStream.Position = function.nameOffset;
                name = stringReader.ReadCStringSJIS();

                _cache.Set(function.crc16, name);
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

                _cache.Set(jump.crc16, name);
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

        protected override ScriptArgument CreateArgument(XseqArgument argument, IBinaryReaderX? stringReader)
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

                    if (_cache.TryGet((ushort)argument.value, out string name))
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

        protected override IEnumerable<XseqFunction> OrderFunctions(IReadOnlyList<XseqFunction> functions)
        {
            return functions.OrderBy(x => x.instructionOffset).ThenBy(x => x.instructionEndOffset).ThenBy(x => x.crc16);
        }
    }
}
