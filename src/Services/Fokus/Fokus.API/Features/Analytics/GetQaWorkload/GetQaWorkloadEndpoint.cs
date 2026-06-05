namespace Fokus.API.Features.Analytics.GetQaWorkload;

[HttpGet("/api/analytics/qa-workload")]
[Tags("Analytics")]
public class GetQaWorkloadEndpoint(
    SprintRepository sprintRepository,
    AppSettingsRepository appSettingsRepository,
    DeveloperRepository developerRepository,
    TestExecutionRepository testExecutionRepository,
    QaWorkloadService qaWorkloadService)
    : Endpoint<GetQaWorkloadRequest, QaWorkloadResponseDto>
{
    public override async Task HandleAsync(GetQaWorkloadRequest req, CancellationToken ct)
    {
        // 1. Load settings
        var settings = await appSettingsRepository.GetAsync(ct);

        // 2. If Xray disabled, return hasQaData = false
        if (!settings.XrayEnabled)
        {
            await SendOkAsync(new QaWorkloadResponseDto { HasQaData = false, Mode = "multi" }, ct);
            return;
        }

        // 3. Load analytics sprints — workload alert baseline and sparkline use closed sprints only
        var allSprints = await sprintRepository.GetAnalyticsSprintsAsync(ct);
        var ascending = allSprints.Where(s => s.State == SprintState.Closed).OrderBy(s => s.StartDate).ToList();

        if (ascending.Count == 0)
        {
            await SendOkAsync(new QaWorkloadResponseDto { HasQaData = false, Mode = "multi" }, ct);
            return;
        }

        // 4. Normalize sub-team
        var subTeam = string.IsNullOrWhiteSpace(req.SubTeam) ? null : req.SubTeam;

        // 5. Load all developers (not filtered by active — BR20: exclusion filter not applied)
        var allDevelopers = await developerRepository.GetAllAsync(ct);

        // 6. Determine mode
        if (req.SprintId.HasValue)
        {
            await HandleSingleSprintAsync(req, allSprints, ascending, allDevelopers, settings, subTeam, ct);
        }
        else
        {
            await HandleMultiSprintAsync(req, ascending, allDevelopers, settings, subTeam, ct);
        }
    }

    private async Task HandleMultiSprintAsync(
        GetQaWorkloadRequest req,
        List<Sprint> ascending,
        List<Developer> allDevelopers,
        AppSettings settings,
        string? subTeam,
        CancellationToken ct)
    {
        var last = req.Last ?? 5;
        var candidateIds = ascending.TakeLast(last).Select(s => s.Id).ToList();

        // Filter to sprints with QA data
        var withQaData = await testExecutionRepository.GetSprintIdsWithQaDataAsync(candidateIds, ct);
        var targetIds = candidateIds.Where(id => withQaData.Contains(id)).ToList();

        if (targetIds.Count == 0)
        {
            await SendOkAsync(new QaWorkloadResponseDto { HasQaData = false, Mode = "multi" }, ct);
            return;
        }

        // Load target sprints with memberships
        var targetSprints = await sprintRepository.GetSprintsWithMembershipsAsync(targetIds, ct);

        // Load TEs for each target sprint
        var tesBySprintId = new Dictionary<int, List<TestExecution>>();
        foreach (var sprintId in targetIds)
        {
            tesBySprintId[sprintId] = await testExecutionRepository.GetTestExecutionsForSprintAsync(sprintId, ct);
        }

        // For workload alert: load ALL closed sprints and TEs
        var allClosedIds = ascending.Select(s => s.Id).ToList();
        var allClosedSprints = await sprintRepository.GetSprintsWithMembershipsAsync(allClosedIds, ct);
        var allClosedTesBySprintId = new Dictionary<int, List<TestExecution>>();
        foreach (var sprintId in allClosedIds)
        {
            allClosedTesBySprintId[sprintId] = await testExecutionRepository.GetTestExecutionsForSprintAsync(sprintId, ct);
        }

        var result = qaWorkloadService.ComputeMultiSprint(
            targetSprints,
            tesBySprintId,
            allDevelopers,
            allClosedSprints,
            allClosedTesBySprintId,
            settings,
            subTeam);

        await SendOkAsync(MapMultiResponse(result), ct);
    }

    private async Task HandleSingleSprintAsync(
        GetQaWorkloadRequest req,
        List<Sprint> allSprints,
        List<Sprint> ascending,
        List<Developer> allDevelopers,
        AppSettings settings,
        string? subTeam,
        CancellationToken ct)
    {
        // Search allSprints so active sprints are accepted, not just closed ones
        var targetSprintInfo = allSprints.FirstOrDefault(s => s.Id == req.SprintId!.Value);
        if (targetSprintInfo is null)
        {
            AddError(r => r.SprintId, "Sprint not found.");
            await SendErrorsAsync(400, ct);
            return;
        }

        // Load target sprint with memberships
        var targetLoaded = await sprintRepository.GetSprintsWithMembershipsAsync([targetSprintInfo.Id], ct);
        var targetSprint = targetLoaded[0];

        // Load TEs for target sprint
        var targetTEs = await testExecutionRepository.GetTestExecutionsForSprintAsync(targetSprint.Id, ct);

        // Check if sprint has QA data
        var withQaData = await testExecutionRepository.GetSprintIdsWithQaDataAsync([targetSprint.Id], ct);
        if (!withQaData.Contains(targetSprint.Id))
        {
            await SendOkAsync(new QaWorkloadResponseDto { HasQaData = false, Mode = "single" }, ct);
            return;
        }

        // targetIndex is -1 when the selected sprint is active (not in closed ascending list)
        var targetIndex = ascending.FindIndex(s => s.Id == targetSprint.Id);

        // Prior sprint: last closed sprint before target (or last closed sprint when active)
        Sprint? priorSprint = null;
        List<TestExecution>? priorTEs = null;
        var priorIndex = targetIndex > 0 ? targetIndex - 1
            : targetIndex < 0 && ascending.Count > 0 ? ascending.Count - 1
            : -1;
        if (priorIndex >= 0)
        {
            var priorInfo = ascending[priorIndex];
            var priorWithQa = await testExecutionRepository.GetSprintIdsWithQaDataAsync([priorInfo.Id], ct);
            if (priorWithQa.Contains(priorInfo.Id))
            {
                var priorLoaded = await sprintRepository.GetSprintsWithMembershipsAsync([priorInfo.Id], ct);
                priorSprint = priorLoaded[0];
                priorTEs = await testExecutionRepository.GetTestExecutionsForSprintAsync(priorInfo.Id, ct);
            }
        }

        // Sparkline window: up to 4 trailing closed sprints with QA data
        // When active (targetIndex == -1), anchor at last closed sprint so window is non-empty
        var sparklineAnchorIndex = targetIndex >= 0 ? targetIndex : ascending.Count - 1;
        var candidateIds = ascending.Take(sparklineAnchorIndex + 1).Select(s => s.Id).ToList();
        var qaFilteredIds = (await testExecutionRepository.GetSprintIdsWithQaDataAsync(candidateIds, ct))
            .OrderBy(id => ascending.FindIndex(s => s.Id == id))
            .ToList();
        var sparklineIds = qaFilteredIds.TakeLast(4).ToList();
        var sparklineSprints = await sprintRepository.GetSprintsWithMembershipsAsync(sparklineIds, ct);
        var sparklineWindow = sparklineSprints.OrderBy(s => s.StartDate).ToList();

        var sparklineTEsBySprintId = new Dictionary<int, List<TestExecution>>();
        foreach (var sprint in sparklineWindow)
        {
            sparklineTEsBySprintId[sprint.Id] = await testExecutionRepository.GetTestExecutionsForSprintAsync(sprint.Id, ct);
        }

        // For workload alert: load ALL closed sprints and TEs
        var allClosedIds = ascending.Select(s => s.Id).ToList();
        var allClosedSprints = await sprintRepository.GetSprintsWithMembershipsAsync(allClosedIds, ct);
        var allClosedTesBySprintId = new Dictionary<int, List<TestExecution>>();
        foreach (var sprintId in allClosedIds)
        {
            // Reuse sparkline TEs if available
            if (sparklineTEsBySprintId.TryGetValue(sprintId, out var existing))
                allClosedTesBySprintId[sprintId] = existing;
            else
                allClosedTesBySprintId[sprintId] = await testExecutionRepository.GetTestExecutionsForSprintAsync(sprintId, ct);
        }

        var result = qaWorkloadService.ComputeSingleSprint(
            targetSprint,
            targetTEs,
            priorSprint,
            priorTEs,
            sparklineWindow,
            sparklineTEsBySprintId,
            allClosedSprints.OrderBy(s => s.StartDate).ToList(),
            allClosedTesBySprintId,
            allDevelopers,
            settings,
            subTeam);

        await SendOkAsync(MapSingleResponse(result), ct);
    }

    // --- Mapping ---

    private static QaWorkloadResponseDto MapMultiResponse(QaWorkloadMultiSprintResponse result) => new()
    {
        HasQaData = true,
        Mode = "multi",
        MultiSprint = new QaWorkloadMultiSprintDto
        {
            Sprints = result.Sprints.Select(MapSprintInfo).ToList(),
            TeamMetrics = new QaWorkloadTeamMetricsDto
            {
                TotalTes = result.TeamMetrics.TotalTes,
                TotalRunsCompleted = result.TeamMetrics.TotalRunsCompleted,
                PassCount = result.TeamMetrics.PassCount,
                FailCount = result.TeamMetrics.FailCount,
                TeamPassRate = result.TeamMetrics.TeamPassRate
            },
            Developers = result.Developers.Select(MapEntry).ToList()
        }
    };

    private static QaWorkloadResponseDto MapSingleResponse(QaWorkloadSingleSprintResponse result) => new()
    {
        HasQaData = true,
        Mode = "single",
        SingleSprint = new QaWorkloadSingleSprintDto
        {
            Sprint = MapSprintInfo(result.Sprint),
            TeamMetrics = new QaWorkloadSingleTeamMetricsDto
            {
                TotalTes = MapMetricCard(result.TeamMetrics.TotalTes),
                TotalRunsCompleted = MapMetricCard(result.TeamMetrics.TotalRunsCompleted),
                TeamPassRate = MapMetricCard(result.TeamMetrics.TeamPassRate)
            },
            Developers = result.Developers.Select(MapSingleEntry).ToList()
        }
    };

    private static QaWorkloadSprintInfoDto MapSprintInfo(SprintSummaryItem s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        StartDate = s.StartDate,
        EndDate = s.EndDate
    };

    private static WorkloadAlertDto MapAlert(WorkloadAlert alert) => new()
    {
        IsActive = alert.IsActive,
        ConsecutiveSprintCount = alert.ConsecutiveSprintCount,
        ThresholdPercent = alert.ThresholdPercent
    };

    private static QaWorkloadEntryDto MapEntry(QaWorkloadEntry e) => new()
    {
        AccountId = e.AccountId,
        DisplayName = e.DisplayName,
        SubTeam = e.SubTeam,
        AvatarUrl = e.AvatarUrl,
        TesOwned = e.TesOwned,
        RunsCompleted = e.RunsCompleted,
        PassCount = e.PassCount,
        FailCount = e.FailCount,
        PassRate = e.PassRate,
        StoriesCovered = e.StoriesCovered,
        BugsFound = e.BugsFound,
        SprintBreakdowns = e.SprintBreakdowns.Select(b => new QaWorkloadSprintBreakdownDto
        {
            SprintId = b.SprintId,
            SprintName = b.SprintName,
            TesOwned = b.TesOwned,
            RunsCompleted = b.RunsCompleted,
            PassCount = b.PassCount,
            FailCount = b.FailCount,
            PassRate = b.PassRate,
            StoriesCovered = b.StoriesCovered,
            BugsFound = b.BugsFound
        }).ToList(),
        WorkloadAlert = MapAlert(e.WorkloadAlert)
    };

    private static QaWorkloadSingleEntryDto MapSingleEntry(QaWorkloadSingleEntry e) => new()
    {
        AccountId = e.AccountId,
        DisplayName = e.DisplayName,
        SubTeam = e.SubTeam,
        AvatarUrl = e.AvatarUrl,
        TesOwned = e.TesOwned,
        RunsCompleted = e.RunsCompleted,
        PassCount = e.PassCount,
        FailCount = e.FailCount,
        PassRate = e.PassRate,
        StoriesCovered = e.StoriesCovered,
        BugsFound = e.BugsFound,
        TesOwnedDelta = e.TesOwnedDelta,
        TesOwnedDirection = e.TesOwnedDirection,
        TesOwnedPolarity = e.TesOwnedPolarity,
        RunsCompletedDelta = e.RunsCompletedDelta,
        RunsCompletedDirection = e.RunsCompletedDirection,
        RunsCompletedPolarity = e.RunsCompletedPolarity,
        PassCountDelta = e.PassCountDelta,
        PassCountDirection = e.PassCountDirection,
        PassCountPolarity = e.PassCountPolarity,
        FailCountDelta = e.FailCountDelta,
        FailCountDirection = e.FailCountDirection,
        FailCountPolarity = e.FailCountPolarity,
        PassRateDelta = e.PassRateDelta,
        PassRateDirection = e.PassRateDirection,
        PassRatePolarity = e.PassRatePolarity,
        StoriesCoveredDelta = e.StoriesCoveredDelta,
        StoriesCoveredDirection = e.StoriesCoveredDirection,
        StoriesCoveredPolarity = e.StoriesCoveredPolarity,
        BugsFoundDelta = e.BugsFoundDelta,
        BugsFoundDirection = e.BugsFoundDirection,
        BugsFoundPolarity = e.BugsFoundPolarity,
        WorkloadAlert = MapAlert(e.WorkloadAlert)
    };

    private static QaWorkloadMetricCardDto MapMetricCard(MetricCard card) => new()
    {
        Name = card.Name,
        Value = card.Value,
        DisplayValue = card.DisplayValue,
        Delta = card.Delta,
        DeltaDirection = card.DeltaDirection,
        DeltaPolarity = card.DeltaPolarity,
        Sparkline = card.Sparkline.Select(p => new SparklinePointDto
        {
            SprintName = p.SprintName,
            Value = p.Value
        }).ToList(),
        Rag = card.Rag
    };
}
