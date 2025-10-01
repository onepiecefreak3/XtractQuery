using Logic.Domain.Level5.Contract.Script.Gds.DataClasses;

namespace Logic.Domain.Level5.Contract.Script.Gds;

public interface IGdsScriptReader
{
    GdsArgument[] Read(Stream input);
}