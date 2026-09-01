using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

namespace Logic.Business.Level5ScriptManagement.InternalContract.Conversion.HighLevelSyntax;

public interface IXq32LowLevelCodeUnitConverter
{
    CodeUnitSyntax Convert(CodeUnitSyntax tree);
}
