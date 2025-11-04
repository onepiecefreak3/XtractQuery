using System.Text;
using Komponent.IO;
using Logic.Domain.Level5.Contract.DataClasses.Script;
using Logic.Domain.Level5.Contract.DataClasses.Script.Xscr;
using Logic.Domain.Level5.Contract.Script.Xscr;

namespace Logic.Domain.Level5.Script.Xscr;

class XscrScriptComposer : IXscrScriptComposer
{
    private static readonly Encoding SjisEncoding = Encoding.GetEncoding("Shift-JIS");

    public XscrScriptContainer Compose(XscrScriptFile script)
    {
        Stream stringStream = new MemoryStream();
        using var stringWriter = new BinaryWriterX(stringStream, true);

        var writtenNames = new Dictionary<string, long>();
        var stringOffset = 0;

        XscrInstruction[] instructions = ComposeInstructions(script);
        XscrArgument[] arguments = ComposeArguments(script, stringWriter, ref stringOffset, writtenNames);

        stringStream.Position = 0;
        return new XscrScriptContainer
        {
            Instructions = instructions,
            Arguments = arguments,
            Strings = new CompressedScriptStringTable
            {
                Stream = stringStream,
                BaseOffset = 0
            }
        };
    }

    private XscrInstruction[] ComposeInstructions(XscrScriptFile script)
    {
        var result = new List<XscrInstruction>(script.Instructions.Count);

        foreach (XscrScriptInstruction instruction in script.Instructions)
        {
            result.Add(new XscrInstruction
            {
                instructionType = instruction.Type,
                argOffset = instruction.ArgumentIndex,
                argCount = instruction.ArgumentCount
            });
        }

        return [.. result];
    }

    private XscrArgument[] ComposeArguments(XscrScriptFile script, BinaryWriterX stringWriter, ref int stringOffset, IDictionary<string, long> writtenNames)
    {
        var result = new List<XscrArgument>(script.Arguments.Count);

        foreach (XscrScriptArgument argument in script.Arguments)
        {
            XscrArgument nativeArgument = CreateArgument(argument, stringWriter, ref stringOffset, writtenNames);

            result.Add(nativeArgument);
        }

        return [.. result];
    }

    private XscrArgument CreateArgument(XscrScriptArgument argument, BinaryWriterX stringWriter, ref int stringOffset, IDictionary<string, long> writtenNames)
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

        return new XscrArgument
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