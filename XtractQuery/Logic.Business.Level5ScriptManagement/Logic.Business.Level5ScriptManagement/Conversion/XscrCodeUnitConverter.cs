using Logic.Business.Level5ScriptManagement.InternalContract;
using Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses;
using Logic.Domain.Level5.Contract.Script.DataClasses;
using System.Globalization;
using System.Text.RegularExpressions;
using Logic.Business.Level5ScriptManagement.InternalContract.Conversion;
using Logic.Domain.CodeAnalysis.Contract.DataClasses;
using Logic.Domain.Level5.Contract.Script.Xscr.DataClasses;

namespace Logic.Business.Level5ScriptManagement.Conversion;

internal class XscrCodeUnitConverter : IXscrCodeUnitConverter
{
    private readonly Regex _subPattern = new("^sub[0-9]+$", RegexOptions.Compiled);
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
        int instructionType = GetInstructionType(methodInvocation.Identifier);
        int argumentIndex = result.Arguments.Count;

        if (methodInvocation.Parameters.ParameterList != null)
            foreach (ValueExpressionSyntax parameter in methodInvocation.Parameters.ParameterList.Elements)
                AddArgument(result, parameter);

        int argumentCount = result.Arguments.Count - argumentIndex;
        AddInstruction(result, instructionType, argumentIndex, argumentCount);
    }

    private int GetInstructionType(SyntaxToken identifier)
    {
        if (_subPattern.IsMatch(identifier.Text))
            return GetNumberFromStringEnd(identifier.Text);

        if (_methodNameMapper.MapsMethodName(identifier.Text))
            return _methodNameMapper.GetInstructionType(identifier.Text);

        throw CreateException("Could not determine instruction type.", identifier.Location);
    }

    private void AddInstruction(XscrScriptFile result, int instructionType, int argumentIndex, int argumentCount)
    {
        result.Instructions.Add(new XscrScriptInstruction
        {
            Type = (short)instructionType,
            ArgumentIndex = (short)argumentIndex,
            ArgumentCount = (short)argumentCount
        });
    }

    private void AddArgument(XscrScriptFile result, ValueExpressionSyntax parameter)
    {
        switch (parameter.Value)
        {
            case LiteralExpressionSyntax literalExpression:
                AddArgument(result, literalExpression, parameter.MetadataParameters);
                break;

            default:
                throw CreateException($"Invalid value expression {parameter.Value.GetType().Name}.", parameter.Location);
        }
    }

    private void AddArgument(XscrScriptFile result, LiteralExpressionSyntax literal, ValueMetadataParametersSyntax? metadata)
    {
        int rawArgumentType = -1;
        ScriptArgumentType type;
        object value;

        switch (literal.Literal.RawKind)
        {
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
                type = ScriptArgumentType.Float;
                value = GetFloatingNumericLiteral(literal);
                break;

            case (int)SyntaxTokenKind.StringLiteral:
                if (metadata != null)
                    rawArgumentType = GetNumericLiteral(metadata.Parameter);

                type = ScriptArgumentType.String;
                value = GetStringLiteral(literal);
                break;

            default:
                throw CreateException($"Invalid literal {(SyntaxTokenKind)literal.Literal.RawKind}.", literal.Location,
                    SyntaxTokenKind.NumericLiteral, SyntaxTokenKind.HashNumericLiteral, SyntaxTokenKind.HashStringLiteral,
                    SyntaxTokenKind.FloatingNumericLiteral, SyntaxTokenKind.StringLiteral);
        }

        AddArgument(result, type, value, rawArgumentType);
    }

    private void AddArgument(XscrScriptFile result, ScriptArgumentType type, object value, int rawArgumentType = -1)
    {
        result.Arguments.Add(new XscrScriptArgument
        {
            RawArgumentType = rawArgumentType,
            Type = type,
            Value = value
        });
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

    private float GetFloatingNumericLiteral(LiteralExpressionSyntax literal)
    {
        if (literal.Literal.RawKind != (int)SyntaxTokenKind.FloatingNumericLiteral)
            throw CreateException($"Invalid literal {(SyntaxTokenKind)literal.Literal.RawKind}.", literal.Location, SyntaxTokenKind.FloatingNumericLiteral);

        return float.Parse(literal.Literal.Text[..^1], CultureInfo.GetCultureInfo("en-gb"));
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
}