using Logic.Domain.Kuriimu2.KomponentAdapter.Contract;
using Logic.Domain.Level5.Compression.InternalContract;
using Logic.Domain.Level5.Contract.Script;
using Logic.Domain.Level5.Contract.Script.DataClasses;
using Logic.Domain.Level5.Script.InternalContract.DataClasses;
using CompressionType = Logic.Domain.Level5.Contract.Compression.DataClasses.CompressionType;

namespace Logic.Domain.Level5.Script;

internal abstract class ScriptDecompressor<THeader> : IScriptDecompressor
{
    private readonly IBinaryFactory _binaryFactory;
    private readonly IBinaryTypeReader _typeReader;
    private readonly IStreamFactory _streamFactory;
    private readonly IDecompressor _decompressor;
    private readonly IScriptEntrySizeProvider _entrySizeProvider;

    public ScriptDecompressor(IBinaryFactory binaryFactory, IBinaryTypeReader typeReader, IStreamFactory streamFactory, IDecompressor decompressor,
        IScriptEntrySizeProvider entrySizeProvider)
    {
        _binaryFactory = binaryFactory;
        _typeReader = typeReader;
        _streamFactory = streamFactory;
        _decompressor = decompressor;
        _entrySizeProvider = entrySizeProvider;
    }

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

    public ScriptTable DecompressFunctions(Stream input)
    {
        THeader header = ReadHeader(input);

        TableData functionTable = GetFunctionTableData(header);
        TableData jumpTable = GetJumpTableData(header);
        bool hasCompression = HasCompression(header);

        return ReadTable(input, functionTable, jumpTable.offset, hasCompression);
    }

    public ScriptTable DecompressJumps(Stream input)
    {
        THeader header = ReadHeader(input);
            
        TableData jumpTable = GetJumpTableData(header);
        TableData instructionTable = GetInstructionTableData(header);
        bool hasCompression = HasCompression(header);

        return ReadTable(input, jumpTable, instructionTable.offset, hasCompression);
    }

    public ScriptTable DecompressInstructions(Stream input)
    {
        THeader header = ReadHeader(input);

        TableData instructionTable = GetInstructionTableData(header);
        TableData argumentTable = GetArgumentTableData(header);
        bool hasCompression = HasCompression(header);

        return ReadTable(input, instructionTable, argumentTable.offset, hasCompression);
    }

    public ScriptTable DecompressArguments(Stream input)
    {
        THeader header = ReadHeader(input);

        TableData argumentTable = GetArgumentTableData(header);
        int stringOffset = GetStringTableOffset(header);
        bool hasCompression = HasCompression(header);

        return ReadTable(input, argumentTable, stringOffset, hasCompression);
    }

    public ScriptStringTable DecompressStrings(Stream input)
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
            int entrySize = _entrySizeProvider.GetFunctionEntrySize((PointerLength)i);
            if (functionTable.count * entrySize != jumpTable.offset - functionTable.offset)
                continue;

            entrySize = _entrySizeProvider.GetJumpEntrySize((PointerLength)i);
            if (jumpTable.count * entrySize != instructionTable.offset - jumpTable.offset)
                continue;

            entrySize = _entrySizeProvider.GetInstructionEntrySize((PointerLength)i);
            if (instructionTable.count * entrySize != argumentTable.offset - instructionTable.offset)
                continue;

            entrySize = _entrySizeProvider.GetArgumentEntrySize((PointerLength)i);
            if (argumentTable.count * entrySize != stringOffset - argumentTable.offset)
                continue;

            return false;
        }

        return true;
    }

    private ScriptTable ReadTable(Stream input, TableData tableData, long nextOffset, bool hasCompression)
    {
        if (hasCompression)
            return DecompressTable(input, tableData);

        return new ScriptTable
        {
            EntryCount = tableData.count,
            CompressionType = null,
            Stream = _streamFactory.CreateSubStream(input, tableData.offset, nextOffset - tableData.offset)
        };
    }

    private ScriptTable DecompressTable(Stream input, TableData tableData)
    {
        Stream decompressedStream = Decompress(input, tableData.offset, out CompressionType compressionType);

        return new ScriptTable
        {
            EntryCount = tableData.count,
            CompressionType = compressionType,
            Stream = decompressedStream
        };
    }

    private ScriptStringTable ReadStringTable(Stream input, int offset, bool hasCompression)
    {
        if (hasCompression)
            return DecompressStringTable(input, offset);

        return new ScriptStringTable
        {
            CompressionType = null,
            Stream = _streamFactory.CreateSubStream(input, offset, input.Length - offset)
        };
    }

    private ScriptStringTable DecompressStringTable(Stream input, int offset)
    {
        Stream decompressedStream = Decompress(input, offset, out CompressionType compressionType);

        return new ScriptStringTable
        {
            CompressionType = compressionType,
            Stream = decompressedStream
        };
    }

    private Stream Decompress(Stream input, int offset, out CompressionType compressionType)
    {
        compressionType = _decompressor.PeekCompressionType(input, offset);
        return _decompressor.Decompress(input, offset);
    }

    private THeader ReadHeader(Stream input)
    {
        long bkPos = input.Position;
        input.Position = 0;

        using IBinaryReaderX br = _binaryFactory.CreateReader(input, true);

        var header = _typeReader.Read<THeader>(br)!;
        input.Position = bkPos;

        return header;
    }
}