using Logic.Business.Level5ScriptManagement.InternalContract;
using Logic.Business.Level5ScriptManagement.InternalContract.Conversion;
using Logic.Domain.CodeAnalysis.Contract.DataClasses;
using Logic.Domain.CodeAnalysis.Contract.Level5;
using Logic.Domain.Level5.Contract.DataClasses.Script.Gds;
using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

namespace Logic.Business.Level5ScriptManagement.Conversion;

internal class GdsScriptFileConverter : IGdsScriptFileConverter
{
    private readonly IMethodNameMapper _methodNameMapper;
    private readonly ILevel5SyntaxFactory _syntaxFactory;

    public GdsScriptFileConverter(IMethodNameMapper methodNameMapper, ILevel5SyntaxFactory syntaxFactory)
    {
        _methodNameMapper = methodNameMapper;
        _syntaxFactory = syntaxFactory;
    }

    public CodeUnitSyntax CreateCodeUnit(GdsScriptFile script)
    {
        IReadOnlyList<MethodDeclarationSyntax> methods = CreateMethodDeclarations(script);

        return new CodeUnitSyntax(methods);
    }

    private IReadOnlyList<MethodDeclarationSyntax> CreateMethodDeclarations(GdsScriptFile script)
    {
        return [CreateMethodDeclaration(script)];
    }

    private MethodDeclarationSyntax CreateMethodDeclaration(GdsScriptFile script)
    {
        SyntaxToken identifier = _syntaxFactory.Identifier("Main");
        var parameters = CreateMethodDeclarationParameters();
        var body = CreateMethodDeclarationBody(script);

        return new MethodDeclarationSyntax(identifier, null, parameters, body);
    }

    private MethodDeclarationParametersSyntax CreateMethodDeclarationParameters()
    {
        SyntaxToken parenOpen = _syntaxFactory.Token(SyntaxTokenKind.ParenOpen);
        SyntaxToken parenClose = _syntaxFactory.Token(SyntaxTokenKind.ParenClose);

        return new MethodDeclarationParametersSyntax(parenOpen, null, parenClose);
    }

    private MethodDeclarationBodySyntax CreateMethodDeclarationBody(GdsScriptFile script)
    {
        SyntaxToken curlyOpen = _syntaxFactory.Token(SyntaxTokenKind.CurlyOpen);
        var expressions = CreateStatements(script);
        SyntaxToken curlyClose = _syntaxFactory.Token(SyntaxTokenKind.CurlyClose);

        return new MethodDeclarationBodySyntax(curlyOpen, expressions, curlyClose);
    }

    private IReadOnlyList<StatementSyntax> CreateStatements(GdsScriptFile script)
    {
        var result = new List<StatementSyntax>();

        foreach (GdsScriptInstruction instruction in script.Instructions)
        {
            if (instruction.Jump is not null)
                result.Add(CreateGotoLabelStatement(instruction.Jump));

            result.Add(CreateStatement(instruction));
        }

        return result;
    }

    private GotoLabelStatementSyntax CreateGotoLabelStatement(GdsScriptJump jump)
    {
        var labelLiteral = CreateStringLiteralExpression(jump.Label);
        SyntaxToken colonToken = _syntaxFactory.Token(SyntaxTokenKind.Colon);

        return new GotoLabelStatementSyntax(labelLiteral, colonToken);
    }

    private StatementSyntax CreateStatement(GdsScriptInstruction instruction)
    {
        switch (instruction.Type)
        {
            case 7:
                return CreateMethodInvocationExpression(_syntaxFactory.Identifier("cmd7"), instruction);

            case 8:
                return CreateMethodInvocationExpression(_syntaxFactory.Identifier("cmd8"), instruction);

            case 9:
                return CreateMethodInvocationExpression(_syntaxFactory.Identifier("cmd9"), instruction);

            case 11:
                return CreateMethodInvocationExpression(_syntaxFactory.Identifier("cmd11"), instruction);

            case 12:
                return CreateReturnStatement();

            default:
                return CreateMethodInvocationExpression(instruction);
        }
    }

    private ReturnStatementSyntax CreateReturnStatement()
    {
        SyntaxToken returnToken = _syntaxFactory.Token(SyntaxTokenKind.ReturnKeyword);
        SyntaxToken semicolon = _syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        return new ReturnStatementSyntax(returnToken, null, semicolon);
    }

    private MethodInvocationStatementSyntax CreateMethodInvocationExpression(GdsScriptInstruction instruction)
    {
        SyntaxToken identifier = CreateMethodNameIdentifier(instruction);

        return CreateMethodInvocationExpression(identifier, instruction);
    }

    private MethodInvocationStatementSyntax CreateMethodInvocationExpression(SyntaxToken methodName, GdsScriptInstruction instruction)
    {
        var parameters = CreateMethodInvocationExpressionParameters(instruction);
        SyntaxToken semicolon = _syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        return new MethodInvocationStatementSyntax(methodName, null, parameters, semicolon);
    }

    private SyntaxToken CreateMethodNameIdentifier(GdsScriptInstruction instruction)
    {
        if (instruction.Arguments.Length < 1)
            throw new InvalidOperationException("Missing call type for instruction.");

        if (instruction.Arguments[0].Type is not GdsScriptArgumentType.Int)
            throw new InvalidOperationException("Wrong call type for instruction.");

        var instructionType = (int)instruction.Arguments[0].Value!;

        if (_methodNameMapper.MapsInstructionType(instructionType))
        {
            string mappedMethod = _methodNameMapper.GetMethodName(instructionType);
            return _syntaxFactory.Identifier(mappedMethod);
        }

        return _syntaxFactory.Identifier($"sub{instructionType}");
    }

    private MethodInvocationParametersSyntax CreateMethodInvocationExpressionParameters(GdsScriptInstruction instruction)
    {
        SyntaxToken parenOpen = _syntaxFactory.Token(SyntaxTokenKind.ParenOpen);
        var parameterList = CreateValueList(instruction);
        SyntaxToken parenClose = _syntaxFactory.Token(SyntaxTokenKind.ParenClose);

        return new MethodInvocationParametersSyntax(parenOpen, parameterList, parenClose);
    }

    private CommaSeparatedSyntaxList<ValueExpressionSyntax>? CreateValueList(GdsScriptInstruction instruction)
    {
        if (instruction.Arguments.Length <= 0)
            return null;

        GdsScriptArgument[] arguments = instruction.Arguments;

        if (instruction.Type is 0)
            arguments = arguments[1..];

        var result = new List<ValueExpressionSyntax>();
        foreach (GdsScriptArgument argument in arguments)
            result.Add(CreateValueExpression(argument));

        return new CommaSeparatedSyntaxList<ValueExpressionSyntax>(result);
    }

    private ValueExpressionSyntax CreateValueExpression(GdsScriptArgument argument)
    {
        return CreateValueExpression(argument.Value, argument.Type);
    }

    private ValueExpressionSyntax CreateValueExpression(object? value, GdsScriptArgumentType argumentType, int rawArgumentType = -1)
    {
        ExpressionSyntax parameter = CreateArgumentExpression(value, argumentType);

        ValueMetadataParametersSyntax? parameters = null;
        if (rawArgumentType >= 0)
            parameters = CreateValueMetadataParameters(rawArgumentType);

        return new ValueExpressionSyntax(parameter, parameters);
    }

    private ValueMetadataParametersSyntax CreateValueMetadataParameters(int rawArgumentType)
    {
        SyntaxToken relSmaller = _syntaxFactory.Token(SyntaxTokenKind.Smaller);
        var metadataParameter = CreateNumericLiteralExpression(rawArgumentType);
        SyntaxToken relBigger = _syntaxFactory.Token(SyntaxTokenKind.Greater);

        return new ValueMetadataParametersSyntax(relSmaller, metadataParameter, relBigger);
    }

    private ExpressionSyntax CreateArgumentExpression(object? value, GdsScriptArgumentType argumentType)
    {
        return CreateLiteralExpression(value, argumentType);
    }

    private LiteralExpressionSyntax CreateLiteralExpression(object? value, GdsScriptArgumentType argumentType)
    {
        switch (argumentType)
        {
            case GdsScriptArgumentType.Int:
                return CreateNumericLiteralExpression((int)value!);

            case GdsScriptArgumentType.Float:
                return CreateFloatingNumericLiteralExpression((float)value!);

            case GdsScriptArgumentType.String:
                return CreateStringLiteralExpression((string)value!);

            case GdsScriptArgumentType.Jump:
                return CreateHashStringExpression((string)value!);

            default:
                throw new InvalidOperationException($"Unknown argument type {argumentType}.");
        }
    }

    private LiteralExpressionSyntax CreateNumericLiteralExpression(int value)
    {
        return new LiteralExpressionSyntax(_syntaxFactory.NumericLiteral(value));
    }

    private LiteralExpressionSyntax CreateFloatingNumericLiteralExpression(float value)
    {
        return new LiteralExpressionSyntax(_syntaxFactory.FloatingNumericLiteral(value));
    }

    private LiteralExpressionSyntax CreateStringLiteralExpression(string value)
    {
        return new LiteralExpressionSyntax(_syntaxFactory.StringLiteral(value));
    }

    private LiteralExpressionSyntax CreateHashStringExpression(string value)
    {
        return new LiteralExpressionSyntax(_syntaxFactory.HashStringLiteral(value));
    }
}