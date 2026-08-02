using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;
using Logic.Domain.CodeAnalysis.Contract.Level5;

namespace Logic.Business.Level5ScriptManagement.Conversion.HighLevelSyntax;

/// <summary>
/// Applies a flat-list transform recursively into nested structured statements.
/// </summary>
internal static class StructuredSyntaxRecursor
{
    public static IReadOnlyList<StatementSyntax> Apply(
        IReadOnlyList<StatementSyntax> statements,
        Func<IReadOnlyList<StatementSyntax>, IReadOnlyList<StatementSyntax>> applyFlat,
        ILevel5SyntaxFactory syntaxFactory)
    {
        IReadOnlyList<StatementSyntax> ApplyAll(IReadOnlyList<StatementSyntax> stmts)
        {
            IReadOnlyList<StatementSyntax> flat = applyFlat(stmts);
            return MapNested(flat, ApplyAll, syntaxFactory);
        }

        return ApplyAll(statements);
    }

    private static IReadOnlyList<StatementSyntax> MapNested(
        IReadOnlyList<StatementSyntax> statements,
        Func<IReadOnlyList<StatementSyntax>, IReadOnlyList<StatementSyntax>> applyAll,
        ILevel5SyntaxFactory syntaxFactory)
    {
        var result = new List<StatementSyntax>(statements.Count);
        foreach (StatementSyntax statement in statements)
            result.Add(MapStatement(statement, applyAll, syntaxFactory));
        return result;
    }

    private static StatementSyntax MapStatement(
        StatementSyntax statement,
        Func<IReadOnlyList<StatementSyntax>, IReadOnlyList<StatementSyntax>> applyAll,
        ILevel5SyntaxFactory syntaxFactory)
    {
        switch (statement)
        {
            case WhileStatementSyntax { Body: not null } whileStatement:
            {
                IReadOnlyList<StatementSyntax> body = applyAll(whileStatement.Body.Statements);
                whileStatement.SetBody(CreateBlock(body, syntaxFactory), false);
                return whileStatement;
            }

            case DoWhileStatementSyntax doWhile:
            {
                IReadOnlyList<StatementSyntax> body = applyAll(doWhile.Body.Statements);
                doWhile.SetBody(CreateBlock(body, syntaxFactory), false);
                return doWhile;
            }

            case IfStatementSyntax ifStatement:
            {
                IReadOnlyList<StatementSyntax> thenBody = applyAll(ifStatement.Body.Statements);
                ifStatement.SetBody(CreateBlock(thenBody, syntaxFactory), false);
                if (ifStatement.Else != null)
                {
                    StatementSyntax elseStmt = MapStatement(ifStatement.Else.Statement, applyAll, syntaxFactory);
                    ifStatement.Else.SetStatement(elseStmt, false);
                }

                return ifStatement;
            }

            case BlockSyntax block:
            {
                IReadOnlyList<StatementSyntax> nested = applyAll(block.Statements);
                block.SetStatements(nested, false);
                return block;
            }

            default:
                return statement;
        }
    }

    public static BlockSyntax CreateBlock(IReadOnlyList<StatementSyntax> statements, ILevel5SyntaxFactory syntaxFactory)
    {
        return new BlockSyntax(
            syntaxFactory.Token(SyntaxTokenKind.CurlyOpen),
            statements,
            syntaxFactory.Token(SyntaxTokenKind.CurlyClose));
    }
}
