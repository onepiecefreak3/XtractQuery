using Logic.Domain.CodeAnalysis.Contract.DataClasses;

namespace Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses;

public class PostfixUnaryStatementSyntax : StatementSyntax
{
    public PostfixUnaryExpressionSyntax Expression { get; private set; }
    public SyntaxToken Semicolon { get; private set; }

    public override SyntaxLocation Location => Expression.Location;
    public override SyntaxSpan Span => new(Expression.Span.Position, Semicolon.FullSpan.EndPosition);

    public PostfixUnaryStatementSyntax(PostfixUnaryExpressionSyntax expression, SyntaxToken semicolon)
    {
        expression.Parent = this;
        semicolon.Parent = this;
            
        Expression = expression;
        Semicolon = semicolon;

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
        SyntaxToken semicolon = Semicolon;
            
        position = Expression.UpdatePosition(position, ref line, ref column);
        position = semicolon.UpdatePosition(position, ref line, ref column);
            
        Semicolon = semicolon;

        return position;
    }
}