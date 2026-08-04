using Logic.Domain.CodeAnalysis.Contract.Level5;
using Logic.Domain.CodeAnalysis.Contract.DataClasses;
using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;
using Logic.Domain.CodeAnalysis.DataClasses.Level5;

namespace Logic.Domain.CodeAnalysis.Level5;

internal class Level5ScriptWhitespaceNormalizer : ILevel5ScriptWhitespaceNormalizer
{
    public void NormalizeCodeUnit(CodeUnitSyntax codeUnit)
    {
        var ctx = new WhitespaceNormalizeContext();
        NormalizeCodeUnit(codeUnit, ctx);

        codeUnit.Update();
    }

    public void NormalizeMethodDeclaration(MethodDeclarationSyntax methodDeclaration)
    {
        var ctx = new WhitespaceNormalizeContext();
        NormalizeMethodDeclaration(methodDeclaration, ctx);

        methodDeclaration.Update();
    }

    public void NormalizeMethodDeclarationMetadataParameters(
        MethodDeclarationMetadataParametersSyntax methodDeclarationMetadataParameters)
    {
        var ctx = new WhitespaceNormalizeContext();
        NormalizeMethodDeclarationMetadataParameters(methodDeclarationMetadataParameters, ctx);

        methodDeclarationMetadataParameters.Update();
    }

    public void NormalizeMethodDeclarationMetadataParameterList(
        MethodDeclarationMetadataParameterListSyntax methodDeclarationMetadataParameterList)
    {
        var ctx = new WhitespaceNormalizeContext();
        NormalizeMethodDeclarationMetadataParameterList(methodDeclarationMetadataParameterList, ctx);

        methodDeclarationMetadataParameterList.Update();
    }

    public void NormalizeMethodDeclarationParameters(MethodDeclarationParametersSyntax methodDeclarationParameters)
    {
        var ctx = new WhitespaceNormalizeContext();
        NormalizeMethodDeclarationParameters(methodDeclarationParameters, ctx);

        methodDeclarationParameters.Update();
    }

    public void NormalizeMethodDeclarationBody(MethodDeclarationBodySyntax methodDeclarationBody)
    {
        var ctx = new WhitespaceNormalizeContext();
        NormalizeMethodDeclarationBody(methodDeclarationBody, ctx);

        methodDeclarationBody.Update();
    }

    public void NormalizeGotoLabelStatement(GotoLabelStatementSyntax gotoLabelStatement)
    {
        var ctx = new WhitespaceNormalizeContext();
        NormalizeGotoLabelStatement(gotoLabelStatement, ctx);

        gotoLabelStatement.Update();
    }

    public void NormalizeReturnStatement(ReturnStatementSyntax returnStatement)
    {
        var ctx = new WhitespaceNormalizeContext();
        NormalizeReturnStatement(returnStatement, ctx);

        returnStatement.Update();
    }

    public void NormalizeMethodInvocationExpression(MethodInvocationExpressionSyntax invocation)
    {
        var ctx = new WhitespaceNormalizeContext();
        NormalizeMethodInvocationExpression(invocation, ctx);

        invocation.Update();
    }

    public void NormalizeMethodInvocationStatement(MethodInvocationStatementSyntax invocation)
    {
        var ctx = new WhitespaceNormalizeContext();
        NormalizeMethodInvocationStatement(invocation, ctx);

        invocation.Update();
    }

    public void NormalizeMethodInvocationParameters(MethodInvocationParametersSyntax invocationParameters)
    {
        var ctx = new WhitespaceNormalizeContext();
        NormalizeMethodInvocationParameters(invocationParameters, ctx);

        invocationParameters.Update();
    }

    public void NormalizeValueList(CommaSeparatedSyntaxList<ValueExpressionSyntax> valueList)
    {
        var ctx = new WhitespaceNormalizeContext();
        NormalizeValueExpressions(valueList, ctx);

        valueList.Update();
    }

    public void NormalizeValue(ValueExpressionSyntax valueExpression)
    {
        var ctx = new WhitespaceNormalizeContext();

        ctx.IsFirstElement = true;
        NormalizeValueExpression(valueExpression, ctx);

        valueExpression.Update();
    }

    public void NormalizeValueMetadataParameters(ValueMetadataParametersSyntax valueMetadataParameters)
    {
        var ctx = new WhitespaceNormalizeContext();
        NormalizeValueMetadataParameters(valueMetadataParameters, ctx);

        valueMetadataParameters.Update();
    }


    private void NormalizeCodeUnit(CodeUnitSyntax codeUnit, WhitespaceNormalizeContext ctx)
    {
        foreach (CodeUnitMemberSyntax member in codeUnit.Members)
        {
            ctx.IsFirstElement = codeUnit.Members[0] == member;
            ctx.ShouldLineBreak = codeUnit.Members[^1] != member;
            NormalizeCodeUnitMember(member, ctx);
        }
    }

    private void NormalizeCodeUnitMember(CodeUnitMemberSyntax member, WhitespaceNormalizeContext ctx)
    {
        switch (member)
        {
            case MethodDeclarationSyntax methodDeclaration:
                NormalizeMethodDeclaration(methodDeclaration, ctx);
                break;

            case GlobalDeclarationStatementSyntax globalDeclaration:
                NormalizeGlobalDeclarationStatement(globalDeclaration, ctx);
                break;
        }
    }

    private void NormalizeGlobalDeclarationStatement(
        GlobalDeclarationStatementSyntax globalDeclaration,
        WhitespaceNormalizeContext ctx)
    {
        SyntaxToken globalKeyword = globalDeclaration.GlobalKeyword.WithNoTrivia().WithTrailingTrivia(" ");
        SyntaxToken semicolon = globalDeclaration.Semicolon.WithNoTrivia();

        if (ctx.ShouldLineBreak)
            semicolon = semicolon.WithTrailingTrivia("\r\n\r\n");
        else
            semicolon = semicolon.WithTrailingTrivia("\r\n");

        globalDeclaration.SetGlobalKeyword(globalKeyword, false);
        NormalizeGlobalDeclarationVariableList(globalDeclaration.Variables, ctx);
        globalDeclaration.SetSemicolon(semicolon, false);
    }

    private void NormalizeGlobalDeclarationVariableList(
        CommaSeparatedSyntaxList<VariableExpressionSyntax> list,
        WhitespaceNormalizeContext ctx)
    {
        foreach (VariableExpressionSyntax value in list.Elements)
        {
            ctx.IsFirstElement = list.Elements[0] == value;
            ctx.ShouldLineBreak = false;
            ctx.ShouldIndent = false;
            NormalizeVariableExpression(value, ctx);
        }
    }

    private void NormalizeMethodDeclaration(MethodDeclarationSyntax methodDeclaration, WhitespaceNormalizeContext ctx)
    {
        bool shouldLineBreak = ctx.ShouldLineBreak;

        SyntaxToken newIdentifier = methodDeclaration.Identifier.WithNoTrivia();

        methodDeclaration.SetIdentifier(newIdentifier, false);
        NormalizeMethodDeclarationMetadataParameters(methodDeclaration.MetadataParameters, ctx);

        ctx.ShouldLineBreak = true;
        NormalizeMethodDeclarationParameters(methodDeclaration.Parameters, ctx);

        ctx.ShouldLineBreak = shouldLineBreak;
        NormalizeMethodDeclarationBody(methodDeclaration.Body, ctx);
    }

    private void NormalizeMethodDeclarationMetadataParameters(
        MethodDeclarationMetadataParametersSyntax? methodDeclarationMetadataParameters, WhitespaceNormalizeContext ctx)
    {
        if (methodDeclarationMetadataParameters == null)
            return;

        SyntaxToken newRelSmaller = methodDeclarationMetadataParameters.RelSmaller.WithNoTrivia();
        SyntaxToken newRelBigger = methodDeclarationMetadataParameters.RelBigger.WithLeadingTrivia(null).WithLeadingTrivia(null);

        methodDeclarationMetadataParameters.SetRelSmaller(newRelSmaller, false);
        NormalizeMethodDeclarationMetadataParameterList(methodDeclarationMetadataParameters.List, ctx);
        methodDeclarationMetadataParameters.SetRelBigger(newRelBigger, false);
    }

    private void NormalizeMethodDeclarationMetadataParameterList(
        MethodDeclarationMetadataParameterListSyntax methodDeclarationMetadataParameterList, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken newComma = methodDeclarationMetadataParameterList.Comma.WithNoTrivia();

        ctx.IsFirstElement = true;
        ctx.ShouldLineBreak = false;
        NormalizeLiteralExpression(methodDeclarationMetadataParameterList.Parameter1, ctx);
        methodDeclarationMetadataParameterList.SetComma(newComma, false);
        ctx.IsFirstElement = false;
        NormalizeLiteralExpression(methodDeclarationMetadataParameterList.Parameter2, ctx);
    }

    private void NormalizeMethodDeclarationParameters(MethodDeclarationParametersSyntax methodDeclarationParameters, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken newParenOpen = methodDeclarationParameters.ParenOpen.WithLeadingTrivia(null).WithLeadingTrivia(null);
        SyntaxToken newParenClose = methodDeclarationParameters.ParenClose.WithLeadingTrivia(null).WithLeadingTrivia(null);

        if (ctx.ShouldLineBreak)
            newParenClose = newParenClose.WithTrailingTrivia("\r\n");

        methodDeclarationParameters.SetParenOpen(newParenOpen, false);
        NormalizeMethodDeclarationParameterList(methodDeclarationParameters.Parameters, ctx);
        methodDeclarationParameters.SetParenClose(newParenClose, false);
    }

    private void NormalizeMethodDeclarationParameterList(CommaSeparatedSyntaxList<VariableExpressionSyntax>? list,
        WhitespaceNormalizeContext ctx)
    {
        if (list == null)
            return;

        foreach (VariableExpressionSyntax value in list.Elements)
        {
            ctx.IsFirstElement = list.Elements[0] == value;
            ctx.ShouldLineBreak = false;
            NormalizeVariableExpression(value, ctx);
        }
    }

    private void NormalizeMethodDeclarationBody(MethodDeclarationBodySyntax methodDeclarationBody, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken newCurlyOpen = methodDeclarationBody.CurlyOpen.WithLeadingTrivia(null).WithTrailingTrivia("\r\n");
        SyntaxToken newCurlyClose = methodDeclarationBody.CurlyClose.WithNoTrivia();

        if (ctx.ShouldLineBreak)
            newCurlyClose = newCurlyClose.WithTrailingTrivia("\r\n\r\n");

        methodDeclarationBody.SetCurlyOpen(newCurlyOpen, false);
        methodDeclarationBody.SetCurlyClose(newCurlyClose, false);

        ctx.Indent++;
        foreach (StatementSyntax expression in methodDeclarationBody.Expressions)
        {
            ctx.IsFirstElement = methodDeclarationBody.Expressions[0] == expression;
            ctx.ShouldLineBreak = true;
            ctx.ShouldIndent = true;

            NormalizeStatement(expression, ctx);
        }
    }

    private void NormalizeStatement(StatementSyntax statement, WhitespaceNormalizeContext ctx)
    {
        switch (statement)
        {
            case GotoLabelStatementSyntax gotoStatement:
                NormalizeGotoLabelStatement(gotoStatement, ctx);
                break;

            case YieldStatementSyntax yieldStatement:
                NormalizeYieldStatement(yieldStatement, ctx);
                break;

            case ReturnStatementSyntax returnStatement:
                NormalizeReturnStatement(returnStatement, ctx);
                break;

            case ExitStatementSyntax exitStatement:
                NormalizeExitStatement(exitStatement, ctx);
                break;

            case IfGotoStatementSyntax ifGotoStatement:
                NormalizeIfGotoStatement(ifGotoStatement, ctx);
                break;

            case GotoStatementSyntax gotoStatement:
                ctx.IsFirstElement = true;
                NormalizeGotoStatement(gotoStatement, ctx);
                break;

            case IfNotGotoStatementSyntax ifNotGotoStatement:
                NormalizeIfNotGotoStatement(ifNotGotoStatement, ctx);
                break;

            case IfStatementSyntax ifStatement:
                NormalizeIfStatement(ifStatement, ctx);
                break;

            case WhileStatementSyntax whileStatement:
                NormalizeWhileStatement(whileStatement, ctx);
                break;

            case ForStatementSyntax forStatement:
                NormalizeForStatement(forStatement, ctx);
                break;

            case DoWhileStatementSyntax doWhileStatement:
                NormalizeDoWhileStatement(doWhileStatement, ctx);
                break;

            case BreakStatementSyntax breakStatement:
                NormalizeBreakStatement(breakStatement, ctx);
                break;

            case ContinueStatementSyntax continueStatement:
                NormalizeContinueStatement(continueStatement, ctx);
                break;

            case BlockSyntax block:
                NormalizeBlock(block, ctx);
                break;

            case PostfixUnaryStatementSyntax postfixUnaryStatement:
                NormalizePostfixUnaryStatement(postfixUnaryStatement, ctx);
                break;

            case AssignmentStatementSyntax assignmentStatement:
                NormalizeAssignmentStatement(assignmentStatement, ctx);
                break;

            case MethodInvocationStatementSyntax methodInvocationStatement:
                NormalizeMethodInvocationStatement(methodInvocationStatement, ctx);
                break;
        }
    }

    private void NormalizePostfixUnaryStatement(PostfixUnaryStatementSyntax postfixUnaryStatement, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken semicolon = postfixUnaryStatement.Semicolon.WithNoTrivia();

        if (ctx.ShouldLineBreak)
            semicolon = semicolon.WithTrailingTrivia("\r\n");

        ctx.IsFirstElement = true;
        ctx.ShouldLineBreak = false;
        NormalizeExpression(postfixUnaryStatement.Expression, ctx);

        postfixUnaryStatement.SetSemicolon(semicolon, false);
    }

    private void NormalizeIfGotoStatement(IfGotoStatementSyntax ifGotoStatement, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken newIf = ifGotoStatement.If.WithTrailingTrivia(null).WithTrailingTrivia(" ");
        SyntaxToken newSemicolon = ifGotoStatement.Semicolon.WithNoTrivia();

        if (ctx is { ShouldIndent: true, Indent: > 0 })
            newIf = newIf.WithLeadingTrivia(new string('\t', ctx.Indent));

        if (ctx.ShouldLineBreak)
            newSemicolon = newSemicolon.WithTrailingTrivia("\r\n");

        ctx.ShouldIndent = false;
        ctx.ShouldLineBreak = false;
        ctx.IsFirstElement = true;
        NormalizeExpression(ifGotoStatement.Value, ctx);
        NormalizeGotoExpression(ifGotoStatement.Goto, ctx);

        ifGotoStatement.SetIf(newIf, false);
        ifGotoStatement.SetSemicolon(newSemicolon, false);
    }

    private void NormalizeIfNotGotoStatement(IfNotGotoStatementSyntax ifNotGotoStatement, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken newIf = ifNotGotoStatement.If.WithTrailingTrivia(null).WithTrailingTrivia(" ");
        SyntaxToken newSemicolon = ifNotGotoStatement.Semicolon.WithNoTrivia();

        if (ctx is { ShouldIndent: true, Indent: > 0 })
            newIf = newIf.WithLeadingTrivia(new string('\t', ctx.Indent));

        if (ctx.ShouldLineBreak)
            newSemicolon = newSemicolon.WithTrailingTrivia("\r\n");

        ctx.ShouldIndent = false;
        ctx.ShouldLineBreak = false;
        ctx.IsFirstElement = true;
        NormalizeExpression(ifNotGotoStatement.Comparison, ctx);
        NormalizeGotoExpression(ifNotGotoStatement.Goto, ctx);

        ifNotGotoStatement.SetIf(newIf, false);
        ifNotGotoStatement.SetSemicolon(newSemicolon, false);
    }

    private void NormalizeIfStatement(IfStatementSyntax ifStatement, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken newIf = ifStatement.If.WithNoTrivia().WithTrailingTrivia(" ");

        if (ctx is { ShouldIndent: true, Indent: > 0 })
            newIf = newIf.WithLeadingTrivia(new string('\t', ctx.Indent));

        ctx.ShouldIndent = false;
        ctx.ShouldLineBreak = false;
        ctx.IsFirstElement = true;
        NormalizeExpression(ifStatement.Condition, ctx);

        bool hasElse = ifStatement.Else != null;
        NormalizeBlock(ifStatement.Body, ctx, attachElseOnSameLine: hasElse);

        if (ifStatement.Else != null)
            NormalizeElseClause(ifStatement.Else, ctx);

        ifStatement.SetIf(newIf, false);
    }

    private void NormalizeWhileStatement(WhileStatementSyntax whileStatement, WhitespaceNormalizeContext ctx)
    {
        bool shouldLineBreak = ctx.ShouldLineBreak;
        SyntaxToken newWhile = whileStatement.While.WithNoTrivia();
        if (ctx is { ShouldIndent: true, Indent: > 0 })
            newWhile = newWhile.WithLeadingTrivia(new string('\t', ctx.Indent));

        SyntaxToken parenOpen = whileStatement.ParenOpen.WithNoTrivia().WithLeadingTrivia(" ");
        SyntaxToken parenClose = whileStatement.ParenClose.WithNoTrivia();

        ctx.ShouldIndent = false;
        ctx.ShouldLineBreak = false;
        ctx.IsFirstElement = true;
        NormalizeExpression(whileStatement.Condition, ctx);

        if (whileStatement.Body != null)
        {
            NormalizeBlock(whileStatement.Body, ctx);
            whileStatement.SetSemicolon(null, false);
        }
        else
        {
            SyntaxToken semicolon = whileStatement.Semicolon!.Value.WithNoTrivia();
            if (shouldLineBreak)
                semicolon = semicolon.WithTrailingTrivia("\r\n");
            whileStatement.SetSemicolon(semicolon, false);
            ctx.ShouldLineBreak = true;
        }

        whileStatement.SetWhile(newWhile, false);
        whileStatement.SetParenOpen(parenOpen, false);
        whileStatement.SetParenClose(parenClose, false);
    }

    private void NormalizeForStatement(ForStatementSyntax forStatement, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken newFor = forStatement.For.WithNoTrivia();
        if (ctx is { ShouldIndent: true, Indent: > 0 })
            newFor = newFor.WithLeadingTrivia(new string('\t', ctx.Indent));

        SyntaxToken parenOpen = forStatement.ParenOpen.WithNoTrivia().WithLeadingTrivia(" ");
        SyntaxToken parenClose = forStatement.ParenClose.WithNoTrivia();
        SyntaxToken secondSemicolon = forStatement.SecondSemicolon.WithNoTrivia();

        ctx.ShouldIndent = false;
        ctx.ShouldLineBreak = false;
        ctx.IsFirstElement = true;

        if (forStatement.Initializer != null)
        {
            NormalizeForClauseStatement(forStatement.Initializer, ctx, trailingSpace: true);
        }
        else if (forStatement.FirstSemicolon != null)
        {
            forStatement.SetFirstSemicolon(
                forStatement.FirstSemicolon.Value.WithNoTrivia().WithTrailingTrivia(" "), false);
        }

        NormalizeExpression(forStatement.Condition, ctx);
        forStatement.SetSecondSemicolon(secondSemicolon.WithTrailingTrivia(" "), false);

        if (forStatement.Iterator != null)
            NormalizeForClauseStatement(forStatement.Iterator, ctx, trailingSpace: false);

        NormalizeBlock(forStatement.Body, ctx);

        forStatement.SetFor(newFor, false);
        forStatement.SetParenOpen(parenOpen, false);
        forStatement.SetParenClose(parenClose, false);
    }

    private void NormalizeForClauseStatement(StatementSyntax statement, WhitespaceNormalizeContext ctx, bool trailingSpace)
    {
        switch (statement)
        {
            case AssignmentStatementSyntax assignment:
            {
                SyntaxToken equals = assignment.EqualsOperator.WithNoTrivia()
                    .WithLeadingTrivia(" ")
                    .WithTrailingTrivia(" ");
                SyntaxToken semicolon = assignment.Semicolon.WithNoTrivia();
                if (trailingSpace)
                    semicolon = semicolon.WithTrailingTrivia(" ");

                ctx.IsFirstElement = true;
                NormalizeExpression(assignment.Left, ctx);
                assignment.SetEqualsOperator(equals, false);
                NormalizeExpression(assignment.Right, ctx);
                assignment.SetSemicolon(semicolon, false);
                break;
            }

            case PostfixUnaryStatementSyntax postfix:
            {
                SyntaxToken semicolon = postfix.Semicolon.WithNoTrivia();
                if (trailingSpace)
                    semicolon = semicolon.WithTrailingTrivia(" ");

                ctx.IsFirstElement = true;
                NormalizeExpression(postfix.Expression, ctx);
                postfix.SetSemicolon(semicolon, false);
                break;
            }

            default:
                NormalizeStatement(statement, ctx);
                break;
        }
    }

    private void NormalizeDoWhileStatement(DoWhileStatementSyntax doWhileStatement, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken newDo = doWhileStatement.Do.WithNoTrivia();
        if (ctx is { ShouldIndent: true, Indent: > 0 })
            newDo = newDo.WithLeadingTrivia(new string('\t', ctx.Indent));

        NormalizeBlock(doWhileStatement.Body, ctx, attachElseOnSameLine: true);

        SyntaxToken newWhile = doWhileStatement.While.WithNoTrivia().WithLeadingTrivia(" ").WithTrailingTrivia(" ");
        SyntaxToken parenOpen = doWhileStatement.ParenOpen.WithNoTrivia();
        SyntaxToken parenClose = doWhileStatement.ParenClose.WithNoTrivia();
        SyntaxToken semicolon = doWhileStatement.Semicolon.WithNoTrivia().WithTrailingTrivia("\r\n");

        ctx.ShouldIndent = false;
        ctx.ShouldLineBreak = false;
        ctx.IsFirstElement = true;
        NormalizeExpression(doWhileStatement.Condition, ctx);

        doWhileStatement.SetDo(newDo, false);
        doWhileStatement.SetWhile(newWhile, false);
        doWhileStatement.SetParenOpen(parenOpen, false);
        doWhileStatement.SetParenClose(parenClose, false);
        doWhileStatement.SetSemicolon(semicolon, false);
        ctx.ShouldLineBreak = true;
    }

    private void NormalizeBreakStatement(BreakStatementSyntax breakStatement, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken newBreak = breakStatement.Break.WithNoTrivia();
        SyntaxToken semicolon = breakStatement.Semicolon.WithNoTrivia();

        if (ctx is { ShouldIndent: true, Indent: > 0 })
            newBreak = newBreak.WithLeadingTrivia(new string('\t', ctx.Indent));

        if (ctx.ShouldLineBreak)
            semicolon = semicolon.WithTrailingTrivia("\r\n");

        breakStatement.SetBreak(newBreak, false);
        breakStatement.SetSemicolon(semicolon, false);
    }

    private void NormalizeContinueStatement(ContinueStatementSyntax continueStatement, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken newContinue = continueStatement.Continue.WithNoTrivia();
        SyntaxToken semicolon = continueStatement.Semicolon.WithNoTrivia();

        if (ctx is { ShouldIndent: true, Indent: > 0 })
            newContinue = newContinue.WithLeadingTrivia(new string('\t', ctx.Indent));

        if (ctx.ShouldLineBreak)
            semicolon = semicolon.WithTrailingTrivia("\r\n");

        continueStatement.SetContinue(newContinue, false);
        continueStatement.SetSemicolon(semicolon, false);
    }

    private void NormalizeElseClause(ElseClauseSyntax elseClause, WhitespaceNormalizeContext ctx)
    {
        bool isElseIf = elseClause.Statement is IfStatementSyntax;
        SyntaxToken newElse = elseClause.ElseKeyword.WithNoTrivia().WithLeadingTrivia(" ");

        // else-if needs a space before `if`. A plain else block already gets a leading
        // space on `{` from NormalizeBlock — do not add a trailing space here or you get
        // "} else  {".
        if (isElseIf)
            newElse = newElse.WithTrailingTrivia(" ");

        ctx.ShouldIndent = false;
        ctx.ShouldLineBreak = false;
        ctx.IsFirstElement = true;

        if (elseClause.Statement is IfStatementSyntax elseIf)
        {
            // Keep "} else if cond {" on one line — if keyword has no leading indent.
            SyntaxToken nestedIf = elseIf.If.WithNoTrivia().WithTrailingTrivia(" ");
            ctx.ShouldIndent = false;
            NormalizeExpression(elseIf.Condition, ctx);
            bool hasElse = elseIf.Else != null;
            NormalizeBlock(elseIf.Body, ctx, attachElseOnSameLine: hasElse);
            if (elseIf.Else != null)
                NormalizeElseClause(elseIf.Else, ctx);
            elseIf.SetIf(nestedIf, false);
        }
        else if (elseClause.Statement is BlockSyntax block)
        {
            NormalizeBlock(block, ctx, attachElseOnSameLine: false);
        }
        else
        {
            NormalizeStatement(elseClause.Statement, ctx);
        }

        elseClause.SetElseKeyword(newElse, false);
    }

    private void NormalizeBlock(BlockSyntax block, WhitespaceNormalizeContext ctx, bool attachElseOnSameLine = false)
    {
        SyntaxToken newCurlyOpen = block.CurlyOpen.WithNoTrivia().WithLeadingTrivia(" ").WithTrailingTrivia("\r\n");
        SyntaxToken newCurlyClose = block.CurlyClose.WithNoTrivia();

        if (ctx is { Indent: > 0 })
            newCurlyClose = newCurlyClose.WithLeadingTrivia(new string('\t', ctx.Indent));

        if (attachElseOnSameLine)
            newCurlyClose = newCurlyClose.WithTrailingTrivia(null);
        else
            newCurlyClose = newCurlyClose.WithTrailingTrivia("\r\n");

        int previousIndent = ctx.Indent;
        ctx.Indent++;
        foreach (StatementSyntax statement in block.Statements)
        {
            ctx.IsFirstElement = true;
            ctx.ShouldLineBreak = true;
            ctx.ShouldIndent = true;
            NormalizeStatement(statement, ctx);
        }
        ctx.Indent = previousIndent;

        // Closing brace should force a line break for the next top-level statement
        // unless an else clause continues on the same line.
        if (!attachElseOnSameLine)
            ctx.ShouldLineBreak = true;

        block.SetCurlyOpen(newCurlyOpen, false);
        block.SetCurlyClose(newCurlyClose, false);
        block.SetStatements(block.Statements, false);
    }

    private void NormalizeGotoStatement(GotoStatementSyntax gotoStatement, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken newGoto = gotoStatement.Goto.WithLeadingTrivia(null).WithTrailingTrivia(" ");
        SyntaxToken newSemicolon = gotoStatement.Semicolon.WithNoTrivia();

        string? leadingTrivia = null;
        if (ctx is { ShouldIndent: true, Indent: > 0 })
            leadingTrivia = new string('\t', ctx.Indent);
        if (!ctx.IsFirstElement)
            leadingTrivia = " ";
        newGoto = newGoto.WithLeadingTrivia(leadingTrivia);

        if (ctx.ShouldLineBreak)
            newSemicolon = newSemicolon.WithTrailingTrivia("\r\n");

        ctx.ShouldIndent = false;
        ctx.ShouldLineBreak = false;
        ctx.IsFirstElement = true;
        NormalizeValueExpressions(gotoStatement.Targets, ctx);

        gotoStatement.SetGoto(newGoto, false);
        gotoStatement.SetSemicolon(newSemicolon, false);
    }

    private void NormalizeGotoExpression(GotoExpressionSyntax gotoStatement, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken newGoto = gotoStatement.Goto.WithLeadingTrivia(null).WithTrailingTrivia(" ");

        if (ctx.IsFirstElement)
            newGoto = newGoto.WithLeadingTrivia(" ");

        ctx.ShouldIndent = false;
        ctx.ShouldLineBreak = false;
        ctx.IsFirstElement = true;
        NormalizeValueExpression(gotoStatement.Target, ctx);

        gotoStatement.SetGoto(newGoto, false);
    }

    private void NormalizeGotoLabelStatement(GotoLabelStatementSyntax gotoLabelStatement, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken newLiteral = gotoLabelStatement.Label.Literal.WithNoTrivia();
        SyntaxToken newColon = gotoLabelStatement.Colon.WithNoTrivia();

        int indent = ctx.Indent - 1;
        if (ctx.ShouldIndent && indent > 0)
            newLiteral = newLiteral.WithLeadingTrivia(new string('\t', indent));

        if (ctx.ShouldLineBreak)
            newColon = newColon.WithTrailingTrivia("\r\n");

        gotoLabelStatement.Label.SetLiteral(newLiteral, false);
        gotoLabelStatement.SetColon(newColon, false);
    }

    private void NormalizeYieldStatement(YieldStatementSyntax yieldStatement, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken newYieldKeyword = yieldStatement.Yield.WithNoTrivia();
        SyntaxToken newSemicolon = yieldStatement.Semicolon.WithNoTrivia();

        if (ctx is { ShouldIndent: true, Indent: > 0 })
            newYieldKeyword = newYieldKeyword.WithLeadingTrivia(new string('\t', ctx.Indent));

        if (ctx.ShouldLineBreak)
            newSemicolon = newSemicolon.WithTrailingTrivia("\r\n");

        yieldStatement.SetYield(newYieldKeyword, false);
        yieldStatement.SetSemicolon(newSemicolon, false);
    }

    private void NormalizeReturnStatement(ReturnStatementSyntax returnStatement, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken newReturnKeyword = returnStatement.Return.WithNoTrivia();
        SyntaxToken newSemicolon = returnStatement.Semicolon.WithNoTrivia();

        if (returnStatement.ValueExpression != null)
            newReturnKeyword = newReturnKeyword.WithTrailingTrivia(" ");

        if (ctx is { ShouldIndent: true, Indent: > 0 })
            newReturnKeyword = newReturnKeyword.WithLeadingTrivia(new string('\t', ctx.Indent));

        if (ctx.ShouldLineBreak)
            newSemicolon = newSemicolon.WithTrailingTrivia("\r\n");

        returnStatement.SetReturn(newReturnKeyword, false);
        returnStatement.SetSemicolon(newSemicolon, false);

        if (returnStatement.ValueExpression == null)
            return;

        ctx.IsFirstElement = true;
        ctx.ShouldIndent = false;
        ctx.ShouldLineBreak = false;
        NormalizeExpression(returnStatement.ValueExpression, ctx);
    }

    private void NormalizeExitStatement(ExitStatementSyntax exitStatement, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken newExitKeyword = exitStatement.Exit.WithLeadingTrivia(null).WithTrailingTrivia(" ");
        SyntaxToken newSemicolon = exitStatement.Semicolon.WithNoTrivia();

        if (ctx is { ShouldIndent: true, Indent: > 0 })
            newExitKeyword = newExitKeyword.WithLeadingTrivia(new string('\t', ctx.Indent));

        if (ctx.ShouldLineBreak)
            newSemicolon = newSemicolon.WithTrailingTrivia("\r\n");

        exitStatement.SetExit(newExitKeyword, false);
        exitStatement.SetSemicolon(newSemicolon, false);
    }

    private void NormalizeAssignmentStatement(AssignmentStatementSyntax assignmentStatement,
        WhitespaceNormalizeContext ctx)
    {
        SyntaxToken newEqualsOperator = assignmentStatement.EqualsOperator.WithLeadingTrivia(" ").WithTrailingTrivia(" ");
        SyntaxToken newSemicolon = assignmentStatement.Semicolon.WithNoTrivia();

        if (ctx.ShouldLineBreak)
            newSemicolon = newSemicolon.WithTrailingTrivia("\r\n");

        ctx.ShouldIndent = true;
        ctx.ShouldLineBreak = false;
        switch (assignmentStatement.Left)
        {
            case ValueExpressionSyntax leftValue:
                ctx.IsFirstElement = true;
                NormalizeValueExpression(leftValue, ctx);
                break;

            case ArrayIndexExpressionSyntax leftArrayIndexExpression:
                ctx.IsFirstElement = true;
                NormalizeArrayIndexExpression(leftArrayIndexExpression, ctx);
                break;
        }

        ctx.ShouldIndent = false;
        NormalizeExpression(assignmentStatement.Right, ctx);

        assignmentStatement.SetEqualsOperator(newEqualsOperator, false);
        assignmentStatement.SetSemicolon(newSemicolon, false);
    }

    private void NormalizeExpression(ExpressionSyntax expression, WhitespaceNormalizeContext ctx)
    {
        switch (expression)
        {
            case TypeCastValueExpressionSyntax typeCastValueExpression:
                NormalizeTypeCastValueExpression(typeCastValueExpression, ctx);
                break;

            case ParenthesizedExpressionSyntax parenthesizedExpression:
                NormalizeParenthesizedExpression(parenthesizedExpression, ctx);
                break;

            case PostfixUnaryExpressionSyntax postfixUnaryExpression:
                NormalizePostfixUnaryExpression(postfixUnaryExpression, ctx);
                break;

            case SwitchExpressionSyntax switchExpression:
                NormalizeSwitchExpression(switchExpression, ctx);
                break;

            case LogicalExpressionSyntax logicalExpression:
                NormalizeLogicalExpression(logicalExpression, ctx);
                break;

            case UnaryExpressionSyntax rightUnaryExpression:
                NormalizeUnaryExpression(rightUnaryExpression, ctx);
                break;

            case BinaryExpressionSyntax rightBinaryExpression:
                NormalizeBinaryExpression(rightBinaryExpression, ctx);
                break;

            case AssignmentExpressionSyntax assignmentExpression:
                NormalizeAssignmentExpression(assignmentExpression, ctx);
                break;

            case ArrayInstantiationExpressionSyntax rightArrayInstantiation:
                NormalizeArrayInstantiationExpression(rightArrayInstantiation, ctx);
                break;

            case ArrayIndexExpressionSyntax rightArrayIndex:
                NormalizeArrayIndexExpression(rightArrayIndex, ctx);
                break;

            case MethodInvocationExpressionSyntax rightMethodInvocation:
                NormalizeMethodInvocationExpression(rightMethodInvocation, ctx);
                break;

            case ValueExpressionSyntax rightValueExpression:
                NormalizeValueExpression(rightValueExpression, ctx);
                break;

            case VariableExpressionSyntax variableExpression:
                NormalizeVariableExpression(variableExpression, ctx);
                break;

            case LiteralExpressionSyntax literalExpression:
                NormalizeLiteralExpression(literalExpression, ctx);
                break;

            default:
                throw new InvalidOperationException("Unknown expression.");
        }
    }

    private void NormalizeTypeCastValueExpression(TypeCastValueExpressionSyntax typeCastValueExpression, WhitespaceNormalizeContext ctx)
    {
        // Same list-separator spacing as other unaries (`foo($a, (float)$b)`).
        SyntaxToken parenOpen = typeCastValueExpression.TypeCast.ParenOpen.WithNoTrivia();
        if (!ctx.IsFirstElement)
            parenOpen = parenOpen.WithLeadingTrivia(" ");

        SyntaxToken typeKeyword = typeCastValueExpression.TypeCast.TypeKeyword.WithNoTrivia();
        SyntaxToken parenClose = typeCastValueExpression.TypeCast.ParenClose.WithNoTrivia();
        typeCastValueExpression.TypeCast.SetParenOpen(parenOpen, false);
        typeCastValueExpression.TypeCast.SetType(typeKeyword, false);
        typeCastValueExpression.TypeCast.SetParenClose(parenClose, false);

        ctx.IsFirstElement = true;
        ctx.ShouldIndent = false;
        ctx.ShouldLineBreak = false;
        NormalizeValueExpression(typeCastValueExpression.Value, ctx);
    }

    private void NormalizeParenthesizedExpression(ParenthesizedExpressionSyntax parenthesizedExpression, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken parenOpen = parenthesizedExpression.ParenOpen.WithNoTrivia();
        SyntaxToken parenClose = parenthesizedExpression.ParenClose.WithNoTrivia();

        // List args: `foo(a, (b + c))`. Unary `not(...)` keeps IsFirstElement true after the operator.
        if (!ctx.IsFirstElement)
            parenOpen = parenOpen.WithLeadingTrivia(" ");

        ctx.IsFirstElement = true;
        ctx.ShouldIndent = false;
        ctx.ShouldLineBreak = false;
        NormalizeExpression(parenthesizedExpression.Expression, ctx);

        parenthesizedExpression.SetParenOpen(parenOpen, false);
        parenthesizedExpression.SetParenClose(parenClose, false);
    }

    private void NormalizePostfixUnaryExpression(PostfixUnaryExpressionSyntax postfixUnaryExpression, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken operation = postfixUnaryExpression.Operation.WithNoTrivia();

        NormalizeExpression(postfixUnaryExpression.Value, ctx);

        postfixUnaryExpression.SetOperation(operation, false);
    }

    private void NormalizeSwitchExpression(SwitchExpressionSyntax switchExpression, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken switchToken = switchExpression.Switch.WithLeadingTrivia(" ").WithTrailingTrivia(null);

        NormalizeExpression(switchExpression.Value, ctx);
        NormalizeSwitchBlockExpression(switchExpression.CaseBlock, ctx);

        switchExpression.SetSwitch(switchToken, false);
    }

    private void NormalizeSwitchBlockExpression(SwitchBlockExpressionSyntax caseBlockExpression, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken curlyOpen = caseBlockExpression.CurlyOpen;
        SyntaxToken curlyClose = caseBlockExpression.CurlyClose;

        curlyOpen = curlyOpen.WithLeadingTrivia("\r\n" + new string('\t', ctx.Indent)).WithTrailingTrivia("\r\n");
        curlyClose = curlyClose.WithLeadingTrivia("\r\n" + new string('\t', ctx.Indent)).WithTrailingTrivia(null);

        NormalizeSwitchCaseExpressions(caseBlockExpression.Cases, ctx);

        caseBlockExpression.SetCurlyOpen(curlyOpen, false);
        caseBlockExpression.SetCurlyClose(curlyClose, false);
    }

    private void NormalizeSwitchCaseExpressions(IReadOnlyList<SwitchCaseExpressionSyntax> caseExpressions, WhitespaceNormalizeContext ctx)
    {
        ctx.Indent++;

        ctx.ShouldLineBreak = true;
        for (var i = 0; i < caseExpressions.Count - 1; i++)
            NormalizeSwitchCaseExpression(caseExpressions[i], ctx);

        ctx.ShouldLineBreak = false;
        NormalizeSwitchCaseExpression(caseExpressions[^1], ctx);
    }

    private void NormalizeSwitchCaseExpression(SwitchCaseExpressionSyntax caseExpression, WhitespaceNormalizeContext ctx)
    {
        ctx.ShouldIndent = true;

        switch (caseExpression)
        {
            case DefaultSwitchCaseExpressionSyntax defaultCase:
                NormalizeDefaultSwitchCaseExpression(defaultCase, ctx);
                break;

            case LiteralSwitchCaseExpressionSyntax literalCase:
                NormalizeLiteralSwitchCaseExpression(literalCase, ctx);
                break;
        }
    }

    private void NormalizeDefaultSwitchCaseExpression(DefaultSwitchCaseExpressionSyntax defaultCase, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken underscore = defaultCase.Underscore.WithNoTrivia();
        SyntaxToken arrowRight = defaultCase.ArrowRight.WithLeadingTrivia(" ").WithTrailingTrivia(" ");

        if (ctx is { ShouldIndent: true, Indent: > 0 })
            underscore = underscore.WithLeadingTrivia(new string('\t', ctx.Indent));

        ctx.ShouldIndent = false;
        NormalizeValueExpression(defaultCase.Value, ctx);

        defaultCase.SetUnderscore(underscore, false);
        defaultCase.SetArrowRight(arrowRight, false);
    }

    private void NormalizeLiteralSwitchCaseExpression(LiteralSwitchCaseExpressionSyntax literalCase, WhitespaceNormalizeContext ctx)
    {
        bool shouldLineBreak = ctx.ShouldLineBreak;

        ctx.ShouldLineBreak = false;
        ctx.ShouldIndent = true;
        NormalizeValueExpression(literalCase.CaseValue, ctx);

        SyntaxToken arrowRight = literalCase.ArrowRight.WithLeadingTrivia(" ").WithTrailingTrivia(" ");

        ctx.ShouldLineBreak = shouldLineBreak;
        ctx.ShouldIndent = false;
        NormalizeValueExpression(literalCase.Value, ctx);

        literalCase.SetArrowRight(arrowRight, false);
    }

    private void NormalizeUnaryExpression(UnaryExpressionSyntax unaryExpression, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken operation = unaryExpression.Operation.WithNoTrivia();

        // Preserve list-separator spacing on the operator (`foo(a, not b)`).
        if (!ctx.IsFirstElement)
            operation = operation.WithLeadingTrivia(" ");

        // Always separate the `not` keyword from its operand (`not $x`, `not sub()`, `not (...)`).
        if (operation.RawKind == (int)SyntaxTokenKind.NotKeyword)
            operation = operation.WithTrailingTrivia(" ");

        ctx.IsFirstElement = true;
        NormalizeExpression(unaryExpression.Value, ctx);

        unaryExpression.SetOperation(operation, false);
    }

    private void NormalizeLogicalExpression(LogicalExpressionSyntax logicalExpression, WhitespaceNormalizeContext ctx)
    {
        // Keep IsFirstElement for the left operand so comma-separated args get a leading space.
        NormalizeExpression(logicalExpression.Left, ctx);

        SyntaxToken operation = logicalExpression.Operation.WithLeadingTrivia(" ").WithTrailingTrivia(" ");

        ctx.IsFirstElement = true;
        NormalizeExpression(logicalExpression.Right, ctx);

        logicalExpression.SetOperation(operation, false);
    }

    private void NormalizeBinaryExpression(BinaryExpressionSyntax binaryExpression, WhitespaceNormalizeContext ctx)
    {
        // Keep IsFirstElement for the left operand so comma-separated args get a leading space.
        NormalizeExpression(binaryExpression.Left, ctx);

        SyntaxToken operation = binaryExpression.Operation.WithLeadingTrivia(" ").WithTrailingTrivia(" ");

        ctx.IsFirstElement = true;
        NormalizeExpression(binaryExpression.Right, ctx);

        binaryExpression.SetOperation(operation, false);
    }

    private void NormalizeAssignmentExpression(AssignmentExpressionSyntax assignmentExpression, WhitespaceNormalizeContext ctx)
    {
        NormalizeExpression(assignmentExpression.Left, ctx);

        SyntaxToken operation = assignmentExpression.Operation.WithLeadingTrivia(" ").WithTrailingTrivia(" ");

        ctx.IsFirstElement = true;
        NormalizeExpression(assignmentExpression.Right, ctx);

        assignmentExpression.SetOperation(operation, false);
    }

    private void NormalizeArrayInstantiationExpression(ArrayInstantiationExpressionSyntax arrayInstantiation,
        WhitespaceNormalizeContext ctx)
    {
        SyntaxToken newToken = arrayInstantiation.New.WithNoTrivia();
        if (!ctx.IsFirstElement)
            newToken = newToken.WithLeadingTrivia(" ");

        ctx.IsFirstElement = true;
        foreach (var index in arrayInstantiation.Indexer)
            NormalizeArrayIndexExpression(index, ctx);

        arrayInstantiation.SetNew(newToken);
    }

    private void NormalizeArrayIndexExpression(ArrayIndexExpressionSyntax arrayIndex, WhitespaceNormalizeContext ctx)
    {
        NormalizeExpression(arrayIndex.Value, ctx);
        foreach (var index in arrayIndex.Indexer)
            NormalizeArrayIndexExpression(index, ctx);
    }

    private void NormalizeArrayIndexExpression(ArrayIndexerExpressionSyntax index, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken bracketOpen = index.BracketOpen.WithNoTrivia();
        SyntaxToken bracketClose = index.BracketClose.WithNoTrivia();

        ctx.IsFirstElement = true;
        ctx.ShouldIndent = false;
        NormalizeValueExpression(index.Index, ctx);

        index.SetBracketOpen(bracketOpen, false);
        index.SetBracketClose(bracketClose, false);
    }

    private void NormalizeMethodInvocationExpression(MethodInvocationExpressionSyntax invocation, WhitespaceNormalizeContext ctx)
    {
        ctx.ShouldIndent = false;

        NormalizeName(invocation.Name, ctx);
        NormalizeMethodInvocationMetadata(invocation.Metadata, ctx);
        NormalizeMethodInvocationParameters(invocation.Parameters, ctx);
    }

    private void NormalizeMethodInvocationStatement(MethodInvocationStatementSyntax invocation, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken newSemicolon = invocation.Semicolon.WithNoTrivia();

        if (ctx.ShouldLineBreak)
            newSemicolon = newSemicolon.WithTrailingTrivia("\r\n");

        // Statement start is separated by indent, not by IsFirstElement list spacing.
        ctx.IsFirstElement = true;
        NormalizeName(invocation.Name, ctx);

        invocation.SetSemicolon(newSemicolon, false);

        ctx.ShouldIndent = false;
        ctx.ShouldLineBreak = false;
        NormalizeMethodInvocationMetadata(invocation.Metadata, ctx);
        NormalizeMethodInvocationParameters(invocation.Parameters, ctx);
    }

    private void NormalizeMethodInvocationMetadata(MethodInvocationMetadataSyntax? metadata, WhitespaceNormalizeContext ctx)
    {
        if (metadata == null)
            return;

        SyntaxToken newRelSmaller = metadata.RelSmaller.WithNoTrivia();
        SyntaxToken newRelBigger = metadata.RelBigger.WithNoTrivia();

        metadata.SetRelSmaller(newRelSmaller, false);

        ctx.IsFirstElement = true;
        NormalizeLiteralExpression(metadata.Parameter, ctx);

        metadata.SetRelBigger(newRelBigger, false);
    }

    private void NormalizeMethodInvocationParameters(MethodInvocationParametersSyntax invocationParameters, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken parenOpen = invocationParameters.ParenOpen.WithNoTrivia();
        SyntaxToken parenClose = invocationParameters.ParenClose.WithNoTrivia();

        invocationParameters.SetParenOpen(parenOpen, false);
        invocationParameters.SetParenClose(parenClose, false);

        NormalizeExpressions(invocationParameters.ParameterList, ctx);
    }

    private void NormalizeExpressions(CommaSeparatedSyntaxList<ExpressionSyntax>? valueList, WhitespaceNormalizeContext ctx)
    {
        if (valueList == null)
            return;

        foreach (ExpressionSyntax value in valueList.Elements)
        {
            ctx.IsFirstElement = valueList.Elements[0] == value;
            NormalizeExpression(value, ctx);
        }
    }

    private void NormalizeValueExpressions(CommaSeparatedSyntaxList<ValueExpressionSyntax>? valueList, WhitespaceNormalizeContext ctx)
    {
        if (valueList == null)
            return;

        foreach (ValueExpressionSyntax value in valueList.Elements)
        {
            ctx.IsFirstElement = valueList.Elements[0] == value;
            NormalizeValueExpression(value, ctx);
        }
    }

    private void NormalizeValueExpression(ValueExpressionSyntax valueExpression, WhitespaceNormalizeContext ctx)
    {
        var shouldLineBreak = ctx.ShouldLineBreak;

        ctx.ShouldLineBreak = valueExpression.MetadataParameters == null && shouldLineBreak;
        NormalizeExpression(valueExpression.Value, ctx);

        ctx.ShouldLineBreak = shouldLineBreak;
        NormalizeValueMetadataParameters(valueExpression.MetadataParameters, ctx);
    }

    private void NormalizeVariableExpression(VariableExpressionSyntax variable, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken variableToken = variable.Variable.WithNoTrivia();

        string? leadingTrivia = null;
        if (ctx is { ShouldIndent: true, Indent: > 0 })
            leadingTrivia = new string('\t', ctx.Indent);
        if (!ctx.IsFirstElement)
            leadingTrivia += " ";

        variableToken = variableToken.WithLeadingTrivia(leadingTrivia);
        if (ctx.ShouldLineBreak)
            variableToken = variableToken.WithTrailingTrivia("\r\n");

        variable.SetVariable(variableToken, false);
    }

    private void NormalizeLiteralExpression(LiteralExpressionSyntax literal, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken literalToken = literal.Literal.WithNoTrivia();

        string? leadingTrivia = null;
        if (ctx is { ShouldIndent: true, Indent: > 0 })
            leadingTrivia = new string('\t', ctx.Indent);
        if (!ctx.IsFirstElement)
            leadingTrivia += " ";

        literalToken = literalToken.WithLeadingTrivia(leadingTrivia);
        if (ctx.ShouldLineBreak)
            literalToken = literalToken.WithTrailingTrivia("\r\n");

        literal.SetLiteral(literalToken, false);
    }

    private void NormalizeValueMetadataParameters(ValueMetadataParametersSyntax? valueMetadataParameters, WhitespaceNormalizeContext ctx)
    {
        if (valueMetadataParameters == null)
            return;

        SyntaxToken newRelSmaller = valueMetadataParameters.RelSmaller.WithNoTrivia();
        SyntaxToken newRelBigger = valueMetadataParameters.RelBigger.WithNoTrivia();

        valueMetadataParameters.SetRelSmaller(newRelSmaller, false);
        valueMetadataParameters.SetRelBigger(newRelBigger, false);

        ctx.IsFirstElement = true;
        NormalizeLiteralExpression(valueMetadataParameters.Parameter, ctx);
    }

    private void NormalizeName(NameSyntax name, WhitespaceNormalizeContext ctx)
    {
        switch (name)
        {
            case SimpleNameSyntax simpleName:
                NormalizeSimpleName(simpleName, ctx);
                break;

            case QualifiedNameSyntax qualifiedName:
                NormalizeQualifiedName(qualifiedName, ctx);
                break;
        }
    }

    private void NormalizeSimpleName(SimpleNameSyntax name, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken identifierToken = name.Identifier.WithNoTrivia();

        string? leadingTrivia = null;
        if (ctx is { ShouldIndent: true, Indent: > 0 })
            leadingTrivia = new string('\t', ctx.Indent);
        if (!ctx.IsFirstElement)
            leadingTrivia += " ";

        identifierToken = identifierToken.WithLeadingTrivia(leadingTrivia);

        name.SetIdentifier(identifierToken, false);
    }

    private void NormalizeQualifiedName(QualifiedNameSyntax name, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken dotToken = name.Dot.WithNoTrivia();

        name.SetDot(dotToken);

        NormalizeName(name.Left, ctx);
        NormalizeName(name.Right, ctx);
    }
}