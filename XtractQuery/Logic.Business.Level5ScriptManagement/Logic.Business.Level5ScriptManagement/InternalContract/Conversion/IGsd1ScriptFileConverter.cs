using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;
using Logic.Domain.Level5.Contract.DataClasses.Script.Gsd1;

namespace Logic.Business.Level5ScriptManagement.InternalContract.Conversion;

public interface IGsd1ScriptFileConverter
{
    CodeUnitSyntax CreateCodeUnit(Gsd1ScriptFile script);
}
