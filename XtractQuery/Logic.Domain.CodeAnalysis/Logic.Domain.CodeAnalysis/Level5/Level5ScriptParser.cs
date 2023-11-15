using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logic.Domain.CodeAnalysis.Contract;
using Logic.Domain.CodeAnalysis.Contract.DataClasses;
using Logic.Domain.CodeAnalysis.Contract.Level5;
using Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses;
using Logic.Domain.CodeAnalysis.Level5.InternalContract.DataClasses;

namespace Logic.Domain.CodeAnalysis.Level5
{
    internal class Level5ScriptParser : ILevel5ScriptParser
    {
        private readonly ITokenFactory<Level5SyntaxToken> _scriptFactory;
        private readonly ILevel5SyntaxFactory _syntaxFactory;

        public Level5ScriptParser(ITokenFactory<Level5SyntaxToken> scriptFactory, ILevel5SyntaxFactory syntaxFactory)
        {
            _scriptFactory = scriptFactory;
            _syntaxFactory = syntaxFactory;
        }

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

        public MethodDeclarationMetadataParametersSyntax ParseMethodDeclarationMetadataParameters(string text)
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

        public MethodInvocationExpressionParametersSyntax ParseMethodInvocationExpressionParameters(string text)
        {
            IBuffer<Level5SyntaxToken> buffer = CreateTokenBuffer(text);

            return ParseMethodInvocationExpressionParameters(buffer);
        }

        public CommaSeparatedSyntaxList<ValueExpressionSyntax>? ParseValueList(string text)
        {
            IBuffer<Level5SyntaxToken> buffer = CreateTokenBuffer(text);

            return ParseValueList(buffer);
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
            var methodDeclarations = ParseMethodDeclarations(buffer);

            return new CodeUnitSyntax(methodDeclarations);
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
                   HasTokenKind(buffer, SyntaxTokenKind.IfKeyword);
        }

        private StatementSyntax ParseStatement(IBuffer<Level5SyntaxToken> buffer)
        {
            if (HasTokenKind(buffer, SyntaxTokenKind.YieldKeyword))
                return ParseYieldStatement(buffer);

            if (HasTokenKind(buffer, SyntaxTokenKind.ReturnKeyword))
                return ParseReturnStatement(buffer);

            if (HasTokenKind(buffer, SyntaxTokenKind.ExitKeyword))
                return ParseExitStatement(buffer);

            if (HasTokenKind(buffer, SyntaxTokenKind.StringLiteral))
                return ParseGotoLabelStatement(buffer);

            if (HasTokenKind(buffer, SyntaxTokenKind.GotoKeyword))
                return ParseGotoStatement(buffer);

            if (HasTokenKind(buffer, SyntaxTokenKind.IfKeyword))
                return ParseIfStatement(buffer);

            if (HasTokenKind(buffer, SyntaxTokenKind.Variable))
            {
                ExpressionSyntax value = ParseExpression(buffer);

                if (IsPostfixUnaryStatement(buffer))
                    return ParsePostfixUnaryStatement(buffer, value);

                return ParseAssignmentStatement(buffer, value);
            }

            throw CreateException(buffer, "Unknown statement.", SyntaxTokenKind.ReturnKeyword, SyntaxTokenKind.StringLiteral,
                SyntaxTokenKind.Variable, SyntaxTokenKind.YieldKeyword, SyntaxTokenKind.ExitKeyword);
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

            if (HasTokenKind(buffer, SyntaxTokenKind.NotKeyword))
                return new IfNotGotoStatementSyntax(ifToken, ParseUnaryExpression(buffer), ParseGotoStatement(buffer));

            if (IsValueExpression(buffer))
                return new IfGotoStatementSyntax(ifToken, ParseValueExpression(buffer), ParseGotoStatement(buffer));

            throw CreateException(buffer, "Unknown if statement.", SyntaxTokenKind.NotKeyword, SyntaxTokenKind.Variable,
                SyntaxTokenKind.StringLiteral, SyntaxTokenKind.NumericLiteral, SyntaxTokenKind.FloatingNumericLiteral,
                SyntaxTokenKind.HashNumericLiteral, SyntaxTokenKind.HashStringLiteral);
        }

        private GotoStatementSyntax ParseGotoStatement(IBuffer<Level5SyntaxToken> buffer)
        {
            SyntaxToken gotoToken = ParseGotoKeywordToken(buffer);
            var value = ParseValueExpression(buffer);
            SyntaxToken semicolon = ParseSemicolonToken(buffer);

            return new GotoStatementSyntax(gotoToken, value, semicolon);
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
            ValueExpressionSyntax valueExpression = ParseValueExpression(buffer);
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

        private AssignmentStatementSyntax ParseAssignmentStatement(IBuffer<Level5SyntaxToken> buffer, ExpressionSyntax value)
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

            var right = ParseExpression(buffer);
            SyntaxToken semicolon = ParseSemicolonToken(buffer);

            return new AssignmentStatementSyntax(value, equalsOperator, right, semicolon);
        }

        private ExpressionSyntax ParseExpression(IBuffer<Level5SyntaxToken> buffer)
        {
            if (HasTokenKind(buffer, SyntaxTokenKind.ParenOpen))
                return ParseTypeCastValueExpression(buffer);

            if (HasTokenKind(buffer, SyntaxTokenKind.NewKeyword))
                return ParseArrayInstantiationExpression(buffer);

            if (IsUnaryExpression(buffer))
                return ParseUnaryExpression(buffer);

            if (HasTokenKind(buffer, SyntaxTokenKind.Identifier) && HasTokenKind(buffer, 1, SyntaxTokenKind.ParenOpen))
                return ParseMethodInvocationExpression(buffer);

            ExpressionSyntax left;
            if (IsValueExpression(buffer))
            {
                ValueExpressionSyntax value = ParseValueExpression(buffer);
                left = value;

                if (HasTokenKind(buffer, SyntaxTokenKind.BracketOpen))
                    left = ParseArrayIndexExpression(buffer, value);
            }
            else
                left = ParseExpression(buffer);

            if (HasTokenKind(buffer, SyntaxTokenKind.SwitchKeyword))
                return ParseSwitchExpression(buffer, left);

            if (IsBinaryExpression(buffer))
                return ParseBinaryExpression(buffer, left);

            if (IsLogicalExpression(buffer))
                return ParseLogicalExpression(buffer, left);

            return left;
        }

        private TypeCastValueExpressionSyntax ParseTypeCastValueExpression(IBuffer<Level5SyntaxToken> buffer)
        {
            var typeCast = ParseTypeCastExpression(buffer);
            var value = ParseValueExpression(buffer);

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

        private bool IsLogicalExpression(IBuffer<Level5SyntaxToken> buffer)
        {
            return HasTokenKind(buffer, SyntaxTokenKind.AndKeyword) ||
                   HasTokenKind(buffer, SyntaxTokenKind.OrKeyword);
        }

        private LogicalExpressionSyntax ParseLogicalExpression(IBuffer<Level5SyntaxToken> buffer, ExpressionSyntax left)
        {
            switch (buffer.Peek().Kind)
            {
                case SyntaxTokenKind.AndKeyword:
                    return new LogicalExpressionSyntax(left, ParseAndKeywordToken(buffer), ParseExpression(buffer));

                case SyntaxTokenKind.OrKeyword:
                    return new LogicalExpressionSyntax(left, ParseOrKeywordToken(buffer), ParseExpression(buffer));

                default:
                    throw CreateException(buffer, "Unknown logical expression.", SyntaxTokenKind.AndKeyword, SyntaxTokenKind.OrKeyword);
            }
        }

        private bool IsBinaryExpression(IBuffer<Level5SyntaxToken> buffer)
        {
            return HasTokenKind(buffer, SyntaxTokenKind.Equals) ||
                   HasTokenKind(buffer, SyntaxTokenKind.NotEquals) ||
                   HasTokenKind(buffer, SyntaxTokenKind.GreaterEquals) ||
                   HasTokenKind(buffer, SyntaxTokenKind.SmallerEquals) ||
                   HasTokenKind(buffer, SyntaxTokenKind.Greater) ||
                   HasTokenKind(buffer, SyntaxTokenKind.Smaller) ||
                   HasTokenKind(buffer, SyntaxTokenKind.Plus) ||
                   HasTokenKind(buffer, SyntaxTokenKind.Minus) ||
                   HasTokenKind(buffer, SyntaxTokenKind.Mul) ||
                   HasTokenKind(buffer, SyntaxTokenKind.Div) ||
                   HasTokenKind(buffer, SyntaxTokenKind.Mod) ||
                   HasTokenKind(buffer, SyntaxTokenKind.And) ||
                   HasTokenKind(buffer, SyntaxTokenKind.Or) ||
                   HasTokenKind(buffer, SyntaxTokenKind.Xor) ||
                   HasTokenKind(buffer, SyntaxTokenKind.LeftShift) ||
                   HasTokenKind(buffer, SyntaxTokenKind.RightShift);
        }

        private BinaryExpressionSyntax ParseBinaryExpression(IBuffer<Level5SyntaxToken> buffer, ExpressionSyntax left)
        {
            switch (buffer.Peek().Kind)
            {
                case SyntaxTokenKind.Equals:
                    return new BinaryExpressionSyntax(left, ParseEqualsToken(buffer), ParseValueExpression(buffer));

                case SyntaxTokenKind.NotEquals:
                    return new BinaryExpressionSyntax(left, ParseNotEqualsToken(buffer), ParseValueExpression(buffer));

                case SyntaxTokenKind.GreaterEquals:
                    return new BinaryExpressionSyntax(left, ParseGreaterEqualsToken(buffer), ParseValueExpression(buffer));

                case SyntaxTokenKind.SmallerEquals:
                    return new BinaryExpressionSyntax(left, ParseSmallerEqualsToken(buffer), ParseValueExpression(buffer));

                case SyntaxTokenKind.Greater:
                    return new BinaryExpressionSyntax(left, ParseGreaterToken(buffer), ParseValueExpression(buffer));

                case SyntaxTokenKind.Smaller:
                    return new BinaryExpressionSyntax(left, ParseSmallerToken(buffer), ParseValueExpression(buffer));

                case SyntaxTokenKind.Plus:
                    return new BinaryExpressionSyntax(left, ParsePlusToken(buffer), ParseValueExpression(buffer));

                case SyntaxTokenKind.Minus:
                    return new BinaryExpressionSyntax(left, ParseMinusToken(buffer), ParseValueExpression(buffer));

                case SyntaxTokenKind.Mul:
                    return new BinaryExpressionSyntax(left, ParseMulToken(buffer), ParseValueExpression(buffer));

                case SyntaxTokenKind.Div:
                    return new BinaryExpressionSyntax(left, ParseDivToken(buffer), ParseValueExpression(buffer));

                case SyntaxTokenKind.Mod:
                    return new BinaryExpressionSyntax(left, ParseModToken(buffer), ParseValueExpression(buffer));

                case SyntaxTokenKind.And:
                    return new BinaryExpressionSyntax(left, ParseAndToken(buffer), ParseValueExpression(buffer));

                case SyntaxTokenKind.Or:
                    return new BinaryExpressionSyntax(left, ParseOrToken(buffer), ParseValueExpression(buffer));

                case SyntaxTokenKind.Xor:
                    return new BinaryExpressionSyntax(left, ParseXorToken(buffer), ParseValueExpression(buffer));

                case SyntaxTokenKind.LeftShift:
                    return new BinaryExpressionSyntax(left, ParseLeftShiftToken(buffer), ParseValueExpression(buffer));

                case SyntaxTokenKind.RightShift:
                    return new BinaryExpressionSyntax(left, ParseRightShiftToken(buffer), ParseValueExpression(buffer));

                default:
                    throw CreateException(buffer, "Unknown binary expression.", SyntaxTokenKind.Equals, SyntaxTokenKind.NotEquals,
                        SyntaxTokenKind.GreaterEquals, SyntaxTokenKind.SmallerEquals, SyntaxTokenKind.Greater, SyntaxTokenKind.Smaller);
            }
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
                   HasTokenKind(buffer, SyntaxTokenKind.NotKeyword);
        }

        private UnaryExpressionSyntax ParseUnaryExpression(IBuffer<Level5SyntaxToken> buffer)
        {
            switch (buffer.Peek().Kind)
            {
                case SyntaxTokenKind.Complement:
                    return new UnaryExpressionSyntax(ParseComplementToken(buffer), ParseValueExpression(buffer));

                case SyntaxTokenKind.Minus:
                    return new UnaryExpressionSyntax(ParseMinusToken(buffer), ParseValueExpression(buffer));

                case SyntaxTokenKind.NotKeyword:
                    return new UnaryExpressionSyntax(ParseNotKeywordToken(buffer), ParseValueExpression(buffer));

                default:
                    throw CreateException(buffer, "Unknown unary expression.", SyntaxTokenKind.Complement, SyntaxTokenKind.Minus);
            }
        }

        private ArrayIndexerExpressionSyntax ParseArrayIndexerExpression(IBuffer<Level5SyntaxToken> buffer)
        {
            SyntaxToken bracketOpen = ParseBracketOpenToken(buffer);
            var index = ParseValueExpression(buffer);
            SyntaxToken bracketClose = ParseBracketCloseToken(buffer);

            return new ArrayIndexerExpressionSyntax(bracketOpen, index, bracketClose);
        }

        private MethodInvocationExpressionSyntax ParseMethodInvocationExpression(IBuffer<Level5SyntaxToken> buffer)
        {
            SyntaxToken identifier = ParseIdentifierToken(buffer);
            var methodInvocationParameters = ParseMethodInvocationExpressionParameters(buffer);

            return new MethodInvocationExpressionSyntax(identifier, methodInvocationParameters);
        }

        private MethodInvocationExpressionParametersSyntax ParseMethodInvocationExpressionParameters(IBuffer<Level5SyntaxToken> buffer)
        {
            SyntaxToken parenOpen = ParseParenOpenToken(buffer);
            var parameters = ParseValueList(buffer);
            SyntaxToken parenClose = ParseParenCloseToken(buffer);

            return new MethodInvocationExpressionParametersSyntax(parenOpen, parameters, parenClose);
        }

        private CommaSeparatedSyntaxList<ValueExpressionSyntax>? ParseValueList(IBuffer<Level5SyntaxToken> buffer)
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
                        SyntaxTokenKind.StringLiteral, SyntaxTokenKind.NumericLiteral,
                        SyntaxTokenKind.HashNumericLiteral, SyntaxTokenKind.HashStringLiteral,
                        SyntaxTokenKind.FloatingNumericLiteral);

                parameter = ParseValueExpression(buffer);
                result.Add(parameter);
            }

            return new CommaSeparatedSyntaxList<ValueExpressionSyntax>(result);
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
                   HasTokenKind(buffer, SyntaxTokenKind.HashStringLiteral) ||
                   HasTokenKind(buffer, SyntaxTokenKind.HashNumericLiteral) ||
                   HasTokenKind(buffer, SyntaxTokenKind.FloatingNumericLiteral);
        }

        private ValueExpressionSyntax ParseValueExpression(IBuffer<Level5SyntaxToken> buffer)
        {
            if (HasTokenKind(buffer, SyntaxTokenKind.Variable))
                return new ValueExpressionSyntax(ParseVariableExpression(buffer), ParseValueMetadataParameters(buffer));

            if (HasTokenKind(buffer, SyntaxTokenKind.StringLiteral))
                return new ValueExpressionSyntax(ParseStringLiteralExpression(buffer), ParseValueMetadataParameters(buffer));

            if (HasTokenKind(buffer, SyntaxTokenKind.NumericLiteral))
                return new ValueExpressionSyntax(ParseNumericLiteralExpression(buffer), ParseValueMetadataParameters(buffer));

            if (HasTokenKind(buffer, SyntaxTokenKind.HashNumericLiteral))
                return new ValueExpressionSyntax(ParseHashNumericLiteralExpression(buffer), ParseValueMetadataParameters(buffer));

            if (HasTokenKind(buffer, SyntaxTokenKind.HashStringLiteral))
                return new ValueExpressionSyntax(ParseHashStringLiteralExpression(buffer), ParseValueMetadataParameters(buffer));

            if (HasTokenKind(buffer, SyntaxTokenKind.FloatingNumericLiteral))
                return new ValueExpressionSyntax(ParseFloatingNumericLiteralExpression(buffer), ParseValueMetadataParameters(buffer));

            throw CreateException(buffer, "Unknown value expression.", SyntaxTokenKind.Variable, SyntaxTokenKind.StringLiteral,
                SyntaxTokenKind.NumericLiteral, SyntaxTokenKind.FloatingNumericLiteral, SyntaxTokenKind.HashNumericLiteral,
                SyntaxTokenKind.HashStringLiteral);
        }

        private ValueMetadataParametersSyntax? ParseValueMetadataParameters(IBuffer<Level5SyntaxToken> buffer)
        {
            if (!HasTokenKind(buffer, SyntaxTokenKind.Smaller) || !HasTokenKind(buffer, 2, SyntaxTokenKind.Greater))
                return null;

            SyntaxToken relSmaller = ParseSmallerToken(buffer);
            var parameter = ParseStringLiteralExpression(buffer);
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

        private LiteralExpressionSyntax ParseHashNumericLiteralExpression(IBuffer<Level5SyntaxToken> buffer)
        {
            SyntaxToken literal = ParseHashNumericLiteralToken(buffer);

            return new LiteralExpressionSyntax(literal);
        }

        private LiteralExpressionSyntax ParseHashStringLiteralExpression(IBuffer<Level5SyntaxToken> buffer)
        {
            SyntaxToken literal = ParseHashStringLiteralToken(buffer);

            return new LiteralExpressionSyntax(literal);
        }

        private LiteralExpressionSyntax ParseFloatingNumericLiteralExpression(IBuffer<Level5SyntaxToken> buffer)
        {
            SyntaxToken literal = ParseFloatingNumericLiteralToken(buffer);

            return new LiteralExpressionSyntax(literal);
        }

        private VariableExpressionSyntax ParseVariableExpression(IBuffer<Level5SyntaxToken> buffer)
        {
            SyntaxToken variable = ParseVariableToken(buffer);

            return new VariableExpressionSyntax(variable);
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

        private SyntaxToken ParseOrToken(IBuffer<Level5SyntaxToken> buffer)
        {
            return CreateToken(buffer, SyntaxTokenKind.Or);
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

            return _syntaxFactory.Create(content.Text, (int)expectedKind, leadingTrivia, trailingTrivia);
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
            ILexer<Level5SyntaxToken> lexer = _scriptFactory.CreateLexer(text);
            return _scriptFactory.CreateTokenBuffer(lexer);
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

            throw new InvalidOperationException(message);
        }
    }
}
