using Logic.Domain.Level5.Contract.DataClasses.Script.Gss1;

namespace Logic.Domain.Level5.Contract.Script.Gss1;

public interface IGss1ScriptReader
{
    Gss1ScriptContainer Read(Stream input);
}
