namespace Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

public class IfGotoStatementSyntax : StatementSyntax
{
    public SyntaxToken If { get; private set; }
    public ExpressionSyntax Value { get; private set; }
    public GotoExpressionSyntax Goto { get; private set; }
    public SyntaxToken Semicolon { get; private set; }

    public override SyntaxLocation Location => If.FullLocation;
    public override SyntaxSpan Span => new(If.FullSpan.Position, Goto.Span.EndPosition);

    public IfGotoStatementSyntax(SyntaxToken ifToken, ExpressionSyntax value, GotoExpressionSyntax gotoStatement, SyntaxToken semicolon)
    {
        ifToken.Parent = this;
        value.Parent = this;
        gotoStatement.Parent = this;
        semicolon.Parent = this;

        If = ifToken;
        Value = value;
        Goto = gotoStatement;
        Semicolon = semicolon;

        Root.Update();
    }

    public void SetIf(SyntaxToken ifToken, bool updatePositions = true)
    {
        ifToken.Parent = this;

        If = ifToken;

        if (updatePositions)
            Root.Update();
    }

    public void SetValue(ExpressionSyntax value, bool updatePositions = true)
    {
        value.Parent = this;

        Value = value;

        if (updatePositions)
            Root.Update();
    }

    public void SetSemicolon(SyntaxToken semicolon, bool updatePositions = true)
    {
        semicolon.Parent = this;

        Semicolon = semicolon;

        if (updatePositions)
            Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        SyntaxToken ifToken = If;
        SyntaxToken semicolon = Semicolon;

        position = ifToken.UpdatePosition(position, ref line, ref column);
        position = Value.UpdatePosition(position, ref line, ref column);
        position = Goto.UpdatePosition(position, ref line, ref column);
        position = semicolon.UpdatePosition(position, ref line, ref column);

        If = ifToken;
        Semicolon = semicolon;

        return position;
    }
}
