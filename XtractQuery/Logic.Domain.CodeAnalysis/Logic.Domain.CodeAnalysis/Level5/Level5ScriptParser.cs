using Logic.Domain.CodeAnalysis.Contract;
using Logic.Domain.CodeAnalysis.Contract.DataClasses;
using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;
using Logic.Domain.CodeAnalysis.Contract.Exceptions.Level5;
using Logic.Domain.CodeAnalysis.Contract.Level5;
using Logic.Domain.CodeAnalysis.DataClasses.Level5;

namespace Logic.Domain.CodeAnalysis.Level5;

internal class Level5ScriptParser(ITokenFactory<Level5SyntaxToken> scriptFactory, ILevel5SyntaxFactory syntaxFactory)
    : ILevel5ScriptParser
{
    public CodeUnitSyntax ParseCodeUnit(string text)
    {
        IBuffer<Level5SyntaxToken> buffer = CreateTokenBuffer(text);

        return ParseCodeUnit(buffer);
    }

    public MethodDeclarationSyntax ParseMethodDeclaration(string text)
    {
        IBuffer<Level5SyntaxToken> buffer = CreateTokenBuffer(text);

        return ParseMethodDeclaration(buffer);
    }

    public MethodDeclarationMetadataParametersSyntax? ParseMethodDeclarationMetadataParameters(string text)
    {
        IBuffer<Level5SyntaxToken> buffer = CreateTokenBuffer(text);

        return ParseMethodDeclarationMetadataParameters(buffer);
    }

    public MethodDeclarationMetadataParameterListSyntax ParseMethodDeclarationMetadataParameterList(string text)
    {
        IBuffer<Level5SyntaxToken> buffer = CreateTokenBuffer(text);

        return ParseMethodDeclarationMetadataParameterList(buffer);
    }

    public MethodDeclarationParametersSyntax ParseMethodDeclarationParameters(string text)
    {
        IBuffer<Level5SyntaxToken> buffer = CreateTokenBuffer(text);

        return ParseMethodDeclarationParameters(buffer);
    }

    public MethodDeclarationBodySyntax ParseMethodDeclarationBody(string text)
    {
        IBuffer<Level5SyntaxToken> buffer = CreateTokenBuffer(text);

        return ParseMethodDeclarationBody(buffer);
    }

    public StatementSyntax ParseStatement(string text)
    {
        IBuffer<Level5SyntaxToken> buffer = CreateTokenBuffer(text);

        return ParseStatement(buffer);
    }

    public GotoLabelStatementSyntax ParseGotoLabelStatement(string text)
    {
        IBuffer<Level5SyntaxToken> buffer = CreateTokenBuffer(text);

        return ParseGotoLabelStatement(buffer);
    }

    public ReturnStatementSyntax ParseReturnStatement(string text)
    {
        IBuffer<Level5SyntaxToken> buffer = CreateTokenBuffer(text);

        return ParseReturnStatement(buffer);
    }

    public MethodInvocationExpressionSyntax ParseMethodInvocationExpression(string text)
    {
        IBuffer<Level5SyntaxToken> buffer = CreateTokenBuffer(text);

        return ParseMethodInvocationExpression(buffer);
    }

    public MethodInvocationStatementSyntax ParseMethodInvocationStatement(string text)
    {
        IBuffer<Level5SyntaxToken> buffer = CreateTokenBuffer(text);

        return ParseMethodInvocationStatement(buffer);
    }

    public MethodInvocationParametersSyntax ParseMethodInvocationParameters(string text)
    {
        IBuffer<Level5SyntaxToken> buffer = CreateTokenBuffer(text);

        return ParseMethodInvocationParameters(buffer);
    }

    public CommaSeparatedSyntaxList<ExpressionSyntax>? ParseMethodInvocationParameterList(string text)
    {
        IBuffer<Level5SyntaxToken> buffer = CreateTokenBuffer(text);

        return ParseMethodInvocationParameterList(buffer);
    }

    public ValueExpressionSyntax ParseValueExpression(string text)
    {
        IBuffer<Level5SyntaxToken> buffer = CreateTokenBuffer(text);

        return ParseValueExpression(buffer);
    }

    public ValueMetadataParametersSyntax? ParseValueMetadataParameters(string text)
    {
        IBuffer<Level5SyntaxToken> buffer = CreateTokenBuffer(text);

        return ParseValueMetadataParameters(buffer);
    }


    private CodeUnitSyntax ParseCodeUnit(IBuffer<Level5SyntaxToken> buffer)
    {
        var members = ParseCodeUnitMembers(buffer);

        return new CodeUnitSyntax(members);
    }

    private IReadOnlyList<CodeUnitMemberSyntax> ParseCodeUnitMembers(IBuffer<Level5SyntaxToken> buffer)
    {
        var result = new List<CodeUnitMemberSyntax>();

        while (buffer.Peek().Kind != SyntaxTokenKind.EndOfFile)
        {
            if (HasTokenKind(buffer, SyntaxTokenKind.GlobalKeyword))
                result.Add(ParseGlobalDeclarationStatement(buffer));
            else
                result.Add(ParseMethodDeclaration(buffer));
        }

        return result;
    }

    private GlobalDeclarationStatementSyntax ParseGlobalDeclarationStatement(IBuffer<Level5SyntaxToken> buffer)
    {
        SyntaxToken globalKeyword = ParseGlobalKeywordToken(buffer);
        var variables = ParseGlobalDeclarationVariableList(buffer);
        SyntaxToken semicolon = ParseSemicolonToken(buffer);

        return new GlobalDeclarationStatementSyntax(globalKeyword, variables, semicolon);
    }

    private CommaSeparatedSyntaxList<VariableExpressionSyntax> ParseGlobalDeclarationVariableList(
        IBuffer<Level5SyntaxToken> buffer)
    {
        var result = new List<VariableExpressionSyntax>();

        if (!HasTokenKind(buffer, SyntaxTokenKind.Variable))
            throw CreateException(buffer, "Invalid global declaration variable list.", SyntaxTokenKind.Variable);

        result.Add(ParseVariableExpression(buffer));

        while (HasTokenKind(buffer, SyntaxTokenKind.Comma))
        {
            SkipTokenKind(buffer, SyntaxTokenKind.Comma);

            if (!HasTokenKind(buffer, SyntaxTokenKind.Variable))
                throw CreateException(buffer, "Invalid end of global declaration variable list.", SyntaxTokenKind.Variable);

            result.Add(ParseVariableExpression(buffer));
        }

        return new CommaSeparatedSyntaxList<VariableExpressionSyntax>(result);
    }

    private IReadOnlyList<MethodDeclarationSyntax> ParseMethodDeclarations(IBuffer<Level5SyntaxToken> buffer)
    {
        var result = new List<MethodDeclarationSyntax>();

        while (buffer.Peek().Kind != SyntaxTokenKind.EndOfFile)
            result.Add(ParseMethodDeclaration(buffer));

        return result;
    }

    private MethodDeclarationSyntax ParseMethodDeclaration(IBuffer<Level5SyntaxToken> buffer)
    {
        SyntaxToken identifier = ParseIdentifierToken(buffer);
        var metadataParameters = ParseMethodDeclarationMetadataParameters(buffer);
        var parameters = ParseMethodDeclarationParameters(buffer);
        var body = ParseMethodDeclarationBody(buffer);

        return new MethodDeclarationSyntax(identifier, metadataParameters, parameters, body);
    }

    private MethodDeclarationMetadataParametersSyntax? ParseMethodDeclarationMetadataParameters(IBuffer<Level5SyntaxToken> buffer)
    {
        if (!HasTokenKind(buffer, SyntaxTokenKind.Smaller))
            return null;

        SyntaxToken relSmallerToken = ParseSmallerToken(buffer);
        var parameterList = ParseMethodDeclarationMetadataParameterList(buffer);
        SyntaxToken relBiggerToken = ParseGreaterToken(buffer);

        return new MethodDeclarationMetadataParametersSyntax(relSmallerToken, parameterList, relBiggerToken);
    }

    private MethodDeclarationMetadataParameterListSyntax ParseMethodDeclarationMetadataParameterList(IBuffer<Level5SyntaxToken> buffer)
    {
        var parameter1 = ParseNumericLiteralExpression(buffer);
        SyntaxToken commaToken = ParseCommaToken(buffer);
        var parameter2 = ParseNumericLiteralExpression(buffer);

        return new MethodDeclarationMetadataParameterListSyntax(parameter1, commaToken, parameter2);
    }

    private MethodDeclarationParametersSyntax ParseMethodDeclarationParameters(IBuffer<Level5SyntaxToken> buffer)
    {
        SyntaxToken parenOpenToken = ParseParenOpenToken(buffer);
        var parameterList = ParseMethodDeclarationParameterList(buffer);
        SyntaxToken parenCloseToken = ParseParenCloseToken(buffer);

        return new MethodDeclarationParametersSyntax(parenOpenToken, parameterList, parenCloseToken);
    }

    private CommaSeparatedSyntaxList<VariableExpressionSyntax>? ParseMethodDeclarationParameterList(IBuffer<Level5SyntaxToken> buffer)
    {
        var result = new List<VariableExpressionSyntax>();

        if (!HasTokenKind(buffer, SyntaxTokenKind.Variable))
            return null;

        VariableExpressionSyntax variable = ParseVariableExpression(buffer);
        result.Add(variable);

        while (HasTokenKind(buffer, SyntaxTokenKind.Comma))
        {
            SkipTokenKind(buffer, SyntaxTokenKind.Comma);

            if (!HasTokenKind(buffer, SyntaxTokenKind.Variable))
                throw CreateException(buffer, "Invalid end of parameter list.", SyntaxTokenKind.Variable);

            variable = ParseVariableExpression(buffer);
            result.Add(variable);
        }

        return new CommaSeparatedSyntaxList<VariableExpressionSyntax>(result);
    }

    private MethodDeclarationBodySyntax ParseMethodDeclarationBody(IBuffer<Level5SyntaxToken> buffer)
    {
        SyntaxToken curlyOpenToken = ParseCurlyOpenToken(buffer);
        var expressions = ParseStatements(buffer);
        SyntaxToken curlyCloseToken = ParseCurlyCloseToken(buffer);

        return new MethodDeclarationBodySyntax(curlyOpenToken, expressions, curlyCloseToken);
    }

    private IReadOnlyList<StatementSyntax> ParseStatements(IBuffer<Level5SyntaxToken> buffer)
    {
        var result = new List<StatementSyntax>();

        while (IsStatement(buffer))
            result.Add(ParseStatement(buffer));

        return result;
    }

    private bool IsStatement(IBuffer<Level5SyntaxToken> buffer)
    {
        return HasTokenKind(buffer, SyntaxTokenKind.StringLiteral) ||
               HasTokenKind(buffer, SyntaxTokenKind.Variable) ||
               HasTokenKind(buffer, SyntaxTokenKind.ReturnKeyword) ||
               HasTokenKind(buffer, SyntaxTokenKind.YieldKeyword) ||
               HasTokenKind(buffer, SyntaxTokenKind.ExitKeyword) ||
               HasTokenKind(buffer, SyntaxTokenKind.GotoKeyword) ||
               HasTokenKind(buffer, SyntaxTokenKind.IfKeyword) ||
               HasTokenKind(buffer, SyntaxTokenKind.WhileKeyword) ||
               HasTokenKind(buffer, SyntaxTokenKind.ForKeyword) ||
               HasTokenKind(buffer, SyntaxTokenKind.DoKeyword) ||
               HasTokenKind(buffer, SyntaxTokenKind.BreakKeyword) ||
               HasTokenKind(buffer, SyntaxTokenKind.ContinueKeyword) ||
               IsMethodInvocation(buffer);
    }

    private bool IsMethodInvocation(IBuffer<Level5SyntaxToken> buffer)
    {
        return HasTokenKind(buffer, SyntaxTokenKind.Identifier) &&
               (HasTokenKind(buffer, 1, SyntaxTokenKind.ParenOpen) ||
                HasTokenKind(buffer, 1, SyntaxTokenKind.Dot) ||
                HasTokenKind(buffer, 1, SyntaxTokenKind.Smaller));
    }

    private StatementSyntax ParseStatement(IBuffer<Level5SyntaxToken> buffer)
    {
        if (HasTokenKind(buffer, SyntaxTokenKind.YieldKeyword))
            return ParseYieldStatement(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.ReturnKeyword))
            return ParseReturnStatement(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.ExitKeyword))
            return ParseExitStatement(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.BreakKeyword))
            return ParseBreakStatement(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.ContinueKeyword))
            return ParseContinueStatement(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.StringLiteral))
            return ParseGotoLabelStatement(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.GotoKeyword))
            return ParseGotoStatement(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.IfKeyword))
            return ParseIfStatement(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.WhileKeyword))
            return ParseWhileStatement(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.ForKeyword))
            return ParseForStatement(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.DoKeyword))
            return ParseDoWhileStatement(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.Variable))
        {
            ExpressionSyntax value = ParseExpression(buffer);

            if (IsPostfixUnaryStatement(buffer))
                return ParsePostfixUnaryStatement(buffer, value);

            return ParseAssignmentStatement(buffer, value);
        }

        if (IsMethodInvocation(buffer))
            return ParseMethodInvocationStatement(buffer);

        throw CreateException(buffer, "Unknown statement.", SyntaxTokenKind.ReturnKeyword, SyntaxTokenKind.StringLiteral,
            SyntaxTokenKind.Variable, SyntaxTokenKind.YieldKeyword, SyntaxTokenKind.ExitKeyword,
            SyntaxTokenKind.WhileKeyword, SyntaxTokenKind.ForKeyword, SyntaxTokenKind.DoKeyword,
            SyntaxTokenKind.BreakKeyword, SyntaxTokenKind.ContinueKeyword);
    }

    private bool IsPostfixUnaryStatement(IBuffer<Level5SyntaxToken> buffer)
    {
        return HasTokenKind(buffer, SyntaxTokenKind.Increment) ||
               HasTokenKind(buffer, SyntaxTokenKind.Decrement);
    }

    private PostfixUnaryStatementSyntax ParsePostfixUnaryStatement(IBuffer<Level5SyntaxToken> buffer, ExpressionSyntax value)
    {
        var expression = ParsePostfixUnaryExpression(buffer, value);
        SyntaxToken semicolon = ParseSemicolonToken(buffer);

        return new PostfixUnaryStatementSyntax(expression, semicolon);
    }

    private PostfixUnaryExpressionSyntax ParsePostfixUnaryExpression(IBuffer<Level5SyntaxToken> buffer, ExpressionSyntax value)
    {
        if (HasTokenKind(buffer, SyntaxTokenKind.Decrement))
            return new PostfixUnaryExpressionSyntax(value, ParseMinusMinusToken(buffer));

        if (HasTokenKind(buffer, SyntaxTokenKind.Increment))
            return new PostfixUnaryExpressionSyntax(value, ParsePlusPlusToken(buffer));

        throw CreateException(buffer, "Unknown postfix unary expression.", SyntaxTokenKind.Decrement, SyntaxTokenKind.Increment);
    }

    private StatementSyntax ParseIfStatement(IBuffer<Level5SyntaxToken> buffer)
    {
        SyntaxToken ifToken = ParseIfKeywordToken(buffer);
        ExpressionSyntax condition = ParseExpression(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.GotoKeyword))
        {
            if (condition is UnaryExpressionSyntax unary &&
                unary.Operation.RawKind is (int)SyntaxTokenKind.NotKeyword or (int)SyntaxTokenKind.Not)
                return new IfNotGotoStatementSyntax(ifToken, unary, ParseGotoExpression(buffer), ParseSemicolonToken(buffer));

            return new IfGotoStatementSyntax(ifToken, condition, ParseGotoExpression(buffer), ParseSemicolonToken(buffer));
        }

        if (HasTokenKind(buffer, SyntaxTokenKind.CurlyOpen))
        {
            BlockSyntax body = ParseBlock(buffer);
            ElseClauseSyntax? elseClause = null;
            if (HasTokenKind(buffer, SyntaxTokenKind.ElseKeyword))
                elseClause = ParseElseClause(buffer);

            return new IfStatementSyntax(ifToken, condition, body, elseClause);
        }

        throw CreateException(buffer, "Invalid if statement.", SyntaxTokenKind.GotoKeyword, SyntaxTokenKind.CurlyOpen);
    }

    private WhileStatementSyntax ParseWhileStatement(IBuffer<Level5SyntaxToken> buffer)
    {
        SyntaxToken whileToken = ParseWhileKeywordToken(buffer);
        SyntaxToken parenOpen = ParseParenOpenToken(buffer);
        ExpressionSyntax condition = ParseExpression(buffer);
        SyntaxToken parenClose = ParseParenCloseToken(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.Semicolon))
            return new WhileStatementSyntax(whileToken, parenOpen, condition, parenClose, null, ParseSemicolonToken(buffer));

        if (HasTokenKind(buffer, SyntaxTokenKind.CurlyOpen))
            return new WhileStatementSyntax(whileToken, parenOpen, condition, parenClose, ParseBlock(buffer), null);

        throw CreateException(buffer, "Invalid while statement.", SyntaxTokenKind.Semicolon, SyntaxTokenKind.CurlyOpen);
    }

    private ForStatementSyntax ParseForStatement(IBuffer<Level5SyntaxToken> buffer)
    {
        SyntaxToken forToken = ParseForKeywordToken(buffer);
        SyntaxToken parenOpen = ParseParenOpenToken(buffer);

        StatementSyntax? initializer = null;
        SyntaxToken? firstSemicolon = null;
        if (HasTokenKind(buffer, SyntaxTokenKind.Semicolon))
        {
            firstSemicolon = ParseSemicolonToken(buffer);
        }
        else
        {
            initializer = ParseForClauseStatement(buffer, requireSemicolon: true);
            if (initializer is not (AssignmentStatementSyntax or PostfixUnaryStatementSyntax))
                throw CreateException(buffer, "Invalid for initializer.", SyntaxTokenKind.Variable);
        }

        ExpressionSyntax condition = ParseExpression(buffer);
        SyntaxToken secondSemicolon = ParseSemicolonToken(buffer);

        StatementSyntax? iterator = null;
        if (!HasTokenKind(buffer, SyntaxTokenKind.ParenClose))
            iterator = ParseForClauseStatement(buffer, requireSemicolon: false);

        SyntaxToken parenClose = ParseParenCloseToken(buffer);
        BlockSyntax body = ParseBlock(buffer);

        return new ForStatementSyntax(
            forToken, parenOpen, initializer, firstSemicolon, condition, secondSemicolon, iterator, parenClose, body);
    }

    private StatementSyntax ParseForClauseStatement(IBuffer<Level5SyntaxToken> buffer, bool requireSemicolon)
    {
        if (!HasTokenKind(buffer, SyntaxTokenKind.Variable))
            throw CreateException(buffer, "Invalid for clause.", SyntaxTokenKind.Variable);

        ExpressionSyntax value = ParseExpression(buffer);

        if (IsPostfixUnaryStatement(buffer))
        {
            PostfixUnaryExpressionSyntax expression = ParsePostfixUnaryExpression(buffer, value);
            SyntaxToken semicolon = requireSemicolon
                ? ParseSemicolonToken(buffer)
                : CreateEmptySemicolon();
            return new PostfixUnaryStatementSyntax(expression, semicolon);
        }

        return ParseAssignmentStatement(buffer, value, requireSemicolon);
    }

    private static SyntaxToken CreateEmptySemicolon()
    {
        return new SyntaxToken(string.Empty, (int)SyntaxTokenKind.Semicolon);
    }

    private DoWhileStatementSyntax ParseDoWhileStatement(IBuffer<Level5SyntaxToken> buffer)
    {
        SyntaxToken doToken = ParseDoKeywordToken(buffer);
        BlockSyntax body = ParseBlock(buffer);
        SyntaxToken whileToken = ParseWhileKeywordToken(buffer);
        SyntaxToken parenOpen = ParseParenOpenToken(buffer);
        ExpressionSyntax condition = ParseExpression(buffer);
        SyntaxToken parenClose = ParseParenCloseToken(buffer);
        SyntaxToken semicolon = ParseSemicolonToken(buffer);

        return new DoWhileStatementSyntax(doToken, body, whileToken, parenOpen, condition, parenClose, semicolon);
    }

    private BreakStatementSyntax ParseBreakStatement(IBuffer<Level5SyntaxToken> buffer)
    {
        return new BreakStatementSyntax(ParseBreakKeywordToken(buffer), ParseSemicolonToken(buffer));
    }

    private ContinueStatementSyntax ParseContinueStatement(IBuffer<Level5SyntaxToken> buffer)
    {
        return new ContinueStatementSyntax(ParseContinueKeywordToken(buffer), ParseSemicolonToken(buffer));
    }

    private ElseClauseSyntax ParseElseClause(IBuffer<Level5SyntaxToken> buffer)
    {
        SyntaxToken elseKeyword = ParseElseKeywordToken(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.IfKeyword))
            return new ElseClauseSyntax(elseKeyword, ParseIfStatement(buffer));

        if (HasTokenKind(buffer, SyntaxTokenKind.CurlyOpen))
            return new ElseClauseSyntax(elseKeyword, ParseBlock(buffer));

        throw CreateException(buffer, "Invalid else clause.", SyntaxTokenKind.IfKeyword, SyntaxTokenKind.CurlyOpen);
    }

    private BlockSyntax ParseBlock(IBuffer<Level5SyntaxToken> buffer)
    {
        SyntaxToken curlyOpen = ParseCurlyOpenToken(buffer);
        IReadOnlyList<StatementSyntax> statements = ParseStatements(buffer);
        SyntaxToken curlyClose = ParseCurlyCloseToken(buffer);

        return new BlockSyntax(curlyOpen, statements, curlyClose);
    }

    private GotoExpressionSyntax ParseGotoExpression(IBuffer<Level5SyntaxToken> buffer)
    {
        SyntaxToken gotoToken = ParseGotoKeywordToken(buffer);
        var value = ParseValueExpression(buffer);

        return new GotoExpressionSyntax(gotoToken, value);
    }

    private GotoStatementSyntax ParseGotoStatement(IBuffer<Level5SyntaxToken> buffer)
    {
        SyntaxToken gotoToken = ParseGotoKeywordToken(buffer);
        var labelList = ParseGotoLabelList(buffer);
        if (labelList is null)
            throw CreateException(buffer, "Could not parse goto statement");
        SyntaxToken semicolon = ParseSemicolonToken(buffer);

        return new GotoStatementSyntax(gotoToken, labelList, semicolon);
    }

    private CommaSeparatedSyntaxList<ValueExpressionSyntax>? ParseGotoLabelList(IBuffer<Level5SyntaxToken> buffer)
    {
        if (!IsValueExpression(buffer))
            return null;

        var result = new List<ValueExpressionSyntax>();

        ValueExpressionSyntax parameter = ParseValueExpression(buffer);
        result.Add(parameter);

        while (HasTokenKind(buffer, SyntaxTokenKind.Comma))
        {
            SkipTokenKind(buffer, SyntaxTokenKind.Comma);

            if (!IsValueExpression(buffer))
                throw CreateException(buffer, "Invalid end of parameter list.", SyntaxTokenKind.Variable,
                    SyntaxTokenKind.StringLiteral, SyntaxTokenKind.NumericLiteral, SyntaxTokenKind.UnsignedNumericLiteral,
                    SyntaxTokenKind.HashNumericLiteral, SyntaxTokenKind.HashStringLiteral,
                    SyntaxTokenKind.FloatingNumericLiteral);

            parameter = ParseValueExpression(buffer);
            result.Add(parameter);
        }

        return new CommaSeparatedSyntaxList<ValueExpressionSyntax>(result);
    }

    private YieldStatementSyntax ParseYieldStatement(IBuffer<Level5SyntaxToken> buffer)
    {
        SyntaxToken yieldToken = ParseYieldKeywordToken(buffer);
        SyntaxToken semicolon = ParseSemicolonToken(buffer);

        return new YieldStatementSyntax(yieldToken, semicolon);
    }

    private ReturnStatementSyntax ParseReturnStatement(IBuffer<Level5SyntaxToken> buffer)
    {
        SyntaxToken returnToken = ParseReturnKeywordToken(buffer);
        ExpressionSyntax? valueExpression = null;
        if (!HasTokenKind(buffer, SyntaxTokenKind.Semicolon))
            valueExpression = ParseExpression(buffer);
        SyntaxToken semicolon = ParseSemicolonToken(buffer);

        return new ReturnStatementSyntax(returnToken, valueExpression, semicolon);
    }

    private ExitStatementSyntax ParseExitStatement(IBuffer<Level5SyntaxToken> buffer)
    {
        SyntaxToken exitToken = ParseExitKeywordToken(buffer);
        SyntaxToken semicolon = ParseSemicolonToken(buffer);

        return new ExitStatementSyntax(exitToken, semicolon);
    }

    private GotoLabelStatementSyntax ParseGotoLabelStatement(IBuffer<Level5SyntaxToken> buffer)
    {
        LiteralExpressionSyntax identifier = ParseStringLiteralExpression(buffer);
        SyntaxToken colon = ParseColonToken(buffer);

        return new GotoLabelStatementSyntax(identifier, colon);
    }

    private AssignmentStatementSyntax ParseAssignmentStatement(
        IBuffer<Level5SyntaxToken> buffer,
        ExpressionSyntax value,
        bool requireSemicolon = true)
    {
        SyntaxToken equalsOperator;
        switch (buffer.Peek().Kind)
        {
            case SyntaxTokenKind.EqualsSign:
                equalsOperator = ParseEqualsSignToken(buffer);
                break;

            case SyntaxTokenKind.PlusEquals:
                equalsOperator = ParsePlusEqualsToken(buffer);
                break;

            case SyntaxTokenKind.MinusEquals:
                equalsOperator = ParseMinusEqualsToken(buffer);
                break;

            case SyntaxTokenKind.MulEquals:
                equalsOperator = ParseMulEqualsToken(buffer);
                break;

            case SyntaxTokenKind.DivEquals:
                equalsOperator = ParseDivEqualsToken(buffer);
                break;

            case SyntaxTokenKind.ModEquals:
                equalsOperator = ParseModEqualsToken(buffer);
                break;

            case SyntaxTokenKind.AndEquals:
                equalsOperator = ParseAndEqualsToken(buffer);
                break;

            case SyntaxTokenKind.OrEquals:
                equalsOperator = ParseOrEqualsToken(buffer);
                break;

            case SyntaxTokenKind.XorEquals:
                equalsOperator = ParseXorEqualsToken(buffer);
                break;

            case SyntaxTokenKind.LeftShiftEquals:
                equalsOperator = ParseLeftShiftEqualsToken(buffer);
                break;

            case SyntaxTokenKind.RightShiftEquals:
                equalsOperator = ParseRightShiftEqualsToken(buffer);
                break;

            default:
                throw CreateException(buffer, "Unknown assignment operation.");
        }

        ExpressionSyntax right = equalsOperator.RawKind == (int)SyntaxTokenKind.EqualsSign
            ? ParseAssignmentExpression(buffer)
            : ParseExpression(buffer);
        SyntaxToken semicolon = requireSemicolon
            ? ParseSemicolonToken(buffer)
            : CreateEmptySemicolon();

        return new AssignmentStatementSyntax(value, equalsOperator, right, semicolon);
    }

    private ExpressionSyntax ParseAssignmentExpression(IBuffer<Level5SyntaxToken> buffer)
    {
        ExpressionSyntax left = ParseExpression(buffer);

        if (!HasTokenKind(buffer, SyntaxTokenKind.EqualsSign))
            return left;

        if (!IsAssignmentTarget(left))
            throw CreateException(buffer, "Invalid assignment target in chained assignment.");

        SyntaxToken equalsOperator = ParseEqualsSignToken(buffer);
        ExpressionSyntax right = ParseAssignmentExpression(buffer);

        return new AssignmentExpressionSyntax(left, equalsOperator, right);
    }

    private static bool IsAssignmentTarget(ExpressionSyntax expression)
    {
        expression = UnwrapParentheses(expression);

        return expression is ValueExpressionSyntax { Value: VariableExpressionSyntax }
            or VariableExpressionSyntax
            or ArrayIndexExpressionSyntax;
    }

    private static ExpressionSyntax UnwrapParentheses(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
            expression = parenthesized.Expression;

        return expression;
    }

    private ExpressionSyntax ParseExpression(IBuffer<Level5SyntaxToken> buffer)
    {
        return ParseBinaryExpression(buffer, ExpressionPrecedence.LogicalOr);
    }

    private ExpressionSyntax ParseBinaryExpression(IBuffer<Level5SyntaxToken> buffer, int maxPrecedence)
    {
        ExpressionSyntax left = ParseUnaryOrPrimaryExpression(buffer);

        while (true)
        {
            if (HasTokenKind(buffer, SyntaxTokenKind.SwitchKeyword))
                return ParseSwitchExpression(buffer, left);

            if (!TryGetBinaryOperatorPrecedence(buffer, out int precedence) || precedence > maxPrecedence)
                return left;

            SyntaxToken operation = ParseBinaryOperatorToken(buffer);
            ExpressionSyntax right = ParseBinaryExpression(buffer, precedence - 1);

            if (IsLogicalOperator(operation))
                left = new LogicalExpressionSyntax(left, operation, right);
            else
                left = new BinaryExpressionSyntax(left, operation, right);
        }
    }

    private ExpressionSyntax ParseUnaryOrPrimaryExpression(IBuffer<Level5SyntaxToken> buffer)
    {
        if (HasTokenKind(buffer, SyntaxTokenKind.ParenOpen))
        {
            if (IsTypeCast(buffer))
                return ParseTypeCastValueExpression(buffer);

            return ParseParenthesizedExpression(buffer);
        }

        if (HasTokenKind(buffer, SyntaxTokenKind.NewKeyword))
            return ParseArrayInstantiationExpression(buffer);

        if (IsUnaryExpression(buffer) && !IsFloatingNumberLiteralExpression(buffer))
            return ParseUnaryExpression(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.Identifier)
            && (HasTokenKind(buffer, 1, SyntaxTokenKind.ParenOpen) || HasTokenKind(buffer, 1, SyntaxTokenKind.Dot) || HasTokenKind(buffer, 1, SyntaxTokenKind.Smaller)))
            return ParseMethodInvocationExpression(buffer);

        if (IsValueExpression(buffer))
        {
            ValueExpressionSyntax value = ParseValueExpression(buffer);
            ExpressionSyntax left = value;

            if (HasTokenKind(buffer, SyntaxTokenKind.BracketOpen))
                left = ParseArrayIndexExpression(buffer, value);

            return left;
        }

        throw CreateException(buffer, "Invalid expression.", SyntaxTokenKind.Variable,
            SyntaxTokenKind.StringLiteral,
            SyntaxTokenKind.NumericLiteral, SyntaxTokenKind.UnsignedNumericLiteral, SyntaxTokenKind.FloatingNumericLiteral,
            SyntaxTokenKind.HashStringLiteral,
            SyntaxTokenKind.HashNumericLiteral, SyntaxTokenKind.ParenOpen);
    }

    private ParenthesizedExpressionSyntax ParseParenthesizedExpression(IBuffer<Level5SyntaxToken> buffer)
    {
        SyntaxToken parenOpen = ParseParenOpenToken(buffer);
        ExpressionSyntax expression = ParseExpression(buffer);
        SyntaxToken parenClose = ParseParenCloseToken(buffer);

        return new ParenthesizedExpressionSyntax(parenOpen, expression, parenClose);
    }

    private bool IsTypeCast(IBuffer<Level5SyntaxToken> buffer)
    {
        return HasTokenKind(buffer, 1, SyntaxTokenKind.IntKeyword) ||
               HasTokenKind(buffer, 1, SyntaxTokenKind.BoolKeyword) ||
               HasTokenKind(buffer, 1, SyntaxTokenKind.FloatKeyword);
    }

    private bool TryGetBinaryOperatorPrecedence(IBuffer<Level5SyntaxToken> buffer, out int precedence)
    {
        SyntaxTokenKind[] operators =
        [
            SyntaxTokenKind.OrKeyword, SyntaxTokenKind.OrOr,
            SyntaxTokenKind.AndKeyword, SyntaxTokenKind.AndAnd,
            SyntaxTokenKind.Or, SyntaxTokenKind.Xor, SyntaxTokenKind.And,
            SyntaxTokenKind.Equals, SyntaxTokenKind.NotEquals,
            SyntaxTokenKind.GreaterEquals, SyntaxTokenKind.SmallerEquals,
            SyntaxTokenKind.Greater, SyntaxTokenKind.Smaller,
            SyntaxTokenKind.LeftShift, SyntaxTokenKind.RightShift,
            SyntaxTokenKind.Plus, SyntaxTokenKind.Minus,
            SyntaxTokenKind.Mul, SyntaxTokenKind.Div, SyntaxTokenKind.Mod
        ];

        foreach (SyntaxTokenKind op in operators)
        {
            if (!HasTokenKind(buffer, op))
                continue;

            int? value = ExpressionPrecedence.GetOperatorPrecedence(op);
            if (value is null or < 0)
                break;

            precedence = value.Value;
            return true;
        }

        precedence = 0;
        return false;
    }

    private bool IsLogicalOperator(SyntaxToken operation)
    {
        return operation.RawKind is (int)SyntaxTokenKind.AndKeyword or (int)SyntaxTokenKind.OrKeyword
            or (int)SyntaxTokenKind.AndAnd or (int)SyntaxTokenKind.OrOr;
    }

    private SyntaxToken ParseBinaryOperatorToken(IBuffer<Level5SyntaxToken> buffer)
    {
        if (HasTokenKind(buffer, SyntaxTokenKind.Equals)) return ParseEqualsToken(buffer);
        if (HasTokenKind(buffer, SyntaxTokenKind.NotEquals)) return ParseNotEqualsToken(buffer);
        if (HasTokenKind(buffer, SyntaxTokenKind.GreaterEquals)) return ParseGreaterEqualsToken(buffer);
        if (HasTokenKind(buffer, SyntaxTokenKind.SmallerEquals)) return ParseSmallerEqualsToken(buffer);
        if (HasTokenKind(buffer, SyntaxTokenKind.Greater)) return ParseGreaterToken(buffer);
        if (HasTokenKind(buffer, SyntaxTokenKind.Smaller)) return ParseSmallerToken(buffer);
        if (HasTokenKind(buffer, SyntaxTokenKind.Plus)) return ParsePlusToken(buffer);
        if (HasTokenKind(buffer, SyntaxTokenKind.Minus)) return ParseMinusToken(buffer);
        if (HasTokenKind(buffer, SyntaxTokenKind.Mul)) return ParseMulToken(buffer);
        if (HasTokenKind(buffer, SyntaxTokenKind.Div)) return ParseDivToken(buffer);
        if (HasTokenKind(buffer, SyntaxTokenKind.Mod)) return ParseModToken(buffer);
        if (HasTokenKind(buffer, SyntaxTokenKind.And)) return ParseAndToken(buffer);
        if (HasTokenKind(buffer, SyntaxTokenKind.Or)) return ParseOrToken(buffer);
        if (HasTokenKind(buffer, SyntaxTokenKind.Xor)) return ParseXorToken(buffer);
        if (HasTokenKind(buffer, SyntaxTokenKind.LeftShift)) return ParseLeftShiftToken(buffer);
        if (HasTokenKind(buffer, SyntaxTokenKind.RightShift)) return ParseRightShiftToken(buffer);
        if (HasTokenKind(buffer, SyntaxTokenKind.AndKeyword)) return ParseAndKeywordToken(buffer);
        if (HasTokenKind(buffer, SyntaxTokenKind.AndAnd)) return ParseAndAndToken(buffer);
        if (HasTokenKind(buffer, SyntaxTokenKind.OrKeyword)) return ParseOrKeywordToken(buffer);
        if (HasTokenKind(buffer, SyntaxTokenKind.OrOr)) return ParseOrOrToken(buffer);

        throw CreateException(buffer, "Unknown binary expression.", SyntaxTokenKind.Equals, SyntaxTokenKind.NotEquals,
            SyntaxTokenKind.GreaterEquals, SyntaxTokenKind.SmallerEquals, SyntaxTokenKind.Greater, SyntaxTokenKind.Smaller);
    }

    private TypeCastValueExpressionSyntax ParseTypeCastValueExpression(IBuffer<Level5SyntaxToken> buffer)
    {
        var typeCast = ParseTypeCastExpression(buffer);
        // Casts are unary: operand is another unary/primary (`(int)random(10)`, `(float)-$x`),
        // not a full binary expression (`(int)$a / $b` is a cast of `$a`, then `/`).
        ExpressionSyntax operand = ParseUnaryOrPrimaryExpression(buffer);
        ValueExpressionSyntax value = operand as ValueExpressionSyntax ?? new ValueExpressionSyntax(operand);

        return new TypeCastValueExpressionSyntax(typeCast, value);
    }

    private TypeCastExpressionSyntax ParseTypeCastExpression(IBuffer<Level5SyntaxToken> buffer)
    {
        SyntaxToken parenOpen = ParseParenOpenToken(buffer);
        SyntaxToken type;
        switch (buffer.Peek().Kind)
        {
            case SyntaxTokenKind.IntKeyword:
                type = ParseIntKeywordToken(buffer);
                break;

            case SyntaxTokenKind.BoolKeyword:
                type = ParseBoolKeywordToken(buffer);
                break;

            case SyntaxTokenKind.FloatKeyword:
                type = ParseFloatKeywordToken(buffer);
                break;

            default:
                throw CreateException(buffer, "Invalid type cast expression.", SyntaxTokenKind.IntKeyword,
                    SyntaxTokenKind.BoolKeyword, SyntaxTokenKind.FloatKeyword);
        }
        SyntaxToken parenClose = ParseParenCloseToken(buffer);

        return new TypeCastExpressionSyntax(parenOpen, type, parenClose);
    }

    private SwitchExpressionSyntax ParseSwitchExpression(IBuffer<Level5SyntaxToken> buffer, ExpressionSyntax value)
    {
        SyntaxToken switchToken = ParseSwitchKeywordToken(buffer);
        var caseBlock = ParseSwitchBlockExpression(buffer);

        return new SwitchExpressionSyntax(value, switchToken, caseBlock);
    }

    private SwitchBlockExpressionSyntax ParseSwitchBlockExpression(IBuffer<Level5SyntaxToken> buffer)
    {
        SyntaxToken curlyOpen = ParseCurlyOpenToken(buffer);
        var cases = ParseSwitchCaseExpressions(buffer);
        SyntaxToken curlyClose = ParseCurlyCloseToken(buffer);

        return new SwitchBlockExpressionSyntax(curlyOpen, cases, curlyClose);
    }

    private IReadOnlyList<SwitchCaseExpressionSyntax> ParseSwitchCaseExpressions(IBuffer<Level5SyntaxToken> buffer)
    {
        var result = new List<SwitchCaseExpressionSyntax>();

        while (IsLiteralExpression(buffer) || HasTokenKind(buffer, SyntaxTokenKind.Underscore))
        {
            if (HasTokenKind(buffer, SyntaxTokenKind.Underscore))
            {
                result.Add(ParseDefaultSwitchCaseExpression(buffer));
                continue;
            }

            result.Add(ParseLiteralSwitchCaseExpression(buffer));
        }

        return result;
    }

    private DefaultSwitchCaseExpressionSyntax ParseDefaultSwitchCaseExpression(IBuffer<Level5SyntaxToken> buffer)
    {
        SyntaxToken underscore = ParseUnderscoreToken(buffer);
        SyntaxToken arrowRight = ParseArrowRightToken(buffer);
        var value = ParseValueExpression(buffer);

        return new DefaultSwitchCaseExpressionSyntax(underscore, arrowRight, value);
    }

    private LiteralSwitchCaseExpressionSyntax ParseLiteralSwitchCaseExpression(IBuffer<Level5SyntaxToken> buffer)
    {
        var caseValue = ParseValueExpression(buffer);
        SyntaxToken arrowRight = ParseArrowRightToken(buffer);
        var value = ParseValueExpression(buffer);

        return new LiteralSwitchCaseExpressionSyntax(caseValue, arrowRight, value);
    }

    private ArrayInstantiationExpressionSyntax ParseArrayInstantiationExpression(IBuffer<Level5SyntaxToken> buffer)
    {
        SyntaxToken newToken = ParseNewKeywordToken(buffer);

        var indexes = new List<ArrayIndexerExpressionSyntax>();
        while (HasTokenKind(buffer, SyntaxTokenKind.BracketOpen))
            indexes.Add(ParseArrayIndexerExpression(buffer));

        return new ArrayInstantiationExpressionSyntax(newToken, indexes);
    }

    private ArrayIndexExpressionSyntax ParseArrayIndexExpression(IBuffer<Level5SyntaxToken> buffer, ValueExpressionSyntax value)
    {
        var indexes = new List<ArrayIndexerExpressionSyntax>();
        while (HasTokenKind(buffer, SyntaxTokenKind.BracketOpen))
            indexes.Add(ParseArrayIndexerExpression(buffer));

        return new ArrayIndexExpressionSyntax(value, indexes);
    }

    private bool IsUnaryExpression(IBuffer<Level5SyntaxToken> buffer)
    {
        return HasTokenKind(buffer, SyntaxTokenKind.Complement) ||
               HasTokenKind(buffer, SyntaxTokenKind.Minus) ||
               HasTokenKind(buffer, SyntaxTokenKind.NotKeyword) ||
               HasTokenKind(buffer, SyntaxTokenKind.Not);
    }

    private UnaryExpressionSyntax ParseUnaryExpression(IBuffer<Level5SyntaxToken> buffer)
    {
        if (HasTokenKind(buffer, SyntaxTokenKind.Complement))
            return new UnaryExpressionSyntax(ParseComplementToken(buffer), ParseUnaryOrPrimaryExpression(buffer));

        if (HasTokenKind(buffer, SyntaxTokenKind.Minus))
            return new UnaryExpressionSyntax(ParseMinusToken(buffer), ParseUnaryOrPrimaryExpression(buffer));

        if (HasTokenKind(buffer, SyntaxTokenKind.NotKeyword))
            return new UnaryExpressionSyntax(ParseNotKeywordToken(buffer), ParseUnaryOrPrimaryExpression(buffer));

        if (HasTokenKind(buffer, SyntaxTokenKind.Not))
            return new UnaryExpressionSyntax(ParseNotToken(buffer), ParseUnaryOrPrimaryExpression(buffer));

        throw CreateException(buffer, "Unknown unary expression.", SyntaxTokenKind.Complement, SyntaxTokenKind.Minus);
    }

    private ArrayIndexerExpressionSyntax ParseArrayIndexerExpression(IBuffer<Level5SyntaxToken> buffer)
    {
        SyntaxToken bracketOpen = ParseBracketOpenToken(buffer);
        // Indices may be full expressions (`$arr[$i + 1]`); brackets provide grouping.
        ExpressionSyntax indexExpression = ParseExpression(buffer);
        ValueExpressionSyntax index = indexExpression as ValueExpressionSyntax
            ?? new ValueExpressionSyntax(indexExpression);
        SyntaxToken bracketClose = ParseBracketCloseToken(buffer);

        return new ArrayIndexerExpressionSyntax(bracketOpen, index, bracketClose);
    }

    private MethodInvocationExpressionSyntax ParseMethodInvocationExpression(IBuffer<Level5SyntaxToken> buffer)
    {
        NameSyntax name = ParseName(buffer);
        var metadata = ParseMethodInvocationMetadata(buffer);
        var methodInvocationParameters = ParseMethodInvocationParameters(buffer);

        return new MethodInvocationExpressionSyntax(name, metadata, methodInvocationParameters);
    }

    private MethodInvocationStatementSyntax ParseMethodInvocationStatement(IBuffer<Level5SyntaxToken> buffer)
    {
        NameSyntax name = ParseName(buffer);
        var metadata = ParseMethodInvocationMetadata(buffer);
        var methodInvocationParameters = ParseMethodInvocationParameters(buffer);
        SyntaxToken semicolon = ParseSemicolonToken(buffer);

        return new MethodInvocationStatementSyntax(name, metadata, methodInvocationParameters, semicolon);
    }

    private MethodInvocationMetadataSyntax? ParseMethodInvocationMetadata(IBuffer<Level5SyntaxToken> buffer)
    {
        if (!HasTokenKind(buffer, SyntaxTokenKind.Smaller))
            return null;

        SyntaxToken relSmallerToken = ParseSmallerToken(buffer);
        var parameter = ParseNumericLiteralExpression(buffer);
        SyntaxToken relBiggerToken = ParseGreaterToken(buffer);

        return new MethodInvocationMetadataSyntax(relSmallerToken, parameter, relBiggerToken);
    }

    private MethodInvocationParametersSyntax ParseMethodInvocationParameters(IBuffer<Level5SyntaxToken> buffer)
    {
        SyntaxToken parenOpen = ParseParenOpenToken(buffer);
        var parameters = ParseMethodInvocationParameterList(buffer);
        SyntaxToken parenClose = ParseParenCloseToken(buffer);

        return new MethodInvocationParametersSyntax(parenOpen, parameters, parenClose);
    }

    private CommaSeparatedSyntaxList<ExpressionSyntax>? ParseMethodInvocationParameterList(IBuffer<Level5SyntaxToken> buffer)
    {
        if (!IsExpressionStart(buffer))
            return null;

        var result = new List<ExpressionSyntax>();

        ExpressionSyntax parameter = ParseExpression(buffer);
        result.Add(parameter);

        while (HasTokenKind(buffer, SyntaxTokenKind.Comma))
        {
            SkipTokenKind(buffer, SyntaxTokenKind.Comma);

            if (!IsExpressionStart(buffer))
                throw CreateException(buffer, "Invalid end of parameter list.", SyntaxTokenKind.Variable,
                    SyntaxTokenKind.StringLiteral, SyntaxTokenKind.NumericLiteral, SyntaxTokenKind.UnsignedNumericLiteral,
                    SyntaxTokenKind.HashNumericLiteral, SyntaxTokenKind.HashStringLiteral,
                    SyntaxTokenKind.FloatingNumericLiteral, SyntaxTokenKind.ParenOpen, SyntaxTokenKind.Identifier);

            parameter = ParseExpression(buffer);
            result.Add(parameter);
        }

        return new CommaSeparatedSyntaxList<ExpressionSyntax>(result);
    }

    private bool IsExpressionStart(IBuffer<Level5SyntaxToken> buffer)
    {
        return IsValueExpression(buffer) ||
               HasTokenKind(buffer, SyntaxTokenKind.ParenOpen) ||
               HasTokenKind(buffer, SyntaxTokenKind.Identifier) ||
               HasTokenKind(buffer, SyntaxTokenKind.NewKeyword) ||
               IsUnaryExpression(buffer);
    }

    private bool IsValueExpression(IBuffer<Level5SyntaxToken> buffer)
    {
        return HasTokenKind(buffer, SyntaxTokenKind.Variable) ||
               IsLiteralExpression(buffer);
    }

    private bool IsLiteralExpression(IBuffer<Level5SyntaxToken> buffer)
    {
        return HasTokenKind(buffer, SyntaxTokenKind.StringLiteral) ||
               HasTokenKind(buffer, SyntaxTokenKind.NumericLiteral) ||
               HasTokenKind(buffer, SyntaxTokenKind.UnsignedNumericLiteral) ||
               HasTokenKind(buffer, SyntaxTokenKind.HashStringLiteral) ||
               HasTokenKind(buffer, SyntaxTokenKind.HashNumericLiteral) ||
               HasTokenKind(buffer, SyntaxTokenKind.UndefinedKeyword) ||
               HasTokenKind(buffer, SyntaxTokenKind.TrueKeyword) ||
               IsFloatingNumberLiteralExpression(buffer);
    }

    private bool IsFloatingNumberLiteralExpression(IBuffer<Level5SyntaxToken> buffer)
    {
        return HasTokenKind(buffer, SyntaxTokenKind.FloatingNumericLiteral) ||
               (HasTokenKind(buffer, SyntaxTokenKind.Minus) && HasTokenKind(buffer, 1, SyntaxTokenKind.InfinityKeyword)) ||
               HasTokenKind(buffer, SyntaxTokenKind.InfinityKeyword) ||
               (HasTokenKind(buffer, SyntaxTokenKind.Minus) && HasTokenKind(buffer, 1, SyntaxTokenKind.InfKeyword)) ||
               HasTokenKind(buffer, SyntaxTokenKind.InfKeyword) ||
               (HasTokenKind(buffer, SyntaxTokenKind.Minus) && HasTokenKind(buffer, 1, SyntaxTokenKind.Infinite)) ||
               HasTokenKind(buffer, SyntaxTokenKind.Infinite) ||
               (HasTokenKind(buffer, SyntaxTokenKind.Minus) && HasTokenKind(buffer, 1, SyntaxTokenKind.NanKeyword)) ||
               HasTokenKind(buffer, SyntaxTokenKind.NanKeyword);
    }

    private ValueExpressionSyntax ParseValueExpression(IBuffer<Level5SyntaxToken> buffer)
    {
        if (HasTokenKind(buffer, SyntaxTokenKind.Variable))
            return new ValueExpressionSyntax(ParseVariableExpression(buffer), ParseValueMetadataParameters(buffer));

        if (HasTokenKind(buffer, SyntaxTokenKind.StringLiteral))
            return new ValueExpressionSyntax(ParseStringLiteralExpression(buffer), ParseValueMetadataParameters(buffer));

        if (HasTokenKind(buffer, SyntaxTokenKind.NumericLiteral))
            return new ValueExpressionSyntax(ParseNumericLiteralExpression(buffer), ParseValueMetadataParameters(buffer));

        if (HasTokenKind(buffer, SyntaxTokenKind.UnsignedNumericLiteral))
            return new ValueExpressionSyntax(ParseUnsignedNumericLiteralExpression(buffer), ParseValueMetadataParameters(buffer));

        if (HasTokenKind(buffer, SyntaxTokenKind.HashNumericLiteral))
            return new ValueExpressionSyntax(ParseHashNumericLiteralExpression(buffer), ParseValueMetadataParameters(buffer));

        if (HasTokenKind(buffer, SyntaxTokenKind.HashStringLiteral))
            return new ValueExpressionSyntax(ParseHashStringLiteralExpression(buffer), ParseValueMetadataParameters(buffer));

        if (HasTokenKind(buffer, SyntaxTokenKind.UndefinedKeyword))
            return new ValueExpressionSyntax(ParseUndefinedLiteralExpression(buffer), ParseValueMetadataParameters(buffer));

        if (HasTokenKind(buffer, SyntaxTokenKind.TrueKeyword))
            return new ValueExpressionSyntax(ParseTrueLiteralExpression(buffer), ParseValueMetadataParameters(buffer));

        if (IsFloatingNumberLiteralExpression(buffer))
            return new ValueExpressionSyntax(ParseFloatingNumericLiteralExpression(buffer), ParseValueMetadataParameters(buffer));

        throw CreateException(buffer, "Unknown value expression.", SyntaxTokenKind.Variable, SyntaxTokenKind.StringLiteral,
            SyntaxTokenKind.NumericLiteral, SyntaxTokenKind.UnsignedNumericLiteral, SyntaxTokenKind.HashNumericLiteral,
            SyntaxTokenKind.HashStringLiteral, SyntaxTokenKind.UndefinedKeyword, SyntaxTokenKind.TrueKeyword,
            SyntaxTokenKind.FloatingNumericLiteral,
            SyntaxTokenKind.Infinite, SyntaxTokenKind.InfKeyword, SyntaxTokenKind.InfinityKeyword, SyntaxTokenKind.NanKeyword);
    }

    private ValueMetadataParametersSyntax? ParseValueMetadataParameters(IBuffer<Level5SyntaxToken> buffer)
    {
        if (!HasTokenKind(buffer, SyntaxTokenKind.Smaller) || !HasTokenKind(buffer, 2, SyntaxTokenKind.Greater))
            return null;

        SyntaxToken relSmaller = ParseSmallerToken(buffer);
        var parameter = ParseNumericLiteralExpression(buffer);
        SyntaxToken relBigger = ParseGreaterToken(buffer);

        return new ValueMetadataParametersSyntax(relSmaller, parameter, relBigger);
    }

    private LiteralExpressionSyntax ParseStringLiteralExpression(IBuffer<Level5SyntaxToken> buffer)
    {
        SyntaxToken literal = ParseStringLiteralToken(buffer);

        return new LiteralExpressionSyntax(literal);
    }

    private LiteralExpressionSyntax ParseNumericLiteralExpression(IBuffer<Level5SyntaxToken> buffer)
    {
        SyntaxToken literal = ParseNumericLiteralToken(buffer);

        return new LiteralExpressionSyntax(literal);
    }

    private LiteralExpressionSyntax ParseUnsignedNumericLiteralExpression(IBuffer<Level5SyntaxToken> buffer)
    {
        SyntaxToken literal = ParseUnsignedNumericLiteralToken(buffer);

        return new LiteralExpressionSyntax(literal);
    }

    private LiteralExpressionSyntax ParseHashNumericLiteralExpression(IBuffer<Level5SyntaxToken> buffer)
    {
        SyntaxToken literal = ParseHashNumericLiteralToken(buffer);

        return new LiteralExpressionSyntax(literal);
    }

    private LiteralExpressionSyntax ParseUndefinedLiteralExpression(IBuffer<Level5SyntaxToken> buffer)
    {
        SyntaxToken literal = ParseUndefinedLiteralToken(buffer);

        return new LiteralExpressionSyntax(literal);
    }

    private LiteralExpressionSyntax ParseTrueLiteralExpression(IBuffer<Level5SyntaxToken> buffer)
    {
        return new LiteralExpressionSyntax(ParseTrueKeywordToken(buffer));
    }

    private LiteralExpressionSyntax ParseHashStringLiteralExpression(IBuffer<Level5SyntaxToken> buffer)
    {
        SyntaxToken literal = ParseHashStringLiteralToken(buffer);

        return new LiteralExpressionSyntax(literal);
    }

    private ExpressionSyntax ParseFloatingNumericLiteralExpression(IBuffer<Level5SyntaxToken> buffer)
    {
        if (HasTokenKind(buffer, SyntaxTokenKind.FloatingNumericLiteral))
            return new LiteralExpressionSyntax(ParseFloatingNumericLiteralToken(buffer));

        if (HasTokenKind(buffer, SyntaxTokenKind.InfinityKeyword))
            return new LiteralExpressionSyntax(ParseInfinityKeywordToken(buffer));

        if (HasTokenKind(buffer, SyntaxTokenKind.Minus) && HasTokenKind(buffer, 1, SyntaxTokenKind.InfinityKeyword))
            return new UnaryExpressionSyntax(ParseMinusToken(buffer), ParseValueExpression(buffer));

        if (HasTokenKind(buffer, SyntaxTokenKind.InfKeyword))
            return new LiteralExpressionSyntax(ParseInfKeywordToken(buffer));

        if (HasTokenKind(buffer, SyntaxTokenKind.Minus) && HasTokenKind(buffer, 1, SyntaxTokenKind.InfKeyword))
            return new UnaryExpressionSyntax(ParseMinusToken(buffer), ParseValueExpression(buffer));

        if (HasTokenKind(buffer, SyntaxTokenKind.Infinite))
            return new LiteralExpressionSyntax(ParseInfiniteToken(buffer));

        if (HasTokenKind(buffer, SyntaxTokenKind.Minus) && HasTokenKind(buffer, 1, SyntaxTokenKind.Infinite))
            return new UnaryExpressionSyntax(ParseMinusToken(buffer), ParseValueExpression(buffer));

        if (HasTokenKind(buffer, SyntaxTokenKind.NanKeyword))
            return new LiteralExpressionSyntax(ParseNanKeywordToken(buffer));

        if (HasTokenKind(buffer, SyntaxTokenKind.Minus) && HasTokenKind(buffer, 1, SyntaxTokenKind.NanKeyword))
            return new UnaryExpressionSyntax(ParseMinusToken(buffer), ParseValueExpression(buffer));

        throw CreateException(buffer, "Unknown floating point literal expression.", SyntaxTokenKind.FloatingNumericLiteral,
            SyntaxTokenKind.Infinite, SyntaxTokenKind.NanKeyword);
    }

    private VariableExpressionSyntax ParseVariableExpression(IBuffer<Level5SyntaxToken> buffer)
    {
        SyntaxToken variable = ParseVariableToken(buffer);

        return new VariableExpressionSyntax(variable);
    }

    private NameSyntax ParseName(IBuffer<Level5SyntaxToken> buffer)
    {
        if (!HasTokenKind(buffer, SyntaxTokenKind.Identifier))
            throw CreateException(buffer, "Invalid name syntax.", SyntaxTokenKind.Identifier);

        NameSyntax left = new SimpleNameSyntax(ParseIdentifierToken(buffer));
        if (!HasTokenKind(buffer, SyntaxTokenKind.Dot))
            return left;

        SyntaxToken dot = ParseDotToken(buffer);

        return new QualifiedNameSyntax(left, dot, ParseName(buffer));
    }

    private SyntaxToken ParseDotToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.Dot);
    }

    private SyntaxToken ParseCommaToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.Comma);
    }

    private SyntaxToken ParseSemicolonToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.Semicolon);
    }

    private SyntaxToken ParseColonToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.Colon);
    }

    private SyntaxToken ParseNotToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.Not);
    }

    private SyntaxToken ParseEqualsSignToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.EqualsSign);
    }

    private SyntaxToken ParsePlusEqualsToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.PlusEquals);
    }

    private SyntaxToken ParseMinusEqualsToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.MinusEquals);
    }

    private SyntaxToken ParseMulEqualsToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.MulEquals);
    }

    private SyntaxToken ParseDivEqualsToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.DivEquals);
    }

    private SyntaxToken ParseModEqualsToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.ModEquals);
    }

    private SyntaxToken ParseAndEqualsToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.AndEquals);
    }

    private SyntaxToken ParseOrEqualsToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.OrEquals);
    }

    private SyntaxToken ParseXorEqualsToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.XorEquals);
    }

    private SyntaxToken ParseLeftShiftEqualsToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.LeftShiftEquals);
    }

    private SyntaxToken ParseRightShiftEqualsToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.RightShiftEquals);
    }

    private SyntaxToken ParseComplementToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.Complement);
    }

    private SyntaxToken ParsePlusToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.Plus);
    }

    private SyntaxToken ParseMinusToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.Minus);
    }

    private SyntaxToken ParseMulToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.Mul);
    }

    private SyntaxToken ParseDivToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.Div);
    }

    private SyntaxToken ParseModToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.Mod);
    }

    private SyntaxToken ParseAndToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.And);
    }

    private SyntaxToken ParseAndAndToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.AndAnd);
    }

    private SyntaxToken ParseOrToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.Or);
    }

    private SyntaxToken ParseOrOrToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.OrOr);
    }

    private SyntaxToken ParseXorToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.Xor);
    }

    private SyntaxToken ParseLeftShiftToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.LeftShift);
    }

    private SyntaxToken ParseRightShiftToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.RightShift);
    }

    private SyntaxToken ParseUnderscoreToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.Underscore);
    }

    private SyntaxToken ParseEqualsToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.Equals);
    }

    private SyntaxToken ParseNotEqualsToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.NotEquals);
    }

    private SyntaxToken ParseGreaterEqualsToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.GreaterEquals);
    }

    private SyntaxToken ParseSmallerEqualsToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.SmallerEquals);
    }

    private SyntaxToken ParseArrowRightToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.ArrowRight);
    }

    private SyntaxToken ParseMinusMinusToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.Decrement);
    }

    private SyntaxToken ParsePlusPlusToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.Increment);
    }

    private SyntaxToken ParseParenOpenToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.ParenOpen);
    }

    private SyntaxToken ParseParenCloseToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.ParenClose);
    }

    private SyntaxToken ParseCurlyOpenToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.CurlyOpen);
    }

    private SyntaxToken ParseCurlyCloseToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.CurlyClose);
    }

    private SyntaxToken ParseBracketOpenToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.BracketOpen);
    }

    private SyntaxToken ParseBracketCloseToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.BracketClose);
    }

    private SyntaxToken ParseSmallerToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.Smaller);
    }

    private SyntaxToken ParseGreaterToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.Greater);
    }

    private SyntaxToken ParseYieldKeywordToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.YieldKeyword);
    }

    private SyntaxToken ParseReturnKeywordToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.ReturnKeyword);
    }

    private SyntaxToken ParseExitKeywordToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.ExitKeyword);
    }

    private SyntaxToken ParseNewKeywordToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.NewKeyword);
    }

    private SyntaxToken ParseNotKeywordToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.NotKeyword);
    }

    private SyntaxToken ParseAndKeywordToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.AndKeyword);
    }

    private SyntaxToken ParseOrKeywordToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.OrKeyword);
    }

    private SyntaxToken ParseSwitchKeywordToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.SwitchKeyword);
    }

    private SyntaxToken ParseGotoKeywordToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.GotoKeyword);
    }

    private SyntaxToken ParseIfKeywordToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.IfKeyword);
    }

    private SyntaxToken ParseElseKeywordToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.ElseKeyword);
    }

    private SyntaxToken ParseWhileKeywordToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.WhileKeyword);
    }

    private SyntaxToken ParseForKeywordToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.ForKeyword);
    }

    private SyntaxToken ParseDoKeywordToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.DoKeyword);
    }

    private SyntaxToken ParseBreakKeywordToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.BreakKeyword);
    }

    private SyntaxToken ParseContinueKeywordToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.ContinueKeyword);
    }

    private SyntaxToken ParseGlobalKeywordToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.GlobalKeyword);
    }

    private SyntaxToken ParseTrueKeywordToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.TrueKeyword);
    }

    private SyntaxToken ParseIntKeywordToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.IntKeyword);
    }

    private SyntaxToken ParseBoolKeywordToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.BoolKeyword);
    }

    private SyntaxToken ParseFloatKeywordToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.FloatKeyword);
    }

    private SyntaxToken ParseNumericLiteralToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.NumericLiteral);
    }

    private SyntaxToken ParseUnsignedNumericLiteralToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.UnsignedNumericLiteral);
    }

    private SyntaxToken ParseHashNumericLiteralToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.HashNumericLiteral);
    }

    private SyntaxToken ParseHashStringLiteralToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.HashStringLiteral);
    }

    private SyntaxToken ParseFloatingNumericLiteralToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.FloatingNumericLiteral);
    }

    private SyntaxToken ParseInfiniteToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.Infinite);
    }

    private SyntaxToken ParseNanKeywordToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.NanKeyword);
    }

    private SyntaxToken ParseInfinityKeywordToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.InfinityKeyword);
    }

    private SyntaxToken ParseInfKeywordToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.InfKeyword);
    }

    private SyntaxToken ParseUndefinedLiteralToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.UndefinedKeyword);
    }

    private SyntaxToken ParseStringLiteralToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.StringLiteral);
    }

    private SyntaxToken ParseIdentifierToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.Identifier);
    }

    private SyntaxToken ParseVariableToken(IBuffer<Level5SyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.Variable);
    }

    private SyntaxToken CreateToken(IBuffer<Level5SyntaxToken> buffer, SyntaxTokenKind expectedKind)
    {
        SyntaxTokenTrivia? leadingTrivia = ReadTrivia(buffer);

        if (buffer.Peek().Kind != expectedKind)
            throw CreateException(buffer, $"Unexpected token {buffer.Peek().Kind}.", expectedKind);
        Level5SyntaxToken content = buffer.Read();

        SyntaxTokenTrivia? trailingTrivia = ReadTrivia(buffer);

        return syntaxFactory.Create(content.Text, (int)expectedKind, leadingTrivia, trailingTrivia);
    }

    private SyntaxTokenTrivia? ReadTrivia(IBuffer<Level5SyntaxToken> buffer)
    {
        if (buffer.Peek().Kind == SyntaxTokenKind.Trivia)
        {
            Level5SyntaxToken token = buffer.Read();
            return new SyntaxTokenTrivia(token.Text);
        }

        return null;
    }

    private void SkipTokenKind(IBuffer<Level5SyntaxToken> buffer, SyntaxTokenKind expectedKind)
    {
        int toSkip = 1;

        Level5SyntaxToken peekedToken = buffer.Peek();
        if (peekedToken.Kind == SyntaxTokenKind.Trivia)
        {
            peekedToken = buffer.Peek(1);
            toSkip++;
        }

        if (peekedToken.Kind != expectedKind)
            throw CreateException(buffer, $"Unexpected token {peekedToken.Kind}.", expectedKind);

        for (var i = 0; i < toSkip; i++)
            buffer.Read();
    }

    protected bool HasTokenKind(IBuffer<Level5SyntaxToken> buffer, SyntaxTokenKind expectedKind)
    {
        return HasTokenKind(buffer, 0, expectedKind);
    }

    protected bool HasTokenKind(IBuffer<Level5SyntaxToken> buffer, int position, SyntaxTokenKind expectedKind)
    {
        var toPeek = 0;
        Level5SyntaxToken peekedToken = buffer.Peek(toPeek);

        position = Math.Max(0, position);
        for (var i = 0; i < position + 1; i++)
        {
            peekedToken = buffer.Peek(toPeek++);
            if (peekedToken.Kind == SyntaxTokenKind.Trivia)
                peekedToken = buffer.Peek(toPeek++);
        }

        return peekedToken.Kind == expectedKind;
    }

    private (int, int) GetCurrentLineAndColumn(IBuffer<Level5SyntaxToken> buffer)
    {
        var toPeek = 0;

        if (buffer.Peek().Kind == SyntaxTokenKind.Trivia)
            toPeek++;

        Level5SyntaxToken token = buffer.Peek(toPeek);
        return (token.Line, token.Column);
    }

    private IBuffer<Level5SyntaxToken> CreateTokenBuffer(string text)
    {
        ILexer<Level5SyntaxToken> lexer = scriptFactory.CreateLexer(text);
        return scriptFactory.CreateTokenBuffer(lexer);
    }

    private Exception CreateException(IBuffer<Level5SyntaxToken> buffer, string message, params SyntaxTokenKind[] expected)
    {
        (int line, int column) = GetCurrentLineAndColumn(buffer);
        return CreateException(message, line, column, expected);
    }

    private Exception CreateException(string message, int line, int column, params SyntaxTokenKind[] expected)
    {
        message = $"{message} (Line {line}, Column {column})";

        if (expected.Length > 0)
        {
            message = expected.Length == 1 ?
                $"{message} (Expected {expected[0]})" :
                $"{message} (Expected any of {string.Join(", ", expected)})";
        }

        throw new Level5ScriptParserException(message, line, column);
    }
}