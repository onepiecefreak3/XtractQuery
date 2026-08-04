using Logic.Business.Level5ScriptManagement.InternalContract.Conversion.HighLevelSyntax;
using Logic.Domain.CodeAnalysis.Contract.DataClasses;
using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;
using Logic.Domain.CodeAnalysis.Contract.Level5;

namespace Logic.Business.Level5ScriptManagement.Conversion.HighLevelSyntax;

internal class LowLevelCodeUnitConverter(ILevel5SyntaxFactory syntaxFactory) : ILowLevelCodeUnitConverter
{
    public CodeUnitSyntax Convert(CodeUnitSyntax tree)
    {
        var members = new List<CodeUnitMemberSyntax>();
        foreach (CodeUnitMemberSyntax member in tree.Members)
        {
            if (member is MethodDeclarationSyntax method)
                members.Add(ConvertMethod(method));
            else
                members.Add(member);
        }

        return new CodeUnitSyntax(members);
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
                FlattenAssignment(
                    assignment.Left,
                    assignment.EqualsOperator,
                    assignment.Right,
                    assignment.Semicolon,
                    output,
                    temps);
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

            case ForStatementSyntax forStatement:
                LowerForStatement(forStatement, output, temps, usedLabels, ref nextLabel, loopStack);
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

    private void LowerForStatement(
        ForStatementSyntax forStatement,
        List<StatementSyntax> output,
        TempAllocator temps,
        HashSet<string> usedLabels,
        ref int nextLabel,
        Stack<LoopContext> loopStack)
    {
        if (forStatement.Initializer != null)
            FlattenStatement(EnsureStatementSemicolon(forStatement.Initializer), output, temps, usedLabels, ref nextLabel, loopStack);

        string headLabel = AllocateLabel(usedLabels, ref nextLabel);
        string exitLabel = AllocateLabel(usedLabels, ref nextLabel);
        // Only allocate a distinct continue latch when the body uses continue; otherwise
        // the latch would become an unreferenced dangling label after re-raise.
        bool needsContinueLatch = ContainsContinue(forStatement.Body.Statements);
        string continueLabel = needsContinueLatch
            ? AllocateLabel(usedLabels, ref nextLabel)
            : headLabel;
        var context = new LoopContext(headLabel, continueLabel, exitLabel);
        loopStack.Push(context);

        output.Add(CreateLabel(headLabel));
        EmitIfNotGoto(NormalizeCondition(forStatement.Condition), exitLabel, output, temps);

        foreach (StatementSyntax nested in forStatement.Body.Statements)
            FlattenStatement(nested, output, temps, usedLabels, ref nextLabel, loopStack);

        if (needsContinueLatch)
            output.Add(CreateLabel(continueLabel));

        if (forStatement.Iterator != null)
            FlattenStatement(EnsureStatementSemicolon(forStatement.Iterator), output, temps, usedLabels, ref nextLabel, loopStack);

        output.Add(CreateGoto(headLabel));
        output.Add(CreateLabel(exitLabel));
        loopStack.Pop();
    }

    private static bool ContainsContinue(IReadOnlyList<StatementSyntax> statements)
    {
        foreach (StatementSyntax statement in statements)
        {
            switch (statement)
            {
                case ContinueStatementSyntax:
                    return true;

                case IfStatementSyntax ifStatement:
                    if (ContainsContinue(ifStatement.Body.Statements))
                        return true;
                    if (ifStatement.Else != null && ContainsContinueStatement(ifStatement.Else.Statement))
                        return true;
                    break;

                case BlockSyntax block:
                    if (ContainsContinue(block.Statements))
                        return true;
                    break;

                // Nested loops own their continues; do not scan into them.
            }
        }

        return false;
    }

    private static bool ContainsContinueStatement(StatementSyntax statement)
    {
        return statement switch
        {
            ContinueStatementSyntax => true,
            IfStatementSyntax ifStatement => ContainsContinue(ifStatement.Body.Statements) ||
                                             (ifStatement.Else != null && ContainsContinueStatement(ifStatement.Else.Statement)),
            BlockSyntax block => ContainsContinue(block.Statements),
            _ => false
        };
    }

    private StatementSyntax EnsureStatementSemicolon(StatementSyntax statement)
    {
        SyntaxToken semicolon = syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        switch (statement)
        {
            case AssignmentStatementSyntax assignment when string.IsNullOrEmpty(assignment.Semicolon.Text):
                return new AssignmentStatementSyntax(
                    assignment.Left, assignment.EqualsOperator, assignment.Right, semicolon);

            case PostfixUnaryStatementSyntax postfix when string.IsNullOrEmpty(postfix.Semicolon.Text):
                return new PostfixUnaryStatementSyntax(postfix.Expression, semicolon);

            default:
                return statement;
        }
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
        // Empty `else { }` is indistinguishable from if-then after jump-table hash sort
        // co-locates ELSE/JOIN at the same instruction index. Emit the if-then shape.
        if (ifStatement.Else is null || IsEmptyElse(ifStatement.Else))
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

    private static bool IsEmptyElse(ElseClauseSyntax elseClause)
    {
        return elseClause.Statement is BlockSyntax { Statements.Count: 0 };
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

            case ForStatementSyntax forStatement:
                CollectUsedLabels(forStatement.Body.Statements, usedLabels);
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

    private void FlattenAssignment(
        ExpressionSyntax left,
        SyntaxToken operation,
        ExpressionSyntax right,
        SyntaxToken semicolon,
        List<StatementSyntax> output,
        TempAllocator temps)
    {
        if (right is AssignmentExpressionSyntax nested)
        {
            if (operation.RawKind != (int)SyntaxTokenKind.EqualsSign ||
                nested.Operation.RawKind != (int)SyntaxTokenKind.EqualsSign)
                throw CreateException("Only '=' can be chained in assignments.", nested.Location);

            FlattenAssignment(nested.Left, nested.Operation, nested.Right, semicolon, output, temps);

            ExpressionSyntax flatLeft = FlattenExpression(left, output, temps, forceValue: false);
            ValueExpressionSyntax copyRight = EnsureValueExpression(
                FlattenExpression(nested.Left, output, temps, forceValue: true), output, temps);

            output.Add(new AssignmentStatementSyntax(flatLeft, operation, copyRight, semicolon));
            temps.ReleaseExpression(copyRight);
            temps.ReleaseExpression(flatLeft);
            return;
        }

        ExpressionSyntax flatTarget = FlattenExpression(left, output, temps, forceValue: false);

        // Plain `=` may keep instruction-shaped RHS (calls, binaries, casts) when the
        // destination is a plain slot. Array stores append LHS indexes as trailing
        // arguments; only type 100 / compound-assigns peel those on decompile, so a
        // complex RHS would steal the indexes (`$a[i] = $b[j]` → `$a = $b[j][i]`).
        // Spill to a value first: `$temp = rhs; $a[i] = $temp`.
        bool forceValueRhs = operation.RawKind != (int)SyntaxTokenKind.EqualsSign
                             || flatTarget is ArrayIndexExpressionSyntax;
        ExpressionSyntax flatValue = forceValueRhs
            ? EnsureValueExpression(
                FlattenExpression(right, output, temps, forceValue: true), output, temps)
            : FlattenExpression(right, output, temps, forceValue: false);

        output.Add(new AssignmentStatementSyntax(flatTarget, operation, flatValue, semicolon));
        temps.ReleaseExpression(flatValue);
        temps.ReleaseExpression(flatTarget);
    }

    private ValueExpressionSyntax FlattenAssignmentExpression(
        AssignmentExpressionSyntax assignment,
        List<StatementSyntax> output,
        TempAllocator temps)
    {
        if (assignment.Operation.RawKind != (int)SyntaxTokenKind.EqualsSign)
            throw CreateException("Only '=' can be chained in assignments.", assignment.Location);

        FlattenAssignment(
            assignment.Left,
            assignment.Operation,
            assignment.Right,
            syntaxFactory.Token(SyntaxTokenKind.Semicolon),
            output,
            temps);

        return EnsureValueExpression(
            FlattenExpression(assignment.Left, output, temps, forceValue: true),
            output,
            temps);
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

            case TypeCastValueExpressionSyntax typeCast:
                // Same shape as other unaries: flatten the operand to a value, keep the cast.
                // Callers that need a bare value (args, conditions) spill via EnsureArgument /
                // EnsureValueExpression — casts are their own instruction and cannot be inlined.
                return FlattenTypeCast(typeCast, output, temps);

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
                IReadOnlyList<ArrayIndexerExpressionSyntax> indexers =
                    FlattenIndexers(arrayIndex.Indexer, output, temps);
                return new ArrayIndexExpressionSyntax(arrayValue, indexers);

            case ArrayInstantiationExpressionSyntax arrayInstantiation:
                IReadOnlyList<ArrayIndexerExpressionSyntax> instantiationIndexers =
                    FlattenIndexers(arrayInstantiation.Indexer, output, temps);
                var flatInstantiation = new ArrayInstantiationExpressionSyntax(
                    arrayInstantiation.New,
                    instantiationIndexers);
                return forceValue ? Spill(flatInstantiation, output, temps) : flatInstantiation;

            case SwitchExpressionSyntax switchExpression:
                ExpressionSyntax switchValue = EnsureArgument(
                    FlattenExpression(switchExpression.Value, output, temps, false), output, temps);
                var flatSwitch = new SwitchExpressionSyntax(switchValue, switchExpression.Switch, switchExpression.CaseBlock);
                return forceValue ? Spill(flatSwitch, output, temps) : flatSwitch;

            case AssignmentExpressionSyntax assignment:
            {
                return FlattenAssignmentExpression(assignment, output, temps);
            }

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

    private TypeCastValueExpressionSyntax FlattenTypeCast(
        TypeCastValueExpressionSyntax typeCast,
        List<StatementSyntax> output,
        TempAllocator temps)
    {
        // Operand may be a method invocation or other primary wrapped in ValueExpression
        // (e.g. `(int)random(10)`). Spill that first so the cast instruction only sees a slot.
        ValueExpressionSyntax castValue = EnsureValueExpression(
            FlattenExpression(typeCast.Value, output, temps, true), output, temps);
        return new TypeCastValueExpressionSyntax(typeCast.TypeCast, castValue);
    }

    private IReadOnlyList<ArrayIndexerExpressionSyntax> FlattenIndexers(
        IReadOnlyList<ArrayIndexerExpressionSyntax> indexers,
        List<StatementSyntax> output,
        TempAllocator temps)
    {
        // VM array ops take value arguments only — spill `$arr[$i + 1]` to `$temp = $i + 1; ...[$temp]`.
        var result = new List<ArrayIndexerExpressionSyntax>(indexers.Count);
        foreach (ArrayIndexerExpressionSyntax indexer in indexers)
        {
            ValueExpressionSyntax index = EnsureValueExpression(
                FlattenExpression(indexer.Index, output, temps, true), output, temps);
            result.Add(new ArrayIndexerExpressionSyntax(indexer.BracketOpen, index, indexer.BracketClose));
        }

        return result;
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

        // Only `-inf`/`-nan` encode as float arguments. `-$var` / `-(...)` are negate
        // instructions and must be spilled before use as a call/binary operand.
        if (IsEncodableNegativeFloat(expression))
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

            if (IsEncodableNegativeFloat(value.Value))
                return value;

            return Spill(value.Value, output, temps);
        }

        if (IsTrueLiteral(expression))
            return CreateIntOneValue();

        if (expression is VariableExpressionSyntax or LiteralExpressionSyntax)
            return new ValueExpressionSyntax(expression);

        if (IsEncodableNegativeFloat(expression))
            return new ValueExpressionSyntax(expression);

        // TypeCastValueExpressionSyntax, `-$var`, `not x`, and other unaries spill here.
        return Spill(expression, output, temps);
    }

    /// <summary>
    /// True for unary-minus over float keywords (<c>-inf</c>, <c>-nan</c>).
    /// Signed numeric floats like <c>-12f</c> are a single literal token, not unary.
    /// </summary>
    private static bool IsEncodableNegativeFloat(ExpressionSyntax expression)
    {
        return expression is UnaryExpressionSyntax
        {
            Operation.RawKind: (int)SyntaxTokenKind.Minus,
            Value: ValueExpressionSyntax
            {
                Value: LiteralExpressionSyntax
                {
                    Literal.RawKind: (int)SyntaxTokenKind.Infinite
                        or (int)SyntaxTokenKind.InfinityKeyword
                        or (int)SyntaxTokenKind.InfKeyword
                        or (int)SyntaxTokenKind.NanKeyword
                }
            }
        };
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

            case ForStatementSyntax forStatement:
                if (forStatement.Initializer != null)
                    CollectUsedTempSlots(forStatement.Initializer, usedTemps);
                CollectUsedTempSlots(forStatement.Condition, usedTemps);
                if (forStatement.Iterator != null)
                    CollectUsedTempSlots(forStatement.Iterator, usedTemps);
                CollectUsedTempSlots(forStatement.Body.Statements, usedTemps);
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
                foreach (ArrayIndexerExpressionSyntax indexer in arrayIndex.Indexer)
                    CollectUsedTempSlots(indexer.Index, usedTemps);
                break;

            case ArrayInstantiationExpressionSyntax arrayInstantiation:
                foreach (ArrayIndexerExpressionSyntax indexer in arrayInstantiation.Indexer)
                    CollectUsedTempSlots(indexer.Index, usedTemps);
                break;

            case TypeCastValueExpressionSyntax typeCast:
                CollectUsedTempSlots(typeCast.Value, usedTemps);
                break;

            case SwitchExpressionSyntax switchExpression:
                CollectUsedTempSlots(switchExpression.Value, usedTemps);
                break;

            case AssignmentExpressionSyntax assignment:
                CollectUsedTempSlots(assignment.Left, usedTemps);
                CollectUsedTempSlots(assignment.Right, usedTemps);
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

                case ArrayInstantiationExpressionSyntax arrayInstantiation:
                    foreach (ArrayIndexerExpressionSyntax indexer in arrayInstantiation.Indexer)
                        ReleaseExpression(indexer.Index);
                    break;

                case TypeCastValueExpressionSyntax typeCast:
                    ReleaseExpression(typeCast.Value);
                    break;

                case SwitchExpressionSyntax switchExpression:
                    ReleaseExpression(switchExpression.Value);
                    break;

                case AssignmentExpressionSyntax assignment:
                    ReleaseExpression(assignment.Left);
                    ReleaseExpression(assignment.Right);
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
