using Logic.Domain.Level5.Contract.DataClasses.Script;
using Logic.Domain.Level5.InternalContract.Script.Xseq;

namespace Logic.Domain.Level5.Script.Xseq;

internal class XseqScriptEntrySizeProvider : IXseqScriptEntrySizeProvider
{
    public int GetFunctionEntrySize(PointerLength length)
    {
        return length switch
        {
            PointerLength.Int => 0x14,
            PointerLength.Long => 0x18,
            _ => throw new InvalidOperationException($"Unknown pointer length {length}.")
        };
    }

    public int GetJumpEntrySize(PointerLength length)
    {
        return length switch
        {
            PointerLength.Int => 0x8,
            PointerLength.Long => 0x10,
            _ => throw new InvalidOperationException($"Unknown pointer length {length}.")
        };
    }

    public int GetInstructionEntrySize(PointerLength length)
    {
        return length switch
        {
            PointerLength.Int => 0xC,
            PointerLength.Long => 0x10,
            _ => throw new InvalidOperationException($"Unknown pointer length {length}.")
        };
    }

    public int GetArgumentEntrySize(PointerLength length)
    {
        return length switch
        {
            PointerLength.Int => 0x8,
            PointerLength.Long => 0x10,
            _ => throw new InvalidOperationException($"Unknown pointer length {length}.")
        };
    }
}