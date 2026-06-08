namespace Fokus.Domain.Analytics;

public class BugRatioCalculator
{
    public BugRatioMultiSprintData ComputeMultiSprint(
        List<Sprint> targetSprints,
        List<Sprint> allClosedSprints,
        List<Developer> activeDevelopers,
        AppSettings settings,
        List<StatusTransition> statusTransitions,
        string? subTeam,
        HashSet<string>? excludedDeveloperIds = null)
    {
        var excludedStatuses = settings.ExcludedFromScopeStatuses;
        var defaultSpPerBug = settings.BugRatio.DefaultSpPerBug;

        var (orderedStages, startIndex) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, orderedStages);

        var sortedTarget = targetSprints.OrderBy(s => s.StartDate).ToList();
        var filteredDevelopers = FilterDevelopers(activeDevelopers, subTeam);

        var sprintInfos = sortedTarget
            .Select(s => new BugRatioSprintData(s.Id, s.Name, s.StartDate, s.EndDate))
            .ToList();

        // Team-level per-sprint trend
        var perSprintTrend = sortedTarget.Select(s =>
        {
            var memberships = FilterMemberships(s.Memberships, subTeam, excludedDeveloperIds);
            var (bugSp, nonBugSp) = ComputeCompletedBugNonBugSp(
                memberships, statusTransitions, s.StartDate, s.EndDate,
                orderedStages, endIndex, excludedStatuses, defaultSpPerBug);
            var completedSp = bugSp + nonBugSp;
            var ratioPercent = completedSp > 0 ? bugSp / completedSp * 100 : 0m;
            return new BugRatioTeamTrendData(
                s.Id,
                Math.Round(ratioPercent, 1),
                Math.Round(bugSp, 1),
                Math.Round(nonBugSp, 1),
                Math.Round(completedSp, 1));
        }).ToList();

        // Team-level totals (ratio of totals, not average of ratios — BR8)
        var totalBugSp = perSprintTrend.Sum(t => t.BugSp);
        var totalNonBugSp = perSprintTrend.Sum(t => t.NonBugSp);
        var totalCompletedSp = totalBugSp + totalNonBugSp;
        var teamRatio = totalCompletedSp > 0 ? totalBugSp / totalCompletedSp * 100 : 0m;

        var teamMetrics = new BugRatioTeamMetricsData(
            Math.Round(teamRatio, 1),
            Math.Round(totalBugSp, 1),
            Math.Round(totalNonBugSp, 1),
            Math.Round(totalCompletedSp, 1),
            perSprintTrend);

        // Issue type breakdown across all target sprints (BR18)
        var allCompletedMemberships = sortedTarget
            .SelectMany(s => GetTransitionCompletedMemberships(
                FilterMemberships(s.Memberships, subTeam, excludedDeveloperIds),
                statusTransitions, s.StartDate, s.EndDate,
                orderedStages, endIndex, excludedStatuses))
            .ToList();
        var issueTypeBreakdown = BuildIssueTypeBreakdown(allCompletedMemberships, defaultSpPerBug);

        // Per-developer entries
        var developerEntries = filteredDevelopers.Select(dev =>
        {
            var sprintBreakdowns = sortedTarget.Select(s =>
            {
                var memberships = GetDeveloperMemberships(s, dev.Id, subTeam);
                var (bugSp, nonBugSp) = ComputeCompletedBugNonBugSp(
                    memberships, statusTransitions, s.StartDate, s.EndDate,
                    orderedStages, endIndex, excludedStatuses, defaultSpPerBug);
                var completedSp = bugSp + nonBugSp;
                var ratio = completedSp > 0 ? bugSp / completedSp * 100 : 0m;
                var completed = GetTransitionCompletedMemberships(
                    memberships, statusTransitions, s.StartDate, s.EndDate,
                    orderedStages, endIndex, excludedStatuses);
                var bugCount = completed.Count(IsBug);
                var nonBugCount = completed.Count(m => !IsBug(m));
                return new BugRatioDeveloperSprintData(
                    s.Id,
                    Math.Round(bugSp, 1),
                    Math.Round(nonBugSp, 1),
                    Math.Round(completedSp, 1),
                    Math.Round(ratio, 1),
                    bugCount,
                    nonBugCount);
            }).ToList();

            var devBugSp = sprintBreakdowns.Sum(b => b.BugSp);
            var devNonBugSp = sprintBreakdowns.Sum(b => b.NonBugSp);
            var devCompletedSp = devBugSp + devNonBugSp;
            var devRatio = devCompletedSp > 0 ? devBugSp / devCompletedSp * 100 : 0m;
            var devBugCount = sprintBreakdowns.Sum(b => b.BugTicketCount);
            var devNonBugCount = sprintBreakdowns.Sum(b => b.NonBugTicketCount);

            var alert = EvaluateAlert(
                dev.Id, allClosedSprints, statusTransitions, orderedStages, endIndex,
                excludedStatuses, settings.BugRatio.AlertThreshold,
                settings.BugRatio.ConsecutiveSprintCount, subTeam, defaultSpPerBug);

            return new BugRatioDeveloperData(
                dev.Id, dev.DisplayName, dev.SubTeam, dev.AvatarUrl,
                Math.Round(devBugSp, 1),
                Math.Round(devNonBugSp, 1),
                Math.Round(devCompletedSp, 1),
                Math.Round(devRatio, 1),
                devBugCount,
                devNonBugCount,
                sprintBreakdowns,
                alert);
        }).ToList();

        return new BugRatioMultiSprintData(sprintInfos, teamMetrics, issueTypeBreakdown, developerEntries);
    }

    public BugRatioSingleSprintData ComputeSingleSprint(
        Sprint targetSprint,
        Sprint? priorSprint,
        List<Sprint> allClosedSprints,
        List<Developer> activeDevelopers,
        AppSettings settings,
        List<StatusTransition> statusTransitions,
        string? subTeam,
        HashSet<string>? excludedDeveloperIds = null)
    {
        var excludedStatuses = settings.ExcludedFromScopeStatuses;
        var defaultSpPerBug = settings.BugRatio.DefaultSpPerBug;
        var filteredDevelopers = FilterDevelopers(activeDevelopers, subTeam);

        var (orderedStages, startIndex) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, orderedStages);

        var sprintInfo = new BugRatioSprintData(
            targetSprint.Id, targetSprint.Name, targetSprint.StartDate, targetSprint.EndDate);

        // Team metrics for current sprint
        var memberships = FilterMemberships(targetSprint.Memberships, subTeam, excludedDeveloperIds);
        var (bugSp, nonBugSp) = ComputeCompletedBugNonBugSp(
            memberships, statusTransitions, targetSprint.StartDate, targetSprint.EndDate,
            orderedStages, endIndex, excludedStatuses, defaultSpPerBug);
        var completedSp = bugSp + nonBugSp;
        var ratio = completedSp > 0 ? bugSp / completedSp * 100 : 0m;

        // Prior sprint team metrics for deltas
        decimal? priorBugSp = null;
        decimal? priorNonBugSp = null;
        decimal? priorCompletedSp = null;
        decimal? priorRatio = null;
        if (priorSprint is not null)
        {
            var priorMemberships = FilterMemberships(priorSprint.Memberships, subTeam, excludedDeveloperIds);
            var (pBugSp, pNonBugSp) = ComputeCompletedBugNonBugSp(
                priorMemberships, statusTransitions, priorSprint.StartDate, priorSprint.EndDate,
                orderedStages, endIndex, excludedStatuses, defaultSpPerBug);
            priorBugSp = pBugSp;
            priorNonBugSp = pNonBugSp;
            priorCompletedSp = pBugSp + pNonBugSp;
            priorRatio = priorCompletedSp > 0 ? priorBugSp / priorCompletedSp * 100 : 0m;
        }

        var teamMetrics = new BugRatioTeamSingleData(
            MetricCardBuilder.Build("Bug Ratio %", ratio, $"{ratio:0.#}%",
                priorRatio.HasValue ? ratio - priorRatio.Value : null, "positive-down"),
            MetricCardBuilder.Build("Bug SP", bugSp, $"{bugSp:0.#}",
                priorBugSp.HasValue ? bugSp - priorBugSp.Value : null, "positive-down"),
            MetricCardBuilder.Build("Non-Bug SP", nonBugSp, $"{nonBugSp:0.#}",
                priorNonBugSp.HasValue ? nonBugSp - priorNonBugSp.Value : null, "positive-up"));

        // Issue type breakdown for current sprint completed tickets (BR18)
        var completed = GetTransitionCompletedMemberships(
            memberships, statusTransitions, targetSprint.StartDate, targetSprint.EndDate,
            orderedStages, endIndex, excludedStatuses);
        var issueTypeBreakdown = BuildIssueTypeBreakdown(completed, defaultSpPerBug);

        // Per-developer entries
        var developerEntries = filteredDevelopers.Select(dev =>
        {
            var devMemberships = GetDeveloperMemberships(targetSprint, dev.Id, subTeam);
            var (devBugSp, devNonBugSp) = ComputeCompletedBugNonBugSp(
                devMemberships, statusTransitions, targetSprint.StartDate, targetSprint.EndDate,
                orderedStages, endIndex, excludedStatuses, defaultSpPerBug);
            var devCompletedSp = devBugSp + devNonBugSp;
            var devRatio = devCompletedSp > 0 ? devBugSp / devCompletedSp * 100 : 0m;
            var devCompleted = GetTransitionCompletedMemberships(
                devMemberships, statusTransitions, targetSprint.StartDate, targetSprint.EndDate,
                orderedStages, endIndex, excludedStatuses);
            var devBugCount = devCompleted.Count(IsBug);
            var devNonBugCount = devCompleted.Count(m => !IsBug(m));

            BugRatioDeveloperDeltaData? delta = null;
            if (priorSprint is not null)
            {
                var priorDevMemberships = GetDeveloperMemberships(priorSprint, dev.Id, subTeam);
                var (priorDevBugSp, priorDevNonBugSp) = ComputeCompletedBugNonBugSp(
                    priorDevMemberships, statusTransitions, priorSprint.StartDate, priorSprint.EndDate,
                    orderedStages, endIndex, excludedStatuses, defaultSpPerBug);
                var priorDevCompletedSp = priorDevBugSp + priorDevNonBugSp;
                var priorDevRatio = priorDevCompletedSp > 0 ? priorDevBugSp / priorDevCompletedSp * 100 : 0m;
                var priorDevCompleted = GetTransitionCompletedMemberships(
                    priorDevMemberships, statusTransitions, priorSprint.StartDate, priorSprint.EndDate,
                    orderedStages, endIndex, excludedStatuses);
                var priorDevBugCount = priorDevCompleted.Count(IsBug);
                var priorDevNonBugCount = priorDevCompleted.Count(m => !IsBug(m));

                var bugSpDelta = devBugSp - priorDevBugSp;
                var nonBugSpDelta = devNonBugSp - priorDevNonBugSp;
                var ratioDelta = devRatio - priorDevRatio;
                var bugCountDelta = devBugCount - priorDevBugCount;
                var nonBugCountDelta = devNonBugCount - priorDevNonBugCount;

                delta = new BugRatioDeveloperDeltaData(
                    Math.Round(bugSpDelta, 1), MetricCardBuilder.Direction(bugSpDelta), MetricCardBuilder.Polarity(bugSpDelta, positiveUp: false),
                    Math.Round(nonBugSpDelta, 1), MetricCardBuilder.Direction(nonBugSpDelta), MetricCardBuilder.Polarity(nonBugSpDelta, positiveUp: true),
                    Math.Round(ratioDelta, 1), MetricCardBuilder.Direction(ratioDelta), MetricCardBuilder.Polarity(ratioDelta, positiveUp: false),
                    bugCountDelta, MetricCardBuilder.Direction(bugCountDelta), MetricCardBuilder.Polarity(bugCountDelta, positiveUp: false),
                    nonBugCountDelta, MetricCardBuilder.Direction(nonBugCountDelta), MetricCardBuilder.Polarity(nonBugCountDelta, positiveUp: true));
            }

            var alert = EvaluateAlert(
                dev.Id, allClosedSprints, statusTransitions, orderedStages, endIndex,
                excludedStatuses, settings.BugRatio.AlertThreshold,
                settings.BugRatio.ConsecutiveSprintCount, subTeam, defaultSpPerBug);

            return new BugRatioDeveloperSingleData(
                dev.Id, dev.DisplayName, dev.SubTeam, dev.AvatarUrl,
                Math.Round(devBugSp, 1),
                Math.Round(devNonBugSp, 1),
                Math.Round(devCompletedSp, 1),
                Math.Round(devRatio, 1),
                devBugCount,
                devNonBugCount,
                delta,
                alert);
        }).ToList();

        return new BugRatioSingleSprintData(sprintInfo, teamMetrics, issueTypeBreakdown, developerEntries);
    }

    // --- Alert evaluation (BR9, BR10, BR11) ---

    private BugRatioAlertData EvaluateAlert(
        string developerId,
        List<Sprint> allClosedSprints,
        List<StatusTransition> statusTransitions,
        List<string> orderedStages,
        int endIndex,
        List<string> excludedStatuses,
        int threshold,
        int consecutiveCount,
        string? subTeam,
        int defaultSpPerBug = 0)
    {
        var sortedDesc = allClosedSprints.OrderByDescending(s => s.StartDate).ToList();

        var consecutiveAbove = 0;
        foreach (var sprint in sortedDesc)
        {
            var memberships = GetDeveloperMemberships(sprint, developerId, subTeam);
            var (bugSp, nonBugSp) = ComputeCompletedBugNonBugSp(
                memberships, statusTransitions, sprint.StartDate, sprint.EndDate,
                orderedStages, endIndex, excludedStatuses, defaultSpPerBug);
            var completedSp = bugSp + nonBugSp;

            // BR11: zero SP sprint resets streak
            if (completedSp == 0)
                break;

            var ratio = bugSp / completedSp * 100;
            if (ratio >= threshold)
            {
                consecutiveAbove++;
            }
            else
            {
                break;
            }
        }

        return new BugRatioAlertData(consecutiveAbove >= consecutiveCount, consecutiveAbove, threshold);
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

    private static (decimal BugSp, decimal NonBugSp) ComputeCompletedBugNonBugSp(
        List<SprintMembership> memberships,
        List<StatusTransition> statusTransitions,
        DateTime sprintStart,
        DateTime sprintEnd,
        List<string> orderedStages,
        int endIndex,
        List<string> excludedStatuses,
        int defaultSpPerBug)
    {
        var completed = GetTransitionCompletedMemberships(
            memberships, statusTransitions, sprintStart, sprintEnd,
            orderedStages, endIndex, excludedStatuses);

        var bugSp = completed.Where(IsBug).Sum(m => m.GetEffectiveSp(defaultSpPerBug) ?? 0m);
        var nonBugSp = completed.Where(m => !IsBug(m)).Sum(m => m.GetEffectiveSp(defaultSpPerBug) ?? 0m);
        return (bugSp, nonBugSp);
    }

    // --- Bug classification (BR1) ---

    private static bool IsBug(SprintMembership m) =>
        m.Ticket?.IssueType == "Bug";

    // --- Sub-team filtering (BR16) ---

    private static List<Developer> FilterDevelopers(List<Developer> developers, string? subTeam)
    {
        if (string.IsNullOrWhiteSpace(subTeam))
            return developers;
        return developers.Where(d => d.SubTeam == subTeam).ToList();
    }

    private static List<SprintMembership> FilterMemberships(
        IReadOnlyList<SprintMembership> memberships,
        string? subTeam,
        HashSet<string>? excludedDeveloperIds = null)
    {
        var result = memberships.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(subTeam))
            result = result.Where(m => m.Ticket?.Assignee?.SubTeam == subTeam);
        if (excludedDeveloperIds is { Count: > 0 })
            result = result.Where(m => m.Ticket?.AssigneeId == null || !excludedDeveloperIds.Contains(m.Ticket.AssigneeId));
        return result.ToList();
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

    // --- Issue type breakdown (BR18) ---

    private static List<BugRatioIssueTypeData> BuildIssueTypeBreakdown(
        List<SprintMembership> completedMemberships,
        int defaultSpPerBug = 0)
    {
        return completedMemberships
            .GroupBy(m => m.Ticket?.IssueType ?? "Unknown")
            .Select(g =>
            {
                var spTotal = g.Sum(m => m.GetEffectiveSp(defaultSpPerBug) ?? 0m);
                return new BugRatioIssueTypeData(g.Key, g.Count(), Math.Round(spTotal, 1));
            })
            .OrderByDescending(e => e.TicketCount)
            .ToList();
    }
}
