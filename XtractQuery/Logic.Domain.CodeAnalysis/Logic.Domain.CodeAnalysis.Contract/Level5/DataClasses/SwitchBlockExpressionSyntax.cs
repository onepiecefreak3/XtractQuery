using Logic.Domain.CodeAnalysis.Contract.DataClasses;

namespace Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses;

public class SwitchBlockExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken CurlyOpen { get; private set; }
    public IReadOnlyList<SwitchCaseExpressionSyntax> Cases { get; private set; }
    public SyntaxToken CurlyClose { get; private set; }

    public override SyntaxLocation Location => CurlyClose.FullLocation;
    public override SyntaxSpan Span => new(CurlyOpen.FullSpan.Position, CurlyClose.FullSpan.EndPosition);

    public SwitchBlockExpressionSyntax(SyntaxToken curlyOpen, IReadOnlyList<SwitchCaseExpressionSyntax> cases, SyntaxToken curlyClose)
    {
        curlyOpen.Parent = this;
        foreach (var @case in cases)
            @case.Parent = this;
        curlyClose.Parent = this;

        CurlyOpen = curlyOpen;
        Cases = cases;
        CurlyClose = curlyClose;

        Root.Update();
    }

    public void SetCurlyOpen(SyntaxToken curlyOpen, bool updatePositions = true)
    {
        curlyOpen.Parent = this;

        CurlyOpen = curlyOpen;

        if (updatePositions)
            Root.Update();
    }

    public void SetCases(IReadOnlyList<SwitchCaseExpressionSyntax> cases, bool updatePositions = true)
    {
        foreach (var @case in cases)
            @case.Parent = this;

        Cases = cases;

        if (updatePositions)
            Root.Update();
    }

    public void SetCurlyClose(SyntaxToken curlyClose, bool updatePositions = true)
    {
        curlyClose.Parent = this;

        CurlyClose = curlyClose;

        if (updatePositions)
            Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        SyntaxToken curlyOpen = CurlyOpen;
        SyntaxToken curlyClose = CurlyClose;

        position = curlyOpen.UpdatePosition(position, ref line, ref column);
        foreach (var @case in Cases)
            position = @case.UpdatePosition(position, ref line, ref column);
        position = curlyClose.UpdatePosition(position, ref line, ref column);

        CurlyOpen = curlyOpen;
        CurlyClose = curlyClose;

        return position;
    }
}