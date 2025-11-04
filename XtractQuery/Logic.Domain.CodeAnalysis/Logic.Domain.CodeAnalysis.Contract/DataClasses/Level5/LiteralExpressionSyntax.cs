using Logic.Domain.CodeAnalysis.Contract.DataClasses;

namespace Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

public class LiteralExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken Literal { get; private set; }

    public override SyntaxLocation Location => Literal.FullLocation;
    public override SyntaxSpan Span => Literal.FullSpan;

    public LiteralExpressionSyntax(SyntaxToken literal)
    {
        literal.Parent = this;

        Literal = literal;

        Root.Update();
    }

    public void SetLiteral(SyntaxToken literal, bool updatePositions = true)
    {
        literal.Parent = this;

        Literal = literal;

        if (updatePositions)
            Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        SyntaxToken literal = Literal;

        position = literal.UpdatePosition(position, ref line, ref column);

        Literal = literal;

        return position;
    }
}