using Logic.Domain.Level5.Contract.Script.DataClasses;

namespace Logic.Domain.Level5.Contract.Script;

public interface IScriptEntrySizeProviderFactory
{
    IScriptEntrySizeProvider Create(ScriptType type);
}