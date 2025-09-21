using Logic.Domain.Level5.Contract.Script.Gss1.DataClasses;

namespace Logic.Domain.Level5.Contract.Script.Gss1;

public interface IGss1ScriptComposer
{
    Gss1ScriptContainer Compose(Gss1ScriptFile script);
}