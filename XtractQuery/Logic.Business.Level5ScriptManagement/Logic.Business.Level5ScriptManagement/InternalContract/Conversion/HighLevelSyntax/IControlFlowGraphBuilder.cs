using Logic.Business.Level5ScriptManagement.DataClasses.Conversion;
using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

namespace Logic.Business.Level5ScriptManagement.InternalContract.Conversion.HighLevelSyntax;

internal interface IControlFlowGraphBuilder
{
    ControlFlowGraph Build(IReadOnlyList<StatementSyntax> statements);
}
