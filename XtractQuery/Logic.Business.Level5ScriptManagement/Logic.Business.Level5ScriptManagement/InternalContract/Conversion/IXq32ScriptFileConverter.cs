using Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses;
using Logic.Domain.Level5.Contract.Script.DataClasses;

namespace Logic.Business.Level5ScriptManagement.InternalContract.Conversion;

public interface IXq32ScriptFileConverter
{
    CodeUnitSyntax CreateCodeUnit(ScriptFile script);
}