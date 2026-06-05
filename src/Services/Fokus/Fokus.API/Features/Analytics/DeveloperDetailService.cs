namespace Fokus.API.Features.Analytics;

// --- Response record hierarchy ---

public record DeveloperDetailInfo(
    string AccountId,
    string DisplayName,
    string? SubTeam,
    string? AvatarUrl,
    string Role,
    int DefaultCapacityPercent);

public record SprintTrendEntry(
    int SprintId,
    string SprintName,
    DateTime StartDate,
    DateTime EndDate,
    decimal FeatureSp,
    decimal BugSp,
    decimal TotalSp,
    decimal AssignedSp,
    decimal CompletionPercent,
    int CapacityPercent,
    decimal? RollingAverageSp,
    decimal BugPercent);

public record WorkAllocationSummary(
    decimal AverageBugPercent,
    int SprintsAboveTarget,
    int TotalSprints);

public record CurrentSprintDetail(
    DeveloperProgressSprintInfo Sprint,
    int CurrentDay,
    int TotalDays,
    decimal AssignedSp,
    decimal CompletedSp,
    decimal CompletionPercent,
    decimal FeatureCompletedSp,
    decimal BugCompletedSp,
    decimal DailyPace,
    bool IsBehindPace,
    decimal? PaceGapSp,
    List<DayBreakdownEntry> DailyBreakdown,
    List<TicketDetailEntry> Tickets);

public record TicketDetailEntry(
    string Key,
    string Summary,
    string CurrentStatus,
    decimal? StoryPoints,
    string IssueType,
    int DaysInCurrentStatus,
    string State,
    bool IsStalled);

public record DeveloperDetailResponse(
    DeveloperDetailInfo Developer,
    List<SprintTrendEntry> SprintTrends,
    WorkAllocationSummary WorkAllocation,
    CurrentSprintDetail? CurrentSprint,
    decimal BugRatioTarget,
    string JiraInstanceUrl);

// --- Service ---

public class DeveloperDetailService
{
    public DeveloperDetailResponse ComputeDetail(
        string accountId,
        Developer developer,
        List<Sprint> closedSprints,
        Sprint? activeSprint,
        Dictionary<string, Dictionary<int, int>> capacityLookup,
        List<DeveloperSprintCapacity> capacityRecords,
        AppSettings settings,
        List<StatusTransition> statusTransitions,
        int last,
        decimal bugRatioTarget,
        string jiraInstanceUrl)
    {
        var (orderedStages, startIndex) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, orderedStages);
        var defaultSpPerBug = settings.DefaultSpPerBug;
        var excludedStatuses = settings.ExcludedFromScopeStatuses;

        // Build all developers list for capacity resolution (single dev here)
        var devList = new List<Developer> { developer };

        // --- Cross-sprint trends ---
        // All sprints where developer has at least one non-removed membership, ordered chronologically.
        // The full qualifying list is used for rolling average look-back; TakeLast is applied at the end
        // so the `last` count is over post-exclusion sprints only (plan: "Range counts only sprints
        // passing inclusion rule").
        var qualifyingSprints = closedSprints
            .OrderBy(s => s.StartDate)
            .Where(s => s.Memberships.Any(m => m.Ticket?.AssigneeId == accountId && m.RemovedAt == null))
            .ToList();

        // Compute per-sprint trends over all qualifying sprints, applying exclusion guard inline.
        // Rolling average receives the full qualifying list so look-back is stable regardless of TakeLast.
        var allSprintTrends = new List<SprintTrendEntry>();
        for (var i = 0; i < qualifyingSprints.Count; i++)
        {
            var sprint = qualifyingSprints[i];
            var memberships = GetDeveloperMemberships(sprint, accountId);

            // Completed memberships (transition-based, excludedStatuses filter)
            var completed = GetTransitionCompletedMemberships(
                memberships, statusTransitions, sprint.StartDate, sprint.EndDate,
                orderedStages, endIndex, excludedStatuses);

            var featureSp = completed.Where(m => !IsBug(m)).Sum(m => m.GetEffectiveSp(defaultSpPerBug) ?? 0m);
            var bugSp = completed.Where(m => IsBug(m)).Sum(m => m.GetEffectiveSp(defaultSpPerBug) ?? 0m);
            var totalSp = featureSp + bugSp;

            // AssignedSp: all non-removed memberships, all types
            var assignedSp = memberships
                .Where(m => m.RemovedAt == null)
                .Select(m => m.GetEffectiveSp(defaultSpPerBug))
                .Where(sp => sp.HasValue)
                .Sum(sp => sp!.Value);

            var capacityPercent = GetCapacity(capacityLookup, accountId, sprint.Id, devList);

            // Exclude sprint: capacity == 0 AND totalSp == 0 (developer absent from this sprint)
            if (capacityPercent == 0 && totalSp == 0)
                continue;

            var completionPercent = assignedSp > 0 ? totalSp / assignedSp * 100m : 0m;
            var bugPercent = totalSp > 0 ? bugSp / totalSp * 100m : 0m;

            // Rolling average: 3-sprint window over all-types SP, computed over full qualifying list
            // so the window is stable regardless of how many sprints TakeLast will return.
            decimal? rollingAverage = ComputeRollingAverage(accountId, i, qualifyingSprints, capacityLookup, statusTransitions, settings, devList, defaultSpPerBug);

            allSprintTrends.Add(new SprintTrendEntry(
                sprint.Id,
                sprint.Name,
                sprint.StartDate,
                sprint.EndDate,
                Math.Round(featureSp, 1),
                Math.Round(bugSp, 1),
                Math.Round(totalSp, 1),
                Math.Round(assignedSp, 1),
                Math.Round(completionPercent, 1),
                capacityPercent,
                rollingAverage.HasValue ? Math.Round(rollingAverage.Value, 1) : null,
                Math.Round(bugPercent, 1)));
        }

        // Apply last filter after exclusion so the count is over qualifying sprints only (0 = all)
        var sprintTrends = last > 0
            ? allSprintTrends.TakeLast(last).ToList()
            : allSprintTrends;

        // --- Work allocation summary ---
        var sprintsWithSp = sprintTrends.Where(t => t.TotalSp > 0).ToList();
        var averageBugPercent = sprintsWithSp.Count > 0
            ? sprintsWithSp.Average(t => t.BugPercent)
            : 0m;
        var sprintsAboveTarget = sprintTrends.Count(t => t.BugPercent > bugRatioTarget);
        var workAllocation = new WorkAllocationSummary(
            Math.Round(averageBugPercent, 1),
            sprintsAboveTarget,
            sprintTrends.Count);

        // --- Current sprint detail ---
        CurrentSprintDetail? currentSprintDetail = null;
        if (activeSprint is not null)
        {
            currentSprintDetail = ComputeCurrentSprintDetail(
                accountId, developer, activeSprint, capacityRecords, settings,
                statusTransitions, orderedStages, startIndex, endIndex, defaultSpPerBug);
        }

        // --- Developer info ---
        var devInfo = new DeveloperDetailInfo(
            developer.Id,
            developer.DisplayName,
            developer.SubTeam,
            developer.AvatarUrl,
            developer.Role,
            developer.DefaultCapacityPercent);

        return new DeveloperDetailResponse(
            devInfo,
            sprintTrends,
            workAllocation,
            currentSprintDetail,
            bugRatioTarget,
            jiraInstanceUrl);
    }

    // --- Current sprint computation (mirrors DeveloperProgressService) ---

    private static CurrentSprintDetail ComputeCurrentSprintDetail(
        string accountId,
        Developer developer,
        Sprint activeSprint,
        List<DeveloperSprintCapacity> capacityRecords,
        AppSettings settings,
        List<StatusTransition> statusTransitions,
        List<string> orderedStages,
        int startIndex,
        int endIndex,
        int defaultSpPerBug)
    {
        var totalDays = Math.Max(1, (activeSprint.EndDate.Date - activeSprint.StartDate.Date).Days + 1);
        var currentDay = Math.Min(totalDays, Math.Max(1, (DateTime.UtcNow.Date - activeSprint.StartDate.Date).Days + 1));

        var capacityLookup = capacityRecords
            .Where(c => c.SprintId == activeSprint.Id)
            .ToDictionary(c => c.DeveloperAccountId, c => c.CapacityPercent);

        var capacity = capacityLookup.TryGetValue(accountId, out var cap)
            ? cap
            : developer.DefaultCapacityPercent;

        var memberships = activeSprint.Memberships
            .Where(m => m.Ticket?.AssigneeId == accountId && m.RemovedAt == null)
            .ToList();

        var assignedSp = memberships
            .Select(m => m.GetEffectiveSp(settings.DefaultSpPerBug))
            .Where(sp => sp.HasValue)
            .Sum(sp => sp!.Value);

        var dailyPace = assignedSp > 0
            ? assignedSp * (capacity / 100m) / totalDays
            : 0m;

        var completionsByDay = new Dictionary<int, List<CompletedTicketEntry>>();
        var completedSp = 0m;
        var featureCompletedSp = 0m;
        var bugCompletedSp = 0m;

        foreach (var m in memberships)
        {
            var (isCompleted, completedAt) = TransitionAttributionChecker.IsCompletedInSprint(
                m.TicketId, statusTransitions,
                activeSprint.StartDate, activeSprint.EndDate,
                orderedStages, endIndex);

            if (!isCompleted || completedAt is null) continue;

            var effectiveSp = m.GetEffectiveSp(settings.DefaultSpPerBug);
            if (effectiveSp.HasValue)
            {
                completedSp += effectiveSp.Value;
                if (m.Ticket?.IssueType == "Bug")
                    bugCompletedSp += effectiveSp.Value;
                else
                    featureCompletedSp += effectiveSp.Value;
            }

            var completionDay = (completedAt.Value.Date - activeSprint.StartDate.Date).Days + 1;
            completionDay = Math.Min(completionDay, currentDay);

            if (!completionsByDay.ContainsKey(completionDay))
                completionsByDay[completionDay] = [];

            completionsByDay[completionDay].Add(new CompletedTicketEntry(
                m.TicketId,
                m.Ticket?.Summary ?? m.TicketId,
                effectiveSp,
                m.Ticket?.IssueType ?? "Unknown"));
        }

        var dailyBreakdown = new List<DayBreakdownEntry>();
        var cumulativeSp = 0m;
        for (var day = 1; day <= currentDay; day++)
        {
            var dayTickets = completionsByDay.TryGetValue(day, out var dayCompletions) ? dayCompletions : [];
            var daySp = dayTickets.Where(t => t.StoryPoints.HasValue).Sum(t => t.StoryPoints!.Value);
            cumulativeSp += daySp;
            var expectedCumulative = Math.Round(dailyPace * day, 10);
            var date = activeSprint.StartDate.Date.AddDays(day - 1);
            dailyBreakdown.Add(new DayBreakdownEntry(day, date, dayTickets, cumulativeSp, expectedCumulative));
        }

        var completionPercent = assignedSp > 0 ? completedSp / assignedSp * 100m : 0m;
        var isGracePeriod = currentDay <= 2;
        bool isBehindPace = false;
        decimal? paceGapSp = null;

        if (!isGracePeriod && dailyPace > 0)
        {
            var expectedAtCurrentDay = dailyPace * currentDay;
            var gap = expectedAtCurrentDay - completedSp;
            isBehindPace = gap > dailyPace;
            paceGapSp = Math.Round(gap, 1);
        }

        // Ticket state derivation (BR10)
        var tickets = BuildTicketDetails(
            memberships, statusTransitions, activeSprint, orderedStages, startIndex, endIndex);

        var sprintInfo = new DeveloperProgressSprintInfo(
            activeSprint.Id, activeSprint.Name, activeSprint.StartDate, activeSprint.EndDate);

        return new CurrentSprintDetail(
            sprintInfo,
            currentDay,
            totalDays,
            Math.Round(assignedSp, 1),
            Math.Round(completedSp, 1),
            Math.Round(completionPercent, 1),
            Math.Round(featureCompletedSp, 1),
            Math.Round(bugCompletedSp, 1),
            Math.Round(dailyPace, 10),
            isBehindPace,
            paceGapSp,
            dailyBreakdown,
            tickets);
    }

    // --- Ticket state derivation (BR10) and ordering (BR12) ---

    private static List<TicketDetailEntry> BuildTicketDetails(
        List<SprintMembership> memberships,
        List<StatusTransition> statusTransitions,
        Sprint sprint,
        List<string> orderedStages,
        int startIndex,
        int endIndex)
    {
        var today = DateTime.UtcNow.Date;
        var entries = new List<TicketDetailEntry>();

        foreach (var m in memberships)
        {
            var (isStarted, _) = TransitionAttributionChecker.IsStartedInSprint(
                m.TicketId, statusTransitions,
                sprint.StartDate, sprint.EndDate,
                orderedStages, startIndex);

            var (isCompleted, _) = TransitionAttributionChecker.IsCompletedInSprint(
                m.TicketId, statusTransitions,
                sprint.StartDate, sprint.EndDate,
                orderedStages, endIndex);

            string state;
            bool isStalled = false;
            int daysInCurrentStatus;

            var ticketTransitions = statusTransitions
                .Where(t => t.TicketId == m.TicketId)
                .OrderByDescending(t => t.Timestamp)
                .ToList();

            if (isCompleted)
            {
                state = "done";
                // Days since last transition or sprint start
                daysInCurrentStatus = ticketTransitions.Count > 0
                    ? CountBusinessDays(ticketTransitions[0].Timestamp, today)
                    : CountBusinessDays(sprint.StartDate, today);
            }
            else if (!isStarted)
            {
                state = "not-started";
                daysInCurrentStatus = ticketTransitions.Count > 0
                    ? CountBusinessDays(ticketTransitions[0].Timestamp, today)
                    : CountBusinessDays(sprint.StartDate, today);
            }
            else
            {
                // Started and not completed — check stall (2+ business days since last transition)
                if (ticketTransitions.Count > 0)
                {
                    var businessDaysSinceTransition = CountBusinessDays(ticketTransitions[0].Timestamp, today);
                    daysInCurrentStatus = businessDaysSinceTransition;
                    isStalled = businessDaysSinceTransition > 2;
                }
                else
                {
                    daysInCurrentStatus = CountBusinessDays(sprint.StartDate, today);
                    isStalled = daysInCurrentStatus > 2;
                }
                state = isStalled ? "stalled" : "in-progress";
            }

            entries.Add(new TicketDetailEntry(
                m.TicketId,
                m.Ticket?.Summary ?? m.TicketId,
                m.Ticket?.CurrentStatus ?? m.FinalStatus,
                m.GetEffectiveSp(0),
                m.Ticket?.IssueType ?? "Unknown",
                daysInCurrentStatus,
                state,
                isStalled));
        }

        // BR12 ordering: stalled (most days desc), in-progress, not-started, done; within group SP desc
        var stateOrder = new Dictionary<string, int>
        {
            ["stalled"] = 0,
            ["in-progress"] = 1,
            ["not-started"] = 2,
            ["done"] = 3
        };

        return entries
            .OrderBy(t => stateOrder.GetValueOrDefault(t.State, 99))
            .ThenByDescending(t => t.IsStalled ? t.DaysInCurrentStatus : 0)
            .ThenByDescending(t => t.StoryPoints ?? 0m)
            .ToList();
    }

    // --- Rolling average: 3-sprint window, all-types SP, skip 0-capacity sprints ---

    private static decimal? ComputeRollingAverage(
        string accountId,
        int currentIndexInDisplayed,
        List<Sprint> displayedSprints,
        Dictionary<string, Dictionary<int, int>> capacityLookup,
        List<StatusTransition> statusTransitions,
        AppSettings settings,
        List<Developer> developers,
        int defaultSpPerBug)
    {
        var (orderedStages, _) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, orderedStages);
        var excludedStatuses = settings.ExcludedFromScopeStatuses;

        var qualifyingTotals = new List<decimal>();

        for (var i = currentIndexInDisplayed; i >= 0 && qualifyingTotals.Count < 3; i--)
        {
            var sprint = displayedSprints[i];
            var capacity = GetCapacity(capacityLookup, accountId, sprint.Id, developers);
            if (capacity == 0) continue;

            var memberships = GetDeveloperMemberships(sprint, accountId);
            var completed = GetTransitionCompletedMemberships(
                memberships, statusTransitions, sprint.StartDate, sprint.EndDate,
                orderedStages, endIndex, excludedStatuses);

            var totalSp = completed.Sum(m => m.GetEffectiveSp(defaultSpPerBug) ?? 0m);
            qualifyingTotals.Add(totalSp);
        }

        if (qualifyingTotals.Count < 3) return null;

        return qualifyingTotals.Average();
    }

    // --- Helpers ---

    private static bool IsBug(SprintMembership m) =>
        m.Ticket?.IssueType == "Bug";

    private static List<SprintMembership> GetDeveloperMemberships(Sprint sprint, string accountId) =>
        sprint.Memberships
            .Where(m => m.Ticket?.AssigneeId == accountId)
            .ToList();

    private static List<SprintMembership> GetTransitionCompletedMemberships(
        List<SprintMembership> memberships,
        List<StatusTransition> statusTransitions,
        DateTime sprintStart,
        DateTime sprintEnd,
        List<string> orderedStages,
        int endIndex,
        List<string> excludedStatuses)
    {
        var transitionsByTicket = statusTransitions
            .GroupBy(t => t.TicketId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        return memberships
            .Where(m =>
            {
                if (m.RemovedAt != null) return false;
                if (excludedStatuses.Contains(m.FinalStatus, StringComparer.OrdinalIgnoreCase)) return false;
                var ticketTransitions = transitionsByTicket.GetValueOrDefault(m.TicketId, []);
                var (isCompleted, _) = TransitionAttributionChecker.IsCompletedInSprint(
                    m.TicketId, ticketTransitions, sprintStart, sprintEnd, orderedStages, endIndex);
                return isCompleted;
            })
            .ToList();
    }

    private static int GetCapacity(
        Dictionary<string, Dictionary<int, int>> capacityLookup,
        string developerId,
        int sprintId,
        List<Developer> developers)
    {
        if (capacityLookup.TryGetValue(developerId, out var sprintMap) &&
            sprintMap.TryGetValue(sprintId, out var percent))
        {
            return percent;
        }
        var developer = developers.FirstOrDefault(d => d.Id == developerId);
        return developer?.DefaultCapacityPercent ?? 100;
    }

    // Business day counter — excludes transition day, counts Mon-Fri up to 'to'
    private static int CountBusinessDays(DateTime from, DateTime to)
    {
        var count = 0;
        var current = from.Date.AddDays(1);
        while (current <= to.Date)
        {
            if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
                count++;
            current = current.AddDays(1);
        }
        return count;
    }
}
