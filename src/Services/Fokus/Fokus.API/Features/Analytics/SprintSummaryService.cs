namespace Fokus.API.Features.Analytics;

// --- Response record hierarchy ---

public record SprintInfo(
    int Id,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    int DurationDays,
    DateTime SyncedAt);

public record QualityBreakdownResult(
    decimal CoverageScore,
    int CoverageWeight,
    decimal PassRateScore,
    int PassRateWeight);

public record HealthScoreResult(
    decimal CompositeScore,
    string CompositeRag,
    decimal CompletionSubScore,
    string CompletionRag,
    decimal DisruptionSubScore,
    string DisruptionRag,
    decimal CarryOverSubScore,
    string CarryOverRag)
{
    public decimal? QualitySubScore { get; init; }
    public string? QualityRag { get; init; }
    public QualityBreakdownResult? QualityBreakdown { get; init; }
}

public record SparklinePoint(string SprintName, decimal Value);

public record MetricCard(
    string Name,
    decimal Value,
    string DisplayValue,
    decimal? Delta,
    string? DeltaDirection,
    string? DeltaPolarity,
    List<SparklinePoint> Sparkline)
{
    public string? Rag { get; init; }
}

public record MetricsResult(
    MetricCard SpCompleted,
    MetricCard CompletionRate,
    MetricCard ScopeDisruptionRate,
    MetricCard BugDisruptionRate,
    MetricCard CarryOverRate,
    decimal BugSpCompleted);

public record EpicProgress(
    string EpicName,
    decimal SpCompletedThisSprint,
    decimal TotalSp,
    decimal DoneSp,
    decimal CompletionPercentage);

public record DeveloperSummary(
    string DisplayName,
    string? AvatarUrl,
    string? SubTeam,
    decimal SpCompleted,
    decimal FeatureSp,
    decimal BugSp,
    int FeatureTickets,
    int BugTickets,
    int CapacityPercent);

public record ZombieTicket(string TicketKey, string Summary, int SprintCount, string? AssigneeName);

public record MidSprintDisruption(decimal TotalSp, int TicketCount);

public record FlagsResult(
    List<ZombieTicket> ZombieTickets,
    MidSprintDisruption? MidSprintDisruption,
    List<string> ZeroSpDevelopers,
    bool HasAnyFlags)
{
    public TestingCrunchFlag? TestingCrunch { get; init; }
}

public record SprintSummaryResponse(
    SprintInfo? Sprint,
    HealthScoreResult? HealthScore,
    MetricsResult? Metrics,
    List<EpicProgress> TopEpics,
    List<DeveloperSummary> Leaderboard,
    FlagsResult Flags);

// --- Service ---

public class SprintSummaryService
{
    public SprintSummaryResponse ComputeSummary(
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
        QualityBreakdownResult? qualityBreakdown = null,
        bool hasQaData = false,
        TestingCrunchFlag? testingCrunch = null)
    {
        // C2: sub-team filtering + cross-cutting exclusion
        var selectedMemberships = FilterMemberships(selectedSprint.Memberships, subTeam, excludedDeveloperIds);
        var filteredDevelopers = FilterDevelopers(activeDevelopers, subTeam);

        var completedStatuses = CompletionChecker.ResolveCompletedStatuses(settings);
        var thresholds = settings.HealthThresholds;
        var weights = settings.HealthWeights;
        var defaultSpPerBug = settings.DefaultSpPerBug;

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
            settings.ExcludedFromScopeStatuses, defaultSpPerBug, settings.PlanningWindowDays);

        // Delta metrics (C1 pattern)
        SprintMetrics? prior = priorMemberships is not null && priorSprint is not null
            ? ComputeMetrics(
                priorMemberships, statusTransitions,
                priorSprint.StartDate, priorSprint.EndDate,
                orderedStages, startIndex, endIndex,
                settings.ExcludedFromScopeStatuses, defaultSpPerBug, settings.PlanningWindowDays)
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
        var metrics = new MetricsResult(
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
            settings.ExcludedFromScopeStatuses, defaultSpPerBug, settings.PlanningWindowDays, excludedDeveloperIds,
            testingCrunch);

        var sprintInfo = new SprintInfo(
            selectedSprint.Id,
            selectedSprint.Name,
            selectedSprint.StartDate,
            selectedSprint.EndDate,
            (int)(selectedSprint.EndDate - selectedSprint.StartDate).TotalDays,
            selectedSprint.SyncedAt);

        return new SprintSummaryResponse(sprintInfo, healthScore, metrics, topEpics, leaderboard, flags);
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

        // Pre-group transitions by ticketId once — O(M) — so per-ticket checks are O(1) lookup + O(k) scan
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

        // Spec BR21 formulas
        var completionRate = activeSp > 0 ? completedSp / activeSp * 100 : 0;
        var scopeDisruptionRate = activeSp > 0 ? addedSp / activeSp * 100 : 0;
        var bugDisruptionRate = activeSp > 0 ? bugAddedSp / activeSp * 100 : 0;
        var disruptionRate = scopeDisruptionRate + bugDisruptionRate;
        var carryOverRate = activeSp > 0 ? carryOverSp / activeSp * 100 : 0;

        return new SprintMetrics(activeSp, completedSp, addedSp, carryOverSp, completionRate, disruptionRate, scopeDisruptionRate, bugDisruptionRate, carryOverRate, completedSp, bugSpCompleted);
    }

    // --- Sparkline helpers (new signature: memberships, transitions, sprint, settings) ---

    private static decimal ComputeSpCompleted(
        List<SprintMembership> memberships,
        List<StatusTransition> transitions,
        Sprint sprint,
        AppSettings settings)
    {
        var (orderedStages, _) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, orderedStages);
        var excludedStatuses = settings.ExcludedFromScopeStatuses;
        var defaultSpPerBug = settings.DefaultSpPerBug;

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
        var defaultSpPerBug = settings.DefaultSpPerBug;

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
        var defaultSpPerBug = settings.DefaultSpPerBug;
        var planningCutoff = sprint.StartDate.AddDays(settings.PlanningWindowDays);

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
        var defaultSpPerBug = settings.DefaultSpPerBug;
        var planningCutoff = sprint.StartDate.AddDays(settings.PlanningWindowDays);

        var transitionsByTicket = transitions
            .GroupBy(t => t.TicketId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        // activeSp = feature active (denominator per spec BR21)
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
        var defaultSpPerBug = settings.DefaultSpPerBug;

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

    private static HealthScoreResult ComputeHealthScore(
        SprintMetrics metrics,
        HealthThresholdConfig thresholds,
        HealthWeightConfig weights,
        decimal? qualitySubScore = null,
        QualityBreakdownResult? qualityBreakdown = null,
        int qualityWeight = 0,
        bool hasQaData = false)
    {
        var completionScore = ScoreHigherIsBetter(metrics.CompletionRate, thresholds.CompletionGreen, thresholds.CompletionAmber);
        var disruptionScore = ScoreLowerIsBetter(metrics.DisruptionRate, thresholds.DisruptionGreen, thresholds.DisruptionAmber);
        var carryOverScore = ScoreLowerIsBetter(metrics.CarryOverRate, thresholds.CarryOverGreen, thresholds.CarryOverAmber);

        var deliverySum = weights.Completion + weights.Disruption + weights.CarryOver;

        // BR18 three-way branching:
        // - hasQaData && qualityWeight > 0: include quality in composite
        // - hasQaData && qualityWeight == 0 (observation mode): quality computed but not in composite
        // - !hasQaData: quality null, delivery-only composite
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

        // Quality fields: non-null whenever hasQaData (even observation mode), null when !hasQaData
        decimal? roundedQualitySubScore = null;
        string? qualityRag = null;
        if (hasQaData && qualitySubScore.HasValue)
        {
            roundedQualitySubScore = Math.Round(qualitySubScore.Value, 1);
            qualityRag = HealthScoreCalculator.CompositeRag(qualitySubScore.Value);
        }

        return new HealthScoreResult(
            composite, CompositeRag(composite),
            completionScore, MetricRag(metrics.CompletionRate, thresholds.CompletionGreen, thresholds.CompletionAmber, higherIsBetter: true),
            disruptionScore, MetricRag(metrics.DisruptionRate, thresholds.DisruptionGreen, thresholds.DisruptionAmber, higherIsBetter: false),
            carryOverScore, MetricRag(metrics.CarryOverRate, thresholds.CarryOverGreen, thresholds.CarryOverAmber, higherIsBetter: false))
        {
            QualitySubScore = roundedQualitySubScore,
            QualityRag = qualityRag,
            QualityBreakdown = hasQaData ? qualityBreakdown : null
        };
    }

    private static decimal ScoreHigherIsBetter(decimal value, decimal green, decimal amber) =>
        HealthScoreCalculator.ScoreHigherIsBetter(value, green, amber);

    private static decimal ScoreLowerIsBetter(decimal value, decimal green, decimal amber) =>
        HealthScoreCalculator.ScoreLowerIsBetter(value, green, amber);

    private static string CompositeRag(decimal score) =>
        HealthScoreCalculator.CompositeRag(score);

    private static string MetricRag(decimal value, decimal green, decimal amber, bool higherIsBetter) =>
        HealthScoreCalculator.MetricRag(value, green, amber, higherIsBetter);

    // --- Sparkline (refactored: carries transitions, sprint object, and settings) ---

    private static List<SparklinePoint> BuildSparkline(
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
            return new SparklinePoint(s.Name, Math.Round(valueSelector(memberships, statusTransitions, sprintData, settings), 1));
        }).ToList();
    }

    // --- Metric card builder ---

    private static MetricCard BuildMetricCard(
        string name,
        decimal value,
        string displayValue,
        decimal? delta,
        string polarity,
        List<SparklinePoint> sparkline)
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

        return new MetricCard(name, Math.Round(value, 1), displayValue, delta.HasValue ? Math.Round(delta.Value, 1) : null, direction, deltaPolarity, sparkline);
    }

    // --- Effective SP for Ticket entities (mirrors EpicProgressService.GetEffectiveTicketSp) ---

    private static decimal? GetEffectiveTicketSp(Ticket t, int defaultSpPerBug)
    {
        if (t.StoryPoints.HasValue && t.StoryPoints.Value > 0)
            return t.StoryPoints;
        if (t.IssueType == "Bug" && defaultSpPerBug > 0)
            return (decimal)defaultSpPerBug;
        return null;
    }

    // --- Top epics (dual mode per spec BR16: sprint attribution = transition-based, epic progress = position-based) ---

    private static List<EpicProgress> ComputeTopEpics(
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
        // Sprint attribution: transition-based (endIndex boundary check)
        // Epic progress: position-based CurrentStatus (completedStatuses) — spec BR16 dual mode

        // Step 1: Group by EpicKey, sum SP for tickets with a qualifying completion transition during this sprint
        var thisSprintByEpic = selectedMemberships
            .Where(m => m.Ticket?.EpicKey != null && m.RemovedAt == null && m.GetEffectiveSp(defaultSpPerBug).HasValue)
            .Where(m =>
            {
                // Transition-based: first transition to endStage (or beyond) within sprint window
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

        // Step 2 (BR16): for each top epic, compute overall completion from ALL tickets with that epic key
        // using ticket current status — position-based, not sprint attribution
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

            return new EpicProgress(
                e.EpicName,
                Math.Round(e.SpCompletedThisSprint, 1),
                Math.Round(totalSp, 1),
                Math.Round(doneSp, 1),
                completionPct);
        }).ToList();
    }

    // --- Leaderboard (transition-based completion) ---

    private static List<DeveloperSummary> ComputeLeaderboard(
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
        // Transition-based completed memberships
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
                return new DeveloperSummary(
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

    private static FlagsResult ComputeFlags(
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
        // Zombie tickets (BR16): tickets appearing in 3+ sprints
        var allMembershipsFlat = allSprints
            .SelectMany(s => FilterMemberships(s.Memberships, subTeam, excludedDeveloperIds))
            .ToList();

        var zombies = allMembershipsFlat
            .GroupBy(m => m.TicketId)
            .Where(g => g.Select(m => m.SprintId).Distinct().Count() >= 3)
            .Select(g =>
            {
                var first = g.First();
                return new ZombieTicket(
                    first.TicketId,
                    first.Ticket?.Summary ?? first.TicketId,
                    g.Select(m => m.SprintId).Distinct().Count(),
                    first.Ticket?.Assignee?.DisplayName);
            })
            .OrderByDescending(z => z.SprintCount)
            .ToList();

        // Mid-sprint disruption: added after planning window closes, not removed (uses planningWindowDays per spec)
        var disruptionCutoff = selectedSprint.StartDate.AddDays(planningWindowDays);
        var disruptedMemberships = selectedMemberships
            .Where(m => m.AddedAt > disruptionCutoff && m.RemovedAt == null)
            .ToList();

        MidSprintDisruption? midSprintDisruption = disruptedMemberships.Count > 0
            ? new MidSprintDisruption(
                Math.Round(disruptedMemberships.Where(m => m.GetEffectiveSp(defaultSpPerBug).HasValue).Sum(m => m.GetEffectiveSp(defaultSpPerBug)!.Value), 1),
                disruptedMemberships.Count)
            : null;

        // Zero-SP developers (BR18): active devs who had work assigned but completed 0 SP (transition-based)
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

        return new FlagsResult(zombies, midSprintDisruption, zeroSpDevs, hasFlags)
        {
            TestingCrunch = testingCrunch
        };
    }
}
