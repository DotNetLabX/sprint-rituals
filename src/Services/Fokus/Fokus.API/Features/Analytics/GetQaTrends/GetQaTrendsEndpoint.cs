namespace Fokus.API.Features.Analytics.GetQaTrends;

[HttpGet("/api/analytics/qa-trends")]
[Tags("Analytics")]
public class GetQaTrendsEndpoint(
    SprintRepository sprintRepository,
    AppSettingsRepository appSettingsRepository,
    TestExecutionRepository testExecutionRepository,
    TicketRepository ticketRepository,
    DeveloperRepository developerRepository,
    BugRatioService bugRatioService,
    QaTrendsService qaTrendsService)
    : Endpoint<GetQaTrendsRequest, QaTrendsResponse>
{
    public override async Task HandleAsync(GetQaTrendsRequest req, CancellationToken ct)
    {
        // 1. Load settings. If Xray is disabled, return empty.
        var settings = await appSettingsRepository.GetAsync(ct);
        if (!settings.XrayEnabled)
        {
            await SendOkAsync(new QaTrendsResponse(
                HasQaData: false,
                Sprints: [],
                QualityTrends: [],
                TestingVolume: [],
                DefectCorrelation: null), ct);
            return;
        }

        // 2. Load analytics sprints, but QA trends use closed sprints only for aggregates and N+1 correlation
        var allSprints = await sprintRepository.GetAnalyticsSprintsAsync(ct);
        var ascending = allSprints.Where(s => s.State == SprintState.Closed).OrderBy(s => s.StartDate).ToList();

        if (ascending.Count < 2)
        {
            await SendOkAsync(new QaTrendsResponse(
                HasQaData: false,
                Sprints: [],
                QualityTrends: [],
                TestingVolume: [],
                DefectCorrelation: null), ct);
            return;
        }

        // 3. Normalize last: 0 means all, null means all, < 2 treat as 2
        var allSprintIds = ascending.Select(s => s.Id).ToList();

        // 4. Filter to sprints with QA data
        var qaSyncedIds = await testExecutionRepository.GetSprintIdsWithQaDataAsync(allSprintIds, ct);
        var qaSyncedAscending = ascending.Where(s => qaSyncedIds.Contains(s.Id)).ToList();

        if (qaSyncedAscending.Count < 2)
        {
            await SendOkAsync(new QaTrendsResponse(
                HasQaData: false,
                Sprints: [],
                QualityTrends: [],
                TestingVolume: [],
                DefectCorrelation: null), ct);
            return;
        }

        // 5. Select last N QA-synced sprints
        int? last = req.Last;
        List<Sprint> targetQaSprints;
        if (last == null || last == 0)
        {
            targetQaSprints = qaSyncedAscending;
        }
        else
        {
            var effectiveLast = Math.Max(last.Value, 2);
            targetQaSprints = qaSyncedAscending.TakeLast(effectiveLast).ToList();
        }

        // 6. Load target sprints with memberships
        var targetIds = targetQaSprints.Select(s => s.Id).ToList();
        var loadedSprints = await sprintRepository.GetSprintsWithMembershipsAsync(targetIds, ct);
        var sortedLoaded = loadedSprints.OrderBy(s => s.StartDate).ToList();

        // Build membershipsBySprintId
        var membershipsBySprintId = sortedLoaded
            .ToDictionary(s => s.Id, s => s.Memberships.ToList());

        // 7. Load TEs per sprint
        var tesBySprintId = new Dictionary<int, List<TestExecution>>();
        foreach (var sprintId in targetIds)
        {
            tesBySprintId[sprintId] = await testExecutionRepository.GetTestExecutionsForSprintAsync(sprintId, ct);
        }

        // 8. Load status transitions for target sprint tickets
        var statusTransitions = await ticketRepository.GetStatusTransitionsForSprintTicketsAsync(targetIds, ct);

        // 9. Bug ratio per sprint via BugRatioService.ComputeMultiSprint
        // Include one extra sprint beyond the target range for N+1 correlation
        var activeDevelopers = await developerRepository.GetActiveDevelopersAsync(ct);

        // For ComputeMultiSprint we need target sprints WITH memberships (all closed, for alert eval)
        // Load one sprint beyond target range for next-sprint bug ratio
        var extendedIds = targetIds.ToList();
        var lastTargetIndex = ascending.FindIndex(s => s.Id == targetQaSprints.Last().Id);
        Sprint? nextSprint = null;
        if (lastTargetIndex >= 0 && lastTargetIndex + 1 < ascending.Count)
        {
            nextSprint = ascending[lastTargetIndex + 1];
            extendedIds.Add(nextSprint.Id);
        }

        // Load all closed sprints with memberships for BugRatioService (alert evaluation needs all)
        var allLoadedSprints = await sprintRepository.GetSprintsWithMembershipsAsync(allSprintIds, ct);
        var allSortedSprints = allLoadedSprints.OrderBy(s => s.StartDate).ToList();

        // Get extended transitions (includes extra sprint if any)
        List<StatusTransition> bugRatioTransitions;
        if (nextSprint != null)
        {
            bugRatioTransitions = await ticketRepository.GetStatusTransitionsForSprintTicketsAsync(extendedIds, ct);
        }
        else
        {
            bugRatioTransitions = statusTransitions;
        }

        // Build target sprints for ComputeMultiSprint (extended with next sprint if available)
        var extendedTargetSprints = allSortedSprints
            .Where(s => extendedIds.Contains(s.Id))
            .OrderBy(s => s.StartDate)
            .ToList();

        // 10. Normalize sub-team
        var subTeam = string.IsNullOrWhiteSpace(req.SubTeam) ? null : req.SubTeam;

        var bugRatioResult = bugRatioService.ComputeMultiSprint(
            extendedTargetSprints, allSortedSprints, activeDevelopers,
            settings, bugRatioTransitions, subTeam);

        // Build bugRatioBySprintId from the per-sprint trend
        var bugRatioBySprintId = bugRatioResult.TeamMetrics.PerSprintTrend
            .ToDictionary(t => t.SprintId, t => t.BugRatioPercent);

        // 11. Call QaTrendsService
        var result = qaTrendsService.ComputeTrends(
            sortedLoaded,
            tesBySprintId,
            membershipsBySprintId,
            bugRatioBySprintId,
            statusTransitions,
            settings,
            subTeam);

        // 12. Return result
        await SendOkAsync(result, ct);
    }
}
