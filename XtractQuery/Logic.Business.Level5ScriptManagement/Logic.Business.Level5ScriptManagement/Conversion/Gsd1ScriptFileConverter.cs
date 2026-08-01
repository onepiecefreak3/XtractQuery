using Logic.Business.Level5ScriptManagement.InternalContract;
using Logic.Business.Level5ScriptManagement.InternalContract.Conversion;
using Logic.Domain.CodeAnalysis.Contract.DataClasses;
using Logic.Domain.CodeAnalysis.Contract.Level5;
using Logic.Domain.Level5.Contract.DataClasses.Script.Gsd1;
using Logic.Domain.Level5.Contract.DataClasses.Script;
using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

namespace Logic.Business.Level5ScriptManagement.Conversion;

internal class Gsd1ScriptFileConverter : IGsd1ScriptFileConverter
{
    private readonly IMethodNameMapper _methodNameMapper;
    private readonly ILevel5SyntaxFactory _syntaxFactory;

    public Gsd1ScriptFileConverter(IMethodNameMapper methodNameMapper, ILevel5SyntaxFactory syntaxFactory)
    {
        _methodNameMapper = methodNameMapper;
        _syntaxFactory = syntaxFactory;
    }

    public CodeUnitSyntax CreateCodeUnit(Gsd1ScriptFile script)
    {
        IReadOnlyList<MethodDeclarationSyntax> methods = CreateMethodDeclarations(script);

        return new CodeUnitSyntax(methods);
    }

    private IReadOnlyList<MethodDeclarationSyntax> CreateMethodDeclarations(Gsd1ScriptFile script)
    {
        return [CreateMethodDeclaration(script)];
    }

    private MethodDeclarationSyntax CreateMethodDeclaration(Gsd1ScriptFile script)
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

    private MethodDeclarationBodySyntax CreateMethodDeclarationBody(Gsd1ScriptFile script)
    {
        SyntaxToken curlyOpen = _syntaxFactory.Token(SyntaxTokenKind.CurlyOpen);
        var expressions = CreateStatements(script);
        SyntaxToken curlyClose = _syntaxFactory.Token(SyntaxTokenKind.CurlyClose);

        return new MethodDeclarationBodySyntax(curlyOpen, expressions, curlyClose);
    }

    private IReadOnlyList<StatementSyntax> CreateStatements(Gsd1ScriptFile script)
    {
        var result = new List<StatementSyntax>();

        foreach (Gsd1ScriptInstruction instruction in script.Instructions)
            result.Add(CreateStatement(instruction, script));

        return result;
    }

    private StatementSyntax CreateStatement(Gsd1ScriptInstruction instruction, Gsd1ScriptFile script)
    {
        return CreateMethodInvocationExpression(instruction, script);
    }

    private MethodInvocationStatementSyntax CreateMethodInvocationExpression(Gsd1ScriptInstruction instruction, Gsd1ScriptFile script)
    {
        NameSyntax identifier = CreateMethodNameIdentifier(instruction);
        var metadata = CreateMethodInvocationMetadata(instruction, script);
        var parameters = CreateMethodInvocationExpressionParameters(instruction, script);
        SyntaxToken semicolon = _syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        return new MethodInvocationStatementSyntax(identifier, metadata, parameters, semicolon);
    }

    private NameSyntax CreateMethodNameIdentifier(Gsd1ScriptInstruction instruction)
    {
        if (_methodNameMapper.MapsInstructionType(instruction.Type))
        {
            string mappedMethod = _methodNameMapper.GetMethodName(instruction.Type);
            return CreateName(mappedMethod);
        }

        return CreateName($"sub{instruction.Type}");
    }

    private MethodInvocationMetadataSyntax? CreateMethodInvocationMetadata(Gsd1ScriptInstruction instruction, Gsd1ScriptFile script)
    {
        if (instruction.ArgumentIndex >= script.Arguments.Count ||
            script.Arguments[instruction.ArgumentIndex].RawArgumentType < 0)
            return null;

        SyntaxToken relSmaller = _syntaxFactory.Token(SyntaxTokenKind.Smaller);
        var value = CreateNumericLiteralExpression(script.Arguments[instruction.ArgumentIndex].RawArgumentType);
        SyntaxToken relBigger = _syntaxFactory.Token(SyntaxTokenKind.Greater);

        return new MethodInvocationMetadataSyntax(relSmaller, value, relBigger);
    }

    private MethodInvocationParametersSyntax CreateMethodInvocationExpressionParameters(Gsd1ScriptInstruction instruction, Gsd1ScriptFile script)
    {
        SyntaxToken parenOpen = _syntaxFactory.Token(SyntaxTokenKind.ParenOpen);
        var parameterList = CreateMethodInvocationParameterList(instruction, script);
        SyntaxToken parenClose = _syntaxFactory.Token(SyntaxTokenKind.ParenClose);

        return new MethodInvocationParametersSyntax(parenOpen, parameterList, parenClose);
    }

    private CommaSeparatedSyntaxList<ExpressionSyntax>? CreateMethodInvocationParameterList(Gsd1ScriptInstruction instruction, Gsd1ScriptFile script)
    {
        int argumentCount = instruction.ArgumentCount;
        int argumentIndex = instruction.ArgumentIndex;

        if (argumentCount <= 0)
            return null;

        if (script.Arguments.Count < argumentIndex + argumentCount)
            throw new InvalidOperationException($"Can't fetch all arguments ({script.Arguments.Count} >= {argumentIndex + argumentCount})");

        var result = new List<ExpressionSyntax>();
        for (int i = argumentIndex; i < argumentIndex + argumentCount; i++)
            result.Add(CreateValueExpression(script.Arguments[i]));

        return new CommaSeparatedSyntaxList<ExpressionSyntax>(result);
    }

    private ValueExpressionSyntax CreateValueExpression(Gsd1ScriptArgument argument)
    {
        return CreateValueExpression(argument.Value, argument.Type, argument.RawArgumentType);
    }

    private ValueExpressionSyntax CreateValueExpression(object value, ScriptArgumentType argumentType, int rawArgumentType = -1)
    {
        ExpressionSyntax parameter = CreateArgumentExpression(value, argumentType);

        ValueMetadataParametersSyntax? parameters = null;
        if (rawArgumentType >= 0 && parameter is not LiteralExpressionSyntax { Literal.RawKind: (int)SyntaxTokenKind.UndefinedKeyword })
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

    private ExpressionSyntax CreateArgumentExpression(object value, ScriptArgumentType argumentType)
    {
        switch (argumentType)
        {
            default:
                return CreateLiteralExpression(value, argumentType);
        }
    }

    private ExpressionSyntax CreateLiteralExpression(object value, ScriptArgumentType argumentType)
    {
        switch (argumentType)
        {
            case ScriptArgumentType.Raw:
                return value is 0u ?
                    CreateUndefinedLiteralExpression() :
                    CreateNumericLiteralExpression((uint)value);

            case ScriptArgumentType.Int:
                return CreateNumericLiteralExpression((int)value);

            case ScriptArgumentType.StringHash:
                if (value is string stringValue)
                    return CreateHashStringExpression(stringValue);

                return CreateHashNumericLiteral((uint)value);

            case ScriptArgumentType.Float:
                return CreateFloatingNumericLiteralExpression((float)value);

            case ScriptArgumentType.String:
                return CreateStringLiteralExpression((string)value);

            default:
                throw new InvalidOperationException($"Unknown argument type {argumentType}.");
        }
    }

    private LiteralExpressionSyntax CreateUndefinedLiteralExpression()
    {
        return new LiteralExpressionSyntax(_syntaxFactory.Token(SyntaxTokenKind.UndefinedKeyword));
    }

    private LiteralExpressionSyntax CreateNumericLiteralExpression(uint value)
    {
        return new LiteralExpressionSyntax(_syntaxFactory.NumericLiteral(value));
    }

    private LiteralExpressionSyntax CreateNumericLiteralExpression(int value)
    {
        return new LiteralExpressionSyntax(_syntaxFactory.NumericLiteral(value));
    }

    private LiteralExpressionSyntax CreateHashStringExpression(string value)
    {
        return new LiteralExpressionSyntax(_syntaxFactory.HashStringLiteral(value));
    }

    private LiteralExpressionSyntax CreateHashNumericLiteral(uint value)
    {
        return new LiteralExpressionSyntax(_syntaxFactory.HashNumericLiteral(value));
    }

    private ExpressionSyntax CreateFloatingNumericLiteralExpression(float value)
    {
        if (value is float.PositiveInfinity)
            return new LiteralExpressionSyntax(_syntaxFactory.Token(SyntaxTokenKind.InfKeyword));

        if (value is float.NegativeInfinity)
            return new UnaryExpressionSyntax(_syntaxFactory.Token(SyntaxTokenKind.Minus), CreateValueExpression(float.PositiveInfinity, ScriptArgumentType.Float));

        if (value is float.NaN)
            return new LiteralExpressionSyntax(_syntaxFactory.Token(SyntaxTokenKind.NanKeyword));

        return new LiteralExpressionSyntax(_syntaxFactory.FloatingNumericLiteral(value));
    }

    private LiteralExpressionSyntax CreateStringLiteralExpression(string value)
    {
        return new LiteralExpressionSyntax(_syntaxFactory.StringLiteral(value));
    }

    private NameSyntax CreateName(string name)
    {
        if (name.Contains('.'))
            return new SimpleNameSyntax(_syntaxFactory.Identifier(name));

        NameSyntax? result = null;

        foreach (string part in name.Split('.').Reverse())
        {
            if (result is null)
                result = new SimpleNameSyntax(_syntaxFactory.Identifier(part));
            else
                result = new QualifiedNameSyntax(new SimpleNameSyntax(_syntaxFactory.Identifier(part)), _syntaxFactory.Token(SyntaxTokenKind.Dot), result);
        }

        return result!;
    }
}