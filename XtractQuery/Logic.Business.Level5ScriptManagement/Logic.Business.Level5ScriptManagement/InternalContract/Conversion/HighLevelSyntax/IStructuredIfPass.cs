using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

namespace Logic.Business.Level5ScriptManagement.InternalContract.Conversion.HighLevelSyntax;

internal interface IStructuredIfPass
{
    IReadOnlyList<StatementSyntax> Apply(IReadOnlyList<StatementSyntax> statements);
}
