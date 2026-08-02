using Logic.Business.Level5ScriptManagement.DataClasses.Conversion;
using Logic.Business.Level5ScriptManagement.InternalContract.Conversion.HighLevelSyntax;
using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

namespace Logic.Business.Level5ScriptManagement.Conversion.HighLevelSyntax.Cfg;

/// <summary>
/// Builds a dominator tree and recovers natural loops plus single-entry branch regions.
/// </summary>
internal class ControlFlowRegionAnalyzer : IControlFlowRegionAnalyzer
{
    public ControlFlowRegions Analyze(ControlFlowGraph cfg)
    {
        DominatorTree dominators = BuildDominatorTree(cfg);
        IReadOnlyList<NaturalLoop> loops = FindNaturalLoops(cfg, dominators);
        IReadOnlyList<BranchRegion> branches = FindBranchRegions(cfg, dominators, loops);

        return new ControlFlowRegions
        {
            Dominators = dominators,
            Loops = loops,
            Branches = branches
        };
    }

    private static DominatorTree BuildDominatorTree(ControlFlowGraph cfg)
    {
        List<StatementBlock> blocks = cfg.Blocks.Where(b => !b.IsExit).ToList();
        StatementBlock entry = cfg.Entry;

        // Iterative data-flow (Cooper/Harvey/Kennedy style on the full dom sets, then derive idom).
        var dom = new Dictionary<StatementBlock, HashSet<StatementBlock>>();
        HashSet<StatementBlock> all = blocks.ToHashSet();

        foreach (StatementBlock block in blocks)
            dom[block] = ReferenceEquals(block, entry) ? [entry] : [.. all];

        var changed = true;
        while (changed)
        {
            changed = false;

            foreach (StatementBlock block in blocks)
            {
                if (ReferenceEquals(block, entry))
                    continue;

                List<StatementBlock> preds = GetPredecessors(cfg, block)
                    .Where(p => !p.IsExit && dom.ContainsKey(p))
                    .ToList();

                HashSet<StatementBlock> next;
                if (preds.Count == 0)
                {
                    next = [block];
                }
                else
                {
                    next = new HashSet<StatementBlock>(dom[preds[0]]);
                    for (var i = 1; i < preds.Count; i++)
                        next.IntersectWith(dom[preds[i]]);
                    next.Add(block);
                }

                if (!next.SetEquals(dom[block]))
                {
                    dom[block] = next;
                    changed = true;
                }
            }
        }

        var idom = new Dictionary<StatementBlock, StatementBlock?>();
        foreach (StatementBlock block in blocks)
        {
            if (ReferenceEquals(block, entry))
            {
                idom[block] = null;
                continue;
            }

            // idom(n) = unique strict dominator of n dominated by every other strict dominator.
            List<StatementBlock> strict = dom[block].Where(d => !ReferenceEquals(d, block)).ToList();
            StatementBlock? immediate = strict.FirstOrDefault(candidate =>
                strict.All(other =>
                    ReferenceEquals(other, candidate) || dom[candidate].Contains(other)));

            // Fallback: deepest strict dominator (largest dom set).
            immediate ??= strict.OrderByDescending(d => dom[d].Count).FirstOrDefault();
            idom[block] = immediate;
        }

        var children = blocks.ToDictionary(
            b => b,
            _ => new List<StatementBlock>());

        foreach ((StatementBlock block, StatementBlock? parent) in idom)
        {
            if (parent is not null)
                children[parent].Add(block);
        }

        return new DominatorTree
        {
            Entry = entry,
            ImmediateDominator = idom,
            Children = children.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<StatementBlock>)pair.Value)
        };
    }

    private static IReadOnlyList<NaturalLoop> FindNaturalLoops(ControlFlowGraph cfg, DominatorTree dominators)
    {
        var loopsByHeader = new Dictionary<StatementBlock, NaturalLoopAccumulator>();

        foreach (ControlFlowEdge edge in cfg.Edges)
        {
            if (edge.Source.IsExit || edge.Target.IsExit)
                continue;

            if (edge.Kind is not (ControlFlowEdgeKind.Branch or ControlFlowEdgeKind.Jump or ControlFlowEdgeKind.FallThrough))
                continue;

            // Back-edge: target dominates source.
            if (!dominators.Dominates(edge.Target, edge.Source))
                continue;

            StatementBlock header = edge.Target;
            HashSet<StatementBlock> body = CollectLoopBody(cfg, header, edge.Source);

            if (!loopsByHeader.TryGetValue(header, out NaturalLoopAccumulator? acc))
            {
                acc = new NaturalLoopAccumulator(header);
                loopsByHeader[header] = acc;
            }

            acc.Body.UnionWith(body);
            acc.Latches.Add(edge.Source);
            acc.BackEdges.Add(edge);
        }

        var result = new List<NaturalLoop>();
        foreach (NaturalLoopAccumulator acc in loopsByHeader.Values)
        {
            StatementBlock? loopExit = FindUniqueHeaderExit(cfg, acc.Header, acc.Body);
            // Break/continue-out blocks never reach a latch, so the classic natural-loop
            // body omits them. Expand with header-dominated escapes that only leave via
            // the loop exit (or stay inside) so structured raising sees the full while body.
            if (loopExit is not null)
                ExpandLoopBodyWithEscapes(cfg, dominators, acc.Header, acc.Body, loopExit);

            result.Add(new NaturalLoop
            {
                Header = acc.Header,
                Latches = acc.Latches.Distinct().ToList(),
                Body = acc.Body,
                BackEdges = acc.BackEdges,
                LoopExit = loopExit
            });
        }

        // Innermost first: smaller bodies before larger (nested loops raise inside-out).
        return result
            .OrderBy(l => l.Body.Count)
            .ThenBy(l => l.Header.InstructionIndex)
            .ToList();
    }

    private static HashSet<StatementBlock> CollectLoopBody(
        ControlFlowGraph cfg,
        StatementBlock header,
        StatementBlock latch)
    {
        var body = new HashSet<StatementBlock> { header };
        var stack = new Stack<StatementBlock>();
        stack.Push(latch);

        while (stack.Count > 0)
        {
            StatementBlock block = stack.Pop();
            if (!body.Add(block))
                continue;

            foreach (StatementBlock pred in GetPredecessors(cfg, block))
            {
                if (pred.IsExit)
                    continue;

                if (!body.Contains(pred))
                    stack.Push(pred);
            }
        }

        return body;
    }

    private static StatementBlock? FindUniqueHeaderExit(
        ControlFlowGraph cfg,
        StatementBlock header,
        IReadOnlySet<StatementBlock> body)
    {
        List<StatementBlock> exits = ControlFlowGraphQueries.GetOutgoing(cfg, header)
            .Select(e => e.Target)
            .Where(t => !t.IsExit && !body.Contains(t))
            .Distinct()
            .ToList();

        return exits.Count == 1 ? exits[0] : null;
    }

    /// <summary>
    /// Adds blocks dominated by <paramref name="header"/> that are not on a path to any
    /// latch (e.g. <c>break</c> arms) but only succeed into the loop body or
    /// <paramref name="loopExit"/>.
    /// </summary>
    private static void ExpandLoopBodyWithEscapes(
        ControlFlowGraph cfg,
        DominatorTree dominators,
        StatementBlock header,
        HashSet<StatementBlock> body,
        StatementBlock loopExit)
    {
        var changed = true;
        while (changed)
        {
            changed = false;

            foreach (StatementBlock block in cfg.Blocks)
            {
                if (block.IsExit || body.Contains(block) || ReferenceEquals(block, loopExit))
                    continue;

                if (!dominators.StrictlyDominates(header, block))
                    continue;

                List<ControlFlowEdge> outgoing = ControlFlowGraphQueries.GetOutgoing(cfg, block).ToList();
                if (outgoing.Count == 0)
                    continue;

                if (!outgoing.All(e =>
                        body.Contains(e.Target) ||
                        ReferenceEquals(e.Target, loopExit) ||
                        ReferenceEquals(e.Target, block)))
                    continue;

                body.Add(block);
                changed = true;
            }
        }
    }

    private static IReadOnlyList<BranchRegion> FindBranchRegions(
        ControlFlowGraph cfg,
        DominatorTree dominators,
        IReadOnlyList<NaturalLoop> loops)
    {
        var regions = new List<BranchRegion>();

        foreach (StatementBlock header in cfg.Blocks)
        {
            if (header.IsExit || header.StatementCount <= 0)
                continue;

            // Skip headers that are natural-loop headers with a back-edge in this block —
            // those belong to loop recovery, not branch recovery.
            if (loops.Any(l => ReferenceEquals(l.Header, header) && l.Body.Count > 0 &&
                               l.BackEdges.Any(e => ReferenceEquals(e.Source, header))))
            {
                // Self-loop / while header: still may look like a conditional; exclude when
                // the branch target is outside and fallthrough stays in-loop (while), or
                // self-branch (spin). Those are handled as loops.
                if (IsLoopHeaderConditional(cfg, header, loops))
                    continue;
            }

            if (!TryGetConditionalSuccessors(cfg, header, out StatementBlock? fallThrough, out StatementBlock? branchTarget) ||
                fallThrough is null ||
                branchTarget is null)
                continue;

            if (TryCreateIfElseRegion(cfg, dominators, header, fallThrough, branchTarget, out BranchRegion? ifElse) &&
                ifElse is not null)
            {
                regions.Add(ifElse);
                continue;
            }

            if (TryCreateIfThenRegion(cfg, dominators, header, fallThrough, branchTarget, out BranchRegion? ifThen) &&
                ifThen is not null)
            {
                regions.Add(ifThen);
            }
        }

        return regions
            .OrderBy(r => r.ThenBlocks.Count + r.ElseBlocks.Count)
            .ThenBy(r => r.Header.InstructionIndex)
            .ToList();
    }

    private static bool IsLoopHeaderConditional(
        ControlFlowGraph cfg,
        StatementBlock header,
        IReadOnlyList<NaturalLoop> loops)
    {
        NaturalLoop? loop = loops.FirstOrDefault(l => ReferenceEquals(l.Header, header));
        if (loop is null)
            return false;

        // Spin: back-edge to self.
        if (loop.BackEdges.Any(e => ReferenceEquals(e.Source, header) && ReferenceEquals(e.Target, header)))
            return true;

        // Top-tested while: header exits the loop via a branch.
        if (loop.LoopExit is not null &&
            ControlFlowGraphQueries.HasBranchTo(cfg, header, loop.LoopExit))
            return true;

        return false;
    }

    private static bool TryCreateIfThenRegion(
        ControlFlowGraph cfg,
        DominatorTree dominators,
        StatementBlock header,
        StatementBlock thenEntry,
        StatementBlock join,
        out BranchRegion? region)
    {
        region = null;

        if (!IsNumericLabelBlock(cfg, join))
            return false;

        if (!dominators.Dominates(header, thenEntry) && !ReferenceEquals(header, thenEntry))
            return false;

        // Then-entry must be the fall-through successor (dominated / immediately after header).
        if (!ControlFlowGraphQueries.HasFallThrough(cfg, header))
            return false;

        ControlFlowEdge? fallEdge = ControlFlowGraphQueries.GetOutgoing(cfg, header)
            .FirstOrDefault(e => e.Kind == ControlFlowEdgeKind.FallThrough);
        if (fallEdge is null || !ReferenceEquals(fallEdge.Target, thenEntry))
            return false;

        if (!ControlFlowGraphQueries.HasBranchTo(cfg, header, join))
            return false;

        HashSet<StatementBlock> thenBlocks = CollectForwardRegion(cfg, dominators, header, thenEntry, join);
        if (!IsSingleEntryRegion(cfg, thenBlocks, header, thenEntry))
            return false;

        // Join must not be part of the then arm.
        if (thenBlocks.Contains(join))
            return false;

        // No jump from then-arm to anywhere except join / internal (validated later by pass).
        region = new BranchRegion
        {
            Header = header,
            ThenEntry = thenEntry,
            ElseEntry = null,
            Join = join,
            Kind = BranchRegionKind.IfThen,
            ThenBlocks = thenBlocks,
            ElseBlocks = new HashSet<StatementBlock>()
        };
        return true;
    }

    private static bool TryCreateIfElseRegion(
        ControlFlowGraph cfg,
        DominatorTree dominators,
        StatementBlock header,
        StatementBlock thenEntry,
        StatementBlock elseEntry,
        out BranchRegion? region)
    {
        region = null;

        if (!IsNumericLabelBlock(cfg, elseEntry))
            return false;

        if (!ControlFlowGraphQueries.HasFallThrough(cfg, header) ||
            !ControlFlowGraphQueries.HasBranchTo(cfg, header, elseEntry))
            return false;

        ControlFlowEdge? fallEdge = ControlFlowGraphQueries.GetOutgoing(cfg, header)
            .FirstOrDefault(e => e.Kind == ControlFlowEdgeKind.FallThrough);
        if (fallEdge is null || !ReferenceEquals(fallEdge.Target, thenEntry))
            return false;

        // Then arm must end with an unconditional jump to a numeric join that the else arm reaches.
        if (!TryFindThenJoin(cfg, thenEntry, elseEntry, header, dominators, out StatementBlock? join, out HashSet<StatementBlock>? thenBlocks) ||
            join is null ||
            thenBlocks is null)
            return false;

        if (!IsNumericLabelBlock(cfg, join))
            return false;

        if (!dominators.Dominates(header, elseEntry) && !ReferenceEquals(header, elseEntry))
            return false;

        HashSet<StatementBlock> elseBlocks = CollectForwardRegion(cfg, dominators, header, elseEntry, join);
        if (!IsSingleEntryRegion(cfg, elseBlocks, header, elseEntry))
            return false;

        if (thenBlocks.Overlaps(elseBlocks))
            return false;

        if (thenBlocks.Contains(join) || elseBlocks.Contains(join))
            return false;

        // Else arm must not jump to elseEntry from outside; single branch from header already required.
        if (ControlFlowLabels.CountLabelReferences(cfg.Statements, GetPrimaryNumericLabel(cfg, elseEntry)!) != 1)
            return false;

        region = new BranchRegion
        {
            Header = header,
            ThenEntry = thenEntry,
            ElseEntry = elseEntry,
            Join = join,
            Kind = BranchRegionKind.IfElse,
            ThenBlocks = thenBlocks,
            ElseBlocks = elseBlocks
        };
        return true;
    }

    private static bool TryFindThenJoin(
        ControlFlowGraph cfg,
        StatementBlock thenEntry,
        StatementBlock elseEntry,
        StatementBlock header,
        DominatorTree dominators,
        out StatementBlock? join,
        out HashSet<StatementBlock>? thenBlocks)
    {
        join = null;
        thenBlocks = null;

        // Walk linearly from thenEntry until elseEntry's statement range; the block before else
        // should end with goto join (classic lowered if/else).
        if (!cfg.LabelStatementIndex.Values.Any())
            return false;

        string? elseLabel = GetPrimaryNumericLabel(cfg, elseEntry);
        if (elseLabel is null || !cfg.LabelStatementIndex.TryGetValue(elseLabel, out int elseIndex))
            return false;

        if (elseIndex <= 0)
            return false;

        if (cfg.Statements[elseIndex - 1] is not GotoStatementSyntax gotoStatement ||
            !ControlFlowLabels.TryGetSingleGotoTarget(gotoStatement, out string? joinLabel) ||
            joinLabel is null ||
            !ControlFlowLabels.IsNumericJumpLabel(joinLabel) ||
            !cfg.BlockByLabel.TryGetValue(joinLabel, out StatementBlock? joinBlock))
            return false;

        if (!cfg.BlockByStatementIndex.TryGetValue(elseIndex - 1, out StatementBlock? thenTail))
            return false;

        if (!ControlFlowGraphQueries.HasBranchTo(cfg, thenTail, joinBlock))
            return false;

        HashSet<StatementBlock> blocks = CollectForwardRegion(cfg, dominators, header, thenEntry, elseEntry);
        // Include thenTail / exclude elseEntry.
        blocks.Remove(elseEntry);
        if (!blocks.Contains(thenTail) && thenTail.InstructionIndex < elseEntry.InstructionIndex)
            blocks.Add(thenTail);

        if (!IsSingleEntryRegion(cfg, blocks, header, thenEntry))
            return false;

        join = joinBlock;
        thenBlocks = blocks;
        return true;
    }

    /// <summary>
    /// Blocks reachable from <paramref name="entry"/> without passing through <paramref name="stop"/>,
    /// restricted to nodes dominated by <paramref name="header"/> (or the entry itself).
    /// </summary>
    private static HashSet<StatementBlock> CollectForwardRegion(
        ControlFlowGraph cfg,
        DominatorTree dominators,
        StatementBlock header,
        StatementBlock entry,
        StatementBlock stop)
    {
        var result = new HashSet<StatementBlock>();
        var stack = new Stack<StatementBlock>();
        stack.Push(entry);

        while (stack.Count > 0)
        {
            StatementBlock block = stack.Pop();
            if (ReferenceEquals(block, stop) || block.IsExit)
                continue;

            if (!dominators.Dominates(header, block) && !ReferenceEquals(block, entry))
                continue;

            if (!result.Add(block))
                continue;

            foreach (ControlFlowEdge edge in ControlFlowGraphQueries.GetOutgoing(cfg, block))
            {
                if (ReferenceEquals(edge.Target, stop) || edge.Target.IsExit)
                    continue;

                stack.Push(edge.Target);
            }
        }

        return result;
    }

    private static bool IsSingleEntryRegion(
        ControlFlowGraph cfg,
        IReadOnlySet<StatementBlock> region,
        StatementBlock header,
        StatementBlock entry)
    {
        foreach (StatementBlock block in region)
        {
            foreach (StatementBlock pred in GetPredecessors(cfg, block))
            {
                if (pred.IsExit)
                    continue;

                if (region.Contains(pred))
                    continue;

                // Only the header may enter the region (typically into entry).
                if (!ReferenceEquals(pred, header))
                    return false;

                if (!ReferenceEquals(block, entry))
                    return false;
            }
        }

        return true;
    }

    private static bool TryGetConditionalSuccessors(
        ControlFlowGraph cfg,
        StatementBlock header,
        out StatementBlock? fallThrough,
        out StatementBlock? branchTarget)
    {
        fallThrough = null;
        branchTarget = null;

        if (!ControlFlowGraphQueries.TryGetTerminator(cfg, header, out StatementSyntax? terminator) ||
            terminator is null)
            return false;

        if (terminator is not (IfGotoStatementSyntax or IfNotGotoStatementSyntax))
            return false;

        foreach (ControlFlowEdge edge in ControlFlowGraphQueries.GetOutgoing(cfg, header))
        {
            if (edge.Kind == ControlFlowEdgeKind.FallThrough)
                fallThrough = edge.Target;
            else if (edge.Kind == ControlFlowEdgeKind.Branch)
                branchTarget = edge.Target;
        }

        return fallThrough is not null && branchTarget is not null;
    }

    private static bool IsNumericLabelBlock(ControlFlowGraph cfg, StatementBlock block)
    {
        return block.Labels.Any(ControlFlowLabels.IsNumericJumpLabel);
    }

    private static string? GetPrimaryNumericLabel(ControlFlowGraph cfg, StatementBlock block)
    {
        return block.Labels.FirstOrDefault(ControlFlowLabels.IsNumericJumpLabel);
    }

    private static IEnumerable<StatementBlock> GetPredecessors(ControlFlowGraph cfg, StatementBlock block)
    {
        return ControlFlowGraphQueries.GetIncoming(cfg, block).Select(e => e.Source);
    }

    private sealed class NaturalLoopAccumulator(StatementBlock header)
    {
        public StatementBlock Header { get; } = header;
        public HashSet<StatementBlock> Body { get; } = [];
        public List<StatementBlock> Latches { get; } = [];
        public List<ControlFlowEdge> BackEdges { get; } = [];
    }
}
