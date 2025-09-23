using Logic.Business.Level5ScriptManagement.DataClasses.Conversion;
using Logic.Business.Level5ScriptManagement.InternalContract.Conversion;
using Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses;

namespace Logic.Business.Level5ScriptManagement.Conversion;

class Gss1CodeUnitReducer : IGss1CodeUnitReducer
{
    public void Reduce(MethodDeclarationSyntax method)
    {
        StatementBlock[] blocks = CreateBlocks(method);
        RelateBlocks(blocks);
    }

    private static StatementBlock[] CreateBlocks(MethodDeclarationSyntax method)
    {
        IList<StatementBlock> blocks = [];
        blocks.Add(new StatementBlock());

        var instructionIndex = 0;
        for (var i = 0; i < method.Body.Expressions.Count; i++)
        {
            StatementSyntax expression = method.Body.Expressions[i];
            switch (expression)
            {
                case ReturnStatementSyntax:
                case ExitStatementSyntax:
                    blocks[^1].Statements.Add(expression);
                    blocks[^1].IsExit = true;
                    blocks.Add(new StatementBlock { InstructionIndex = instructionIndex + 1 });
                    break;

                case IfGotoStatementSyntax:
                case IfNotGotoStatementSyntax:
                    blocks[^1].Statements.Add(expression);
                    blocks.Add(new StatementBlock { InstructionIndex = instructionIndex + 1 });
                    break;

                case GotoLabelStatementSyntax gotoLabel:
                    if (blocks[^1].Statements.Count > 0)
                        blocks.Add(new StatementBlock { InstructionIndex = instructionIndex });

                    blocks[^1].Labels.Add(gotoLabel.Label.Literal.Text);
                    while (i + 1 < method.Body.Expressions.Count)
                    {
                        if (method.Body.Expressions[i + 1] is not GotoLabelStatementSyntax nextGotoLabel)
                            break;

                        blocks[^1].Labels.Add(nextGotoLabel.Label.Literal.Text[1..^1]);
                        i++;
                    }
                    continue;

                default:
                    blocks[^1].Statements.Add(expression);
                    break;
            }

            instructionIndex++;
        }

        blocks[^1].IsExit = true;

        return [.. blocks];
    }

    private static void RelateBlocks(StatementBlock[] blocks)
    {
        foreach (StatementBlock block in blocks)
        {
            if (block.IsExit || block.Statements.Count <= 0)
                continue;

            StatementBlock? nextBlock = blocks.FirstOrDefault(x => x.InstructionIndex == block.InstructionIndex + block.Statements.Count);
            if (nextBlock is not null)
            {
                block.Children.Add(nextBlock);
                nextBlock.Parents.Add(block);
            }

            string? label = GetGotoLabel(block.Statements[^1]);
            if (label is null)
                continue;

            nextBlock = blocks.FirstOrDefault(x => x.Labels.Contains(label));
            if (nextBlock is null)
                continue;

            block.Children.Add(nextBlock);
            nextBlock.Parents.Add(block);
        }
    }

    private static string? GetGotoLabel(StatementSyntax statement)
    {
        ExpressionSyntax gotoTarget;
        switch (statement)
        {
            case IfGotoStatementSyntax gotoStatement:
                gotoTarget = gotoStatement.Goto.Target.Value;
                break;

            case IfNotGotoStatementSyntax notGotoStatement:
                gotoTarget = notGotoStatement.Goto.Target.Value;
                break;

            default:
                return null;
        }

        if (gotoTarget is not LiteralExpressionSyntax gotoLiteral)
            throw new InvalidOperationException("Invalid goto value expression.");

        return gotoLiteral.Literal.RawKind switch
        {
            (int)SyntaxTokenKind.HashStringLiteral => gotoLiteral.Literal.Text[1..^2],
            (int)SyntaxTokenKind.StringLiteral => gotoLiteral.Literal.Text[1..^1],
            _ => throw new InvalidOperationException("Invalid goto value expression.")
        };
    }
}