using Logic.Domain.Level5.Contract.Script.Gds.DataClasses;

namespace Logic.Domain.Level5.Contract.Script.Gds;

public interface IGdsScriptComposer
{
    GdsArgument[] Compose(GdsScriptFile script);
}