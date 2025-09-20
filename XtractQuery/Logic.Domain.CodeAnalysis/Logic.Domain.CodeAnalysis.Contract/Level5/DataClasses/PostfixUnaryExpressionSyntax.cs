using Logic.Domain.CodeAnalysis.Contract.DataClasses;

namespace Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses;

public class PostfixUnaryExpressionSyntax : ExpressionSyntax
{
    public ExpressionSyntax Value { get; private set; }
    public SyntaxToken Operation { get; private set; }

    public override SyntaxLocation Location => Value.Location;
    public override SyntaxSpan Span => new(Value.Span.Position, Operation.FullSpan.EndPosition);

    public PostfixUnaryExpressionSyntax(ExpressionSyntax value, SyntaxToken operation)
    {
        value.Parent = this;
        operation.Parent = this;

        Value = value;
        Operation = operation;

        Root.Update();
    }

    public void SetOperation(SyntaxToken operation, bool updatePositions = true)
    {
        operation.Parent = this;

        Operation = operation;

        if (updatePositions)
            Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        SyntaxToken operation = Operation;

        position = Value.UpdatePosition(position, ref line, ref column);
        position = operation.UpdatePosition(position, ref line, ref column);

        Operation = operation;

        return position;
    }
}