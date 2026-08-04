using Komponent.Streams;
using Logic.Domain.Level5.Contract.DataClasses.Script;
using Logic.Domain.Level5.Contract.Script;
using Logic.Domain.Level5.DataClasses.Script;
using Logic.Domain.Level5.InternalContract.Compression;
using CompressionType = Logic.Domain.Level5.Contract.Enums.Compression.CompressionType;

namespace Logic.Domain.Level5.Script;

internal abstract class ScriptDecompressor<THeader>(
    IDecompressor decompressor,
    IScriptEntrySizeProvider entrySizeProvider)
    : IScriptDecompressor
{
    public ScriptContainer Decompress(Stream input)
    {
        THeader header = ReadHeader(input);

        TableData functionTable = GetFunctionTableData(header);
        TableData jumpTable = GetJumpTableData(header);
        TableData instructionTable = GetInstructionTableData(header);
        TableData argumentTable = GetArgumentTableData(header);
        int stringOffset = GetStringTableOffset(header);

        bool hasCompression = HasCompression(functionTable, jumpTable, instructionTable, argumentTable, stringOffset);

        return new ScriptContainer
        {
            GlobalVariableCount = GetGlobalVariableCount(header),

            FunctionTable = ReadTable(input, functionTable, jumpTable.offset, hasCompression),
            JumpTable = ReadTable(input, jumpTable, instructionTable.offset, hasCompression),
            InstructionTable = ReadTable(input, instructionTable, argumentTable.offset, hasCompression),
            ArgumentTable = ReadTable(input, argumentTable, stringOffset, hasCompression),
            StringTable = ReadStringTable(input, stringOffset, hasCompression)
        };
    }

    public int GetGlobalVariableCount(Stream input)
    {
        THeader header = ReadHeader(input);

        return GetGlobalVariableCount(header);
    }

    public CompressedScriptTable DecompressFunctions(Stream input)
    {
        THeader header = ReadHeader(input);

        TableData functionTable = GetFunctionTableData(header);
        TableData jumpTable = GetJumpTableData(header);
        bool hasCompression = HasCompression(header);

        return ReadTable(input, functionTable, jumpTable.offset, hasCompression);
    }

    public CompressedScriptTable DecompressJumps(Stream input)
    {
        THeader header = ReadHeader(input);
            
        TableData jumpTable = GetJumpTableData(header);
        TableData instructionTable = GetInstructionTableData(header);
        bool hasCompression = HasCompression(header);

        return ReadTable(input, jumpTable, instructionTable.offset, hasCompression);
    }

    public CompressedScriptTable DecompressInstructions(Stream input)
    {
        THeader header = ReadHeader(input);

        TableData instructionTable = GetInstructionTableData(header);
        TableData argumentTable = GetArgumentTableData(header);
        bool hasCompression = HasCompression(header);

        return ReadTable(input, instructionTable, argumentTable.offset, hasCompression);
    }

    public CompressedScriptTable DecompressArguments(Stream input)
    {
        THeader header = ReadHeader(input);

        TableData argumentTable = GetArgumentTableData(header);
        int stringOffset = GetStringTableOffset(header);
        bool hasCompression = HasCompression(header);

        return ReadTable(input, argumentTable, stringOffset, hasCompression);
    }

    public CompressedScriptStringTable DecompressStrings(Stream input)
    {
        THeader header = ReadHeader(input);

        int stringOffset = GetStringTableOffset(header);
        bool hasCompression = HasCompression(header);

        return ReadStringTable(input, stringOffset, hasCompression);
    }

    protected abstract int GetGlobalVariableCount(THeader header);

    protected abstract TableData GetFunctionTableData(THeader header);

    protected abstract TableData GetJumpTableData(THeader header);

    protected abstract TableData GetInstructionTableData(THeader header);

    protected abstract TableData GetArgumentTableData(THeader header);

    protected abstract int GetStringTableOffset(THeader header);

    private bool HasCompression(THeader header)
    {
        TableData functionTable = GetFunctionTableData(header);
        TableData jumpTable = GetJumpTableData(header);
        TableData instructionTable = GetInstructionTableData(header);
        TableData argumentTable = GetArgumentTableData(header);
        int stringOffset = GetStringTableOffset(header);

        return HasCompression(functionTable, jumpTable, instructionTable, argumentTable, stringOffset);
    }

    private bool HasCompression(TableData functionTable, TableData jumpTable, TableData instructionTable, TableData argumentTable, int stringOffset)
    {
        for (var i = 0; i < 2; i++)
        {
            int entrySize = entrySizeProvider.GetFunctionEntrySize((PointerLength)i);
            if (functionTable.count * entrySize != jumpTable.offset - functionTable.offset)
                continue;

            entrySize = entrySizeProvider.GetJumpEntrySize((PointerLength)i);
            if (jumpTable.count * entrySize != instructionTable.offset - jumpTable.offset)
                continue;

            entrySize = entrySizeProvider.GetInstructionEntrySize((PointerLength)i);
            if (instructionTable.count * entrySize != argumentTable.offset - instructionTable.offset)
                continue;

            entrySize = entrySizeProvider.GetArgumentEntrySize((PointerLength)i);
            if (argumentTable.count * entrySize != stringOffset - argumentTable.offset)
                continue;

            return false;
        }

        return true;
    }

    private CompressedScriptTable ReadTable(Stream input, TableData tableData, long nextOffset, bool hasCompression)
    {
        if (hasCompression)
            return DecompressTable(input, tableData);

        return new CompressedScriptTable
        {
            EntryCount = tableData.count,
            CompressionType = null,
            Stream = new SubStream(input, tableData.offset, nextOffset - tableData.offset)
        };
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

    private CompressedScriptStringTable ReadStringTable(Stream input, int offset, bool hasCompression)
    {
        if (hasCompression)
            return DecompressStringTable(input, offset);

        return new CompressedScriptStringTable
        {
            CompressionType = null,
            Stream = new SubStream(input, offset, input.Length - offset),
            BaseOffset = 0
        };
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

    protected abstract THeader ReadHeader(Stream input);
}