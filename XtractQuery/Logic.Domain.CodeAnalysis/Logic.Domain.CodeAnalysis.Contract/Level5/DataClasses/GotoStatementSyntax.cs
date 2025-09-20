using Logic.Domain.CodeAnalysis.Contract.DataClasses;

namespace Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses;

public class GotoStatementSyntax : StatementSyntax
{
    public SyntaxToken Goto { get; private set; }
    public ValueExpressionSyntax Target { get; private set; }
    public SyntaxToken Semicolon { get; private set; }

    public override SyntaxLocation Location => Goto.FullLocation;
    public override SyntaxSpan Span => new(Goto.FullSpan.Position, Target.Span.EndPosition);

    public GotoStatementSyntax(SyntaxToken gotoToken, ValueExpressionSyntax target, SyntaxToken semicolon)
    {
        gotoToken.Parent = this;
        target.Parent = this;
        semicolon.Parent = this;

        Goto = gotoToken;
        Target = target;
        Semicolon = semicolon;

        Root.Update();
    }

    public void SetGoto(SyntaxToken gotoToken, bool updatePositions = true)
    {
        gotoToken.Parent = this;

        Goto = gotoToken;

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
        SyntaxToken gotoToken = Goto;
        SyntaxToken semicolon = Semicolon;

        position = gotoToken.UpdatePosition(position, ref line, ref column);
        position = Target.UpdatePosition(position, ref line, ref column);
        position = semicolon.UpdatePosition(position, ref line, ref column);

        Goto = gotoToken;
        Semicolon = semicolon;

        return position;
    }
}