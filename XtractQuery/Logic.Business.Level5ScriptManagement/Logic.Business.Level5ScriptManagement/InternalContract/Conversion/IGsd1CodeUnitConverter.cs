using Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses;
using Logic.Domain.Level5.Contract.Script.Gsd1.DataClasses;

namespace Logic.Business.Level5ScriptManagement.InternalContract.Conversion;

public interface IGsd1CodeUnitConverter
{
    Gsd1ScriptFile CreateScriptFile(CodeUnitSyntax tree);
}