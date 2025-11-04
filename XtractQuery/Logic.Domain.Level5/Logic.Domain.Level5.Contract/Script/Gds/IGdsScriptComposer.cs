using Logic.Domain.Level5.Contract.DataClasses.Script.Gds;

namespace Logic.Domain.Level5.Contract.Script.Gds;

public interface IGdsScriptComposer
{
    GdsArgument[] Compose(GdsScriptFile script);
}