using System.Text;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract;
using Logic.Domain.Kuriimu2.KryptographyAdapter.Contract;
using Logic.Domain.Level5.Contract.Compression.DataClasses;
using Logic.Domain.Level5.Contract.Script.DataClasses;
using Logic.Domain.Level5.Contract.Script.Xseq;
using Logic.Domain.Level5.Contract.Script.Xseq.DataClasses;
using Logic.Domain.Level5.Cryptography.InternalContract;

namespace Logic.Domain.Level5.Script.Xseq;

internal class XseqScriptWriter : IXseqScriptWriter
{
    private readonly IXseqScriptCompressor _compressor;
    private readonly IBinaryFactory _binaryFactory;
    private readonly IChecksum<ushort> _checksum;
    private readonly Encoding _sjisEncoding;

    public XseqScriptWriter(IXseqScriptCompressor compressor, IBinaryFactory binaryFactory, IChecksumFactory checksumFactory)
    {
        _compressor = compressor;
        _binaryFactory = binaryFactory;
        _checksum = checksumFactory.CreateCrc16();

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _sjisEncoding = Encoding.GetEncoding("Shift-JIS");
    }

    public void Write(ScriptFile script, Stream output, bool hasCompression)
    {
        ScriptContainer container = CreateContainer(script);

        Write(container, output, hasCompression);
    }

    public void Write(ScriptFile script, Stream output, CompressionType compressionType)
    {
        ScriptContainer container = CreateContainer(script);

        Write(container, output, compressionType);
    }

    public void Write(ScriptContainer container, Stream output, bool hasCompression)
    {
        _compressor.Compress(container, output, hasCompression);
    }

    public void Write(ScriptContainer container, Stream output, CompressionType compressionType)
    {
        _compressor.Compress(container, output, compressionType);
    }

    public void WriteFunctions(IReadOnlyList<XseqFunction> functions, Stream output, PointerLength length)
    {
        using IBinaryWriterX bw = _binaryFactory.CreateWriter(output, false);

        foreach (XseqFunction function in functions)
            WriteFunction(function, bw, length);
    }

    public void WriteJumps(IReadOnlyList<XseqJump> jumps, Stream output, PointerLength length)
    {
        using IBinaryWriterX bw = _binaryFactory.CreateWriter(output, false);

        foreach (XseqJump jump in jumps)
            WriteJump(jump, bw, length);
    }

    public void WriteInstructions(IReadOnlyList<XseqInstruction> instructions, Stream output, PointerLength length)
    {
        using IBinaryWriterX bw = _binaryFactory.CreateWriter(output, false);

        foreach (XseqInstruction instruction in instructions)
            WriteInstruction(instruction, bw, length);
    }

    public void WriteArguments(IReadOnlyList<XseqArgument> arguments, Stream output, PointerLength length)
    {
        using IBinaryWriterX bw = _binaryFactory.CreateWriter(output, false);

        foreach (XseqArgument argument in arguments)
            WriteArgument(argument, bw, length);
    }

    private ScriptContainer CreateContainer(ScriptFile script)
    {
        Stream stringStream = new MemoryStream();
        using IBinaryWriterX stringWriter = _binaryFactory.CreateWriter(stringStream, true);

        var writtenNames = new Dictionary<string, long>();

        Stream functionStream = WriteFunctions(script, stringWriter, writtenNames);
        Stream jumpStream = WriteJumps(script, stringWriter, writtenNames);
        Stream instructionStream = WriteInstructions(script);
        Stream argumentStream = WriteArguments(script, stringWriter, writtenNames);

        stringStream.Position = 0;
        return new ScriptContainer
        {
            GlobalVariableCount = CalculateGlobalVariableCount(script.Instructions, script.Arguments),

            FunctionTable = new CompressedScriptTable { Stream = functionStream, EntryCount = script.Functions.Count },
            JumpTable = new CompressedScriptTable { Stream = jumpStream, EntryCount = script.Jumps.Count },
            InstructionTable = new CompressedScriptTable { Stream = instructionStream, EntryCount = script.Instructions.Count },
            ArgumentTable = new CompressedScriptTable { Stream = argumentStream, EntryCount = script.Arguments.Count },
            StringTable = new CompressedScriptStringTable { Stream = stringStream, BaseOffset = 0 }
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

    private Stream WriteFunctions(ScriptFile script, IBinaryWriterX stringWriter, IDictionary<string, long> writtenNames)
    {
        Stream functionStream = new MemoryStream();
        using IBinaryWriterX functionWriter = _binaryFactory.CreateWriter(functionStream, true);

        foreach ((ScriptFunction function, ushort nameHash) in script.Functions.Select(f => (f, _checksum.ComputeValue(f.Name))).OrderBy(x => x.Item2))
        {
            long nameOffset = WriteString(function.Name, stringWriter, writtenNames);

            var nativeFunction = new XseqFunction
            {
                nameOffset = (int)nameOffset,
                crc16 = nameHash,

                instructionOffset = function.InstructionIndex,
                instructionEndOffset = (short)(function.InstructionIndex + function.InstructionCount),

                jumpOffset = function.JumpIndex,
                jumpCount = function.JumpCount,

                parameterCount = (short)function.ParameterCount,

                localCount = function.LocalCount < 0 ? CalculateLocalVariableCount(script, function) : function.LocalCount,
                objectCount = function.ObjectCount < 0 ? CalculateObjectVariableCount(script, function) : function.ObjectCount
            };

            WriteFunction(nativeFunction, functionWriter, script.Length);
        }

        functionStream.Position = 0;
        return functionStream;
    }

    private Stream WriteJumps(ScriptFile script, IBinaryWriterX stringWriter, IDictionary<string, long> writtenNames)
    {
        Stream jumpStream = new MemoryStream();
        using IBinaryWriterX jumpWriter = _binaryFactory.CreateWriter(jumpStream, true);

        // HINT: Here we go through functions sequentially, not sorted by hash
        foreach (ScriptFunction function in script.Functions)
        {
            IEnumerable<ScriptJump> jumps = script.Jumps.Skip(function.JumpIndex).Take(function.JumpCount);
            foreach ((ScriptJump jump, ushort nameHash) in jumps.Select(f => (f, _checksum.ComputeValue(f.Name))).OrderBy(x => x.Item2))
            {
                long nameOffset = WriteString(jump.Name, stringWriter, writtenNames);

                var nativeJump = new XseqJump
                {
                    nameOffset = (int)nameOffset,
                    crc16 = nameHash,

                    instructionIndex = (short)jump.InstructionIndex
                };

                WriteJump(nativeJump, jumpWriter, script.Length);
            }
        }

        jumpStream.Position = 0;
        return jumpStream;
    }

    private Stream WriteInstructions(ScriptFile script)
    {
        Stream instructionStream = new MemoryStream();
        using IBinaryWriterX instructionWriter = _binaryFactory.CreateWriter(instructionStream, true);

        foreach (ScriptInstruction instruction in script.Instructions)
        {
            var nativeInstruction = new XseqInstruction
            {
                argOffset = instruction.ArgumentIndex,
                argCount = instruction.ArgumentCount,

                returnParameter = instruction.ReturnParameter,
                instructionType = instruction.Type
            };

            WriteInstruction(nativeInstruction, instructionWriter, script.Length);
        }

        instructionStream.Position = 0;
        return instructionStream;
    }

    private Stream WriteArguments(ScriptFile script, IBinaryWriterX stringWriter, IDictionary<string, long> writtenNames)
    {
        Stream argumentStream = new MemoryStream();
        using IBinaryWriterX argumentWriter = _binaryFactory.CreateWriter(argumentStream, true);

        foreach (ScriptArgument argument in script.Arguments)
        {
            XseqArgument nativeArgument = CreateArgument(argument, stringWriter, writtenNames);

            WriteArgument(nativeArgument, argumentWriter, script.Length);
        }

        argumentStream.Position = 0;
        return argumentStream;
    }

    private XseqArgument CreateArgument(ScriptArgument argument, IBinaryWriterX stringWriter,
        IDictionary<string, long> writtenNames)
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
                    value = _checksum.ComputeValue(stringValue);
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

        return new XseqArgument
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

    private void WriteFunction(XseqFunction function, IBinaryWriterX bw, PointerLength length)
    {
        switch (length)
        {
            case PointerLength.Int:
                bw.Write((int)function.nameOffset);
                break;

            case PointerLength.Long:
                bw.Write(function.nameOffset);
                break;

            default:
                throw new InvalidOperationException($"Unknown pointer length {length}.");
        }

        bw.Write(function.crc16);
        bw.Write(function.instructionOffset);
        bw.Write(function.instructionEndOffset);
        bw.Write(function.jumpOffset);
        bw.Write(function.jumpCount);
        bw.Write(function.localCount);
        bw.Write(function.objectCount);
        bw.Write(function.parameterCount);
    }

    private void WriteJump(XseqJump jump, IBinaryWriterX bw, PointerLength length)
    {
        switch (length)
        {
            case PointerLength.Int:
                bw.Write((int)jump.nameOffset);
                bw.Write(jump.crc16);
                bw.Write(jump.instructionIndex);
                break;

            case PointerLength.Long:
                bw.Write(jump.nameOffset);
                bw.Write(jump.crc16);
                bw.Write(jump.instructionIndex);
                bw.WritePadding(4);
                break;

            default:
                throw new InvalidOperationException($"Unknown pointer length {length}.");
        }
    }

    private void WriteInstruction(XseqInstruction instruction, IBinaryWriterX bw, PointerLength length)
    {
        bw.Write(instruction.argOffset);
        bw.Write(instruction.argCount);
        bw.Write(instruction.returnParameter);
        bw.Write(instruction.instructionType);

        switch (length)
        {
            case PointerLength.Int:
                bw.WritePadding(4);
                break;

            case PointerLength.Long:
                bw.WritePadding(8);
                break;

            default:
                throw new InvalidOperationException($"Unknown pointer length {length}.");
        }
    }

    private void WriteArgument(XseqArgument argument, IBinaryWriterX bw, PointerLength length)
    {
        switch (length)
        {
            case PointerLength.Int:
                bw.Write(argument.type);
                bw.Write(argument.value);
                break;

            case PointerLength.Long:
                bw.Write(argument.type);
                bw.BaseStream.Position += 4;
                bw.Write(argument.value);
                bw.WritePadding(4);
                break;

            default:
                throw new InvalidOperationException($"Unknown pointer length {length}.");
        }
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