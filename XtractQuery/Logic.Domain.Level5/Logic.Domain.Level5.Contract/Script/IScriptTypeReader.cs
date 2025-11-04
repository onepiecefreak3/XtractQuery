using Logic.Domain.Level5.Contract.DataClasses.Script;

namespace Logic.Domain.Level5.Contract.Script;

public interface IScriptTypeReader
{
    ScriptType Read(Stream stream);
    ScriptType Peek(Stream stream);
}