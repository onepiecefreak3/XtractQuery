using Logic.Business.Level5ScriptManagement.InternalContract;
using System.Globalization;
using System.Text.RegularExpressions;
using Logic.Business.Level5ScriptManagement.InternalContract.Conversion;
using Logic.Domain.CodeAnalysis.Contract.DataClasses;
using Logic.Domain.Level5.Contract.DataClasses.Script.Gds;
using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

namespace Logic.Business.Level5ScriptManagement.Conversion;

internal class GdsCodeUnitConverter : IGdsCodeUnitConverter
{
    private readonly Regex _subPattern = new("^sub[0-9]+$", RegexOptions.Compiled);
    private readonly IMethodNameMapper _methodNameMapper;

    public GdsCodeUnitConverter(IMethodNameMapper methodNameMapper)
    {
        _methodNameMapper = methodNameMapper;
    }

    public GdsScriptFile CreateScriptFile(CodeUnitSyntax tree)
    {
        var instructions = new List<GdsScriptInstruction>();

        AddFunctions(instructions, tree.MethodDeclarations);

        var result = new GdsScriptFile
        {
            Instructions = [.. instructions]
        };

        return result;
    }

    private void AddFunctions(List<GdsScriptInstruction> instructions, IReadOnlyList<MethodDeclarationSyntax> methods)
    {
        if (methods.Count <= 0)
            return;

        AddStatements(instructions, methods[0].Body.Expressions);
    }

    private void AddStatements(List<GdsScriptInstruction> instructions, IReadOnlyList<StatementSyntax> statements)
    {
        GdsScriptJump? jump = null;

        foreach (StatementSyntax statement in statements)
        {
            switch (statement)
            {
                case GotoLabelStatementSyntax gotoLabelStatement:
                    if (jump is not null)
                        throw CreateException("Only one jump label is allowed per instruction.", gotoLabelStatement.Location);

                    jump = new GdsScriptJump
                    {
                        Label = gotoLabelStatement.Label.Literal.Text[1..^1]
                    };
                    continue;

                case MethodInvocationStatementSyntax methodInvocation:
                    if (IsCommand(methodInvocation))
                        AddCommandInvocationStatement(instructions, methodInvocation, jump);
                    else
                        AddMethodInvocationStatement(instructions, methodInvocation, jump);
                    break;

                case ReturnStatementSyntax:
                    AddReturnStatement(instructions, jump);
                    break;

                default:
                    throw CreateException("Only method invocations or returns are allowed.", statement.Location);
            }

            jump = null;
        }
    }

    private bool IsCommand(MethodInvocationStatementSyntax methodInvocation)
    {
        switch (methodInvocation.Identifier.Text)
        {
            case "cmd7":
            case "cmd8":
            case "cmd9":
            case "cmd11":
                return true;

            default:
                return false;
        }
    }

    private void AddReturnStatement(List<GdsScriptInstruction> instructions, GdsScriptJump? jump = null)
    {
        AddInstruction(instructions, 12, [], jump);
    }

    private void AddCommandInvocationStatement(List<GdsScriptInstruction> instructions, MethodInvocationStatementSyntax methodInvocation, GdsScriptJump? jump = null)
    {
        switch (methodInvocation.Identifier.Text)
        {
            case "cmd7":
                if (methodInvocation.Parameters.ParameterList is null || methodInvocation.Parameters.ParameterList.Elements.Count <= 0)
                    throw CreateException("No jump parameter given for cmd7.", methodInvocation.Parameters.Location);

                var arguments = new List<GdsScriptArgument>();
                AddArgument(arguments, methodInvocation.Parameters.ParameterList.Elements[0]);

                AddInstruction(instructions, 7, arguments, jump);
                break;

            case "cmd8":
                AddInstruction(instructions, 8, [], jump);
                break;

            case "cmd9":
                AddInstruction(instructions, 9, [], jump);
                break;

            case "cmd11":
                AddInstruction(instructions, 11, [], jump);
                break;
        }
    }

    private void AddMethodInvocationStatement(List<GdsScriptInstruction> instructions, MethodInvocationStatementSyntax methodInvocation, GdsScriptJump? jump = null)
    {
        int invocationType = GetInvocationType(methodInvocation.Identifier);

        var arguments = new List<GdsScriptArgument>();
        AddArgument(arguments, GdsScriptArgumentType.Int, invocationType);

        if (methodInvocation.Parameters.ParameterList != null)
            foreach (ValueExpressionSyntax parameter in methodInvocation.Parameters.ParameterList.Elements)
                AddArgument(arguments, parameter);

        AddInstruction(instructions, 0, arguments, jump);
    }

    private int GetInvocationType(SyntaxToken identifier)
    {
        if (_subPattern.IsMatch(identifier.Text))
            return GetNumberFromStringEnd(identifier.Text);

        if (_methodNameMapper.MapsMethodName(identifier.Text))
            return _methodNameMapper.GetInstructionType(identifier.Text);

        throw CreateException("Could not determine instruction type.", identifier.Location);
    }

    private void AddInstruction(List<GdsScriptInstruction> instructions, int instructionType, List<GdsScriptArgument> arguments, GdsScriptJump? jump = null)
    {
        instructions.Add(new GdsScriptInstruction
        {
            Type = instructionType,
            Arguments = [.. arguments],
            Jump = jump
        });
    }

    private void AddArgument(List<GdsScriptArgument> arguments, ValueExpressionSyntax parameter)
    {
        switch (parameter.Value)
        {
            case LiteralExpressionSyntax literalExpression:
                AddArgument(arguments, literalExpression);
                break;

            default:
                throw CreateException($"Invalid value expression {parameter.Value.GetType().Name}.", parameter.Location);
        }
    }

    private void AddArgument(List<GdsScriptArgument> arguments, LiteralExpressionSyntax literal)
    {
        GdsScriptArgumentType type;
        object value;

        switch (literal.Literal.RawKind)
        {
            case (int)SyntaxTokenKind.NumericLiteral:
                type = GdsScriptArgumentType.Int;
                value = GetNumericLiteral(literal);
                break;

            case (int)SyntaxTokenKind.FloatingNumericLiteral:
                type = GdsScriptArgumentType.Float;
                value = GetFloatingNumericLiteral(literal);
                break;

            case (int)SyntaxTokenKind.StringLiteral:
                type = GdsScriptArgumentType.String;
                value = GetStringLiteral(literal);
                break;

            case (int)SyntaxTokenKind.HashStringLiteral:
                type = GdsScriptArgumentType.Jump;
                value = GetHashStringLiteral(literal);
                break;

            default:
                throw CreateException($"Invalid literal {(SyntaxTokenKind)literal.Literal.RawKind}.", literal.Location,
                    SyntaxTokenKind.NumericLiteral, SyntaxTokenKind.FloatingNumericLiteral, SyntaxTokenKind.StringLiteral,
                    SyntaxTokenKind.HashStringLiteral);
        }

        AddArgument(arguments, type, value);
    }

    private void AddArgument(List<GdsScriptArgument> arguments, GdsScriptArgumentType type, object value)
    {
        arguments.Add(new GdsScriptArgument
        {
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

    private float GetFloatingNumericLiteral(LiteralExpressionSyntax literal)
    {
        if (literal.Literal.RawKind != (int)SyntaxTokenKind.FloatingNumericLiteral)
            throw CreateException($"Invalid literal {(SyntaxTokenKind)literal.Literal.RawKind}.", literal.Location, SyntaxTokenKind.FloatingNumericLiteral);

        return float.Parse(literal.Literal.Text[..^1], CultureInfo.GetCultureInfo("en-gb"));
    }

    private string GetHashStringLiteral(LiteralExpressionSyntax literal)
    {
        if (literal.Literal.RawKind != (int)SyntaxTokenKind.HashStringLiteral)
            throw CreateException($"Invalid literal {(SyntaxTokenKind)literal.Literal.RawKind}.", literal.Location, SyntaxTokenKind.HashStringLiteral);

        return literal.Literal.Text[1..^2];
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