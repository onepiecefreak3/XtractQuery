using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;
using Logic.Domain.Level5.Contract.DataClasses.Script;

namespace Logic.Business.Level5ScriptManagement.InternalContract.Conversion;

public interface IXseqCodeUnitConverter
{
    ScriptFile CreateScriptFile(CodeUnitSyntax tree);
}