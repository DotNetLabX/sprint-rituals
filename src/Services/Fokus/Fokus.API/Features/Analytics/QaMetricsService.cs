namespace Fokus.API.Features.Analytics;

// --- QA metrics records ---

public record QaMetricsResult(
    bool HasQaData,
    MetricCard CoverageRate,
    MetricCard ExecutionRate,
    MetricCard PassRate,
    int BugsFound,
    decimal QualitySubScore,
    QualityBreakdownInfo QualityBreakdown,
    int UntestedCount,
    int FailingCount);

public record QualityBreakdownInfo(
    decimal CoverageScore,
    int CoverageWeight,
    decimal PassRateScore,
    int PassRateWeight);

// --- Service ---

public class QaMetricsService
{
    public QaMetricsResult ComputeQaMetrics(
        List<TestExecution> sprintTEs,
        List<SprintMembership> activeMemberships,
        List<StatusTransition> statusTransitions,
        Sprint selectedSprint,
        Sprint? priorSprint,
        List<TestExecution>? priorSprintTEs,
        List<SprintMembership>? priorMemberships,
        List<Sprint> sparklineWindow,
        Dictionary<int, List<TestExecution>> sparklineTEsBySprintId,
        Dictionary<int, List<SprintMembership>> sparklineMembershipsBySprintId,
        AppSettings settings,
        string? subTeam)
    {
        var (orderedStages, startIndex) = ResolveStartIndex(settings);

        // Apply sub-team filter to memberships
        var filteredMemberships = FilterMemberships(activeMemberships, subTeam);

        // Compute active scope (feature tickets in sprint with effective SP and IsStartedInSprint)
        var transitionsByTicket = statusTransitions
            .GroupBy(t => t.TicketId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var activeTicketKeys = GetActiveFeatureTicketKeys(
            filteredMemberships, transitionsByTicket,
            selectedSprint.StartDate, selectedSprint.EndDate,
            orderedStages, startIndex,
            settings.ExcludedFromScopeStatuses, settings.DefaultSpPerBug);

        // sprintTEs is pre-filtered by the repository (Status != "Cancelled" excluded at the DB level).
        var nonCancelledTEs = sprintTEs;

        // Map from ticketKey -> list of TE IDs (Tests links only)
        var testsTesByTicket = BuildTestsTesByTicket(nonCancelledTEs);

        // Coverage rate (BR1)
        var (coveredKeys, coverageRate) = ComputeCoverageRate(
            activeTicketKeys, testsTesByTicket, activeMemberships);
        var untestedCount = activeTicketKeys.Count - coveredKeys.Count;

        // Execution rate (BR2)
        var runsByTe = BuildRunsByTe(nonCancelledTEs);
        var (executedKeys, executionRate) = ComputeExecutionRate(coveredKeys, testsTesByTicket, runsByTe);

        // Pass rate (BR3)
        var passRate = ComputePassRate(nonCancelledTEs);

        // Bugs found (BR4)
        var bugsFound = ComputeBugsFound(nonCancelledTEs);

        // Failing ticket count
        var failingCount = ComputeFailingCount(coveredKeys, testsTesByTicket, runsByTe);

        // Quality sub-score (BR16)
        var thresholds = settings.QaHealthThresholds;
        var subScoreWeights = settings.QualitySubScoreWeights;
        var coverageScore = HealthScoreCalculator.ScoreHigherIsBetter(coverageRate, thresholds.CoverageGreen, thresholds.CoverageAmber);
        var passRateScore = HealthScoreCalculator.ScoreHigherIsBetter(passRate, thresholds.PassRateGreen, thresholds.PassRateAmber);

        var totalSubScoreWeight = subScoreWeights.CoverageWeight + subScoreWeights.PassRateWeight;
        var qualitySubScore = totalSubScoreWeight > 0
            ? (coverageScore * subScoreWeights.CoverageWeight + passRateScore * subScoreWeights.PassRateWeight) / totalSubScoreWeight
            : 0;

        qualitySubScore = Math.Round(qualitySubScore, 1);
        coverageScore = Math.Round(coverageScore, 1);
        passRateScore = Math.Round(passRateScore, 1);

        var qualityBreakdown = new QualityBreakdownInfo(
            coverageScore, subScoreWeights.CoverageWeight,
            passRateScore, subScoreWeights.PassRateWeight);

        // Delta (BR21) — prior sprint
        decimal? priorCoverageRate = null;
        decimal? priorExecutionRate = null;
        decimal? priorPassRate = null;

        if (priorSprint is not null && priorSprintTEs is not null && priorMemberships is not null)
        {
            var priorFiltered = FilterMemberships(priorMemberships, subTeam);
            // priorSprintTEs is pre-filtered by the repository (non-cancelled only).
            var priorNonCancelled = priorSprintTEs;
            var priorActiveKeys = GetActiveFeatureTicketKeys(
                priorFiltered, transitionsByTicket,
                priorSprint.StartDate, priorSprint.EndDate,
                orderedStages, startIndex,
                settings.ExcludedFromScopeStatuses, settings.DefaultSpPerBug);
            var priorTestsTesByTicket = BuildTestsTesByTicket(priorNonCancelled);
            var (priorCovered, priorCov) = ComputeCoverageRate(priorActiveKeys, priorTestsTesByTicket, priorMemberships);
            priorCoverageRate = priorCov;
            var priorRunsByTe = BuildRunsByTe(priorNonCancelled);
            var (_, priorExec) = ComputeExecutionRate(priorCovered, priorTestsTesByTicket, priorRunsByTe);
            priorExecutionRate = priorExec;
            priorPassRate = ComputePassRate(priorNonCancelled);
        }

        // Sparkline (BR22)
        var coverageSparkline = BuildQaSparkline(
            sparklineWindow, sparklineTEsBySprintId, sparklineMembershipsBySprintId,
            transitionsByTicket, settings, orderedStages, startIndex, subTeam,
            (nonCancelled, activeKeys, memberships2) =>
            {
                var testsMap = BuildTestsTesByTicket(nonCancelled);
                var (_, rate) = ComputeCoverageRate(activeKeys, testsMap, memberships2);
                return rate;
            });

        var executionSparkline = BuildQaSparkline(
            sparklineWindow, sparklineTEsBySprintId, sparklineMembershipsBySprintId,
            transitionsByTicket, settings, orderedStages, startIndex, subTeam,
            (nonCancelled, activeKeys, memberships2) =>
            {
                var testsMap = BuildTestsTesByTicket(nonCancelled);
                var runsMap = BuildRunsByTe(nonCancelled);
                var (covered, _) = ComputeCoverageRate(activeKeys, testsMap, memberships2);
                var (_, rate) = ComputeExecutionRate(covered, testsMap, runsMap);
                return rate;
            });

        var passRateSparkline = BuildQaSparkline(
            sparklineWindow, sparklineTEsBySprintId, sparklineMembershipsBySprintId,
            transitionsByTicket, settings, orderedStages, startIndex, subTeam,
            (nonCancelled, _, _) => ComputePassRate(nonCancelled));

        var coverageRag = HealthScoreCalculator.MetricRag(coverageRate, thresholds.CoverageGreen, thresholds.CoverageAmber, higherIsBetter: true);
        var executionRag = HealthScoreCalculator.MetricRag(executionRate, thresholds.ExecutionGreen, thresholds.ExecutionAmber, higherIsBetter: true);
        var passRateRag = HealthScoreCalculator.MetricRag(passRate, thresholds.PassRateGreen, thresholds.PassRateAmber, higherIsBetter: true);

        return new QaMetricsResult(
            HasQaData: true,
            CoverageRate: BuildQaMetricCard("Coverage Rate", coverageRate, priorCoverageRate, coverageSparkline, coverageRag),
            ExecutionRate: BuildQaMetricCard("Execution Rate", executionRate, priorExecutionRate, executionSparkline, executionRag),
            PassRate: BuildQaMetricCard("Pass Rate", passRate, priorPassRate, passRateSparkline, passRateRag),
            BugsFound: bugsFound,
            QualitySubScore: qualitySubScore,
            QualityBreakdown: qualityBreakdown,
            UntestedCount: untestedCount,
            FailingCount: failingCount);
    }

    // --- Active scope ---

    private static HashSet<string> GetActiveFeatureTicketKeys(
        List<SprintMembership> memberships,
        Dictionary<string, List<StatusTransition>> transitionsByTicket,
        DateTime sprintStart,
        DateTime sprintEnd,
        List<string> orderedStages,
        int startIndex,
        List<string> excludedStatuses,
        int defaultSpPerBug)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in memberships)
        {
            if (m.RemovedAt != null) continue;
            if (m.Ticket?.IssueType == "Bug") continue;
            if (excludedStatuses.Contains(m.FinalStatus, StringComparer.OrdinalIgnoreCase)) continue;
            if (!GetEffectiveSp(m, defaultSpPerBug).HasValue) continue;

            var ticketTransitions = transitionsByTicket.GetValueOrDefault(m.TicketId, []);
            if (IsStartedInSprint(ticketTransitions, sprintStart, sprintEnd, orderedStages, startIndex))
                result.Add(m.TicketId);
        }
        return result;
    }

    private static bool IsStartedInSprint(
        List<StatusTransition> transitions,
        DateTime sprintStart,
        DateTime sprintEnd,
        List<string> orderedStages,
        int startIndex)
    {
        if (startIndex < 0) return false;
        return transitions.Any(t =>
            t.Timestamp >= sprintStart &&
            t.Timestamp <= sprintEnd &&
            GetStageIndex(t.ToStatus, orderedStages) >= startIndex);
    }

    private static int GetStageIndex(string status, List<string> orderedStages) =>
        orderedStages.FindIndex(s => string.Equals(s, status, StringComparison.OrdinalIgnoreCase));

    private static decimal? GetEffectiveSp(SprintMembership m, int defaultSpPerBug)
    {
        if (m.StoryPoints.HasValue && m.StoryPoints.Value > 0) return m.StoryPoints;
        if (m.Ticket?.IssueType == "Bug" && defaultSpPerBug > 0) return (decimal)defaultSpPerBug;
        return null;
    }

    // --- Coverage helpers ---

    /// <summary>
    /// Builds a map from ticketKey -> list of TE IDs (Tests links only, from non-cancelled TEs).
    /// </summary>
    private static Dictionary<string, List<string>> BuildTestsTesByTicket(List<TestExecution> nonCancelledTEs)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var te in nonCancelledTEs)
        {
            foreach (var link in te.Links.Where(l => l.LinkType == TestExecutionLinkType.Tests))
            {
                if (!result.TryGetValue(link.TicketKey, out var list))
                {
                    list = [];
                    result[link.TicketKey] = list;
                }
                if (!list.Contains(te.Id))
                    list.Add(te.Id);
            }
        }
        return result;
    }

    private static Dictionary<string, List<TestRun>> BuildRunsByTe(List<TestExecution> nonCancelledTEs) =>
        nonCancelledTEs.ToDictionary(te => te.Id, te => te.TestRuns.ToList());

    /// <summary>
    /// Coverage rate (BR1): feature tickets in active scope with at least one non-cancelled TE via Tests link.
    /// Sub-task inheritance (BR9): if a ticket has no own TE links, check its parent's links.
    /// </summary>
    private static (HashSet<string> CoveredKeys, decimal Rate) ComputeCoverageRate(
        HashSet<string> activeTicketKeys,
        Dictionary<string, List<string>> testsTesByTicket,
        List<SprintMembership> memberships)
    {
        if (activeTicketKeys.Count == 0)
            return ([], 0);

        // Build parentKey lookup from memberships
        var parentByTicket = memberships
            .Where(m => m.Ticket?.ParentTicketKey != null)
            .ToDictionary(m => m.TicketId, m => m.Ticket!.ParentTicketKey!, StringComparer.OrdinalIgnoreCase);

        var coveredKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in activeTicketKeys)
        {
            // Own links
            if (testsTesByTicket.ContainsKey(key) && testsTesByTicket[key].Count > 0)
            {
                coveredKeys.Add(key);
                continue;
            }
            // BR9: inherit from parent
            if (parentByTicket.TryGetValue(key, out var parentKey) &&
                !string.IsNullOrEmpty(parentKey) &&
                testsTesByTicket.TryGetValue(parentKey, out var parentTeIds) &&
                parentTeIds.Count > 0)
            {
                coveredKeys.Add(key);
            }
        }

        var rate = (decimal)coveredKeys.Count / activeTicketKeys.Count * 100;
        return (coveredKeys, Math.Round(rate, 1));
    }

    /// <summary>
    /// Execution rate (BR2): covered tickets where at least one linked TE has a Pass or Fail run.
    /// </summary>
    private static (HashSet<string> ExecutedKeys, decimal Rate) ComputeExecutionRate(
        HashSet<string> coveredKeys,
        Dictionary<string, List<string>> testsTesByTicket,
        Dictionary<string, List<TestRun>> runsByTe)
    {
        if (coveredKeys.Count == 0)
            return ([], 0);

        var executedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in coveredKeys)
        {
            var teIds = testsTesByTicket.GetValueOrDefault(key, []);
            var hasExecutedRun = teIds.Any(teId =>
            {
                var runs = runsByTe.GetValueOrDefault(teId, []);
                return runs.Any(r => r.Status == TestRunStatus.Pass || r.Status == TestRunStatus.Fail);
            });
            if (hasExecutedRun)
                executedKeys.Add(key);
        }

        var rate = (decimal)executedKeys.Count / coveredKeys.Count * 100;
        return (executedKeys, Math.Round(rate, 1));
    }

    /// <summary>
    /// Pass rate (BR3): pass runs / (pass + fail runs) across all non-cancelled TEs.
    /// </summary>
    private static decimal ComputePassRate(List<TestExecution> nonCancelledTEs)
    {
        var passCount = 0;
        var totalCount = 0;
        foreach (var te in nonCancelledTEs)
        {
            foreach (var run in te.TestRuns)
            {
                if (run.Status == TestRunStatus.Pass) { passCount++; totalCount++; }
                else if (run.Status == TestRunStatus.Fail) { totalCount++; }
            }
        }
        return totalCount > 0 ? Math.Round((decimal)passCount / totalCount * 100, 1) : 0;
    }

    /// <summary>
    /// Bugs found (BR4): unique bug ticket keys linked via Blocks links from non-cancelled TEs.
    /// </summary>
    private static int ComputeBugsFound(List<TestExecution> nonCancelledTEs)
    {
        var bugKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var te in nonCancelledTEs)
        {
            foreach (var link in te.Links.Where(l => l.LinkType == TestExecutionLinkType.Blocks))
            {
                // The plan says: where linked ticket's IssueType = "Bug"
                // TestExecutionLink.Ticket is a nav property — check if it's loaded
                if (link.Ticket?.IssueType == "Bug")
                    bugKeys.Add(link.TicketKey);
            }
        }
        return bugKeys.Count;
    }

    /// <summary>
    /// Failing ticket count: covered tickets where any linked TE has at least one Fail run.
    /// </summary>
    private static int ComputeFailingCount(
        HashSet<string> coveredKeys,
        Dictionary<string, List<string>> testsTesByTicket,
        Dictionary<string, List<TestRun>> runsByTe)
    {
        var count = 0;
        foreach (var key in coveredKeys)
        {
            var teIds = testsTesByTicket.GetValueOrDefault(key, []);
            var hasFail = teIds.Any(teId =>
            {
                var runs = runsByTe.GetValueOrDefault(teId, []);
                return runs.Any(r => r.Status == TestRunStatus.Fail);
            });
            if (hasFail) count++;
        }
        return count;
    }

    // --- Sub-team filtering ---

    private static List<SprintMembership> FilterMemberships(List<SprintMembership> memberships, string? subTeam)
    {
        if (string.IsNullOrWhiteSpace(subTeam))
            return memberships;
        return memberships.Where(m => m.Ticket?.Assignee?.SubTeam == subTeam).ToList();
    }

    // --- Sparkline (BR22) ---

    private static List<SparklinePoint> BuildQaSparkline(
        List<Sprint> window,
        Dictionary<int, List<TestExecution>> tesBySprintId,
        Dictionary<int, List<SprintMembership>> membershipsBySprintId,
        Dictionary<string, List<StatusTransition>> transitionsByTicket,
        AppSettings settings,
        List<string> orderedStages,
        int startIndex,
        string? subTeam,
        Func<List<TestExecution>, HashSet<string>, List<SprintMembership>, decimal> valueSelector)
    {
        var points = new List<SparklinePoint>();
        foreach (var sprint in window)
        {
            // sprintTEs is pre-filtered by the repository (non-cancelled only).
            var sprintTEs = tesBySprintId.GetValueOrDefault(sprint.Id, []);
            var sprintMemberships = membershipsBySprintId.GetValueOrDefault(sprint.Id, []);
            var filteredMemberships = FilterMemberships(sprintMemberships, subTeam);
            var nonCancelled = sprintTEs;

            var activeKeys = GetActiveFeatureTicketKeys(
                filteredMemberships, transitionsByTicket,
                sprint.StartDate, sprint.EndDate,
                orderedStages, startIndex,
                settings.ExcludedFromScopeStatuses, settings.DefaultSpPerBug);

            var value = valueSelector(nonCancelled, activeKeys, sprintMemberships);
            points.Add(new SparklinePoint(sprint.Name, Math.Round(value, 1)));
        }
        return points;
    }

    // --- Settings helpers ---

    private static (List<string> OrderedStages, int StartIndex) ResolveStartIndex(AppSettings settings)
    {
        var orderedStages = new List<string>();
        orderedStages.AddRange(settings.WorkflowStages);
        orderedStages.AddRange(settings.DoneStatuses);

        var startStage = settings.CycleTimeStartStage
            ?? (orderedStages.Count > 0 ? orderedStages[0] : null);

        if (startStage is null)
            return (orderedStages, 0);

        var startIndex = orderedStages.FindIndex(s => string.Equals(s, startStage, StringComparison.OrdinalIgnoreCase));
        if (startIndex < 0) startIndex = 0;

        return (orderedStages, startIndex);
    }

    // --- Metric card builder ---

    private static MetricCard BuildQaMetricCard(
        string name,
        decimal value,
        decimal? priorValue,
        List<SparklinePoint> sparkline,
        string rag)
    {
        decimal? delta = priorValue.HasValue ? value - priorValue.Value : null;
        string? direction = null;
        string? deltaPolarity = null;

        if (delta.HasValue)
        {
            direction = delta.Value > 0 ? "up" : delta.Value < 0 ? "down" : "flat";
            // All QA metrics are positive-up
            deltaPolarity = delta.Value > 0 ? "positive" : delta.Value < 0 ? "negative" : "neutral";
        }

        return new MetricCard(
            name,
            Math.Round(value, 1),
            $"{value:0.#}%",
            delta.HasValue ? Math.Round(delta.Value, 1) : null,
            direction,
            deltaPolarity,
            sparkline)
        {
            Rag = rag
        };
    }
}
