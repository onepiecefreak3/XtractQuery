using Logic.Domain.CodeAnalysis.Contract.DataClasses;
using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;
using Logic.Domain.CodeAnalysis.Contract.Level5;

namespace Logic.Business.Level5ScriptManagement.Conversion;

internal static class ExpressionParenthesizer
{
    public static ExpressionSyntax MaybeParenthesize(
        ExpressionSyntax expression,
        int parentPrecedence,
        bool isRightOperand,
        ILevel5SyntaxFactory syntaxFactory)
    {
        if (!ExpressionPrecedence.NeedsParentheses(expression, parentPrecedence, isRightOperand))
            return expression;

        return new ParenthesizedExpressionSyntax(
            syntaxFactory.Token(SyntaxTokenKind.ParenOpen),
            expression,
            syntaxFactory.Token(SyntaxTokenKind.ParenClose));
    }

    public static ExpressionSyntax UnwrapParentheses(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
            expression = parenthesized.Expression;

        return expression;
    }
}
