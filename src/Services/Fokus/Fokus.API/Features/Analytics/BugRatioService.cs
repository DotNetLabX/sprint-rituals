namespace Fokus.API.Features.Analytics;

// --- Response record hierarchy ---

public record BugRatioSprintInfo(int Id, string Name, DateTime StartDate, DateTime EndDate);

// --- Multi-sprint ---

public record BugRatioTeamTrendEntry(
    int SprintId,
    decimal BugRatioPercent,
    decimal BugSp,
    decimal NonBugSp,
    decimal CompletedSp);

public record BugRatioTeamMetrics(
    decimal BugRatioPercent,
    decimal TotalBugSp,
    decimal TotalNonBugSp,
    decimal TotalCompletedSp,
    List<BugRatioTeamTrendEntry> PerSprintTrend);

public record BugRatioIssueTypeEntry(
    string IssueType,
    int TicketCount,
    decimal SpTotal);

public record BugRatioAlertStatus(bool IsActive, int ConsecutiveSprintCount, int ThresholdPercent);

public record BugRatioDeveloperSprintBreakdown(
    int SprintId,
    decimal BugSp,
    decimal NonBugSp,
    decimal CompletedSp,
    decimal BugRatioPercent,
    int BugTicketCount,
    int NonBugTicketCount);

public record BugRatioDeveloperEntry(
    string AccountId,
    string DisplayName,
    string? SubTeam,
    string? AvatarUrl,
    decimal BugSp,
    decimal NonBugSp,
    decimal CompletedSp,
    decimal BugRatioPercent,
    int BugTicketCount,
    int NonBugTicketCount,
    List<BugRatioDeveloperSprintBreakdown> SprintBreakdowns,
    BugRatioAlertStatus Alert);

public record BugRatioMultiSprintResponse(
    List<BugRatioSprintInfo> Sprints,
    BugRatioTeamMetrics TeamMetrics,
    List<BugRatioIssueTypeEntry> IssueTypeBreakdown,
    List<BugRatioDeveloperEntry> Developers);

// --- Single-sprint ---

public record BugRatioMetricCard(
    string Name,
    decimal Value,
    string DisplayValue,
    decimal? Delta,
    string? DeltaDirection,
    string? DeltaPolarity);

public record BugRatioTeamSingleMetrics(
    BugRatioMetricCard BugRatioPercent,
    BugRatioMetricCard BugSp,
    BugRatioMetricCard NonBugSp);

public record BugRatioDeveloperDelta(
    decimal BugSpDelta,
    string BugSpDeltaDirection,
    string BugSpDeltaPolarity,
    decimal NonBugSpDelta,
    string NonBugSpDeltaDirection,
    string NonBugSpDeltaPolarity,
    decimal BugRatioPercentDelta,
    string BugRatioPercentDeltaDirection,
    string BugRatioPercentDeltaPolarity,
    int BugTicketCountDelta,
    string BugTicketCountDeltaDirection,
    string BugTicketCountDeltaPolarity,
    int NonBugTicketCountDelta,
    string NonBugTicketCountDeltaDirection,
    string NonBugTicketCountDeltaPolarity);

public record BugRatioDeveloperSingleEntry(
    string AccountId,
    string DisplayName,
    string? SubTeam,
    string? AvatarUrl,
    decimal BugSp,
    decimal NonBugSp,
    decimal CompletedSp,
    decimal BugRatioPercent,
    int BugTicketCount,
    int NonBugTicketCount,
    BugRatioDeveloperDelta? Delta,
    BugRatioAlertStatus Alert);

public record BugRatioSingleSprintResponse(
    BugRatioSprintInfo Sprint,
    BugRatioTeamSingleMetrics TeamMetrics,
    List<BugRatioIssueTypeEntry> IssueTypeBreakdown,
    List<BugRatioDeveloperSingleEntry> Developers);

// --- Response wrapper ---

public record BugRatioResponse(
    string Mode,
    BugRatioMultiSprintResponse? MultiSprint,
    BugRatioSingleSprintResponse? SingleSprint);

// --- Service ---

public class BugRatioService
{
    public BugRatioMultiSprintResponse ComputeMultiSprint(
        List<Sprint> targetSprints,
        List<Sprint> allClosedSprints,
        List<Developer> activeDevelopers,
        AppSettings settings,
        List<StatusTransition> statusTransitions,
        string? subTeam,
        HashSet<string>? excludedDeveloperIds = null)
    {
        var excludedStatuses = settings.ExcludedFromScopeStatuses;
        var defaultSpPerBug = settings.DefaultSpPerBug;

        var (orderedStages, startIndex) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, orderedStages);

        var sortedTarget = targetSprints.OrderBy(s => s.StartDate).ToList();
        var filteredDevelopers = FilterDevelopers(activeDevelopers, subTeam);

        var sprintInfos = sortedTarget
            .Select(s => new BugRatioSprintInfo(s.Id, s.Name, s.StartDate, s.EndDate))
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
            return new BugRatioTeamTrendEntry(
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

        var teamMetrics = new BugRatioTeamMetrics(
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
                return new BugRatioDeveloperSprintBreakdown(
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
                excludedStatuses, settings.BugRatioAlertThreshold,
                settings.BugRatioConsecutiveSprintCount, subTeam, defaultSpPerBug);

            return new BugRatioDeveloperEntry(
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

        return new BugRatioMultiSprintResponse(sprintInfos, teamMetrics, issueTypeBreakdown, developerEntries);
    }

    public BugRatioSingleSprintResponse ComputeSingleSprint(
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
        var defaultSpPerBug = settings.DefaultSpPerBug;
        var filteredDevelopers = FilterDevelopers(activeDevelopers, subTeam);

        var (orderedStages, startIndex) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, orderedStages);

        var sprintInfo = new BugRatioSprintInfo(
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

        var teamMetrics = new BugRatioTeamSingleMetrics(
            BuildMetricCard("Bug Ratio %", ratio, $"{ratio:0.#}%",
                priorRatio.HasValue ? ratio - priorRatio.Value : null, "positive-down"),
            BuildMetricCard("Bug SP", bugSp, $"{bugSp:0.#}",
                priorBugSp.HasValue ? bugSp - priorBugSp.Value : null, "positive-down"),
            BuildMetricCard("Non-Bug SP", nonBugSp, $"{nonBugSp:0.#}",
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

            BugRatioDeveloperDelta? delta = null;
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

                delta = new BugRatioDeveloperDelta(
                    Math.Round(bugSpDelta, 1), DeltaDirection(bugSpDelta), DeltaPolarity(bugSpDelta, positiveUp: false),
                    Math.Round(nonBugSpDelta, 1), DeltaDirection(nonBugSpDelta), DeltaPolarity(nonBugSpDelta, positiveUp: true),
                    Math.Round(ratioDelta, 1), DeltaDirection(ratioDelta), DeltaPolarity(ratioDelta, positiveUp: false),
                    bugCountDelta, DeltaDirection(bugCountDelta), DeltaPolarity(bugCountDelta, positiveUp: false),
                    nonBugCountDelta, DeltaDirection(nonBugCountDelta), DeltaPolarity(nonBugCountDelta, positiveUp: true));
            }

            var alert = EvaluateAlert(
                dev.Id, allClosedSprints, statusTransitions, orderedStages, endIndex,
                excludedStatuses, settings.BugRatioAlertThreshold,
                settings.BugRatioConsecutiveSprintCount, subTeam, defaultSpPerBug);

            return new BugRatioDeveloperSingleEntry(
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

        return new BugRatioSingleSprintResponse(sprintInfo, teamMetrics, issueTypeBreakdown, developerEntries);
    }

    // --- Alert evaluation (BR9, BR10, BR11) ---

    private BugRatioAlertStatus EvaluateAlert(
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
        // Evaluate sprints from most recent backward (BR9)
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

        return new BugRatioAlertStatus(consecutiveAbove >= consecutiveCount, consecutiveAbove, threshold);
    }

    // --- Transition-based completed memberships ---

    /// <summary>
    /// Returns memberships whose ticket has a qualifying completion transition during the sprint window.
    /// Excludes removed and excluded-from-scope tickets.
    /// </summary>
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

    /// <summary>
    /// Computes bug and non-bug completed SP using transition-based attribution.
    /// </summary>
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

    private static List<BugRatioIssueTypeEntry> BuildIssueTypeBreakdown(
        List<SprintMembership> completedMemberships,
        int defaultSpPerBug = 0)
    {
        return completedMemberships
            .GroupBy(m => m.Ticket?.IssueType ?? "Unknown")
            .Select(g =>
            {
                var spTotal = g.Sum(m => m.GetEffectiveSp(defaultSpPerBug) ?? 0m);
                return new BugRatioIssueTypeEntry(g.Key, g.Count(), Math.Round(spTotal, 1));
            })
            .OrderByDescending(e => e.TicketCount)
            .ToList();
    }

    // --- Metric card builder ---

    private static BugRatioMetricCard BuildMetricCard(
        string name,
        decimal value,
        string displayValue,
        decimal? delta,
        string polarity)
    {
        string? direction = null;
        string? deltaPolarity = null;

        if (delta.HasValue)
        {
            direction = delta.Value > 0 ? "up" : delta.Value < 0 ? "down" : "flat";
            deltaPolarity = polarity switch
            {
                "positive-up" => delta.Value > 0 ? "positive" : delta.Value < 0 ? "negative" : "neutral",
                "positive-down" => delta.Value < 0 ? "positive" : delta.Value > 0 ? "negative" : "neutral",
                _ => "neutral"
            };
        }

        return new BugRatioMetricCard(
            name,
            Math.Round(value, 1),
            displayValue,
            delta.HasValue ? Math.Round(delta.Value, 1) : null,
            direction,
            deltaPolarity);
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
