using Logic.Domain.Kuriimu2.KomponentAdapter.Contract;
using Logic.Domain.Level5.Compression.InternalContract;
using Logic.Domain.Level5.Contract.Script.Xseq;
using Logic.Domain.Level5.Contract.Script.Xseq.DataClasses;
using Logic.Domain.Level5.Script.Xseq.InternalContract;
using Logic.Domain.Level5.Script.InternalContract.DataClasses;

namespace Logic.Domain.Level5.Script.Xseq;

internal class XseqScriptDecompressor : ScriptDecompressor<XseqHeader>, IXseqScriptDecompressor
{
    public XseqScriptDecompressor(IBinaryFactory binaryFactory, IBinaryTypeReader typeReader, IStreamFactory streamFactory,
        IDecompressor decompressor, IXseqScriptEntrySizeProvider entrySizeProvider)
        : base(binaryFactory, typeReader, streamFactory, decompressor, entrySizeProvider)
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