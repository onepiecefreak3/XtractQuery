using Logic.Business.Level5ScriptManagement.InternalContract.Conversion.HighLevelSyntax;
using Logic.Domain.CodeAnalysis.Contract.DataClasses;
using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;
using Logic.Domain.CodeAnalysis.Contract.Level5;

namespace Logic.Business.Level5ScriptManagement.Conversion.HighLevelSyntax;

internal class Xq32LowLevelCodeUnitConverter(ILevel5SyntaxFactory syntaxFactory) : IXq32LowLevelCodeUnitConverter
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
        var temps = new TempSlotFrame(reservedTemps);

        var usedLabels = new HashSet<string>(StringComparer.Ordinal);
        CollectUsedLabels(method.Body.Expressions, usedLabels);
        int nextLabel = 0;
        int ifConditionDest = 1;
        var packedReserve = new NamedDestReserve();

        var loopStack = new Stack<LoopContext>();
        var statements = new List<StatementSyntax>();
        foreach (StatementSyntax statement in method.Body.Expressions)
            FlattenStatement(
                statement, statements, temps, usedLabels, ref nextLabel, loopStack, ref ifConditionDest, packedReserve);

        var body = new MethodDeclarationBodySyntax(method.Body.CurlyOpen, statements, method.Body.CurlyClose);
        return new MethodDeclarationSyntax(method.Identifier, method.MetadataParameters, method.Parameters, body);
    }

    private void FlattenStatement(
        StatementSyntax statement,
        List<StatementSyntax> output,
        TempSlotFrame temps,
        HashSet<string> usedLabels,
        ref int nextLabel,
        Stack<LoopContext> loopStack,
        ref int ifConditionDest,
        NamedDestReserve packedReserve)
    {
        if (statement is IfStatementSyntax ifStatement)
        {
            packedReserve.Clear();
            LowerIfStatement(
                ifStatement, output, temps, usedLabels, ref nextLabel, loopStack, ref ifConditionDest, packedReserve);
            return;
        }

        ifConditionDest = 1;

        switch (statement)
        {
            case MethodInvocationStatementSyntax invocation:
                {
                    MethodInvocationExpressionSyntax call = FlattenInvocationExpression(
                        invocation, output, temps, dest: 1);
                    Spill(call, output, temps, dest: 1);
                    packedReserve.Clear();
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
                        temps,
                        packedReserve.End);
                    UpdatePackedReserveAfterAssignment(assignment, packedReserve);
                    break;
                }

            case IfGotoStatementSyntax ifGoto:
                {
                    ValueExpressionSyntax ifValue = EnsureValueExpression(
                        FlattenExpression(ifGoto.Value, output, temps, dest: 1, reservedEnd: 1, forceValue: true),
                        output,
                        temps,
                        dest: 1);
                    output.Add(new IfGotoStatementSyntax(ifGoto.If, ifValue, ifGoto.Goto, ifGoto.Semicolon));
                    break;
                }

            case IfNotGotoStatementSyntax ifNotGoto:
                {
                    UnaryExpressionSyntax comparison = FlattenUnary(
                        ifNotGoto.Comparison, output, temps, dest: 1, reservedEnd: 1);
                    output.Add(new IfNotGotoStatementSyntax(ifNotGoto.If, comparison, ifNotGoto.Goto, ifNotGoto.Semicolon));
                    break;
                }

            case WhileStatementSyntax whileStatement:
                packedReserve.Clear();
                LowerWhileStatement(
                    whileStatement, output, temps, usedLabels, ref nextLabel, loopStack, ref ifConditionDest, packedReserve);
                break;

            case ForStatementSyntax forStatement:
                packedReserve.Clear();
                LowerForStatement(
                    forStatement, output, temps, usedLabels, ref nextLabel, loopStack, ref ifConditionDest, packedReserve);
                break;

            case DoWhileStatementSyntax doWhileStatement:
                packedReserve.Clear();
                LowerDoWhileStatement(
                    doWhileStatement, output, temps, usedLabels, ref nextLabel, loopStack, ref ifConditionDest, packedReserve);
                break;

            case BreakStatementSyntax breakStatement:
                LowerBreakStatement(breakStatement, output, loopStack);
                break;

            case ContinueStatementSyntax continueStatement:
                LowerContinueStatement(continueStatement, output, loopStack);
                break;

            case BlockSyntax block:
                foreach (StatementSyntax nested in block.Statements)
                    FlattenStatement(
                        nested, output, temps, usedLabels, ref nextLabel, loopStack, ref ifConditionDest, packedReserve);
                break;

            case ReturnStatementSyntax returnStatement:
                {
                    ValueExpressionSyntax? returnValue = null;
                    if (returnStatement.ValueExpression != null)
                    {
                        returnValue = EnsureValueExpression(
                            FlattenExpression(returnStatement.ValueExpression, output, temps, dest: 1, reservedEnd: 1, forceValue: true),
                            output,
                            temps,
                            dest: 1);
                    }

                    output.Add(new ReturnStatementSyntax(returnStatement.Return, returnValue, returnStatement.Semicolon));
                    packedReserve.Clear();
                    break;
                }

            case PostfixUnaryStatementSyntax postfix:
                {
                    PostfixUnaryExpressionSyntax postfixExpr = FlattenPostfix(
                        postfix.Expression, output, temps, dest: 0, reservedEnd: 0);
                    output.Add(new PostfixUnaryStatementSyntax(postfixExpr, postfix.Semicolon));
                    break;
                }

            default:
                packedReserve.Clear();
                output.Add(statement);
                break;
        }
    }

    private void LowerWhileStatement(
        WhileStatementSyntax whileStatement,
        List<StatementSyntax> output,
        TempSlotFrame temps,
        HashSet<string> usedLabels,
        ref int nextLabel,
        Stack<LoopContext> loopStack,
        ref int ifConditionDest,
        NamedDestReserve packedReserve)
    {
        // Empty / one-liner while → spin: L: if cond goto L; (IfGoto dest 1).
        if (whileStatement.Body is null || whileStatement.Body.Statements.Count == 0)
        {
            string spinLabel = AllocateLabel(usedLabels, ref nextLabel);
            output.Add(CreateLabel(spinLabel));
            EmitIfGoto(NormalizeCondition(whileStatement.Condition), spinLabel, output, temps);
            return;
        }

        string headLabel = AllocateLabel(usedLabels, ref nextLabel);
        string exitLabel = AllocateLabel(usedLabels, ref nextLabel);
        var context = new LoopContext(headLabel, headLabel, exitLabel);
        loopStack.Push(context);

        output.Add(CreateLabel(headLabel));
        EmitIfNotGoto(NormalizeCondition(whileStatement.Condition), exitLabel, output, temps, dest: 1);

        foreach (StatementSyntax nested in whileStatement.Body.Statements)
            FlattenStatement(
                nested, output, temps, usedLabels, ref nextLabel, loopStack, ref ifConditionDest, packedReserve);

        output.Add(CreateGoto(headLabel));
        output.Add(CreateLabel(exitLabel));
        loopStack.Pop();
    }

    private void LowerForStatement(
        ForStatementSyntax forStatement,
        List<StatementSyntax> output,
        TempSlotFrame temps,
        HashSet<string> usedLabels,
        ref int nextLabel,
        Stack<LoopContext> loopStack,
        ref int ifConditionDest,
        NamedDestReserve packedReserve)
    {
        if (forStatement.Initializer != null)
            FlattenStatement(
                EnsureStatementSemicolon(forStatement.Initializer),
                output,
                temps,
                usedLabels,
                ref nextLabel,
                loopStack,
                ref ifConditionDest,
                packedReserve);

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
        EmitIfNotGoto(NormalizeCondition(forStatement.Condition), exitLabel, output, temps, dest: 1);

        foreach (StatementSyntax nested in forStatement.Body.Statements)
            FlattenStatement(
                nested, output, temps, usedLabels, ref nextLabel, loopStack, ref ifConditionDest, packedReserve);

        if (needsContinueLatch)
            output.Add(CreateLabel(continueLabel));

        if (forStatement.Iterator != null)
            FlattenStatement(
                EnsureStatementSemicolon(forStatement.Iterator),
                output,
                temps,
                usedLabels,
                ref nextLabel,
                loopStack,
                ref ifConditionDest,
                packedReserve);

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

    private void LowerDoWhileStatement(
        DoWhileStatementSyntax doWhileStatement,
        List<StatementSyntax> output,
        TempSlotFrame temps,
        HashSet<string> usedLabels,
        ref int nextLabel,
        Stack<LoopContext> loopStack,
        ref int ifConditionDest,
        NamedDestReserve packedReserve)
    {
        string headLabel = AllocateLabel(usedLabels, ref nextLabel);
        string continueLabel = AllocateLabel(usedLabels, ref nextLabel);
        string exitLabel = AllocateLabel(usedLabels, ref nextLabel);
        var context = new LoopContext(headLabel, continueLabel, exitLabel);
        loopStack.Push(context);

        output.Add(CreateLabel(headLabel));
        foreach (StatementSyntax nested in doWhileStatement.Body.Statements)
            FlattenStatement(
                nested, output, temps, usedLabels, ref nextLabel, loopStack, ref ifConditionDest, packedReserve);

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
        TempSlotFrame temps,
        HashSet<string> usedLabels,
        ref int nextLabel,
        Stack<LoopContext> loopStack,
        ref int ifConditionDest,
        NamedDestReserve packedReserve)
    {
        int condDest = ifConditionDest;

        // Empty `else { }` is indistinguishable from if-then after jump-table hash sort
        // co-locates ELSE/JOIN at the same instruction index. Emit the if-then shape.
        if (ifStatement.Else is null || IsEmptyElse(ifStatement.Else))
        {
            string endLabel = AllocateLabel(usedLabels, ref nextLabel);
            EmitIfNotGoto(ifStatement.Condition, endLabel, output, temps, condDest);
            foreach (StatementSyntax nested in ifStatement.Body.Statements)
                FlattenStatement(
                    nested, output, temps, usedLabels, ref nextLabel, loopStack, ref ifConditionDest, packedReserve);

            ifConditionDest = 1;
            output.Add(CreateLabel(endLabel));
            return;
        }

        string elseLabel = AllocateLabel(usedLabels, ref nextLabel);
        // Allocate JOIN before the then-body so nested if/while labels come after
        // (`if { while } else` → else @000, join @001, while @002), matching Level5.
        string joinLabel = AllocateLabel(usedLabels, ref nextLabel);

        EmitIfNotGoto(ifStatement.Condition, elseLabel, output, temps, condDest);
        foreach (StatementSyntax nested in ifStatement.Body.Statements)
            FlattenStatement(
                nested, output, temps, usedLabels, ref nextLabel, loopStack, ref ifConditionDest, packedReserve);

        output.Add(CreateGoto(joinLabel));
        output.Add(CreateLabel(elseLabel));

        if (ifStatement.Else.Statement is IfStatementSyntax elseIf)
            LowerIfStatement(
                elseIf, output, temps, usedLabels, ref nextLabel, loopStack, ref ifConditionDest, packedReserve);
        else if (ifStatement.Else.Statement is BlockSyntax elseBlock)
        {
            foreach (StatementSyntax nested in elseBlock.Statements)
                FlattenStatement(
                    nested, output, temps, usedLabels, ref nextLabel, loopStack, ref ifConditionDest, packedReserve);
        }
        else
            FlattenStatement(
                ifStatement.Else.Statement,
                output,
                temps,
                usedLabels,
                ref nextLabel,
                loopStack,
                ref ifConditionDest,
                packedReserve);

        ifConditionDest = 1;
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
        TempSlotFrame temps)
    {
        EmitConditionalJump(condition, targetLabel, jumpIfTrue: true, output, temps, dest: 1);
    }

    private void EmitIfNotGoto(
        ExpressionSyntax condition,
        string targetLabel,
        List<StatementSyntax> output,
        TempSlotFrame temps,
        int dest)
    {
        EmitConditionalJump(condition, targetLabel, jumpIfTrue: false, output, temps, dest);
    }

    private void EmitConditionalJump(
        ExpressionSyntax condition,
        string targetLabel,
        bool jumpIfTrue,
        List<StatementSyntax> output,
        TempSlotFrame temps,
        int dest)
    {
        SyntaxToken ifToken = syntaxFactory.Token(SyntaxTokenKind.IfKeyword);
        SyntaxToken semicolon = syntaxFactory.Token(SyntaxTokenKind.Semicolon);
        GotoExpressionSyntax gotoExpr = CreateGotoExpression(targetLabel);
        dest = dest < 1 ? 1 : dest;

        condition = NormalizeCondition(condition);
        int notCount = 0;
        while (TryUnwrapUnaryNot(condition, out ExpressionSyntax? inner))
        {
            notCount++;
            condition = NormalizeCondition(inner);
        }

        ValueExpressionSyntax condValue = EnsureValueExpression(
            FlattenExpression(condition, output, temps, dest, dest, forceValue: true),
            output,
            temps,
            dest);

        // Level5 encodes polarity in the jump opcode, never as a NOT instruction
        // (`if not x` → `if x goto`; `if x` → `if not x goto`).
        bool useIfGoto = jumpIfTrue == (notCount % 2 == 0);
        if (useIfGoto)
        {
            output.Add(new IfGotoStatementSyntax(ifToken, condValue, gotoExpr, semicolon));
            return;
        }

        var notComparison = new UnaryExpressionSyntax(syntaxFactory.Token(SyntaxTokenKind.NotKeyword), condValue);
        output.Add(new IfNotGotoStatementSyntax(ifToken, notComparison, gotoExpr, semicolon));
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

    private static bool TryUnwrapUnaryNot(ExpressionSyntax expression, out ExpressionSyntax operand)
    {
        expression = ExpressionParenthesizer.UnwrapParentheses(expression);
        if (expression is ValueExpressionSyntax { MetadataParameters: null } value)
            expression = ExpressionParenthesizer.UnwrapParentheses(value.Value);

        if (expression is UnaryExpressionSyntax unary &&
            unary.Operation.RawKind is (int)SyntaxTokenKind.NotKeyword or (int)SyntaxTokenKind.Not)
        {
            operand = unary.Value;
            return true;
        }

        operand = expression;
        return false;
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
        TempSlotFrame temps,
        int dest)
    {
        MethodInvocationParametersSyntax parameters = FlattenParameters(invocation.Parameters, output, temps, dest);
        return new MethodInvocationExpressionSyntax(invocation.Name, invocation.Metadata, parameters);
    }

    private void FlattenAssignment(
        ExpressionSyntax left,
        SyntaxToken operation,
        ExpressionSyntax right,
        SyntaxToken semicolon,
        List<StatementSyntax> output,
        TempSlotFrame temps,
        int packedEnd = 0)
    {
        if (right is AssignmentExpressionSyntax nested)
        {
            if (operation.RawKind != (int)SyntaxTokenKind.EqualsSign ||
                nested.Operation.RawKind != (int)SyntaxTokenKind.EqualsSign)
                throw CreateException("Only '=' can be chained in assignments.", nested.Location);

            FlattenAssignment(nested.Left, nested.Operation, nested.Right, semicolon, output, temps, packedEnd);

            int dest = GetAssignmentDest(left);
            ExpressionSyntax flatLeft = FlattenExpression(left, output, temps, dest, dest, forceValue: false);
            ValueExpressionSyntax copyRight = EnsureValueExpression(
                FlattenExpression(nested.Left, output, temps, dest: 1, reservedEnd: 1, forceValue: true),
                output,
                temps,
                dest: 1);

            output.Add(new AssignmentStatementSyntax(flatLeft, operation, copyRight, semicolon));
            return;
        }

        int assignDest = GetAssignmentDest(left);
        ExpressionSyntax flatTarget = FlattenExpression(left, output, temps, assignDest, assignDest, forceValue: false);

        // Plain `=` may keep instruction-shaped RHS (calls, binaries, casts) when the
        // destination is a plain slot. Array stores append LHS indexes as trailing
        // arguments; only type 100 / compound-assigns peel those on decompile, so a
        // complex RHS would steal the indexes (`$a[i] = $b[j]` → `$a = $b[j][i]`).
        // Spill to a value first: `$temp = rhs; $a[i] = $temp`.
        bool forceValueRhs = operation.RawKind != (int)SyntaxTokenKind.EqualsSign
                             || flatTarget is ArrayIndexExpressionSyntax;
        int rhsDest = forceValueRhs && assignDest < 1 ? 1 : assignDest;
        int reservedEnd = assignDest < 1 ? Math.Max(rhsDest, packedEnd) : rhsDest;
        ExpressionSyntax flatValue = forceValueRhs
            ? EnsureValueExpression(
                FlattenExpression(right, output, temps, rhsDest, reservedEnd, forceValue: true), output, temps, rhsDest)
            : FlattenExpression(right, output, temps, rhsDest, reservedEnd, forceValue: false);

        output.Add(new AssignmentStatementSyntax(flatTarget, operation, flatValue, semicolon));
    }

    private static void UpdatePackedReserveAfterAssignment(
        AssignmentStatementSyntax assignment,
        NamedDestReserve packedReserve)
    {
        if (IsVarToVarCopy(assignment))
        {
            packedReserve.OnVarCopy();
            return;
        }

        packedReserve.OnConsumed(AssignmentNeedsPackedTemps(assignment));
    }

    private static bool IsVarToVarCopy(AssignmentStatementSyntax assignment)
    {
        if (assignment.EqualsOperator.RawKind != (int)SyntaxTokenKind.EqualsSign)
            return false;

        return IsNamedVariable(assignment.Left) && IsNamedVariable(assignment.Right);
    }

    private static bool IsNamedVariable(ExpressionSyntax expression)
    {
        expression = ExpressionParenthesizer.UnwrapParentheses(expression);
        if (expression is ValueExpressionSyntax value)
            expression = ExpressionParenthesizer.UnwrapParentheses(value.Value);

        return expression is VariableExpressionSyntax variable && !TryGetTempSlot(variable, out _);
    }

    private static bool AssignmentNeedsPackedTemps(AssignmentStatementSyntax assignment)
    {
        if (GetAssignmentDest(assignment.Left) >= 1)
            return false;

        return ExpressionNeedsPackedTemps(assignment.Right);
    }

    private static bool ExpressionNeedsPackedTemps(ExpressionSyntax expression)
    {
        expression = ExpressionParenthesizer.UnwrapParentheses(expression);
        if (expression is ValueExpressionSyntax value)
            expression = ExpressionParenthesizer.UnwrapParentheses(value.Value);

        return expression switch
        {
            BinaryExpressionSyntax binary => NeedsTempOperand(binary.Left) || NeedsTempOperand(binary.Right),
            LogicalExpressionSyntax logical => NeedsTempOperand(logical.Left) || NeedsTempOperand(logical.Right),
            _ => false
        };
    }

    private ValueExpressionSyntax FlattenAssignmentExpression(
        AssignmentExpressionSyntax assignment,
        List<StatementSyntax> output,
        TempSlotFrame temps,
        int dest,
        int reservedEnd)
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
            FlattenExpression(assignment.Left, output, temps, dest, reservedEnd, forceValue: true),
            output,
            temps,
            dest);
    }

    private ExpressionSyntax FlattenExpression(
        ExpressionSyntax expression,
        List<StatementSyntax> output,
        TempSlotFrame temps,
        int dest,
        int reservedEnd,
        bool forceValue)
    {
        expression = ExpressionParenthesizer.UnwrapParentheses(expression);
        dest = temps.Resolve(dest);

        switch (expression)
        {
            case ValueExpressionSyntax value:
                ExpressionSyntax flattenedInner = FlattenExpression(
                    value.Value, output, temps, dest, reservedEnd, forceValue: false);
                if (IsTrueLiteral(flattenedInner))
                    return CreateIntOneValue();

                if (flattenedInner is VariableExpressionSyntax or LiteralExpressionSyntax or UnaryExpressionSyntax)
                    return new ValueExpressionSyntax(flattenedInner, value.MetadataParameters);

                ValueExpressionSyntax spilled = Spill(flattenedInner, output, temps, dest);
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
                UnaryExpressionSyntax flatUnary = FlattenUnary(unary, output, temps, dest, reservedEnd);
                return forceValue && dest >= 1 ? Spill(flatUnary, output, temps, dest) : flatUnary;

            case TypeCastValueExpressionSyntax typeCast:
                TypeCastValueExpressionSyntax flatCast = FlattenTypeCast(typeCast, output, temps, dest, reservedEnd);
                return forceValue && dest >= 1 ? Spill(flatCast, output, temps, dest) : flatCast;

            case BinaryExpressionSyntax binary:
                return FlattenBinary(binary, output, temps, dest, reservedEnd, forceValue);

            case LogicalExpressionSyntax logical:
                return FlattenLogical(logical, output, temps, dest, reservedEnd, forceValue);

            case MethodInvocationExpressionSyntax invocation:
                return FlattenInvocation(invocation, output, temps, dest, reservedEnd, forceValue);

            case PostfixUnaryExpressionSyntax postfix:
                return FlattenPostfix(postfix, output, temps, dest, reservedEnd);

            case ArrayIndexExpressionSyntax arrayIndex:
                return FlattenArrayIndex(arrayIndex, output, temps, dest, reservedEnd);

            case ArrayInstantiationExpressionSyntax arrayInstantiation:
                IReadOnlyList<ArrayIndexerExpressionSyntax> instantiationIndexers =
                FlattenIndexers(arrayInstantiation.Indexer, output, temps, dest);
                var flatInstantiation = new ArrayInstantiationExpressionSyntax(
                    arrayInstantiation.New,
                    instantiationIndexers);
                return forceValue && dest >= 1 ? Spill(flatInstantiation, output, temps, dest) : flatInstantiation;

            case SwitchExpressionSyntax switchExpression:
                return FlattenSwitch(switchExpression, output, temps, dest, reservedEnd, forceValue);

            case AssignmentExpressionSyntax assignment:
                return FlattenAssignmentExpression(assignment, output, temps, dest, reservedEnd);

            case ParenthesizedExpressionSyntax parenthesized:
                return FlattenExpression(parenthesized.Expression, output, temps, dest, reservedEnd, forceValue);

            default:
                return forceValue && dest >= 1 ? Spill(expression, output, temps, dest) : expression;
        }
    }

    private ExpressionSyntax FlattenBinary(
        BinaryExpressionSyntax binary,
        List<StatementSyntax> output,
        TempSlotFrame temps,
        int dest,
        int reservedEnd,
        bool forceValue)
    {
        FlattenBinaryOperands(
            binary.Left, binary.Right, output, temps, dest, reservedEnd, out ExpressionSyntax left, out ExpressionSyntax right);
        var flatBinary = new BinaryExpressionSyntax(left, binary.Operation, right);
        return forceValue && dest >= 1 ? Spill(flatBinary, output, temps, dest) : flatBinary;
    }

    private ExpressionSyntax FlattenLogical(
        LogicalExpressionSyntax logical,
        List<StatementSyntax> output,
        TempSlotFrame temps,
        int dest,
        int reservedEnd,
        bool forceValue)
    {
        FlattenBinaryOperands(
            logical.Left, logical.Right, output, temps, dest, reservedEnd, out ExpressionSyntax left, out ExpressionSyntax right);
        var flatLogical = new LogicalExpressionSyntax(left, logical.Operation, right);
        return forceValue && dest >= 1 ? Spill(flatLogical, output, temps, dest) : flatLogical;
    }

    private void FlattenBinaryOperands(
        ExpressionSyntax leftExpr,
        ExpressionSyntax rightExpr,
        List<StatementSyntax> output,
        TempSlotFrame temps,
        int dest,
        int reservedEnd,
        out ExpressionSyntax left,
        out ExpressionSyntax right)
    {
        // Complex operands pack into sequential temps starting at the result dest
        // (`$temp1 = a != f()` → `$temp1 = f(); $temp1 = a != $temp1`). Named dests
        // start at dest 1. Leaves do not occupy slots.
        FlattenPackedBinaryOperands(
            leftExpr, rightExpr, output, temps, dest, reservedEnd, out left, out right);
    }

    private void FlattenPackedBinaryOperands(
        ExpressionSyntax leftExpr,
        ExpressionSyntax rightExpr,
        List<StatementSyntax> output,
        TempSlotFrame temps,
        int dest,
        int reservedEnd,
        out ExpressionSyntax left,
        out ExpressionSyntax right)
    {
        int next = dest >= 1
            ? temps.Resolve(dest)
            : temps.Resolve(FirstOperandDest(dest, EffectivePackedReservedEnd(dest, reservedEnd, leftExpr)));
        bool leftComplex = NeedsTempOperand(leftExpr);
        bool rightComplex = NeedsTempOperand(rightExpr);
        int leftDest = leftComplex ? next : dest;
        if (leftComplex)
            next = temps.Resolve(next + 1);
        int rightDest = rightComplex ? next : dest;
        int childReserved = reservedEnd;
        if (leftComplex)
            childReserved = Math.Max(childReserved, leftDest);
        if (rightComplex)
            childReserved = Math.Max(childReserved, rightDest);

        left = FlattenBinaryOperand(leftExpr, output, temps, leftDest, childReserved);
        right = FlattenBinaryOperand(rightExpr, output, temps, rightDest, childReserved);
    }

    private ExpressionSyntax FlattenBinaryOperand(
        ExpressionSyntax expression,
        List<StatementSyntax> output,
        TempSlotFrame temps,
        int dest,
        int reservedEnd)
    {
        return EnsureArgument(
            FlattenExpression(expression, output, temps, dest, reservedEnd, forceValue: false),
            output,
            temps,
            dest);
    }

    private ExpressionSyntax FlattenInvocation(
        MethodInvocationExpressionSyntax invocation,
        List<StatementSyntax> output,
        TempSlotFrame temps,
        int dest,
        int reservedEnd,
        bool forceValue)
    {
        _ = reservedEnd;
        MethodInvocationParametersSyntax parameters = FlattenParameters(invocation.Parameters, output, temps, dest);
        var flatInvocation = new MethodInvocationExpressionSyntax(invocation.Name, invocation.Metadata, parameters);
        return forceValue && dest >= 1 ? Spill(flatInvocation, output, temps, dest) : flatInvocation;
    }

    private ExpressionSyntax FlattenSwitch(
        SwitchExpressionSyntax switchExpression,
        List<StatementSyntax> output,
        TempSlotFrame temps,
        int dest,
        int reservedEnd,
        bool forceValue)
    {
        int valueDest = dest >= 1
            ? temps.Resolve(dest)
            : temps.Resolve(FirstOperandDest(dest, reservedEnd));
        ExpressionSyntax switchValue = EnsureArgument(
            FlattenExpression(switchExpression.Value, output, temps, valueDest, valueDest, forceValue: false),
            output,
            temps,
            valueDest);
        var flatSwitch = new SwitchExpressionSyntax(switchValue, switchExpression.Switch, switchExpression.CaseBlock);
        return forceValue && dest >= 1 ? Spill(flatSwitch, output, temps, dest) : flatSwitch;
    }

    private ArrayIndexExpressionSyntax FlattenArrayIndex(
        ArrayIndexExpressionSyntax arrayIndex,
        List<StatementSyntax> output,
        TempSlotFrame temps,
        int dest,
        int reservedEnd)
    {
        int valueDest = dest >= 1 ? dest : temps.Resolve(FirstOperandDest(dest, reservedEnd));
        ValueExpressionSyntax arrayValue = EnsureValueExpression(
            FlattenExpression(arrayIndex.Value, output, temps, valueDest, reservedEnd, forceValue: true),
            output,
            temps,
            valueDest);
        IReadOnlyList<ArrayIndexerExpressionSyntax> indexers =
            FlattenIndexers(arrayIndex.Indexer, output, temps, dest);
        return new ArrayIndexExpressionSyntax(arrayValue, indexers);
    }

    private UnaryExpressionSyntax FlattenUnary(
        UnaryExpressionSyntax unary,
        List<StatementSyntax> output,
        TempSlotFrame temps,
        int dest,
        int reservedEnd)
    {
        int childDest = dest >= 1
            ? temps.Resolve(dest)
            : temps.Resolve(FirstOperandDest(dest, reservedEnd));
        ValueExpressionSyntax value = EnsureValueExpression(
            FlattenExpression(unary.Value, output, temps, childDest, Math.Max(childDest, reservedEnd), forceValue: true),
            output,
            temps,
            childDest);
        return new UnaryExpressionSyntax(unary.Operation, value);
    }

    private TypeCastValueExpressionSyntax FlattenTypeCast(
        TypeCastValueExpressionSyntax typeCast,
        List<StatementSyntax> output,
        TempSlotFrame temps,
        int dest,
        int reservedEnd)
    {
        int childDest = dest >= 1
            ? temps.Resolve(dest)
            : temps.Resolve(FirstOperandDest(dest, reservedEnd));
        ValueExpressionSyntax castValue = EnsureValueExpression(
            FlattenExpression(typeCast.Value, output, temps, childDest, Math.Max(childDest, reservedEnd), forceValue: true),
            output,
            temps,
            childDest);
        return new TypeCastValueExpressionSyntax(typeCast.TypeCast, castValue);
    }

    private IReadOnlyList<ArrayIndexerExpressionSyntax> FlattenIndexers(
        IReadOnlyList<ArrayIndexerExpressionSyntax> indexers,
        List<StatementSyntax> output,
        TempSlotFrame temps,
        int dest)
    {
        var result = new List<ArrayIndexerExpressionSyntax>(indexers.Count);
        int next = dest >= 1 ? temps.Resolve(dest) : temps.Resolve(1);
        for (int i = 0; i < indexers.Count; i++)
        {
            ArrayIndexerExpressionSyntax indexer = indexers[i];
            int indexDest;
            if (NeedsTempOperand(indexer.Index))
            {
                indexDest = next;
                next = temps.Resolve(next + 1);
            }
            else
                indexDest = dest >= 1 ? dest : 1;
            ValueExpressionSyntax index = EnsureValueExpression(
                FlattenExpression(indexer.Index, output, temps, indexDest, indexDest, forceValue: true),
                output,
                temps,
                indexDest);
            result.Add(new ArrayIndexerExpressionSyntax(indexer.BracketOpen, index, indexer.BracketClose));
        }

        return result;
    }

    private PostfixUnaryExpressionSyntax FlattenPostfix(
        PostfixUnaryExpressionSyntax postfix,
        List<StatementSyntax> output,
        TempSlotFrame temps,
        int dest,
        int reservedEnd)
    {
        ExpressionSyntax value = FlattenExpression(postfix.Value, output, temps, dest, reservedEnd, forceValue: false);
        return new PostfixUnaryExpressionSyntax(value, postfix.Operation);
    }

    private MethodInvocationParametersSyntax FlattenParameters(
        MethodInvocationParametersSyntax parameters,
        List<StatementSyntax> output,
        TempSlotFrame temps,
        int dest)
    {
        if (parameters.ParameterList?.Elements is null)
            return parameters;

        var elements = new List<ExpressionSyntax>();
        IReadOnlyList<ExpressionSyntax> source = parameters.ParameterList.Elements;
        int next = dest >= 1 ? temps.Resolve(dest) : temps.Resolve(1);
        for (int i = 0; i < source.Count; i++)
        {
            int argDest;
            if (NeedsTempOperand(source[i]))
            {
                argDest = next;
                next = temps.Resolve(next + 1);
            }
            else
                argDest = dest >= 1 ? dest : 1;

            ExpressionSyntax flattened = FlattenExpression(source[i], output, temps, argDest, argDest, forceValue: true);
            elements.Add(EnsureValueExpression(flattened, output, temps, argDest));
        }

        return new MethodInvocationParametersSyntax(
            parameters.ParenOpen,
            new CommaSeparatedSyntaxList<ExpressionSyntax>(elements),
            parameters.ParenClose);
    }

    private ExpressionSyntax EnsureArgument(ExpressionSyntax expression, List<StatementSyntax> output, TempSlotFrame temps, int dest)
    {
        expression = ExpressionParenthesizer.UnwrapParentheses(expression);

        if (expression is ValueExpressionSyntax or VariableExpressionSyntax or LiteralExpressionSyntax)
            return expression is ValueExpressionSyntax ? expression : new ValueExpressionSyntax(expression);

        if (IsEncodableNegativeFloat(expression))
            return new ValueExpressionSyntax(expression);

        return Spill(expression, output, temps, dest);
    }

    private ValueExpressionSyntax EnsureValueExpression(
        ExpressionSyntax expression,
        List<StatementSyntax> output,
        TempSlotFrame temps,
        int dest)
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

            return Spill(value.Value, output, temps, dest);
        }

        if (IsTrueLiteral(expression))
            return CreateIntOneValue();

        if (expression is VariableExpressionSyntax or LiteralExpressionSyntax)
            return new ValueExpressionSyntax(expression);

        if (IsEncodableNegativeFloat(expression))
            return new ValueExpressionSyntax(expression);

        return Spill(expression, output, temps, dest);
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

    private ValueExpressionSyntax Spill(ExpressionSyntax expression, List<StatementSyntax> output, TempSlotFrame temps, int dest)
    {
        dest = temps.Resolve(dest < 1 ? 1 : dest);
        ValueExpressionSyntax temp = CreateTemp(dest);
        output.Add(new AssignmentStatementSyntax(
            temp,
            syntaxFactory.Token(SyntaxTokenKind.EqualsSign),
            expression,
            syntaxFactory.Token(SyntaxTokenKind.Semicolon)));
        return temp;
    }

    private ValueExpressionSyntax CreateTemp(int dest)
    {
        return new ValueExpressionSyntax(new VariableExpressionSyntax(syntaxFactory.Variable("temp", (uint)dest)));
    }

    private static int FirstOperandDest(int dest, int reservedEnd)
    {
        return Math.Max(dest + 1, reservedEnd + 1);
    }

    /// <summary>
    /// A discarded call leaves dest 1 live. The first complex operand can overwrite it
    /// when it is on the left; a leaf left keeps dest 1, so a complex right starts at dest 2.
    /// </summary>
    private static int EffectivePackedReservedEnd(int dest, int reservedEnd, ExpressionSyntax leftExpr)
    {
        if (dest >= 1 || reservedEnd < 1)
            return reservedEnd;

        return NeedsTempOperand(leftExpr) ? 0 : reservedEnd;
    }

    private static bool NeedsTempOperand(ExpressionSyntax expression)
    {
        expression = ExpressionParenthesizer.UnwrapParentheses(expression);
        if (expression is ValueExpressionSyntax value)
            expression = ExpressionParenthesizer.UnwrapParentheses(value.Value);

        if (expression is VariableExpressionSyntax or LiteralExpressionSyntax)
            return false;

        if (IsTrueLiteral(expression) || IsEncodableNegativeFloat(expression))
            return false;

        return true;
    }

    private static int GetAssignmentDest(ExpressionSyntax left)
    {
        left = ExpressionParenthesizer.UnwrapParentheses(left);
        if (left is ValueExpressionSyntax value)
            left = value.Value;

        if (left is VariableExpressionSyntax variable && TryGetTempSlot(variable, out int slot))
            return slot;

        return 0;
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

    private sealed class NamedDestReserve
    {
        public int End { get; private set; }
        public bool Sticky { get; private set; }

        public void OnCall()
        {
            End = 1;
            Sticky = false;
        }

        public void OnVarCopy()
        {
            if (End > 0)
                Sticky = true;
        }

        public void OnConsumed(bool usedPackedTemps)
        {
            if (Sticky && !usedPackedTemps)
                return;

            Clear();
        }

        public void Clear()
        {
            End = 0;
            Sticky = false;
        }
    }

    private sealed record LoopContext(string HeadLabel, string ContinueLabel, string ExitLabel);
}
