using Komponent.IO;
using Logic.Domain.Level5.Contract.DataClasses.Script.Xq32;
using Logic.Domain.Level5.Contract.DataClasses.Script.Xseq;
using Logic.Domain.Level5.Contract.Script.Xseq;
using Logic.Domain.Level5.DataClasses.Script;
using Logic.Domain.Level5.InternalContract.Compression;
using Logic.Domain.Level5.InternalContract.Script.Xseq;
using System.Reflection.PortableExecutable;

namespace Logic.Domain.Level5.Script.Xseq;

internal class XseqScriptDecompressor : ScriptDecompressor<XseqHeader>, IXseqScriptDecompressor
{
    public XseqScriptDecompressor(IDecompressor decompressor, IXseqScriptEntrySizeProvider entrySizeProvider)
        : base(decompressor, entrySizeProvider)
    {
    }

    protected override int GetGlobalVariableCount(XseqHeader header)
    {
        return header.globalVariableCount;
    }

    protected override TableData GetFunctionTableData(XseqHeader header)
    {
        return new TableData
        {
            offset = header.functionOffset << 2,
            count = header.functionEntryCount
        };
    }

    protected override TableData GetJumpTableData(XseqHeader header)
    {
        return new TableData
        {
            offset = header.jumpOffset << 2,
            count = header.jumpEntryCount
        };
    }

    protected override TableData GetInstructionTableData(XseqHeader header)
    {
        return new TableData
        {
            offset = header.instructionOffset << 2,
            count = header.instructionEntryCount
        };
    }

    protected override TableData GetArgumentTableData(XseqHeader header)
    {
        return new TableData
        {
            offset = header.argumentOffset << 2,
            count = header.argumentEntryCount
        };
    }

    protected override int GetStringTableOffset(XseqHeader header)
    {
        return header.stringOffset << 2;
    }

    protected override XseqHeader ReadHeader(Stream input)
    {
        var bkPos = input.Position;
        input.Position = 0;

        using var br = new BinaryReaderX(input, true);

        var header = new XseqHeader
        {
            magic = br.ReadString(4),
            functionEntryCount = br.ReadInt16(),
            functionOffset = br.ReadUInt16(),
            jumpOffset = br.ReadUInt16(),
            jumpEntryCount = br.ReadInt16(),
            instructionOffset = br.ReadUInt16(),
            instructionEntryCount = br.ReadInt16(),
            argumentOffset = br.ReadUInt16(),
            argumentEntryCount = br.ReadInt16(),
            globalVariableCount = br.ReadInt16(),
            stringOffset = br.ReadUInt16()
        };

        input.Position = bkPos;

        return header;
    }
}