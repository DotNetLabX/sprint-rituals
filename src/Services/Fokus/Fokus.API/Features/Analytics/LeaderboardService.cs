namespace Fokus.API.Features.Analytics;

// --- Response record hierarchy ---

public record LeaderboardSprintInfo(int Id, string Name, DateTime StartDate, DateTime EndDate);

public record LeaderboardDeveloperSprintBreakdown(
    int SprintId,
    decimal FeatureSp,
    decimal BugSp,
    decimal TotalSp,
    int FeatureTickets,
    int BugTickets,
    int CapacityPercent);

public record LeaderboardDeveloperEntry(
    string AccountId,
    string DisplayName,
    string? SubTeam,
    string? AvatarUrl,
    decimal FeatureSp,
    decimal BugSp,
    decimal TotalSp,
    int FeatureTickets,
    int BugTickets,
    int TotalTickets,
    int CapacityPercent,
    List<LeaderboardDeveloperSprintBreakdown> SprintBreakdowns);

public record LeaderboardMultiSprintResponse(
    List<LeaderboardSprintInfo> Sprints,
    List<LeaderboardDeveloperEntry> Developers);

public record LeaderboardDeveloperDelta(
    decimal FeatureSpDelta,
    string FeatureSpDeltaDirection,
    string FeatureSpDeltaPolarity,
    decimal BugSpDelta,
    string BugSpDeltaDirection,
    string BugSpDeltaPolarity,
    decimal TotalSpDelta,
    string TotalSpDeltaDirection,
    string TotalSpDeltaPolarity,
    int FeatureTicketsDelta,
    string FeatureTicketsDeltaDirection,
    string FeatureTicketsDeltaPolarity,
    int BugTicketsDelta,
    string BugTicketsDeltaDirection,
    string BugTicketsDeltaPolarity);

public record LeaderboardDeveloperSingleEntry(
    string AccountId,
    string DisplayName,
    string? SubTeam,
    string? AvatarUrl,
    decimal FeatureSp,
    decimal BugSp,
    decimal TotalSp,
    int FeatureTickets,
    int BugTickets,
    int TotalTickets,
    int CapacityPercent,
    LeaderboardDeveloperDelta? Delta);

public record LeaderboardSingleSprintResponse(
    LeaderboardSprintInfo Sprint,
    List<LeaderboardDeveloperSingleEntry> Developers);

public record LeaderboardResponse(
    string Mode,
    LeaderboardMultiSprintResponse? MultiSprint,
    LeaderboardSingleSprintResponse? SingleSprint);

// --- Service ---

public class LeaderboardService
{
    public LeaderboardMultiSprintResponse ComputeMultiSprint(
        List<Sprint> targetSprints,
        List<Developer> activeDevelopers,
        AppSettings settings,
        List<StatusTransition> statusTransitions,
        string? subTeam,
        Dictionary<string, Dictionary<int, int>> capacityLookup,
        List<Developer> allDevelopers,
        HashSet<string>? excludedDeveloperIds = null)
    {
        var excludedStatuses = settings.ExcludedFromScopeStatuses;
        var defaultSpPerBug = settings.DefaultSpPerBug;

        var (orderedStages, _) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, orderedStages);

        var sortedTarget = targetSprints.OrderBy(s => s.StartDate).ToList();
        var filteredDevelopers = FilterDevelopers(activeDevelopers, subTeam);

        var sprintInfos = sortedTarget
            .Select(s => new LeaderboardSprintInfo(s.Id, s.Name, s.StartDate, s.EndDate))
            .ToList();

        var developerEntries = filteredDevelopers.Select(dev =>
        {
            var sprintBreakdowns = sortedTarget.Select(s =>
            {
                var memberships = GetDeveloperMemberships(s, dev.Id, subTeam);
                var completed = GetTransitionCompletedMemberships(
                    memberships, statusTransitions, s.StartDate, s.EndDate,
                    orderedStages, endIndex, excludedStatuses);
                var featureSp = completed.Where(m => !IsBug(m)).Sum(m => m.GetEffectiveSp(defaultSpPerBug) ?? 0m);
                var bugSp = completed.Where(m => IsBug(m)).Sum(m => m.GetEffectiveSp(defaultSpPerBug) ?? 0m);
                var totalSp = featureSp + bugSp;
                var featureTickets = completed.Count(m => !IsBug(m));
                var bugTickets = completed.Count(m => IsBug(m));
                var capacityPercent = GetCapacity(capacityLookup, dev.Id, s.Id, allDevelopers);
                return new LeaderboardDeveloperSprintBreakdown(
                    s.Id,
                    Math.Round(featureSp, 1),
                    Math.Round(bugSp, 1),
                    Math.Round(totalSp, 1),
                    featureTickets,
                    bugTickets,
                    capacityPercent);
            }).ToList();

            var devFeatureSp = sprintBreakdowns.Sum(b => b.FeatureSp);
            var devBugSp = sprintBreakdowns.Sum(b => b.BugSp);
            var devTotalSp = devFeatureSp + devBugSp;
            var devFeatureTickets = sprintBreakdowns.Sum(b => b.FeatureTickets);
            var devBugTickets = sprintBreakdowns.Sum(b => b.BugTickets);
            var devTotalTickets = devFeatureTickets + devBugTickets;
            var avgCapacity = sprintBreakdowns.Count > 0
                ? (int)Math.Round(sprintBreakdowns.Average(b => b.CapacityPercent))
                : 100;

            return new LeaderboardDeveloperEntry(
                dev.Id,
                dev.DisplayName,
                dev.SubTeam,
                dev.AvatarUrl,
                Math.Round(devFeatureSp, 1),
                Math.Round(devBugSp, 1),
                Math.Round(devTotalSp, 1),
                devFeatureTickets,
                devBugTickets,
                devTotalTickets,
                avgCapacity,
                sprintBreakdowns);
        })
        .OrderByDescending(d => d.TotalSp)
        .ThenBy(d => d.DisplayName)
        .ToList();

        return new LeaderboardMultiSprintResponse(sprintInfos, developerEntries);
    }

    public LeaderboardSingleSprintResponse ComputeSingleSprint(
        Sprint targetSprint,
        Sprint? priorSprint,
        List<Developer> activeDevelopers,
        AppSettings settings,
        List<StatusTransition> statusTransitions,
        string? subTeam,
        Dictionary<string, Dictionary<int, int>> capacityLookup,
        List<Developer> allDevelopers,
        HashSet<string>? excludedDeveloperIds = null)
    {
        var excludedStatuses = settings.ExcludedFromScopeStatuses;
        var defaultSpPerBug = settings.DefaultSpPerBug;
        var filteredDevelopers = FilterDevelopers(activeDevelopers, subTeam);

        var (orderedStages, _) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, orderedStages);

        var sprintInfo = new LeaderboardSprintInfo(
            targetSprint.Id, targetSprint.Name, targetSprint.StartDate, targetSprint.EndDate);

        var developerEntries = filteredDevelopers.Select(dev =>
        {
            var devMemberships = GetDeveloperMemberships(targetSprint, dev.Id, subTeam);
            var devCompleted = GetTransitionCompletedMemberships(
                devMemberships, statusTransitions, targetSprint.StartDate, targetSprint.EndDate,
                orderedStages, endIndex, excludedStatuses);
            var featureSp = devCompleted.Where(m => !IsBug(m)).Sum(m => m.GetEffectiveSp(defaultSpPerBug) ?? 0m);
            var bugSp = devCompleted.Where(m => IsBug(m)).Sum(m => m.GetEffectiveSp(defaultSpPerBug) ?? 0m);
            var totalSp = featureSp + bugSp;
            var featureTickets = devCompleted.Count(m => !IsBug(m));
            var bugTickets = devCompleted.Count(m => IsBug(m));
            var totalTickets = featureTickets + bugTickets;

            LeaderboardDeveloperDelta? delta = null;
            if (priorSprint is not null)
            {
                var priorMemberships = GetDeveloperMemberships(priorSprint, dev.Id, subTeam);
                var priorCompleted = GetTransitionCompletedMemberships(
                    priorMemberships, statusTransitions, priorSprint.StartDate, priorSprint.EndDate,
                    orderedStages, endIndex, excludedStatuses);
                var priorFeatureSp = priorCompleted.Where(m => !IsBug(m)).Sum(m => m.GetEffectiveSp(defaultSpPerBug) ?? 0m);
                var priorBugSp = priorCompleted.Where(m => IsBug(m)).Sum(m => m.GetEffectiveSp(defaultSpPerBug) ?? 0m);
                var priorTotalSp = priorFeatureSp + priorBugSp;
                var priorFeatureTickets = priorCompleted.Count(m => !IsBug(m));
                var priorBugTickets = priorCompleted.Count(m => IsBug(m));

                var featureSpDelta = featureSp - priorFeatureSp;
                var bugSpDelta = bugSp - priorBugSp;
                var totalSpDelta = totalSp - priorTotalSp;
                var featureTicketsDelta = featureTickets - priorFeatureTickets;
                var bugTicketsDelta = bugTickets - priorBugTickets;

                delta = new LeaderboardDeveloperDelta(
                    Math.Round(featureSpDelta, 1), DeltaDirection(featureSpDelta), DeltaPolarity(featureSpDelta, positiveUp: true),
                    Math.Round(bugSpDelta, 1), DeltaDirection(bugSpDelta), DeltaPolarity(bugSpDelta, positiveUp: false),
                    Math.Round(totalSpDelta, 1), DeltaDirection(totalSpDelta), "neutral",
                    featureTicketsDelta, DeltaDirection(featureTicketsDelta), DeltaPolarity(featureTicketsDelta, positiveUp: true),
                    bugTicketsDelta, DeltaDirection(bugTicketsDelta), DeltaPolarity(bugTicketsDelta, positiveUp: false));
            }

            var capacityPercent = GetCapacity(capacityLookup, dev.Id, targetSprint.Id, allDevelopers);

            return new LeaderboardDeveloperSingleEntry(
                dev.Id,
                dev.DisplayName,
                dev.SubTeam,
                dev.AvatarUrl,
                Math.Round(featureSp, 1),
                Math.Round(bugSp, 1),
                Math.Round(totalSp, 1),
                featureTickets,
                bugTickets,
                totalTickets,
                capacityPercent,
                delta);
        })
        .OrderByDescending(d => d.TotalSp)
        .ThenBy(d => d.DisplayName)
        .ToList();

        return new LeaderboardSingleSprintResponse(sprintInfo, developerEntries);
    }

    // --- Transition-based completed memberships ---

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

    // --- Capacity resolution ---

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

    // --- Bug classification ---

    private static bool IsBug(SprintMembership m) =>
        m.Ticket?.IssueType == "Bug";

    // --- Sub-team filtering ---

    private static List<Developer> FilterDevelopers(List<Developer> developers, string? subTeam)
    {
        if (string.IsNullOrWhiteSpace(subTeam))
            return developers;
        return developers.Where(d => d.SubTeam == subTeam).ToList();
    }

    private static List<SprintMembership> GetDeveloperMemberships(
        Sprint sprint,
        string developerId,
        string? subTeam)
    {
        return sprint.Memberships
            .Where(m => m.Ticket?.AssigneeId == developerId &&
                        (string.IsNullOrWhiteSpace(subTeam) || m.Ticket?.Assignee?.SubTeam == subTeam))
            .ToList();
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
