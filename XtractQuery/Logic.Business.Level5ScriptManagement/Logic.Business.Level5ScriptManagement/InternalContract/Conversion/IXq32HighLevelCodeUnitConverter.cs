using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;
using Logic.Domain.CodeAnalysis.Contract.Level5;

namespace Logic.Business.Level5ScriptManagement.InternalContract.Conversion;

public interface IXq32HighLevelCodeUnitConverter
{
    CodeUnitSyntax Convert(CodeUnitSyntax tree);
}
