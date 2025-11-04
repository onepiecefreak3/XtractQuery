using Logic.Domain.Level5.Contract.DataClasses.Script.Gss1;

namespace Logic.Domain.Level5.Contract.Script.Gss1;

public interface IGss1ScriptComposer
{
    Gss1ScriptContainer Compose(Gss1ScriptFile script);
}