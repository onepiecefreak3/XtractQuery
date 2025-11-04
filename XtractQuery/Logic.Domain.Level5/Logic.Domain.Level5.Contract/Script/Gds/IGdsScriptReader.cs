using Logic.Domain.Level5.Contract.DataClasses.Script.Gds;

namespace Logic.Domain.Level5.Contract.Script.Gds;

public interface IGdsScriptReader
{
    GdsArgument[] Read(Stream input);
}