using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract;
using Logic.Domain.Kuriimu2.KryptographyAdapter.Contract;
using Logic.Domain.Level5.Contract.Compression.DataClasses;
using Logic.Domain.Level5.Contract.Script.DataClasses;
using Logic.Domain.Level5.Contract.Script.Xq32;
using Logic.Domain.Level5.Contract.Script.Xq32.DataClasses;
using Logic.Domain.Level5.Cryptography.InternalContract;
using Logic.Domain.Level5.Script.Xq32.InternalContract;

namespace Logic.Domain.Level5.Script.Xq32
{
    internal class Xq32ScriptWriter : IXq32ScriptWriter
    {
        private readonly IXq32ScriptCompressor _compressor;
        private readonly IBinaryFactory _binaryFactory;
        private readonly IBinaryTypeWriter _typeWriter;
        private readonly IChecksum<uint> _checksum;
        private readonly Encoding _sjisEncoding;

        public Xq32ScriptWriter(IXq32ScriptCompressor compressor, IBinaryFactory binaryFactory, IBinaryTypeWriter typeWriter, IChecksumFactory checksumFactory)
        {
            _compressor = compressor;
            _binaryFactory = binaryFactory;
            _typeWriter = typeWriter;
            _checksum = checksumFactory.CreateCrc32();

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            _sjisEncoding = Encoding.GetEncoding("Shift-JIS");
        }

        public void Write(ScriptFile script, Stream output)
        {
            ScriptContainer container = CreateContainer(script);

            Write(container, output);
        }

        public void Write(ScriptFile script, Stream output, CompressionType compressionType)
        {
            ScriptContainer container = CreateContainer(script);

            Write(container, output, compressionType);
        }

        public void Write(ScriptContainer container, Stream output)
        {
            _compressor.Compress(container, output);
        }

        public void Write(ScriptContainer container, Stream output, CompressionType compressionType)
        {
            _compressor.Compress(container, output, compressionType);
        }

        public void WriteFunctions(IReadOnlyList<Xq32Function> functions, Stream output)
        {
            using IBinaryWriterX bw = _binaryFactory.CreateWriter(output, false);

            foreach (Xq32Function function in functions)
            {
                bw.Write(function.nameOffset);
                bw.Write(function.crc32);
                bw.Write(function.instructionOffset);
                bw.Write(function.instructionEndOffset);
                bw.Write(function.jumpOffset);
                bw.Write(function.jumpCount);
                bw.Write(function.localCount);
                bw.Write(function.objectCount);
                bw.Write(function.parameterCount);
            }
        }

        public void WriteJumps(IReadOnlyList<Xq32Jump> jumps, Stream output)
        {
            using IBinaryWriterX bw = _binaryFactory.CreateWriter(output, false);

            foreach (Xq32Jump jump in jumps)
            {
                bw.Write(jump.nameOffset);
                bw.Write(jump.crc32);
                bw.Write(jump.instructionIndex);
            }
        }

        public void WriteInstructions(IReadOnlyList<Xq32Instruction> instructions, Stream output)
        {
            using IBinaryWriterX bw = _binaryFactory.CreateWriter(output, false);

            foreach (Xq32Instruction instruction in instructions)
            {
                bw.Write(instruction.argOffset);
                bw.Write(instruction.argCount);
                bw.Write(instruction.returnParameter);
                bw.Write(instruction.instructionType);
                bw.Write(instruction.zero0);
            }
        }

        public void WriteArguments(IReadOnlyList<Xq32Argument> arguments, Stream output)
        {
            using IBinaryWriterX bw = _binaryFactory.CreateWriter(output, false);

            foreach (Xq32Argument argument in arguments)
            {
                bw.Write(argument.type);
                bw.Write(argument.value);
            }
        }

        private ScriptContainer CreateContainer(ScriptFile script)
        {
            Stream stringStream = new MemoryStream();
            using IBinaryWriterX stringWriter = _binaryFactory.CreateWriter(stringStream, true);

            var writtenNames = new Dictionary<string, long>();
            var hashedNames = new Dictionary<string, uint>();

            Stream functionStream = WriteFunctions(script, stringWriter, writtenNames, hashedNames);
            Stream jumpStream = WriteJumps(script, stringWriter, writtenNames, hashedNames);
            Stream instructionStream = WriteInstructions(script.Instructions);
            Stream argumentStream = WriteArguments(script.Arguments, stringWriter, writtenNames, hashedNames);

            stringStream.Position = 0;
            return new ScriptContainer
            {
                GlobalVariableCount = CalculateGlobalVariableCount(script.Instructions, script.Arguments),

                FunctionTable = new ScriptTable { Stream = functionStream, EntryCount = script.Functions.Count },
                JumpTable = new ScriptTable { Stream = jumpStream, EntryCount = script.Jumps.Count },
                InstructionTable = new ScriptTable { Stream = instructionStream, EntryCount = script.Instructions.Count },
                ArgumentTable = new ScriptTable { Stream = argumentStream, EntryCount = script.Arguments.Count },
                StringTable = new ScriptStringTable { Stream = stringStream }
            };
        }

        private int CalculateGlobalVariableCount(IList<ScriptInstruction> instructions, IList<ScriptArgument> arguments)
        {
            IEnumerable<short> argumentVariables = arguments.Where(a => a.Type == ScriptArgumentType.Variable).Select(a => (short)(int)a.Value);
            IEnumerable<short> instructionVariables = instructions.Select(i => i.ReturnParameter);

            HashSet<short> globalVariables = argumentVariables.Concat(instructionVariables).Where(v => v is >= 4000 and < 5000).ToHashSet();
            if (globalVariables.Count <= 0)
                return 0;

            return globalVariables.Max() - 3999;
        }

        private short CalculateLocalVariableCount(ScriptFile script, ScriptFunction currentFunction)
        {
            ScriptInstruction[] instructions = script.Instructions.Skip(currentFunction.InstructionIndex).Take(currentFunction.InstructionCount).ToArray();
            IEnumerable<ScriptArgument> arguments = instructions.SelectMany(x => script.Arguments.Skip(x.ArgumentIndex).Take(x.ArgumentCount));

            IEnumerable<short> argumentVariables = arguments.Where(a => a.Type == ScriptArgumentType.Variable).Select(a => (short)(int)a.Value);
            IEnumerable<short> instructionVariables = instructions.Select(i => i.ReturnParameter);

            HashSet<short> localVariables = argumentVariables.Concat(instructionVariables).Where(v => v is >= 1001 and < 2000).ToHashSet();
            if (localVariables.Count <= 0)
                return 0;

            return (short)(localVariables.Max() - 1000);
        }

        private short CalculateObjectVariableCount(ScriptFile script, ScriptFunction currentFunction)
        {
            ScriptInstruction[] instructions = script.Instructions.Skip(currentFunction.InstructionIndex).Take(currentFunction.InstructionCount).ToArray();
            IEnumerable<ScriptArgument> arguments = instructions.SelectMany(x => script.Arguments.Skip(x.ArgumentIndex).Take(x.ArgumentCount));

            IEnumerable<short> argumentVariables = arguments.Where(a => a.Type == ScriptArgumentType.Variable).Select(a => (short)(int)a.Value);
            IEnumerable<short> instructionVariables = instructions.Select(i => i.ReturnParameter);

            HashSet<short> objectVariables = argumentVariables.Concat(instructionVariables).Where(v => v is >= 2000 and < 3000).ToHashSet();
            if (objectVariables.Count <= 0)
                return 0;

            return (short)(objectVariables.Max() - 1999);
        }

        private Stream WriteFunctions(ScriptFile script, IBinaryWriterX stringWriter, IDictionary<string, long> writtenNames, IDictionary<string, uint> hashedNames)
        {
            Stream functionStream = new MemoryStream();
            using IBinaryWriterX functionWriter = _binaryFactory.CreateWriter(functionStream, true);

            foreach ((ScriptFunction function, uint nameHash) in script.Functions.Select(f => (f, _checksum.ComputeValue(f.Name))).OrderBy(x => x.Item2))
            {
                long nameOffset = WriteString(function.Name, stringWriter, writtenNames);
                hashedNames[function.Name] = nameHash;

                var nativeFunction = new Xq32Function
                {
                    nameOffset = (int)nameOffset,
                    crc32 = nameHash,

                    instructionOffset = function.InstructionIndex,
                    instructionEndOffset = (short)(function.InstructionIndex + function.InstructionCount),

                    jumpOffset = function.JumpIndex,
                    jumpCount = function.JumpCount,

                    parameterCount = function.ParameterCount,

                    localCount = function.LocalCount < 0 ? CalculateLocalVariableCount(script, function) : function.LocalCount,
                    objectCount = function.ObjectCount < 0 ? CalculateObjectVariableCount(script, function) : function.ObjectCount
                };

                _typeWriter.Write(nativeFunction, functionWriter);
            }

            functionStream.Position = 0;
            return functionStream;
        }

        private Stream WriteJumps(ScriptFile script, IBinaryWriterX stringWriter, IDictionary<string, long> writtenNames, IDictionary<string, uint> hashedNames)
        {
            Stream jumpStream = new MemoryStream();
            using IBinaryWriterX jumpWriter = _binaryFactory.CreateWriter(jumpStream, true);

            // HINT: Here we go through functions sequentially, not sorted by hash
            foreach (ScriptFunction function in script.Functions)
            {
                IEnumerable<ScriptJump> jumps = script.Jumps.Skip(function.JumpIndex).Take(function.JumpCount);
                foreach ((ScriptJump jump, uint nameHash) in jumps.Select(f => (f, _checksum.ComputeValue(f.Name))).OrderBy(x => x.Item2))
                {
                    long nameOffset = WriteString(jump.Name, stringWriter, writtenNames);
                    hashedNames[jump.Name] = nameHash;

                    var nativeJump = new Xq32Jump
                    {
                        nameOffset = (int)nameOffset,
                        crc32 = nameHash,

                        instructionIndex = jump.InstructionIndex
                    };

                    _typeWriter.Write(nativeJump, jumpWriter);
                }
            }

            jumpStream.Position = 0;
            return jumpStream;
        }

        private Stream WriteInstructions(IList<ScriptInstruction> instructions)
        {
            Stream instructionStream = new MemoryStream();
            using IBinaryWriterX instructionWriter = _binaryFactory.CreateWriter(instructionStream, true);

            foreach (ScriptInstruction instruction in instructions)
            {
                var nativeInstruction = new Xq32Instruction
                {
                    argOffset = instruction.ArgumentIndex,
                    argCount = instruction.ArgumentCount,

                    returnParameter = instruction.ReturnParameter,
                    instructionType = instruction.Type
                };

                _typeWriter.Write(nativeInstruction, instructionWriter);
            }

            instructionStream.Position = 0;
            return instructionStream;
        }

        private Stream WriteArguments(IList<ScriptArgument> arguments, IBinaryWriterX stringWriter, IDictionary<string, long> writtenNames, IDictionary<string, uint> hashedNames)
        {
            Stream argumentStream = new MemoryStream();
            using IBinaryWriterX argumentWriter = _binaryFactory.CreateWriter(argumentStream, true);

            foreach (ScriptArgument argument in arguments)
            {
                Xq32Argument nativeArgument = CreateArgument(argument, stringWriter, writtenNames, hashedNames);

                _typeWriter.Write(nativeArgument, argumentWriter);
            }

            argumentStream.Position = 0;
            return argumentStream;
        }

        private Xq32Argument CreateArgument(ScriptArgument argument, IBinaryWriterX stringWriter, IDictionary<string, long> writtenNames, IDictionary<string, uint> hashedNames)
        {
            int type;
            uint value;

            switch (argument.Type)
            {
                case ScriptArgumentType.Int:
                    type = 1;
                    value = unchecked((uint)(int)argument.Value);
                    break;

                case ScriptArgumentType.StringHash:
                    type = 2;
                    if (argument.Value is string stringValue)
                    {
                        if (!hashedNames.TryGetValue(stringValue, out uint hash))
                            hash = _checksum.ComputeValue(stringValue);

                        value = hash;
                    }
                    else
                        value = (uint)argument.Value;

                    break;

                case ScriptArgumentType.Float:
                    type = 3;
                    value = BitConverter.SingleToUInt32Bits((float)argument.Value);
                    break;

                case ScriptArgumentType.Variable:
                    type = 4;
                    value = unchecked((uint)(int)argument.Value);
                    break;

                case ScriptArgumentType.String:
                    long nameOffset = WriteString((string)argument.Value, stringWriter, writtenNames);

                    type = argument.RawArgumentType <= 0 ? 24 : argument.RawArgumentType;
                    value = (uint)nameOffset;
                    break;

                default:
                    throw new InvalidOperationException($"Unknown argument type {argument.Type}.");
            }

            return new Xq32Argument
            {
                type = type,
                value = value
            };
        }

        private long WriteString(string value, IBinaryWriterX stringWriter, IDictionary<string, long> writtenNames)
        {
            if (writtenNames.TryGetValue(value, out long nameOffset))
                return nameOffset;

            CacheStrings(value, stringWriter, writtenNames);

            nameOffset = stringWriter.BaseStream.Position;
            stringWriter.WriteString(value, _sjisEncoding, false);

            return nameOffset;
        }

        private void CacheStrings(string value, IBinaryWriterX stringWriter, IDictionary<string, long> writtenNames)
        {
            long nameOffset = stringWriter.BaseStream.Position;

            do
            {
                if (!writtenNames.ContainsKey(value))
                    writtenNames[value] = nameOffset;

                nameOffset += _sjisEncoding.GetByteCount(value[..1]);
                value = value.Length > 1 ? value[1..] : string.Empty;
            } while (value.Length > 0);

            if (!writtenNames.ContainsKey(value))
                writtenNames[value] = nameOffset;
        }
    }
}
