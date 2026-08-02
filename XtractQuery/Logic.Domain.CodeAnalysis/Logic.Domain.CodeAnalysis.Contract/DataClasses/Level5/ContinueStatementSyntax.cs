namespace Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

public class ContinueStatementSyntax : StatementSyntax
{
    public SyntaxToken Continue { get; private set; }
    public SyntaxToken Semicolon { get; private set; }

    public override SyntaxLocation Location => Continue.FullLocation;
    public override SyntaxSpan Span => new(Continue.FullSpan.Position, Semicolon.FullSpan.EndPosition);

    public ContinueStatementSyntax(SyntaxToken continueToken, SyntaxToken semicolon)
    {
        continueToken.Parent = this;
        semicolon.Parent = this;

        Continue = continueToken;
        Semicolon = semicolon;

        Root.Update();
    }

    public void SetContinue(SyntaxToken continueToken, bool updatePositions = true)
    {
        continueToken.Parent = this;
        Continue = continueToken;

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
        SyntaxToken continueToken = Continue;
        SyntaxToken semicolon = Semicolon;

        position = continueToken.UpdatePosition(position, ref line, ref column);
        position = semicolon.UpdatePosition(position, ref line, ref column);

        Continue = continueToken;
        Semicolon = semicolon;

        return position;
    }
}
