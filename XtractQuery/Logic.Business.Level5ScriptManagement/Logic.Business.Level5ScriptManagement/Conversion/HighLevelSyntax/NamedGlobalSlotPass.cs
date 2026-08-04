using Logic.Business.Level5ScriptManagement.InternalContract.Conversion.HighLevelSyntax;
using Logic.Domain.CodeAnalysis.Contract.DataClasses;
using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;
using Logic.Domain.CodeAnalysis.Contract.Level5;

namespace Logic.Business.Level5ScriptManagement.Conversion.HighLevelSyntax;

/// <summary>
/// Compile-only: rewrites declared named globals (<c>$bossType</c>) to <c>$globalN</c>
/// with script-wide slot assignment. Explicit <c>$globalN</c> / <c>$globalN_name</c> reserve
/// slots. Declaration members are stripped from the returned code unit.
/// </summary>
internal class NamedGlobalSlotPass(ILevel5SyntaxFactory syntaxFactory) : INamedGlobalSlotPass
{
    public CodeUnitSyntax Convert(CodeUnitSyntax tree)
    {
        List<(string Name, SyntaxLocation Location)> declared = CollectDeclaredNames(tree);
        bool hasGlobalMembers = tree.Members.Any(m => m is GlobalDeclarationStatementSyntax);

        if (declared.Count == 0)
        {
            if (!hasGlobalMembers)
                return tree;

            return new CodeUnitSyntax(tree.Members.OfType<MethodDeclarationSyntax>().Cast<CodeUnitMemberSyntax>().ToList());
        }

        HashSet<int> reserved = CollectReservedGlobalSlots(tree);
        ValidateReservedGlobalSlots(reserved);

        Dictionary<string, int> assignment = AssignSlots(declared, reserved);
        RewriteNamedGlobals(tree, assignment);

        return new CodeUnitSyntax(tree.Members.OfType<MethodDeclarationSyntax>().Cast<CodeUnitMemberSyntax>().ToList());
    }

    private static List<(string Name, SyntaxLocation Location)> CollectDeclaredNames(CodeUnitSyntax tree)
    {
        var result = new List<(string Name, SyntaxLocation Location)>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (CodeUnitMemberSyntax member in tree.Members)
        {
            if (member is not GlobalDeclarationStatementSyntax declaration)
                continue;

            foreach (VariableExpressionSyntax variable in declaration.Variables.Elements)
            {
                string name = variable.Variable.Text;
                if (!VariableSlotClassifier.IsNamedVariable(name))
                {
                    throw CreateException(
                        $"Global declaration name \"{name}\" must be a free-form variable, not a typed slot.",
                        variable.Location);
                }

                if (!seen.Add(name))
                {
                    throw CreateException(
                        $"Global variable \"{name}\" is declared more than once.",
                        variable.Location);
                }

                result.Add((name, variable.Location));
            }
        }

        return result;
    }

    private static HashSet<int> CollectReservedGlobalSlots(CodeUnitSyntax tree)
    {
        var reserved = new HashSet<int>();

        foreach (MethodDeclarationSyntax method in tree.MethodDeclarations)
        {
            foreach (StatementSyntax statement in method.Body.Expressions)
            {
                foreach (string name in CollectAllVariableNames(statement))
                {
                    if (VariableSlotClassifier.TryGetExplicitGlobalSlot(name, out int slot))
                        reserved.Add(slot);
                }
            }
        }

        return reserved;
    }

    private static void ValidateReservedGlobalSlots(HashSet<int> reserved)
    {
        foreach (int slot in reserved)
        {
            if (slot is < 0 or >= VariableSlotClassifier.GlobalSlotCount)
            {
                throw new InvalidOperationException(
                    $"Global slot {slot} is out of range. Valid global slots are 0..{VariableSlotClassifier.GlobalSlotCount - 1}.");
            }
        }
    }

    private static Dictionary<string, int> AssignSlots(
        IReadOnlyList<(string Name, SyntaxLocation Location)> declared,
        HashSet<int> reserved)
    {
        var assignment = new Dictionary<string, int>(StringComparer.Ordinal);
        var used = new HashSet<int>(reserved);

        foreach ((string name, SyntaxLocation location) in declared)
        {
            int slot = -1;
            for (var candidate = 0; candidate < VariableSlotClassifier.GlobalSlotCount; candidate++)
            {
                if (used.Add(candidate))
                {
                    slot = candidate;
                    break;
                }
            }

            if (slot < 0)
            {
                throw CreateException(
                    $"Cannot allocate global slot for \"{name}\": all {VariableSlotClassifier.GlobalSlotCount} global slots are in use.",
                    location);
            }

            assignment[name] = slot;
        }

        return assignment;
    }

    private void RewriteNamedGlobals(CodeUnitSyntax tree, IReadOnlyDictionary<string, int> assignment)
    {
        foreach (MethodDeclarationSyntax method in tree.MethodDeclarations)
        {
            foreach (StatementSyntax statement in method.Body.Expressions)
                RewriteStatement(statement, assignment);
        }
    }

    private void RewriteStatement(StatementSyntax statement, IReadOnlyDictionary<string, int> assignment)
    {
        switch (statement)
        {
            case AssignmentStatementSyntax assignmentStatement:
                RewriteExpression(assignmentStatement.Left, assignment);
                RewriteExpression(assignmentStatement.Right, assignment);
                break;

            case IfGotoStatementSyntax ifGoto:
                RewriteExpression(ifGoto.Value, assignment);
                break;

            case IfNotGotoStatementSyntax ifNotGoto:
                RewriteExpression(ifNotGoto.Comparison, assignment);
                break;

            case ReturnStatementSyntax { ValueExpression: not null } returnStatement:
                RewriteExpression(returnStatement.ValueExpression, assignment);
                break;

            case PostfixUnaryStatementSyntax postfix:
                RewriteExpression(postfix.Expression, assignment);
                break;

            case MethodInvocationStatementSyntax invocation:
                RewriteInvocationParameters(invocation.Parameters, assignment);
                break;

            case GotoStatementSyntax gotoStatement:
                foreach (ValueExpressionSyntax target in gotoStatement.Targets.Elements)
                    RewriteExpression(target, assignment);
                break;

            case IfStatementSyntax ifStatement:
                RewriteExpression(ifStatement.Condition, assignment);
                foreach (StatementSyntax nested in ifStatement.Body.Statements)
                    RewriteStatement(nested, assignment);
                if (ifStatement.Else != null)
                    RewriteStatement(ifStatement.Else.Statement, assignment);
                break;

            case WhileStatementSyntax whileStatement:
                RewriteExpression(whileStatement.Condition, assignment);
                if (whileStatement.Body != null)
                {
                    foreach (StatementSyntax nested in whileStatement.Body.Statements)
                        RewriteStatement(nested, assignment);
                }
                break;

            case ForStatementSyntax forStatement:
                if (forStatement.Initializer != null)
                    RewriteStatement(forStatement.Initializer, assignment);
                RewriteExpression(forStatement.Condition, assignment);
                if (forStatement.Iterator != null)
                    RewriteStatement(forStatement.Iterator, assignment);
                foreach (StatementSyntax nested in forStatement.Body.Statements)
                    RewriteStatement(nested, assignment);
                break;

            case DoWhileStatementSyntax doWhile:
                RewriteExpression(doWhile.Condition, assignment);
                foreach (StatementSyntax nested in doWhile.Body.Statements)
                    RewriteStatement(nested, assignment);
                break;

            case BlockSyntax block:
                foreach (StatementSyntax nested in block.Statements)
                    RewriteStatement(nested, assignment);
                break;
        }
    }

    private void RewriteExpression(ExpressionSyntax expression, IReadOnlyDictionary<string, int> assignment)
    {
        switch (expression)
        {
            case VariableExpressionSyntax variable:
                RewriteVariable(variable, assignment);
                break;

            case ValueExpressionSyntax value:
                RewriteExpression(value.Value, assignment);
                break;

            case ParenthesizedExpressionSyntax parenthesized:
                RewriteExpression(parenthesized.Expression, assignment);
                break;

            case UnaryExpressionSyntax unary:
                RewriteExpression(unary.Value, assignment);
                break;

            case BinaryExpressionSyntax binary:
                RewriteExpression(binary.Left, assignment);
                RewriteExpression(binary.Right, assignment);
                break;

            case LogicalExpressionSyntax logical:
                RewriteExpression(logical.Left, assignment);
                RewriteExpression(logical.Right, assignment);
                break;

            case MethodInvocationExpressionSyntax invocation:
                RewriteInvocationParameters(invocation.Parameters, assignment);
                break;

            case PostfixUnaryExpressionSyntax postfix:
                RewriteExpression(postfix.Value, assignment);
                break;

            case ArrayIndexExpressionSyntax arrayIndex:
                RewriteExpression(arrayIndex.Value, assignment);
                foreach (ArrayIndexerExpressionSyntax indexer in arrayIndex.Indexer)
                    RewriteExpression(indexer.Index, assignment);
                break;

            case ArrayInstantiationExpressionSyntax arrayInstantiation:
                foreach (ArrayIndexerExpressionSyntax indexer in arrayInstantiation.Indexer)
                    RewriteExpression(indexer.Index, assignment);
                break;

            case TypeCastValueExpressionSyntax typeCast:
                RewriteExpression(typeCast.Value, assignment);
                break;

            case SwitchExpressionSyntax switchExpression:
                RewriteExpression(switchExpression.Value, assignment);
                foreach (SwitchCaseExpressionSyntax @case in switchExpression.CaseBlock.Cases)
                    RewriteSwitchCase(@case, assignment);
                break;

            case AssignmentExpressionSyntax nestedAssignment:
                RewriteExpression(nestedAssignment.Left, assignment);
                RewriteExpression(nestedAssignment.Right, assignment);
                break;
        }
    }

    private void RewriteSwitchCase(SwitchCaseExpressionSyntax @case, IReadOnlyDictionary<string, int> assignment)
    {
        switch (@case)
        {
            case LiteralSwitchCaseExpressionSyntax literal:
                RewriteExpression(literal.CaseValue, assignment);
                RewriteExpression(literal.Value, assignment);
                break;

            case DefaultSwitchCaseExpressionSyntax defaultCase:
                RewriteExpression(defaultCase.Value, assignment);
                break;
        }
    }

    private void RewriteInvocationParameters(
        MethodInvocationParametersSyntax parameters,
        IReadOnlyDictionary<string, int> assignment)
    {
        if (parameters.ParameterList?.Elements is null)
            return;

        foreach (ExpressionSyntax parameter in parameters.ParameterList.Elements)
            RewriteExpression(parameter, assignment);
    }

    private void RewriteVariable(VariableExpressionSyntax variable, IReadOnlyDictionary<string, int> assignment)
    {
        string text = variable.Variable.Text;
        if (!assignment.TryGetValue(text, out int slot))
            return;

        SyntaxToken token = syntaxFactory.Variable("global", (uint)slot);
        if (variable.Variable.LeadingTrivia is { } leading)
            token = token.WithLeadingTrivia(leading.Text);
        if (variable.Variable.TrailingTrivia is { } trailing)
            token = token.WithTrailingTrivia(trailing.Text);

        variable.SetVariable(token, updatePositions: false);
    }

    private static HashSet<string> CollectAllVariableNames(StatementSyntax statement)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        names.UnionWith(CollectStatementUses(statement));
        names.UnionWith(ExpressionSideEffectClassifier.CollectAssignedVariables(statement));

        switch (statement)
        {
            case IfStatementSyntax ifStatement:
                names.UnionWith(ExpressionSideEffectClassifier.CollectReadVariables(ifStatement.Condition));
                foreach (StatementSyntax nested in ifStatement.Body.Statements)
                    names.UnionWith(CollectAllVariableNames(nested));
                if (ifStatement.Else != null)
                    names.UnionWith(CollectAllVariableNames(ifStatement.Else.Statement));
                break;

            case WhileStatementSyntax whileStatement:
                names.UnionWith(ExpressionSideEffectClassifier.CollectReadVariables(whileStatement.Condition));
                if (whileStatement.Body != null)
                {
                    foreach (StatementSyntax nested in whileStatement.Body.Statements)
                        names.UnionWith(CollectAllVariableNames(nested));
                }
                break;

            case ForStatementSyntax forStatement:
                if (forStatement.Initializer != null)
                    names.UnionWith(CollectAllVariableNames(forStatement.Initializer));
                names.UnionWith(ExpressionSideEffectClassifier.CollectReadVariables(forStatement.Condition));
                if (forStatement.Iterator != null)
                    names.UnionWith(CollectAllVariableNames(forStatement.Iterator));
                foreach (StatementSyntax nested in forStatement.Body.Statements)
                    names.UnionWith(CollectAllVariableNames(nested));
                break;

            case DoWhileStatementSyntax doWhile:
                names.UnionWith(ExpressionSideEffectClassifier.CollectReadVariables(doWhile.Condition));
                foreach (StatementSyntax nested in doWhile.Body.Statements)
                    names.UnionWith(CollectAllVariableNames(nested));
                break;

            case BlockSyntax block:
                foreach (StatementSyntax nested in block.Statements)
                    names.UnionWith(CollectAllVariableNames(nested));
                break;
        }

        return names;
    }

    private static HashSet<string> CollectStatementUses(StatementSyntax statement)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);

        switch (statement)
        {
            case AssignmentStatementSyntax assignment:
                CollectAssignmentReads(assignment.Left, assignment.Right, result);
                break;

            case IfGotoStatementSyntax ifGoto:
                result.UnionWith(ExpressionSideEffectClassifier.CollectReadVariables(ifGoto.Value));
                break;

            case IfNotGotoStatementSyntax ifNotGoto:
                result.UnionWith(ExpressionSideEffectClassifier.CollectReadVariables(ifNotGoto.Comparison));
                break;

            case ReturnStatementSyntax { ValueExpression: not null } returnStatement:
                result.UnionWith(ExpressionSideEffectClassifier.CollectReadVariables(returnStatement.ValueExpression));
                break;

            case PostfixUnaryStatementSyntax postfix:
                result.UnionWith(ExpressionSideEffectClassifier.CollectReadVariables(postfix.Expression));
                break;

            case MethodInvocationStatementSyntax invocation:
                if (invocation.Parameters.ParameterList?.Elements is not null)
                {
                    foreach (ExpressionSyntax parameter in invocation.Parameters.ParameterList.Elements)
                        result.UnionWith(ExpressionSideEffectClassifier.CollectReadVariables(parameter));
                }
                break;

            case GotoStatementSyntax gotoStatement:
                foreach (ValueExpressionSyntax target in gotoStatement.Targets.Elements)
                    result.UnionWith(ExpressionSideEffectClassifier.CollectReadVariables(target));
                break;
        }

        return result;
    }

    private static void CollectAssignmentReads(
        ExpressionSyntax left,
        ExpressionSyntax right,
        HashSet<string> result)
    {
        switch (left)
        {
            case ValueExpressionSyntax { Value: VariableExpressionSyntax }:
            case VariableExpressionSyntax:
                break;

            case ArrayIndexExpressionSyntax arrayIndex:
                result.UnionWith(ExpressionSideEffectClassifier.CollectReadVariables(arrayIndex.Value));
                foreach (ArrayIndexerExpressionSyntax indexer in arrayIndex.Indexer)
                    result.UnionWith(ExpressionSideEffectClassifier.CollectReadVariables(indexer.Index));
                break;

            default:
                result.UnionWith(ExpressionSideEffectClassifier.CollectReadVariables(left));
                break;
        }

        result.UnionWith(ExpressionSideEffectClassifier.CollectReadVariables(right));
    }

    private static Exception CreateException(string message, SyntaxLocation location)
    {
        return new InvalidOperationException($"{message} (Line {location.Line}, Column {location.Column})");
    }
}
