using Logic.Business.Level5ScriptManagement.InternalContract.Conversion.HighLevelSyntax;
using Logic.Domain.CodeAnalysis.Contract.DataClasses;
using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;
using Logic.Domain.CodeAnalysis.Contract.Level5;

namespace Logic.Business.Level5ScriptManagement.Conversion.HighLevelSyntax;

internal class LowLevelCodeUnitConverter(ILevel5SyntaxFactory syntaxFactory) : ILowLevelCodeUnitConverter
{
    public CodeUnitSyntax Convert(CodeUnitSyntax tree)
    {
        var methods = new List<MethodDeclarationSyntax>();
        foreach (MethodDeclarationSyntax method in tree.MethodDeclarations)
            methods.Add(ConvertMethod(method));

        return new CodeUnitSyntax(methods);
    }

    private MethodDeclarationSyntax ConvertMethod(MethodDeclarationSyntax method)
    {
        var usedTemps = new HashSet<int>();
        CollectUsedTempSlots(method.Body.Expressions, usedTemps);

        var usedLabels = new HashSet<string>(StringComparer.Ordinal);
        CollectUsedLabels(method.Body.Expressions, usedLabels);
        int nextLabel = 0;

        var statements = new List<StatementSyntax>();
        foreach (StatementSyntax statement in method.Body.Expressions)
            FlattenStatement(statement, statements, usedTemps, usedLabels, ref nextLabel);

        var body = new MethodDeclarationBodySyntax(method.Body.CurlyOpen, statements, method.Body.CurlyClose);
        return new MethodDeclarationSyntax(method.Identifier, method.MetadataParameters, method.Parameters, body);
    }

    private void FlattenStatement(
        StatementSyntax statement,
        List<StatementSyntax> output,
        HashSet<int> usedTemps,
        HashSet<string> usedLabels,
        ref int nextLabel)
    {
        switch (statement)
        {
            case MethodInvocationStatementSyntax invocation:
                ValueExpressionSyntax temp = AllocateTemp(usedTemps);
                MethodInvocationExpressionSyntax call = FlattenInvocationExpression(invocation, output, usedTemps);
                output.Add(new AssignmentStatementSyntax(
                    temp,
                    syntaxFactory.Token(SyntaxTokenKind.EqualsSign),
                    call,
                    invocation.Semicolon));
                break;

            case AssignmentStatementSyntax assignment:
                ExpressionSyntax left = FlattenExpression(assignment.Left, output, usedTemps, forceValue: false);
                ExpressionSyntax right = FlattenExpression(assignment.Right, output, usedTemps, forceValue: false);
                output.Add(new AssignmentStatementSyntax(
                    left,
                    assignment.EqualsOperator,
                    right,
                    assignment.Semicolon));
                break;

            case IfGotoStatementSyntax ifGoto:
                ValueExpressionSyntax ifValue = EnsureValueExpression(
                    FlattenExpression(ifGoto.Value, output, usedTemps, forceValue: true), output, usedTemps);
                output.Add(new IfGotoStatementSyntax(ifGoto.If, ifValue, ifGoto.Goto, ifGoto.Semicolon));
                break;

            case IfNotGotoStatementSyntax ifNotGoto:
                UnaryExpressionSyntax comparison = FlattenUnary(ifNotGoto.Comparison, output, usedTemps);
                output.Add(new IfNotGotoStatementSyntax(ifNotGoto.If, comparison, ifNotGoto.Goto, ifNotGoto.Semicolon));
                break;

            case IfStatementSyntax ifStatement:
                LowerIfStatement(ifStatement, output, usedTemps, usedLabels, ref nextLabel);
                break;

            case BlockSyntax block:
                foreach (StatementSyntax nested in block.Statements)
                    FlattenStatement(nested, output, usedTemps, usedLabels, ref nextLabel);
                break;

            case ReturnStatementSyntax returnStatement:
                ValueExpressionSyntax? returnValue = null;
                if (returnStatement.ValueExpression != null)
                {
                    returnValue = EnsureValueExpression(
                        FlattenExpression(returnStatement.ValueExpression, output, usedTemps, forceValue: true),
                        output,
                        usedTemps);
                }

                output.Add(new ReturnStatementSyntax(returnStatement.Return, returnValue, returnStatement.Semicolon));
                break;

            case PostfixUnaryStatementSyntax postfix:
                PostfixUnaryExpressionSyntax postfixExpr = FlattenPostfix(postfix.Expression, output, usedTemps);
                output.Add(new PostfixUnaryStatementSyntax(postfixExpr, postfix.Semicolon));
                break;

            default:
                output.Add(statement);
                break;
        }
    }

    private void LowerIfStatement(
        IfStatementSyntax ifStatement,
        List<StatementSyntax> output,
        HashSet<int> usedTemps,
        HashSet<string> usedLabels,
        ref int nextLabel)
    {
        if (ifStatement.Else is null)
        {
            string endLabel = AllocateLabel(usedLabels, ref nextLabel, "end");
            EmitIfNotGoto(ifStatement.Condition, endLabel, output, usedTemps);
            foreach (StatementSyntax nested in ifStatement.Body.Statements)
                FlattenStatement(nested, output, usedTemps, usedLabels, ref nextLabel);
            output.Add(CreateLabel(endLabel));
            return;
        }

        string elseLabel = AllocateLabel(usedLabels, ref nextLabel, "else");
        string joinLabel = AllocateLabel(usedLabels, ref nextLabel, "join");

        EmitIfNotGoto(ifStatement.Condition, elseLabel, output, usedTemps);
        foreach (StatementSyntax nested in ifStatement.Body.Statements)
            FlattenStatement(nested, output, usedTemps, usedLabels, ref nextLabel);
        output.Add(CreateGoto(joinLabel));
        output.Add(CreateLabel(elseLabel));

        if (ifStatement.Else.Statement is IfStatementSyntax elseIf)
            LowerIfStatement(elseIf, output, usedTemps, usedLabels, ref nextLabel);
        else if (ifStatement.Else.Statement is BlockSyntax elseBlock)
        {
            foreach (StatementSyntax nested in elseBlock.Statements)
                FlattenStatement(nested, output, usedTemps, usedLabels, ref nextLabel);
        }
        else
            FlattenStatement(ifStatement.Else.Statement, output, usedTemps, usedLabels, ref nextLabel);

        output.Add(CreateLabel(joinLabel));
    }

    private void EmitIfNotGoto(
        ExpressionSyntax condition,
        string targetLabel,
        List<StatementSyntax> output,
        HashSet<int> usedTemps)
    {
        SyntaxToken ifToken = syntaxFactory.Token(SyntaxTokenKind.IfKeyword);
        SyntaxToken semicolon = syntaxFactory.Token(SyntaxTokenKind.Semicolon);
        GotoExpressionSyntax gotoExpr = CreateGotoExpression(targetLabel);

        ExpressionSyntax flatCondition = ExpressionParenthesizer.UnwrapParentheses(
            FlattenExpression(condition, output, usedTemps, forceValue: false));

        if (flatCondition is UnaryExpressionSyntax unary &&
            unary.Operation.RawKind is (int)SyntaxTokenKind.NotKeyword or (int)SyntaxTokenKind.Not)
        {
            ValueExpressionSyntax value = EnsureValueExpression(unary.Value, output, usedTemps);
            output.Add(new IfGotoStatementSyntax(ifToken, value, gotoExpr, semicolon));
            return;
        }

        ValueExpressionSyntax condValue = EnsureValueExpression(flatCondition, output, usedTemps);
        var notComparison = new UnaryExpressionSyntax(syntaxFactory.Token(SyntaxTokenKind.NotKeyword), condValue);
        output.Add(new IfNotGotoStatementSyntax(ifToken, notComparison, gotoExpr, semicolon));
    }

    private GotoExpressionSyntax CreateGotoExpression(string labelName)
    {
        return new GotoExpressionSyntax(
            syntaxFactory.Token(SyntaxTokenKind.GotoKeyword),
            CreateLabelValue(labelName));
    }

    private GotoStatementSyntax CreateGoto(string labelName)
    {
        return new GotoStatementSyntax(
            syntaxFactory.Token(SyntaxTokenKind.GotoKeyword),
            new CommaSeparatedSyntaxList<ValueExpressionSyntax>([CreateLabelValue(labelName)]),
            syntaxFactory.Token(SyntaxTokenKind.Semicolon));
    }

    private GotoLabelStatementSyntax CreateLabel(string labelName)
    {
        return new GotoLabelStatementSyntax(
            new LiteralExpressionSyntax(syntaxFactory.StringLiteral(labelName)),
            syntaxFactory.Token(SyntaxTokenKind.Colon));
    }

    private ValueExpressionSyntax CreateLabelValue(string labelName)
    {
        return new ValueExpressionSyntax(new LiteralExpressionSyntax(syntaxFactory.StringLiteral(labelName)));
    }

    private static string AllocateLabel(HashSet<string> usedLabels, ref int nextLabel, string prefix)
    {
        while (true)
        {
            string name = $"@__{prefix}_{nextLabel++}@";
            if (usedLabels.Add(name))
                return name;
        }
    }

    private static void CollectUsedLabels(IReadOnlyList<StatementSyntax> statements, HashSet<string> usedLabels)
    {
        foreach (StatementSyntax statement in statements)
            CollectUsedLabels(statement, usedLabels);
    }

    private static void CollectUsedLabels(StatementSyntax statement, HashSet<string> usedLabels)
    {
        switch (statement)
        {
            case GotoLabelStatementSyntax label:
                if (TryGetLabelName(label.Label, out string? name) && name is not null)
                    usedLabels.Add(name);
                break;

            case IfStatementSyntax ifStatement:
                CollectUsedLabels(ifStatement.Body.Statements, usedLabels);
                if (ifStatement.Else != null)
                    CollectUsedLabels(ifStatement.Else.Statement, usedLabels);
                break;

            case BlockSyntax block:
                CollectUsedLabels(block.Statements, usedLabels);
                break;
        }
    }

    private static bool TryGetLabelName(LiteralExpressionSyntax literal, out string? label)
    {
        label = null;
        if (literal.Literal.RawKind != (int)SyntaxTokenKind.StringLiteral)
            return false;

        label = literal.Literal.Text[1..^1].Replace("\\\"", "\"");
        return true;
    }

    private MethodInvocationExpressionSyntax FlattenInvocationExpression(
        MethodInvocationStatementSyntax invocation,
        List<StatementSyntax> output,
        HashSet<int> usedTemps)
    {
        MethodInvocationParametersSyntax parameters = FlattenParameters(invocation.Parameters, output, usedTemps);
        return new MethodInvocationExpressionSyntax(invocation.Name, invocation.Metadata, parameters);
    }

    private ExpressionSyntax FlattenExpression(
        ExpressionSyntax expression,
        List<StatementSyntax> output,
        HashSet<int> usedTemps,
        bool forceValue)
    {
        expression = ExpressionParenthesizer.UnwrapParentheses(expression);

        switch (expression)
        {
            case ValueExpressionSyntax value:
                ExpressionSyntax flattenedInner = FlattenExpression(value.Value, output, usedTemps, forceValue: false);
                if (flattenedInner is VariableExpressionSyntax or LiteralExpressionSyntax or UnaryExpressionSyntax)
                    return new ValueExpressionSyntax(flattenedInner, value.MetadataParameters);

                ValueExpressionSyntax spilled = Spill(flattenedInner, output, usedTemps);
                return value.MetadataParameters is null
                    ? spilled
                    : new ValueExpressionSyntax(spilled.Value, value.MetadataParameters);

            case VariableExpressionSyntax:
            case LiteralExpressionSyntax:
                return forceValue ? new ValueExpressionSyntax(expression) : expression;

            case UnaryExpressionSyntax unary:
                return FlattenUnary(unary, output, usedTemps);

            case BinaryExpressionSyntax binary:
                ExpressionSyntax left = EnsureArgument(FlattenExpression(binary.Left, output, usedTemps, false), output, usedTemps);
                ExpressionSyntax right = EnsureArgument(FlattenExpression(binary.Right, output, usedTemps, false), output, usedTemps);
                var flatBinary = new BinaryExpressionSyntax(left, binary.Operation, right);
                return forceValue ? Spill(flatBinary, output, usedTemps) : flatBinary;

            case LogicalExpressionSyntax logical:
                ExpressionSyntax logicalLeft = EnsureArgument(FlattenExpression(logical.Left, output, usedTemps, false), output, usedTemps);
                ExpressionSyntax logicalRight = EnsureArgument(FlattenExpression(logical.Right, output, usedTemps, false), output, usedTemps);
                var flatLogical = new LogicalExpressionSyntax(logicalLeft, logical.Operation, logicalRight);
                return forceValue ? Spill(flatLogical, output, usedTemps) : flatLogical;

            case MethodInvocationExpressionSyntax invocation:
                MethodInvocationParametersSyntax parameters = FlattenParameters(invocation.Parameters, output, usedTemps);
                var flatInvocation = new MethodInvocationExpressionSyntax(invocation.Name, invocation.Metadata, parameters);
                return forceValue ? Spill(flatInvocation, output, usedTemps) : flatInvocation;

            case PostfixUnaryExpressionSyntax postfix:
                return FlattenPostfix(postfix, output, usedTemps);

            case ArrayIndexExpressionSyntax arrayIndex:
                ValueExpressionSyntax arrayValue = EnsureValueExpression(
                    FlattenExpression(arrayIndex.Value, output, usedTemps, true), output, usedTemps);
                return new ArrayIndexExpressionSyntax(arrayValue, arrayIndex.Indexer);

            case TypeCastValueExpressionSyntax typeCast:
                ValueExpressionSyntax castValue = EnsureValueExpression(
                    FlattenExpression(typeCast.Value, output, usedTemps, true), output, usedTemps);
                var flatCast = new TypeCastValueExpressionSyntax(typeCast.TypeCast, castValue);
                return forceValue ? Spill(flatCast, output, usedTemps) : flatCast;

            case SwitchExpressionSyntax switchExpression:
                ExpressionSyntax switchValue = EnsureArgument(
                    FlattenExpression(switchExpression.Value, output, usedTemps, false), output, usedTemps);
                var flatSwitch = new SwitchExpressionSyntax(switchValue, switchExpression.Switch, switchExpression.CaseBlock);
                return forceValue ? Spill(flatSwitch, output, usedTemps) : flatSwitch;

            case ParenthesizedExpressionSyntax parenthesized:
                return FlattenExpression(parenthesized.Expression, output, usedTemps, forceValue);

            default:
                return forceValue ? Spill(expression, output, usedTemps) : expression;
        }
    }

    private UnaryExpressionSyntax FlattenUnary(
        UnaryExpressionSyntax unary,
        List<StatementSyntax> output,
        HashSet<int> usedTemps)
    {
        ValueExpressionSyntax value = EnsureValueExpression(
            FlattenExpression(unary.Value, output, usedTemps, true), output, usedTemps);
        return new UnaryExpressionSyntax(unary.Operation, value);
    }

    private PostfixUnaryExpressionSyntax FlattenPostfix(
        PostfixUnaryExpressionSyntax postfix,
        List<StatementSyntax> output,
        HashSet<int> usedTemps)
    {
        ExpressionSyntax value = FlattenExpression(postfix.Value, output, usedTemps, false);
        return new PostfixUnaryExpressionSyntax(value, postfix.Operation);
    }

    private MethodInvocationParametersSyntax FlattenParameters(
        MethodInvocationParametersSyntax parameters,
        List<StatementSyntax> output,
        HashSet<int> usedTemps)
    {
        if (parameters.ParameterList?.Elements is null)
            return parameters;

        var elements = new List<ExpressionSyntax>();
        foreach (ExpressionSyntax parameter in parameters.ParameterList.Elements)
        {
            ExpressionSyntax flattened = FlattenExpression(parameter, output, usedTemps, forceValue: true);
            elements.Add(EnsureValueExpression(flattened, output, usedTemps));
        }

        return new MethodInvocationParametersSyntax(
            parameters.ParenOpen,
            new CommaSeparatedSyntaxList<ExpressionSyntax>(elements),
            parameters.ParenClose);
    }

    private ExpressionSyntax EnsureArgument(ExpressionSyntax expression, List<StatementSyntax> output, HashSet<int> usedTemps)
    {
        expression = ExpressionParenthesizer.UnwrapParentheses(expression);

        if (expression is ValueExpressionSyntax or VariableExpressionSyntax or LiteralExpressionSyntax)
            return expression is ValueExpressionSyntax ? expression : new ValueExpressionSyntax(expression);

        if (expression is UnaryExpressionSyntax { Operation.RawKind: (int)SyntaxTokenKind.Minus, Value: ValueExpressionSyntax })
            return new ValueExpressionSyntax(expression);

        return Spill(expression, output, usedTemps);
    }

    private ValueExpressionSyntax EnsureValueExpression(
        ExpressionSyntax expression,
        List<StatementSyntax> output,
        HashSet<int> usedTemps)
    {
        expression = ExpressionParenthesizer.UnwrapParentheses(expression);

        if (expression is ValueExpressionSyntax value)
        {
            if (value.Value is VariableExpressionSyntax or LiteralExpressionSyntax)
                return value;

            if (value.Value is UnaryExpressionSyntax { Operation.RawKind: (int)SyntaxTokenKind.Minus })
                return value;

            return Spill(value.Value, output, usedTemps);
        }

        if (expression is VariableExpressionSyntax or LiteralExpressionSyntax)
            return new ValueExpressionSyntax(expression);

        if (expression is UnaryExpressionSyntax { Operation.RawKind: (int)SyntaxTokenKind.Minus, Value: ValueExpressionSyntax })
            return new ValueExpressionSyntax(expression);

        return Spill(expression, output, usedTemps);
    }

    private ValueExpressionSyntax Spill(ExpressionSyntax expression, List<StatementSyntax> output, HashSet<int> usedTemps)
    {
        ValueExpressionSyntax temp = AllocateTemp(usedTemps);
        output.Add(new AssignmentStatementSyntax(
            temp,
            syntaxFactory.Token(SyntaxTokenKind.EqualsSign),
            expression,
            syntaxFactory.Token(SyntaxTokenKind.Semicolon)));
        return temp;
    }

    private ValueExpressionSyntax AllocateTemp(HashSet<int> usedTemps)
    {
        int slot = 1;
        while (usedTemps.Contains(slot))
            slot++;

        usedTemps.Add(slot);
        return new ValueExpressionSyntax(new VariableExpressionSyntax(syntaxFactory.Variable("temp", (uint)slot)));
    }

    private static void CollectUsedTempSlots(IReadOnlyList<StatementSyntax> statements, HashSet<int> usedTemps)
    {
        foreach (StatementSyntax statement in statements)
            CollectUsedTempSlots(statement, usedTemps);
    }

    private static void CollectUsedTempSlots(StatementSyntax statement, HashSet<int> usedTemps)
    {
        switch (statement)
        {
            case AssignmentStatementSyntax assignment:
                CollectUsedTempSlots(assignment.Left, usedTemps);
                CollectUsedTempSlots(assignment.Right, usedTemps);
                break;

            case IfGotoStatementSyntax ifGoto:
                CollectUsedTempSlots(ifGoto.Value, usedTemps);
                break;

            case IfNotGotoStatementSyntax ifNotGoto:
                CollectUsedTempSlots(ifNotGoto.Comparison, usedTemps);
                break;

            case ReturnStatementSyntax { ValueExpression: not null } returnStatement:
                CollectUsedTempSlots(returnStatement.ValueExpression, usedTemps);
                break;

            case MethodInvocationStatementSyntax invocation:
                CollectUsedTempSlots(invocation.Parameters, usedTemps);
                break;

            case PostfixUnaryStatementSyntax postfix:
                CollectUsedTempSlots(postfix.Expression, usedTemps);
                break;

            case IfStatementSyntax ifStatement:
                CollectUsedTempSlots(ifStatement.Condition, usedTemps);
                CollectUsedTempSlots(ifStatement.Body.Statements, usedTemps);
                if (ifStatement.Else != null)
                    CollectUsedTempSlots(ifStatement.Else.Statement, usedTemps);
                break;

            case BlockSyntax block:
                CollectUsedTempSlots(block.Statements, usedTemps);
                break;
        }
    }

    private static void CollectUsedTempSlots(MethodInvocationParametersSyntax parameters, HashSet<int> usedTemps)
    {
        if (parameters.ParameterList?.Elements is null)
            return;

        foreach (ExpressionSyntax parameter in parameters.ParameterList.Elements)
            CollectUsedTempSlots(parameter, usedTemps);
    }

    private static void CollectUsedTempSlots(ExpressionSyntax expression, HashSet<int> usedTemps)
    {
        switch (expression)
        {
            case VariableExpressionSyntax variable:
                if (TryGetTempSlot(variable, out int slot))
                    usedTemps.Add(slot);
                break;

            case ValueExpressionSyntax value:
                CollectUsedTempSlots(value.Value, usedTemps);
                break;

            case ParenthesizedExpressionSyntax parenthesized:
                CollectUsedTempSlots(parenthesized.Expression, usedTemps);
                break;

            case UnaryExpressionSyntax unary:
                CollectUsedTempSlots(unary.Value, usedTemps);
                break;

            case BinaryExpressionSyntax binary:
                CollectUsedTempSlots(binary.Left, usedTemps);
                CollectUsedTempSlots(binary.Right, usedTemps);
                break;

            case LogicalExpressionSyntax logical:
                CollectUsedTempSlots(logical.Left, usedTemps);
                CollectUsedTempSlots(logical.Right, usedTemps);
                break;

            case MethodInvocationExpressionSyntax invocation:
                CollectUsedTempSlots(invocation.Parameters, usedTemps);
                break;

            case PostfixUnaryExpressionSyntax postfix:
                CollectUsedTempSlots(postfix.Value, usedTemps);
                break;

            case ArrayIndexExpressionSyntax arrayIndex:
                CollectUsedTempSlots(arrayIndex.Value, usedTemps);
                break;

            case TypeCastValueExpressionSyntax typeCast:
                CollectUsedTempSlots(typeCast.Value, usedTemps);
                break;

            case SwitchExpressionSyntax switchExpression:
                CollectUsedTempSlots(switchExpression.Value, usedTemps);
                break;
        }
    }

    private static bool TryGetTempSlot(VariableExpressionSyntax variable, out int slot)
    {
        slot = 0;
        string text = variable.Variable.Text;
        if (!text.StartsWith("$temp", StringComparison.Ordinal))
            return false;

        return int.TryParse(text["$temp".Length..], out slot);
    }
}
