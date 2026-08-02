namespace Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

public class ParenthesizedExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken ParenOpen { get; private set; }
    public ExpressionSyntax Expression { get; private set; }
    public SyntaxToken ParenClose { get; private set; }

    public override SyntaxLocation Location => ParenOpen.FullLocation;
    public override SyntaxSpan Span => new(ParenOpen.FullSpan.Position, ParenClose.FullSpan.EndPosition);

    public ParenthesizedExpressionSyntax(SyntaxToken parenOpen, ExpressionSyntax expression, SyntaxToken parenClose)
    {
        parenOpen.Parent = this;
        expression.Parent = this;
        parenClose.Parent = this;

        ParenOpen = parenOpen;
        Expression = expression;
        ParenClose = parenClose;

        Root.Update();
    }

    public void SetParenOpen(SyntaxToken parenOpen, bool updatePositions = true)
    {
        parenOpen.Parent = this;
        ParenOpen = parenOpen;

        if (updatePositions)
            Root.Update();
    }

    public void SetExpression(ExpressionSyntax expression, bool updatePositions = true)
    {
        expression.Parent = this;
        Expression = expression;

        if (updatePositions)
            Root.Update();
    }

    public void SetParenClose(SyntaxToken parenClose, bool updatePositions = true)
    {
        parenClose.Parent = this;
        ParenClose = parenClose;

        if (updatePositions)
            Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        SyntaxToken parenOpen = ParenOpen;
        SyntaxToken parenClose = ParenClose;

        position = parenOpen.UpdatePosition(position, ref line, ref column);
        position = Expression.UpdatePosition(position, ref line, ref column);
        position = parenClose.UpdatePosition(position, ref line, ref column);

        ParenOpen = parenOpen;
        ParenClose = parenClose;

        return position;
    }
}
