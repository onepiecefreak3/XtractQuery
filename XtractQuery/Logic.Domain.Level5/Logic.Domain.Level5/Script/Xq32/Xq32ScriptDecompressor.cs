using Logic.Domain.Level5.Contract.DataClasses.Script.Xq32;
using Logic.Domain.Level5.Contract.Script.Xq32;
using Logic.Domain.Level5.DataClasses.Script;
using Logic.Domain.Level5.InternalContract.Compression;
using Logic.Domain.Level5.InternalContract.Script.Xq32;

namespace Logic.Domain.Level5.Script.Xq32;

internal class Xq32ScriptDecompressor : ScriptDecompressor<Xq32Header>, IXq32ScriptDecompressor
{
    public Xq32ScriptDecompressor(IDecompressor decompressor, IXq32ScriptEntrySizeProvider entrySizeProvider)
        : base(decompressor, entrySizeProvider)
    {
    }

    protected override int GetGlobalVariableCount(Xq32Header header)
    {
        return header.globalVariableCount;
    }

    protected override TableData GetFunctionTableData(Xq32Header header)
    {
        return new TableData
        {
            offset = header.functionOffset << 2,
            count = header.functionEntryCount
        };
    }

    protected override TableData GetJumpTableData(Xq32Header header)
    {
        return new TableData
        {
            offset = header.jumpOffset << 2,
            count = header.jumpEntryCount
        };
    }

    protected override TableData GetInstructionTableData(Xq32Header header)
    {
        return new TableData
        {
            offset = header.instructionOffset << 2,
            count = header.instructionEntryCount
        };
    }

    protected override TableData GetArgumentTableData(Xq32Header header)
    {
        return new TableData
        {
            offset = header.argumentOffset << 2,
            count = header.argumentEntryCount
        };
    }

    protected override int GetStringTableOffset(Xq32Header header)
    {
        return header.stringOffset << 2;
    }
}