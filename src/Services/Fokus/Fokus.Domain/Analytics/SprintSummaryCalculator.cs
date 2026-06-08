namespace Fokus.Domain.Analytics;

public class SprintSummaryCalculator
{
    public SprintSummaryData ComputeSummary(
        Sprint selectedSprint,
        List<Sprint> windowSprints,
        List<Developer> activeDevelopers,
        AppSettings settings,
        string? subTeam,
        List<Ticket> allEpicTickets,
        Dictionary<string, Dictionary<int, int>> capacityLookup,
        List<Developer> allDevelopers,
        List<StatusTransition> statusTransitions,
        HashSet<string>? excludedDeveloperIds = null,
        decimal? qualitySubScore = null,
        QaBreakdownData? qualityBreakdown = null,
        bool hasQaData = false,
        TestingCrunchFlag? testingCrunch = null)
    {
        // C2: sub-team filtering + cross-cutting exclusion
        var selectedMemberships = FilterMemberships(selectedSprint.Memberships, subTeam, excludedDeveloperIds);
        var filteredDevelopers = FilterDevelopers(activeDevelopers, subTeam);

        var completedStatuses = CompletionChecker.ResolveCompletedStatuses(settings);
        var thresholds = settings.HealthThresholds;
        var weights = settings.HealthWeights;
        var defaultSpPerBug = settings.BugRatio.DefaultSpPerBug;

        // Resolve transition-based boundaries once for this request
        var (orderedStages, startIndex) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, orderedStages);

        // Identify the prior sprint from sorted window (ascending by StartDate)
        var sortedWindow = windowSprints
            .OrderBy(s => s.StartDate)
            .ToList();

        var selectedIndex = sortedWindow.FindIndex(s => s.Id == selectedSprint.Id);
        var priorSprint = selectedIndex > 0 ? sortedWindow[selectedIndex - 1] : null;
        var priorMemberships = priorSprint is not null
            ? FilterMemberships(priorSprint.Memberships, subTeam, excludedDeveloperIds)
            : null;

        // Core metrics for selected sprint
        var selected = ComputeMetrics(
            selectedMemberships, statusTransitions,
            selectedSprint.StartDate, selectedSprint.EndDate,
            orderedStages, startIndex, endIndex,
            settings.ExcludedFromScopeStatuses, defaultSpPerBug, settings.Sync.PlanningWindowDays);

        // Delta metrics (C1 pattern)
        SprintMetrics? prior = priorMemberships is not null && priorSprint is not null
            ? ComputeMetrics(
                priorMemberships, statusTransitions,
                priorSprint.StartDate, priorSprint.EndDate,
                orderedStages, startIndex, endIndex,
                settings.ExcludedFromScopeStatuses, defaultSpPerBug, settings.Sync.PlanningWindowDays)
            : null;

        // Health score — uses combined DisruptionRate (scope + bug) for continuity
        var healthScore = ComputeHealthScore(selected, thresholds, weights,
            qualitySubScore, qualityBreakdown, settings.QualityHealthWeight, hasQaData);

        // Sparklines — window of up to 4 sprints ending at selected
        var sparklineWindow = sortedWindow
            .Take(selectedIndex + 1)
            .TakeLast(4)
            .ToList();

        var completionSparkline = BuildSparkline(sparklineWindow, windowSprints, subTeam, statusTransitions, settings,
            (m, t, s, cfg) => ComputeCompletionRate(m, t, s, cfg), excludedDeveloperIds);
        var spCompletedSparkline = BuildSparkline(sparklineWindow, windowSprints, subTeam, statusTransitions, settings,
            (m, t, s, cfg) => ComputeSpCompleted(m, t, s, cfg), excludedDeveloperIds);
        var scopeDisruptionSparkline = BuildSparkline(sparklineWindow, windowSprints, subTeam, statusTransitions, settings,
            (m, t, s, cfg) => ComputeScopeDisruptionRate(m, t, s, cfg), excludedDeveloperIds);
        var bugDisruptionSparkline = BuildSparkline(sparklineWindow, windowSprints, subTeam, statusTransitions, settings,
            (m, t, s, cfg) => ComputeBugDisruptionRate(m, t, s, cfg), excludedDeveloperIds);
        var carryOverSparkline = BuildSparkline(sparklineWindow, windowSprints, subTeam, statusTransitions, settings,
            (m, t, s, cfg) => ComputeCarryOverRate(m, t, s, cfg), excludedDeveloperIds);

        // Build metric cards
        var metrics = new SprintMetricsData(
            SpCompleted: BuildMetricCard(
                "SP Completed",
                selected.FeatureCompleted,
                $"{selected.FeatureCompleted:0.#}",
                prior is not null ? selected.FeatureCompleted - prior.FeatureCompleted : null,
                "neutral",
                spCompletedSparkline),
            CompletionRate: BuildMetricCard(
                "Completion %",
                selected.CompletionRate,
                $"{selected.CompletionRate:0.#}%",
                prior is not null ? selected.CompletionRate - prior.CompletionRate : null,
                "positive-up",
                completionSparkline),
            ScopeDisruptionRate: BuildMetricCard(
                "Scope Disruption Rate",
                selected.ScopeDisruptionRate,
                $"{selected.ScopeDisruptionRate:0.#}%",
                prior is not null ? selected.ScopeDisruptionRate - prior.ScopeDisruptionRate : null,
                "positive-down",
                scopeDisruptionSparkline),
            BugDisruptionRate: BuildMetricCard(
                "Bug Disruption Rate",
                selected.BugDisruptionRate,
                $"{selected.BugDisruptionRate:0.#}%",
                prior is not null ? selected.BugDisruptionRate - prior.BugDisruptionRate : null,
                "positive-down",
                bugDisruptionSparkline),
            CarryOverRate: BuildMetricCard(
                "Carry-Over Rate",
                selected.CarryOverRate,
                $"{selected.CarryOverRate:0.#}%",
                prior is not null ? selected.CarryOverRate - prior.CarryOverRate : null,
                "positive-down",
                carryOverSparkline),
            BugSpCompleted: selected.BugSpCompleted);

        // Top epics
        var topEpics = ComputeTopEpics(
            selectedMemberships, statusTransitions,
            selectedSprint.StartDate, selectedSprint.EndDate,
            windowSprints, subTeam, completedStatuses, allEpicTickets, defaultSpPerBug,
            orderedStages, endIndex);

        // Leaderboard
        var leaderboard = ComputeLeaderboard(
            filteredDevelopers, selectedMemberships, statusTransitions,
            selectedSprint.StartDate, selectedSprint.EndDate,
            orderedStages, endIndex,
            settings.ExcludedFromScopeStatuses, defaultSpPerBug, capacityLookup, allDevelopers, selectedSprint.Id);

        // Flags
        var flags = ComputeFlags(
            selectedSprint, selectedMemberships, statusTransitions,
            windowSprints, subTeam, filteredDevelopers,
            orderedStages, endIndex,
            settings.ExcludedFromScopeStatuses, defaultSpPerBug, settings.Sync.PlanningWindowDays, excludedDeveloperIds,
            testingCrunch);

        var sprintInfo = new SprintInfoData(
            selectedSprint.Id,
            selectedSprint.Name,
            selectedSprint.StartDate,
            selectedSprint.EndDate,
            (int)(selectedSprint.EndDate - selectedSprint.StartDate).TotalDays,
            selectedSprint.SyncedAt);

        return new SprintSummaryData(sprintInfo, healthScore, metrics, topEpics, leaderboard, flags);
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

    private static List<Developer> FilterDevelopers(List<Developer> developers, string? subTeam)
    {
        if (string.IsNullOrWhiteSpace(subTeam))
            return developers;

        return developers.Where(d => d.SubTeam == subTeam).ToList();
    }

    // --- Core metrics computation (transition-based, feature-only per spec BR9, BR21) ---

    private record SprintMetrics(
        decimal ActiveSp,
        decimal CompletedSp,
        decimal AddedSp,
        decimal CarryOverSp,
        decimal CompletionRate,
        decimal DisruptionRate,
        decimal ScopeDisruptionRate,
        decimal BugDisruptionRate,
        decimal CarryOverRate,
        decimal FeatureCompleted,
        decimal BugSpCompleted);

    private static bool IsBug(SprintMembership m) =>
        m.Ticket?.IssueType == "Bug";

    private static bool IsExcluded(SprintMembership m, List<string> excludedStatuses) =>
        excludedStatuses.Contains(m.FinalStatus, StringComparer.OrdinalIgnoreCase);

    private static SprintMetrics ComputeMetrics(
        List<SprintMembership> memberships,
        List<StatusTransition> allTransitions,
        DateTime sprintStart,
        DateTime sprintEnd,
        List<string> orderedStages,
        int startIndex,
        int endIndex,
        List<string> excludedStatuses,
        int defaultSpPerBug,
        int planningWindowDays)
    {
        var planningCutoff = sprintStart.AddDays(planningWindowDays);

        var transitionsByTicket = allTransitions
            .GroupBy(t => t.TicketId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var activeSp = 0m;
        var completedSp = 0m;
        var addedSp = 0m;
        var carryOverSp = 0m;
        var bugAddedSp = 0m;
        var bugSpCompleted = 0m;

        foreach (var m in memberships)
        {
            if (m.RemovedAt != null) continue;
            var sp = m.GetEffectiveSp(defaultSpPerBug);
            if (!sp.HasValue) continue;
            if (IsExcluded(m, excludedStatuses)) continue;

            var ticketTransitions = transitionsByTicket.GetValueOrDefault(m.TicketId, []);
            var (isStarted, _) = TransitionAttributionChecker.IsStartedInSprint(
                m.TicketId, ticketTransitions, sprintStart, sprintEnd, orderedStages, startIndex);
            var (isCompleted, _) = TransitionAttributionChecker.IsCompletedInSprint(
                m.TicketId, ticketTransitions, sprintStart, sprintEnd, orderedStages, endIndex);
            var isAdded = TransitionAttributionChecker.IsAddedInSprint(m, isStarted, planningCutoff);

            if (IsBug(m))
            {
                if (isAdded) bugAddedSp += sp.Value;
                if (isCompleted) bugSpCompleted += sp.Value;
            }
            else
            {
                if (isStarted) activeSp += sp.Value;
                if (isCompleted) completedSp += sp.Value;
                if (isAdded) addedSp += sp.Value;
                if (TransitionAttributionChecker.IsCarryOver(isStarted, isCompleted)) carryOverSp += sp.Value;
            }
        }

        var completionRate = activeSp > 0 ? completedSp / activeSp * 100 : 0;
        var scopeDisruptionRate = activeSp > 0 ? addedSp / activeSp * 100 : 0;
        var bugDisruptionRate = activeSp > 0 ? bugAddedSp / activeSp * 100 : 0;
        var disruptionRate = scopeDisruptionRate + bugDisruptionRate;
        var carryOverRate = activeSp > 0 ? carryOverSp / activeSp * 100 : 0;

        return new SprintMetrics(activeSp, completedSp, addedSp, carryOverSp, completionRate, disruptionRate, scopeDisruptionRate, bugDisruptionRate, carryOverRate, completedSp, bugSpCompleted);
    }

    // --- Sparkline helpers ---

    private static decimal ComputeSpCompleted(
        List<SprintMembership> memberships,
        List<StatusTransition> transitions,
        Sprint sprint,
        AppSettings settings)
    {
        var (orderedStages, _) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, orderedStages);
        var excludedStatuses = settings.ExcludedFromScopeStatuses;
        var defaultSpPerBug = settings.BugRatio.DefaultSpPerBug;

        var transitionsByTicket = transitions
            .GroupBy(t => t.TicketId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        return memberships
            .Where(m => m.RemovedAt == null && !IsBug(m) && !IsExcluded(m, excludedStatuses) && m.GetEffectiveSp(defaultSpPerBug).HasValue)
            .Sum(m =>
            {
                var ticketTransitions = transitionsByTicket.GetValueOrDefault(m.TicketId, []);
                var (isCompleted, _) = TransitionAttributionChecker.IsCompletedInSprint(
                    m.TicketId, ticketTransitions, sprint.StartDate, sprint.EndDate, orderedStages, endIndex);
                return isCompleted ? m.GetEffectiveSp(defaultSpPerBug)!.Value : 0m;
            });
    }

    private static decimal ComputeCompletionRate(
        List<SprintMembership> memberships,
        List<StatusTransition> transitions,
        Sprint sprint,
        AppSettings settings)
    {
        var (orderedStages, startIndex) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, orderedStages);
        var excludedStatuses = settings.ExcludedFromScopeStatuses;
        var defaultSpPerBug = settings.BugRatio.DefaultSpPerBug;

        var transitionsByTicket = transitions
            .GroupBy(t => t.TicketId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var activeSp = 0m;
        var completedSp = 0m;
        foreach (var m in memberships)
        {
            if (m.RemovedAt != null || IsBug(m) || IsExcluded(m, excludedStatuses)) continue;
            var sp = m.GetEffectiveSp(defaultSpPerBug);
            if (!sp.HasValue) continue;

            var ticketTransitions = transitionsByTicket.GetValueOrDefault(m.TicketId, []);
            var (isStarted, _) = TransitionAttributionChecker.IsStartedInSprint(
                m.TicketId, ticketTransitions, sprint.StartDate, sprint.EndDate, orderedStages, startIndex);
            var (isCompleted, _) = TransitionAttributionChecker.IsCompletedInSprint(
                m.TicketId, ticketTransitions, sprint.StartDate, sprint.EndDate, orderedStages, endIndex);

            if (isStarted) activeSp += sp.Value;
            if (isCompleted) completedSp += sp.Value;
        }

        return activeSp > 0 ? completedSp / activeSp * 100 : 0;
    }

    private static decimal ComputeScopeDisruptionRate(
        List<SprintMembership> memberships,
        List<StatusTransition> transitions,
        Sprint sprint,
        AppSettings settings)
    {
        var (orderedStages, startIndex) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var excludedStatuses = settings.ExcludedFromScopeStatuses;
        var defaultSpPerBug = settings.BugRatio.DefaultSpPerBug;
        var planningCutoff = sprint.StartDate.AddDays(settings.Sync.PlanningWindowDays);

        var transitionsByTicket = transitions
            .GroupBy(t => t.TicketId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var activeSp = 0m;
        var addedSp = 0m;
        foreach (var m in memberships)
        {
            if (m.RemovedAt != null || IsBug(m) || IsExcluded(m, excludedStatuses)) continue;
            var sp = m.GetEffectiveSp(defaultSpPerBug);
            if (!sp.HasValue) continue;

            var ticketTransitions = transitionsByTicket.GetValueOrDefault(m.TicketId, []);
            var (isStarted, _) = TransitionAttributionChecker.IsStartedInSprint(
                m.TicketId, ticketTransitions, sprint.StartDate, sprint.EndDate, orderedStages, startIndex);
            var isAdded = TransitionAttributionChecker.IsAddedInSprint(m, isStarted, planningCutoff);

            if (isStarted) activeSp += sp.Value;
            if (isAdded) addedSp += sp.Value;
        }

        return activeSp > 0 ? addedSp / activeSp * 100 : 0;
    }

    private static decimal ComputeBugDisruptionRate(
        List<SprintMembership> memberships,
        List<StatusTransition> transitions,
        Sprint sprint,
        AppSettings settings)
    {
        var (orderedStages, startIndex) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var excludedStatuses = settings.ExcludedFromScopeStatuses;
        var defaultSpPerBug = settings.BugRatio.DefaultSpPerBug;
        var planningCutoff = sprint.StartDate.AddDays(settings.Sync.PlanningWindowDays);

        var transitionsByTicket = transitions
            .GroupBy(t => t.TicketId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var activeSp = 0m;
        foreach (var m in memberships)
        {
            if (m.RemovedAt != null || IsBug(m) || IsExcluded(m, excludedStatuses)) continue;
            var sp = m.GetEffectiveSp(defaultSpPerBug);
            if (!sp.HasValue) continue;

            var ticketTransitions = transitionsByTicket.GetValueOrDefault(m.TicketId, []);
            var (isStarted, _) = TransitionAttributionChecker.IsStartedInSprint(
                m.TicketId, ticketTransitions, sprint.StartDate, sprint.EndDate, orderedStages, startIndex);
            if (isStarted) activeSp += sp.Value;
        }

        var bugAddedSp = 0m;
        foreach (var m in memberships)
        {
            if (m.RemovedAt != null || !IsBug(m) || IsExcluded(m, excludedStatuses)) continue;
            var sp = m.GetEffectiveSp(defaultSpPerBug);
            if (!sp.HasValue) continue;

            var ticketTransitions = transitionsByTicket.GetValueOrDefault(m.TicketId, []);
            var (isStarted, _) = TransitionAttributionChecker.IsStartedInSprint(
                m.TicketId, ticketTransitions, sprint.StartDate, sprint.EndDate, orderedStages, startIndex);
            var isAdded = TransitionAttributionChecker.IsAddedInSprint(m, isStarted, planningCutoff);
            if (isAdded) bugAddedSp += sp.Value;
        }

        return activeSp > 0 ? bugAddedSp / activeSp * 100 : 0;
    }

    private static decimal ComputeCarryOverRate(
        List<SprintMembership> memberships,
        List<StatusTransition> transitions,
        Sprint sprint,
        AppSettings settings)
    {
        var (orderedStages, startIndex) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, orderedStages);
        var excludedStatuses = settings.ExcludedFromScopeStatuses;
        var defaultSpPerBug = settings.BugRatio.DefaultSpPerBug;

        var transitionsByTicket = transitions
            .GroupBy(t => t.TicketId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var activeSp = 0m;
        var carryOverSp = 0m;
        foreach (var m in memberships)
        {
            if (m.RemovedAt != null || IsBug(m) || IsExcluded(m, excludedStatuses)) continue;
            var sp = m.GetEffectiveSp(defaultSpPerBug);
            if (!sp.HasValue) continue;

            var ticketTransitions = transitionsByTicket.GetValueOrDefault(m.TicketId, []);
            var (isStarted, _) = TransitionAttributionChecker.IsStartedInSprint(
                m.TicketId, ticketTransitions, sprint.StartDate, sprint.EndDate, orderedStages, startIndex);
            var (isCompleted, _) = TransitionAttributionChecker.IsCompletedInSprint(
                m.TicketId, ticketTransitions, sprint.StartDate, sprint.EndDate, orderedStages, endIndex);

            if (isStarted) activeSp += sp.Value;
            if (TransitionAttributionChecker.IsCarryOver(isStarted, isCompleted)) carryOverSp += sp.Value;
        }

        return activeSp > 0 ? carryOverSp / activeSp * 100 : 0;
    }

    // --- Health score computation (BR2, BR3, BR4, BR16-BR19) ---

    private static HealthScoreData ComputeHealthScore(
        SprintMetrics metrics,
        HealthThresholdConfig thresholds,
        HealthWeightConfig weights,
        decimal? qualitySubScore = null,
        QaBreakdownData? qualityBreakdown = null,
        int qualityWeight = 0,
        bool hasQaData = false)
    {
        var completionScore = HealthScoreCalculator.ScoreHigherIsBetter(metrics.CompletionRate, thresholds.CompletionGreen, thresholds.CompletionAmber);
        var disruptionScore = HealthScoreCalculator.ScoreLowerIsBetter(metrics.DisruptionRate, thresholds.DisruptionGreen, thresholds.DisruptionAmber);
        var carryOverScore = HealthScoreCalculator.ScoreLowerIsBetter(metrics.CarryOverRate, thresholds.CarryOverGreen, thresholds.CarryOverAmber);

        var deliverySum = weights.Completion + weights.Disruption + weights.CarryOver;

        decimal totalWeight;
        decimal compositeNumerator;

        if (hasQaData && qualityWeight > 0 && qualitySubScore.HasValue)
        {
            totalWeight = deliverySum + qualityWeight;
            compositeNumerator = completionScore * weights.Completion
                + disruptionScore * weights.Disruption
                + carryOverScore * weights.CarryOver
                + qualitySubScore.Value * qualityWeight;
        }
        else
        {
            totalWeight = deliverySum;
            compositeNumerator = completionScore * weights.Completion
                + disruptionScore * weights.Disruption
                + carryOverScore * weights.CarryOver;
        }

        var composite = totalWeight > 0 ? compositeNumerator / totalWeight : 0;

        composite = Math.Round(composite, 1);
        completionScore = Math.Round(completionScore, 1);
        disruptionScore = Math.Round(disruptionScore, 1);
        carryOverScore = Math.Round(carryOverScore, 1);

        decimal? roundedQualitySubScore = null;
        string? qualityRag = null;
        if (hasQaData && qualitySubScore.HasValue)
        {
            roundedQualitySubScore = Math.Round(qualitySubScore.Value, 1);
            qualityRag = HealthScoreCalculator.CompositeRag(qualitySubScore.Value);
        }

        return new HealthScoreData(
            composite, HealthScoreCalculator.CompositeRag(composite),
            completionScore, HealthScoreCalculator.MetricRag(metrics.CompletionRate, thresholds.CompletionGreen, thresholds.CompletionAmber, higherIsBetter: true),
            disruptionScore, HealthScoreCalculator.MetricRag(metrics.DisruptionRate, thresholds.DisruptionGreen, thresholds.DisruptionAmber, higherIsBetter: false),
            carryOverScore, HealthScoreCalculator.MetricRag(metrics.CarryOverRate, thresholds.CarryOverGreen, thresholds.CarryOverAmber, higherIsBetter: false))
        {
            QualitySubScore = roundedQualitySubScore,
            QualityRag = qualityRag,
            QualityBreakdown = hasQaData ? qualityBreakdown : null
        };
    }

    // --- Sparkline ---

    private static List<SprintSparklinePoint> BuildSparkline(
        List<Sprint> window,
        List<Sprint> allWindowSprints,
        string? subTeam,
        List<StatusTransition> statusTransitions,
        AppSettings settings,
        Func<List<SprintMembership>, List<StatusTransition>, Sprint, AppSettings, decimal> valueSelector,
        HashSet<string>? excludedDeveloperIds = null)
    {
        return window.Select(s =>
        {
            var sprintData = allWindowSprints.First(ws => ws.Id == s.Id);
            var memberships = FilterMemberships(sprintData.Memberships, subTeam, excludedDeveloperIds);
            return new SprintSparklinePoint(s.Name, Math.Round(valueSelector(memberships, statusTransitions, sprintData, settings), 1));
        }).ToList();
    }

    // --- Metric card builder ---

    private static SprintMetricCardData BuildMetricCard(
        string name,
        decimal value,
        string displayValue,
        decimal? delta,
        string polarity,
        List<SprintSparklinePoint> sparkline)
    {
        var (direction, deltaPolarity) = MetricCardBuilder.ComputeDelta(delta, polarity);

        return new SprintMetricCardData(name, Math.Round(value, 1), displayValue, delta.HasValue ? Math.Round(delta.Value, 1) : null, direction, deltaPolarity, sparkline);
    }

    // --- Effective SP for Ticket entities ---

    private static decimal? GetEffectiveTicketSp(Ticket t, int defaultSpPerBug)
    {
        if (t.StoryPoints.HasValue && t.StoryPoints.Value > 0)
            return t.StoryPoints;
        if (t.IssueType == "Bug" && defaultSpPerBug > 0)
            return (decimal)defaultSpPerBug;
        return null;
    }

    // --- Top epics ---

    private static List<EpicProgressData> ComputeTopEpics(
        List<SprintMembership> selectedMemberships,
        List<StatusTransition> statusTransitions,
        DateTime sprintStart,
        DateTime sprintEnd,
        List<Sprint> allSprints,
        string? subTeam,
        HashSet<string> completedStatuses,
        List<Ticket> allEpicTickets,
        int defaultSpPerBug,
        List<string> orderedStages,
        int endIndex)
    {
        var thisSprintByEpic = selectedMemberships
            .Where(m => m.Ticket?.EpicKey != null && m.RemovedAt == null && m.GetEffectiveSp(defaultSpPerBug).HasValue)
            .Where(m =>
            {
                var hasCompletion = statusTransitions
                    .Any(t => t.TicketId == m.TicketId
                              && t.Timestamp >= sprintStart
                              && t.Timestamp <= sprintEnd
                              && TransitionAttributionChecker.GetStageIndex(t.ToStatus, orderedStages) >= endIndex);
                return hasCompletion;
            })
            .GroupBy(m => m.Ticket!.EpicKey!)
            .Select(g => new
            {
                EpicKey = g.Key,
                EpicName = g.First().Ticket!.EpicName ?? g.Key,
                SpCompletedThisSprint = g.Sum(m => m.GetEffectiveSp(defaultSpPerBug)!.Value)
            })
            .OrderByDescending(e => e.SpCompletedThisSprint)
            .Take(3)
            .ToList();

        var filteredEpicTickets = string.IsNullOrWhiteSpace(subTeam)
            ? allEpicTickets
            : allEpicTickets.Where(t => t.Assignee?.SubTeam == subTeam).ToList();

        return thisSprintByEpic.Select(e =>
        {
            var epicTickets = filteredEpicTickets
                .Where(t => t.EpicKey == e.EpicKey && GetEffectiveTicketSp(t, defaultSpPerBug) != null)
                .ToList();

            var doneSp = epicTickets
                .Where(t => completedStatuses.Contains(t.CurrentStatus))
                .Sum(t => GetEffectiveTicketSp(t, defaultSpPerBug) ?? 0m);

            var totalSp = epicTickets.Sum(t => GetEffectiveTicketSp(t, defaultSpPerBug) ?? 0m);
            var completionPct = totalSp > 0 ? Math.Round(doneSp / totalSp * 100, 1) : 0;

            return new EpicProgressData(
                e.EpicName,
                Math.Round(e.SpCompletedThisSprint, 1),
                Math.Round(totalSp, 1),
                Math.Round(doneSp, 1),
                completionPct);
        }).ToList();
    }

    // --- Leaderboard ---

    private static List<DeveloperSummaryData> ComputeLeaderboard(
        List<Developer> developers,
        List<SprintMembership> memberships,
        List<StatusTransition> statusTransitions,
        DateTime sprintStart,
        DateTime sprintEnd,
        List<string> orderedStages,
        int endIndex,
        List<string> excludedStatuses,
        int defaultSpPerBug,
        Dictionary<string, Dictionary<int, int>> capacityLookup,
        List<Developer> allDevelopers,
        int sprintId)
    {
        var completed = memberships
            .Where(m => m.RemovedAt == null
                        && !excludedStatuses.Contains(m.FinalStatus, StringComparer.OrdinalIgnoreCase)
                        && m.Ticket?.AssigneeId != null
                        && TransitionAttributionChecker.IsCompletedInSprint(
                            m.TicketId, statusTransitions, sprintStart, sprintEnd, orderedStages, endIndex).IsCompleted)
            .ToList();

        var completedByDev = completed
            .GroupBy(m => m.Ticket!.AssigneeId!)
            .ToDictionary(g => g.Key, g => g.ToList());

        return developers
            .Select(d =>
            {
                var devCompleted = completedByDev.GetValueOrDefault(d.Id, new List<SprintMembership>());
                var featureSp = devCompleted.Where(m => m.Ticket?.IssueType != "Bug").Sum(m => m.GetEffectiveSp(defaultSpPerBug) ?? 0m);
                var bugSp = devCompleted.Where(m => m.Ticket?.IssueType == "Bug").Sum(m => m.GetEffectiveSp(defaultSpPerBug) ?? 0m);
                var featureTickets = devCompleted.Count(m => m.Ticket?.IssueType != "Bug");
                var bugTickets = devCompleted.Count(m => m.Ticket?.IssueType == "Bug");
                var totalSp = featureSp + bugSp;
                var capacityPercent = capacityLookup.TryGetValue(d.Id, out var sprintMap) &&
                    sprintMap.TryGetValue(sprintId, out var pct)
                    ? pct
                    : allDevelopers.FirstOrDefault(dev => dev.Id == d.Id)?.DefaultCapacityPercent ?? 100;
                return new DeveloperSummaryData(
                    d.DisplayName,
                    d.AvatarUrl,
                    d.SubTeam,
                    Math.Round(totalSp, 1),
                    Math.Round(featureSp, 1),
                    Math.Round(bugSp, 1),
                    featureTickets,
                    bugTickets,
                    capacityPercent);
            })
            .OrderByDescending(d => d.SpCompleted)
            .ThenBy(d => d.DisplayName)
            .ToList();
    }

    // --- Flags ---

    private static SprintFlagsData ComputeFlags(
        Sprint selectedSprint,
        List<SprintMembership> selectedMemberships,
        List<StatusTransition> statusTransitions,
        List<Sprint> allSprints,
        string? subTeam,
        List<Developer> filteredDevelopers,
        List<string> orderedStages,
        int endIndex,
        List<string> excludedStatuses,
        int defaultSpPerBug,
        int planningWindowDays,
        HashSet<string>? excludedDeveloperIds = null,
        TestingCrunchFlag? testingCrunch = null)
    {
        var allMembershipsFlat = allSprints
            .SelectMany(s => FilterMemberships(s.Memberships, subTeam, excludedDeveloperIds))
            .ToList();

        var zombies = allMembershipsFlat
            .GroupBy(m => m.TicketId)
            .Where(g => g.Select(m => m.SprintId).Distinct().Count() >= 3)
            .Select(g =>
            {
                var first = g.First();
                return new ZombieTicketData(
                    first.TicketId,
                    first.Ticket?.Summary ?? first.TicketId,
                    g.Select(m => m.SprintId).Distinct().Count(),
                    first.Ticket?.Assignee?.DisplayName);
            })
            .OrderByDescending(z => z.SprintCount)
            .ToList();

        var disruptionCutoff = selectedSprint.StartDate.AddDays(planningWindowDays);
        var disruptedMemberships = selectedMemberships
            .Where(m => m.AddedAt > disruptionCutoff && m.RemovedAt == null)
            .ToList();

        MidSprintDisruptionData? midSprintDisruption = disruptedMemberships.Count > 0
            ? new MidSprintDisruptionData(
                Math.Round(disruptedMemberships.Where(m => m.GetEffectiveSp(defaultSpPerBug).HasValue).Sum(m => m.GetEffectiveSp(defaultSpPerBug)!.Value), 1),
                disruptedMemberships.Count)
            : null;

        var completedByDev = selectedMemberships
            .Where(m => m.RemovedAt == null
                        && m.GetEffectiveSp(defaultSpPerBug).HasValue
                        && !excludedStatuses.Contains(m.FinalStatus, StringComparer.OrdinalIgnoreCase)
                        && m.Ticket?.AssigneeId != null
                        && TransitionAttributionChecker.IsCompletedInSprint(
                            m.TicketId, statusTransitions,
                            selectedSprint.StartDate, selectedSprint.EndDate,
                            orderedStages, endIndex).IsCompleted)
            .GroupBy(m => m.Ticket!.AssigneeId!)
            .ToDictionary(g => g.Key, g => g.Sum(m => m.GetEffectiveSp(defaultSpPerBug)!.Value));

        var assigneesInSprint = selectedMemberships
            .Where(m => m.RemovedAt == null && m.Ticket?.AssigneeId != null)
            .Select(m => m.Ticket!.AssigneeId!)
            .ToHashSet();

        var zeroSpDevs = filteredDevelopers
            .Where(d => assigneesInSprint.Contains(d.Id) && completedByDev.GetValueOrDefault(d.Id, 0) == 0)
            .Select(d => d.DisplayName)
            .OrderBy(n => n)
            .ToList();

        var hasFlags = zombies.Count > 0 || midSprintDisruption is not null || zeroSpDevs.Count > 0 || testingCrunch is not null;

        return new SprintFlagsData(zombies, midSprintDisruption, zeroSpDevs, hasFlags)
        {
            TestingCrunch = testingCrunch
        };
    }
}
