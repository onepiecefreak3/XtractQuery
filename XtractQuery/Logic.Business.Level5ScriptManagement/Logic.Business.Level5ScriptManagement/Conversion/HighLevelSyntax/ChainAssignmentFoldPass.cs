using Logic.Business.Level5ScriptManagement.Conversion.HighLevelSyntax.Cfg;
using Logic.Business.Level5ScriptManagement.DataClasses.Conversion;
using Logic.Business.Level5ScriptManagement.InternalContract.Conversion.HighLevelSyntax;
using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;
using Logic.Domain.CodeAnalysis.Contract.Level5;

namespace Logic.Business.Level5ScriptManagement.Conversion.HighLevelSyntax;

/// <summary>
/// Folds adjacent copy assignments into chained assignments, e.g.
/// <c>$local2 = expr; $local1 = $local2;</c> → <c>$local1 = $local2 = expr;</c>.
/// Unlike temp propagation, intermediate variables are kept in the chain.
/// Only folds pairs that share a basic block in the CFG.
/// </summary>
internal class ChainAssignmentFoldPass(
    IControlFlowGraphBuilder cfgBuilder,
    ILevel5SyntaxFactory syntaxFactory) : IChainAssignmentFoldPass
{
    public IReadOnlyList<StatementSyntax> Apply(IReadOnlyList<StatementSyntax> statements)
    {
        return StructuredSyntaxRecursor.Apply(statements, ApplyFlat, syntaxFactory);
    }

    private IReadOnlyList<StatementSyntax> ApplyFlat(IReadOnlyList<StatementSyntax> statements)
    {
        var result = statements.ToList();
        var changed = true;

        while (changed)
        {
            changed = false;
            ControlFlowGraph cfg = cfgBuilder.Build(result);

            foreach (StatementBlock block in cfg.Blocks)
            {
                if (block.IsExit || block.StatementCount < 2)
                    continue;

                for (int index = block.InstructionIndex; index < block.EndStatementIndex - 1; index++)
                {
                    if (!ControlFlowGraphQueries.AreConsecutiveInSameBlock(cfg, index, index + 1))
                        continue;

                    if (!TryFoldPair(result[index], result[index + 1], out AssignmentStatementSyntax? folded) ||
                        folded is null)
                        continue;

                    result[index] = folded;
                    result.RemoveAt(index + 1);
                    changed = true;
                    break;
                }

                if (changed)
                    break;
            }
        }

        return result;
    }

    private static bool TryFoldPair(
        StatementSyntax first,
        StatementSyntax second,
        out AssignmentStatementSyntax? folded)
    {
        folded = null;

        if (first is not AssignmentStatementSyntax sourceAssign ||
            second is not AssignmentStatementSyntax copyAssign)
            return false;

        if (sourceAssign.EqualsOperator.RawKind != (int)SyntaxTokenKind.EqualsSign ||
            copyAssign.EqualsOperator.RawKind != (int)SyntaxTokenKind.EqualsSign)
            return false;

        if (!TryGetVariableName(sourceAssign.Left, out string sourceName))
            return false;

        if (!TryGetVariableName(copyAssign.Right, out string copiedName) ||
            copiedName != sourceName)
            return false;

        if (!TryGetVariableName(copyAssign.Left, out string destinationName) ||
            destinationName == sourceName)
            return false;

        var chain = new AssignmentExpressionSyntax(
            sourceAssign.Left,
            sourceAssign.EqualsOperator,
            sourceAssign.Right);

        folded = new AssignmentStatementSyntax(
            copyAssign.Left,
            copyAssign.EqualsOperator,
            chain,
            copyAssign.Semicolon);

        return true;
    }

    private static bool TryGetVariableName(ExpressionSyntax expression, out string name)
    {
        name = string.Empty;

        if (expression is ValueExpressionSyntax { Value: VariableExpressionSyntax variable })
        {
            name = variable.Variable.Text;
            return true;
        }

        if (expression is VariableExpressionSyntax bare)
        {
            name = bare.Variable.Text;
            return true;
        }

        return false;
    }
}
