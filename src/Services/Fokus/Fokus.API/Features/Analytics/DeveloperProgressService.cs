namespace Fokus.API.Features.Analytics;

// --- Response record hierarchy ---

public record DeveloperProgressSprintInfo(int Id, string Name, DateTime StartDate, DateTime EndDate);

public record DeveloperProgressAlert(
    string AccountId,
    string DisplayName,
    string? AvatarUrl,
    decimal GapSp,
    decimal GapDays,
    decimal? GapDelta,
    string Direction);

public record CompletedTicketEntry(string Key, string Summary, decimal? StoryPoints, string IssueType);

public record DayBreakdownEntry(int Day, DateTime Date, List<CompletedTicketEntry> CompletedTickets, decimal CumulativeSp, decimal ExpectedCumulativeSp);

public record StalledTicketEntry(string Key, string Summary, string CurrentStatus, string IssueType, decimal? StoryPoints, int DaysSinceLastTransition);

public record DeveloperProgressEntry(
    string AccountId,
    string DisplayName,
    string? SubTeam,
    string? AvatarUrl,
    decimal AssignedSp,
    decimal CompletedSp,
    decimal FeatureCompletedSp,
    decimal BugCompletedSp,
    decimal CompletionPercent,
    int CapacityPercent,
    decimal DailyPace,
    bool IsBehindPace,
    decimal? PaceGapSp,
    List<StalledTicketEntry> StalledTickets,
    List<DayBreakdownEntry> DailyBreakdown);

public record DeveloperProgressResponse(
    bool HasActiveSprint,
    DeveloperProgressSprintInfo? Sprint,
    int CurrentDay,
    int TotalDays,
    bool IsGracePeriod,
    List<DeveloperProgressAlert> Alerts,
    List<DeveloperProgressEntry> Developers);

// --- Service ---

public class DeveloperProgressService
{
    public DeveloperProgressResponse ComputeProgress(
        Sprint? activeSprint,
        List<Developer> activeDevelopers,
        List<DeveloperSprintCapacity> capacityRecords,
        AppSettings settings,
        string? subTeam,
        List<Developer> allDevelopers,
        List<StatusTransition> statusTransitions,
        HashSet<string> excludedDeveloperIds)
    {
        if (activeSprint is null)
            return new DeveloperProgressResponse(false, null, 0, 0, false, [], []);

        // BR8: totalDays inclusive, min 1
        var totalDays = Math.Max(1, (activeSprint.EndDate.Date - activeSprint.StartDate.Date).Days + 1);

        // BR9: currentDay clamped to [1, totalDays]
        var currentDay = Math.Min(totalDays, Math.Max(1, (DateTime.UtcNow.Date - activeSprint.StartDate.Date).Days + 1));

        // BR11: grace period = first 2 days
        var isGracePeriod = currentDay <= 2;

        // Resolve end stage for completion check
        var (orderedStages, _) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, orderedStages);
        var (_, startIndex) = TransitionAttributionChecker.ResolveStartIndex(settings);

        // Capacity lookup: accountId -> capacityPercent (sprint-specific first, then developer default)
        var capacityLookup = capacityRecords
            .Where(c => c.SprintId == activeSprint.Id)
            .ToDictionary(c => c.DeveloperAccountId, c => c.CapacityPercent);

        // Filter developers: sub-team + active + not excluded
        var filteredDevelopers = activeDevelopers
            .Where(d => !excludedDeveloperIds.Contains(d.Id))
            .Where(d => d.IsActive)
            .Where(d => string.IsNullOrWhiteSpace(subTeam) || d.SubTeam == subTeam)
            .ToList();

        // Stall reference dates computed once (shared across all developers)
        var todayDate = DateTime.UtcNow.Date;
        var yesterdayDate = todayDate.AddDays(-1);

        // Build developer entries alongside yesterday's stall set for direction derivation
        var developerData = filteredDevelopers.Select(developer =>
        {
            var capacity = GetCapacity(capacityLookup, developer.Id, allDevelopers);

            // BR1-2: all non-removed memberships, all ticket types
            var memberships = activeSprint.Memberships
                .Where(m => m.Ticket?.AssigneeId == developer.Id && m.RemovedAt == null)
                .ToList();

            // Assigned SP: sum of effective SP (null excluded from sum per plan)
            var assignedSp = memberships
                .Select(m => m.GetEffectiveSp(settings.DefaultSpPerBug))
                .Where(sp => sp.HasValue)
                .Sum(sp => sp!.Value);

            // BR10: dailyPace = assignedSp * (capacity / 100) / totalDays
            var dailyPace = assignedSp > 0
                ? assignedSp * (capacity / 100m) / totalDays
                : 0m;

            // Completion: tickets completed in sprint via IsCompletedInSprint
            // Build per-ticket completion info for daily breakdown
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

                // BR3: completion day = (completedAt.Date - sprintStart.Date).Days + 1
                var completionDay = (completedAt.Value.Date - activeSprint.StartDate.Date).Days + 1;
                completionDay = Math.Min(completionDay, currentDay); // clamp to currentDay

                if (!completionsByDay.ContainsKey(completionDay))
                    completionsByDay[completionDay] = [];

                completionsByDay[completionDay].Add(new CompletedTicketEntry(
                    m.TicketId,
                    m.Ticket?.Summary ?? m.TicketId,
                    effectiveSp,
                    m.Ticket?.IssueType ?? "Unknown"));
            }

            // Build daily breakdown 1..currentDay with running cumulative
            var dailyBreakdown = new List<DayBreakdownEntry>();
            var cumulativeSp = 0m;
            for (var day = 1; day <= currentDay; day++)
            {
                var dayTickets = completionsByDay.TryGetValue(day, out var tickets) ? tickets : [];
                var daySp = dayTickets
                    .Where(t => t.StoryPoints.HasValue)
                    .Sum(t => t.StoryPoints!.Value);
                cumulativeSp += daySp;

                var expectedCumulative = Math.Round(dailyPace * day, 10); // keep precision for comparison
                var date = activeSprint.StartDate.Date.AddDays(day - 1);
                dailyBreakdown.Add(new DayBreakdownEntry(day, date, dayTickets, cumulativeSp, expectedCumulative));
            }

            // BR12-14: behind-pace detection (suppressed during grace period)
            var completionPercent = assignedSp > 0 ? completedSp / assignedSp * 100m : 0m;
            bool isBehindPace = false;
            decimal? paceGapSp = null;

            if (!isGracePeriod && dailyPace > 0)
            {
                var expectedAtCurrentDay = dailyPace * currentDay;
                var gap = expectedAtCurrentDay - completedSp;
                if (gap > dailyPace)
                {
                    isBehindPace = true;
                    paceGapSp = Math.Round(gap, 1);
                }
                else
                {
                    paceGapSp = Math.Round(gap, 1);
                }
            }

            // BR15-17: stall detection for today and yesterday (direction derivation)
            var stalledTickets = BuildStalledTickets(
                memberships, statusTransitions, activeSprint, orderedStages, startIndex, endIndex, todayDate);
            var yesterdayStalledKeys = BuildStalledTickets(
                memberships, statusTransitions, activeSprint, orderedStages, startIndex, endIndex, yesterdayDate)
                .Select(t => t.Key)
                .ToHashSet();

            var entry = new DeveloperProgressEntry(
                developer.Id,
                developer.DisplayName,
                developer.SubTeam,
                developer.AvatarUrl,
                Math.Round(assignedSp, 1),
                Math.Round(completedSp, 1),
                Math.Round(featureCompletedSp, 1),
                Math.Round(bugCompletedSp, 1),
                Math.Round(completionPercent, 1),
                capacity,
                Math.Round(dailyPace, 10), // preserve precision; rounded at usage
                isBehindPace,
                paceGapSp,
                stalledTickets,
                dailyBreakdown);

            return (Entry: entry, YesterdayStalledKeys: yesterdayStalledKeys, Dev: developer);
        }).ToList();

        var developerEntries = developerData.Select(d => d.Entry).ToList();

        // Build alerts (pace-change developers, suppressed during grace period)
        var alerts = new List<DeveloperProgressAlert>();
        if (!isGracePeriod)
        {
            foreach (var (entry, yesterdayStalledKeys, devRecord) in developerData)
            {
                // Gap delta: gap(currentDay) - gap(previousDay); null when breakdown has < 2 days
                decimal? gapDelta = null;
                if (entry.DailyBreakdown.Count >= 2)
                {
                    var last = entry.DailyBreakdown[^1];
                    var prev = entry.DailyBreakdown[^2];
                    var gapCurrent = last.ExpectedCumulativeSp - last.CumulativeSp;
                    var gapPrev = prev.ExpectedCumulativeSp - prev.CumulativeSp;
                    gapDelta = Math.Round(gapCurrent - gapPrev, 1);
                }

                // Stall signals (spec BR6 — stall takes priority over gap direction)
                var todayStalledKeys = entry.StalledTickets.Select(t => t.Key).ToHashSet();
                var hasNewStall = todayStalledKeys.Any(k => !yesterdayStalledKeys.Contains(k));
                var hadStallYesterday = yesterdayStalledKeys.Count > 0;
                var hasNoStallToday = todayStalledKeys.Count == 0;
                var isStallResolved = hadStallYesterday && hasNoStallToday;

                // Direction derivation (stall takes priority)
                string direction;
                if (hasNewStall)
                    direction = "new-stall";
                else if (isStallResolved)
                    direction = "stall-resolved";
                else if (gapDelta.HasValue && gapDelta.Value > 0)
                    direction = "worsening";
                else if (gapDelta.HasValue && gapDelta.Value < 0)
                    direction = "improving";
                else
                    direction = "stable";

                if (direction == "stable") continue;

                var gapSp = entry.PaceGapSp ?? 0m;
                var gapDays = entry.DailyPace > 0 && entry.PaceGapSp.HasValue
                    ? Math.Round(entry.PaceGapSp.Value / entry.DailyPace, 1)
                    : 0m;
                alerts.Add(new DeveloperProgressAlert(
                    entry.AccountId,
                    entry.DisplayName,
                    devRecord.AvatarUrl,
                    gapSp,
                    gapDays,
                    gapDelta,
                    direction));
            }

            // Sort: worsening/new-stall group first, then improving/stall-resolved
            // Within worsening: new-stall at top, then by gapDelta desc (largest gap increase first)
            // Within improving: stall-resolved at top, then by gapDelta asc (largest improvement first)
            alerts = alerts
                .OrderBy(a => a.Direction is "improving" or "stall-resolved" ? 1 : 0)
                .ThenBy(a => a.Direction is "new-stall" or "stall-resolved" ? 0 : 1)
                .ThenBy(a => a.Direction is "improving" or "stall-resolved"
                    ? a.GapDelta ?? 0m
                    : -(a.GapDelta ?? 0m))
                .ToList();
        }

        var sprintInfo = new DeveloperProgressSprintInfo(
            activeSprint.Id, activeSprint.Name, activeSprint.StartDate, activeSprint.EndDate);

        return new DeveloperProgressResponse(
            true, sprintInfo, currentDay, totalDays, isGracePeriod, alerts, developerEntries);
    }

    // --- Stall detection (BR15-17) ---

    private List<StalledTicketEntry> BuildStalledTickets(
        List<SprintMembership> memberships,
        List<StatusTransition> statusTransitions,
        Sprint sprint,
        List<string> orderedStages,
        int startIndex,
        int endIndex,
        DateTime referenceDate)
    {
        var stalled = new List<StalledTicketEntry>();

        foreach (var m in memberships)
        {
            // BR15a: non-removed (already filtered by caller)
            // BR15b: started in sprint
            var (isStarted, _) = TransitionAttributionChecker.IsStartedInSprint(
                m.TicketId, statusTransitions,
                sprint.StartDate, sprint.EndDate,
                orderedStages, startIndex);

            if (!isStarted) continue;

            // BR15c: NOT completed
            var (isCompleted, _) = TransitionAttributionChecker.IsCompletedInSprint(
                m.TicketId, statusTransitions,
                sprint.StartDate, sprint.EndDate,
                orderedStages, endIndex);

            if (isCompleted) continue;

            // BR15d: most recent transition > 2 business days ago
            var ticketTransitions = statusTransitions
                .Where(t => t.TicketId == m.TicketId)
                .OrderByDescending(t => t.Timestamp)
                .ToList();

            if (ticketTransitions.Count == 0) continue;

            var lastTransition = ticketTransitions[0].Timestamp;
            var businessDays = CountBusinessDays(lastTransition, referenceDate);

            if (businessDays <= 2) continue;

            stalled.Add(new StalledTicketEntry(
                m.TicketId,
                m.Ticket?.Summary ?? m.TicketId,
                m.Ticket?.CurrentStatus ?? m.FinalStatus,
                m.Ticket?.IssueType ?? "Unknown",
                m.GetEffectiveSp(0),
                businessDays));
        }

        return stalled;
    }

    // --- Capacity resolution ---

    private static int GetCapacity(
        Dictionary<string, int> capacityLookup,
        string developerId,
        List<Developer> allDevelopers)
    {
        if (capacityLookup.TryGetValue(developerId, out var cap))
            return cap;
        var dev = allDevelopers.FirstOrDefault(d => d.Id == developerId);
        return dev?.DefaultCapacityPercent ?? 100;
    }

    // --- Business day counter (BR17) ---
    // Counts Mon-Fri days from the day AFTER 'from' up to and including 'to'

    private static int CountBusinessDays(DateTime from, DateTime to)
    {
        var count = 0;
        var current = from.Date.AddDays(1); // exclude the transition day itself
        while (current <= to.Date)
        {
            if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
                count++;
            current = current.AddDays(1);
        }
        return count;
    }
}
