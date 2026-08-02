namespace Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

public class WhileStatementSyntax : StatementSyntax
{
    public SyntaxToken While { get; private set; }
    public SyntaxToken ParenOpen { get; private set; }
    public ExpressionSyntax Condition { get; private set; }
    public SyntaxToken ParenClose { get; private set; }
    public BlockSyntax? Body { get; private set; }
    public SyntaxToken? Semicolon { get; private set; }

    public override SyntaxLocation Location => While.FullLocation;

    public override SyntaxSpan Span
    {
        get
        {
            if (Body != null)
                return new(While.FullSpan.Position, Body.Span.EndPosition);

            return new(While.FullSpan.Position, Semicolon!.Value.FullSpan.EndPosition);
        }
    }

    public WhileStatementSyntax(
        SyntaxToken whileToken,
        SyntaxToken parenOpen,
        ExpressionSyntax condition,
        SyntaxToken parenClose,
        BlockSyntax? body,
        SyntaxToken? semicolon)
    {
        if (body is null == semicolon is null)
            throw new ArgumentException("While statement requires either a body or a semicolon.");

        whileToken.Parent = this;
        parenOpen.Parent = this;
        condition.Parent = this;
        parenClose.Parent = this;
        if (body != null)
            body.Parent = this;
        if (semicolon != null)
        {
            SyntaxToken semicolonToken = semicolon.Value;
            semicolonToken.Parent = this;
            semicolon = semicolonToken;
        }

        While = whileToken;
        ParenOpen = parenOpen;
        Condition = condition;
        ParenClose = parenClose;
        Body = body;
        Semicolon = semicolon;

        Root.Update();
    }

    public void SetWhile(SyntaxToken whileToken, bool updatePosition = true)
    {
        whileToken.Parent = this;
        While = whileToken;

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

    public void SetCondition(ExpressionSyntax condition, bool updatePosition = true)
    {
        condition.Parent = this;
        Condition = condition;

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

    public void SetBody(BlockSyntax? body, bool updatePosition = true)
    {
        if (body != null)
            body.Parent = this;

        Body = body;

        if (updatePosition)
            Root.Update();
    }

    public void SetSemicolon(SyntaxToken? semicolon, bool updatePosition = true)
    {
        if (semicolon != null)
        {
            SyntaxToken semicolonToken = semicolon.Value;
            semicolonToken.Parent = this;
            Semicolon = semicolonToken;
        }
        else
        {
            Semicolon = null;
        }

        if (updatePosition)
            Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        SyntaxToken whileToken = While;
        SyntaxToken parenOpen = ParenOpen;
        SyntaxToken parenClose = ParenClose;
        SyntaxToken? semicolon = Semicolon;

        position = whileToken.UpdatePosition(position, ref line, ref column);
        position = parenOpen.UpdatePosition(position, ref line, ref column);
        position = Condition.UpdatePosition(position, ref line, ref column);
        position = parenClose.UpdatePosition(position, ref line, ref column);
        if (Body != null)
            position = Body.UpdatePosition(position, ref line, ref column);
        if (semicolon != null)
        {
            SyntaxToken semicolonToken = semicolon.Value;
            position = semicolonToken.UpdatePosition(position, ref line, ref column);
            semicolon = semicolonToken;
        }

        While = whileToken;
        ParenOpen = parenOpen;
        ParenClose = parenClose;
        Semicolon = semicolon;

        return position;
    }
}
