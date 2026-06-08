namespace Fokus.Domain.Analytics;

public class LeaderboardCalculator
{
    public LeaderboardMultiSprintData ComputeMultiSprint(
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
        var defaultSpPerBug = settings.BugRatio.DefaultSpPerBug;

        var (orderedStages, _) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, orderedStages);

        var sortedTarget = targetSprints.OrderBy(s => s.StartDate).ToList();
        var filteredDevelopers = FilterDevelopers(activeDevelopers, subTeam);

        var sprintInfos = sortedTarget
            .Select(s => new LeaderboardSprintData(s.Id, s.Name, s.StartDate, s.EndDate))
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
                return new LeaderboardDeveloperSprintData(
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

            return new LeaderboardDeveloperData(
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

        return new LeaderboardMultiSprintData(sprintInfos, developerEntries);
    }

    public LeaderboardSingleSprintData ComputeSingleSprint(
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
        var defaultSpPerBug = settings.BugRatio.DefaultSpPerBug;
        var filteredDevelopers = FilterDevelopers(activeDevelopers, subTeam);

        var (orderedStages, _) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, orderedStages);

        var sprintInfo = new LeaderboardSprintData(
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

            LeaderboardDeveloperDeltaData? delta = null;
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

                delta = new LeaderboardDeveloperDeltaData(
                    Math.Round(featureSpDelta, 1), MetricCardBuilder.Direction(featureSpDelta), MetricCardBuilder.Polarity(featureSpDelta, positiveUp: true),
                    Math.Round(bugSpDelta, 1), MetricCardBuilder.Direction(bugSpDelta), MetricCardBuilder.Polarity(bugSpDelta, positiveUp: false),
                    // Finding 1: TotalSpDeltaPolarity is the hard-coded literal "neutral" — must NOT be converted
                    // to MetricCardBuilder.Polarity(...) as that would compute sign-based polarity.
                    Math.Round(totalSpDelta, 1), MetricCardBuilder.Direction(totalSpDelta), "neutral",
                    featureTicketsDelta, MetricCardBuilder.Direction(featureTicketsDelta), MetricCardBuilder.Polarity(featureTicketsDelta, positiveUp: true),
                    bugTicketsDelta, MetricCardBuilder.Direction(bugTicketsDelta), MetricCardBuilder.Polarity(bugTicketsDelta, positiveUp: false));
            }

            var capacityPercent = GetCapacity(capacityLookup, dev.Id, targetSprint.Id, allDevelopers);

            return new LeaderboardDeveloperSingleData(
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

        return new LeaderboardSingleSprintData(sprintInfo, developerEntries);
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
}
