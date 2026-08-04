using Logic.Business.Level5ScriptManagement.DataClasses.Conversion;
using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

namespace Logic.Business.Level5ScriptManagement.InternalContract.Conversion.HighLevelSyntax;

public interface INamedParameterSlotPass
{
    IReadOnlyList<NamedParameterGlobalConflictWarning> Warnings { get; }

    CodeUnitSyntax Convert(CodeUnitSyntax tree);
}
