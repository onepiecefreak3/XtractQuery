using Logic.Domain.CodeAnalysis.Contract.Level5;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses;
using Logic.Domain.CodeAnalysis.Level5.InternalContract.DataClasses;
using Logic.Domain.CodeAnalysis.Contract.DataClasses;

namespace Logic.Domain.CodeAnalysis.Level5
{
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

        public void NormalizeMethodInvocationExpressionParameters(MethodInvocationExpressionParametersSyntax invocationParameters)
        {
            var ctx = new WhitespaceNormalizeContext();
            NormalizeMethodInvocationExpressionParameters(invocationParameters, ctx);

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
            foreach (MethodDeclarationSyntax methodDeclaration in codeUnit.MethodDeclarations)
            {
                ctx.IsFirstElement = codeUnit.MethodDeclarations[0] == methodDeclaration;
                ctx.ShouldLineBreak = codeUnit.MethodDeclarations[^1] != methodDeclaration;
                NormalizeMethodDeclaration(methodDeclaration, ctx);
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

                case PostfixUnaryStatementSyntax postfixUnaryStatement:
                    NormalizePostfixUnaryStatement(postfixUnaryStatement, ctx);
                    break;

                case AssignmentStatementSyntax assignmentStatement:
                    NormalizeAssignmentStatement(assignmentStatement, ctx);
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

            if (ctx is { ShouldIndent: true, Indent: > 0 })
                newIf = newIf.WithLeadingTrivia(new string('\t', ctx.Indent));

            ctx.ShouldIndent = false;
            ctx.ShouldLineBreak = false;
            ctx.IsFirstElement = true;
            NormalizeValueExpression(ifGotoStatement.Value, ctx);

            ctx.ShouldLineBreak = true;
            ctx.IsFirstElement = false;
            NormalizeGotoStatement(ifGotoStatement.Goto, ctx);

            ifGotoStatement.SetIf(newIf, false);
        }

        private void NormalizeIfNotGotoStatement(IfNotGotoStatementSyntax ifNotGotoStatement, WhitespaceNormalizeContext ctx)
        {
            SyntaxToken newIf = ifNotGotoStatement.If.WithTrailingTrivia(null).WithTrailingTrivia(" ");

            if (ctx is { ShouldIndent: true, Indent: > 0 })
                newIf = newIf.WithLeadingTrivia(new string('\t', ctx.Indent));

            ctx.ShouldIndent = false;
            ctx.ShouldLineBreak = false;
            ctx.IsFirstElement = true;
            NormalizeExpression(ifNotGotoStatement.Comparison, ctx);

            ctx.ShouldLineBreak = true;
            ctx.IsFirstElement = false;
            NormalizeGotoStatement(ifNotGotoStatement.Goto, ctx);

            ifNotGotoStatement.SetIf(newIf, false);
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
            NormalizeValueExpression(gotoStatement.Target, ctx);

            gotoStatement.SetGoto(newGoto, false);
            gotoStatement.SetSemicolon(newSemicolon, false);
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
            SyntaxToken newReturnKeyword = returnStatement.Return.WithLeadingTrivia(null).WithTrailingTrivia(" ");
            SyntaxToken newSemicolon = returnStatement.Semicolon.WithNoTrivia();

            if (ctx is { ShouldIndent: true, Indent: > 0 })
                newReturnKeyword = newReturnKeyword.WithLeadingTrivia(new string('\t', ctx.Indent));

            if (ctx.ShouldLineBreak)
                newSemicolon = newSemicolon.WithTrailingTrivia("\r\n");

            returnStatement.SetReturn(newReturnKeyword, false);
            returnStatement.SetSemicolon(newSemicolon, false);

            ctx.IsFirstElement = true;
            ctx.ShouldIndent = false;
            ctx.ShouldLineBreak = false;
            NormalizeValueExpression(returnStatement.ValueExpression, ctx);
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

                case PostfixUnaryExpressionSyntax postfixUnaryExpression:
                    NormalizePostfixUnaryExpression(postfixUnaryExpression, ctx);
                    break;

                case SwitchExpressionSyntax switchExpression:
                    NormalizeSwitchExpression(switchExpression, ctx);
                    break;

                case LogicalExpressionSyntax logicalExpression:
                    ctx.IsFirstElement = true;
                    NormalizeLogicalExpression(logicalExpression, ctx);
                    break;

                case UnaryExpressionSyntax rightUnaryExpression:
                    ctx.IsFirstElement = true;
                    NormalizeUnaryExpression(rightUnaryExpression, ctx);
                    break;

                case BinaryExpressionSyntax rightBinaryExpression:
                    ctx.IsFirstElement = true;
                    NormalizeBinaryExpression(rightBinaryExpression, ctx);
                    break;

                case ArrayInstantiationExpressionSyntax rightArrayInstantiation:
                    ctx.IsFirstElement = true;
                    NormalizeArrayInstantiationExpression(rightArrayInstantiation, ctx);
                    break;

                case ArrayIndexExpressionSyntax rightArrayIndex:
                    ctx.IsFirstElement = true;
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
            NormalizeTypeCastExpression(typeCastValueExpression.TypeCast, ctx);

            ctx.IsFirstElement = true;
            ctx.ShouldIndent = false;
            ctx.ShouldLineBreak = false;
            NormalizeValueExpression(typeCastValueExpression.Value, ctx);
        }

        private void NormalizeTypeCastExpression(TypeCastExpressionSyntax typeCasExpression, WhitespaceNormalizeContext ctx)
        {
            SyntaxToken parenOpen = typeCasExpression.ParenOpen.WithNoTrivia();
            SyntaxToken typeKeyword = typeCasExpression.TypeKeyword.WithNoTrivia();
            SyntaxToken parenClose = typeCasExpression.ParenClose.WithNoTrivia();

            typeCasExpression.SetParenOpen(parenOpen, false);
            typeCasExpression.SetType(typeKeyword, false);
            typeCasExpression.SetParenClose(parenClose, false);
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

            ctx.IsFirstElement = operation.RawKind != (int)SyntaxTokenKind.NotKeyword;
            NormalizeValueExpression(unaryExpression.Value, ctx);

            unaryExpression.SetOperation(operation, false);
        }

        private void NormalizeLogicalExpression(LogicalExpressionSyntax logicalExpression, WhitespaceNormalizeContext ctx)
        {
            NormalizeExpression(logicalExpression.Left, ctx);

            SyntaxToken operation = logicalExpression.Operation.WithLeadingTrivia(" ").WithTrailingTrivia(" ");

            ctx.IsFirstElement = true;
            NormalizeExpression(logicalExpression.Right, ctx);

            logicalExpression.SetOperation(operation, false);
        }

        private void NormalizeBinaryExpression(BinaryExpressionSyntax binaryExpression, WhitespaceNormalizeContext ctx)
        {
            NormalizeExpression(binaryExpression.Left, ctx);

            SyntaxToken operation = binaryExpression.Operation.WithLeadingTrivia(" ").WithTrailingTrivia(" ");

            NormalizeExpression(binaryExpression.Right, ctx);

            binaryExpression.SetOperation(operation, false);
        }

        private void NormalizeArrayInstantiationExpression(ArrayInstantiationExpressionSyntax arrayInstantiation,
            WhitespaceNormalizeContext ctx)
        {
            SyntaxToken newToken = arrayInstantiation.New.WithNoTrivia();

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
            SyntaxToken newIdentifier = invocation.Identifier.WithNoTrivia();

            invocation.SetIdentifier(newIdentifier, false);

            NormalizeMethodInvocationExpressionParameters(invocation.Parameters, ctx);
        }

        private void NormalizeMethodInvocationExpressionParameters(MethodInvocationExpressionParametersSyntax invocationParameters, WhitespaceNormalizeContext ctx)
        {
            SyntaxToken parenOpen = invocationParameters.ParenOpen.WithNoTrivia();
            SyntaxToken parenClose = invocationParameters.ParenClose.WithNoTrivia();

            invocationParameters.SetParenOpen(parenOpen, false);
            invocationParameters.SetParenClose(parenClose, false);

            NormalizeValueExpressions(invocationParameters.ParameterList, ctx);
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
            NormalizeLiteralExpression(valueMetadataParameters.Parameter, ctx);
            valueMetadataParameters.SetRelBigger(newRelBigger, false);
        }
    }
}
