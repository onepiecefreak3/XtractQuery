using Logic.Domain.Level5.Contract.Script.DataClasses;

namespace Logic.Domain.Level5.Contract.Script;

public interface IScriptEntrySizeProvider
{
    int GetFunctionEntrySize(PointerLength length);
    int GetJumpEntrySize(PointerLength length);
    int GetInstructionEntrySize(PointerLength length);
    int GetArgumentEntrySize(PointerLength length);
}