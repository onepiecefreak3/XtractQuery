using System.Text;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract;
using Logic.Domain.Kuriimu2.KryptographyAdapter.Contract;
using Logic.Domain.Level5.Contract.Script.DataClasses;
using Logic.Domain.Level5.Contract.Script.Gss1;
using Logic.Domain.Level5.Contract.Script.Gss1.DataClasses;
using Logic.Domain.Level5.Cryptography.InternalContract;

namespace Logic.Domain.Level5.Script.Gss1;

class Gss1ScriptComposer(IBinaryFactory binaryFactory, IChecksumFactory checksumFactory) : IGss1ScriptComposer
{
    private readonly IChecksum<ushort> _checksum = checksumFactory.CreateCrc16();
    private readonly Encoding _sjisEncoding = Encoding.GetEncoding("Shift-JIS");

    public Gss1ScriptContainer Compose(Gss1ScriptFile script)
    {
        Stream stringStream = new MemoryStream();
        using IBinaryWriterX stringWriter = binaryFactory.CreateWriter(stringStream, true);

        var writtenNames = new Dictionary<string, long>();
        int stringOffset = CalculateStringOffset(script);

        Gss1Function[] functions = ComposeFunctions(script, stringWriter, ref stringOffset, writtenNames);
        Gss1Jump[] jumps = ComposeJumps(script, stringWriter, ref stringOffset, writtenNames);
        Gss1Instruction[] instructions = ComposeInstructions(script);
        Gss1Argument[] arguments = ComposeArguments(script, stringWriter, ref stringOffset, writtenNames);

        return new Gss1ScriptContainer
        {
            Functions = functions,
            Jumps = jumps,
            Instructions = instructions,
            Arguments = arguments,
            Strings = new ScriptStringTable
            {
                Stream = stringStream,
                BaseOffset = 0
            },
            GlobalVariableCount = CalculateGlobalVariableCount(script.Instructions, script.Arguments)
        };
    }

    private int CalculateStringOffset(Gss1ScriptFile script)
    {
        int functionSize = script.Functions.Count * 0x14;
        int jumpSize = script.Jumps.Count * 0x8;
        int instructionSize = script.Instructions.Count * 0xC;
        int argumentSize = (script.Arguments.Count * 0x5 + 3) & ~3;

        return functionSize + jumpSize + instructionSize + argumentSize + 0x20;
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

    private short CalculateLocalVariableCount(Gss1ScriptFile script, ScriptFunction currentFunction)
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

    private short CalculateObjectVariableCount(Gss1ScriptFile script, ScriptFunction currentFunction)
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

    private Gss1Function[] ComposeFunctions(Gss1ScriptFile script, IBinaryWriterX stringWriter, ref int stringOffset, IDictionary<string, long> writtenNames)
    {
        var result = new List<Gss1Function>(script.Functions.Count);

        foreach ((ScriptFunction function, ushort nameHash) in script.Functions.Select(f => (f, _checksum.ComputeValue(f.Name))).OrderBy(x => x.Item2))
        {
            long nameOffset = WriteString(function.Name, stringWriter, ref stringOffset, writtenNames);

            result.Add(new Gss1Function
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
            });
        }

        return [.. result];
    }

    private Gss1Jump[] ComposeJumps(Gss1ScriptFile script, IBinaryWriterX stringWriter, ref int stringOffset, IDictionary<string, long> writtenNames)
    {
        var result = new List<Gss1Jump>(script.Jumps.Count);

        // HINT: Here we go through functions sequentially, not sorted by hash
        foreach (ScriptFunction function in script.Functions)
        {
            IEnumerable<ScriptJump> jumps = script.Jumps.Skip(function.JumpIndex).Take(function.JumpCount);
            foreach ((ScriptJump jump, ushort nameHash) in jumps.Select(f => (f, _checksum.ComputeValue(f.Name))).OrderBy(x => x.Item2))
            {
                long nameOffset = WriteString(jump.Name, stringWriter, ref stringOffset, writtenNames);

                result.Add(new Gss1Jump
                {
                    nameOffset = (int)nameOffset,
                    crc16 = nameHash,

                    instructionIndex = (short)jump.InstructionIndex
                });
            }
        }

        return [.. result];
    }

    private Gss1Instruction[] ComposeInstructions(Gss1ScriptFile script)
    {
        var result = new List<Gss1Instruction>(script.Instructions.Count);

        foreach (ScriptInstruction instruction in script.Instructions)
        {
            result.Add(new Gss1Instruction
            {
                argOffset = instruction.ArgumentIndex,
                argCount = instruction.ArgumentCount,

                returnParameter = instruction.ReturnParameter,
                instructionType = instruction.Type
            });
        }

        return [.. result];
    }

    private Gss1Argument[] ComposeArguments(Gss1ScriptFile script, IBinaryWriterX stringWriter, ref int stringOffset, IDictionary<string, long> writtenNames)
    {
        var result = new List<Gss1Argument>(script.Arguments.Count);

        foreach (ScriptArgument argument in script.Arguments)
        {
            Gss1Argument nativeArgument = CreateArgument(argument, stringWriter, ref stringOffset, writtenNames);

            result.Add(nativeArgument);
        }

        return [.. result];
    }

    private Gss1Argument CreateArgument(ScriptArgument argument, IBinaryWriterX stringWriter, ref int stringOffset, IDictionary<string, long> writtenNames)
    {
        byte type;
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
                long nameOffset = WriteString((string)argument.Value, stringWriter, ref stringOffset, writtenNames);

                type = (byte)(argument.RawArgumentType <= 0 ? 24 : argument.RawArgumentType);
                value = (uint)nameOffset;
                break;

            default:
                throw new InvalidOperationException($"Unknown argument type {argument.Type}.");
        }

        return new Gss1Argument
        {
            type = type,
            value = value
        };
    }

    private long WriteString(string value, IBinaryWriterX stringWriter, ref int stringOffset, IDictionary<string, long> writtenNames)
    {
        if (writtenNames.TryGetValue(value, out long nameOffset))
            return nameOffset;

        CacheStrings(value, stringOffset, writtenNames);

        nameOffset = stringWriter.BaseStream.Position;
        stringWriter.WriteString(value, _sjisEncoding, false);

        var nameLength = (int)(stringWriter.BaseStream.Position - nameOffset);
        stringOffset += nameLength;

        return stringOffset - nameLength;
    }

    private void CacheStrings(string value, int stringOffset, IDictionary<string, long> writtenNames)
    {
        do
        {
            writtenNames.TryAdd(value, stringOffset);

            stringOffset += _sjisEncoding.GetByteCount(value[..1]);
            value = value.Length > 1 ? value[1..] : string.Empty;
        } while (value.Length > 0);

        writtenNames.TryAdd(value, stringOffset);
    }
}