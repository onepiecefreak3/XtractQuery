using Logic.Business.Level5ScriptManagement.Conversion.HighLevelSyntax.Cfg;
using Logic.Business.Level5ScriptManagement.DataClasses.Conversion;
using Logic.Business.Level5ScriptManagement.InternalContract.Conversion.HighLevelSyntax;
using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;
using Logic.Domain.CodeAnalysis.Contract.Level5;

namespace Logic.Business.Level5ScriptManagement.Conversion.HighLevelSyntax;

internal class TempPropagationPass(
    IControlFlowGraphBuilder cfgBuilder,
    ILevel5SyntaxFactory syntaxFactory) : ITempPropagationPass
{
    private readonly TempExpressionRewriter _rewriter = new(syntaxFactory);

    public IReadOnlyList<StatementSyntax> Apply(IReadOnlyList<StatementSyntax> statements)
    {
        var result = statements.ToList();
        var changed = true;

        while (changed)
        {
            changed = false;
            ControlFlowGraph cfg = cfgBuilder.Build(result);
            Dictionary<int, Dictionary<string, HashSet<int>>> reaching = ComputeReachingDefinitions(result, cfg);
            List<TempDefinition> definitions = CollectDefinitions(result);

            foreach (TempDefinition definition in definitions)
            {
                List<TempUse> uses = CollectUsesForDefinition(result, definition, reaching);
                if (TryApplyDefinition(result, cfg, definition, uses))
                {
                    changed = true;
                    break;
                }
            }
        }

        return result;
    }

    private bool TryApplyDefinition(
        List<StatementSyntax> statements,
        ControlFlowGraph cfg,
        TempDefinition definition,
        List<TempUse> uses)
    {
        if (uses.Count == 0)
            return TryEliminateDead(statements, definition);

        if (uses.Count == 1)
            return TryFoldUses(statements, cfg, definition, uses);

        if (ExpressionSideEffectClassifier.IsPure(definition.Right))
            return TryFoldUses(statements, cfg, definition, uses);

        return false;
    }

    private bool TryEliminateDead(List<StatementSyntax> statements, TempDefinition definition)
    {
        if (definition.Right is MethodInvocationExpressionSyntax invocation)
        {
            statements[definition.StatementIndex] = new MethodInvocationStatementSyntax(
                invocation.Name,
                invocation.Metadata,
                invocation.Parameters,
                definition.Statement.Semicolon);
            return true;
        }

        if (!ExpressionSideEffectClassifier.IsPure(definition.Right))
            return false;

        statements.RemoveAt(definition.StatementIndex);
        return true;
    }

    private bool TryFoldUses(
        List<StatementSyntax> statements,
        ControlFlowGraph cfg,
        TempDefinition definition,
        List<TempUse> uses)
    {
        List<TempUse> uniqueUses = DeduplicateUsesByStatement(uses);

        foreach (TempUse use in uniqueUses)
        {
            if (use.StatementIndex <= definition.StatementIndex)
                return false;

            if (!IsFoldSafe(statements, cfg, definition, use.StatementIndex))
                return false;
        }

        var rewrites = new List<(int Index, StatementSyntax Statement)>();
        foreach (TempUse use in uniqueUses)
        {
            StatementSyntax? rewritten = _rewriter.ReplaceTempInStatement(
                statements[use.StatementIndex],
                definition.TempName,
                definition.Right,
                use.Context);

            if (rewritten is null)
                return false;

            rewrites.Add((use.StatementIndex, rewritten));
        }

        foreach ((int index, StatementSyntax statement) in rewrites)
            statements[index] = statement;

        statements.RemoveAt(definition.StatementIndex);
        return true;
    }

    private static List<TempUse> DeduplicateUsesByStatement(List<TempUse> uses)
    {
        var result = new List<TempUse>();
        var seen = new HashSet<int>();

        foreach (TempUse use in uses)
        {
            if (!seen.Add(use.StatementIndex))
                continue;

            result.Add(use);
        }

        return result;
    }

    private static bool IsFoldSafe(
        IReadOnlyList<StatementSyntax> statements,
        ControlFlowGraph cfg,
        TempDefinition definition,
        int useIndex)
    {
        if (!ControlFlowGraphQueries.AreInSameBlock(cfg, definition.StatementIndex, useIndex))
            return false;

        bool effectfulRhs = ExpressionSideEffectClassifier.IsEffectful(definition.Right);
        HashSet<string> readVars = ExpressionSideEffectClassifier.CollectReadVariables(definition.Right);

        for (int i = definition.StatementIndex + 1; i < useIndex; i++)
        {
            StatementSyntax intervening = statements[i];

            if (effectfulRhs && ExpressionSideEffectClassifier.IsEffectful(intervening))
                return false;

            HashSet<string> assigned = ExpressionSideEffectClassifier.CollectAssignedVariables(intervening);
            if (assigned.Overlaps(readVars))
                return false;
        }

        return true;
    }

    private static Dictionary<int, Dictionary<string, HashSet<int>>> ComputeReachingDefinitions(
        IReadOnlyList<StatementSyntax> statements,
        ControlFlowGraph cfg)
    {
        var blockIn = new Dictionary<StatementBlock, Dictionary<string, HashSet<int>>>();
        var blockOut = new Dictionary<StatementBlock, Dictionary<string, HashSet<int>>>();

        foreach (StatementBlock block in cfg.Blocks)
        {
            if (block.IsExit)
                continue;

            blockIn[block] = CreateEmptyReaching();
            blockOut[block] = CreateEmptyReaching();
        }

        var changed = true;
        while (changed)
        {
            changed = false;

            foreach (StatementBlock block in cfg.Blocks)
            {
                if (block.IsExit)
                    continue;

                Dictionary<string, HashSet<int>> incoming = JoinPredecessors(block, blockOut);
                if (!ReachingEquals(blockIn[block], incoming))
                {
                    blockIn[block] = incoming;
                    changed = true;
                }

                Dictionary<string, HashSet<int>> outgoing = TransferBlock(statements, block, incoming);
                if (!ReachingEquals(blockOut[block], outgoing))
                {
                    blockOut[block] = outgoing;
                    changed = true;
                }
            }
        }

        var reachingAt = new Dictionary<int, Dictionary<string, HashSet<int>>>();
        foreach (StatementBlock block in cfg.Blocks)
        {
            if (block.IsExit || block.StatementCount == 0)
                continue;

            Dictionary<string, HashSet<int>> current = CloneReaching(blockIn[block]);
            for (int i = block.InstructionIndex; i < block.EndStatementIndex; i++)
            {
                reachingAt[i] = CloneReaching(current);
                ApplyDefinitionKill(statements, i, current);
            }
        }

        return reachingAt;
    }

    private static Dictionary<string, HashSet<int>> JoinPredecessors(
        StatementBlock block,
        IReadOnlyDictionary<StatementBlock, Dictionary<string, HashSet<int>>> blockOut)
    {
        var result = CreateEmptyReaching();

        foreach (StatementBlock parent in block.Parents)
        {
            if (parent.IsExit || !blockOut.TryGetValue(parent, out Dictionary<string, HashSet<int>>? parentOut))
                continue;

            foreach ((string tempName, HashSet<int> defs) in parentOut)
            {
                if (!result.TryGetValue(tempName, out HashSet<int>? set))
                {
                    set = [];
                    result[tempName] = set;
                }

                set.UnionWith(defs);
            }
        }

        return result;
    }

    private static Dictionary<string, HashSet<int>> TransferBlock(
        IReadOnlyList<StatementSyntax> statements,
        StatementBlock block,
        Dictionary<string, HashSet<int>> incoming)
    {
        Dictionary<string, HashSet<int>> current = CloneReaching(incoming);

        for (int i = block.InstructionIndex; i < block.EndStatementIndex; i++)
            ApplyDefinitionKill(statements, i, current);

        return current;
    }

    private static void ApplyDefinitionKill(
        IReadOnlyList<StatementSyntax> statements,
        int statementIndex,
        Dictionary<string, HashSet<int>> reaching)
    {
        if (!TryGetTempDefinition(statements[statementIndex], statementIndex, out TempDefinition? definition) || definition is null)
            return;

        reaching[definition.TempName] = [definition.StatementIndex];
    }

    private static List<TempDefinition> CollectDefinitions(IReadOnlyList<StatementSyntax> statements)
    {
        var result = new List<TempDefinition>();

        for (var i = 0; i < statements.Count; i++)
        {
            if (TryGetTempDefinition(statements[i], i, out TempDefinition? definition) && definition is not null)
                result.Add(definition);
        }

        return result;
    }

    private static bool TryGetTempDefinition(StatementSyntax statement, int index, out TempDefinition? definition)
    {
        definition = null;

        if (statement is not AssignmentStatementSyntax assignment)
            return false;

        if (assignment.EqualsOperator.RawKind != (int)SyntaxTokenKind.EqualsSign)
            return false;

        if (assignment.Left is not ValueExpressionSyntax { Value: VariableExpressionSyntax variable })
            return false;

        if (!IsTempVariable(variable, out string tempName))
            return false;

        definition = new TempDefinition(tempName, index, assignment, assignment.Right);
        return true;
    }

    private static List<TempUse> CollectUsesForDefinition(
        IReadOnlyList<StatementSyntax> statements,
        TempDefinition definition,
        IReadOnlyDictionary<int, Dictionary<string, HashSet<int>>> reaching)
    {
        var uses = new List<TempUse>();

        for (var i = 0; i < statements.Count; i++)
        {
            foreach (TempUse use in FindTempUses(statements[i], i, definition.TempName))
            {
                // Assignment targets are defs, not reads — including a later `$temp = ...`
                // that kills this value. Counting them caused `Foo() = Bar();`.
                if (use.IsDefiningLeftHandSide)
                    continue;

                if (!reaching.TryGetValue(i, out Dictionary<string, HashSet<int>>? atStatement))
                    continue;

                if (!atStatement.TryGetValue(definition.TempName, out HashSet<int>? defs))
                    continue;

                if (defs.Count != 1 || !defs.Contains(definition.StatementIndex))
                    continue;

                uses.Add(use);
            }
        }

        return uses;
    }

    private static IEnumerable<TempUse> FindTempUses(StatementSyntax statement, int statementIndex, string tempName)
    {
        var uses = new List<TempUse>();

        switch (statement)
        {
            case AssignmentStatementSyntax assignment:
                CollectAssignmentLeftReads(assignment.Left, statementIndex, tempName, uses);
                CollectUses(assignment.Right, statementIndex, tempName, uses, TempExpressionRewriter.FoldContext.None(), false);
                break;

            case IfGotoStatementSyntax ifGoto:
                CollectUses(ifGoto.Value, statementIndex, tempName, uses, TempExpressionRewriter.FoldContext.None(), false);
                break;

            case IfNotGotoStatementSyntax ifNotGoto:
                CollectUses(ifNotGoto.Comparison, statementIndex, tempName, uses,
                    TempExpressionRewriter.FoldContext.ForOperator(ExpressionPrecedence.Unary, true), false);
                break;

            case ReturnStatementSyntax { ValueExpression: not null } returnStatement:
                CollectUses(returnStatement.ValueExpression, statementIndex, tempName, uses,
                    TempExpressionRewriter.FoldContext.None(), false);
                break;

            case GotoStatementSyntax gotoStatement:
                if (gotoStatement.Targets?.Elements != null)
                {
                    foreach (ValueExpressionSyntax target in gotoStatement.Targets.Elements)
                        CollectUses(target, statementIndex, tempName, uses, TempExpressionRewriter.FoldContext.None(), false);
                }
                break;

            case PostfixUnaryStatementSyntax postfix:
                CollectUses(postfix.Expression, statementIndex, tempName, uses,
                    TempExpressionRewriter.FoldContext.ForOperator(ExpressionPrecedence.Postfix, false), false);
                break;

            case MethodInvocationStatementSyntax invocation:
                CollectUsesInParameters(invocation.Parameters, statementIndex, tempName, uses);
                break;
        }

        return uses;
    }

    private static void CollectAssignmentLeftReads(
        ExpressionSyntax left,
        int statementIndex,
        string tempName,
        List<TempUse> uses)
    {
        switch (left)
        {
            case ValueExpressionSyntax { Value: VariableExpressionSyntax }:
                // Bare `$tempN = ...` is a pure def — not a read of the previous value.
                return;

            case ValueExpressionSyntax value:
                CollectAssignmentLeftReads(value.Value, statementIndex, tempName, uses);
                break;

            case ArrayIndexExpressionSyntax arrayIndex:
                // Element store reads the array base and indices.
                CollectUses(arrayIndex.Value, statementIndex, tempName, uses,
                    TempExpressionRewriter.FoldContext.None(), false);
                foreach (ArrayIndexerExpressionSyntax indexer in arrayIndex.Indexer)
                    CollectUses(indexer.Index, statementIndex, tempName, uses,
                        TempExpressionRewriter.FoldContext.None(), false);
                break;

            default:
                CollectUses(left, statementIndex, tempName, uses, TempExpressionRewriter.FoldContext.None(), false);
                break;
        }
    }

    private static void CollectUsesInParameters(
        MethodInvocationParametersSyntax parameters,
        int statementIndex,
        string tempName,
        List<TempUse> uses)
    {
        if (parameters.ParameterList?.Elements is null)
            return;

        foreach (ExpressionSyntax parameter in parameters.ParameterList.Elements)
            CollectUses(parameter, statementIndex, tempName, uses, TempExpressionRewriter.FoldContext.None(), false);
    }

    private static void CollectUses(
        ExpressionSyntax expression,
        int statementIndex,
        string tempName,
        List<TempUse> uses,
        TempExpressionRewriter.FoldContext context,
        bool isDefiningLhs)
    {
        switch (expression)
        {
            case VariableExpressionSyntax variable:
                if (variable.Variable.Text == tempName)
                    uses.Add(new TempUse(tempName, statementIndex, context, isDefiningLhs));
                break;

            case ValueExpressionSyntax value:
                CollectUses(value.Value, statementIndex, tempName, uses, context, isDefiningLhs);
                break;

            case ParenthesizedExpressionSyntax parenthesized:
                CollectUses(parenthesized.Expression, statementIndex, tempName, uses,
                    TempExpressionRewriter.FoldContext.None(), false);
                break;

            case UnaryExpressionSyntax unary:
                CollectUses(unary.Value, statementIndex, tempName, uses,
                    TempExpressionRewriter.FoldContext.ForOperator(ExpressionPrecedence.Unary, true), false);
                break;

            case BinaryExpressionSyntax binary:
                int binaryPrec = ExpressionPrecedence.GetOperatorPrecedence((SyntaxTokenKind)binary.Operation.RawKind)
                                 ?? ExpressionPrecedence.Primary;
                CollectUses(binary.Left, statementIndex, tempName, uses,
                    TempExpressionRewriter.FoldContext.ForOperator(binaryPrec, false), false);
                CollectUses(binary.Right, statementIndex, tempName, uses,
                    TempExpressionRewriter.FoldContext.ForOperator(binaryPrec, true), false);
                break;

            case AssignmentExpressionSyntax assignment:
                CollectAssignmentLeftReads(assignment.Left, statementIndex, tempName, uses);
                CollectUses(assignment.Right, statementIndex, tempName, uses,
                    TempExpressionRewriter.FoldContext.None(), false);
                break;

            case LogicalExpressionSyntax logical:
                int logicalPrec = ExpressionPrecedence.GetOperatorPrecedence((SyntaxTokenKind)logical.Operation.RawKind)
                                  ?? ExpressionPrecedence.Primary;
                CollectUses(logical.Left, statementIndex, tempName, uses,
                    TempExpressionRewriter.FoldContext.ForOperator(logicalPrec, false), false);
                CollectUses(logical.Right, statementIndex, tempName, uses,
                    TempExpressionRewriter.FoldContext.ForOperator(logicalPrec, true), false);
                break;

            case MethodInvocationExpressionSyntax invocation:
                CollectUsesInParameters(invocation.Parameters, statementIndex, tempName, uses);
                break;

            case PostfixUnaryExpressionSyntax postfix:
                CollectUses(postfix.Value, statementIndex, tempName, uses,
                    TempExpressionRewriter.FoldContext.ForOperator(ExpressionPrecedence.Postfix, false), false);
                break;

            case ArrayIndexExpressionSyntax arrayIndex:
                CollectUses(arrayIndex.Value, statementIndex, tempName, uses,
                    TempExpressionRewriter.FoldContext.None(), isDefiningLhs);
                foreach (ArrayIndexerExpressionSyntax indexer in arrayIndex.Indexer)
                    CollectUses(indexer.Index, statementIndex, tempName, uses,
                        TempExpressionRewriter.FoldContext.None(), false);
                break;

            case TypeCastValueExpressionSyntax typeCast:
                CollectUses(typeCast.Value, statementIndex, tempName, uses,
                    TempExpressionRewriter.FoldContext.ForOperator(ExpressionPrecedence.Unary, true), false);
                break;

            case SwitchExpressionSyntax switchExpression:
                CollectUses(switchExpression.Value, statementIndex, tempName, uses,
                    TempExpressionRewriter.FoldContext.None(), false);
                break;
        }
    }

    private static bool IsTempVariable(VariableExpressionSyntax variable, out string tempName)
    {
        tempName = string.Empty;
        string text = variable.Variable.Text;
        if (!text.StartsWith("$temp", StringComparison.Ordinal))
            return false;

        tempName = text;
        return true;
    }

    private static Dictionary<string, HashSet<int>> CreateEmptyReaching() => new(StringComparer.Ordinal);

    private static Dictionary<string, HashSet<int>> CloneReaching(Dictionary<string, HashSet<int>> source)
    {
        var clone = CreateEmptyReaching();
        foreach ((string key, HashSet<int> value) in source)
            clone[key] = [.. value];
        return clone;
    }

    private static bool ReachingEquals(Dictionary<string, HashSet<int>> left, Dictionary<string, HashSet<int>> right)
    {
        if (left.Count != right.Count)
            return false;

        foreach ((string key, HashSet<int> leftDefs) in left)
        {
            if (!right.TryGetValue(key, out HashSet<int>? rightDefs))
                return false;

            if (!leftDefs.SetEquals(rightDefs))
                return false;
        }

        return true;
    }

    private sealed record TempDefinition(
        string TempName,
        int StatementIndex,
        AssignmentStatementSyntax Statement,
        ExpressionSyntax Right);

    private sealed record TempUse(
        string TempName,
        int StatementIndex,
        TempExpressionRewriter.FoldContext Context,
        bool IsDefiningLeftHandSide);
}
