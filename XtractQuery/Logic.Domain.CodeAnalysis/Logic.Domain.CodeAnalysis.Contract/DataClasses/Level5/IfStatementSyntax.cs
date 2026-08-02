namespace Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

public class IfStatementSyntax : StatementSyntax
{
    public SyntaxToken If { get; private set; }
    public ExpressionSyntax Condition { get; private set; }
    public BlockSyntax Body { get; private set; }
    public ElseClauseSyntax? Else { get; private set; }

    public override SyntaxLocation Location => If.FullLocation;

    public override SyntaxSpan Span => Else is null
        ? new(If.FullSpan.Position, Body.Span.EndPosition)
        : new(If.FullSpan.Position, Else.Span.EndPosition);

    public IfStatementSyntax(SyntaxToken ifToken, ExpressionSyntax condition, BlockSyntax body, ElseClauseSyntax? elseClause)
    {
        ifToken.Parent = this;
        condition.Parent = this;
        body.Parent = this;
        if (elseClause != null)
            elseClause.Parent = this;

        If = ifToken;
        Condition = condition;
        Body = body;
        Else = elseClause;

        Root.Update();
    }

    public void SetIf(SyntaxToken ifToken, bool updatePosition = true)
    {
        ifToken.Parent = this;
        If = ifToken;

        if (updatePosition)
            Root.Update();
    }

    public void SetCondition(ExpressionSyntax condition, bool updatePosition = true)
    {
        condition.Parent = this;
        Condition = condition;

        if (updatePosition)
            Root.Update();
    }

    public void SetBody(BlockSyntax body, bool updatePosition = true)
    {
        body.Parent = this;
        Body = body;

        if (updatePosition)
            Root.Update();
    }

    public void SetElse(ElseClauseSyntax? elseClause, bool updatePosition = true)
    {
        if (elseClause != null)
            elseClause.Parent = this;

        Else = elseClause;

        if (updatePosition)
            Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        SyntaxToken ifToken = If;

        position = ifToken.UpdatePosition(position, ref line, ref column);
        position = Condition.UpdatePosition(position, ref line, ref column);
        position = Body.UpdatePosition(position, ref line, ref column);
        if (Else != null)
            position = Else.UpdatePosition(position, ref line, ref column);

        If = ifToken;

        return position;
    }
}
