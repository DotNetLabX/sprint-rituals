using Blocks.Contracts.Grpc;
using Blocks.Contracts.Grpc.DataContracts;
using Fokus.API.Features.Analytics.GetSprintSummary;
using Fokus.Domain.Analytics;
using Fokus.Persistence;
using ProtoBuf.Grpc;

// Alias to avoid name collision with Blocks.Contracts.Grpc.DataContracts.SprintInfo
using FokusSprintInfo = Fokus.API.Features.Analytics.GetSprintSummary.SprintInfo;

namespace Fokus.API.Grpc;

/// <summary>
/// gRPC server implementation exposing anonymized Fokus sprint analytics to other modules.
/// Privacy boundary (§9): no per-developer data crosses this surface — team-level aggregates only.
/// </summary>
public class SprintAnalyticsGrpcService(
    SprintRepository sprintRepository,
    AppSettingsRepository appSettingsRepository,
    DeveloperRepository developerRepository,
    TicketRepository ticketRepository,
    TestExecutionRepository testExecutionRepository,
    SprintSummaryAnalyzer sprintSummaryCalculator,
    ScopeChangeAnalyzer scopeChangeCalculator,
    CarryOverAnalyzer carryOverCalculator,
    QaMetricsAnalyzer qaMetricsCalculator) : ISprintAnalyticsService
{
    public async Task<AnonymizedSprintSummary> GetSprintSummaryAsync(SprintQuery request, CancellationToken ct = default)
    {
        var (selectedSprint, windowSprints, activeDevelopers, allDevelopers, settings,
            capacityLookup, statusTransitions, allEpicTickets, excludedIds) =
            await LoadSprintSummaryDataAsync(request.SprintId, ct);

        // QA sub-score — only when Xray is enabled
        decimal? qualitySubScore = null;
        QaBreakdownData? qualityBreakdown = null;
        var hasQaData = false;

        if (settings.Xray.Enabled)
        {
            (qualitySubScore, qualityBreakdown, hasQaData) =
                await LoadQaDataAsync(selectedSprint, statusTransitions, settings, null, ct);
        }

        var result = sprintSummaryCalculator.ComputeSummary(
            selectedSprint, windowSprints, activeDevelopers, settings,
            subTeam: null, allEpicTickets, capacityLookup, allDevelopers,
            statusTransitions, excludedIds, qualitySubScore, qualityBreakdown, hasQaData);

        return MapToAnonymizedSummary(result, selectedSprint, statusTransitions, settings);
    }

    public async Task<DisruptionMetrics> GetDisruptionMetricsAsync(SprintQuery request, CancellationToken ct = default)
    {
        var analyticsSprints = await sprintRepository.GetAnalyticsSprintsAsync(ct);
        var target = analyticsSprints.FirstOrDefault(s => s.Id == request.SprintId);
        if (target is null)
            return new DisruptionMetrics();

        var settings = await appSettingsRepository.GetAsync(ct);
        var allDevelopers = await developerRepository.GetAllAsync(ct);
        var allClosedIds = analyticsSprints.Where(s => s.State == SprintState.Closed)
            .Select(s => s.Id).ToList();
        var capacityRecords = await developerRepository.GetCapacitiesForSprintsAsync(allClosedIds, ct);

        var sprintIdsToLoad = BuildSingleSprintLoadIds(target, analyticsSprints);
        var loadedSprints = await sprintRepository.GetSprintsWithMembershipsAsync(sprintIdsToLoad, ct);
        var targetSprint = loadedSprints.First(s => s.Id == target.Id);

        var statusTransitions = await ticketRepository.GetStatusTransitionsForSprintTicketsAsync(sprintIdsToLoad, ct);
        var excludedIds = Fokus.Domain.Analytics.ExcludedDeveloperFilter.GetExcludedDeveloperIds(
            targetSprint, allDevelopers, capacityRecords, statusTransitions, settings);

        var priorSprint = loadedSprints.FirstOrDefault(s => s.Id != target.Id);
        var singleResult = scopeChangeCalculator.ComputeSingleSprint(
            targetSprint, priorSprint, statusTransitions, settings, subTeam: null, excludedIds);

        return MapToDisruptionMetrics(singleResult);
    }

    public async Task<CarryOverHistory> GetCarryOverHistoryAsync(TeamQuery request, CancellationToken ct = default)
    {
        var analyticsSprints = await sprintRepository.GetAnalyticsSprintsAsync(ct);
        var ascending = analyticsSprints.Where(s => s.State == SprintState.Closed)
            .OrderBy(s => s.StartDate).ToList();

        if (ascending.Count == 0)
            return new CarryOverHistory();

        var settings = await appSettingsRepository.GetAsync(ct);
        var allDevelopers = await developerRepository.GetAllAsync(ct);
        var allClosedIds = ascending.Select(s => s.Id).ToList();
        var capacityRecords = await developerRepository.GetCapacitiesForSprintsAsync(allClosedIds, ct);

        var allLoadedSprints = await sprintRepository.GetSprintsWithMembershipsAsync(allClosedIds, ct);
        var allSprints = allLoadedSprints.OrderBy(s => s.StartDate).ToList();
        var statusTransitions = await ticketRepository.GetStatusTransitionsForSprintTicketsAsync(allClosedIds, ct);

        var selectedSprints = allSprints.TakeLast(5).ToList();
        var excludedInAll = BuildCrossSprintExclusions(selectedSprints, allDevelopers, capacityRecords, statusTransitions, settings);

        var multiResult = carryOverCalculator.ComputeMultiSprint(
            selectedSprints, allSprints, statusTransitions, settings, subTeam: null, excludedInAll);

        return MapToCarryOverHistory(multiResult, selectedSprints, allSprints);
    }

    // --- Private data-loading helpers ---

    private async Task<(
        Sprint selectedSprint,
        List<Sprint> windowSprints,
        List<Developer> activeDevelopers,
        List<Developer> allDevelopers,
        AppSettings settings,
        Dictionary<string, Dictionary<int, int>> capacityLookup,
        List<StatusTransition> statusTransitions,
        List<Ticket> allEpicTickets,
        HashSet<string> excludedIds)> LoadSprintSummaryDataAsync(int sprintId, CancellationToken ct)
    {
        var sprints = await sprintRepository.GetAnalyticsSprintsAsync(ct);
        var target = sprints.FirstOrDefault(s => s.Id == sprintId) ?? sprints.FirstOrDefault()
            ?? throw new InvalidOperationException("No sprints available.");

        var ascending = sprints.Where(s => s.State == SprintState.Closed).OrderBy(s => s.StartDate).ToList();
        var selectedIndex = ascending.FindIndex(s => s.Id == target.Id);
        var sparklineAnchorIndex = selectedIndex >= 0 ? selectedIndex : ascending.Count - 1;
        var windowIds = ascending.Take(sparklineAnchorIndex + 1).TakeLast(4).Select(s => s.Id).ToList();
        if (!windowIds.Contains(target.Id))
            windowIds.Add(target.Id);

        var windowSprints = await sprintRepository.GetSprintsWithMembershipsAsync(windowIds, ct);
        var activeDevelopers = await developerRepository.GetActiveDevelopersAsync(ct);
        var allDevelopers = await developerRepository.GetAllAsync(ct);
        var settings = await appSettingsRepository.GetAsync(ct);

        var capacityRecords = await developerRepository.GetCapacitiesForSprintsAsync(windowIds, ct);
        var capacityLookup = capacityRecords
            .GroupBy(c => c.DeveloperAccountId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(c => c.SprintId, c => c.CapacityPercent));

        var statusTransitions = await ticketRepository.GetStatusTransitionsForSprintTicketsAsync(windowIds, ct);
        var selectedSprint = windowSprints.First(s => s.Id == target.Id);
        var excludedIds = Fokus.Domain.Analytics.ExcludedDeveloperFilter.GetExcludedDeveloperIds(
            selectedSprint, allDevelopers, capacityRecords, statusTransitions, settings);
        var filteredActive = activeDevelopers.Where(d => !excludedIds.Contains(d.Id)).ToList();

        var allEpicTickets = await ticketRepository.GetTicketsWithEpicAsync(ct);

        return (selectedSprint, windowSprints, filteredActive, allDevelopers, settings,
            capacityLookup, statusTransitions, allEpicTickets, excludedIds);
    }

    private async Task<(decimal? qualitySubScore, QaBreakdownData? qualityBreakdown, bool hasQaData)>
        LoadQaDataAsync(Sprint selectedSprint, List<StatusTransition> statusTransitions,
            AppSettings settings, string? subTeam, CancellationToken ct)
    {
        var sprintTEs = await testExecutionRepository.GetTestExecutionsForSprintAsync(selectedSprint.Id, ct);
        var qaResult = qaMetricsCalculator.ComputeQaMetrics(
            sprintTEs, selectedSprint.Memberships.ToList(), statusTransitions,
            selectedSprint, priorSprint: null, priorSprintTEs: null, priorMemberships: null,
            sparklineWindow: [], sparklineTEsBySprintId: [], sparklineMembershipsBySprintId: [],
            settings, subTeam);

        return (qaResult.QualitySubScore, qaResult.QualityBreakdown, hasQaData: true);
    }

    private static List<int> BuildSingleSprintLoadIds(Sprint target, IReadOnlyList<Sprint> analyticsSprints)
    {
        var ascending = analyticsSprints.Where(s => s.State == SprintState.Closed)
            .OrderBy(s => s.StartDate).ToList();
        var closedIndex = ascending.FindIndex(s => s.Id == target.Id);
        var priorLightweight = closedIndex > 0
            ? ascending[closedIndex - 1]
            : (target.State == SprintState.Active && ascending.Count > 0 ? ascending[^1] : null);

        var ids = new List<int> { target.Id };
        if (priorLightweight is not null)
            ids.Add(priorLightweight.Id);
        return ids;
    }

    private static HashSet<string> BuildCrossSprintExclusions(
        List<Sprint> sprints, List<Developer> allDevelopers,
        List<DeveloperSprintCapacity> capacityRecords, List<StatusTransition> statusTransitions,
        AppSettings settings) =>
        sprints
            .SelectMany(s => Fokus.Domain.Analytics.ExcludedDeveloperFilter.GetExcludedDeveloperIds(
                s, allDevelopers, capacityRecords, statusTransitions, settings))
            .GroupBy(id => id)
            .Where(g => g.Count() == sprints.Count)
            .Select(g => g.Key)
            .ToHashSet();

    // --- Mapping helpers (anonymization boundary) ---

    private static AnonymizedSprintSummary MapToAnonymizedSummary(
        SprintSummaryData result,
        Sprint selectedSprint,
        List<StatusTransition> statusTransitions,
        AppSettings settings)
    {
        var metrics = result.Metrics;
        var health = result.HealthScore;

        // Single source of truth for the scope-addition population — both the displayed
        // top-5 list and the true total derive from the IDENTICAL predicate so "+N more"
        // (total − shown) can never promise an unshowable (bug / removed) ticket (RA5-D).
        var scopeAdditions = GetScopeAdditions(selectedSprint, settings).ToList();
        var topScopeAdditions = BuildTopScopeAdditions(scopeAdditions);

        return new AnonymizedSprintSummary
        {
            CompletionRate = metrics?.CompletionRate.Value ?? 0m,
            DisruptionRate = (metrics?.ScopeDisruptionRate.Value ?? 0m) + (metrics?.BugDisruptionRate.Value ?? 0m),
            CarryOverRate = metrics?.CarryOverRate.Value ?? 0m,
            HealthScore = health?.CompositeScore ?? 0m,
            HealthRag = health?.CompositeRag ?? "red",
            SpCompleted = metrics?.SpCompleted.Value ?? 0m,
            ActiveSp = result.Sprint is not null
                ? selectedSprint.Memberships
                    .Where(m => m.RemovedAt == null)
                    .Sum(m => m.StoryPoints ?? settings.BugRatio.DefaultSpPerBug) : 0m,
            TopScopeAdditions = topScopeAdditions,

            // Sprint identity (RA4 — additive, Orders 9–12)
            SprintId = selectedSprint.Id,
            SprintName = selectedSprint.Name,
            StartDate = selectedSprint.StartDate,
            EndDate = selectedSprint.EndDate,

            // Per-rate RAG bands (RA4 — additive, Orders 13–15)
            // DisruptionRag bands on the combined DisruptionRate (Scope + Bug) — same input as
            // the DisruptionRate field above. Map straight from the analyzer, never recompute (Rule 3).
            CompletionRag = health?.CompletionRag ?? "red",
            DisruptionRag = health?.DisruptionRag ?? "red",
            CarryOverRag = health?.CarryOverRag ?? "red",

            // True scope-addition total (RA5-D — additive, Order 16).
            // Counted over the SAME predicate as TopScopeAdditions, BEFORE the top-5 cap,
            // so the consumer's "+N more" overflow (total − shown) is accurate.
            ScopeAdditionTotal = scopeAdditions.Count,
        };
    }

    /// <summary>
    /// The mid-sprint scope-addition population: post-planning, not removed, non-bug.
    /// SINGLE source of truth — both the displayed top-5 and the true total count this set,
    /// so they can never drift (RA5-D, note 4).
    /// </summary>
    private static IEnumerable<SprintMembership> GetScopeAdditions(Sprint sprint, AppSettings settings)
    {
        var planningCutoff = sprint.StartDate.AddDays(settings.Sync.PlanningWindowDays);
        return sprint.Memberships
            .Where(m => m.AddedAt > planningCutoff && m.RemovedAt == null && m.Ticket?.IssueType != "Bug");
    }

    private static List<ScopeAdditionEntry> BuildTopScopeAdditions(IEnumerable<SprintMembership> scopeAdditions)
    {
        // Rule 7: most-recently-added first. AddedAt is an ORDERING KEY ONLY — it is never
        // projected into ScopeAdditionEntry, so no timestamp crosses the privacy boundary.
        return scopeAdditions
            .OrderByDescending(m => m.AddedAt)
            .Take(5)
            .Select(m => new ScopeAdditionEntry
            {
                TicketKey = m.TicketId,
                Summary = m.Ticket?.Summary ?? m.TicketId,
                StoryPoints = m.StoryPoints,
            })
            .ToList();
    }

    private static DisruptionMetrics MapToDisruptionMetrics(ScopeChangeSingleSprintData result)
    {
        var metrics = result.Metrics;
        return new DisruptionMetrics
        {
            ScopeDisruptionRate = metrics.DisruptionRate.Value,
            BugDisruptionRate = 0m,
            AddedSp = metrics.AddedSp.Value,
            BugAddedSp = 0m,
            // The event table writes the Action lowercase ("added" — ScopeChangeAnalyzer.cs).
            // Compare case-insensitively so this count is correct (was `== "Added"`, which never
            // matched → always 0). The sibling "removed" literal is also lowercase; no "removed"
            // consumer exists in this map today (BugAddedSp/BugDisruptionRate are hard-0m), but any
            // future action compare here MUST use OrdinalIgnoreCase to avoid reintroducing this bug.
            MidSprintAdditionCount = result.Events.Count(
                e => string.Equals(e.Action, "added", StringComparison.OrdinalIgnoreCase) && !e.IsExcluded),
        };
    }

    private static CarryOverHistory MapToCarryOverHistory(
        CarryOverMultiSprintData result,
        List<Sprint> selectedSprints,
        List<Sprint> allSprints)
    {
        var currentSprintData = result.PerSprintData.LastOrDefault();

        var history = result.PerSprintData
            .Zip(result.Sprints, (data, info) => new SprintCarryOverEntry
            {
                SprintName = info.Name,
                CarryOverRate = data.CarryOverRate,
            })
            .ToList();

        return new CarryOverHistory
        {
            CarryOverRate = currentSprintData?.CarryOverRate ?? 0m,
            CarryOverSp = currentSprintData?.CarryOverSp ?? 0m,
            ZombieTicketCount = result.ZombieTickets.Count,
            SprintHistory = history,
        };
    }
}
