using Logic.Domain.CodeAnalysis.Contract.DataClasses;

namespace Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

public class MethodInvocationParametersSyntax : SyntaxNode
{
    public SyntaxToken ParenOpen { get; private set; }
    public CommaSeparatedSyntaxList<ValueExpressionSyntax>? ParameterList { get; private set; }
    public SyntaxToken ParenClose { get; private set; }

    public override SyntaxLocation Location => ParenOpen.FullLocation;
    public override SyntaxSpan Span => new(ParenOpen.FullSpan.Position, ParenClose.FullSpan.EndPosition);

    public MethodInvocationParametersSyntax(SyntaxToken parenOpen,
        CommaSeparatedSyntaxList<ValueExpressionSyntax>? parameterList,
        SyntaxToken parenClose)
    {
        parenOpen.Parent = this;
        if (parameterList != null)
            parameterList.Parent = this;
        parenClose.Parent = this;

        ParenOpen = parenOpen;
        ParameterList = parameterList;
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

    public void SetParameterList(CommaSeparatedSyntaxList<ValueExpressionSyntax>? parameterList, bool updatePosition = true)
    {
        if (parameterList != null)
            parameterList.Parent = this;

        ParameterList = parameterList;

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
        if (ParameterList != null)
            position = ParameterList.UpdatePosition(position, ref line, ref column);
        position = parenClose.UpdatePosition(position, ref line, ref column);

        ParenOpen = parenOpen;
        ParenClose = parenClose;

        return position;
    }
}