namespace Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

public class GlobalDeclarationStatementSyntax : CodeUnitMemberSyntax
{
    public SyntaxToken GlobalKeyword { get; private set; }
    public CommaSeparatedSyntaxList<VariableExpressionSyntax> Variables { get; private set; }
    public SyntaxToken Semicolon { get; private set; }

    public override SyntaxLocation Location => GlobalKeyword.FullLocation;
    public override SyntaxSpan Span => new(GlobalKeyword.FullSpan.Position, Semicolon.FullSpan.EndPosition);

    public GlobalDeclarationStatementSyntax(
        SyntaxToken globalKeyword,
        CommaSeparatedSyntaxList<VariableExpressionSyntax> variables,
        SyntaxToken semicolon)
    {
        globalKeyword.Parent = this;
        variables.Parent = this;
        semicolon.Parent = this;

        GlobalKeyword = globalKeyword;
        Variables = variables;
        Semicolon = semicolon;

        Root.Update();
    }

    public void SetGlobalKeyword(SyntaxToken globalKeyword, bool updatePosition = true)
    {
        globalKeyword.Parent = this;
        GlobalKeyword = globalKeyword;

        if (updatePosition)
            Root.Update();
    }

    public void SetVariables(CommaSeparatedSyntaxList<VariableExpressionSyntax> variables, bool updatePosition = true)
    {
        variables.Parent = this;
        Variables = variables;

        if (updatePosition)
            Root.Update();
    }

    public void SetSemicolon(SyntaxToken semicolon, bool updatePosition = true)
    {
        semicolon.Parent = this;
        Semicolon = semicolon;

        if (updatePosition)
            Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        SyntaxToken globalKeyword = GlobalKeyword;
        SyntaxToken semicolon = Semicolon;

        position = globalKeyword.UpdatePosition(position, ref line, ref column);
        position = Variables.UpdatePosition(position, ref line, ref column);
        position = semicolon.UpdatePosition(position, ref line, ref column);

        GlobalKeyword = globalKeyword;
        Semicolon = semicolon;

        return position;
    }
}
