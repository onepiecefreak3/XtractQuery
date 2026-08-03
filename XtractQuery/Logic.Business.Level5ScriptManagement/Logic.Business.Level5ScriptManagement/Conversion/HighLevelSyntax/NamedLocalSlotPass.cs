using Logic.Business.Level5ScriptManagement.Conversion.HighLevelSyntax.Cfg;
using Logic.Business.Level5ScriptManagement.DataClasses.Conversion;
using Logic.Business.Level5ScriptManagement.InternalContract.Conversion.HighLevelSyntax;
using Logic.Domain.CodeAnalysis.Contract.DataClasses;
using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;
using Logic.Domain.CodeAnalysis.Contract.Level5;

namespace Logic.Business.Level5ScriptManagement.Conversion.HighLevelSyntax;

/// <summary>
/// Compile-only: rewrites free-form named locals (<c>$counter</c>) to <c>$localN</c>
/// with CFG liveness-based slot reuse. Explicit typed slots are left alone and reserve
/// their local indices. Local bank is slots 0..999.
/// </summary>
internal class NamedLocalSlotPass(
    IControlFlowGraphBuilder cfgBuilder,
    ILevel5SyntaxFactory syntaxFactory) : INamedLocalSlotPass
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
        IReadOnlyList<StatementSyntax> statements = method.Body.Expressions;
        if (statements.Count == 0)
            return method;

        HashSet<int> reservedLocals = CollectReservedLocalSlots(statements);
        List<string> namedLocals = CollectNamedLocals(statements);
        if (namedLocals.Count == 0)
            return method;

        ValidateReservedLocalSlots(reservedLocals, method.Location);

        ControlFlowGraph cfg = cfgBuilder.Build(statements);
        Dictionary<string, HashSet<string>> interference = BuildInterference(statements, cfg, namedLocals);
        Dictionary<string, int> assignment = AssignSlots(namedLocals, interference, reservedLocals);

        RewriteNamedLocals(statements, assignment);

        return method;
    }

    private static HashSet<int> CollectReservedLocalSlots(IReadOnlyList<StatementSyntax> statements)
    {
        var reserved = new HashSet<int>();
        foreach (StatementSyntax statement in statements)
            CollectReservedLocalSlots(statement, reserved);
        return reserved;
    }

    private static void CollectReservedLocalSlots(StatementSyntax statement, HashSet<int> reserved)
    {
        foreach (string name in CollectAllVariableNames(statement))
        {
            if (VariableSlotClassifier.TryGetExplicitLocalSlot(name, out int slot))
                reserved.Add(slot);
        }
    }

    private static List<string> CollectNamedLocals(IReadOnlyList<StatementSyntax> statements)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (StatementSyntax statement in statements)
        {
            foreach (string name in CollectAllVariableNames(statement))
            {
                if (VariableSlotClassifier.IsNamedLocal(name))
                    names.Add(name);
            }
        }

        return names.OrderBy(n => n, StringComparer.Ordinal).ToList();
    }

    private static void ValidateReservedLocalSlots(HashSet<int> reserved, SyntaxLocation location)
    {
        foreach (int slot in reserved)
        {
            if (slot is < 0 or >= VariableSlotClassifier.LocalSlotCount)
            {
                throw CreateException(
                    $"Local slot {slot} is out of range. Valid local slots are 0..{VariableSlotClassifier.LocalSlotCount - 1}.",
                    location);
            }
        }
    }

    private static Dictionary<string, HashSet<string>> BuildInterference(
        IReadOnlyList<StatementSyntax> statements,
        ControlFlowGraph cfg,
        IReadOnlyList<string> namedLocals)
    {
        var namedSet = new HashSet<string>(namedLocals, StringComparer.Ordinal);
        var uses = new HashSet<string>[statements.Count];
        var defs = new HashSet<string>[statements.Count];

        for (var i = 0; i < statements.Count; i++)
        {
            uses[i] = FilterNamed(CollectStatementUses(statements[i]), namedSet);
            defs[i] = FilterNamed(ExpressionSideEffectClassifier.CollectAssignedVariables(statements[i]), namedSet);
        }

        Dictionary<StatementBlock, HashSet<string>> liveIn = ComputeLiveIn(cfg, uses, defs);
        var interference = namedLocals.ToDictionary(
            n => n,
            _ => new HashSet<string>(StringComparer.Ordinal),
            StringComparer.Ordinal);

        // Overlapping live ranges interfere: clique on LiveIn/LiveOut at each statement.
        foreach (StatementBlock block in cfg.Blocks)
        {
            if (block.IsExit || block.StatementCount == 0)
                continue;

            HashSet<string> live = JoinSuccessorLiveIn(block, liveIn, cfg);
            for (int i = block.EndStatementIndex - 1; i >= block.InstructionIndex; i--)
            {
                AddInterferenceClique(interference, live);

                live.ExceptWith(defs[i]);
                live.UnionWith(uses[i]);

                AddInterferenceClique(interference, live);
            }
        }

        return interference;
    }

    private static void AddInterferenceClique(
        Dictionary<string, HashSet<string>> interference,
        HashSet<string> live)
    {
        if (live.Count < 2)
            return;

        string[] vars = live.ToArray();
        for (var i = 0; i < vars.Length; i++)
        {
            for (var j = i + 1; j < vars.Length; j++)
            {
                interference[vars[i]].Add(vars[j]);
                interference[vars[j]].Add(vars[i]);
            }
        }
    }

    private static Dictionary<StatementBlock, HashSet<string>> ComputeLiveIn(
        ControlFlowGraph cfg,
        HashSet<string>[] uses,
        HashSet<string>[] defs)
    {
        var liveIn = new Dictionary<StatementBlock, HashSet<string>>();
        var liveOut = new Dictionary<StatementBlock, HashSet<string>>();

        foreach (StatementBlock block in cfg.Blocks)
        {
            if (block.IsExit)
                continue;

            liveIn[block] = [];
            liveOut[block] = [];
        }

        var changed = true;
        while (changed)
        {
            changed = false;

            foreach (StatementBlock block in cfg.Blocks)
            {
                if (block.IsExit)
                    continue;

                HashSet<string> newOut = JoinSuccessorLiveIn(block, liveIn, cfg);
                if (!SetEquals(liveOut[block], newOut))
                {
                    liveOut[block] = newOut;
                    changed = true;
                }

                HashSet<string> newIn = TransferBackward(block, uses, defs, liveOut[block]);
                if (!SetEquals(liveIn[block], newIn))
                {
                    liveIn[block] = newIn;
                    changed = true;
                }
            }
        }

        return liveIn;
    }

    private static HashSet<string> JoinSuccessorLiveIn(
        StatementBlock block,
        IReadOnlyDictionary<StatementBlock, HashSet<string>> liveIn,
        ControlFlowGraph cfg)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (ControlFlowEdge edge in ControlFlowGraphQueries.GetOutgoing(cfg, block))
        {
            if (edge.Target.IsExit)
                continue;

            if (liveIn.TryGetValue(edge.Target, out HashSet<string>? succIn))
                result.UnionWith(succIn);
        }

        return result;
    }

    private static HashSet<string> TransferBackward(
        StatementBlock block,
        HashSet<string>[] uses,
        HashSet<string>[] defs,
        HashSet<string> liveOut)
    {
        HashSet<string> live = CloneSet(liveOut);
        for (int i = block.EndStatementIndex - 1; i >= block.InstructionIndex; i--)
        {
            live.ExceptWith(defs[i]);
            live.UnionWith(uses[i]);
        }

        return live;
    }

    private static Dictionary<string, int> AssignSlots(
        IReadOnlyList<string> namedLocals,
        IReadOnlyDictionary<string, HashSet<string>> interference,
        HashSet<int> reservedLocals)
    {
        var assignment = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (string name in namedLocals)
        {
            var forbidden = new HashSet<int>(reservedLocals);
            foreach (string neighbor in interference[name])
            {
                if (assignment.TryGetValue(neighbor, out int neighborSlot))
                    forbidden.Add(neighborSlot);
            }

            int slot = -1;
            for (var candidate = 0; candidate < VariableSlotClassifier.LocalSlotCount; candidate++)
            {
                if (!forbidden.Contains(candidate))
                {
                    slot = candidate;
                    break;
                }
            }

            if (slot < 0)
            {
                throw new InvalidOperationException(
                    $"Cannot allocate local slot for \"{name}\": all {VariableSlotClassifier.LocalSlotCount} local slots are in use.");
            }

            assignment[name] = slot;
        }

        return assignment;
    }

    private void RewriteNamedLocals(
        IReadOnlyList<StatementSyntax> statements,
        IReadOnlyDictionary<string, int> assignment)
    {
        foreach (StatementSyntax statement in statements)
            RewriteStatement(statement, assignment);
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

        SyntaxToken token = syntaxFactory.Variable("local", (uint)slot);
        if (variable.Variable.LeadingTrivia is { } leading)
            token = token.WithLeadingTrivia(leading.Text);
        if (variable.Variable.TrailingTrivia is { } trailing)
            token = token.WithTrailingTrivia(trailing.Text);

        variable.SetVariable(token, updatePositions: false);
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

    private static HashSet<string> CollectAllVariableNames(StatementSyntax statement)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        names.UnionWith(CollectStatementUses(statement));
        names.UnionWith(ExpressionSideEffectClassifier.CollectAssignedVariables(statement));
        return names;
    }

    private static HashSet<string> FilterNamed(HashSet<string> names, HashSet<string> namedSet)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (string name in names)
        {
            if (namedSet.Contains(name))
                result.Add(name);
        }

        return result;
    }

    private static HashSet<string> CloneSet(HashSet<string> source)
    {
        return new HashSet<string>(source, StringComparer.Ordinal);
    }

    private static bool SetEquals(HashSet<string> left, HashSet<string> right)
    {
        return left.SetEquals(right);
    }

    private static Exception CreateException(string message, SyntaxLocation location)
    {
        return new InvalidOperationException($"{message} (Line {location.Line}, Column {location.Column})");
    }
}
