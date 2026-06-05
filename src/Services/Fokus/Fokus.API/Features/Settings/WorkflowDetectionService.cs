using Fokus.Persistence.Repositories;

namespace Fokus.API.Features.Settings;

public record WorkflowDetectionResult(
    List<string> Stages,
    List<string> Sidelined,
    int TransitionCount,
    int TicketCount,
    int SprintCount);

public class WorkflowDetectionService
{
    public WorkflowDetectionResult Detect(TransitionEdgeData data, List<string> doneStatuses)
    {
        if (data.Edges.Count == 0)
        {
            return new WorkflowDetectionResult(
                new List<string>(),
                new List<string>(),
                0, 0, 0);
        }

        // Collect all unique statuses from edges
        var allStatuses = new HashSet<string>();
        foreach (var edge in data.Edges)
        {
            allStatuses.Add(edge.FromStatus);
            allStatuses.Add(edge.ToStatus);
        }

        var doneSet = new HashSet<string>(doneStatuses, StringComparer.OrdinalIgnoreCase);

        // Separate terminal (done) and non-terminal statuses
        var terminalStatuses = allStatuses.Where(s => doneSet.Contains(s)).ToList();
        var nonTerminalStatuses = allStatuses.Where(s => !doneSet.Contains(s)).ToList();

        // Build adjacency: for each pair (A, B), keep only dominant direction
        // Build a dict of (from, to) -> count for quick lookup
        var edgeMap = new Dictionary<(string From, string To), int>();
        foreach (var edge in data.Edges)
        {
            edgeMap[(edge.FromStatus, edge.ToStatus)] = edge.Count;
        }

        // Resolve cycles: for each bidirectional pair, suppress the weaker direction
        var suppressedEdges = new HashSet<(string From, string To)>();
        var processedPairs = new HashSet<(string A, string B)>();

        foreach (var edge in data.Edges)
        {
            var pair = edge.FromStatus.CompareTo(edge.ToStatus) < 0
                ? (edge.FromStatus, edge.ToStatus)
                : (edge.ToStatus, edge.FromStatus);

            if (processedPairs.Contains(pair))
                continue;
            processedPairs.Add(pair);

            var forwardCount = edgeMap.GetValueOrDefault((edge.FromStatus, edge.ToStatus), 0);
            var reverseCount = edgeMap.GetValueOrDefault((edge.ToStatus, edge.FromStatus), 0);

            if (forwardCount > 0 && reverseCount > 0)
            {
                // Suppress the weaker direction
                if (reverseCount <= forwardCount)
                    suppressedEdges.Add((edge.ToStatus, edge.FromStatus));
                else
                    suppressedEdges.Add((edge.FromStatus, edge.ToStatus));
            }
        }

        // Build the dominant DAG (excluding suppressed reverse edges)
        var successors = new Dictionary<string, List<(string Status, int Weight)>>();
        var predecessors = new Dictionary<string, List<string>>();
        foreach (var s in allStatuses)
        {
            successors[s] = new List<(string, int)>();
            predecessors[s] = new List<string>();
        }

        foreach (var edge in data.Edges)
        {
            if (suppressedEdges.Contains((edge.FromStatus, edge.ToStatus)))
                continue;
            successors[edge.FromStatus].Add((edge.ToStatus, edge.Count));
            predecessors[edge.ToStatus].Add(edge.FromStatus);
        }

        // Score each non-terminal status by reachability to terminals
        // Use BFS/DFS from each status; if it can reach a terminal, it's a main-path candidate
        var canReachTerminal = new HashSet<string>();
        foreach (var terminal in terminalStatuses)
            canReachTerminal.Add(terminal);

        // Work backwards: any status with an edge to a terminal-reachable status is also reachable
        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (var edge in data.Edges)
            {
                if (suppressedEdges.Contains((edge.FromStatus, edge.ToStatus)))
                    continue;
                if (canReachTerminal.Contains(edge.ToStatus) && !canReachTerminal.Contains(edge.FromStatus))
                {
                    canReachTerminal.Add(edge.FromStatus);
                    changed = true;
                }
            }
        }

        // Statuses that cannot reach a terminal are sidelined
        var sidelined = new List<string>();
        var mainCandidates = new List<string>();

        foreach (var status in nonTerminalStatuses)
        {
            // Also sideline statuses with very low total edge weight compared to average
            var totalWeight = data.Edges
                .Where(e => e.FromStatus == status || e.ToStatus == status)
                .Sum(e => e.Count);

            if (!canReachTerminal.Contains(status))
                sidelined.Add(status);
            else
                mainCandidates.Add(status);
        }

        // Topological sort of main candidates + terminals using Kahn's algorithm
        // Build in-degree map only for main path statuses
        var mainSet = new HashSet<string>(mainCandidates);
        foreach (var t in terminalStatuses) mainSet.Add(t);

        var inDegree = new Dictionary<string, int>();
        foreach (var s in mainSet) inDegree[s] = 0;

        foreach (var edge in data.Edges)
        {
            if (suppressedEdges.Contains((edge.FromStatus, edge.ToStatus)))
                continue;
            if (!mainSet.Contains(edge.FromStatus) || !mainSet.Contains(edge.ToStatus))
                continue;
            inDegree[edge.ToStatus]++;
        }

        var queue = new Queue<string>();
        foreach (var s in mainSet)
        {
            if (inDegree[s] == 0)
                queue.Enqueue(s);
        }

        var ordered = new List<string>();
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            ordered.Add(current);

            foreach (var (successor, _) in successors[current])
            {
                if (!mainSet.Contains(successor))
                    continue;
                inDegree[successor]--;
                if (inDegree[successor] == 0)
                    queue.Enqueue(successor);
            }
        }

        // Any main candidates not reached by topological sort (residual cycles) go to sidelined
        var orderedSet = new HashSet<string>(ordered);
        foreach (var s in mainCandidates)
        {
            if (!orderedSet.Contains(s))
                sidelined.Add(s);
        }

        // Separate done statuses and non-done in the ordered list
        // Done statuses must appear at the tail per business rule 5
        var orderedNonTerminal = ordered.Where(s => !doneSet.Contains(s)).ToList();
        var orderedTerminal = ordered.Where(s => doneSet.Contains(s)).ToList();

        var stages = new List<string>(orderedNonTerminal.Count + orderedTerminal.Count);
        stages.AddRange(orderedNonTerminal);
        stages.AddRange(orderedTerminal);

        return new WorkflowDetectionResult(
            stages,
            sidelined,
            data.TransitionCount,
            data.TicketCount,
            data.SprintCount);
    }
}
