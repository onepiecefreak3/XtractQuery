using Logic.Domain.CodeAnalysis.Contract.DataClasses;

namespace Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

public class LiteralSwitchCaseExpressionSyntax : SwitchCaseExpressionSyntax
{
    public ValueExpressionSyntax CaseValue { get; private set; }
    public SyntaxToken ArrowRight { get; private set; }
    public ValueExpressionSyntax Value { get; private set; }

    public override SyntaxLocation Location => CaseValue.Location;
    public override SyntaxSpan Span => new(CaseValue.Span.Position, Value.Span.EndPosition);

    public LiteralSwitchCaseExpressionSyntax(ValueExpressionSyntax caseValue, SyntaxToken arrowRight, ValueExpressionSyntax value)
    {
        caseValue.Parent = this;
        arrowRight.Parent = this;
        value.Parent = this;

        CaseValue = caseValue;
        ArrowRight = arrowRight;
        Value = value;

        Root.Update();
    }

    public void SetCaseValue(ValueExpressionSyntax caseValue, bool updatePositions = true)
    {
        caseValue.Parent = this;

        CaseValue = caseValue;

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
        SyntaxToken arrowRight = ArrowRight;

        position = CaseValue.UpdatePosition(position, ref line, ref column);
        position = arrowRight.UpdatePosition(position, ref line, ref column);
        position = Value.UpdatePosition(position, ref line, ref column);
            
        ArrowRight = arrowRight;

        return position;
    }
}