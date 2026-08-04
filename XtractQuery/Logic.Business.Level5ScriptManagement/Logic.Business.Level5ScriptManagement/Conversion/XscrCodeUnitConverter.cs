using Logic.Business.Level5ScriptManagement.InternalContract;
using System.Globalization;
using System.Text.RegularExpressions;
using Logic.Business.Level5ScriptManagement.InternalContract.Conversion;
using Logic.Domain.CodeAnalysis.Contract.DataClasses;
using Logic.Domain.Level5.Contract.DataClasses.Script.Xscr;
using Logic.Domain.Level5.Contract.DataClasses.Script;
using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

namespace Logic.Business.Level5ScriptManagement.Conversion;

internal partial class XscrCodeUnitConverter : IXscrCodeUnitConverter
{
    private readonly IMethodNameMapper _methodNameMapper;

    public XscrCodeUnitConverter(IMethodNameMapper methodNameMapper)
    {
        _methodNameMapper = methodNameMapper;
    }

    public XscrScriptFile CreateScriptFile(CodeUnitSyntax tree)
    {
        var result = new XscrScriptFile
        {
            Instructions = new List<XscrScriptInstruction>(),
            Arguments = new List<XscrScriptArgument>()
        };

        AddFunctions(result, tree.MethodDeclarations);

        return result;
    }

    private void AddFunctions(XscrScriptFile result, IReadOnlyList<MethodDeclarationSyntax> methods)
    {
        if (methods.Count <= 0)
            return;

        AddStatements(result, methods[0].Body.Expressions);
    }

    private void AddStatements(XscrScriptFile result, IReadOnlyList<StatementSyntax> statements)
    {
        foreach (StatementSyntax statement in statements)
        {
            if (statement is not MethodInvocationStatementSyntax methodInvocation)
                throw CreateException("Only method invocations are allowed.", statement.Location);

            AddMethodInvocationStatement(result, methodInvocation);
        }
    }

    private void AddMethodInvocationStatement(XscrScriptFile result, MethodInvocationStatementSyntax methodInvocation)
    {
        int instructionType = GetInstructionType(methodInvocation.Name);
        int argumentIndex = result.Arguments.Count;

        if (methodInvocation.Parameters.ParameterList != null)
            foreach (var parameter in methodInvocation.Parameters.ParameterList.Elements)
            {
                if (parameter is not ValueExpressionSyntax valueParameter)
                    throw CreateException($"Invalid expression {parameter.GetType().Name} for method invocation parameter.", parameter.Location);

                AddArgument(result, valueParameter);
            }

        int argumentCount = result.Arguments.Count - argumentIndex;
        AddInstruction(result, instructionType, argumentIndex, argumentCount);
    }

    private int GetInstructionType(NameSyntax name)
    {
        string composedName = GetName(name);

        if (SubPattern().IsMatch(composedName))
            return GetNumberFromStringEnd(composedName);

        if (_methodNameMapper.MapsMethodName(composedName))
            return _methodNameMapper.GetInstructionType(composedName);

        throw CreateException("Could not determine instruction type.", name.Location);
    }

    private void AddInstruction(XscrScriptFile result, int instructionType, int argumentIndex, int argumentCount)
    {
        result.Instructions.Add(new XscrScriptInstruction
        {
            Type = (short)instructionType,
            ArgumentIndex = argumentIndex,
            ArgumentCount = (short)argumentCount
        });
    }

    private void AddArgument(XscrScriptFile result, ValueExpressionSyntax parameter)
    {
        switch (parameter.Value)
        {
            case UnaryExpressionSyntax unaryExpression:
                AddArgument(result, unaryExpression, parameter.MetadataParameters);
                break;

            case LiteralExpressionSyntax literalExpression:
                AddArgument(result, literalExpression, parameter.MetadataParameters);
                break;

            default:
                throw CreateException($"Invalid value expression {parameter.Value.GetType().Name}.", parameter.Location);
        }
    }

    private void AddArgument(XscrScriptFile result, UnaryExpressionSyntax unary, ValueMetadataParametersSyntax? metadata)
    {
        var type = ScriptArgumentType.Float;
        float value = GetFloatingNumericLiteral(unary);

        AddArgument(result, type, value, metadata);
    }

    private void AddArgument(XscrScriptFile result, LiteralExpressionSyntax literal, ValueMetadataParametersSyntax? metadata)
    {
        ScriptArgumentType type;
        object value;

        switch (literal.Literal.RawKind)
        {
            case (int)SyntaxTokenKind.UndefinedKeyword:
                type = ScriptArgumentType.Raw;
                value = 0;
                break;

            case (int)SyntaxTokenKind.NumericLiteral:
                type = ScriptArgumentType.Int;
                value = GetNumericLiteral(literal);
                break;

            case (int)SyntaxTokenKind.HashNumericLiteral:
                type = ScriptArgumentType.StringHash;
                value = GetHashNumericLiteral(literal);
                break;

            case (int)SyntaxTokenKind.HashStringLiteral:
                type = ScriptArgumentType.StringHash;
                value = GetHashStringLiteral(literal);
                break;

            case (int)SyntaxTokenKind.FloatingNumericLiteral:
            case (int)SyntaxTokenKind.Infinite:
            case (int)SyntaxTokenKind.InfinityKeyword:
            case (int)SyntaxTokenKind.InfKeyword:
            case (int)SyntaxTokenKind.NanKeyword:
                type = ScriptArgumentType.Float;
                value = GetFloatingNumericLiteral(literal);
                break;

            case (int)SyntaxTokenKind.StringLiteral:
                type = ScriptArgumentType.String;
                value = GetStringLiteral(literal);
                break;

            default:
                throw CreateException($"Invalid literal {(SyntaxTokenKind)literal.Literal.RawKind}.", literal.Location,
                    SyntaxTokenKind.NumericLiteral, SyntaxTokenKind.HashNumericLiteral, SyntaxTokenKind.HashStringLiteral,
                    SyntaxTokenKind.Infinite, SyntaxTokenKind.InfinityKeyword, SyntaxTokenKind.InfKeyword,
                    SyntaxTokenKind.NanKeyword, SyntaxTokenKind.StringLiteral);
        }

        AddArgument(result, type, value, metadata);
    }

    private void AddArgument(XscrScriptFile result, ScriptArgumentType type, object value, ValueMetadataParametersSyntax? metadata)
    {
        var rawArgumentType = -1;
        if (metadata != null)
            rawArgumentType = GetNumericLiteral(metadata.Parameter);

        result.Arguments.Add(new XscrScriptArgument
        {
            RawArgumentType = rawArgumentType,
            Type = type,
            Value = value
        });
    }

    private string GetName(NameSyntax name)
    {
        switch (name)
        {
            case SimpleNameSyntax simpleName:
                return simpleName.Identifier.Text;

            case QualifiedNameSyntax qualifiedName:
                return GetName(qualifiedName.Left) + "." + GetName(qualifiedName.Right);

            default:
                throw CreateException("Invalid name syntax.", name.Location);
        }
    }

    private int GetNumericLiteral(LiteralExpressionSyntax literal)
    {
        if (literal.Literal.RawKind != (int)SyntaxTokenKind.NumericLiteral)
            throw CreateException($"Invalid literal {(SyntaxTokenKind)literal.Literal.RawKind}.", literal.Location, SyntaxTokenKind.NumericLiteral);

        return literal.Literal.Text.StartsWith("0x") ?
            int.Parse(literal.Literal.Text[2..], NumberStyles.HexNumber) :
            int.Parse(literal.Literal.Text);
    }

    private uint GetHashNumericLiteral(LiteralExpressionSyntax literal)
    {
        if (literal.Literal.RawKind != (int)SyntaxTokenKind.HashNumericLiteral)
            throw CreateException($"Invalid literal {(SyntaxTokenKind)literal.Literal.RawKind}.", literal.Location, SyntaxTokenKind.HashNumericLiteral);

        return literal.Literal.Text.StartsWith("0x") ?
            uint.Parse(literal.Literal.Text[2..^1], NumberStyles.HexNumber) :
            uint.Parse(literal.Literal.Text[..^1]);
    }

    private string GetHashStringLiteral(LiteralExpressionSyntax literal)
    {
        if (literal.Literal.RawKind != (int)SyntaxTokenKind.HashStringLiteral)
            throw CreateException($"Invalid literal {(SyntaxTokenKind)literal.Literal.RawKind}.", literal.Location, SyntaxTokenKind.HashStringLiteral);

        return literal.Literal.Text[1..^2];
    }

    private float GetFloatingNumericLiteral(ExpressionSyntax expression)
    {
        if (expression is LiteralExpressionSyntax literal)
        {
            if (literal.Literal.RawKind is (int)SyntaxTokenKind.Infinite or (int)SyntaxTokenKind.InfinityKeyword or (int)SyntaxTokenKind.InfKeyword)
                return float.PositiveInfinity;

            if (literal.Literal.RawKind is (int)SyntaxTokenKind.NanKeyword)
                return float.NaN;

            if (literal.Literal.RawKind is (int)SyntaxTokenKind.FloatingNumericLiteral)
                return float.Parse(literal.Literal.Text[..^1], CultureInfo.GetCultureInfo("en-gb"));

            throw CreateException($"Invalid literal {(SyntaxTokenKind)literal.Literal.RawKind}.", expression.Location, SyntaxTokenKind.FloatingNumericLiteral,
                SyntaxTokenKind.Infinite, SyntaxTokenKind.InfinityKeyword, SyntaxTokenKind.InfKeyword, SyntaxTokenKind.NanKeyword);
        }

        if (expression is UnaryExpressionSyntax { Value: ValueExpressionSyntax value })
        {
            if (value.Value is LiteralExpressionSyntax { Literal.RawKind: (int)SyntaxTokenKind.Infinite or (int)SyntaxTokenKind.InfinityKeyword or (int)SyntaxTokenKind.InfKeyword })
                return float.NegativeInfinity;

            if (value.Value is LiteralExpressionSyntax { Literal.RawKind: (int)SyntaxTokenKind.NanKeyword })
                return float.NaN;
        }

        throw CreateException("Invalid floating literal.", expression.Location, SyntaxTokenKind.FloatingNumericLiteral,
            SyntaxTokenKind.Infinite, SyntaxTokenKind.InfinityKeyword, SyntaxTokenKind.InfKeyword, SyntaxTokenKind.NanKeyword, SyntaxTokenKind.Minus);
    }

    private string GetStringLiteral(LiteralExpressionSyntax literal)
    {
        if (literal.Literal.RawKind != (int)SyntaxTokenKind.StringLiteral)
            throw CreateException($"Invalid literal {(SyntaxTokenKind)literal.Literal.RawKind}.", literal.Location, SyntaxTokenKind.StringLiteral);

        return literal.Literal.Text[1..^1].Replace("\\\"", "\"");
    }

    private int GetNumberFromStringEnd(string text)
    {
        int startIndex = text.Length;
        while (text[startIndex - 1] >= '0' && text[startIndex - 1] <= '9')
            startIndex--;

        return int.Parse(text[startIndex..]);
    }

    private Exception CreateException(string message, SyntaxLocation location, params SyntaxTokenKind[] expected)
    {
        message = $"{message} (Line {location.Line}, Column {location.Column})";

        if (expected.Length > 0)
        {
            message = expected.Length == 1 ?
                $"{message} (Expected {expected[0]})" :
                $"{message} (Expected any of {string.Join(", ", expected)})";
        }

        return new InvalidOperationException(message);
    }

    [GeneratedRegex("^sub[0-9]+$")]
    private static partial Regex SubPattern();
}
