using Logic.Domain.CodeAnalysis.Contract.DataClasses;

namespace Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses;

public class AssignmentStatementSyntax : StatementSyntax
{
    public ExpressionSyntax Left { get; private set; }
    public SyntaxToken EqualsOperator { get; private set; }
    public ExpressionSyntax Right { get; private set; }
    public SyntaxToken Semicolon { get; private set; }

    public override SyntaxLocation Location => Left.Location;
    public override SyntaxSpan Span => new(Left.Span.Position, Right.Span.EndPosition);

    public AssignmentStatementSyntax(ExpressionSyntax left, SyntaxToken equalsOperator, ExpressionSyntax right, SyntaxToken semicolon)
    {
        left.Parent = this;
        equalsOperator.Parent = this;
        right.Parent = this;
        semicolon.Parent = this;

        Left = left;
        EqualsOperator = equalsOperator;
        Right = right;
        Semicolon = semicolon;

        Root.Update();
    }

    public void SetEqualsOperator(SyntaxToken equals, bool updatePositions = true)
    {
        equals.Parent = this;

        EqualsOperator = equals;

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
        SyntaxToken equals = EqualsOperator;
        SyntaxToken semicolon = Semicolon;

        position = Left.UpdatePosition(position, ref line, ref column);
        position = equals.UpdatePosition(position, ref line, ref column);
        position = Right.UpdatePosition(position, ref line, ref column);
        position = semicolon.UpdatePosition(position, ref line, ref column);

        EqualsOperator = equals;
        Semicolon = semicolon;

        return position;
    }
}