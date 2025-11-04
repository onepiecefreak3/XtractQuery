using Logic.Domain.Level5.Contract.Script.Xseq;
using Logic.Domain.Level5.Contract.DataClasses.Script.Xseq;
using Logic.Domain.Level5.InternalContract.Compression;
using Logic.Domain.Level5.DataClasses.Script;
using Logic.Domain.Level5.InternalContract.Script.Xseq;

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
}