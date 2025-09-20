using Logic.Domain.Level5.Contract.Script.DataClasses;

namespace Logic.Domain.Level5.Contract.Script;

public interface IStringTableFactory
{
    IStringTable Create(Stream input, ScriptType type);
    IStringTable Create(ScriptType scriptType);
}