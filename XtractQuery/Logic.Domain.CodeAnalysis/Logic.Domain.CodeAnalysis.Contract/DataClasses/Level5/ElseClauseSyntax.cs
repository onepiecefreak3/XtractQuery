namespace Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

public class ElseClauseSyntax : SyntaxNode
{
    public SyntaxToken ElseKeyword { get; private set; }
    public StatementSyntax Statement { get; private set; }

    public override SyntaxLocation Location => ElseKeyword.FullLocation;
    public override SyntaxSpan Span => new(ElseKeyword.FullSpan.Position, Statement.Span.EndPosition);

    public ElseClauseSyntax(SyntaxToken elseKeyword, StatementSyntax statement)
    {
        elseKeyword.Parent = this;
        statement.Parent = this;

        ElseKeyword = elseKeyword;
        Statement = statement;

        Root.Update();
    }

    public void SetElseKeyword(SyntaxToken elseKeyword, bool updatePosition = true)
    {
        elseKeyword.Parent = this;
        ElseKeyword = elseKeyword;

        if (updatePosition)
            Root.Update();
    }

    public void SetStatement(StatementSyntax statement, bool updatePosition = true)
    {
        statement.Parent = this;
        Statement = statement;

        if (updatePosition)
            Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        SyntaxToken elseKeyword = ElseKeyword;

        position = elseKeyword.UpdatePosition(position, ref line, ref column);
        position = Statement.UpdatePosition(position, ref line, ref column);

        ElseKeyword = elseKeyword;

        return position;
    }
}
