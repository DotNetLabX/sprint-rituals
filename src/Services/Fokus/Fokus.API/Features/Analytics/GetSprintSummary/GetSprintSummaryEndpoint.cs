using Fokus.Domain.Analytics;

namespace Fokus.API.Features.Analytics.GetSprintSummary;

[HttpGet("/api/analytics/sprint-summary")]
[Tags("Analytics")]
public class GetSprintSummaryEndpoint(
    SprintRepository sprintRepository,
    DeveloperRepository developerRepository,
    AppSettingsRepository appSettingsRepository,
    TicketRepository ticketRepository,
    SprintSummaryCalculator sprintSummaryCalculator,
    TestExecutionRepository testExecutionRepository,
    QaMetricsCalculator qaMetricsCalculator)
    : Endpoint<GetSprintSummaryQuery, SprintSummaryResponse>
{
    public override async Task HandleAsync(GetSprintSummaryQuery req, CancellationToken ct)
    {
        // 1. Load all analytics sprints (lightweight, no memberships)
        var sprints = await sprintRepository.GetAnalyticsSprintsAsync(ct);

        // 2. No sprints — return empty response
        if (sprints.Count == 0)
        {
            await Send.OkAsync(new SprintSummaryResponse(null, null, null, [], [], new FlagsResult([], null, [], false)), ct);
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
                await Send.ErrorsAsync(400, ct);
                return;
            }
            selectedSprintInfo = match;
        }
        else
        {
            selectedSprintInfo = sprints[0]; // most recent (sorted descending)
        }

        // 4. Build sparkline window: up to 4 closed sprints ending at selected, from ascending list
        var ascending = sprints.Where(s => s.State == SprintState.Closed).OrderBy(s => s.StartDate).ToList();
        var selectedIndex = ascending.FindIndex(s => s.Id == selectedSprintInfo.Id);
        var sparklineAnchorIndex = selectedIndex >= 0 ? selectedIndex : ascending.Count - 1;
        var windowIds = ascending
            .Take(sparklineAnchorIndex + 1)
            .TakeLast(4)
            .Select(s => s.Id)
            .ToList();

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
        QaBreakdownData? qualityBreakdown = null;
        Fokus.Domain.Analytics.TestingCrunchFlag? testingCrunch = null;
        var hasQaData = false;

        if (settings.Xray.Enabled)
        {
            var sprintTEs = await testExecutionRepository.GetTestExecutionsForSprintAsync(selectedSprint.Id, ct);
            hasQaData = true;

            var qaResult = qaMetricsCalculator.ComputeQaMetrics(
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
            qualityBreakdown = qaResult.QualityBreakdown;

            testingCrunch = TestTimelineCalculator.ComputeCrunchFlag(
                sprintTEs,
                selectedSprint.Memberships.ToList(),
                selectedSprint,
                subTeam);
        }

        // 12. Compute summary
        var result = sprintSummaryCalculator.ComputeSummary(
            selectedSprint, windowSprints, filteredActiveDevelopers, settings, subTeam, allEpicTickets,
            capacityLookup, allDevelopers, statusTransitions, excludedIds,
            qualitySubScore, qualityBreakdown, hasQaData, testingCrunch);

        await Send.OkAsync(MapResponse(result), ct);
    }

    private static SprintSummaryResponse MapResponse(SprintSummaryData d) => new(
        Sprint: d.Sprint is null ? null : new SprintInfo(
            d.Sprint.Id, d.Sprint.Name, d.Sprint.StartDate, d.Sprint.EndDate,
            d.Sprint.DurationDays, d.Sprint.SyncedAt),
        HealthScore: d.HealthScore is null ? null : new HealthScoreResult(
            d.HealthScore.CompositeScore, d.HealthScore.CompositeRag,
            d.HealthScore.CompletionSubScore, d.HealthScore.CompletionRag,
            d.HealthScore.DisruptionSubScore, d.HealthScore.DisruptionRag,
            d.HealthScore.CarryOverSubScore, d.HealthScore.CarryOverRag)
        {
            QualitySubScore = d.HealthScore.QualitySubScore,
            QualityRag = d.HealthScore.QualityRag,
            QualityBreakdown = d.HealthScore.QualityBreakdown is null ? null : new QualityBreakdownResult(
                d.HealthScore.QualityBreakdown.CoverageScore,
                d.HealthScore.QualityBreakdown.CoverageWeight,
                d.HealthScore.QualityBreakdown.PassRateScore,
                d.HealthScore.QualityBreakdown.PassRateWeight)
        },
        Metrics: d.Metrics is null ? null : new MetricsResult(
            MapMetricCard(d.Metrics.SpCompleted),
            MapMetricCard(d.Metrics.CompletionRate),
            MapMetricCard(d.Metrics.ScopeDisruptionRate),
            MapMetricCard(d.Metrics.BugDisruptionRate),
            MapMetricCard(d.Metrics.CarryOverRate),
            d.Metrics.BugSpCompleted),
        TopEpics: d.TopEpics.Select(e => new EpicProgress(
            e.EpicName, e.SpCompletedThisSprint, e.TotalSp, e.DoneSp, e.CompletionPercentage)).ToList(),
        Leaderboard: d.Leaderboard.Select(dev => new DeveloperSummary(
            dev.DisplayName, dev.AvatarUrl, dev.SubTeam, dev.SpCompleted,
            dev.FeatureSp, dev.BugSp, dev.FeatureTickets, dev.BugTickets, dev.CapacityPercent)).ToList(),
        Flags: MapFlags(d.Flags));

    private static FlagsResult MapFlags(SprintFlagsData f) => new(
        ZombieTickets: f.ZombieTickets.Select(z => new ZombieTicket(z.TicketKey, z.Summary, z.SprintCount, z.AssigneeName)).ToList(),
        MidSprintDisruption: f.MidSprintDisruption is null ? null : new MidSprintDisruption(f.MidSprintDisruption.TotalSp, f.MidSprintDisruption.TicketCount),
        ZeroSpDevelopers: f.ZeroSpDevelopers,
        HasAnyFlags: f.HasAnyFlags)
    {
        TestingCrunch = f.TestingCrunch is null ? null : new TestingCrunchResult(
            f.TestingCrunch.CrunchPercentage, f.TestingCrunch.CrunchRunCount, f.TestingCrunch.TotalRunCount)
    };

    private static MetricCard MapMetricCard(SprintMetricCardData c) =>
        new(c.Name, c.Value, c.DisplayValue, c.Delta, c.DeltaDirection, c.DeltaPolarity,
            c.Sparkline.Select(p => new SparklinePoint(p.SprintName, p.Value)).ToList())
        { Rag = c.Rag };
}
