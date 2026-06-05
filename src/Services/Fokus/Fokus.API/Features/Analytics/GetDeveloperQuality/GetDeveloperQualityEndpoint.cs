namespace Fokus.API.Features.Analytics.GetDeveloperQuality;

[HttpGet("/api/analytics/developer-quality")]
[Tags("Analytics")]
public class GetDeveloperQualityEndpoint(
    SprintRepository sprintRepository,
    DeveloperRepository developerRepository,
    AppSettingsRepository appSettingsRepository,
    TicketRepository ticketRepository,
    TestExecutionRepository testExecutionRepository,
    DeveloperQualityService developerQualityService)
    : Endpoint<GetDeveloperQualityRequest, DeveloperQualityResponse>
{
    public override async Task HandleAsync(GetDeveloperQualityRequest req, CancellationToken ct)
    {
        // 1. Load settings
        var settings = await appSettingsRepository.GetAsync(ct);

        // 2. If Xray disabled, return empty response
        if (!settings.XrayEnabled)
        {
            await SendOkAsync(new DeveloperQualityResponse { HasQaData = false, Sprints = [], Developers = [] }, ct);
            return;
        }

        // 3. Load analytics sprints — multi-sprint averaging, sparkline, and streak use closed sprints only
        var allSprints = await sprintRepository.GetAnalyticsSprintsAsync(ct);
        var ascending = allSprints.Where(s => s.State == SprintState.Closed).OrderBy(s => s.StartDate).ToList();

        if (ascending.Count == 0)
        {
            await SendOkAsync(new DeveloperQualityResponse { HasQaData = false, Sprints = [], Developers = [] }, ct);
            return;
        }

        // 4. Determine target sprint IDs based on mode
        var isSingleSprint = req.SprintId.HasValue;
        List<int> targetSprintIds;

        if (req.SprintId.HasValue)
        {
            // Single sprint — accept active or closed
            var match = allSprints.FirstOrDefault(s => s.Id == req.SprintId.Value);
            if (match is null)
            {
                AddError(r => r.SprintId, "Sprint not found.");
                await SendErrorsAsync(400, ct);
                return;
            }
            targetSprintIds = [match.Id];
        }
        else if (req.Last.HasValue)
        {
            // Multi-sprint: last N closed sprints, filtered to only those with QA data
            var lastN = ascending.TakeLast(req.Last.Value).Select(s => s.Id).ToList();
            var withQaData = await testExecutionRepository.GetSprintIdsWithQaDataAsync(lastN, ct);
            targetSprintIds = lastN.Where(id => withQaData.Contains(id)).ToList();
        }
        else
        {
            // All closed sprints, filtered to only those with QA data
            var allIds = ascending.Select(s => s.Id).ToList();
            var withQaData = await testExecutionRepository.GetSprintIdsWithQaDataAsync(allIds, ct);
            targetSprintIds = allIds.Where(id => withQaData.Contains(id)).ToList();
        }

        if (targetSprintIds.Count == 0)
        {
            await SendOkAsync(new DeveloperQualityResponse { HasQaData = false, Sprints = [], Developers = [] }, ct);
            return;
        }

        // 5. Normalize sub-team
        var subTeam = string.IsNullOrWhiteSpace(req.SubTeam) ? null : req.SubTeam;

        // 6. Determine sparkline window and streak window (single-sprint only)
        var sparklineWindow = new List<Sprint>();
        var sparklineTEsBySprintId = new Dictionary<int, List<TestExecution>>();
        var streakWindow = new List<Sprint>();
        var streakTEsBySprintId = new Dictionary<int, List<TestExecution>>();
        Sprint? priorSprint = null;
        List<TestExecution>? priorSprintTEs = null;

        if (isSingleSprint)
        {
            var targetSprintId = targetSprintIds[0];
            var targetIndex = ascending.FindIndex(s => s.Id == targetSprintId);
            // When active sprint is selected, it is not in the closed ascending list (targetIndex == -1).
            // Use ascending.Count as the anchor so all closed sprints are candidates for sparkline/streak.
            var closedAnchorIndex = targetIndex >= 0 ? targetIndex : ascending.Count - 1;

            // Find prior sprint (last closed sprint before target)
            if (closedAnchorIndex > 0 || (targetIndex < 0 && ascending.Count > 0))
            {
                var priorSprintInfo = targetIndex >= 0
                    ? ascending[targetIndex - 1]
                    : ascending[ascending.Count - 1];
                // Load prior sprint with memberships
                var priorLoaded = await sprintRepository.GetSprintsWithMembershipsAsync([priorSprintInfo.Id], ct);
                if (priorLoaded.Count > 0)
                {
                    priorSprint = priorLoaded[0];
                    priorSprintTEs = await testExecutionRepository.GetTestExecutionsForSprintAsync(priorSprint.Id, ct);
                    // Only provide prior data if it has QA data
                    if (priorSprintTEs.Count == 0)
                    {
                        priorSprint = null;
                        priorSprintTEs = null;
                    }
                }
            }

            // All candidate sprint IDs up to and including the closed anchor (ascending order)
            var candidateIds = ascending.Take(closedAnchorIndex + 1).Select(s => s.Id).ToList();

            // Filter to sprints with QA data (BR8 semantics) — used for both sparkline and streak
            var withQaData = await testExecutionRepository.GetSprintIdsWithQaDataAsync(candidateIds, ct);
            var qaFilteredIds = candidateIds.Where(id => withQaData.Contains(id)).ToList();

            // Sparkline: up to 4 trailing sprints with QA data ending at target
            var sparklineIds = qaFilteredIds.TakeLast(4).ToList();
            var sparklineSprints = await sprintRepository.GetSprintsWithMembershipsAsync(sparklineIds, ct);
            sparklineWindow = sparklineSprints.OrderBy(s => s.StartDate).ToList();

            foreach (var sprint in sparklineWindow)
            {
                sparklineTEsBySprintId[sprint.Id] = await testExecutionRepository.GetTestExecutionsForSprintAsync(sprint.Id, ct);
            }

            // Streak window: up to 12 trailing sprints with QA data ending at target (BR15).
            // Capped at 12 to bound DB load while covering realistic streak lengths.
            var streakIds = qaFilteredIds.TakeLast(12).ToList();
            var streakSprints = await sprintRepository.GetSprintsWithMembershipsAsync(streakIds, ct);
            streakWindow = streakSprints.OrderBy(s => s.StartDate).ToList();

            foreach (var sprint in streakWindow)
            {
                // Reuse already-loaded TEs from the sparkline window where available
                if (sparklineTEsBySprintId.TryGetValue(sprint.Id, out var existingTEs))
                    streakTEsBySprintId[sprint.Id] = existingTEs;
                else
                    streakTEsBySprintId[sprint.Id] = await testExecutionRepository.GetTestExecutionsForSprintAsync(sprint.Id, ct);
            }
        }

        // 7. Bulk load target sprints with memberships
        var allSprintIdsToLoad = targetSprintIds.ToList();
        var loadedSprints = await sprintRepository.GetSprintsWithMembershipsAsync(allSprintIdsToLoad, ct);

        // 8. Load active developers
        var activeDevelopers = await developerRepository.GetActiveDevelopersAsync(ct);

        // 9. Load status transitions for all relevant sprints (target + sparkline + prior + streak)
        var transitionSprintIds = allSprintIdsToLoad
            .Concat(sparklineWindow.Select(s => s.Id))
            .Concat(priorSprint is not null ? [priorSprint.Id] : Array.Empty<int>())
            .Concat(streakWindow.Select(s => s.Id))
            .Distinct()
            .ToList();
        var statusTransitions = await ticketRepository.GetStatusTransitionsForSprintTicketsAsync(transitionSprintIds, ct);

        // 10. Bulk load TEs for target sprints
        var tesBySprintId = new Dictionary<int, List<TestExecution>>();
        foreach (var sprintId in targetSprintIds)
        {
            tesBySprintId[sprintId] = await testExecutionRepository.GetTestExecutionsForSprintAsync(sprintId, ct);
        }

        // 11. Compute developer quality
        // allLoadedSprints must contain all sprints whose memberships the service may access:
        // target sprints, sparkline window, prior sprint, and streak window.
        var allLoadedSprints = loadedSprints
            .Concat(sparklineWindow.Where(s => !loadedSprints.Any(ls => ls.Id == s.Id)))
            .Concat(priorSprint is not null && !loadedSprints.Any(ls => ls.Id == priorSprint.Id) ? [priorSprint] : [])
            .Concat(streakWindow.Where(s => !loadedSprints.Any(ls => ls.Id == s.Id)))
            .GroupBy(s => s.Id)
            .Select(g => g.First())
            .ToList();

        // Merge sparkline TEs into the TE lookup for service (service will look up by sprint ID)
        var allTesBySprintId = new Dictionary<int, List<TestExecution>>(tesBySprintId);

        var result = developerQualityService.ComputeDeveloperQuality(
            allLoadedSprints,
            targetSprintIds,
            activeDevelopers,
            allTesBySprintId,
            statusTransitions,
            settings,
            subTeam,
            isSingleSprint,
            priorSprint,
            priorSprintTEs,
            sparklineWindow,
            sparklineTEsBySprintId,
            streakWindow,
            streakTEsBySprintId);

        // 12. Map result to response
        await SendOkAsync(MapResponse(result), ct);
    }

    private static DeveloperQualityResponse MapResponse(DeveloperQualityResult result) => new()
    {
        HasQaData = result.HasQaData,
        Sprints = result.Sprints.Select(s => new DeveloperQualitySprintInfoResponse
        {
            Id = s.Id,
            Name = s.Name,
            StartDate = s.StartDate,
            EndDate = s.EndDate
        }).ToList(),
        Developers = result.Developers.Select(d => new DeveloperQualityEntryResponse
        {
            AccountId = d.AccountId,
            DisplayName = d.DisplayName,
            SubTeam = d.SubTeam,
            AvatarUrl = d.AvatarUrl,
            BelowMedianStreak = d.BelowMedianStreak,
            SprintBreakdowns = d.SprintBreakdowns.Select(b => new DeveloperQualitySprintBreakdownResponse
            {
                SprintId = b.SprintId,
                Stories = b.Stories,
                Covered = b.Covered,
                CoveragePercent = b.CoveragePercent,
                PassRatePercent = b.PassRatePercent,
                Untested = b.Untested,
                BugsFound = b.BugsFound,
                CoverageRag = b.CoverageRag,
                PassRateRag = b.PassRateRag,
                CoveragePercentDelta = b.CoveragePercentDelta,
                CoveragePercentDeltaDirection = b.CoveragePercentDeltaDirection,
                CoveragePercentDeltaPolarity = b.CoveragePercentDeltaPolarity,
                PassRatePercentDelta = b.PassRatePercentDelta,
                PassRatePercentDeltaDirection = b.PassRatePercentDeltaDirection,
                PassRatePercentDeltaPolarity = b.PassRatePercentDeltaPolarity,
                BugsFoundDelta = b.BugsFoundDelta,
                BugsFoundDeltaDirection = b.BugsFoundDeltaDirection,
                BugsFoundDeltaPolarity = b.BugsFoundDeltaPolarity,
                CoverageSparkline = b.CoverageSparkline?.Select(p => new SparklinePointResponse
                {
                    SprintName = p.SprintName,
                    Value = p.Value
                }).ToList(),
                PassRateSparkline = b.PassRateSparkline?.Select(p => new SparklinePointResponse
                {
                    SprintName = p.SprintName,
                    Value = p.Value
                }).ToList()
            }).ToList()
        }).ToList()
    };
}
