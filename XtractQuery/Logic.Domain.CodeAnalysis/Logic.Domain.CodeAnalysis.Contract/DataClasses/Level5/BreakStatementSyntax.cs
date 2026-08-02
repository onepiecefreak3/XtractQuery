namespace Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

public class BreakStatementSyntax : StatementSyntax
{
    public SyntaxToken Break { get; private set; }
    public SyntaxToken Semicolon { get; private set; }

    public override SyntaxLocation Location => Break.FullLocation;
    public override SyntaxSpan Span => new(Break.FullSpan.Position, Semicolon.FullSpan.EndPosition);

    public BreakStatementSyntax(SyntaxToken breakToken, SyntaxToken semicolon)
    {
        breakToken.Parent = this;
        semicolon.Parent = this;

        Break = breakToken;
        Semicolon = semicolon;

        Root.Update();
    }

    public void SetBreak(SyntaxToken breakToken, bool updatePositions = true)
    {
        breakToken.Parent = this;
        Break = breakToken;

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
        SyntaxToken breakToken = Break;
        SyntaxToken semicolon = Semicolon;

        position = breakToken.UpdatePosition(position, ref line, ref column);
        position = semicolon.UpdatePosition(position, ref line, ref column);

        Break = breakToken;
        Semicolon = semicolon;

        return position;
    }
}
