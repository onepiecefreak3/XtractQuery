using Logic.Domain.CodeAnalysis.Contract.DataClasses;

namespace Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

public class DefaultSwitchCaseExpressionSyntax : SwitchCaseExpressionSyntax
{
    public SyntaxToken Underscore { get; private set; }
    public SyntaxToken ArrowRight { get; private set; }
    public ValueExpressionSyntax Value { get; private set; }

    public override SyntaxLocation Location => Underscore.FullLocation;
    public override SyntaxSpan Span => new(Underscore.FullSpan.Position, Value.Span.EndPosition);

    public DefaultSwitchCaseExpressionSyntax(SyntaxToken underscore, SyntaxToken arrowRight, ValueExpressionSyntax value)
    {
        underscore.Parent = this;
        arrowRight.Parent = this;
        value.Parent = this;

        Underscore = underscore;
        ArrowRight = arrowRight;
        Value = value;

        Root.Update();
    }

    public void SetUnderscore(SyntaxToken underscore, bool updatePositions = true)
    {
        underscore.Parent = this;

        Underscore = underscore;

        if (updatePositions)
            Root.Update();
    }

    public void SetArrowRight(SyntaxToken arrowRight, bool updatePositions = true)
    {
        arrowRight.Parent = this;

        ArrowRight = arrowRight;

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
        SyntaxToken underscore = Underscore;
        SyntaxToken arrowRight = ArrowRight;

        position = underscore.UpdatePosition(position, ref line, ref column);
        position = arrowRight.UpdatePosition(position, ref line, ref column);
        position = Value.UpdatePosition(position, ref line, ref column);

        Underscore = underscore;
        ArrowRight = arrowRight;

        return position;
    }
}