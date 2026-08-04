namespace Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

public class ForStatementSyntax : StatementSyntax
{
    public SyntaxToken For { get; private set; }
    public SyntaxToken ParenOpen { get; private set; }
    public StatementSyntax? Initializer { get; private set; }
    public SyntaxToken? FirstSemicolon { get; private set; }
    public ExpressionSyntax Condition { get; private set; }
    public SyntaxToken SecondSemicolon { get; private set; }
    public StatementSyntax? Iterator { get; private set; }
    public SyntaxToken ParenClose { get; private set; }
    public BlockSyntax Body { get; private set; }

    public override SyntaxLocation Location => For.FullLocation;
    public override SyntaxSpan Span => new(For.FullSpan.Position, Body.Span.EndPosition);

    public ForStatementSyntax(
        SyntaxToken forToken,
        SyntaxToken parenOpen,
        StatementSyntax? initializer,
        SyntaxToken? firstSemicolon,
        ExpressionSyntax condition,
        SyntaxToken secondSemicolon,
        StatementSyntax? iterator,
        SyntaxToken parenClose,
        BlockSyntax body)
    {
        if (initializer is null == firstSemicolon is null)
            throw new ArgumentException("For statement requires either an initializer or a first semicolon.");

        forToken.Parent = this;
        parenOpen.Parent = this;
        if (initializer != null)
            initializer.Parent = this;
        if (firstSemicolon != null)
        {
            SyntaxToken first = firstSemicolon.Value;
            first.Parent = this;
            firstSemicolon = first;
        }
        condition.Parent = this;
        secondSemicolon.Parent = this;
        if (iterator != null)
            iterator.Parent = this;
        parenClose.Parent = this;
        body.Parent = this;

        For = forToken;
        ParenOpen = parenOpen;
        Initializer = initializer;
        FirstSemicolon = firstSemicolon;
        Condition = condition;
        SecondSemicolon = secondSemicolon;
        Iterator = iterator;
        ParenClose = parenClose;
        Body = body;

        Root.Update();
    }

    public void SetFor(SyntaxToken forToken, bool updatePosition = true)
    {
        forToken.Parent = this;
        For = forToken;

        if (updatePosition)
            Root.Update();
    }

    public void SetParenOpen(SyntaxToken parenOpen, bool updatePosition = true)
    {
        parenOpen.Parent = this;
        ParenOpen = parenOpen;

        if (updatePosition)
            Root.Update();
    }

    public void SetInitializer(StatementSyntax? initializer, bool updatePosition = true)
    {
        if (initializer != null)
            initializer.Parent = this;

        Initializer = initializer;

        if (updatePosition)
            Root.Update();
    }

    public void SetFirstSemicolon(SyntaxToken? firstSemicolon, bool updatePosition = true)
    {
        if (firstSemicolon != null)
        {
            SyntaxToken first = firstSemicolon.Value;
            first.Parent = this;
            FirstSemicolon = first;
        }
        else
        {
            FirstSemicolon = null;
        }

        if (updatePosition)
            Root.Update();
    }

    public void SetCondition(ExpressionSyntax condition, bool updatePosition = true)
    {
        condition.Parent = this;
        Condition = condition;

        if (updatePosition)
            Root.Update();
    }

    public void SetSecondSemicolon(SyntaxToken secondSemicolon, bool updatePosition = true)
    {
        secondSemicolon.Parent = this;
        SecondSemicolon = secondSemicolon;

        if (updatePosition)
            Root.Update();
    }

    public void SetIterator(StatementSyntax? iterator, bool updatePosition = true)
    {
        if (iterator != null)
            iterator.Parent = this;

        Iterator = iterator;

        if (updatePosition)
            Root.Update();
    }

    public void SetParenClose(SyntaxToken parenClose, bool updatePosition = true)
    {
        parenClose.Parent = this;
        ParenClose = parenClose;

        if (updatePosition)
            Root.Update();
    }

    public void SetBody(BlockSyntax body, bool updatePosition = true)
    {
        body.Parent = this;
        Body = body;

        if (updatePosition)
            Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        SyntaxToken forToken = For;
        SyntaxToken parenOpen = ParenOpen;
        SyntaxToken? firstSemicolon = FirstSemicolon;
        SyntaxToken secondSemicolon = SecondSemicolon;
        SyntaxToken parenClose = ParenClose;

        position = forToken.UpdatePosition(position, ref line, ref column);
        position = parenOpen.UpdatePosition(position, ref line, ref column);
        if (Initializer != null)
            position = Initializer.UpdatePosition(position, ref line, ref column);
        if (firstSemicolon != null)
        {
            SyntaxToken first = firstSemicolon.Value;
            position = first.UpdatePosition(position, ref line, ref column);
            firstSemicolon = first;
        }
        position = Condition.UpdatePosition(position, ref line, ref column);
        position = secondSemicolon.UpdatePosition(position, ref line, ref column);
        if (Iterator != null)
            position = Iterator.UpdatePosition(position, ref line, ref column);
        position = parenClose.UpdatePosition(position, ref line, ref column);
        position = Body.UpdatePosition(position, ref line, ref column);

        For = forToken;
        ParenOpen = parenOpen;
        FirstSemicolon = firstSemicolon;
        SecondSemicolon = secondSemicolon;
        ParenClose = parenClose;

        return position;
    }
}
