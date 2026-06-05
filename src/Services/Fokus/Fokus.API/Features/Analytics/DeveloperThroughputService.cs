namespace Fokus.API.Features.Analytics;

// --- Response record hierarchy ---

public record SprintSummaryItem(int Id, string Name, DateTime StartDate, DateTime EndDate);

public record SprintBreakdown(
    int SprintId,
    decimal SpAssigned,
    decimal SpCompleted,
    decimal CompletionPercent,
    int TicketsDone,
    int TicketsCarriedOver,
    int CapacityPercent,
    decimal? RollingAverageSpCompleted,
    decimal? SpAssignedDelta,
    decimal? SpCompletedDelta,
    decimal? CompletionPercentDelta,
    int? TicketsDoneDelta,
    int? TicketsCarriedOverDelta,
    string? SpAssignedDeltaDirection,
    string? SpCompletedDeltaDirection,
    string? CompletionPercentDeltaDirection,
    string? TicketsDoneDeltaDirection,
    string? TicketsCarriedOverDeltaDirection,
    string? SpAssignedDeltaPolarity,
    string? SpCompletedDeltaPolarity,
    string? CompletionPercentDeltaPolarity,
    string? TicketsDoneDeltaPolarity,
    string? TicketsCarriedOverDeltaPolarity);

public record DeveloperThroughputEntry(
    string AccountId,
    string DisplayName,
    string? SubTeam,
    string? AvatarUrl,
    List<SprintBreakdown> SprintBreakdowns);

public record DeveloperThroughputResponse(
    List<SprintSummaryItem> Sprints,
    List<DeveloperThroughputEntry> Developers);

// --- Service ---

public class DeveloperThroughputService
{
    public DeveloperThroughputResponse ComputeThroughput(
        List<Sprint> allLoadedSprints,
        List<int> targetSprintIds,
        List<Developer> activeDevelopers,
        List<DeveloperSprintCapacity> capacityRecords,
        AppSettings settings,
        string? subTeam,
        List<Developer> allDevelopers,
        List<StatusTransition> statusTransitions)
    {
        var defaultSpPerBug = settings.DefaultSpPerBug;

        var (orderedStages, startIndex) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, orderedStages);

        // C2: sub-team filtering (active developers only for display)
        var filteredDevelopers = FilterDevelopers(activeDevelopers, subTeam);

        // Sort all loaded sprints ascending by start date
        var sortedAllSprints = allLoadedSprints.OrderBy(s => s.StartDate).ToList();

        // Target sprints in ascending order
        var targetSprints = sortedAllSprints
            .Where(s => targetSprintIds.Contains(s.Id))
            .OrderBy(s => s.StartDate)
            .ToList();

        var isSingleSprint = targetSprintIds.Count == 1;

        // Build capacity lookup: accountId -> sprintId -> capacityPercent
        var capacityLookup = capacityRecords
            .GroupBy(c => c.DeveloperAccountId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(c => c.SprintId, c => c.CapacityPercent));

        // Find prior sprint for delta (single-sprint mode only)
        Sprint? priorSprint = null;
        if (isSingleSprint)
        {
            var targetSprint = targetSprints[0];
            var targetIndexInAll = sortedAllSprints.FindIndex(s => s.Id == targetSprint.Id);
            priorSprint = targetIndexInAll > 0 ? sortedAllSprints[targetIndexInAll - 1] : null;
        }

        var sprintSummaryItems = targetSprints
            .Select(s => new SprintSummaryItem(s.Id, s.Name, s.StartDate, s.EndDate))
            .ToList();

        var developerEntries = filteredDevelopers.Select(developer =>
        {
            var breakdowns = targetSprints.Select(sprint =>
            {
                var memberships = GetDeveloperMemberships(sprint, developer.Id, subTeam);
                var capacity = GetCapacity(capacityLookup, developer.Id, sprint.Id, allDevelopers);

                var (spAssigned, spCompleted, ticketsDone, ticketsCarriedOver) = ComputeMetrics(
                    memberships, statusTransitions, sprint.StartDate, sprint.EndDate,
                    orderedStages, startIndex, endIndex, defaultSpPerBug);

                var completionPercent = spAssigned > 0 ? spCompleted / spAssigned * 100 : 0;

                var rollingAverage = ComputeRollingAverage(
                    developer.Id, sprint.Id, sortedAllSprints, capacityLookup, statusTransitions,
                    settings, subTeam, allDevelopers, defaultSpPerBug);

                // Deltas (single-sprint only)
                decimal? spAssignedDelta = null;
                decimal? spCompletedDelta = null;
                decimal? completionPercentDelta = null;
                int? ticketsDoneDelta = null;
                int? ticketsCarriedOverDelta = null;
                string? spAssignedDeltaDirection = null;
                string? spCompletedDeltaDirection = null;
                string? completionPercentDeltaDirection = null;
                string? ticketsDoneDeltaDirection = null;
                string? ticketsCarriedOverDeltaDirection = null;
                string? spAssignedDeltaPolarity = null;
                string? spCompletedDeltaPolarity = null;
                string? completionPercentDeltaPolarity = null;
                string? ticketsDoneDeltaPolarity = null;
                string? ticketsCarriedOverDeltaPolarity = null;

                if (isSingleSprint && priorSprint is not null)
                {
                    var priorMemberships = GetDeveloperMemberships(priorSprint, developer.Id, subTeam);

                    var (priorSpAssigned, priorSpCompleted, priorTicketsDone, priorTicketsCarriedOver) = ComputeMetrics(
                        priorMemberships, statusTransitions, priorSprint.StartDate, priorSprint.EndDate,
                        orderedStages, startIndex, endIndex, defaultSpPerBug);

                    var priorCompletionPercent = priorSpAssigned > 0 ? priorSpCompleted / priorSpAssigned * 100 : 0;

                    spAssignedDelta = spAssigned - priorSpAssigned;
                    spCompletedDelta = spCompleted - priorSpCompleted;
                    completionPercentDelta = completionPercent - priorCompletionPercent;
                    ticketsDoneDelta = ticketsDone - priorTicketsDone;
                    ticketsCarriedOverDelta = ticketsCarriedOver - priorTicketsCarriedOver;

                    spAssignedDeltaDirection = DeltaDirection(spAssignedDelta.Value);
                    spCompletedDeltaDirection = DeltaDirection(spCompletedDelta.Value);
                    completionPercentDeltaDirection = DeltaDirection(completionPercentDelta.Value);
                    ticketsDoneDeltaDirection = DeltaDirection(ticketsDoneDelta.Value);
                    ticketsCarriedOverDeltaDirection = DeltaDirection(ticketsCarriedOverDelta.Value);

                    spAssignedDeltaPolarity = "neutral";
                    spCompletedDeltaPolarity = DeltaPolarity(spCompletedDelta.Value, positiveUp: true);
                    completionPercentDeltaPolarity = DeltaPolarity(completionPercentDelta.Value, positiveUp: true);
                    ticketsDoneDeltaPolarity = DeltaPolarity(ticketsDoneDelta.Value, positiveUp: true);
                    ticketsCarriedOverDeltaPolarity = DeltaPolarity(ticketsCarriedOverDelta.Value, positiveUp: false);
                }

                return new SprintBreakdown(
                    sprint.Id,
                    Math.Round(spAssigned, 1),
                    Math.Round(spCompleted, 1),
                    Math.Round(completionPercent, 1),
                    ticketsDone,
                    ticketsCarriedOver,
                    capacity,
                    rollingAverage.HasValue ? Math.Round(rollingAverage.Value, 1) : null,
                    spAssignedDelta.HasValue ? Math.Round(spAssignedDelta.Value, 1) : null,
                    spCompletedDelta.HasValue ? Math.Round(spCompletedDelta.Value, 1) : null,
                    completionPercentDelta.HasValue ? Math.Round(completionPercentDelta.Value, 1) : null,
                    ticketsDoneDelta,
                    ticketsCarriedOverDelta,
                    spAssignedDeltaDirection,
                    spCompletedDeltaDirection,
                    completionPercentDeltaDirection,
                    ticketsDoneDeltaDirection,
                    ticketsCarriedOverDeltaDirection,
                    spAssignedDeltaPolarity,
                    spCompletedDeltaPolarity,
                    completionPercentDeltaPolarity,
                    ticketsDoneDeltaPolarity,
                    ticketsCarriedOverDeltaPolarity);
            }).ToList();

            return new DeveloperThroughputEntry(developer.Id, developer.DisplayName, developer.SubTeam, developer.AvatarUrl, breakdowns);
        }).ToList();

        return new DeveloperThroughputResponse(sprintSummaryItems, developerEntries);
    }

    // --- Core metrics computation (transition-based, feature-only) ---

    private static (decimal SpAssigned, decimal SpCompleted, int TicketsDone, int TicketsCarriedOver) ComputeMetrics(
        List<SprintMembership> memberships,
        List<StatusTransition> statusTransitions,
        DateTime sprintStart,
        DateTime sprintEnd,
        List<string> orderedStages,
        int startIndex,
        int endIndex,
        int defaultSpPerBug)
    {
        decimal spAssigned = 0;
        decimal spCompleted = 0;
        int ticketsDone = 0;
        int ticketsCarriedOver = 0;

        foreach (var m in memberships.Where(m => m.RemovedAt == null && !IsBug(m)))
        {
            var (isStarted, _) = TransitionAttributionChecker.IsStartedInSprint(
                m.TicketId, statusTransitions, sprintStart, sprintEnd, orderedStages, startIndex);

            if (!isStarted)
                continue;

            var sp = m.GetEffectiveSp(defaultSpPerBug) ?? 0m;
            spAssigned += sp;

            var (isCompleted, _) = TransitionAttributionChecker.IsCompletedInSprint(
                m.TicketId, statusTransitions, sprintStart, sprintEnd, orderedStages, endIndex);

            if (isCompleted)
            {
                spCompleted += sp;
                ticketsDone++;
            }
            else
            {
                ticketsCarriedOver++;
            }
        }

        return (spAssigned, spCompleted, ticketsDone, ticketsCarriedOver);
    }

    // --- Bug classification ---

    private static bool IsBug(SprintMembership m) =>
        m.Ticket?.IssueType == "Bug";

    // --- Sub-team filtering (C2) ---

    private static List<Developer> FilterDevelopers(List<Developer> developers, string? subTeam)
    {
        if (string.IsNullOrWhiteSpace(subTeam))
            return developers;

        return developers.Where(d => d.SubTeam == subTeam).ToList();
    }

    private static List<SprintMembership> GetDeveloperMemberships(Sprint sprint, string developerId, string? subTeam)
    {
        return sprint.Memberships
            .Where(m => m.Ticket?.AssigneeId == developerId &&
                        (string.IsNullOrWhiteSpace(subTeam) || m.Ticket?.Assignee?.SubTeam == subTeam))
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

    // --- Rolling average (capacity-aware, 3-sprint window, transition-based) ---

    private static decimal? ComputeRollingAverage(
        string developerId,
        int currentSprintId,
        List<Sprint> sortedAllSprints,
        Dictionary<string, Dictionary<int, int>> capacityLookup,
        List<StatusTransition> statusTransitions,
        AppSettings settings,
        string? subTeam,
        List<Developer> developers,
        int defaultSpPerBug = 0)
    {
        var (orderedStages, startIndex) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, orderedStages);

        var currentIndex = sortedAllSprints.FindIndex(s => s.Id == currentSprintId);
        if (currentIndex < 0) return null;

        // Look backward from current sprint (inclusive) collecting up to 3 qualifying sprints
        var qualifyingSpCompleted = new List<decimal>();
        for (var i = currentIndex; i >= 0 && qualifyingSpCompleted.Count < 3; i--)
        {
            var sprint = sortedAllSprints[i];
            var capacity = GetCapacity(capacityLookup, developerId, sprint.Id, developers);
            if (capacity == 0) continue;

            var memberships = GetDeveloperMemberships(sprint, developerId, subTeam);
            var (_, spCompleted, _, _) = ComputeMetrics(
                memberships, statusTransitions, sprint.StartDate, sprint.EndDate,
                orderedStages, startIndex, endIndex, defaultSpPerBug);

            qualifyingSpCompleted.Add(spCompleted);
        }

        if (qualifyingSpCompleted.Count < 3) return null;

        return qualifyingSpCompleted.Average();
    }

    // --- Delta helpers ---

    private static string DeltaDirection(decimal delta) =>
        delta > 0 ? "up" : delta < 0 ? "down" : "flat";

    private static string DeltaDirection(int delta) =>
        delta > 0 ? "up" : delta < 0 ? "down" : "flat";

    private static string DeltaPolarity(decimal delta, bool positiveUp) =>
        positiveUp
            ? (delta > 0 ? "positive" : delta < 0 ? "negative" : "neutral")
            : (delta < 0 ? "positive" : delta > 0 ? "negative" : "neutral");

    private static string DeltaPolarity(int delta, bool positiveUp) =>
        positiveUp
            ? (delta > 0 ? "positive" : delta < 0 ? "negative" : "neutral")
            : (delta < 0 ? "positive" : delta > 0 ? "negative" : "neutral");
}
