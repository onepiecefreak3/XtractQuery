using Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses;
using Logic.Domain.Level5.Contract.Script.Gss1.DataClasses;

namespace Logic.Business.Level5ScriptManagement.InternalContract.Conversion;

public interface IGss1ScriptFileConverter
{
    CodeUnitSyntax CreateCodeUnit(Gss1ScriptFile script);
}
