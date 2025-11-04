using Komponent.IO;
using Logic.Domain.Level5.Contract.DataClasses.Script;
using Logic.Domain.Level5.Contract.DataClasses.Script.Gsd1;
using Logic.Domain.Level5.Contract.Script.Gsd1;
using System.Text;

namespace Logic.Domain.Level5.Script.Gsd1;

class Gsd1ScriptComposer : IGsd1ScriptComposer
{
    private static readonly Encoding SjisEncoding = Encoding.GetEncoding("Shift-JIS");

    public Gsd1ScriptContainer Compose(Gsd1ScriptFile script)
    {
        Stream stringStream = new MemoryStream();
        using var stringWriter = new BinaryWriterX(stringStream, true);

        var writtenNames = new Dictionary<string, long>();
        int stringOffset = CalculateStringOffset(script);

        Gsd1Instruction[] instructions = ComposeInstructions(script);
        Gsd1Argument[] arguments = ComposeArguments(script, stringWriter, ref stringOffset, writtenNames);

        return new Gsd1ScriptContainer
        {
            Instructions = instructions,
            Arguments = arguments,
            Strings = new ScriptStringTable
            {
                Stream = stringStream,
                BaseOffset = 0
            }
        };
    }

    private int CalculateStringOffset(Gsd1ScriptFile script)
    {
        int instructionSize = (script.Instructions.Count * 0x6 + 3) &~3;
        int argumentSize = (script.Arguments.Count * 0x5 + 3) & ~3;

        return instructionSize + argumentSize + 0x10;
    }

    private Gsd1Instruction[] ComposeInstructions(Gsd1ScriptFile script)
    {
        var result = new List<Gsd1Instruction>(script.Instructions.Count);

        foreach (Gsd1ScriptInstruction instruction in script.Instructions)
        {
            result.Add(new Gsd1Instruction
            {
                instructionType = instruction.Type,
                argOffset = instruction.ArgumentIndex,
                argCount = instruction.ArgumentCount
            });
        }

        return [.. result];
    }

    private Gsd1Argument[] ComposeArguments(Gsd1ScriptFile script, BinaryWriterX stringWriter, ref int stringOffset, IDictionary<string, long> writtenNames)
    {
        var result = new List<Gsd1Argument>(script.Arguments.Count);

        foreach (Gsd1ScriptArgument argument in script.Arguments)
        {
            Gsd1Argument nativeArgument = CreateArgument(argument, stringWriter, ref stringOffset, writtenNames);

            result.Add(nativeArgument);
        }

        return [.. result];
    }

    private Gsd1Argument CreateArgument(Gsd1ScriptArgument argument, BinaryWriterX stringWriter, ref int stringOffset, IDictionary<string, long> writtenNames)
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

        return new Gsd1Argument
        {
            type = type,
            value = value
        };
    }

    private long WriteString(string value, BinaryWriterX stringWriter, ref int stringOffset, IDictionary<string, long> writtenNames)
    {
        if (writtenNames.TryGetValue(value, out long nameOffset))
            return nameOffset;

        CacheStrings(value, stringOffset, writtenNames);

        nameOffset = stringWriter.BaseStream.Position;
        stringWriter.WriteString(value, SjisEncoding);

        var nameLength = (int)(stringWriter.BaseStream.Position - nameOffset);
        stringOffset += nameLength;

        return stringOffset - nameLength;
    }

    private void CacheStrings(string value, int stringOffset, IDictionary<string, long> writtenNames)
    {
        do
        {
            writtenNames.TryAdd(value, stringOffset);

            stringOffset += SjisEncoding.GetByteCount(value[..1]);
            value = value.Length > 1 ? value[1..] : string.Empty;
        } while (value.Length > 0);

        writtenNames.TryAdd(value, stringOffset);
    }
}