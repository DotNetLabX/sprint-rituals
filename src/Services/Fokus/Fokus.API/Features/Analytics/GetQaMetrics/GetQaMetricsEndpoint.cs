namespace Fokus.API.Features.Analytics.GetQaMetrics;

[HttpGet("/api/sprints/{sprintId}/qa-metrics")]
[Tags("Analytics")]
public class GetQaMetricsEndpoint(
    SprintRepository sprintRepository,
    AppSettingsRepository appSettingsRepository,
    TestExecutionRepository testExecutionRepository,
    TicketRepository ticketRepository,
    QaMetricsService qaMetricsService)
    : Endpoint<GetQaMetricsRequest, QaMetricsResponse>
{
    public override async Task HandleAsync(GetQaMetricsRequest req, CancellationToken ct)
    {
        // 1. Load settings
        var settings = await appSettingsRepository.GetAsync(ct);

        // 2. Load all analytics sprints (lightweight)
        var sprints = await sprintRepository.GetAnalyticsSprintsAsync(ct);

        // 3. Find the requested sprint
        var sprintInfo = sprints.FirstOrDefault(s => s.Id == req.SprintId);
        if (sprintInfo is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        // 4. If Xray disabled, return hasQaData=false with empty metrics
        if (!settings.XrayEnabled)
        {
            await SendOkAsync(BuildEmptyResponse(), ct);
            return;
        }

        // 5. Build sparkline window: up to 4 closed sprints ending at selected (ascending)
        // Sparklines use closed sprints only — active sprint is not a data point
        var closedAscending = sprints.Where(s => s.State == SprintState.Closed).OrderBy(s => s.StartDate).ToList();
        var closedIndex = closedAscending.FindIndex(s => s.Id == req.SprintId);
        // If selected is active (not in closed list), anchor sparkline at last closed sprint
        var sparklineAnchorIndex = closedIndex >= 0 ? closedIndex : closedAscending.Count - 1;
        var windowIds = closedAscending
            .Take(sparklineAnchorIndex + 1)
            .TakeLast(4)
            .Select(s => s.Id)
            .ToList();

        // 6. Bulk load sprints with memberships for the window
        var windowSprints = await sprintRepository.GetSprintsWithMembershipsAsync(windowIds, ct);

        // Active sprint is not in the closed window — load it separately
        Sprint selectedSprint;
        if (closedIndex >= 0)
        {
            selectedSprint = windowSprints.First(s => s.Id == req.SprintId);
        }
        else
        {
            var activeLoaded = await sprintRepository.GetSprintsWithMembershipsAsync([req.SprintId], ct);
            selectedSprint = activeLoaded[0];
        }

        // 7. Normalize sub-team
        var subTeam = string.IsNullOrWhiteSpace(req.SubTeam) ? null : req.SubTeam;

        // 8. Load status transitions for window sprint tickets (active sprint ticket transitions loaded separately)
        var transitionIds = closedIndex >= 0 ? windowIds : [.. windowIds, req.SprintId];
        var statusTransitions = await ticketRepository.GetStatusTransitionsForSprintTicketsAsync(transitionIds, ct);

        // 9. Load TEs for the selected sprint
        var sprintTEs = await testExecutionRepository.GetTestExecutionsForSprintAsync(req.SprintId, ct);

        // 10. Determine prior sprint and prior data
        // For active sprint: prior is the last closed sprint in the window (if any)
        var sortedWindow = windowSprints.OrderBy(s => s.StartDate).ToList();
        var selectedWindowIndex = sortedWindow.FindIndex(s => s.Id == req.SprintId);
        // Active sprint is not in sortedWindow — prior = last closed sprint in window
        var priorSprint = selectedWindowIndex > 0
            ? sortedWindow[selectedWindowIndex - 1]
            : (closedIndex < 0 && sortedWindow.Count > 0 ? sortedWindow[^1] : null);

        List<TestExecution>? priorTEs = null;
        List<SprintMembership>? priorMemberships = null;
        if (priorSprint is not null)
        {
            priorTEs = await testExecutionRepository.GetTestExecutionsForSprintAsync(priorSprint.Id, ct);
            priorMemberships = priorSprint.Memberships.ToList();
        }

        // 11. Build sparkline TE and membership lookups for the window (closed sprints only)
        // For active sprint selectedWindowIndex == -1, so Take(-1 + 1) = Take(0) would be empty.
        // Use the full sortedWindow (all closed window sprints) as the sparkline base instead.
        var sparklineBase = closedIndex >= 0
            ? sortedWindow.Take(selectedWindowIndex + 1).TakeLast(4).ToList()
            : sortedWindow.TakeLast(4).ToList();
        var sparklineWindow = sparklineBase;

        var sparklineTEsBySprintId = new Dictionary<int, List<TestExecution>>();
        foreach (var sprint in sparklineWindow)
        {
            sparklineTEsBySprintId[sprint.Id] = await testExecutionRepository.GetTestExecutionsForSprintAsync(sprint.Id, ct);
        }

        var sparklineMembershipsBySprintId = sparklineWindow
            .ToDictionary(s => s.Id, s => s.Memberships.ToList());

        // 12. Compute QA metrics
        var result = qaMetricsService.ComputeQaMetrics(
            sprintTEs,
            selectedSprint.Memberships.ToList(),
            statusTransitions,
            selectedSprint,
            priorSprint,
            priorTEs,
            priorMemberships,
            sparklineWindow,
            sparklineTEsBySprintId,
            sparklineMembershipsBySprintId,
            settings,
            subTeam);

        await SendOkAsync(MapResponse(result), ct);
    }

    private static QaMetricsResponse BuildEmptyResponse() => new()
    {
        HasQaData = false,
        CoverageRate = null,
        ExecutionRate = null,
        PassRate = null,
        BugsFound = 0,
        QualitySubScore = 0,
        QualityBreakdown = null,
        UntestedCount = 0,
        FailingCount = 0
    };

    private static QaMetricsResponse MapResponse(QaMetricsResult result) => new()
    {
        HasQaData = result.HasQaData,
        CoverageRate = result.CoverageRate,
        ExecutionRate = result.ExecutionRate,
        PassRate = result.PassRate,
        BugsFound = result.BugsFound,
        QualitySubScore = result.QualitySubScore,
        QualityBreakdown = result.QualityBreakdown is null ? null : new QualityBreakdownResponse
        {
            CoverageScore = result.QualityBreakdown.CoverageScore,
            CoverageWeight = result.QualityBreakdown.CoverageWeight,
            PassRateScore = result.QualityBreakdown.PassRateScore,
            PassRateWeight = result.QualityBreakdown.PassRateWeight
        },
        UntestedCount = result.UntestedCount,
        FailingCount = result.FailingCount
    };
}
