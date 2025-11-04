using Logic.Domain.Level5.Contract.DataClasses.Script.Gsd1;

namespace Logic.Domain.Level5.Contract.Script.Gsd1;

public interface IGsd1ScriptComposer
{
    Gsd1ScriptContainer Compose(Gsd1ScriptFile script);
}