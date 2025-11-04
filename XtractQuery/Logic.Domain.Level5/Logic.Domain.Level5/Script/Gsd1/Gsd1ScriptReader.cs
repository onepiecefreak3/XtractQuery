using Komponent.IO;
using Komponent.Streams;
using Logic.Domain.Level5.Contract.DataClasses.Script;
using Logic.Domain.Level5.Contract.DataClasses.Script.Gsd1;
using Logic.Domain.Level5.Contract.Script.Gsd1;

namespace Logic.Domain.Level5.Script.Gsd1;

class Gsd1ScriptReader : IGsd1ScriptReader
{
    public Gsd1ScriptContainer Read(Stream input)
    {
        using var reader = new BinaryReaderX(input, true);

        Gsd1Header header = ReadHeader(reader);

        input.Position = header.instructionOffset << 2;
        Gsd1Instruction[] instructions = ReadInstructions(reader, header.instructionEntryCount);

        input.Position = header.argumentOffset << 2;
        Gsd1Argument[] arguments = ReadArguments(reader, header.argumentEntryCount);

        int stringOffset = header.stringOffset << 2;
        Stream stringStream = new SubStream(input, stringOffset, input.Length - stringOffset);

        return new Gsd1ScriptContainer
        {
            Instructions = instructions,
            Arguments = arguments,
            Strings = new ScriptStringTable
            {
                Stream = stringStream,
                BaseOffset = stringOffset
            }
        };
    }

    private static Gsd1Header ReadHeader(BinaryReaderX reader)
    {
        return new Gsd1Header
        {
            magic = reader.ReadString(4),
            instructionEntryCount = reader.ReadInt16(),
            instructionOffset = reader.ReadUInt16(),
            argumentEntryCount = reader.ReadInt16(),
            argumentOffset = reader.ReadUInt16(),
            stringOffset = reader.ReadInt32()
        };
    }

    private static Gsd1Instruction[] ReadInstructions(BinaryReaderX reader, int count)
    {
        var result = new Gsd1Instruction[count];

        for (var i = 0; i < result.Length; i++)
            result[i] = ReadInstruction(reader);

        return result;
    }

    private static Gsd1Instruction ReadInstruction(BinaryReaderX reader)
    {
        return new Gsd1Instruction
        {
            instructionType = reader.ReadInt16(),
            argOffset = reader.ReadInt16(),
            argCount = reader.ReadInt16()
        };
    }

    private static Gsd1Argument[] ReadArguments(BinaryReaderX reader, int count)
    {
        var result = new Gsd1Argument[count];

        for (var i = 0; i < result.Length; i++)
            result[i] = ReadArgument(reader);

        return result;
    }

    private static Gsd1Argument ReadArgument(BinaryReaderX reader)
    {
        return new Gsd1Argument
        {
            type = reader.ReadByte(),
            value = reader.ReadUInt32()
        };
    }
}