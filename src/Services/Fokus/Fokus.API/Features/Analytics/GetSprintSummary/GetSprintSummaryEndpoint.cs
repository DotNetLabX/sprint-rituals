namespace Fokus.API.Features.Analytics.GetSprintSummary;

[HttpGet("/api/analytics/sprint-summary")]
[Tags("Analytics")]
public class GetSprintSummaryEndpoint(
    SprintRepository sprintRepository,
    DeveloperRepository developerRepository,
    AppSettingsRepository appSettingsRepository,
    TicketRepository ticketRepository,
    SprintSummaryService sprintSummaryService,
    TestExecutionRepository testExecutionRepository,
    QaMetricsService qaMetricsService)
    : Endpoint<GetSprintSummaryRequest, SprintSummaryResponse>
{
    public override async Task HandleAsync(GetSprintSummaryRequest req, CancellationToken ct)
    {
        // 1. Load all analytics sprints (lightweight, no memberships)
        var sprints = await sprintRepository.GetAnalyticsSprintsAsync(ct);

        // 2. No sprints — return empty response
        if (sprints.Count == 0)
        {
            await SendOkAsync(new SprintSummaryResponse(null, null, null, [], [], new FlagsResult([], null, [], false)), ct);
            return;
        }

        // 3. Determine selected sprint
        Sprint selectedSprintInfo;
        if (req.SprintId.HasValue)
        {
            var match = sprints.FirstOrDefault(s => s.Id == req.SprintId.Value);
            if (match is null)
            {
                AddError(r => r.SprintId, "Sprint not found.");
                await SendErrorsAsync(400, ct);
                return;
            }
            selectedSprintInfo = match;
        }
        else
        {
            selectedSprintInfo = sprints[0]; // most recent (sorted descending)
        }

        // 4. Build sparkline window: up to 4 closed sprints ending at selected, from ascending list
        // Sparklines use closed sprints only — active sprint is not a data point
        var ascending = sprints.Where(s => s.State == SprintState.Closed).OrderBy(s => s.StartDate).ToList();
        var selectedIndex = ascending.FindIndex(s => s.Id == selectedSprintInfo.Id);
        // If selected is active (not in closed list), use last closed sprint as anchor for sparkline
        var sparklineAnchorIndex = selectedIndex >= 0 ? selectedIndex : ascending.Count - 1;
        var windowIds = ascending
            .Take(sparklineAnchorIndex + 1)
            .TakeLast(4)
            .Select(s => s.Id)
            .ToList();

        // Always include the selected sprint even when it's active (not in the closed list)
        if (!windowIds.Contains(selectedSprintInfo.Id))
            windowIds.Add(selectedSprintInfo.Id);

        // 5. Bulk load sprints with memberships for the window
        var windowSprints = await sprintRepository.GetSprintsWithMembershipsAsync(windowIds, ct);

        // 6. Load active developers and app settings
        var activeDevelopers = await developerRepository.GetActiveDevelopersAsync(ct);
        var allDevelopers = await developerRepository.GetAllAsync(ct);
        var settings = await appSettingsRepository.GetAsync(ct);

        // 7. Normalize sub-team (empty string -> null)
        var subTeam = string.IsNullOrWhiteSpace(req.SubTeam) ? null : req.SubTeam;

        // 8. Load capacity records for the window sprints
        var capacityRecords = await developerRepository.GetCapacitiesForSprintsAsync(windowIds, ct);

        // 8a. Build capacity lookup: accountId -> sprintId -> capacityPercent
        var capacityLookup = capacityRecords
            .GroupBy(c => c.DeveloperAccountId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(c => c.SprintId, c => c.CapacityPercent));

        // 8b. Load status transitions for all window sprint tickets
        var statusTransitions = await ticketRepository.GetStatusTransitionsForSprintTicketsAsync(windowIds, ct);

        // 9. Apply cross-cutting exclusion for the selected sprint
        var selectedSprint = windowSprints.First(s => s.Id == selectedSprintInfo.Id);
        var excludedIds = ExcludedDeveloperFilter.GetExcludedDeveloperIds(
            selectedSprint, allDevelopers, capacityRecords, statusTransitions, settings);
        var filteredActiveDevelopers = activeDevelopers.Where(d => !excludedIds.Contains(d.Id)).ToList();

        // 10. Load all epic tickets for F8/F14 alignment (BR20)
        var allEpicTickets = await ticketRepository.GetTicketsWithEpicAsync(ct);

        // 11. Load QA data and compute quality sub-score when Xray is enabled (BR19)
        decimal? qualitySubScore = null;
        QualityBreakdownResult? qualityBreakdown = null;
        TestingCrunchFlag? testingCrunch = null;
        var hasQaData = false;

        if (settings.XrayEnabled)
        {
            var sprintTEs = await testExecutionRepository.GetTestExecutionsForSprintAsync(selectedSprint.Id, ct);
            hasQaData = true;

            var qaResult = qaMetricsService.ComputeQaMetrics(
                sprintTEs,
                selectedSprint.Memberships.ToList(),
                statusTransitions,
                selectedSprint,
                priorSprint: null,
                priorSprintTEs: null,
                priorMemberships: null,
                sparklineWindow: [],
                sparklineTEsBySprintId: [],
                sparklineMembershipsBySprintId: [],
                settings,
                subTeam);

            qualitySubScore = qaResult.QualitySubScore;
            qualityBreakdown = new QualityBreakdownResult(
                qaResult.QualityBreakdown.CoverageScore,
                qaResult.QualityBreakdown.CoverageWeight,
                qaResult.QualityBreakdown.PassRateScore,
                qaResult.QualityBreakdown.PassRateWeight);

            testingCrunch = TestTimelineService.ComputeCrunchFlag(
                sprintTEs,
                selectedSprint.Memberships.ToList(),
                selectedSprint,
                subTeam);
        }

        // 12. Compute summary (exclusion applied internally via excludedIds)
        var result = sprintSummaryService.ComputeSummary(selectedSprint, windowSprints, filteredActiveDevelopers, settings, subTeam, allEpicTickets, capacityLookup, allDevelopers, statusTransitions, excludedIds, qualitySubScore, qualityBreakdown, hasQaData, testingCrunch);

        await SendOkAsync(result, ct);
    }
}
