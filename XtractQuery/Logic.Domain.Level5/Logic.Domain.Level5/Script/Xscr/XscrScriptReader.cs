using Logic.Domain.Kuriimu2.KomponentAdapter.Contract;
using Logic.Domain.Level5.Contract.Script.DataClasses;
using Logic.Domain.Level5.Contract.Script.Xscr;
using Logic.Domain.Level5.Contract.Script.Xscr.DataClasses;

namespace Logic.Domain.Level5.Script.Xscr;

class XscrScriptReader(IBinaryFactory binaryFactory, IXscrScriptDecompressor decompressor) : IXscrScriptReader
{
    public XscrScriptContainer Read(Stream input)
    {
        XscrCompressionContainer compressionContainer = decompressor.Decompress(input);

        return Read(compressionContainer);
    }

    public XscrScriptContainer Read(XscrCompressionContainer container)
    {
        XscrInstruction[] instructions = ReadInstructions(container.InstructionTable);
        XscrArgument[] arguments = ReadArguments(container.ArgumentTable);

        return new XscrScriptContainer
        {
            Instructions = instructions,
            Arguments = arguments,
            Strings = container.StringTable
        };
    }

    private XscrInstruction[] ReadInstructions(CompressedScriptTable table)
    {
        using IBinaryReaderX reader = binaryFactory.CreateReader(table.Stream, true);

        var result = new XscrInstruction[table.EntryCount];

        for (var i = 0; i < result.Length; i++)
            result[i] = ReadInstruction(reader);

        return result;
    }

    private static XscrInstruction ReadInstruction(IBinaryReaderX reader)
    {
        return new XscrInstruction
        {
            instructionType = reader.ReadInt16(),
            argCount = reader.ReadInt16(),
            argOffset = reader.ReadInt16(),
            zero = reader.ReadInt16()
        };
    }

    private XscrArgument[] ReadArguments(CompressedScriptTable table)
    {
        using IBinaryReaderX reader = binaryFactory.CreateReader(table.Stream, true);

        var result = new XscrArgument[table.EntryCount];

        for (var i = 0; i < result.Length; i++)
            result[i] = ReadArgument(reader);

        return result;
    }

    private static XscrArgument ReadArgument(IBinaryReaderX reader)
    {
        return new XscrArgument
        {
            type = reader.ReadInt32(),
            value = reader.ReadUInt32()
        };
    }
}