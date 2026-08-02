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
        var reservedTemps = new HashSet<int>();
        CollectUsedTempSlots(method.Body.Expressions, reservedTemps);
        var temps = new TempAllocator(reservedTemps);

        var usedLabels = new HashSet<string>(StringComparer.Ordinal);
        CollectUsedLabels(method.Body.Expressions, usedLabels);
        int nextLabel = 0;

        var loopStack = new Stack<LoopContext>();
        var statements = new List<StatementSyntax>();
        foreach (StatementSyntax statement in method.Body.Expressions)
            FlattenStatement(statement, statements, temps, usedLabels, ref nextLabel, loopStack);

        var body = new MethodDeclarationBodySyntax(method.Body.CurlyOpen, statements, method.Body.CurlyClose);
        return new MethodDeclarationSyntax(method.Identifier, method.MetadataParameters, method.Parameters, body);
    }

    private void FlattenStatement(
        StatementSyntax statement,
        List<StatementSyntax> output,
        TempAllocator temps,
        HashSet<string> usedLabels,
        ref int nextLabel,
        Stack<LoopContext> loopStack)
    {
        switch (statement)
        {
            case MethodInvocationStatementSyntax invocation:
            {
                // Discarded call result: spill then free immediately so the slot can be reused.
                MethodInvocationExpressionSyntax call = FlattenInvocationExpression(invocation, output, temps);
                ValueExpressionSyntax temp = Spill(call, output, temps);
                temps.ReleaseExpression(temp);
                break;
            }

            case AssignmentStatementSyntax assignment:
            {
                ExpressionSyntax left = FlattenExpression(assignment.Left, output, temps, forceValue: false);
                ExpressionSyntax right = FlattenExpression(assignment.Right, output, temps, forceValue: false);
                output.Add(new AssignmentStatementSyntax(
                    left,
                    assignment.EqualsOperator,
                    right,
                    assignment.Semicolon));
                temps.ReleaseExpression(right);
                temps.ReleaseExpression(left);
                break;
            }

            case IfGotoStatementSyntax ifGoto:
            {
                ValueExpressionSyntax ifValue = EnsureValueExpression(
                    FlattenExpression(ifGoto.Value, output, temps, forceValue: true), output, temps);
                output.Add(new IfGotoStatementSyntax(ifGoto.If, ifValue, ifGoto.Goto, ifGoto.Semicolon));
                temps.ReleaseExpression(ifValue);
                break;
            }

            case IfNotGotoStatementSyntax ifNotGoto:
            {
                UnaryExpressionSyntax comparison = FlattenUnary(ifNotGoto.Comparison, output, temps);
                output.Add(new IfNotGotoStatementSyntax(ifNotGoto.If, comparison, ifNotGoto.Goto, ifNotGoto.Semicolon));
                temps.ReleaseExpression(comparison);
                break;
            }

            case IfStatementSyntax ifStatement:
                LowerIfStatement(ifStatement, output, temps, usedLabels, ref nextLabel, loopStack);
                break;

            case WhileStatementSyntax whileStatement:
                LowerWhileStatement(whileStatement, output, temps, usedLabels, ref nextLabel, loopStack);
                break;

            case DoWhileStatementSyntax doWhileStatement:
                LowerDoWhileStatement(doWhileStatement, output, temps, usedLabels, ref nextLabel, loopStack);
                break;

            case BreakStatementSyntax breakStatement:
                LowerBreakStatement(breakStatement, output, loopStack);
                break;

            case ContinueStatementSyntax continueStatement:
                LowerContinueStatement(continueStatement, output, loopStack);
                break;

            case BlockSyntax block:
                foreach (StatementSyntax nested in block.Statements)
                    FlattenStatement(nested, output, temps, usedLabels, ref nextLabel, loopStack);
                break;

            case ReturnStatementSyntax returnStatement:
            {
                ValueExpressionSyntax? returnValue = null;
                if (returnStatement.ValueExpression != null)
                {
                    returnValue = EnsureValueExpression(
                        FlattenExpression(returnStatement.ValueExpression, output, temps, forceValue: true),
                        output,
                        temps);
                }

                output.Add(new ReturnStatementSyntax(returnStatement.Return, returnValue, returnStatement.Semicolon));
                if (returnValue != null)
                    temps.ReleaseExpression(returnValue);
                break;
            }

            case PostfixUnaryStatementSyntax postfix:
            {
                PostfixUnaryExpressionSyntax postfixExpr = FlattenPostfix(postfix.Expression, output, temps);
                output.Add(new PostfixUnaryStatementSyntax(postfixExpr, postfix.Semicolon));
                temps.ReleaseExpression(postfixExpr);
                break;
            }

            default:
                output.Add(statement);
                break;
        }
    }

    private void LowerWhileStatement(
        WhileStatementSyntax whileStatement,
        List<StatementSyntax> output,
        TempAllocator temps,
        HashSet<string> usedLabels,
        ref int nextLabel,
        Stack<LoopContext> loopStack)
    {
        // Empty / one-liner while → spin: L: if cond goto L;
        if (whileStatement.Body is null || whileStatement.Body.Statements.Count == 0)
        {
            LowerWhileSpin(whileStatement.Condition, output, temps, usedLabels, ref nextLabel);
            return;
        }

        string headLabel = AllocateLabel(usedLabels, ref nextLabel);
        string exitLabel = AllocateLabel(usedLabels, ref nextLabel);
        var context = new LoopContext(headLabel, headLabel, exitLabel);
        loopStack.Push(context);

        output.Add(CreateLabel(headLabel));
        EmitIfNotGoto(NormalizeCondition(whileStatement.Condition), exitLabel, output, temps);

        foreach (StatementSyntax nested in whileStatement.Body.Statements)
            FlattenStatement(nested, output, temps, usedLabels, ref nextLabel, loopStack);

        output.Add(CreateGoto(headLabel));
        output.Add(CreateLabel(exitLabel));
        loopStack.Pop();
    }

    private void LowerWhileSpin(
        ExpressionSyntax condition,
        List<StatementSyntax> output,
        TempAllocator temps,
        HashSet<string> usedLabels,
        ref int nextLabel)
    {
        // One-liner / empty while has no body for break/continue; emit classic spin only.
        string headLabel = AllocateLabel(usedLabels, ref nextLabel);
        output.Add(CreateLabel(headLabel));
        EmitSpinIfGoto(NormalizeCondition(condition), headLabel, output, temps);
    }

    private void EmitSpinIfGoto(
        ExpressionSyntax condition,
        string headLabel,
        List<StatementSyntax> output,
        TempAllocator temps)
    {
        SyntaxToken ifToken = syntaxFactory.Token(SyntaxTokenKind.IfKeyword);
        SyntaxToken semicolon = syntaxFactory.Token(SyntaxTokenKind.Semicolon);
        GotoExpressionSyntax gotoHead = CreateGotoExpression(headLabel);

        ExpressionSyntax flatCondition = ExpressionParenthesizer.UnwrapParentheses(
            FlattenExpression(condition, output, temps, forceValue: false));

        // while (not x); → L: if not x goto L;
        if (flatCondition is UnaryExpressionSyntax unary &&
            unary.Operation.RawKind is (int)SyntaxTokenKind.NotKeyword or (int)SyntaxTokenKind.Not)
        {
            ValueExpressionSyntax value = EnsureValueExpression(unary.Value, output, temps);
            var comparison = new UnaryExpressionSyntax(unary.Operation, value);
            output.Add(new IfNotGotoStatementSyntax(ifToken, comparison, gotoHead, semicolon));
            temps.ReleaseExpression(comparison);
            return;
        }

        // while (x); → L: if x goto L;
        ValueExpressionSyntax condValue = EnsureValueExpression(flatCondition, output, temps);
        output.Add(new IfGotoStatementSyntax(ifToken, condValue, gotoHead, semicolon));
        temps.ReleaseExpression(condValue);
    }

    private void LowerDoWhileStatement(
        DoWhileStatementSyntax doWhileStatement,
        List<StatementSyntax> output,
        TempAllocator temps,
        HashSet<string> usedLabels,
        ref int nextLabel,
        Stack<LoopContext> loopStack)
    {
        string headLabel = AllocateLabel(usedLabels, ref nextLabel);
        string continueLabel = AllocateLabel(usedLabels, ref nextLabel);
        string exitLabel = AllocateLabel(usedLabels, ref nextLabel);
        var context = new LoopContext(headLabel, continueLabel, exitLabel);
        loopStack.Push(context);

        output.Add(CreateLabel(headLabel));
        foreach (StatementSyntax nested in doWhileStatement.Body.Statements)
            FlattenStatement(nested, output, temps, usedLabels, ref nextLabel, loopStack);

        output.Add(CreateLabel(continueLabel));
        EmitIfGoto(NormalizeCondition(doWhileStatement.Condition), headLabel, output, temps);
        output.Add(CreateLabel(exitLabel));
        loopStack.Pop();
    }

    private void LowerBreakStatement(
        BreakStatementSyntax breakStatement,
        List<StatementSyntax> output,
        Stack<LoopContext> loopStack)
    {
        if (loopStack.Count == 0)
            throw CreateException("break is only valid inside a loop.", breakStatement.Location);

        output.Add(CreateGoto(loopStack.Peek().ExitLabel));
    }

    private void LowerContinueStatement(
        ContinueStatementSyntax continueStatement,
        List<StatementSyntax> output,
        Stack<LoopContext> loopStack)
    {
        if (loopStack.Count == 0)
            throw CreateException("continue is only valid inside a loop.", continueStatement.Location);

        output.Add(CreateGoto(loopStack.Peek().ContinueLabel));
    }

    private void LowerIfStatement(
        IfStatementSyntax ifStatement,
        List<StatementSyntax> output,
        TempAllocator temps,
        HashSet<string> usedLabels,
        ref int nextLabel,
        Stack<LoopContext> loopStack)
    {
        if (ifStatement.Else is null)
        {
            string endLabel = AllocateLabel(usedLabels, ref nextLabel);
            EmitIfNotGoto(ifStatement.Condition, endLabel, output, temps);
            foreach (StatementSyntax nested in ifStatement.Body.Statements)
                FlattenStatement(nested, output, temps, usedLabels, ref nextLabel, loopStack);
            output.Add(CreateLabel(endLabel));
            return;
        }

        string elseLabel = AllocateLabel(usedLabels, ref nextLabel);
        string joinLabel = AllocateLabel(usedLabels, ref nextLabel);

        EmitIfNotGoto(ifStatement.Condition, elseLabel, output, temps);
        foreach (StatementSyntax nested in ifStatement.Body.Statements)
            FlattenStatement(nested, output, temps, usedLabels, ref nextLabel, loopStack);
        output.Add(CreateGoto(joinLabel));
        output.Add(CreateLabel(elseLabel));

        if (ifStatement.Else.Statement is IfStatementSyntax elseIf)
            LowerIfStatement(elseIf, output, temps, usedLabels, ref nextLabel, loopStack);
        else if (ifStatement.Else.Statement is BlockSyntax elseBlock)
        {
            foreach (StatementSyntax nested in elseBlock.Statements)
                FlattenStatement(nested, output, temps, usedLabels, ref nextLabel, loopStack);
        }
        else
            FlattenStatement(ifStatement.Else.Statement, output, temps, usedLabels, ref nextLabel, loopStack);

        output.Add(CreateLabel(joinLabel));
    }

    private void EmitIfGoto(
        ExpressionSyntax condition,
        string targetLabel,
        List<StatementSyntax> output,
        TempAllocator temps)
    {
        SyntaxToken ifToken = syntaxFactory.Token(SyntaxTokenKind.IfKeyword);
        SyntaxToken semicolon = syntaxFactory.Token(SyntaxTokenKind.Semicolon);
        GotoExpressionSyntax gotoExpr = CreateGotoExpression(targetLabel);

        ExpressionSyntax flatCondition = ExpressionParenthesizer.UnwrapParentheses(
            FlattenExpression(NormalizeCondition(condition), output, temps, forceValue: false));

        ValueExpressionSyntax condValue = EnsureValueExpression(flatCondition, output, temps);
        output.Add(new IfGotoStatementSyntax(ifToken, condValue, gotoExpr, semicolon));
        temps.ReleaseExpression(condValue);
    }

    private void EmitIfNotGoto(
        ExpressionSyntax condition,
        string targetLabel,
        List<StatementSyntax> output,
        TempAllocator temps)
    {
        SyntaxToken ifToken = syntaxFactory.Token(SyntaxTokenKind.IfKeyword);
        SyntaxToken semicolon = syntaxFactory.Token(SyntaxTokenKind.Semicolon);
        GotoExpressionSyntax gotoExpr = CreateGotoExpression(targetLabel);

        ExpressionSyntax flatCondition = ExpressionParenthesizer.UnwrapParentheses(
            FlattenExpression(NormalizeCondition(condition), output, temps, forceValue: false));

        if (flatCondition is UnaryExpressionSyntax unary &&
            unary.Operation.RawKind is (int)SyntaxTokenKind.NotKeyword or (int)SyntaxTokenKind.Not)
        {
            ValueExpressionSyntax value = EnsureValueExpression(unary.Value, output, temps);
            output.Add(new IfGotoStatementSyntax(ifToken, value, gotoExpr, semicolon));
            temps.ReleaseExpression(value);
            return;
        }

        ValueExpressionSyntax condValue = EnsureValueExpression(flatCondition, output, temps);
        var notComparison = new UnaryExpressionSyntax(syntaxFactory.Token(SyntaxTokenKind.NotKeyword), condValue);
        output.Add(new IfNotGotoStatementSyntax(ifToken, notComparison, gotoExpr, semicolon));
        temps.ReleaseExpression(notComparison);
    }

    private ExpressionSyntax NormalizeCondition(ExpressionSyntax condition)
    {
        condition = ExpressionParenthesizer.UnwrapParentheses(condition);

        // Parser wraps literals in ValueExpressionSyntax (`while (true)` → Value(true)).
        if (condition is ValueExpressionSyntax { MetadataParameters: null } value)
            condition = ExpressionParenthesizer.UnwrapParentheses(value.Value);

        if (IsTrueLiteral(condition))
            return CreateIntOneValue();

        return condition;
    }

    private static bool IsTrueLiteral(ExpressionSyntax expression)
    {
        return expression is LiteralExpressionSyntax
        {
            Literal.RawKind: (int)SyntaxTokenKind.TrueKeyword
        };
    }

    private ValueExpressionSyntax CreateIntOneValue()
    {
        return new ValueExpressionSyntax(new LiteralExpressionSyntax(syntaxFactory.NumericLiteral(1)));
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

    private static string AllocateLabel(HashSet<string> usedLabels, ref int nextLabel)
    {
        while (true)
        {
            string name = FormatNumericJumpLabel(nextLabel++);
            if (usedLabels.Add(name))
                return name;
        }
    }

    private static string FormatNumericJumpLabel(int index)
    {
        // "@000@", "@001@", ... — at least 3 digits; more when needed.
        return index < 1000 ? $"@{index:D3}@" : $"@{index}@";
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

            case WhileStatementSyntax { Body: not null } whileStatement:
                CollectUsedLabels(whileStatement.Body.Statements, usedLabels);
                break;

            case DoWhileStatementSyntax doWhile:
                CollectUsedLabels(doWhile.Body.Statements, usedLabels);
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
        TempAllocator temps)
    {
        MethodInvocationParametersSyntax parameters = FlattenParameters(invocation.Parameters, output, temps);
        return new MethodInvocationExpressionSyntax(invocation.Name, invocation.Metadata, parameters);
    }

    private ExpressionSyntax FlattenExpression(
        ExpressionSyntax expression,
        List<StatementSyntax> output,
        TempAllocator temps,
        bool forceValue)
    {
        expression = ExpressionParenthesizer.UnwrapParentheses(expression);

        switch (expression)
        {
            case ValueExpressionSyntax value:
                ExpressionSyntax flattenedInner = FlattenExpression(value.Value, output, temps, forceValue: false);
                if (IsTrueLiteral(flattenedInner))
                    return CreateIntOneValue();

                if (flattenedInner is VariableExpressionSyntax or LiteralExpressionSyntax or UnaryExpressionSyntax)
                    return new ValueExpressionSyntax(flattenedInner, value.MetadataParameters);

                ValueExpressionSyntax spilled = Spill(flattenedInner, output, temps);
                return value.MetadataParameters is null
                    ? spilled
                    : new ValueExpressionSyntax(spilled.Value, value.MetadataParameters);

            case VariableExpressionSyntax:
                return forceValue ? new ValueExpressionSyntax(expression) : expression;

            case LiteralExpressionSyntax literal:
                if (IsTrueLiteral(literal))
                {
                    ExpressionSyntax one = new LiteralExpressionSyntax(syntaxFactory.NumericLiteral(1));
                    return forceValue ? new ValueExpressionSyntax(one) : one;
                }
                return forceValue ? new ValueExpressionSyntax(expression) : expression;

            case UnaryExpressionSyntax unary:
                return FlattenUnary(unary, output, temps);

            case BinaryExpressionSyntax binary:
                ExpressionSyntax left = EnsureArgument(FlattenExpression(binary.Left, output, temps, false), output, temps);
                ExpressionSyntax right = EnsureArgument(FlattenExpression(binary.Right, output, temps, false), output, temps);
                var flatBinary = new BinaryExpressionSyntax(left, binary.Operation, right);
                return forceValue ? Spill(flatBinary, output, temps) : flatBinary;

            case LogicalExpressionSyntax logical:
                ExpressionSyntax logicalLeft = EnsureArgument(FlattenExpression(logical.Left, output, temps, false), output, temps);
                ExpressionSyntax logicalRight = EnsureArgument(FlattenExpression(logical.Right, output, temps, false), output, temps);
                var flatLogical = new LogicalExpressionSyntax(logicalLeft, logical.Operation, logicalRight);
                return forceValue ? Spill(flatLogical, output, temps) : flatLogical;

            case MethodInvocationExpressionSyntax invocation:
                MethodInvocationParametersSyntax parameters = FlattenParameters(invocation.Parameters, output, temps);
                var flatInvocation = new MethodInvocationExpressionSyntax(invocation.Name, invocation.Metadata, parameters);
                return forceValue ? Spill(flatInvocation, output, temps) : flatInvocation;

            case PostfixUnaryExpressionSyntax postfix:
                return FlattenPostfix(postfix, output, temps);

            case ArrayIndexExpressionSyntax arrayIndex:
                ValueExpressionSyntax arrayValue = EnsureValueExpression(
                    FlattenExpression(arrayIndex.Value, output, temps, true), output, temps);
                return new ArrayIndexExpressionSyntax(arrayValue, arrayIndex.Indexer);

            case TypeCastValueExpressionSyntax typeCast:
                ValueExpressionSyntax castValue = EnsureValueExpression(
                    FlattenExpression(typeCast.Value, output, temps, true), output, temps);
                var flatCast = new TypeCastValueExpressionSyntax(typeCast.TypeCast, castValue);
                return forceValue ? Spill(flatCast, output, temps) : flatCast;

            case SwitchExpressionSyntax switchExpression:
                ExpressionSyntax switchValue = EnsureArgument(
                    FlattenExpression(switchExpression.Value, output, temps, false), output, temps);
                var flatSwitch = new SwitchExpressionSyntax(switchValue, switchExpression.Switch, switchExpression.CaseBlock);
                return forceValue ? Spill(flatSwitch, output, temps) : flatSwitch;

            case ParenthesizedExpressionSyntax parenthesized:
                return FlattenExpression(parenthesized.Expression, output, temps, forceValue);

            default:
                return forceValue ? Spill(expression, output, temps) : expression;
        }
    }

    private UnaryExpressionSyntax FlattenUnary(
        UnaryExpressionSyntax unary,
        List<StatementSyntax> output,
        TempAllocator temps)
    {
        ValueExpressionSyntax value = EnsureValueExpression(
            FlattenExpression(unary.Value, output, temps, true), output, temps);
        return new UnaryExpressionSyntax(unary.Operation, value);
    }

    private PostfixUnaryExpressionSyntax FlattenPostfix(
        PostfixUnaryExpressionSyntax postfix,
        List<StatementSyntax> output,
        TempAllocator temps)
    {
        ExpressionSyntax value = FlattenExpression(postfix.Value, output, temps, false);
        return new PostfixUnaryExpressionSyntax(value, postfix.Operation);
    }

    private MethodInvocationParametersSyntax FlattenParameters(
        MethodInvocationParametersSyntax parameters,
        List<StatementSyntax> output,
        TempAllocator temps)
    {
        if (parameters.ParameterList?.Elements is null)
            return parameters;

        var elements = new List<ExpressionSyntax>();
        foreach (ExpressionSyntax parameter in parameters.ParameterList.Elements)
        {
            ExpressionSyntax flattened = FlattenExpression(parameter, output, temps, forceValue: true);
            elements.Add(EnsureValueExpression(flattened, output, temps));
        }

        return new MethodInvocationParametersSyntax(
            parameters.ParenOpen,
            new CommaSeparatedSyntaxList<ExpressionSyntax>(elements),
            parameters.ParenClose);
    }

    private ExpressionSyntax EnsureArgument(ExpressionSyntax expression, List<StatementSyntax> output, TempAllocator temps)
    {
        expression = ExpressionParenthesizer.UnwrapParentheses(expression);

        if (expression is ValueExpressionSyntax or VariableExpressionSyntax or LiteralExpressionSyntax)
            return expression is ValueExpressionSyntax ? expression : new ValueExpressionSyntax(expression);

        if (expression is UnaryExpressionSyntax { Operation.RawKind: (int)SyntaxTokenKind.Minus, Value: ValueExpressionSyntax })
            return new ValueExpressionSyntax(expression);

        return Spill(expression, output, temps);
    }

    private ValueExpressionSyntax EnsureValueExpression(
        ExpressionSyntax expression,
        List<StatementSyntax> output,
        TempAllocator temps)
    {
        expression = ExpressionParenthesizer.UnwrapParentheses(expression);

        if (expression is ValueExpressionSyntax value)
        {
            if (IsTrueLiteral(value.Value))
                return CreateIntOneValue();

            if (value.Value is VariableExpressionSyntax or LiteralExpressionSyntax)
                return value;

            if (value.Value is UnaryExpressionSyntax { Operation.RawKind: (int)SyntaxTokenKind.Minus })
                return value;

            return Spill(value.Value, output, temps);
        }

        if (IsTrueLiteral(expression))
            return CreateIntOneValue();

        if (expression is VariableExpressionSyntax or LiteralExpressionSyntax)
            return new ValueExpressionSyntax(expression);

        if (expression is UnaryExpressionSyntax { Operation.RawKind: (int)SyntaxTokenKind.Minus, Value: ValueExpressionSyntax })
            return new ValueExpressionSyntax(expression);

        return Spill(expression, output, temps);
    }

    private ValueExpressionSyntax Spill(ExpressionSyntax expression, List<StatementSyntax> output, TempAllocator temps)
    {
        // Operands are read before the destination is written, so their slots can
        // be reused as the spill target (e.g. $temp1 = sub5622(..., $temp1)).
        temps.ReleaseExpression(expression);
        ValueExpressionSyntax temp = AllocateTemp(temps);
        output.Add(new AssignmentStatementSyntax(
            temp,
            syntaxFactory.Token(SyntaxTokenKind.EqualsSign),
            expression,
            syntaxFactory.Token(SyntaxTokenKind.Semicolon)));
        return temp;
    }

    private ValueExpressionSyntax AllocateTemp(TempAllocator temps)
    {
        int slot = temps.Allocate();
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

            case WhileStatementSyntax whileStatement:
                CollectUsedTempSlots(whileStatement.Condition, usedTemps);
                if (whileStatement.Body != null)
                    CollectUsedTempSlots(whileStatement.Body.Statements, usedTemps);
                break;

            case DoWhileStatementSyntax doWhile:
                CollectUsedTempSlots(doWhile.Condition, usedTemps);
                CollectUsedTempSlots(doWhile.Body.Statements, usedTemps);
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

    private static Exception CreateException(string message, SyntaxLocation location)
    {
        return new InvalidOperationException($"{message} (Line {location.Line}, Column {location.Column})");
    }

    private sealed record LoopContext(string HeadLabel, string ContinueLabel, string ExitLabel);

    /// <summary>
    /// Allocates <c>$temp</c> slots with reuse after last use.
    /// Slots present in the source method stay reserved and are never reused for spills.
    /// </summary>
    private sealed class TempAllocator(HashSet<int> reserved)
    {
        private readonly HashSet<int> _live = [];

        public int Allocate()
        {
            int slot = 1;
            while (reserved.Contains(slot) || _live.Contains(slot))
                slot++;

            _live.Add(slot);
            return slot;
        }

        public void ReleaseExpression(ExpressionSyntax expression)
        {
            switch (expression)
            {
                case VariableExpressionSyntax variable:
                    if (TryGetTempSlot(variable, out int slot))
                        Release(slot);
                    break;

                case ValueExpressionSyntax value:
                    ReleaseExpression(value.Value);
                    break;

                case ParenthesizedExpressionSyntax parenthesized:
                    ReleaseExpression(parenthesized.Expression);
                    break;

                case UnaryExpressionSyntax unary:
                    ReleaseExpression(unary.Value);
                    break;

                case BinaryExpressionSyntax binary:
                    ReleaseExpression(binary.Left);
                    ReleaseExpression(binary.Right);
                    break;

                case LogicalExpressionSyntax logical:
                    ReleaseExpression(logical.Left);
                    ReleaseExpression(logical.Right);
                    break;

                case MethodInvocationExpressionSyntax invocation:
                    if (invocation.Parameters.ParameterList?.Elements is not null)
                    {
                        foreach (ExpressionSyntax parameter in invocation.Parameters.ParameterList.Elements)
                            ReleaseExpression(parameter);
                    }
                    break;

                case PostfixUnaryExpressionSyntax postfix:
                    ReleaseExpression(postfix.Value);
                    break;

                case ArrayIndexExpressionSyntax arrayIndex:
                    ReleaseExpression(arrayIndex.Value);
                    foreach (ArrayIndexerExpressionSyntax indexer in arrayIndex.Indexer)
                        ReleaseExpression(indexer.Index);
                    break;

                case TypeCastValueExpressionSyntax typeCast:
                    ReleaseExpression(typeCast.Value);
                    break;

                case SwitchExpressionSyntax switchExpression:
                    ReleaseExpression(switchExpression.Value);
                    break;
            }
        }

        private void Release(int slot)
        {
            if (!reserved.Contains(slot))
                _live.Remove(slot);
        }
    }
}
