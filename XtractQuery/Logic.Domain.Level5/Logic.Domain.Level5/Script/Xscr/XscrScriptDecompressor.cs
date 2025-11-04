using Komponent.IO;
using Logic.Domain.Level5.Contract.DataClasses.Script;
using Logic.Domain.Level5.Contract.DataClasses.Script.Xscr;
using Logic.Domain.Level5.Contract.Enums.Compression;
using Logic.Domain.Level5.Contract.Script.Xscr;
using Logic.Domain.Level5.DataClasses.Script;
using Logic.Domain.Level5.InternalContract.Compression;

namespace Logic.Domain.Level5.Script.Xscr;

internal class XscrScriptDecompressor(IDecompressor decompressor) : IXscrScriptDecompressor
{
    public XscrCompressionContainer Decompress(Stream input)
    {
        using var reader = new BinaryReaderX(input, true);

        XscrHeader header = ReadHeader(reader);

        TableData instructionTable = GetInstructionTableData(header);
        TableData argumentTable = GetArgumentTableData(header);
        int stringOffset = GetStringTableOffset(header);

        return new XscrCompressionContainer
        {
            InstructionTable = ReadTable(input, instructionTable),
            ArgumentTable = ReadTable(input, argumentTable),
            StringTable = ReadStringTable(input, stringOffset)
        };
    }

    private XscrHeader ReadHeader(BinaryReaderX reader)
    {
        return new XscrHeader
        {
            magic = reader.ReadString(4),
            instructionEntryCount = reader.ReadInt16(),
            instructionOffset = reader.ReadUInt16(),
            argumentEntryCount = reader.ReadInt32(),
            argumentOffset = reader.ReadInt32(),
            stringOffset = reader.ReadInt32()
        };
    }

    private TableData GetInstructionTableData(XscrHeader header)
    {
        return new TableData
        {
            offset = header.instructionOffset << 2,
            count = header.instructionEntryCount
        };
    }

    private TableData GetArgumentTableData(XscrHeader header)
    {
        return new TableData
        {
            offset = header.argumentOffset << 2,
            count = header.argumentEntryCount
        };
    }

    private int GetStringTableOffset(XscrHeader header)
    {
        return header.stringOffset << 2;
    }

    private CompressedScriptTable ReadTable(Stream input, TableData tableData)
    {
        return DecompressTable(input, tableData);
    }

    private CompressedScriptTable DecompressTable(Stream input, TableData tableData)
    {
        Stream decompressedStream = Decompress(input, tableData.offset, out CompressionType compressionType);

        return new CompressedScriptTable
        {
            EntryCount = tableData.count,
            CompressionType = compressionType,
            Stream = decompressedStream
        };
    }

    private CompressedScriptStringTable ReadStringTable(Stream input, int offset)
    {
        return DecompressStringTable(input, offset);
    }

    private CompressedScriptStringTable DecompressStringTable(Stream input, int offset)
    {
        Stream decompressedStream = Decompress(input, offset, out CompressionType compressionType);

        return new CompressedScriptStringTable
        {
            CompressionType = compressionType,
            Stream = decompressedStream,
            BaseOffset = 0
        };
    }

    private Stream Decompress(Stream input, int offset, out CompressionType compressionType)
    {
        compressionType = decompressor.PeekCompressionType(input, offset);
        return decompressor.Decompress(input, offset);
    }
}