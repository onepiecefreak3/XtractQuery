using Logic.Domain.CodeAnalysis.Contract.DataClasses;

namespace Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

public class UnaryExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken Operation { get; private set; }
    public ValueExpressionSyntax Value { get; private set; }

    public override SyntaxLocation Location => Operation.FullLocation;
    public override SyntaxSpan Span => new(Operation.FullSpan.Position, Value.Span.EndPosition);

    public UnaryExpressionSyntax(SyntaxToken operation, ValueExpressionSyntax value)
    {
        operation.Parent = this;
        value.Parent = this;

        Operation = operation;
        Value = value;

        Root.Update();
    }

    public void SetOperation(SyntaxToken operation, bool updatePositions = true)
    {
        operation.Parent = this;

        Operation = operation;

        if (updatePositions)
            Root.Update();
    }

    public void SetValue(ValueExpressionSyntax value, bool updatePositions = true)
    {
        value.Parent = this;

        Value = value;

        if (updatePositions)
            Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        SyntaxToken operation = Operation;

        position = operation.UpdatePosition(position, ref line, ref column);
        position = Value.UpdatePosition(position, ref line, ref column);

        Operation = operation;

        return position;
    }
}