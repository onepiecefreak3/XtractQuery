using Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses;
using Logic.Domain.Level5.Contract.Script.DataClasses;

namespace Logic.Business.Level5ScriptManagement.InternalContract;

public interface ILevel5ScriptFileConverter
{
    CodeUnitSyntax CreateCodeUnit(ScriptFile script);
}