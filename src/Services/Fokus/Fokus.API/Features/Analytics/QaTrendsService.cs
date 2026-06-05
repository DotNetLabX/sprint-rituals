namespace Fokus.API.Features.Analytics;

// --- Records ---

public record QaTrendsSprintInfo(int Id, string Name, DateTime StartDate, DateTime EndDate);

public record QualityTrendEntry(
    int SprintId,
    string SprintName,
    decimal CoverageRate,
    decimal PassRate,
    decimal ExecutionRate,
    string CoverageRag,
    string PassRateRag,
    string ExecutionRag);

public record TestingVolumeEntry(int SprintId, string SprintName, int TeCount, int BugsFound);

public record DefectCorrelationDataPoint(
    int SprintId,
    string SprintName,
    decimal CoverageRate,
    decimal? NextSprintBugRatio,
    string? NextSprintName);

public record DefectCorrelationResult(
    List<DefectCorrelationDataPoint> DataPoints,
    decimal? PearsonR,
    int DataPointCount);

public record QaTrendsResponse(
    bool HasQaData,
    List<QaTrendsSprintInfo> Sprints,
    List<QualityTrendEntry> QualityTrends,
    List<TestingVolumeEntry> TestingVolume,
    DefectCorrelationResult? DefectCorrelation);

// --- Service ---

public class QaTrendsService
{
    public QaTrendsResponse ComputeTrends(
        List<Sprint> qaSprints,
        Dictionary<int, List<TestExecution>> tesBySprintId,
        Dictionary<int, List<SprintMembership>> membershipsBySprintId,
        Dictionary<int, decimal> bugRatioBySprintId,
        List<StatusTransition> statusTransitions,
        AppSettings settings,
        string? subTeam)
    {
        var sortedSprints = qaSprints.OrderBy(s => s.StartDate).ToList();

        if (sortedSprints.Count < 2)
        {
            return new QaTrendsResponse(
                HasQaData: false,
                Sprints: [],
                QualityTrends: [],
                TestingVolume: [],
                DefectCorrelation: null);
        }

        var (orderedStages, startIndex) = ResolveStartIndex(settings);
        var thresholds = settings.QaHealthThresholds;

        var transitionsByTicket = statusTransitions
            .GroupBy(t => t.TicketId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var sprintInfos = sortedSprints
            .Select(s => new QaTrendsSprintInfo(s.Id, s.Name, s.StartDate, s.EndDate))
            .ToList();

        var qualityTrends = new List<QualityTrendEntry>();
        var testingVolume = new List<TestingVolumeEntry>();

        foreach (var sprint in sortedSprints)
        {
            var sprintTEs = tesBySprintId.GetValueOrDefault(sprint.Id, []);
            var memberships = membershipsBySprintId.GetValueOrDefault(sprint.Id, []);
            var filteredMemberships = FilterMemberships(memberships, subTeam);

            var activeKeys = GetActiveFeatureTicketKeys(
                filteredMemberships, transitionsByTicket,
                sprint.StartDate, sprint.EndDate,
                orderedStages, startIndex,
                settings.ExcludedFromScopeStatuses, settings.DefaultSpPerBug);

            var testsTesByTicket = BuildTestsTesByTicket(sprintTEs);
            var runsByTe = BuildRunsByTe(sprintTEs);

            var (coveredKeys, coverageRate) = ComputeCoverageRate(activeKeys, testsTesByTicket, memberships);
            var (_, executionRate) = ComputeExecutionRate(coveredKeys, testsTesByTicket, runsByTe);
            var passRate = ComputePassRate(sprintTEs);

            var coverageRag = HealthScoreCalculator.MetricRag(coverageRate, thresholds.CoverageGreen, thresholds.CoverageAmber, higherIsBetter: true);
            var executionRag = HealthScoreCalculator.MetricRag(executionRate, thresholds.ExecutionGreen, thresholds.ExecutionAmber, higherIsBetter: true);
            var passRateRag = HealthScoreCalculator.MetricRag(passRate, thresholds.PassRateGreen, thresholds.PassRateAmber, higherIsBetter: true);

            qualityTrends.Add(new QualityTrendEntry(
                sprint.Id, sprint.Name,
                coverageRate, passRate, executionRate,
                coverageRag, passRateRag, executionRag));

            // TE count: unique non-cancelled TEs for this sprint
            var teCount = sprintTEs.Count;

            // Bugs Found: unique bug ticket keys linked via Blocks from non-cancelled TEs
            var bugKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var te in sprintTEs)
            {
                foreach (var link in te.Links.Where(l => l.LinkType == TestExecutionLinkType.Blocks))
                {
                    if (link.Ticket?.IssueType == "Bug")
                        bugKeys.Add(link.TicketKey);
                }
            }

            testingVolume.Add(new TestingVolumeEntry(sprint.Id, sprint.Name, teCount, bugKeys.Count));
        }

        var defectCorrelation = ComputeDefectCorrelation(sortedSprints, qualityTrends, bugRatioBySprintId);

        return new QaTrendsResponse(
            HasQaData: true,
            Sprints: sprintInfos,
            QualityTrends: qualityTrends,
            TestingVolume: testingVolume,
            DefectCorrelation: defectCorrelation);
    }

    // --- Defect correlation ---

    private static DefectCorrelationResult ComputeDefectCorrelation(
        List<Sprint> sortedSprints,
        List<QualityTrendEntry> qualityTrends,
        Dictionary<int, decimal> bugRatioBySprintId)
    {
        var dataPoints = new List<DefectCorrelationDataPoint>();

        for (var i = 0; i < sortedSprints.Count; i++)
        {
            var sprint = sortedSprints[i];
            var trend = qualityTrends.First(t => t.SprintId == sprint.Id);

            decimal? nextBugRatio = null;
            string? nextSprintName = null;

            if (i + 1 < sortedSprints.Count)
            {
                var nextSprint = sortedSprints[i + 1];
                if (bugRatioBySprintId.TryGetValue(nextSprint.Id, out var ratio))
                {
                    nextBugRatio = ratio;
                    nextSprintName = nextSprint.Name;
                }
            }

            dataPoints.Add(new DefectCorrelationDataPoint(
                sprint.Id, sprint.Name, trend.CoverageRate, nextBugRatio, nextSprintName));
        }

        // Complete pairs: both coverage and next-sprint bug ratio present
        var completePairs = dataPoints
            .Where(dp => dp.NextSprintBugRatio.HasValue)
            .ToList();

        var pearsonR = ComputePearsonR(completePairs);

        return new DefectCorrelationResult(dataPoints, pearsonR, completePairs.Count);
    }

    private static decimal? ComputePearsonR(List<DefectCorrelationDataPoint> completePairs)
    {
        var n = completePairs.Count;
        if (n < 6) return null;

        var xs = completePairs.Select(p => p.CoverageRate).ToList();
        var ys = completePairs.Select(p => p.NextSprintBugRatio!.Value).ToList();

        var sumX = xs.Sum();
        var sumY = ys.Sum();
        var sumXY = xs.Zip(ys, (x, y) => x * y).Sum();
        var sumX2 = xs.Sum(x => x * x);
        var sumY2 = ys.Sum(y => y * y);

        var numerator = n * sumXY - sumX * sumY;
        var denominator = (decimal)Math.Sqrt((double)((n * sumX2 - sumX * sumX) * (n * sumY2 - sumY * sumY)));

        if (denominator == 0) return null;

        return Math.Round(numerator / denominator, 2);
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

    private static (HashSet<string> CoveredKeys, decimal Rate) ComputeCoverageRate(
        HashSet<string> activeTicketKeys,
        Dictionary<string, List<string>> testsTesByTicket,
        List<SprintMembership> memberships)
    {
        if (activeTicketKeys.Count == 0)
            return ([], 0);

        var parentByTicket = memberships
            .Where(m => m.Ticket?.ParentTicketKey != null)
            .ToDictionary(m => m.TicketId, m => m.Ticket!.ParentTicketKey!, StringComparer.OrdinalIgnoreCase);

        var coveredKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in activeTicketKeys)
        {
            if (testsTesByTicket.ContainsKey(key) && testsTesByTicket[key].Count > 0)
            {
                coveredKeys.Add(key);
                continue;
            }
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

    // --- Sub-team filtering ---

    private static List<SprintMembership> FilterMemberships(List<SprintMembership> memberships, string? subTeam)
    {
        if (string.IsNullOrWhiteSpace(subTeam))
            return memberships;
        return memberships.Where(m => m.Ticket?.Assignee?.SubTeam == subTeam).ToList();
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
}
