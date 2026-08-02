using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

namespace Logic.Domain.CodeAnalysis.Contract.Level5;

public static class ExpressionPrecedence
{
    public const int Postfix = -2;
    public const int Unary = -1;
    public const int Mul = 0;
    public const int Add = 1;
    public const int Shift = 2;
    public const int Relational = 3;
    public const int Equality = 4;
    public const int BitAnd = 5;
    public const int BitXor = 6;
    public const int BitOr = 7;
    public const int LogicalAnd = 8;
    public const int LogicalOr = 9;
    public const int Primary = int.MinValue;
    public const int None = int.MaxValue;

    public static int? GetOperatorPrecedence(SyntaxTokenKind kind)
    {
        switch (kind)
        {
            case SyntaxTokenKind.Increment:
            case SyntaxTokenKind.Decrement:
                return Postfix;

            case SyntaxTokenKind.NotKeyword:
            case SyntaxTokenKind.Not:
            case SyntaxTokenKind.Complement:
                return Unary;

            case SyntaxTokenKind.Mul:
            case SyntaxTokenKind.Div:
            case SyntaxTokenKind.Mod:
                return Mul;

            case SyntaxTokenKind.Plus:
            case SyntaxTokenKind.Minus:
                return Add;

            case SyntaxTokenKind.LeftShift:
            case SyntaxTokenKind.RightShift:
                return Shift;

            case SyntaxTokenKind.Greater:
            case SyntaxTokenKind.GreaterEquals:
            case SyntaxTokenKind.Smaller:
            case SyntaxTokenKind.SmallerEquals:
                return Relational;

            case SyntaxTokenKind.Equals:
            case SyntaxTokenKind.NotEquals:
                return Equality;

            case SyntaxTokenKind.And:
                return BitAnd;

            case SyntaxTokenKind.Xor:
                return BitXor;

            case SyntaxTokenKind.Or:
                return BitOr;

            case SyntaxTokenKind.AndKeyword:
            case SyntaxTokenKind.AndAnd:
                return LogicalAnd;

            case SyntaxTokenKind.OrKeyword:
            case SyntaxTokenKind.OrOr:
                return LogicalOr;

            default:
                return null;
        }
    }

    public static int GetExpressionPrecedence(ExpressionSyntax expression)
    {
        switch (expression)
        {
            case ParenthesizedExpressionSyntax:
                return Primary;

            case PostfixUnaryExpressionSyntax:
                return Postfix;

            case UnaryExpressionSyntax:
            case TypeCastValueExpressionSyntax:
                return Unary;

            case BinaryExpressionSyntax binary:
                return GetOperatorPrecedence((SyntaxTokenKind)binary.Operation.RawKind) ?? Primary;

            case LogicalExpressionSyntax logical:
                return GetOperatorPrecedence((SyntaxTokenKind)logical.Operation.RawKind) ?? Primary;

            case AssignmentExpressionSyntax:
                return LogicalOr + 1;

            case ValueExpressionSyntax value:
                return GetExpressionPrecedence(value.Value);

            default:
                return Primary;
        }
    }

    public static bool NeedsParentheses(ExpressionSyntax expression, int parentPrecedence, bool isRightOperand)
    {
        if (parentPrecedence == None)
            return false;

        if (expression is ParenthesizedExpressionSyntax)
            return false;

        int expressionPrecedence = GetExpressionPrecedence(expression);
        if (expressionPrecedence == Primary)
            return false;

        if (expressionPrecedence > parentPrecedence)
            return true;

        return isRightOperand && expressionPrecedence == parentPrecedence;
    }
}
