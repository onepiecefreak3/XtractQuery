using Logic.Domain.Level5.Contract.Script.Gds.DataClasses;

namespace Logic.Domain.Level5.Contract.Script.Gds;

public interface IGdsScriptWriter
{
    void Write(GdsScriptFile script, Stream output);

    void Write(GdsArgument[] arguments, Stream output);
}