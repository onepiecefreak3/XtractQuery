using Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses;
using Logic.Domain.Level5.Contract.Script.Gds.DataClasses;

namespace Logic.Business.Level5ScriptManagement.InternalContract.Conversion;

public interface IGdsCodeUnitConverter
{
    GdsScriptFile CreateScriptFile(CodeUnitSyntax tree);
}