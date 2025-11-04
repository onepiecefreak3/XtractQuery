using Logic.Domain.CodeAnalysis.Contract.DataClasses;

namespace Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

public class ValueExpressionSyntax : ExpressionSyntax
{
    public ExpressionSyntax Value { get; private set; }
    public ValueMetadataParametersSyntax? MetadataParameters { get; private set; }

    public override SyntaxLocation Location => Value.Location;
    public override SyntaxSpan Span => new(Value.Span.Position, MetadataParameters?.Span.EndPosition ?? Value.Span.EndPosition);

    public ValueExpressionSyntax(ExpressionSyntax value, ValueMetadataParametersSyntax? metadataParameters = null)
    {
        value.Parent = this;
        if (metadataParameters != null)
            metadataParameters.Parent = this;

        Value = value;
        MetadataParameters = metadataParameters;

        Root.Update();
    }

    public void SetValue(ExpressionSyntax value, bool updatePosition = true)
    {
        value.Parent = this;
        Value = value;

        if (updatePosition)
            Root.Update();
    }

    public void SetMetadataParameters(ValueMetadataParametersSyntax? metadataParameters, bool updatePosition = true)
    {
        if (metadataParameters != null)
            metadataParameters.Parent = this;

        MetadataParameters = metadataParameters;

        if (updatePosition)
            Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        position = Value.UpdatePosition(position, ref line, ref column);
        if (MetadataParameters != null)
            position = MetadataParameters.UpdatePosition(position, ref line, ref column);

        return position;
    }
}