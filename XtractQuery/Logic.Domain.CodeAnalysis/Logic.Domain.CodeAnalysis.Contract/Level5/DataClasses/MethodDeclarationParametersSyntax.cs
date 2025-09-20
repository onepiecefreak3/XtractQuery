using Logic.Domain.CodeAnalysis.Contract.DataClasses;

namespace Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses;

public class MethodDeclarationParametersSyntax : SyntaxNode
{
    public SyntaxToken ParenOpen { get; private set; }
    public CommaSeparatedSyntaxList<VariableExpressionSyntax>? Parameters { get; private set; }
    public SyntaxToken ParenClose { get; private set; }

    public override SyntaxLocation Location => ParenOpen.FullLocation;
    public override SyntaxSpan Span => new(ParenOpen.FullSpan.Position, ParenClose.FullSpan.EndPosition);

    public MethodDeclarationParametersSyntax(SyntaxToken parenOpen, CommaSeparatedSyntaxList<VariableExpressionSyntax>? parameters, SyntaxToken parenClose)
    {
        parenOpen.Parent = this;
        if (parameters != null)
            parameters.Parent = this;
        parenClose.Parent = this;

        ParenOpen = parenOpen;
        Parameters = parameters;
        ParenClose = parenClose;

        Root.Update();
    }

    public void SetParenOpen(SyntaxToken parenOpen, bool updatePosition = true)
    {
        parenOpen.Parent = this;
        ParenOpen = parenOpen;

        if (updatePosition)
            Root.Update();
    }

    public void SetParameters(CommaSeparatedSyntaxList<VariableExpressionSyntax>? parameters, bool updatePosition = true)
    {
        if (parameters != null)
            parameters.Parent = this;

        Parameters = parameters;

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

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        SyntaxToken parenOpen = ParenOpen;
        SyntaxToken parenClose = ParenClose;

        position = parenOpen.UpdatePosition(position, ref line, ref column);
        if (Parameters != null)
            position = Parameters.UpdatePosition(position, ref line, ref column);
        position = parenClose.UpdatePosition(position, ref line, ref column);

        ParenOpen = parenOpen;
        ParenClose = parenClose;

        return position;
    }
}