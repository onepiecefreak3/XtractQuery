using Logic.Domain.CodeAnalysis.Contract.DataClasses;

namespace Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

public class MethodDeclarationBodySyntax : SyntaxNode
{
    public SyntaxToken CurlyOpen { get; private set; }
    public IReadOnlyList<StatementSyntax> Expressions { get; private set; }
    public SyntaxToken CurlyClose { get; private set; }

    public override SyntaxLocation Location => CurlyOpen.FullLocation;
    public override SyntaxSpan Span => new(CurlyOpen.FullSpan.Position, CurlyClose.FullSpan.EndPosition);

    public MethodDeclarationBodySyntax(SyntaxToken curlyOpen, IReadOnlyList<StatementSyntax>? expressions, SyntaxToken curlyClose)
    {
        curlyOpen.Parent = this;
        curlyClose.Parent = this;

        CurlyOpen = curlyOpen;
        Expressions = expressions ?? new List<StatementSyntax>();
        CurlyClose = curlyClose;

        foreach (StatementSyntax expression in Expressions)
            expression.Parent = this;

        Root.Update();
    }

    public void SetCurlyOpen(SyntaxToken curlyOpen, bool updatePosition = true)
    {
        curlyOpen.Parent = this;
        CurlyOpen = curlyOpen;

        if (updatePosition)
            Root.Update();
    }

    public void SetExpressions(IReadOnlyList<StatementSyntax> expressions, bool updatePosition = true)
    {
        Expressions = expressions;
        foreach (StatementSyntax expression in Expressions)
            expression.Parent = this;

        if (updatePosition)
            Root.Update();
    }

    public void SetCurlyClose(SyntaxToken curlyClose, bool updatePosition = true)
    {
        curlyClose.Parent = this;
        CurlyClose = curlyClose;

        if (updatePosition)
            Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        SyntaxToken curlyOpen = CurlyOpen;
        SyntaxToken curlyClose = CurlyClose;

        position = curlyOpen.UpdatePosition(position, ref line, ref column);
        foreach (StatementSyntax expression in Expressions)
            position = expression.UpdatePosition(position, ref line, ref column);
        position = curlyClose.UpdatePosition(position, ref line, ref column);

        CurlyOpen = curlyOpen;
        CurlyClose = curlyClose;

        return position;
    }
}