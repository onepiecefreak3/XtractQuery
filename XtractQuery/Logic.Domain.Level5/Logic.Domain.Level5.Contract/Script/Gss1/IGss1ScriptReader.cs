using Logic.Domain.Level5.Contract.Script.Gss1.DataClasses;

namespace Logic.Domain.Level5.Contract.Script.Gss1;

public interface IGss1ScriptReader
{
    Gss1ScriptContainer Read(Stream input);
}
