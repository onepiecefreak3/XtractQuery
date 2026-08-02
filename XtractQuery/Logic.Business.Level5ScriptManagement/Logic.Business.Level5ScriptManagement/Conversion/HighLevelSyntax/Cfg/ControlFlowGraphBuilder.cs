using Logic.Business.Level5ScriptManagement.DataClasses.Conversion;
using Logic.Business.Level5ScriptManagement.InternalContract.Conversion.HighLevelSyntax;
using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

namespace Logic.Business.Level5ScriptManagement.Conversion.HighLevelSyntax.Cfg;

internal class ControlFlowGraphBuilder : IControlFlowGraphBuilder
{
    public ControlFlowGraph Build(IReadOnlyList<StatementSyntax> statements)
    {
        var exit = new StatementBlock { IsExit = true };
        if (statements.Count == 0)
        {
            var emptyEntry = new StatementBlock { InstructionIndex = 0, StatementCount = 0 };
            ControlFlowEdge emptyEdge = Connect(emptyEntry, exit, ControlFlowEdgeKind.FallThrough);
            return CreateGraph(
                emptyEntry,
                exit,
                [emptyEntry, exit],
                [emptyEdge],
                new Dictionary<int, StatementBlock>(),
                statements,
                new Dictionary<string, StatementBlock>(),
                new Dictionary<string, int>());
        }
        HashSet<int> leaders = CollectLeaders(statements);
        List<StatementBlock> blocks = CreateBlocks(statements, leaders);
        Dictionary<string, StatementBlock> labels = CollectLabels(blocks);
        Dictionary<string, int> labelIndexes = CollectLabelIndexes(statements);
        Dictionary<int, StatementBlock> byIndex = IndexBlocks(blocks);
        List<ControlFlowEdge> edges = ConnectBlocks(statements, blocks, labels, exit);
        return CreateGraph(blocks[0], exit, [.. blocks, exit], edges, byIndex, statements, labels, labelIndexes);
    }
    private static HashSet<int> CollectLeaders(IReadOnlyList<StatementSyntax> statements)
    {
        var leaders = new HashSet<int> { 0 };
        for (var i = 0; i < statements.Count; i++)
        {
            StatementSyntax statement = statements[i];
            if (statement is GotoLabelStatementSyntax)
                leaders.Add(i);
            if (!IsTerminator(statement))
                continue;
            int next = i + 1;
            if (next < statements.Count)
                leaders.Add(next);
        }
        return leaders;
    }
    private static List<StatementBlock> CreateBlocks(IReadOnlyList<StatementSyntax> statements, HashSet<int> leaders)
    {
        List<int> orderedLeaders = leaders.OrderBy(l => l).ToList();
        var blocks = new List<StatementBlock>();
        for (var i = 0; i < orderedLeaders.Count; i++)
        {
            int start = orderedLeaders[i];
            int end = i + 1 < orderedLeaders.Count ? orderedLeaders[i + 1] : statements.Count;
            var block = new StatementBlock
            {
                InstructionIndex = start,
                StatementCount = end - start
            };
            for (int s = start; s < end; s++)
            {
                StatementSyntax statement = statements[s];
                block.Statements.Add(statement);
                if (ControlFlowLabels.TryGetLabelDefinition(statement, out string? label) && label is not null)
                    block.Labels.Add(label);
            }
            blocks.Add(block);
        }
        return blocks;
    }
    private static Dictionary<string, StatementBlock> CollectLabels(IReadOnlyList<StatementBlock> blocks)
    {
        var labels = new Dictionary<string, StatementBlock>(StringComparer.Ordinal);
        foreach (StatementBlock block in blocks)
        {
            foreach (string label in block.Labels)
                labels[label] = block;
        }
        return labels;
    }
    private static Dictionary<string, int> CollectLabelIndexes(IReadOnlyList<StatementSyntax> statements)
    {
        var indexes = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < statements.Count; i++)
        {
            if (ControlFlowLabels.TryGetLabelDefinition(statements[i], out string? label) && label is not null)
                indexes[label] = i;
        }
        return indexes;
    }
    private static Dictionary<int, StatementBlock> IndexBlocks(IReadOnlyList<StatementBlock> blocks)
    {
        var byIndex = new Dictionary<int, StatementBlock>();
        foreach (StatementBlock block in blocks)
        {
            for (int i = block.InstructionIndex; i < block.EndStatementIndex; i++)
                byIndex[i] = block;
        }
        return byIndex;
    }
    private static List<ControlFlowEdge> ConnectBlocks(
        IReadOnlyList<StatementSyntax> statements,
        IReadOnlyList<StatementBlock> blocks,
        IReadOnlyDictionary<string, StatementBlock> labels,
        StatementBlock exit)
    {
        var edges = new List<ControlFlowEdge>();
        Dictionary<int, StatementBlock> blockByStart = blocks.ToDictionary(b => b.InstructionIndex);
        foreach (StatementBlock block in blocks)
        {
            if (block.StatementCount == 0)
            {
                edges.Add(Connect(block, exit, ControlFlowEdgeKind.FallThrough));
                continue;
            }
            int lastIndex = block.EndStatementIndex - 1;
            StatementSyntax last = statements[lastIndex];
            switch (last)
            {
                case ReturnStatementSyntax:
                case ExitStatementSyntax:
                    edges.Add(Connect(block, exit, ControlFlowEdgeKind.Exit));
                    break;
                case GotoStatementSyntax gotoStatement:
                    AddJumpEdges(block, gotoStatement, labels, exit, edges);
                    break;
                case IfGotoStatementSyntax ifGoto:
                    AddBranchEdge(block, ifGoto.Goto.Target, labels, exit, edges);
                    AddFallThrough(block, lastIndex, statements.Count, blockByStart, edges);
                    break;
                case IfNotGotoStatementSyntax ifNotGoto:
                    AddBranchEdge(block, ifNotGoto.Goto.Target, labels, exit, edges);
                    AddFallThrough(block, lastIndex, statements.Count, blockByStart, edges);
                    break;
                default:
                    AddFallThrough(block, lastIndex, statements.Count, blockByStart, edges);
                    break;
            }
        }
        return edges;
    }
    private static void AddJumpEdges(
        StatementBlock block,
        GotoStatementSyntax gotoStatement,
        IReadOnlyDictionary<string, StatementBlock> labels,
        StatementBlock exit,
        List<ControlFlowEdge> edges)
    {
        if (gotoStatement.Targets?.Elements is null)
        {
            edges.Add(Connect(block, exit, ControlFlowEdgeKind.Jump));
            return;
        }
        foreach (ValueExpressionSyntax target in gotoStatement.Targets.Elements)
            AddBranchEdge(block, target, labels, exit, edges, ControlFlowEdgeKind.Jump);
    }
    private static void AddBranchEdge(
        StatementBlock block,
        ValueExpressionSyntax target,
        IReadOnlyDictionary<string, StatementBlock> labels,
        StatementBlock exit,
        List<ControlFlowEdge> edges,
        ControlFlowEdgeKind kind = ControlFlowEdgeKind.Branch)
    {
        if (!ControlFlowLabels.TryGetLabelName(target, out string? labelName) ||
            labelName is null ||
            !labels.TryGetValue(labelName, out StatementBlock? targetBlock))
        {
            edges.Add(Connect(block, exit, kind));
            return;
        }
        edges.Add(Connect(block, targetBlock, kind));
    }
    private static void AddFallThrough(
        StatementBlock block,
        int lastIndex,
        int statementCount,
        IReadOnlyDictionary<int, StatementBlock> blockByStart,
        List<ControlFlowEdge> edges)
    {
        int next = lastIndex + 1;
        if (next >= statementCount || !blockByStart.TryGetValue(next, out StatementBlock? nextBlock))
            return;
        edges.Add(Connect(block, nextBlock, ControlFlowEdgeKind.FallThrough));
    }
    private static ControlFlowEdge Connect(StatementBlock source, StatementBlock target, ControlFlowEdgeKind kind)
    {
        source.Children.Add(target);
        target.Parents.Add(source);
        return new ControlFlowEdge
        {
            Source = source,
            Target = target,
            Kind = kind
        };
    }
    private static ControlFlowGraph CreateGraph(
        StatementBlock entry,
        StatementBlock exit,
        IReadOnlyList<StatementBlock> blocks,
        IReadOnlyList<ControlFlowEdge> edges,
        IReadOnlyDictionary<int, StatementBlock> byIndex,
        IReadOnlyList<StatementSyntax> statements,
        IReadOnlyDictionary<string, StatementBlock> labels,
        IReadOnlyDictionary<string, int> labelIndexes)
    {
        var outgoing = new Dictionary<StatementBlock, List<ControlFlowEdge>>();
        var incoming = new Dictionary<StatementBlock, List<ControlFlowEdge>>();
        foreach (StatementBlock block in blocks)
        {
            outgoing[block] = [];
            incoming[block] = [];
        }
        foreach (ControlFlowEdge edge in edges)
        {
            outgoing[edge.Source].Add(edge);
            incoming[edge.Target].Add(edge);
        }
        return new ControlFlowGraph
        {
            Entry = entry,
            Exit = exit,
            Blocks = blocks,
            Edges = edges,
            BlockByStatementIndex = byIndex,
            Statements = statements,
            BlockByLabel = labels,
            LabelStatementIndex = labelIndexes,
            OutgoingEdges = outgoing.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<ControlFlowEdge>)pair.Value),
            IncomingEdges = incoming.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<ControlFlowEdge>)pair.Value)
        };
    }
    private static bool IsTerminator(StatementSyntax statement)
    {
        return statement is GotoStatementSyntax
            or IfGotoStatementSyntax
            or IfNotGotoStatementSyntax
            or ReturnStatementSyntax
            or ExitStatementSyntax;
    }
}
