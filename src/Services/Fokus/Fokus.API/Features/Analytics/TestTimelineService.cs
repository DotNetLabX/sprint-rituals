namespace Fokus.API.Features.Analytics;

// --- TestTimeline response records ---

public sealed record TestTimelineResponse(
    bool HasQaData,
    bool IsXrayEnabled,
    DateTime SprintStartDate,
    DateTime SprintEndDate,
    int PlanningWindowDays,
    IReadOnlyList<BurnupDayEntry> BurnupData,
    IReadOnlyList<ScopeChangeDayEntry> ScopeChangeOverlay,
    TestingCrunchResult TestingCrunch,
    PostSprintTestingResult PostSprintTesting,
    UntestedAtCloseResult UntestedAtClose,
    DevToTestGapResult DevToTestGap);

public sealed record BurnupDayEntry(
    int DayNumber,
    DateTime CalendarDate,
    bool IsWithinSprint,
    int CumulativePass,
    int CumulativeFail,
    int CumulativeTotal,
    int DailyPass,
    int DailyFail);

public sealed record ScopeChangeDayEntry(
    int DayNumber,
    DateTime CalendarDate,
    decimal AddedSp,
    decimal RemovedSp,
    decimal NetSp);

public sealed record TestingCrunchResult(
    bool IsCrunchFlagged,
    decimal? CrunchPercentage,
    int CrunchRunCount,
    int TotalRunCount,
    IReadOnlyList<CrunchTicketEntry> CrunchTickets);

public sealed record CrunchTicketEntry(
    string TicketKey,
    string Summary,
    string? AssigneeName,
    decimal? StoryPoints,
    int LateRunCount);

public sealed record PostSprintTestingResult(
    bool HasPostSprintTesting,
    decimal? PostSprintPercentage,
    int PostSprintRunCount,
    int TotalRunCount,
    IReadOnlyList<PostSprintTicketEntry> PostSprintTickets);

public sealed record PostSprintTicketEntry(
    string TicketKey,
    string Summary,
    string? AssigneeName,
    decimal? StoryPoints,
    int PostSprintRunCount);

public sealed record UntestedAtCloseResult(
    bool HasUntestedAtClose,
    int UntestedAtCloseCount,
    IReadOnlyList<UntestedTicketEntry> UntestedAtCloseTickets);

public sealed record UntestedTicketEntry(
    string TicketKey,
    string Summary,
    string? AssigneeName,
    decimal? StoryPoints,
    DateTime DevDoneDate);

public sealed record DevToTestGapResult(
    decimal? MedianGapDays,
    decimal? MedianGapDelta,
    string? MedianGapDirection,
    IReadOnlyList<GapTicketEntry> GapTickets);

public sealed record GapTicketEntry(
    string TicketKey,
    string Summary,
    string? AssigneeName,
    DateTime DevDoneDate,
    DateTime FirstTestDate,
    decimal GapDays);

public sealed record TestingCrunchFlag(
    decimal CrunchPercentage,
    int CrunchRunCount,
    int TotalRunCount);

// --- Service ---

public class TestTimelineService
{
    public TestTimelineResponse ComputeTimeline(
        List<TestExecution> sprintTEs,
        List<SprintMembership> memberships,
        List<StatusTransition> statusTransitions,
        Sprint sprint,
        Sprint? priorSprint,
        List<TestExecution>? priorSprintTEs,
        List<SprintMembership>? priorMemberships,
        List<StatusTransition>? priorStatusTransitions,
        AppSettings settings,
        string? subTeam)
    {
        // Apply sub-team filter
        var filteredMemberships = FilterMemberships(memberships, subTeam);

        // Resolve CycleTimeEndStage index for dev-done detection
        var (orderedStages, _) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, orderedStages);

        // Build transitions lookup by ticket (current sprint only)
        var transitionsByTicket = statusTransitions
            .GroupBy(t => t.TicketId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        // Build prior sprint transitions lookup (separate — different ticket scope)
        var priorTransitionsByTicket = priorStatusTransitions is not null
            ? priorStatusTransitions
                .GroupBy(t => t.TicketId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase)
            : null;

        // Collect all terminal runs with effective dates from filtered TEs
        var terminalRuns = CollectTerminalRunsWithDates(sprintTEs, filteredMemberships);

        // Separate within-sprint vs post-sprint runs
        var withinSprintRuns = terminalRuns.Where(r => r.EffectiveDate.Date <= sprint.EndDate.Date).ToList();
        var postSprintRuns = terminalRuns.Where(r => r.EffectiveDate.Date > sprint.EndDate.Date).ToList();

        // Compute components
        var burnupData = ComputeBurnupData(terminalRuns, sprint);
        var scopeChangeOverlay = ComputeScopeChangeOverlay(filteredMemberships, sprint);
        var crunchResult = ComputeTestingCrunch(withinSprintRuns, sprintTEs, filteredMemberships, sprint);
        var postSprintResult = ComputePostSprintTesting(withinSprintRuns, postSprintRuns, sprintTEs, filteredMemberships);
        var untestedResult = ComputeUntestedAtClose(sprintTEs, filteredMemberships, transitionsByTicket, sprint, orderedStages, endIndex);
        var gapResult = ComputeDevToTestGap(sprintTEs, filteredMemberships, transitionsByTicket, sprint, orderedStages, endIndex,
            priorSprint, priorSprintTEs, priorMemberships, priorTransitionsByTicket, settings, subTeam);

        return new TestTimelineResponse(
            HasQaData: true,
            IsXrayEnabled: true,
            SprintStartDate: sprint.StartDate,
            SprintEndDate: sprint.EndDate,
            PlanningWindowDays: settings.PlanningWindowDays,
            BurnupData: burnupData,
            ScopeChangeOverlay: scopeChangeOverlay,
            TestingCrunch: crunchResult,
            PostSprintTesting: postSprintResult,
            UntestedAtClose: untestedResult,
            DevToTestGap: gapResult);
    }

    /// <summary>
    /// Computes testing crunch flag data as a standalone method for reuse by SprintSummaryService.
    /// </summary>
    public static TestingCrunchFlag? ComputeCrunchFlag(
        List<TestExecution> sprintTEs,
        List<SprintMembership> memberships,
        Sprint sprint,
        string? subTeam)
    {
        var filteredMemberships = FilterMemberships(memberships, subTeam);
        var terminalRuns = CollectTerminalRunsWithDates(sprintTEs, filteredMemberships);
        var withinSprintRuns = terminalRuns.Where(r => r.EffectiveDate.Date <= sprint.EndDate.Date).ToList();

        var crunch = ComputeTestingCrunch(withinSprintRuns, sprintTEs, filteredMemberships, sprint);
        if (!crunch.IsCrunchFlagged || !crunch.CrunchPercentage.HasValue)
            return null;

        return new TestingCrunchFlag(crunch.CrunchPercentage.Value, crunch.CrunchRunCount, crunch.TotalRunCount);
    }

    // --- Burnup computation ---

    private static List<BurnupDayEntry> ComputeBurnupData(List<TerminalRunEntry> terminalRuns, Sprint sprint)
    {
        if (terminalRuns.Count == 0)
            return [];

        // Group runs by calendar date
        var runsByDate = terminalRuns
            .GroupBy(r => r.EffectiveDate.Date)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Determine range: sprint start through last run date
        var lastRunDate = terminalRuns.Max(r => r.EffectiveDate.Date);
        var days = new List<BurnupDayEntry>();

        var cumulativePass = 0;
        var cumulativeFail = 0;

        var current = sprint.StartDate.Date;
        var dayNumber = 1;
        while (current <= lastRunDate)
        {
            var runsToday = runsByDate.GetValueOrDefault(current, []);
            var dailyPass = runsToday.Count(r => r.IsPass);
            var dailyFail = runsToday.Count(r => !r.IsPass);
            cumulativePass += dailyPass;
            cumulativeFail += dailyFail;

            days.Add(new BurnupDayEntry(
                DayNumber: dayNumber,
                CalendarDate: current,
                IsWithinSprint: current <= sprint.EndDate.Date,
                CumulativePass: cumulativePass,
                CumulativeFail: cumulativeFail,
                CumulativeTotal: cumulativePass + cumulativeFail,
                DailyPass: dailyPass,
                DailyFail: dailyFail));

            current = current.AddDays(1);
            dayNumber++;
        }

        return days;
    }

    // --- Scope change overlay ---

    private static List<ScopeChangeDayEntry> ComputeScopeChangeOverlay(
        List<SprintMembership> memberships,
        Sprint sprint)
    {
        var eventsByDay = new Dictionary<DateTime, (decimal Added, decimal Removed)>();

        foreach (var m in memberships)
        {
            var sp = m.StoryPoints ?? 0m;

            // AddedAt: mid-sprint addition (after sprint start)
            if (m.AddedAt.Date > sprint.StartDate.Date)
            {
                var day = m.AddedAt.Date;
                if (!eventsByDay.TryGetValue(day, out var existing))
                    existing = (0m, 0m);
                eventsByDay[day] = (existing.Added + sp, existing.Removed);
            }

            // RemovedAt: mid-sprint removal
            if (m.RemovedAt.HasValue && m.RemovedAt.Value.Date > sprint.StartDate.Date)
            {
                var day = m.RemovedAt.Value.Date;
                if (!eventsByDay.TryGetValue(day, out var existing))
                    existing = (0m, 0m);
                eventsByDay[day] = (existing.Added, existing.Removed + sp);
            }
        }

        return eventsByDay
            .Where(kvp => kvp.Value.Added > 0 || kvp.Value.Removed > 0)
            .OrderBy(kvp => kvp.Key)
            .Select(kvp =>
            {
                var dayNumber = (kvp.Key - sprint.StartDate.Date).Days + 1;
                return new ScopeChangeDayEntry(
                    DayNumber: dayNumber,
                    CalendarDate: kvp.Key,
                    AddedSp: kvp.Value.Added,
                    RemovedSp: kvp.Value.Removed,
                    NetSp: kvp.Value.Added - kvp.Value.Removed);
            })
            .ToList();
    }

    // --- Testing crunch ---

    private static TestingCrunchResult ComputeTestingCrunch(
        List<TerminalRunEntry> withinSprintRuns,
        List<TestExecution> sprintTEs,
        List<SprintMembership> filteredMemberships,
        Sprint sprint)
    {
        var totalRunCount = withinSprintRuns.Count;

        // Last 2 calendar days before and including EndDate
        var crunchStart = sprint.EndDate.Date.AddDays(-1); // -1 so we get day N-1 and day N (2 days)
        var crunchRuns = withinSprintRuns.Where(r => r.EffectiveDate.Date >= crunchStart).ToList();
        var crunchRunCount = crunchRuns.Count;

        if (totalRunCount == 0)
        {
            return new TestingCrunchResult(false, null, 0, 0, []);
        }

        var crunchPercentage = (decimal)crunchRunCount / totalRunCount * 100;
        var isCrunchFlagged = crunchPercentage > 50;

        var crunchTickets = new List<CrunchTicketEntry>();
        if (isCrunchFlagged)
        {
            crunchTickets = BuildCrunchTickets(crunchRuns, sprintTEs, filteredMemberships);
        }

        return new TestingCrunchResult(
            IsCrunchFlagged: isCrunchFlagged,
            CrunchPercentage: Math.Round(crunchPercentage, 1),
            CrunchRunCount: crunchRunCount,
            TotalRunCount: totalRunCount,
            CrunchTickets: crunchTickets);
    }

    private static List<CrunchTicketEntry> BuildCrunchTickets(
        List<TerminalRunEntry> crunchRuns,
        List<TestExecution> sprintTEs,
        List<SprintMembership> filteredMemberships)
    {
        // Map run IDs to their TE, then TE to ticket keys via Tests links
        var runToTeMap = new Dictionary<string, string>(); // runId -> teId
        foreach (var te in sprintTEs)
        {
            foreach (var run in te.TestRuns)
                runToTeMap[run.Id] = te.Id;
        }

        var teToTickets = BuildTeToTicketsMap(sprintTEs);
        var membershipByTicket = filteredMemberships
            .Where(m => m.RemovedAt == null)
            .ToDictionary(m => m.TicketId, m => m, StringComparer.OrdinalIgnoreCase);

        // For each crunch run, map to ticket keys
        var lateRunsByTicket = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var run in crunchRuns)
        {
            if (!runToTeMap.TryGetValue(run.RunId, out var teId)) continue;
            var ticketKeys = teToTickets.GetValueOrDefault(teId, []);
            foreach (var key in ticketKeys)
            {
                if (!lateRunsByTicket.ContainsKey(key))
                    lateRunsByTicket[key] = 0;
                lateRunsByTicket[key]++;
            }
        }

        return lateRunsByTicket
            .OrderByDescending(kvp => kvp.Value)
            .Select(kvp =>
            {
                var m = membershipByTicket.GetValueOrDefault(kvp.Key);
                return new CrunchTicketEntry(
                    TicketKey: kvp.Key,
                    Summary: m?.Ticket?.Summary ?? kvp.Key,
                    AssigneeName: m?.Ticket?.Assignee?.DisplayName,
                    StoryPoints: m?.StoryPoints,
                    LateRunCount: kvp.Value);
            })
            .ToList();
    }

    // --- Post-sprint testing ---

    private static PostSprintTestingResult ComputePostSprintTesting(
        List<TerminalRunEntry> withinSprintRuns,
        List<TerminalRunEntry> postSprintRuns,
        List<TestExecution> sprintTEs,
        List<SprintMembership> filteredMemberships)
    {
        var postSprintRunCount = postSprintRuns.Count;

        if (postSprintRunCount == 0)
        {
            return new PostSprintTestingResult(false, null, 0, withinSprintRuns.Count + postSprintRunCount, []);
        }

        var totalRunCount = withinSprintRuns.Count + postSprintRunCount;
        var postSprintPercentage = totalRunCount > 0
            ? Math.Round((decimal)postSprintRunCount / totalRunCount * 100, 1)
            : (decimal?)null;

        var tickets = BuildPostSprintTickets(postSprintRuns, sprintTEs, filteredMemberships);

        return new PostSprintTestingResult(
            HasPostSprintTesting: true,
            PostSprintPercentage: postSprintPercentage,
            PostSprintRunCount: postSprintRunCount,
            TotalRunCount: totalRunCount,
            PostSprintTickets: tickets);
    }

    private static List<PostSprintTicketEntry> BuildPostSprintTickets(
        List<TerminalRunEntry> postSprintRuns,
        List<TestExecution> sprintTEs,
        List<SprintMembership> filteredMemberships)
    {
        var runToTeMap = new Dictionary<string, string>();
        foreach (var te in sprintTEs)
        {
            foreach (var run in te.TestRuns)
                runToTeMap[run.Id] = te.Id;
        }

        var teToTickets = BuildTeToTicketsMap(sprintTEs);
        var membershipByTicket = filteredMemberships
            .ToDictionary(m => m.TicketId, m => m, StringComparer.OrdinalIgnoreCase);

        var runsByTicket = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var run in postSprintRuns)
        {
            if (!runToTeMap.TryGetValue(run.RunId, out var teId)) continue;
            var ticketKeys = teToTickets.GetValueOrDefault(teId, []);
            foreach (var key in ticketKeys)
            {
                if (!runsByTicket.ContainsKey(key))
                    runsByTicket[key] = 0;
                runsByTicket[key]++;
            }
        }

        return runsByTicket
            .OrderByDescending(kvp => kvp.Value)
            .Select(kvp =>
            {
                var m = membershipByTicket.GetValueOrDefault(kvp.Key);
                return new PostSprintTicketEntry(
                    TicketKey: kvp.Key,
                    Summary: m?.Ticket?.Summary ?? kvp.Key,
                    AssigneeName: m?.Ticket?.Assignee?.DisplayName,
                    StoryPoints: m?.StoryPoints,
                    PostSprintRunCount: kvp.Value);
            })
            .ToList();
    }

    // --- Completed but untested at close ---

    private static UntestedAtCloseResult ComputeUntestedAtClose(
        List<TestExecution> sprintTEs,
        List<SprintMembership> filteredMemberships,
        Dictionary<string, List<StatusTransition>> transitionsByTicket,
        Sprint sprint,
        List<string> orderedStages,
        int endIndex)
    {
        if (endIndex < 0)
            return new UntestedAtCloseResult(false, 0, []);

        // Find tickets that reached CycleTimeEndStage before sprint.EndDate
        var devDoneByTicket = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in filteredMemberships.Where(m => m.RemovedAt == null))
        {
            var transitions = transitionsByTicket.GetValueOrDefault(m.TicketId, []);
            var devDone = transitions
                .Where(t => t.Timestamp.Date < sprint.EndDate.Date
                         && TransitionAttributionChecker.GetStageIndex(t.ToStatus, orderedStages) >= endIndex)
                .OrderBy(t => t.Timestamp)
                .FirstOrDefault();

            if (devDone is not null)
                devDoneByTicket[m.TicketId] = devDone.Timestamp;
        }

        if (devDoneByTicket.Count == 0)
            return new UntestedAtCloseResult(false, 0, []);

        // Find tickets that have NO terminal-status test runs with effective date <= sprint.EndDate
        var testedByEndDate = BuildTestedTicketsByDate(sprintTEs, sprint.EndDate.Date);

        var untestedTickets = new List<UntestedTicketEntry>();
        foreach (var (ticketKey, devDoneDate) in devDoneByTicket)
        {
            if (testedByEndDate.Contains(ticketKey))
                continue;

            var m = filteredMemberships.FirstOrDefault(mb =>
                string.Equals(mb.TicketId, ticketKey, StringComparison.OrdinalIgnoreCase) && mb.RemovedAt == null);

            untestedTickets.Add(new UntestedTicketEntry(
                TicketKey: ticketKey,
                Summary: m?.Ticket?.Summary ?? ticketKey,
                AssigneeName: m?.Ticket?.Assignee?.DisplayName,
                StoryPoints: m?.StoryPoints,
                DevDoneDate: devDoneDate));
        }

        var sorted = untestedTickets.OrderBy(t => t.DevDoneDate).ToList();
        return new UntestedAtCloseResult(sorted.Count > 0, sorted.Count, sorted);
    }

    private static HashSet<string> BuildTestedTicketsByDate(List<TestExecution> sprintTEs, DateTime endDate)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var te in sprintTEs)
        {
            var hasRunByDate = te.TestRuns.Any(r =>
                (r.Status == TestRunStatus.Pass || r.Status == TestRunStatus.Fail)
                && GetEffectiveDate(r) is DateTime ed
                && ed.Date <= endDate);

            if (!hasRunByDate) continue;

            foreach (var link in te.Links.Where(l => l.LinkType == TestExecutionLinkType.Tests))
                result.Add(link.TicketKey);
        }
        return result;
    }

    // --- Dev-to-test gap ---

    private static DevToTestGapResult ComputeDevToTestGap(
        List<TestExecution> sprintTEs,
        List<SprintMembership> filteredMemberships,
        Dictionary<string, List<StatusTransition>> transitionsByTicket,
        Sprint sprint,
        List<string> orderedStages,
        int endIndex,
        Sprint? priorSprint,
        List<TestExecution>? priorSprintTEs,
        List<SprintMembership>? priorMemberships,
        Dictionary<string, List<StatusTransition>>? priorTransitionsByTicket,
        AppSettings settings,
        string? subTeam)
    {
        var gapTickets = ComputeGapTickets(sprintTEs, filteredMemberships, transitionsByTicket, orderedStages, endIndex);

        decimal? medianGapDays = ComputeMedian(gapTickets.Select(g => g.GapDays).ToList());

        decimal? medianGapDelta = null;
        string? medianGapDirection = null;

        if (priorSprint is not null && priorSprintTEs is not null && priorMemberships is not null
            && priorTransitionsByTicket is not null)
        {
            var filteredPriorMemberships = FilterMemberships(priorMemberships, subTeam);
            var priorGapTickets = ComputeGapTickets(priorSprintTEs, filteredPriorMemberships, priorTransitionsByTicket, orderedStages, endIndex);
            var priorMedian = ComputeMedian(priorGapTickets.Select(g => g.GapDays).ToList());

            if (priorMedian.HasValue && medianGapDays.HasValue)
            {
                medianGapDelta = Math.Round(medianGapDays.Value - priorMedian.Value, 1);
                // Lower is better: delta < 0 = down (positive), delta > 0 = up (negative)
                medianGapDirection = medianGapDelta.Value < 0 ? "down" : medianGapDelta.Value > 0 ? "up" : "flat";
            }
        }

        return new DevToTestGapResult(
            MedianGapDays: medianGapDays.HasValue ? Math.Round(medianGapDays.Value, 1) : null,
            MedianGapDelta: medianGapDelta,
            MedianGapDirection: medianGapDirection,
            GapTickets: gapTickets.OrderByDescending(g => g.GapDays).ToList());
    }

    private static List<GapTicketEntry> ComputeGapTickets(
        List<TestExecution> sprintTEs,
        List<SprintMembership> memberships,
        Dictionary<string, List<StatusTransition>> transitionsByTicket,
        List<string> orderedStages,
        int endIndex)
    {
        if (endIndex < 0) return [];

        // Build: ticketKey -> earliest FinishedAt among terminal runs linked via Tests
        var teToTickets = BuildTeToTicketsMap(sprintTEs);
        var firstTestByTicket = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

        foreach (var te in sprintTEs)
        {
            var ticketKeys = teToTickets.GetValueOrDefault(te.Id, []);
            foreach (var key in ticketKeys)
            {
                var terminalFinishedAt = te.TestRuns
                    .Where(r => (r.Status == TestRunStatus.Pass || r.Status == TestRunStatus.Fail)
                             && r.FinishedAt.HasValue)
                    .Select(r => r.FinishedAt!.Value)
                    .OrderBy(d => d)
                    .FirstOrDefault();

                if (terminalFinishedAt == default) continue;

                if (!firstTestByTicket.TryGetValue(key, out var existing) || terminalFinishedAt < existing)
                    firstTestByTicket[key] = terminalFinishedAt;
            }
        }

        var membershipByTicket = memberships
            .Where(m => m.RemovedAt == null)
            .ToDictionary(m => m.TicketId, m => m, StringComparer.OrdinalIgnoreCase);

        var gapTickets = new List<GapTicketEntry>();
        foreach (var (ticketKey, firstTestDate) in firstTestByTicket)
        {
            var transitions = transitionsByTicket.GetValueOrDefault(ticketKey, []);
            var devDone = transitions
                .Where(t => TransitionAttributionChecker.GetStageIndex(t.ToStatus, orderedStages) >= endIndex)
                .OrderBy(t => t.Timestamp)
                .FirstOrDefault();

            if (devDone is null) continue;

            var gapDays = (decimal)(firstTestDate - devDone.Timestamp).TotalDays;
            var m = membershipByTicket.GetValueOrDefault(ticketKey);

            gapTickets.Add(new GapTicketEntry(
                TicketKey: ticketKey,
                Summary: m?.Ticket?.Summary ?? ticketKey,
                AssigneeName: m?.Ticket?.Assignee?.DisplayName,
                DevDoneDate: devDone.Timestamp,
                FirstTestDate: firstTestDate,
                GapDays: Math.Round(gapDays, 1)));
        }

        return gapTickets;
    }

    // --- Helpers ---

    private static List<TerminalRunEntry> CollectTerminalRunsWithDates(
        List<TestExecution> sprintTEs,
        List<SprintMembership> filteredMemberships)
    {
        var filteredTicketKeys = filteredMemberships
            .Select(m => m.TicketId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var teToTickets = BuildTeToTicketsMap(sprintTEs);

        var result = new List<TerminalRunEntry>();
        foreach (var te in sprintTEs)
        {
            var ticketKeys = teToTickets.GetValueOrDefault(te.Id, []);

            // Only include this TE if at least one linked ticket is in the filtered scope
            if (filteredMemberships.Count > 0 && !ticketKeys.Any(k => filteredTicketKeys.Contains(k)))
                continue;

            foreach (var run in te.TestRuns)
            {
                if (run.Status != TestRunStatus.Pass && run.Status != TestRunStatus.Fail)
                    continue;

                var effectiveDate = GetEffectiveDate(run);
                if (effectiveDate is null) continue;

                result.Add(new TerminalRunEntry(run.Id, effectiveDate.Value, run.Status == TestRunStatus.Pass));
            }
        }

        return result;
    }

    private static Dictionary<string, List<string>> BuildTeToTicketsMap(List<TestExecution> sprintTEs)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var te in sprintTEs)
        {
            var ticketKeys = te.Links
                .Where(l => l.LinkType == TestExecutionLinkType.Tests)
                .Select(l => l.TicketKey)
                .ToList();

            result[te.Id] = ticketKeys;
        }
        return result;
    }

    private static DateTime? GetEffectiveDate(TestRun run)
    {
        if (run.FinishedAt.HasValue) return run.FinishedAt.Value;
        if (run.StartedAt.HasValue) return run.StartedAt.Value;
        return null;
    }

    private static List<SprintMembership> FilterMemberships(List<SprintMembership> memberships, string? subTeam)
    {
        if (string.IsNullOrWhiteSpace(subTeam))
            return memberships;
        return memberships.Where(m => m.Ticket?.Assignee?.SubTeam == subTeam).ToList();
    }

    private static decimal? ComputeMedian(List<decimal> values)
    {
        if (values.Count == 0) return null;
        var sorted = values.OrderBy(v => v).ToList();
        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2
            : sorted[mid];
    }

    // --- Internal data transfer type ---

    private sealed record TerminalRunEntry(string RunId, DateTime EffectiveDate, bool IsPass);
}
