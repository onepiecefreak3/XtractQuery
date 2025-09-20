using Logic.Business.Level5ScriptManagement.InternalContract;
using Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses;
using Logic.Domain.Level5.Contract.Script.DataClasses;
using System.Globalization;
using System.Text.RegularExpressions;
using Logic.Domain.CodeAnalysis.Contract.DataClasses;

namespace Logic.Business.Level5ScriptManagement;

internal class Level5CodeUnitConverter : ILevel5CodeUnitConverter
{
    private readonly Regex _subPattern = new("^sub[0-9]+$", RegexOptions.Compiled);
    private readonly IMethodNameMapper _methodNameMapper;

    public Level5CodeUnitConverter(IMethodNameMapper methodNameMapper)
    {
        _methodNameMapper = methodNameMapper;
    }

    public ScriptFile CreateScriptFile(CodeUnitSyntax tree)
    {
        var result = new ScriptFile
        {
            Functions = new List<ScriptFunction>(),
            Jumps = new List<ScriptJump>(),
            Instructions = new List<ScriptInstruction>(),
            Arguments = new List<ScriptArgument>()
        };

        AddFunctions(result, tree.MethodDeclarations);

        return result;
    }

    private void AddFunctions(ScriptFile result, IReadOnlyList<MethodDeclarationSyntax> methods)
    {
        foreach (MethodDeclarationSyntax method in methods)
        {
            var instructionStartIndex = (short)result.Instructions.Count;
            var jumpStartIndex = (short)result.Jumps.Count;

            AddExpressions(result, method.Body.Expressions);

            result.Functions.Add(new ScriptFunction
            {
                Name = method.Identifier.Text,

                InstructionIndex = instructionStartIndex,
                InstructionCount = (short)(result.Instructions.Count - instructionStartIndex),
                JumpIndex = jumpStartIndex,
                JumpCount = (short)(result.Jumps.Count - jumpStartIndex),
                ParameterCount = method.Parameters.Parameters?.Elements.Count ?? 0,

                LocalCount = method.MetadataParameters == null ? (short)-1 : (short)GetNumericLiteral(method.MetadataParameters.List.Parameter1),
                ObjectCount = method.MetadataParameters == null ? (short)-1 : (short)GetNumericLiteral(method.MetadataParameters.List.Parameter2)
            });
        }
    }

    private void AddExpressions(ScriptFile result, IReadOnlyList<StatementSyntax> expressions)
    {
        foreach (StatementSyntax statement in expressions)
        {
            switch (statement)
            {
                case GotoLabelStatementSyntax gotoStatement:
                    AddJump(result, gotoStatement);
                    break;

                case YieldStatementSyntax:
                    AddYieldStatement(result);
                    break;

                case ReturnStatementSyntax returnStatement:
                    AddReturnStatement(result, returnStatement);
                    break;

                case ExitStatementSyntax:
                    AddExitStatement(result);
                    break;

                case AssignmentStatementSyntax assignmentStatement:
                    AddAssignmentStatement(result, assignmentStatement);
                    break;

                case IfGotoStatementSyntax ifGotoStatement:
                    AddIfGotoStatement(result, ifGotoStatement);
                    break;

                case GotoStatementSyntax gotoStatement:
                    AddGotoStatement(result, gotoStatement);
                    break;

                case IfNotGotoStatementSyntax ifNotGotoStatement:
                    AddIfNotGotoStatement(result, ifNotGotoStatement);
                    break;

                case PostfixUnaryStatementSyntax postfixUnaryStatement:
                    AddPostfixUnaryStatement(result, postfixUnaryStatement);
                    break;

                default:
                    throw CreateException($"Unknown statement {statement.GetType().Name}.", statement.Location);
            }
        }
    }

    private void AddPostfixUnaryStatement(ScriptFile result, PostfixUnaryStatementSyntax postfixUnaryStatement)
    {
        int instructionType;
        switch (postfixUnaryStatement.Expression.Operation.RawKind)
        {
            case (int)SyntaxTokenKind.Increment:
                instructionType = 240;
                break;

            case (int)SyntaxTokenKind.Decrement:
                instructionType = 241;
                break;

            default:
                throw CreateException($"Invalid operation {(SyntaxTokenKind)postfixUnaryStatement.Expression.Operation.RawKind} in postfix unary expression.", postfixUnaryStatement.Expression.Location);
        }

        if (postfixUnaryStatement.Expression.Value is ValueExpressionSyntax { Value: VariableExpressionSyntax variable })
        {
            int variableSlot = GetVariable(variable);
            AddVariablePostfixUnaryStatement(result, instructionType, variableSlot);
        }
        else if (postfixUnaryStatement.Expression.Value is ArrayIndexExpressionSyntax { Value.Value: VariableExpressionSyntax variable1 } arrayValue)
        {
            int variableSlot = GetVariable(variable1);
            AddArrayPostfixUnaryVariableStatement(result, arrayValue.Indexer, instructionType, variableSlot);
        }
        else
            throw CreateException($"Invalid expression {postfixUnaryStatement.Expression.Value.GetType().Name}", postfixUnaryStatement.Expression.Value.Location);
    }

    private void AddVariablePostfixUnaryStatement(ScriptFile result, int instructionType, int variableSlot)
    {
        AddInstruction(result, result.Arguments.Count, 0, instructionType, variableSlot);
    }

    private void AddArrayPostfixUnaryVariableStatement(ScriptFile result, IReadOnlyList<ArrayIndexerExpressionSyntax> indexer, int instructionType, int variableSlot)
    {
        int argumentIndex = result.Arguments.Count;

        foreach (var index in indexer)
            AddArgument(result, index.Index);

        AddInstruction(result, argumentIndex, indexer.Count, instructionType, variableSlot);
    }

    private void AddIfGotoStatement(ScriptFile result, IfGotoStatementSyntax ifGotoStatement)
    {
        var argumentStartIndex = (short)result.Arguments.Count;

        AddArgument(result, ifGotoStatement.Goto.Target);
        AddArgument(result, ifGotoStatement.Value);
        AddInstruction(result, argumentStartIndex, 2, 30, 1001);
    }

    private void AddGotoStatement(ScriptFile result, GotoStatementSyntax gotoStatement)
    {
        var argumentStartIndex = (short)result.Arguments.Count;

        AddArgument(result, gotoStatement.Target);
        AddInstruction(result, argumentStartIndex, 1, 31, 1001);
    }

    private void AddIfNotGotoStatement(ScriptFile result, IfNotGotoStatementSyntax ifNotGotoStatement)
    {
        var argumentStartIndex = (short)result.Arguments.Count;

        AddArgument(result, ifNotGotoStatement.Goto.Target);
        AddExpression(result, ifNotGotoStatement.Comparison, out _);
        AddInstruction(result, argumentStartIndex, 2, 33, 1002);
    }

    private void AddJump(ScriptFile result, GotoLabelStatementSyntax gotoLabelStatement)
    {
        result.Jumps.Add(new ScriptJump
        {
            Name = GetStringLiteral(gotoLabelStatement.Label),

            InstructionIndex = result.Instructions.Count
        });
    }

    private void AddYieldStatement(ScriptFile result)
    {
        AddInstruction(result, (short)result.Arguments.Count, 0, 10, 1001);
    }

    private void AddReturnStatement(ScriptFile result, ReturnStatementSyntax returnStatement)
    {
        var argumentStartIndex = (short)result.Arguments.Count;

        var argumentCount = 0;
        if (returnStatement.ValueExpression != null)
        {
            AddArgument(result, returnStatement.ValueExpression);
            argumentCount = 1;
        }

        AddInstruction(result, argumentStartIndex, argumentCount, 11, 1000);
    }

    private void AddExitStatement(ScriptFile result)
    {
        AddInstruction(result, (short)result.Arguments.Count, 0, 12, 1001);
    }

    private void AddAssignmentStatement(ScriptFile result, AssignmentStatementSyntax assignmentStatement)
    {
        int argumentStartIndex = result.Arguments.Count;

        ExpressionSyntax rightAssignment = assignmentStatement.Right;

        AddExpression(result, rightAssignment, out int instructionType);
        if (instructionType < 0)
            throw CreateException($"Invalid expression {assignmentStatement.Right.GetType().Name}.", assignmentStatement.Right.Location);

        switch (assignmentStatement.EqualsOperator.RawKind)
        {
            case (int)SyntaxTokenKind.PlusEquals:
                instructionType = 250;
                if (assignmentStatement.Right is not ValueExpressionSyntax)
                    throw CreateException($"Invalid expression {assignmentStatement.Right.GetType().Name}.", assignmentStatement.Right.Location);
                break;

            case (int)SyntaxTokenKind.MinusEquals:
                instructionType = 251;
                if (assignmentStatement.Right is not ValueExpressionSyntax)
                    throw CreateException($"Invalid expression {assignmentStatement.Right.GetType().Name}.", assignmentStatement.Right.Location);
                break;

            case (int)SyntaxTokenKind.MulEquals:
                instructionType = 252;
                if (assignmentStatement.Right is not ValueExpressionSyntax)
                    throw CreateException($"Invalid expression {assignmentStatement.Right.GetType().Name}.", assignmentStatement.Right.Location);
                break;

            case (int)SyntaxTokenKind.DivEquals:
                instructionType = 253;
                if (assignmentStatement.Right is not ValueExpressionSyntax)
                    throw CreateException($"Invalid expression {assignmentStatement.Right.GetType().Name}.", assignmentStatement.Right.Location);
                break;

            case (int)SyntaxTokenKind.ModEquals:
                instructionType = 254;
                if (assignmentStatement.Right is not ValueExpressionSyntax)
                    throw CreateException($"Invalid expression {assignmentStatement.Right.GetType().Name}.", assignmentStatement.Right.Location);
                break;

            case (int)SyntaxTokenKind.AndEquals:
                instructionType = 260;
                if (assignmentStatement.Right is not ValueExpressionSyntax)
                    throw CreateException($"Invalid expression {assignmentStatement.Right.GetType().Name}.", assignmentStatement.Right.Location);
                break;

            case (int)SyntaxTokenKind.OrEquals:
                instructionType = 261;
                if (assignmentStatement.Right is not ValueExpressionSyntax)
                    throw CreateException($"Invalid expression {assignmentStatement.Right.GetType().Name}.", assignmentStatement.Right.Location);
                break;

            case (int)SyntaxTokenKind.XorEquals:
                instructionType = 262;
                if (assignmentStatement.Right is not ValueExpressionSyntax)
                    throw CreateException($"Invalid expression {assignmentStatement.Right.GetType().Name}.", assignmentStatement.Right.Location);
                break;

            case (int)SyntaxTokenKind.LeftShiftEquals:
                instructionType = 270;
                if (assignmentStatement.Right is not ValueExpressionSyntax)
                    throw CreateException($"Invalid expression {assignmentStatement.Right.GetType().Name}.", assignmentStatement.Right.Location);
                break;

            case (int)SyntaxTokenKind.RightShiftEquals:
                instructionType = 271;
                if (assignmentStatement.Right is not ValueExpressionSyntax)
                    throw CreateException($"Invalid expression {assignmentStatement.Right.GetType().Name}.", assignmentStatement.Right.Location);
                break;
        }

        int returnParameter;
        switch (assignmentStatement.Left)
        {
            case ValueExpressionSyntax leftValue:
                switch (leftValue.Value)
                {
                    case VariableExpressionSyntax leftVariable:
                        returnParameter = GetVariable(leftVariable);
                        break;

                    default:
                        throw CreateException($"Invalid value expression {leftValue.Value.GetType().Name}.", leftValue.Value.Location);
                }
                break;

            case ArrayIndexExpressionSyntax leftArrayIndex:
                switch (leftArrayIndex.Value.Value)
                {
                    case VariableExpressionSyntax leftVariable:
                        returnParameter = GetVariable(leftVariable);
                        break;

                    default:
                        throw CreateException($"Invalid value expression {leftArrayIndex.Value.Value.GetType().Name}.", leftArrayIndex.Value.Value.Location);
                }
                foreach (var index in leftArrayIndex.Indexer)
                    AddArgument(result, index.Index);
                break;

            default:
                throw CreateException($"Invalid expression {assignmentStatement.Left.GetType().Name}.", assignmentStatement.Left.Location);
        }

        int argumentCount = result.Arguments.Count - argumentStartIndex;

        AddInstruction(result, argumentStartIndex, argumentCount, instructionType, returnParameter);
    }

    private void AddExpression(ScriptFile result, ExpressionSyntax expression, out int instructionType)
    {
        instructionType = -1;

        switch (expression)
        {
            case ValueExpressionSyntax value:
                instructionType = 100;

                AddArgument(result, value);
                break;

            case TypeCastValueExpressionSyntax typeCastValue:
                switch (typeCastValue.TypeCast.TypeKeyword.RawKind)
                {
                    case (int)SyntaxTokenKind.IntKeyword:
                        instructionType = 511;
                        AddExpression(result, typeCastValue.Value, out _);
                        break;

                    case (int)SyntaxTokenKind.BoolKeyword:
                        instructionType = 512;
                        AddExpression(result, typeCastValue.Value, out _);
                        break;

                    case (int)SyntaxTokenKind.FloatKeyword:
                        instructionType = 513;
                        AddExpression(result, typeCastValue.Value, out _);
                        break;

                    default:
                        throw CreateException($"Invalid type cast expression {(SyntaxTokenKind)typeCastValue.TypeCast.TypeKeyword.RawKind}.",
                            typeCastValue.TypeCast.Location, SyntaxTokenKind.IntKeyword,
                            SyntaxTokenKind.BoolKeyword, SyntaxTokenKind.FloatKeyword);
                }
                break;

            case UnaryExpressionSyntax unaryExpression:
                switch (unaryExpression.Operation.RawKind)
                {
                    case (int)SyntaxTokenKind.Complement:
                        instructionType = 110;
                        break;

                    case (int)SyntaxTokenKind.Minus:
                        instructionType = 112;
                        break;

                    case (int)SyntaxTokenKind.NotKeyword:
                        instructionType = 120;
                        break;

                    default:
                        throw CreateException($"Invalid unary expression operation {(SyntaxTokenKind)unaryExpression.Operation.RawKind}.", expression.Location);
                }

                AddArgument(result, unaryExpression.Value);
                break;

            case LogicalExpressionSyntax logicalExpression:
                switch (logicalExpression.Operation.RawKind)
                {
                    case (int)SyntaxTokenKind.AndKeyword:
                        instructionType = 121;
                        break;

                    case (int)SyntaxTokenKind.OrKeyword:
                        instructionType = 122;
                        break;

                    default:
                        throw CreateException($"Invalid logical expression operation {(SyntaxTokenKind)logicalExpression.Operation.RawKind}.", expression.Location);
                }

                AddExpression(result, logicalExpression.Left, out _);
                AddExpression(result, logicalExpression.Right, out _);
                break;

            case BinaryExpressionSyntax binaryExpression:
                switch (binaryExpression.Operation.RawKind)
                {
                    case (int)SyntaxTokenKind.Equals:
                        instructionType = 130;
                        break;

                    case (int)SyntaxTokenKind.NotEquals:
                        instructionType = 131;
                        break;

                    case (int)SyntaxTokenKind.GreaterEquals:
                        instructionType = 132;
                        break;

                    case (int)SyntaxTokenKind.SmallerEquals:
                        instructionType = 133;
                        break;

                    case (int)SyntaxTokenKind.Greater:
                        instructionType = 134;
                        break;

                    case (int)SyntaxTokenKind.Smaller:
                        instructionType = 135;
                        break;

                    case (int)SyntaxTokenKind.Plus:
                        instructionType = 150;
                        break;

                    case (int)SyntaxTokenKind.Minus:
                        instructionType = 151;
                        break;

                    case (int)SyntaxTokenKind.Mul:
                        instructionType = 152;
                        break;

                    case (int)SyntaxTokenKind.Div:
                        instructionType = 153;
                        break;

                    case (int)SyntaxTokenKind.Mod:
                        instructionType = 154;
                        break;

                    case (int)SyntaxTokenKind.And:
                        instructionType = 160;
                        break;

                    case (int)SyntaxTokenKind.Or:
                        instructionType = 161;
                        break;

                    case (int)SyntaxTokenKind.Xor:
                        instructionType = 162;
                        break;

                    case (int)SyntaxTokenKind.LeftShift:
                        instructionType = 170;
                        break;

                    case (int)SyntaxTokenKind.RightShift:
                        instructionType = 171;
                        break;

                    default:
                        throw CreateException($"Invalid binary expression operation {(SyntaxTokenKind)binaryExpression.Operation.RawKind}.", expression.Location);
                }

                if (binaryExpression is
                    {
                        Left: ValueExpressionSyntax { Value: VariableExpressionSyntax },
                        Right: ValueExpressionSyntax
                        {
                            Value: LiteralExpressionSyntax
                            {
                                Literal: { RawKind: (int)SyntaxTokenKind.NumericLiteral, Text: "1" }
                            }
                        }
                    })
                {
                    switch (binaryExpression.Operation.RawKind)
                    {
                        case (int)SyntaxTokenKind.Plus:
                            instructionType = 140;
                            break;

                        case (int)SyntaxTokenKind.Minus:
                            instructionType = 141;
                            break;
                    }
                }

                AddExpression(result, binaryExpression.Left, out _);
                if (instructionType is not 140 and not 141)
                    AddExpression(result, binaryExpression.Right, out _);
                break;

            case SwitchExpressionSyntax switchExpression:
                instructionType = 523;

                var defaultCases = switchExpression.CaseBlock.Cases.OfType<DefaultSwitchCaseExpressionSyntax>().ToArray();
                if (!defaultCases.Any())
                    throw CreateException("Missing mandatory default case for switch expression.", expression.Location);

                AddExpression(result, switchExpression.Value, out _);
                AddArgument(result, defaultCases[0].Value);

                foreach (var @case in switchExpression.CaseBlock.Cases.OfType<LiteralSwitchCaseExpressionSyntax>())
                {
                    AddArgument(result, @case.CaseValue);
                    AddArgument(result, @case.Value);
                }

                break;

            case ArrayInstantiationExpressionSyntax arrayInstantiation:
                instructionType = 530;

                foreach (var index in arrayInstantiation.Indexer)
                    AddArgument(result, index.Index);
                break;

            case ArrayIndexExpressionSyntax arrayIndex:
                instructionType = 531;

                AddArgument(result, arrayIndex.Value);

                foreach (var index in arrayIndex.Indexer)
                    AddArgument(result, index.Index);
                break;

            case MethodInvocationExpressionSyntax methodInvocation:
                instructionType = GetInstructionType(methodInvocation.Identifier, out bool isMethodTransfer);

                if (isMethodTransfer)
                {
                    int rawArgumentType = -1;
                    var argumentType = ScriptArgumentType.StringHash;
                    if (methodInvocation.Metadata != null)
                    {
                        rawArgumentType = GetNumericLiteral(methodInvocation.Metadata.Parameter);
                        argumentType = ScriptArgumentType.String;
                    }

                    AddArgument(result, argumentType, methodInvocation.Identifier.Text, rawArgumentType);
                }

                if (methodInvocation.Parameters.ParameterList != null)
                    foreach (var parameter in methodInvocation.Parameters.ParameterList.Elements)
                        AddArgument(result, parameter);
                break;

            default:
                throw CreateException($"Invalid expression {expression.GetType().Name}.", expression.Location);
        }
    }

    private int GetInstructionType(SyntaxToken identifier, out bool isMethodTransfer)
    {
        isMethodTransfer = false;

        if (_subPattern.IsMatch(identifier.Text))
            return GetNumberFromStringEnd(identifier.Text);

        if (_methodNameMapper.MapsMethodName(identifier.Text))
            return _methodNameMapper.GetInstructionType(identifier.Text);

        isMethodTransfer = true;
        return 20;
    }

    private void AddInstruction(ScriptFile result, int argumentIndex, int argumentCount, int instructionType, int returnParameter)
    {
        result.Instructions.Add(new ScriptInstruction
        {
            ArgumentIndex = (short)argumentIndex,
            ArgumentCount = (short)argumentCount,

            Type = (short)instructionType,

            ReturnParameter = (short)returnParameter
        });
    }

    private void AddArgument(ScriptFile result, ValueExpressionSyntax parameter)
    {
        switch (parameter.Value)
        {
            case VariableExpressionSyntax variableExpression:
                AddArgument(result, variableExpression);
                break;

            case LiteralExpressionSyntax literalExpression:
                AddArgument(result, literalExpression, parameter.MetadataParameters);
                break;

            default:
                throw CreateException($"Invalid value expression {parameter.Value.GetType().Name}.", parameter.Location);
        }
    }

    private void AddArgument(ScriptFile result, VariableExpressionSyntax variable)
    {
        var type = ScriptArgumentType.Variable;
        int value = GetVariable(variable);

        AddArgument(result, type, value);
    }

    private void AddArgument(ScriptFile result, LiteralExpressionSyntax literal, ValueMetadataParametersSyntax? metadata)
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

    private void AddArgument(ScriptFile result, ScriptArgumentType type, object value, int rawArgumentType = -1)
    {
        result.Arguments.Add(new ScriptArgument
        {
            RawArgumentType = rawArgumentType,
            Type = type,
            Value = value
        });
    }

    private int GetVariable(VariableExpressionSyntax variable)
    {
        // 5000+ ?
        // Values 4000+ are script global values
        // Values 3000+ are input parameters to the function
        // Values 2000+ are dynamically typed local values
        // Values 1000+ are primitively types local values
        // 0000+ ?

        int slot = GetVariableSlot(variable.Variable.Text, out int slotIndex);
        string varName = variable.Variable.Text[1..slotIndex];
        switch (varName)
        {
            case "unk":
                return slot;

            case "local":
                return slot + 1000;

            case "object":
                return slot + 2000;

            case "param":
                return slot + 3000;

            case "global":
                return slot + 4000;

            default:
                throw CreateException($"Invalid variable type \"{varName}\".", variable.Location);
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

    private int GetVariableSlot(string text, out int slotStartIndex)
    {
        slotStartIndex = 0;
        while (slotStartIndex < text.Length && (text[slotStartIndex] < '0' || text[slotStartIndex] > '9'))
            slotStartIndex++;

        int endIndex = slotStartIndex;
        while (endIndex < text.Length && text[endIndex] >= '0' && text[endIndex] <= '9')
            endIndex++;

        return int.Parse(text[slotStartIndex..endIndex]);
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