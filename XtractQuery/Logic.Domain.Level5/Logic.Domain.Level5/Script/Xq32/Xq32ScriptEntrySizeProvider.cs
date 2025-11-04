using Logic.Domain.Level5.Contract.DataClasses.Script;
using Logic.Domain.Level5.InternalContract.Script.Xq32;

namespace Logic.Domain.Level5.Script.Xq32;

internal class Xq32ScriptEntrySizeProvider : IXq32ScriptEntrySizeProvider
{
    public int GetFunctionEntrySize(PointerLength length)
    {
        return length switch
        {
            PointerLength.Int => 0x18,
            PointerLength.Long => 0x20,
            _ => throw new InvalidOperationException($"Unknown pointer length {length}.")
        };
    }

    public int GetJumpEntrySize(PointerLength length)
    {
        return length switch
        {
            PointerLength.Int => 0xC,
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