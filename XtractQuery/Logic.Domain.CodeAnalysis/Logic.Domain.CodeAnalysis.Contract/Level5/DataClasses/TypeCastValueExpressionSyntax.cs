using Logic.Domain.CodeAnalysis.Contract.DataClasses;

namespace Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses;

public class TypeCastValueExpressionSyntax : ExpressionSyntax
{
    public TypeCastExpressionSyntax TypeCast { get; private set; }
    public ValueExpressionSyntax Value { get; private set; }

    public override SyntaxLocation Location => TypeCast.Location;
    public override SyntaxSpan Span => new(TypeCast.Span.Position, Value.Span.EndPosition);

    public TypeCastValueExpressionSyntax(TypeCastExpressionSyntax typeCast, ValueExpressionSyntax value)
    {
        typeCast.Parent = this;
        value.Parent = this;

        TypeCast = typeCast;
        Value = value;

        Root.Update();
    }

    public void SetTypeCast(TypeCastExpressionSyntax typeCast, bool updatePositions = true)
    {
        typeCast.Parent = this;

        TypeCast = typeCast;

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
        position = TypeCast.UpdatePosition(position, ref line,ref column);
        position = Value.UpdatePosition(position, ref line, ref column);

        return position;
    }
}