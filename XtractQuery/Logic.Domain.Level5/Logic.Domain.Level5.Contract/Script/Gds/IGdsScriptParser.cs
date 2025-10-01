using Logic.Domain.Level5.Contract.Script.Gds.DataClasses;

namespace Logic.Domain.Level5.Contract.Script.Gds;

public interface IGdsScriptParser
{
    GdsScriptFile Parse(Stream input);
    GdsScriptFile Parse(GdsArgument[] arguments);
}