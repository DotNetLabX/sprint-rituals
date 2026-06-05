namespace Fokus.API.Features.Analytics;

// --- Response record hierarchy ---

public record CarryOverSprintInfo(int Id, string Name, DateTime StartDate, DateTime EndDate);

// --- Multi-sprint ---

public record CarryOverSummaryMetrics(
    decimal AverageCarryOverRate,
    decimal AverageCarryOverSp,
    int TotalZombieTickets);

public record CarryOverStatusDistributionEntry(
    string StageName,
    int TicketCount,
    decimal SpTotal,
    decimal Percentage);

public record CarryOverPerSprintData(
    int SprintId,
    decimal CarryOverSp,
    int CarryOverTicketCount,
    decimal CarryOverRate,
    decimal TotalScopeSp,
    List<CarryOverStatusDistributionEntry> StatusDistribution);

public record CarryOverIssueTypeEntry(
    string IssueType,
    int TicketCount,
    decimal SpTotal,
    decimal Percentage);

public record CarryOverZombieSummary(
    string TicketKey,
    string Summary,
    string IssueType,
    string CurrentStatus,
    decimal? StoryPoints,
    int SprintCount);

public record CarryOverMultiSprintResponse(
    List<CarryOverSprintInfo> Sprints,
    CarryOverSummaryMetrics SummaryMetrics,
    List<CarryOverPerSprintData> PerSprintData,
    List<CarryOverIssueTypeEntry> IssueTypeBreakdown,
    List<CarryOverZombieSummary> ZombieTickets);

// --- Single-sprint ---

public record CarryOverSingleSprintMetrics(
    ScopeMetricCard CarryOverRate,
    ScopeMetricCard CarryOverSp,
    ScopeMetricCard CarryOverTicketCount);

public record CarryOverDestinationBucket(int Count, decimal Sp);

public record CarryOverDestination(
    int PriorSprintId,
    string PriorSprintName,
    int PriorCarryOverCount,
    decimal PriorCarryOverSp,
    CarryOverDestinationBucket Completed,
    CarryOverDestinationBucket CarriedAgain,
    CarryOverDestinationBucket Removed,
    CarryOverDestinationBucket Dropped);

public record CarryOverTicketEntry(
    string TicketKey,
    string Summary,
    string IssueType,
    decimal? StoryPoints,
    string FinalStatus,
    string WorkflowStage,
    int SprintCount,
    bool IsZombie,
    bool IsExcluded);

public record ZombieTrajectorySprintEntry(int SprintId, string SprintName, string FinalStatus);

public record ZombieTrajectoryEntry(
    string TicketKey,
    string Summary,
    string IssueType,
    decimal? StoryPoints,
    string CurrentStatus,
    int SprintCount,
    List<ZombieTrajectorySprintEntry> Sprints);

public record CarryOverSingleSprintResponse(
    CarryOverSprintInfo Sprint,
    CarryOverSingleSprintMetrics Metrics,
    List<CarryOverStatusDistributionEntry> StatusDistribution,
    List<CarryOverIssueTypeEntry> IssueTypeBreakdown,
    CarryOverDestination? CarryOverDestination,
    List<CarryOverTicketEntry> Tickets,
    List<ZombieTrajectoryEntry> ZombieTrajectories);

// --- Response wrapper ---

public record CarryOverResponse(
    string Mode,
    CarryOverMultiSprintResponse? MultiSprint,
    CarryOverSingleSprintResponse? SingleSprint);

// --- Service ---

public class CarryOverService
{
    public CarryOverMultiSprintResponse ComputeMultiSprint(
        List<Sprint> selectedSprints,
        List<Sprint> allSyncedSprints,
        List<StatusTransition> statusTransitions,
        AppSettings settings,
        string? subTeam,
        HashSet<string>? excludedDeveloperIds = null)
    {
        var sortedSelected = selectedSprints.OrderBy(s => s.StartDate).ToList();
        var excludedStatuses = settings.ExcludedFromScopeStatuses;
        var workflowStages = settings.WorkflowStages;
        var defaultSpPerBug = settings.DefaultSpPerBug;

        // Resolve transition boundaries once
        var (orderedStages, startIndex) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, orderedStages);

        var sprintInfos = sortedSelected
            .Select(s => new CarryOverSprintInfo(s.Id, s.Name, s.StartDate, s.EndDate))
            .ToList();

        var perSprintData = sortedSelected
            .Select(s =>
            {
                var memberships = FilterMemberships(s.Memberships, subTeam, excludedDeveloperIds);
                return ComputePerSprintData(
                    s, memberships, statusTransitions,
                    orderedStages, startIndex, endIndex,
                    excludedStatuses, workflowStages, defaultSpPerBug);
            })
            .ToList();

        var avgCarryOverRate = perSprintData.Count > 0
            ? Math.Round(perSprintData.Average(d => d.CarryOverRate), 1)
            : 0;

        var avgCarryOverSp = perSprintData.Count > 0
            ? Math.Round(perSprintData.Average(d => d.CarryOverSp), 1)
            : 0;

        var transitionsByTicket = statusTransitions
            .GroupBy(t => t.TicketId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        // Issue type breakdown: carry-over tickets across all selected sprints (feature-only)
        var allCarryOverMemberships = sortedSelected
            .SelectMany(s =>
            {
                var memberships = FilterMemberships(s.Memberships, subTeam, excludedDeveloperIds);
                return memberships.Where(m =>
                {
                    if (m.RemovedAt != null || m.Ticket?.IssueType == "Bug") return false;
                    var ticketTransitions = transitionsByTicket.GetValueOrDefault(m.TicketId, []);
                    var (isStarted, _) = TransitionAttributionChecker.IsStartedInSprint(
                        m.TicketId, ticketTransitions, s.StartDate, s.EndDate, orderedStages, startIndex);
                    var (isCompleted, _) = TransitionAttributionChecker.IsCompletedInSprint(
                        m.TicketId, ticketTransitions, s.StartDate, s.EndDate, orderedStages, endIndex);
                    return TransitionAttributionChecker.IsCarryOver(isStarted, isCompleted);
                });
            })
            .ToList();

        var issueTypeBreakdown = BuildIssueTypeBreakdown(allCarryOverMemberships, excludedStatuses, defaultSpPerBug);

        // Zombie detection
        var selectedSprintIds = new HashSet<int>(sortedSelected.Select(s => s.Id));
        var zombieTicketKeys = GetZombieTicketKeys(allSyncedSprints);

        var zombieTickets = new List<CarryOverZombieSummary>();
        var seenZombieKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var sprint in sortedSelected)
        {
            var memberships = FilterMemberships(sprint.Memberships, subTeam, excludedDeveloperIds);
            foreach (var m in memberships)
            {
                if (m.RemovedAt != null || m.Ticket?.IssueType == "Bug") continue;
                var ticketTransitions = transitionsByTicket.GetValueOrDefault(m.TicketId, []);
                var (isStarted, _) = TransitionAttributionChecker.IsStartedInSprint(
                    m.TicketId, ticketTransitions, sprint.StartDate, sprint.EndDate, orderedStages, startIndex);
                var (isCompleted, _) = TransitionAttributionChecker.IsCompletedInSprint(
                    m.TicketId, ticketTransitions, sprint.StartDate, sprint.EndDate, orderedStages, endIndex);
                if (!TransitionAttributionChecker.IsCarryOver(isStarted, isCompleted)) continue;

                if (!zombieTicketKeys.TryGetValue(m.TicketId, out var sprintCount)) continue;
                if (!seenZombieKeys.Add(m.TicketId)) continue;

                zombieTickets.Add(new CarryOverZombieSummary(
                    m.TicketId,
                    m.Ticket?.Summary ?? m.TicketId,
                    m.Ticket?.IssueType ?? "Unknown",
                    m.FinalStatus,
                    m.GetEffectiveSp(defaultSpPerBug),
                    sprintCount));
            }
        }

        var summaryMetrics = new CarryOverSummaryMetrics(
            avgCarryOverRate,
            avgCarryOverSp,
            seenZombieKeys.Count);

        return new CarryOverMultiSprintResponse(
            sprintInfos,
            summaryMetrics,
            perSprintData,
            issueTypeBreakdown,
            zombieTickets);
    }

    public CarryOverSingleSprintResponse ComputeSingleSprint(
        Sprint targetSprint,
        Sprint? priorSprint,
        List<Sprint> allSyncedSprints,
        List<StatusTransition> statusTransitions,
        AppSettings settings,
        string? subTeam,
        HashSet<string>? excludedDeveloperIds = null)
    {
        var excludedStatuses = settings.ExcludedFromScopeStatuses;
        var workflowStages = settings.WorkflowStages;
        var defaultSpPerBug = settings.DefaultSpPerBug;

        // Resolve transition boundaries once
        var (orderedStages, startIndex) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, orderedStages);

        var memberships = FilterMemberships(targetSprint.Memberships, subTeam, excludedDeveloperIds);
        var priorMemberships = priorSprint is not null
            ? FilterMemberships(priorSprint.Memberships, subTeam, excludedDeveloperIds)
            : null;

        var sprintInfo = new CarryOverSprintInfo(
            targetSprint.Id, targetSprint.Name, targetSprint.StartDate, targetSprint.EndDate);

        var zombieTicketKeys = GetZombieTicketKeys(allSyncedSprints);

        var transitionsByTicket = statusTransitions
            .GroupBy(t => t.TicketId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        // Identify carry-over memberships for this sprint (feature-only: spec BR12)
        var carryOverMemberships = memberships
            .Where(m =>
            {
                if (m.RemovedAt != null || m.Ticket?.IssueType == "Bug") return false;
                var ticketTransitions = transitionsByTicket.GetValueOrDefault(m.TicketId, []);
                var (isStarted, _) = TransitionAttributionChecker.IsStartedInSprint(
                    m.TicketId, ticketTransitions, targetSprint.StartDate, targetSprint.EndDate, orderedStages, startIndex);
                var (isCompleted, _) = TransitionAttributionChecker.IsCompletedInSprint(
                    m.TicketId, ticketTransitions, targetSprint.StartDate, targetSprint.EndDate, orderedStages, endIndex);
                return TransitionAttributionChecker.IsCarryOver(isStarted, isCompleted);
            })
            .ToList();

        // Core carry-over metrics
        var current = ComputeCarryOverMetrics(
            memberships, statusTransitions, targetSprint,
            orderedStages, startIndex, endIndex, excludedStatuses, defaultSpPerBug);
        CarryOverMetrics? prior = priorMemberships is not null && priorSprint is not null
            ? ComputeCarryOverMetrics(
                priorMemberships, statusTransitions, priorSprint,
                orderedStages, startIndex, endIndex, excludedStatuses, defaultSpPerBug)
            : null;

        var metrics = new CarryOverSingleSprintMetrics(
            CarryOverRate: BuildMetricCard("Carry-Over Rate", current.CarryOverRate,
                $"{current.CarryOverRate:0.#}%",
                prior is not null ? current.CarryOverRate - prior.CarryOverRate : null,
                "positive-down"),
            CarryOverSp: BuildMetricCard("Carry-Over SP", current.CarryOverSp,
                $"{current.CarryOverSp:0.#}",
                prior is not null ? current.CarryOverSp - prior.CarryOverSp : null,
                "positive-down"),
            CarryOverTicketCount: BuildMetricCard("Carry-Over Tickets", current.CarryOverTicketCount,
                $"{current.CarryOverTicketCount}",
                prior is not null ? (decimal)(current.CarryOverTicketCount - prior.CarryOverTicketCount) : null,
                "positive-down"));

        var statusDistribution = BuildStatusDistribution(carryOverMemberships, workflowStages, excludedStatuses, defaultSpPerBug);
        var issueTypeBreakdown = BuildIssueTypeBreakdown(carryOverMemberships, excludedStatuses, defaultSpPerBug);

        // Carry-over destination (transition-based — spec BR22)
        var destination = BuildCarryOverDestination(
            priorSprint, priorMemberships, targetSprint, memberships,
            statusTransitions, orderedStages, startIndex, endIndex,
            excludedStatuses, defaultSpPerBug);

        // Full carry-over ticket table
        var tickets = carryOverMemberships
            .Select(m =>
            {
                var sprintCount = zombieTicketKeys.TryGetValue(m.TicketId, out var sc) ? sc : 1;
                return new CarryOverTicketEntry(
                    m.TicketId,
                    m.Ticket?.Summary ?? m.TicketId,
                    m.Ticket?.IssueType ?? "Unknown",
                    m.GetEffectiveSp(defaultSpPerBug),
                    m.FinalStatus,
                    MapWorkflowStage(m.FinalStatus, workflowStages),
                    sprintCount,
                    sprintCount >= 3,
                    IsExcluded(m.FinalStatus, excludedStatuses));
            })
            .ToList();

        var zombieTrajectories = BuildZombieTrajectories(tickets, allSyncedSprints, zombieTicketKeys);

        return new CarryOverSingleSprintResponse(
            sprintInfo,
            metrics,
            statusDistribution,
            issueTypeBreakdown,
            destination,
            tickets,
            zombieTrajectories);
    }

    // --- Sub-team filtering (C2) ---

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

    // --- Excluded status check (case-insensitive) ---

    private static bool IsExcluded(string status, List<string> excludedStatuses) =>
        excludedStatuses.Contains(status, StringComparer.OrdinalIgnoreCase);

    // --- Carry-over metrics per sprint (feature-only) ---

    private record CarryOverMetrics(
        decimal CarryOverSp,
        int CarryOverTicketCount,
        decimal TotalScopeSp,
        decimal CarryOverRate);

    private static CarryOverMetrics ComputeCarryOverMetrics(
        List<SprintMembership> memberships,
        List<StatusTransition> statusTransitions,
        Sprint sprint,
        List<string> orderedStages,
        int startIndex,
        int endIndex,
        List<string> excludedStatuses,
        int defaultSpPerBug)
    {
        var activeSp = 0m;
        var carryOverSp = 0m;
        var carryOverCount = 0;

        var transitionsByTicket = statusTransitions
            .GroupBy(t => t.TicketId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        foreach (var m in memberships)
        {
            if (m.RemovedAt != null || m.Ticket?.IssueType == "Bug") continue;
            var sp = m.GetEffectiveSp(defaultSpPerBug);

            var ticketTransitions = transitionsByTicket.GetValueOrDefault(m.TicketId, []);
            var (isStarted, _) = TransitionAttributionChecker.IsStartedInSprint(
                m.TicketId, ticketTransitions, sprint.StartDate, sprint.EndDate, orderedStages, startIndex);
            var (isCompleted, _) = TransitionAttributionChecker.IsCompletedInSprint(
                m.TicketId, ticketTransitions, sprint.StartDate, sprint.EndDate, orderedStages, endIndex);

            if (isStarted)
            {
                if (sp.HasValue && !IsExcluded(m.FinalStatus, excludedStatuses))
                    activeSp += sp.Value;
            }

            if (TransitionAttributionChecker.IsCarryOver(isStarted, isCompleted))
            {
                carryOverCount++;
                if (sp.HasValue && !IsExcluded(m.FinalStatus, excludedStatuses))
                    carryOverSp += sp.Value;
            }
        }

        // totalScopeSp = activeSp (feature tickets that transitioned to startStage — spec BR21)
        var carryOverRate = activeSp > 0 ? carryOverSp / activeSp * 100 : 0;

        return new CarryOverMetrics(
            Math.Round(carryOverSp, 1),
            carryOverCount,
            Math.Round(activeSp, 1),
            Math.Round(carryOverRate, 1));
    }

    // --- Per-sprint data ---

    private static CarryOverPerSprintData ComputePerSprintData(
        Sprint sprint,
        List<SprintMembership> memberships,
        List<StatusTransition> statusTransitions,
        List<string> orderedStages,
        int startIndex,
        int endIndex,
        List<string> excludedStatuses,
        List<string> workflowStages,
        int defaultSpPerBug)
    {
        var metrics = ComputeCarryOverMetrics(
            memberships, statusTransitions, sprint,
            orderedStages, startIndex, endIndex, excludedStatuses, defaultSpPerBug);

        var transitionsByTicket = statusTransitions
            .GroupBy(t => t.TicketId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var carryOverMemberships = memberships
            .Where(m =>
            {
                if (m.RemovedAt != null || m.Ticket?.IssueType == "Bug") return false;
                var ticketTransitions = transitionsByTicket.GetValueOrDefault(m.TicketId, []);
                var (isStarted, _) = TransitionAttributionChecker.IsStartedInSprint(
                    m.TicketId, ticketTransitions, sprint.StartDate, sprint.EndDate, orderedStages, startIndex);
                var (isCompleted, _) = TransitionAttributionChecker.IsCompletedInSprint(
                    m.TicketId, ticketTransitions, sprint.StartDate, sprint.EndDate, orderedStages, endIndex);
                return TransitionAttributionChecker.IsCarryOver(isStarted, isCompleted);
            })
            .ToList();

        var statusDistribution = BuildStatusDistribution(carryOverMemberships, workflowStages, excludedStatuses, defaultSpPerBug);

        return new CarryOverPerSprintData(
            sprint.Id,
            metrics.CarryOverSp,
            metrics.CarryOverTicketCount,
            metrics.CarryOverRate,
            metrics.TotalScopeSp,
            statusDistribution);
    }

    // --- Status distribution ---

    private static List<CarryOverStatusDistributionEntry> BuildStatusDistribution(
        List<SprintMembership> carryOverMemberships,
        List<string> workflowStages,
        List<string> excludedStatuses,
        int defaultSpPerBug)
    {
        var visibleMemberships = carryOverMemberships
            .Where(m => !IsExcluded(m.FinalStatus, excludedStatuses))
            .ToList();

        var total = visibleMemberships.Count;
        var result = new List<CarryOverStatusDistributionEntry>();

        foreach (var stage in workflowStages)
        {
            var items = visibleMemberships
                .Where(m => string.Equals(m.FinalStatus, stage, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (items.Count == 0) continue;

            var spTotal = items
                .Where(m => m.GetEffectiveSp(defaultSpPerBug).HasValue)
                .Sum(m => m.GetEffectiveSp(defaultSpPerBug)!.Value);
            var percentage = total > 0 ? Math.Round((decimal)items.Count / total * 100, 1) : 0;

            result.Add(new CarryOverStatusDistributionEntry(
                stage, items.Count, Math.Round(spTotal, 1), percentage));
        }

        var stageSet = new HashSet<string>(workflowStages, StringComparer.OrdinalIgnoreCase);
        var otherItems = visibleMemberships
            .Where(m => !stageSet.Contains(m.FinalStatus))
            .ToList();

        if (otherItems.Count > 0)
        {
            var otherSp = otherItems
                .Where(m => m.GetEffectiveSp(defaultSpPerBug).HasValue)
                .Sum(m => m.GetEffectiveSp(defaultSpPerBug)!.Value);
            var otherPct = total > 0 ? Math.Round((decimal)otherItems.Count / total * 100, 1) : 0;
            result.Add(new CarryOverStatusDistributionEntry(
                "Other", otherItems.Count, Math.Round(otherSp, 1), otherPct));
        }

        return result;
    }

    // --- Issue type breakdown ---

    private static List<CarryOverIssueTypeEntry> BuildIssueTypeBreakdown(
        List<SprintMembership> carryOverMemberships,
        List<string> excludedStatuses,
        int defaultSpPerBug)
    {
        var includedMemberships = carryOverMemberships
            .Where(m => !IsExcluded(m.FinalStatus, excludedStatuses))
            .ToList();

        var total = includedMemberships.Count;

        return includedMemberships
            .GroupBy(m => m.Ticket?.IssueType ?? "Unknown")
            .Select(g =>
            {
                var spTotal = g
                    .Where(m => m.GetEffectiveSp(defaultSpPerBug).HasValue)
                    .Sum(m => m.GetEffectiveSp(defaultSpPerBug)!.Value);
                var percentage = total > 0 ? Math.Round((decimal)g.Count() / total * 100, 1) : 0;
                return new CarryOverIssueTypeEntry(
                    g.Key, g.Count(), Math.Round(spTotal, 1), percentage);
            })
            .OrderByDescending(e => e.TicketCount)
            .ToList();
    }

    // --- Zombie detection ---

    private static Dictionary<string, int> GetZombieTicketKeys(List<Sprint> allSyncedSprints)
    {
        return allSyncedSprints
            .SelectMany(s => s.Memberships.Select(m => new { m.TicketId, s.Id }))
            .GroupBy(x => x.TicketId, StringComparer.OrdinalIgnoreCase)
            .Select(g => new { TicketId = g.Key, SprintCount = g.Select(x => x.Id).Distinct().Count() })
            .Where(x => x.SprintCount >= 3)
            .ToDictionary(x => x.TicketId, x => x.SprintCount, StringComparer.OrdinalIgnoreCase);
    }

    // --- Workflow stage mapping ---

    private static string MapWorkflowStage(string finalStatus, List<string> workflowStages)
    {
        var match = workflowStages.FirstOrDefault(
            stage => string.Equals(stage, finalStatus, StringComparison.OrdinalIgnoreCase));
        return match ?? "Other";
    }

    // --- Carry-over destination (transition-based — spec BR22) ---

    private static CarryOverDestination? BuildCarryOverDestination(
        Sprint? priorSprint,
        List<SprintMembership>? priorMemberships,
        Sprint targetSprint,
        List<SprintMembership> targetMemberships,
        List<StatusTransition> statusTransitions,
        List<string> orderedStages,
        int startIndex,
        int endIndex,
        List<string> excludedStatuses,
        int defaultSpPerBug)
    {
        if (priorSprint is null || priorMemberships is null)
            return null;

        var transitionsByTicket = statusTransitions
            .GroupBy(t => t.TicketId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        // Prior sprint carry-over: feature tickets that started but did not complete in prior sprint
        var priorCarryOverMemberships = priorMemberships
            .Where(m =>
            {
                if (m.RemovedAt != null || m.Ticket?.IssueType == "Bug") return false;
                var ticketTransitions = transitionsByTicket.GetValueOrDefault(m.TicketId, []);
                var (isStarted, _) = TransitionAttributionChecker.IsStartedInSprint(
                    m.TicketId, ticketTransitions, priorSprint.StartDate, priorSprint.EndDate, orderedStages, startIndex);
                var (isCompleted, _) = TransitionAttributionChecker.IsCompletedInSprint(
                    m.TicketId, ticketTransitions, priorSprint.StartDate, priorSprint.EndDate, orderedStages, endIndex);
                return TransitionAttributionChecker.IsCarryOver(isStarted, isCompleted);
            })
            .ToList();

        var priorCarryOverCount = priorCarryOverMemberships.Count;
        var priorCarryOverSp = priorCarryOverMemberships
            .Where(m => m.GetEffectiveSp(defaultSpPerBug).HasValue && !IsExcluded(m.FinalStatus, excludedStatuses))
            .Sum(m => m.GetEffectiveSp(defaultSpPerBug)!.Value);

        if (priorCarryOverCount == 0)
        {
            return new CarryOverDestination(
                priorSprint.Id,
                priorSprint.Name,
                0,
                Math.Round(priorCarryOverSp, 1),
                new CarryOverDestinationBucket(0, 0),
                new CarryOverDestinationBucket(0, 0),
                new CarryOverDestinationBucket(0, 0),
                new CarryOverDestinationBucket(0, 0));
        }

        var currentMembershipByTicket = targetMemberships
            .ToDictionary(m => m.TicketId, StringComparer.OrdinalIgnoreCase);

        var completedCount = 0; var completedSp = 0m;
        var carriedAgainCount = 0; var carriedAgainSp = 0m;
        var removedCount = 0; var removedSp = 0m;
        var droppedCount = 0; var droppedSp = 0m;

        foreach (var pm in priorCarryOverMemberships)
        {
            var sp = pm.GetEffectiveSp(defaultSpPerBug) ?? 0m;
            if (!currentMembershipByTicket.TryGetValue(pm.TicketId, out var cm))
            {
                droppedCount++;
                droppedSp += sp;
            }
            else if (cm.RemovedAt != null)
            {
                removedCount++;
                removedSp += sp;
            }
            else
            {
                var pmTransitions = transitionsByTicket.GetValueOrDefault(pm.TicketId, []);
                // Transition-based: completed in current sprint = has a transition to endStage during current sprint
                var (isCompletedInCurrent, _) = TransitionAttributionChecker.IsCompletedInSprint(
                    pm.TicketId, pmTransitions,
                    targetSprint.StartDate, targetSprint.EndDate,
                    orderedStages, endIndex);

                if (isCompletedInCurrent)
                {
                    completedCount++;
                    completedSp += sp;
                }
                else
                {
                    // Carried again: started in current sprint but not completed
                    var (isStartedInCurrent, _) = TransitionAttributionChecker.IsStartedInSprint(
                        pm.TicketId, pmTransitions,
                        targetSprint.StartDate, targetSprint.EndDate,
                        orderedStages, startIndex);

                    if (isStartedInCurrent)
                    {
                        carriedAgainCount++;
                        carriedAgainSp += sp;
                    }
                    else
                    {
                        // In current sprint but no qualifying transition — dropped into backlog
                        droppedCount++;
                        droppedSp += sp;
                    }
                }
            }
        }

        return new CarryOverDestination(
            priorSprint.Id,
            priorSprint.Name,
            priorCarryOverCount,
            Math.Round(priorCarryOverSp, 1),
            new CarryOverDestinationBucket(completedCount, Math.Round(completedSp, 1)),
            new CarryOverDestinationBucket(carriedAgainCount, Math.Round(carriedAgainSp, 1)),
            new CarryOverDestinationBucket(removedCount, Math.Round(removedSp, 1)),
            new CarryOverDestinationBucket(droppedCount, Math.Round(droppedSp, 1)));
    }

    // --- Zombie trajectories ---

    private static List<ZombieTrajectoryEntry> BuildZombieTrajectories(
        List<CarryOverTicketEntry> tickets,
        List<Sprint> allSyncedSprints,
        Dictionary<string, int> zombieTicketKeys)
    {
        var zombies = tickets.Where(t => t.IsZombie).ToList();
        if (zombies.Count == 0) return new List<ZombieTrajectoryEntry>();

        var result = new List<ZombieTrajectoryEntry>();

        foreach (var zombie in zombies)
        {
            var sprintAppearances = allSyncedSprints
                .Where(s => s.Memberships.Any(m =>
                    string.Equals(m.TicketId, zombie.TicketKey, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(s => s.StartDate)
                .ToList();

            if (sprintAppearances.Count > 10)
                sprintAppearances = sprintAppearances.TakeLast(10).ToList();

            var sprintEntries = sprintAppearances
                .Select(s =>
                {
                    var m = s.Memberships.First(m =>
                        string.Equals(m.TicketId, zombie.TicketKey, StringComparison.OrdinalIgnoreCase));
                    return new ZombieTrajectorySprintEntry(s.Id, s.Name, m.FinalStatus);
                })
                .ToList();

            result.Add(new ZombieTrajectoryEntry(
                zombie.TicketKey,
                zombie.Summary,
                zombie.IssueType,
                zombie.StoryPoints,
                zombie.FinalStatus,
                zombie.SprintCount,
                sprintEntries));
        }

        return result.OrderByDescending(e => e.SprintCount).ToList();
    }

    // --- Metric card builder ---

    private static ScopeMetricCard BuildMetricCard(
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

        return new ScopeMetricCard(
            name,
            Math.Round(value, 1),
            displayValue,
            delta.HasValue ? Math.Round(delta.Value, 1) : null,
            direction,
            deltaPolarity);
    }
}
