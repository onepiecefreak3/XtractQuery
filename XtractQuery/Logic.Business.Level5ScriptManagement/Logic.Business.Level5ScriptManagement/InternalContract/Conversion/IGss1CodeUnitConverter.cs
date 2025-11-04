using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;
using Logic.Domain.Level5.Contract.DataClasses.Script.Gss1;

namespace Logic.Business.Level5ScriptManagement.InternalContract.Conversion;

public interface IGss1CodeUnitConverter
{
    Gss1ScriptFile CreateScriptFile(CodeUnitSyntax tree);
}