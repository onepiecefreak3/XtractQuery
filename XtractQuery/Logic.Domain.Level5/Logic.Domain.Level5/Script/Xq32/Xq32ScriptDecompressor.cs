using Logic.Domain.Kuriimu2.KomponentAdapter.Contract;
using Logic.Domain.Level5.Compression.InternalContract;
using Logic.Domain.Level5.Contract.Script.Xq32;
using Logic.Domain.Level5.Contract.Script.Xq32.DataClasses;
using Logic.Domain.Level5.Script.InternalContract.DataClasses;
using Logic.Domain.Level5.Script.Xq32.InternalContract;

namespace Logic.Domain.Level5.Script.Xq32;

internal class Xq32ScriptDecompressor : ScriptDecompressor<Xq32Header>, IXq32ScriptDecompressor
{
    public Xq32ScriptDecompressor(IBinaryFactory binaryFactory, IBinaryTypeReader typeReader, IStreamFactory streamFactory,
        IDecompressor decompressor, IXq32ScriptEntrySizeProvider entrySizeProvider)
        : base(binaryFactory, typeReader, streamFactory, decompressor, entrySizeProvider)
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