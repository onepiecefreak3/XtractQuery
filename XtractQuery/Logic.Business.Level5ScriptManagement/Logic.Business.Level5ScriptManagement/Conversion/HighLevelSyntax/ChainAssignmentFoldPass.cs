using Logic.Business.Level5ScriptManagement.InternalContract.Conversion.HighLevelSyntax;
using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;
using Logic.Domain.CodeAnalysis.Contract.Level5;

namespace Logic.Business.Level5ScriptManagement.Conversion.HighLevelSyntax;

/// <summary>
/// Folds adjacent copy assignments into chained assignments, e.g.
/// <c>$local2 = expr; $local1 = $local2;</c> → <c>$local1 = $local2 = expr;</c>.
/// Unlike temp propagation, intermediate variables are kept in the chain.
/// </summary>
internal class ChainAssignmentFoldPass(ILevel5SyntaxFactory syntaxFactory) : IChainAssignmentFoldPass
{
    public IReadOnlyList<StatementSyntax> Apply(IReadOnlyList<StatementSyntax> statements)
    {
        var result = ApplyFlat(statements);
        return RecurseIntoNested(result);
    }

    private IReadOnlyList<StatementSyntax> ApplyFlat(IReadOnlyList<StatementSyntax> statements)
    {
        var result = statements.ToList();
        var changed = true;

        while (changed)
        {
            changed = false;

            for (var i = 0; i < result.Count - 1; i++)
            {
                if (!TryFoldPair(result[i], result[i + 1], out AssignmentStatementSyntax? folded) ||
                    folded is null)
                    continue;

                result[i] = folded;
                result.RemoveAt(i + 1);
                changed = true;
                break;
            }
        }

        return result;
    }

    private IReadOnlyList<StatementSyntax> RecurseIntoNested(IReadOnlyList<StatementSyntax> statements)
    {
        var result = new List<StatementSyntax>(statements.Count);
        foreach (StatementSyntax statement in statements)
            result.Add(RecurseStatement(statement));
        return result;
    }

    private StatementSyntax RecurseStatement(StatementSyntax statement)
    {
        switch (statement)
        {
            case WhileStatementSyntax { Body: not null } whileStatement:
            {
                IReadOnlyList<StatementSyntax> body = Apply(whileStatement.Body.Statements);
                whileStatement.SetBody(CreateBlock(body), false);
                return whileStatement;
            }

            case DoWhileStatementSyntax doWhile:
            {
                IReadOnlyList<StatementSyntax> body = Apply(doWhile.Body.Statements);
                doWhile.SetBody(CreateBlock(body), false);
                return doWhile;
            }

            case IfStatementSyntax ifStatement:
            {
                IReadOnlyList<StatementSyntax> thenBody = Apply(ifStatement.Body.Statements);
                ifStatement.SetBody(CreateBlock(thenBody), false);
                if (ifStatement.Else != null)
                {
                    StatementSyntax elseStmt = RecurseStatement(ifStatement.Else.Statement);
                    ifStatement.Else.SetStatement(elseStmt, false);
                }

                return ifStatement;
            }

            case BlockSyntax block:
            {
                IReadOnlyList<StatementSyntax> nested = Apply(block.Statements);
                block.SetStatements(nested, false);
                return block;
            }

            default:
                return statement;
        }
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

    private BlockSyntax CreateBlock(IReadOnlyList<StatementSyntax> statements)
    {
        return new BlockSyntax(
            syntaxFactory.Token(SyntaxTokenKind.CurlyOpen),
            statements,
            syntaxFactory.Token(SyntaxTokenKind.CurlyClose));
    }
}
