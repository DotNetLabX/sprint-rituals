namespace Fokus.API.Features.Analytics;

// --- Response record hierarchy ---

public record EpicProgressSummaryMetrics(
    int ActiveEpicCount,
    int CompletedEpicCount,
    decimal AverageCompletionPercentage);

public record TestRunSummaryDto(
    int Passed,
    int Failed,
    int Todo,
    int Executing,
    int Aborted);

public record EpicProgressTicketEntry(
    string TicketKey,
    string Summary,
    string IssueType,
    decimal? StoryPoints,
    string CurrentStatus,
    string? AssigneeDisplayName,
    bool IsDone,
    string? TestStatus,
    decimal? TestPassRate,
    int? TestBugsFound,
    TestRunSummaryDto? TestRunSummary);

public record EpicProgressEntry(
    string EpicKey,
    string EpicName,
    int TotalTickets,
    int DoneTickets,
    int RemainingTickets,
    decimal TicketCompletionPercentage,
    decimal TotalSp,
    decimal DoneSp,
    decimal RemainingSp,
    decimal? ImputedSp,
    decimal? AdjustedTotalSp,
    decimal? SpCompletionPercentage,
    int UnestimatedTicketCount,
    decimal? Velocity,
    decimal? ProjectedSprintsRemaining,
    string? ProjectionConfidence,
    int ActiveSprintCount,
    DateOnly? StartedDate,
    DateOnly? LastWorkDate,
    bool IsCompleted,
    List<EpicProgressTicketEntry> Tickets,
    decimal? CoverageRate,
    decimal? PassRate,
    int BugsFound,
    int FeatureTicketCount,
    int CoveredTicketCount,
    string? CoverageRag,
    string? PassRateRag);

public record EpicProgressUnlinkedWork(
    int TicketCount,
    decimal TotalSp);

public record EpicProgressResponse(
    EpicProgressSummaryMetrics SummaryMetrics,
    List<EpicProgressEntry> Epics,
    EpicProgressUnlinkedWork UnlinkedWork,
    bool HasQaData,
    decimal? AverageTestCoverage);

// --- QA data parameter type ---

public record EpicQaData(
    Dictionary<string, List<string>> TestsLinksByTicket,
    Dictionary<string, List<string>> BlocksLinksByTicket,
    Dictionary<string, List<TestRun>> RunsByTeId);

// --- Service ---

public class EpicProgressService
{
    public EpicProgressResponse ComputeEpicProgress(
        List<Ticket> epicTickets,
        List<SprintMembership> allClosedMemberships,
        List<Ticket> unlinkedTickets,
        AppSettings settings,
        List<StatusTransition> statusTransitions,
        List<Sprint> closedSprints,
        string? subTeam,
        EpicQaData? qaData = null)
    {
        var completedStatuses = CompletionChecker.ResolveCompletedStatuses(settings);
        var defaultSpPerBug = settings.DefaultSpPerBug;

        var (orderedStages, startIndex) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, orderedStages);

        // Build sprint date lookup for velocity transition checks
        var sprintDateLookup = closedSprints.ToDictionary(s => s.Id, s => (s.StartDate, s.EndDate));

        // BR13: sub-team filtering
        var filteredEpicTickets = FilterTickets(epicTickets, subTeam);
        var filteredUnlinkedTickets = FilterTickets(unlinkedTickets, subTeam);
        var filteredMemberships = FilterMemberships(allClosedMemberships, subTeam);

        // Group filtered tickets by EpicKey
        var epicGroups = filteredEpicTickets
            .GroupBy(t => t.EpicKey!)
            .ToList();

        // Build once — used for velocity, date derivation, and sprint completion checks across all epics
        var transitionsByTicket = statusTransitions
            .GroupBy(t => t.TicketId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var epicEntries = new List<EpicProgressEntry>();

        foreach (var group in epicGroups)
        {
            var epicKey = group.Key;
            var tickets = group.ToList();

            // Epic name: first ticket's EpicName ?? EpicKey (BR22)
            var epicName = tickets.First().EpicName ?? epicKey;

            // Done vs remaining (BR2)
            var doneTickets = tickets.Where(t => completedStatuses.Contains(t.CurrentStatus)).ToList();
            var remainingTickets = tickets.Where(t => !completedStatuses.Contains(t.CurrentStatus)).ToList();

            var totalTickets = tickets.Count;
            var doneCount = doneTickets.Count;
            var remainingCount = remainingTickets.Count;
            var ticketCompletionPct = totalTickets > 0
                ? Math.Round((decimal)doneCount / totalTickets * 100, 1)
                : 0m;

            // SP sums from estimated tickets (BR15)
            var totalSp = tickets
                .Sum(t => GetEffectiveTicketSp(t, defaultSpPerBug) ?? 0m);

            var doneSp = doneTickets
                .Sum(t => GetEffectiveTicketSp(t, defaultSpPerBug) ?? 0m);

            var remainingSp = remainingTickets
                .Sum(t => GetEffectiveTicketSp(t, defaultSpPerBug) ?? 0m);

            // Unestimated ticket count (BR15)
            var unestimatedCount = tickets.Count(t => GetEffectiveTicketSp(t, defaultSpPerBug) == null);

            // Imputation (BR4, BR5, BR6): apply only to remaining unestimated tickets
            var estimatedTickets = tickets.Where(t => GetEffectiveTicketSp(t, defaultSpPerBug).HasValue).ToList();
            decimal? imputedSp = null;
            decimal? adjustedTotalSp = null;
            decimal? spCompletionPct = null;

            if (estimatedTickets.Count > 0)
            {
                var avgSp = estimatedTickets.Average(t => GetEffectiveTicketSp(t, defaultSpPerBug)!.Value);
                var remainingUnestimatedCount = remainingTickets.Count(t => GetEffectiveTicketSp(t, defaultSpPerBug) == null);
                imputedSp = remainingUnestimatedCount > 0
                    ? Math.Round(avgSp * remainingUnestimatedCount, 1)
                    : 0m;
                adjustedTotalSp = Math.Round(totalSp + imputedSp.Value, 1);

                // SP completion % (BR3, BR19)
                spCompletionPct = adjustedTotalSp.Value > 0
                    ? Math.Round(doneSp / adjustedTotalSp.Value * 100, 1)
                    : 0m;
            }

            // Velocity (BR7, BR8, BR16, BR24): from closed sprint memberships for this epic
            var epicMemberships = filteredMemberships
                .Where(sm => sm.Ticket?.EpicKey == epicKey)
                .ToList();

            // Activity dates (F35 BR1, BR2): earliest and most recent qualifying transition across epic tickets
            // A transition qualifies if its ToStatus stage index >= startIndex (same boundary as velocity)
            // Guard: if no cycle time start stage is configured (startIndex < 0), dates cannot be derived —
            // GetStageIndex returns -1 for unrecognised statuses, so -1 >= -1 would match noise transitions
            DateOnly? startedDate = null;
            DateOnly? lastWorkDate = null;

            if (startIndex >= 0)
            {
                foreach (var ticket in tickets)
                {
                    var ticketTransitions = transitionsByTicket.GetValueOrDefault(ticket.Id, []);
                    foreach (var transition in ticketTransitions)
                    {
                        if (TransitionAttributionChecker.GetStageIndex(transition.ToStatus, orderedStages) >= startIndex)
                        {
                            var transitionDate = DateOnly.FromDateTime(transition.Timestamp);
                            if (startedDate is null || transitionDate < startedDate)
                                startedDate = transitionDate;
                            if (lastWorkDate is null || transitionDate > lastWorkDate)
                                lastWorkDate = transitionDate;
                        }
                    }
                }
            }

            // Group by SprintId, sum completed SP per sprint (transition-based — spec BR16 velocity)
            var sprintSpCompleted = epicMemberships
                .GroupBy(sm => sm.SprintId)
                .Select(g =>
                {
                    var sprintId = g.Key;
                    var sprintStartDate = g.First().Sprint?.StartDate ?? DateTime.MinValue;
                    var (sprintStart, sprintEnd) = sprintDateLookup.TryGetValue(sprintId, out var dates)
                        ? dates
                        : (sprintStartDate, sprintStartDate);

                    var completedSp = g
                        .Where(sm =>
                        {
                            if (sm.RemovedAt != null) return false;
                            var ticketTransitions = transitionsByTicket.GetValueOrDefault(sm.TicketId, []);
                            var (isCompleted, _) = TransitionAttributionChecker.IsCompletedInSprint(
                                sm.TicketId, ticketTransitions, sprintStart, sprintEnd, orderedStages, endIndex);
                            return isCompleted;
                        })
                        .Sum(sm => sm.GetEffectiveSp(defaultSpPerBug) ?? 0m);

                    return new { SprintId = sprintId, SprintStartDate = sprintStartDate, CompletedSp = completedSp };
                })
                .Where(x => x.CompletedSp > 0) // BR8: skip zero-progress sprints
                .OrderByDescending(x => x.SprintStartDate)
                .Take(3) // rolling 3-sprint window
                .ToList();

            decimal? velocity = null;
            if (sprintSpCompleted.Count > 0)
            {
                velocity = Math.Round(sprintSpCompleted.Average(x => x.CompletedSp), 1);
            }

            // Projection (BR9, BR10)
            decimal? projectedSprintsRemaining = null;
            string? projectionConfidence = null;

            if (velocity.HasValue && velocity.Value > 0)
            {
                var remainingWork = remainingSp + (imputedSp ?? 0m);
                projectedSprintsRemaining = Math.Round(remainingWork / velocity.Value, 1);

                // Confidence (BR10): low when fewer than 3 data points
                if (sprintSpCompleted.Count < 3)
                {
                    projectionConfidence = "low";
                }
            }

            // Active sprint count (BR17): distinct sprints where this epic had at least one ticket membership
            var activeSprintCount = epicMemberships
                .Select(sm => sm.SprintId)
                .Distinct()
                .Count();

            // Is completed (BR11): all tickets have done status
            var isCompleted = tickets.All(t => completedStatuses.Contains(t.CurrentStatus));

            // QA metrics per epic (BR1–BR12, BR20)
            var (epicCoverageRate, epicPassRate, epicBugsFound, epicFeatureCount, epicCoveredCount, epicCoverageRag, epicPassRateRag) =
                ComputeEpicQaMetrics(tickets, qaData, settings);

            // Ticket list — sort: remaining first (by current status for grouping), then done (BR spec Flow 2 step 3)
            var ticketEntries = remainingTickets
                .OrderBy(t => t.CurrentStatus)
                .Concat(doneTickets.OrderBy(t => t.CurrentStatus))
                .Select(t => BuildTicketEntry(t, completedStatuses, qaData))
                .ToList();

            epicEntries.Add(new EpicProgressEntry(
                epicKey,
                epicName,
                totalTickets,
                doneCount,
                remainingCount,
                ticketCompletionPct,
                Math.Round(totalSp, 1),
                Math.Round(doneSp, 1),
                Math.Round(remainingSp, 1),
                imputedSp,
                adjustedTotalSp,
                spCompletionPct,
                unestimatedCount,
                velocity,
                projectedSprintsRemaining,
                projectionConfidence,
                activeSprintCount,
                startedDate,
                lastWorkDate,
                isCompleted,
                ticketEntries,
                epicCoverageRate,
                epicPassRate,
                epicBugsFound,
                epicFeatureCount,
                epicCoveredCount,
                epicCoverageRag,
                epicPassRateRag));
        }

        // BR12: sort — active epics by SP completion % asc (null last), then completed epics by name asc
        var activeEpics = epicEntries
            .Where(e => !e.IsCompleted)
            .OrderBy(e => e.SpCompletionPercentage.HasValue ? 0 : 1)
            .ThenBy(e => e.SpCompletionPercentage ?? e.TicketCompletionPercentage)
            .ToList();

        var completedEpics = epicEntries
            .Where(e => e.IsCompleted)
            .OrderBy(e => e.EpicName)
            .ToList();

        var sortedEpics = activeEpics.Concat(completedEpics).ToList();

        // Summary metrics
        var activeEpicCount = activeEpics.Count;
        var completedEpicCount = completedEpics.Count;

        // Average completion: weighted by adjustedTotalSp (BR spec AC)
        decimal averageCompletionPct;
        var totalAdjustedSp = activeEpics
            .Where(e => e.AdjustedTotalSp.HasValue)
            .Sum(e => e.AdjustedTotalSp!.Value);

        if (totalAdjustedSp > 0)
        {
            var weightedSum = activeEpics
                .Where(e => e.AdjustedTotalSp.HasValue && e.SpCompletionPercentage.HasValue)
                .Sum(e => e.SpCompletionPercentage!.Value * e.AdjustedTotalSp!.Value);
            averageCompletionPct = Math.Round(weightedSum / totalAdjustedSp, 1);
        }
        else if (activeEpics.Count > 0)
        {
            // All active epics have null adjustedTotalSp — fall back to ticket-count average
            averageCompletionPct = Math.Round(
                activeEpics.Average(e => e.TicketCompletionPercentage), 1);
        }
        else
        {
            averageCompletionPct = 0m;
        }

        var summaryMetrics = new EpicProgressSummaryMetrics(
            activeEpicCount,
            completedEpicCount,
            averageCompletionPct);

        // Unlinked work (BR14)
        var unlinkedCount = filteredUnlinkedTickets.Count;
        var unlinkedTotalSp = filteredUnlinkedTickets
            .Sum(t => GetEffectiveTicketSp(t, defaultSpPerBug) ?? 0m);

        var unlinkedWork = new EpicProgressUnlinkedWork(
            unlinkedCount,
            Math.Round(unlinkedTotalSp, 1));

        // BR16: average test coverage = arithmetic mean of non-null coverage rates (epics with null excluded)
        var hasQaData = qaData is not null;
        decimal? averageTestCoverage = null;
        if (hasQaData)
        {
            var coverageRates = sortedEpics
                .Where(e => e.CoverageRate.HasValue)
                .Select(e => e.CoverageRate!.Value)
                .ToList();
            if (coverageRates.Count > 0)
                averageTestCoverage = Math.Round(coverageRates.Average(), 1);
        }

        return new EpicProgressResponse(summaryMetrics, sortedEpics, unlinkedWork, hasQaData, averageTestCoverage);
    }

    // --- Effective SP for Ticket entities (mirrors SprintMembership.GetEffectiveSp) ---

    private static decimal? GetEffectiveTicketSp(Ticket t, int defaultSpPerBug)
    {
        if (t.StoryPoints.HasValue && t.StoryPoints.Value > 0)
            return t.StoryPoints;
        if (t.IssueType == "Bug" && defaultSpPerBug > 0)
            return (decimal)defaultSpPerBug;
        return null;
    }

    // --- Sub-team filtering (BR13) ---

    private static List<Ticket> FilterTickets(List<Ticket> tickets, string? subTeam)
    {
        if (string.IsNullOrWhiteSpace(subTeam))
            return tickets;

        return tickets
            .Where(t => t.Assignee?.SubTeam == subTeam)
            .ToList();
    }

    private static List<SprintMembership> FilterMemberships(
        List<SprintMembership> memberships,
        string? subTeam)
    {
        if (string.IsNullOrWhiteSpace(subTeam))
            return memberships;

        return memberships
            .Where(sm => sm.Ticket?.Assignee?.SubTeam == subTeam)
            .ToList();
    }

    // --- QA computation helpers ---

    /// <summary>
    /// Computes per-epic QA metrics (BR1–BR4, BR12, BR13, BR20).
    /// Returns (coverageRate, passRate, bugsFound, featureTicketCount, coveredTicketCount, coverageRag, passRateRag).
    /// </summary>
    private static (decimal? CoverageRate, decimal? PassRate, int BugsFound, int FeatureTicketCount,
        int CoveredTicketCount, string? CoverageRag, string? PassRateRag)
        ComputeEpicQaMetrics(List<Ticket> epicTickets, EpicQaData? qaData, AppSettings settings)
    {
        if (qaData is null)
            return (null, null, 0, 0, 0, null, null);

        // BR1: feature ticket scope = non-bug tickets
        var featureTickets = epicTickets.Where(t => t.IssueType != "Bug").ToList();

        // BR13: zero feature tickets → null coverage and pass rate
        if (featureTickets.Count == 0)
            return (null, null, 0, 0, 0, null, null);

        var coveredCount = 0;
        var totalPassRuns = 0;
        var totalExecutedRuns = 0; // PASS + FAIL only (BR3)
        var bugKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var ticket in featureTickets)
        {
            var ticketKey = ticket.Id;

            // BR2, BR12: get effective TE IDs for coverage (own Tests links, fallback to parent)
            var ownTeIds = qaData.TestsLinksByTicket.GetValueOrDefault(ticketKey, []);
            var effectiveTeIds = ownTeIds.Count > 0
                ? ownTeIds
                : (!string.IsNullOrEmpty(ticket.ParentTicketKey)
                    ? qaData.TestsLinksByTicket.GetValueOrDefault(ticket.ParentTicketKey, [])
                    : []);

            if (effectiveTeIds.Count > 0)
                coveredCount++;

            // BR3: accumulate pass/fail runs across effective TEs
            foreach (var teId in effectiveTeIds)
            {
                var runs = qaData.RunsByTeId.GetValueOrDefault(teId, []);
                totalPassRuns += runs.Count(r => r.Status == TestRunStatus.Pass);
                totalExecutedRuns += runs.Count(r => r.Status == TestRunStatus.Pass || r.Status == TestRunStatus.Fail);
            }

        }

        // BR4: collect unique bug keys via Blocks links on all feature tickets' TE IDs
        // BlocksLinksByTicket: key = bug ticket key, value = list of TE IDs that block it
        // We need to invert: for each feature ticket's TE (via Tests links), find what bugs they block
        // The data structure stores: bug ticket key → list of TE IDs that have a Blocks link to that bug
        // So we need to find all bug keys where at least one of their blocking TE IDs is in our feature epic's TEs
        var epicTeIds = featureTickets
            .SelectMany(t =>
            {
                var ownTeIds = qaData.TestsLinksByTicket.GetValueOrDefault(t.Id, []);
                return ownTeIds.Count > 0
                    ? ownTeIds
                    : (!string.IsNullOrEmpty(t.ParentTicketKey)
                        ? qaData.TestsLinksByTicket.GetValueOrDefault(t.ParentTicketKey, [])
                        : Enumerable.Empty<string>());
            })
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var (bugKey, teIds) in qaData.BlocksLinksByTicket)
        {
            if (teIds.Any(id => epicTeIds.Contains(id)))
                bugKeys.Add(bugKey);
        }

        var featureCount = featureTickets.Count;
        var coverageRate = Math.Round((decimal)coveredCount / featureCount * 100, 1);
        var passRate = totalExecutedRuns > 0
            ? Math.Round((decimal)totalPassRuns / totalExecutedRuns * 100, 1)
            : 0m;

        var thresholds = settings.QaHealthThresholds;
        var coverageRag = HealthScoreCalculator.MetricRag(coverageRate, thresholds.CoverageGreen, thresholds.CoverageAmber, higherIsBetter: true);
        var passRateRag = HealthScoreCalculator.MetricRag(passRate, thresholds.PassRateGreen, thresholds.PassRateAmber, higherIsBetter: true);

        return (coverageRate, passRate, bugKeys.Count, featureCount, coveredCount, coverageRag, passRateRag);
    }

    /// <summary>
    /// Builds a ticket entry with per-ticket QA fields (BR5–BR7).
    /// Bug tickets get null for all QA fields.
    /// </summary>
    private static EpicProgressTicketEntry BuildTicketEntry(
        Ticket ticket,
        HashSet<string> completedStatuses,
        EpicQaData? qaData)
    {
        var isDone = completedStatuses.Contains(ticket.CurrentStatus);

        if (qaData is null || ticket.IssueType == "Bug")
        {
            return new EpicProgressTicketEntry(
                ticket.Id,
                ticket.Summary,
                ticket.IssueType,
                ticket.StoryPoints,
                ticket.CurrentStatus,
                ticket.Assignee?.DisplayName,
                isDone,
                TestStatus: null,
                TestPassRate: null,
                TestBugsFound: null,
                TestRunSummary: null);
        }

        // BR12: own links first, fallback to parent
        var ownTeIds = qaData.TestsLinksByTicket.GetValueOrDefault(ticket.Id, []);
        var effectiveTeIds = ownTeIds.Count > 0
            ? ownTeIds
            : (!string.IsNullOrEmpty(ticket.ParentTicketKey)
                ? qaData.TestsLinksByTicket.GetValueOrDefault(ticket.ParentTicketKey, [])
                : []);

        if (effectiveTeIds.Count == 0)
        {
            return new EpicProgressTicketEntry(
                ticket.Id,
                ticket.Summary,
                ticket.IssueType,
                ticket.StoryPoints,
                ticket.CurrentStatus,
                ticket.Assignee?.DisplayName,
                isDone,
                TestStatus: "NoTests",
                TestPassRate: null,
                TestBugsFound: 0,
                TestRunSummary: null);
        }

        // Aggregate runs across all effective TEs
        var passCount = 0;
        var failCount = 0;
        var todoCount = 0;
        var executingCount = 0;
        var abortedCount = 0;

        foreach (var teId in effectiveTeIds)
        {
            var runs = qaData.RunsByTeId.GetValueOrDefault(teId, []);
            passCount += runs.Count(r => r.Status == TestRunStatus.Pass);
            failCount += runs.Count(r => r.Status == TestRunStatus.Fail);
            todoCount += runs.Count(r => r.Status == TestRunStatus.Todo);
            executingCount += runs.Count(r => r.Status == TestRunStatus.Executing);
            abortedCount += runs.Count(r => r.Status == TestRunStatus.Aborted);
        }

        // BR5/BR8: derive test status
        string testStatus;
        if (failCount > 0)
            testStatus = "Failed";
        else if (passCount > 0 && todoCount == 0 && executingCount == 0)
            testStatus = "Passed";
        else if (todoCount > 0 || executingCount > 0)
            testStatus = "InProgress";
        else
            testStatus = "NoTests"; // TEs exist but all runs are aborted — no effective test results

        // BR6: per-ticket pass rate
        var executedRuns = passCount + failCount;
        decimal? testPassRate = executedRuns > 0
            ? Math.Round((decimal)passCount / executedRuns * 100, 1)
            : null;

        // BR7: per-ticket bugs found via Blocks links
        var teIdSet = effectiveTeIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var ticketBugKeys = qaData.BlocksLinksByTicket
            .Where(kvp => kvp.Value.Any(id => teIdSet.Contains(id)))
            .Select(kvp => kvp.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var runSummary = new TestRunSummaryDto(passCount, failCount, todoCount, executingCount, abortedCount);

        return new EpicProgressTicketEntry(
            ticket.Id,
            ticket.Summary,
            ticket.IssueType,
            ticket.StoryPoints,
            ticket.CurrentStatus,
            ticket.Assignee?.DisplayName,
            isDone,
            TestStatus: testStatus,
            TestPassRate: testPassRate,
            TestBugsFound: ticketBugKeys.Count,
            TestRunSummary: runSummary);
    }
}
