using Logic.Domain.CodeAnalysis.Contract.DataClasses;

namespace Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses;

public class ValueMetadataParametersSyntax : SyntaxNode
{
    public SyntaxToken RelSmaller { get; private set; }
    public LiteralExpressionSyntax Parameter { get; private set; }
    public SyntaxToken RelBigger { get; private set; }

    public override SyntaxLocation Location => RelSmaller.FullLocation;
    public override SyntaxSpan Span => new(RelSmaller.FullSpan.Position, RelBigger.FullSpan.EndPosition);

    public ValueMetadataParametersSyntax(SyntaxToken relSmaller, LiteralExpressionSyntax parameter, SyntaxToken relBigger)
    {
        relSmaller.Parent = this;
        parameter.Parent = this;
        relBigger.Parent = this;

        RelSmaller = relSmaller;
        Parameter = parameter;
        RelBigger = relBigger;

        Root.Update();
    }

    public void SetRelSmaller(SyntaxToken relSmaller, bool updatePosition = true)
    {
        relSmaller.Parent = this;
        RelSmaller = relSmaller;

        if (updatePosition)
            Root.Update();
    }

    public void SetParameter(LiteralExpressionSyntax parameterSyntax, bool updatePosition = true)
    {
        parameterSyntax.Parent = this;
        Parameter = parameterSyntax;

        if (updatePosition)
            Root.Update();
    }

    public void SetRelBigger(SyntaxToken relBigger, bool updatePosition = true)
    {
        relBigger.Parent = this;
        RelBigger = relBigger;

        if (updatePosition)
            Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        SyntaxToken relSmaller = RelSmaller;
        SyntaxToken relBigger = RelBigger;

        position = relSmaller.UpdatePosition(position, ref line, ref column);
        position = Parameter.UpdatePosition(position, ref line, ref column);
        position = relBigger.UpdatePosition(position, ref line, ref column);

        RelSmaller = relSmaller;
        RelBigger = relBigger;

        return position;
    }
}