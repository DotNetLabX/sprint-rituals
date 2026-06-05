namespace Fokus.API.Features.Sync.SyncBacklogSprints;

[HttpPost("/api/sync/backlog")]
[Tags("Sync")]
[Authorize(Roles = "Admin")]
public class SyncBacklogSprintsEndpoint(
    IJiraClient jiraClient,
    AppSettingsRepository settingsRepository,
    SprintIssueSyncService syncService)
    : EndpointWithoutRequest<SyncBacklogSprintsResponse>
{
    public override async Task HandleAsync(CancellationToken ct)
    {
        var settings = await settingsRepository.GetAsync(ct);
        if (settings.BoardId is null)
            throw new BadRequestException("BoardId is not configured in settings.");

        var boardId = settings.BoardId.Value;
        var boardName = $"Board {boardId}";

        var futureSprints = await jiraClient.GetSprintsAsync(boardId, ct, SprintState.Future);

        var sprintResult = await syncService.SyncSprintsFromJiraAsync(
            futureSprints, boardName, forcedNotCommitted: true, ct);

        var epicResult = await syncService.SyncEpicDiscoveryAsync(ct);

        SyncSprints.XraySyncSummary? xray = null;
        if (sprintResult.XrayTestExecutionsSynced > 0 || sprintResult.XrayTestRunsSynced > 0 || sprintResult.XrayTestSetsSynced > 0 || (sprintResult.XrayWarnings?.Count > 0))
        {
            xray = new SyncSprints.XraySyncSummary
            {
                TestExecutionsSynced = sprintResult.XrayTestExecutionsSynced,
                TestRunsSynced = sprintResult.XrayTestRunsSynced,
                TestSetsSynced = sprintResult.XrayTestSetsSynced,
                Warnings = sprintResult.XrayWarnings?.ToArray() ?? []
            };
        }

        await SendOkAsync(new SyncBacklogSprintsResponse
        {
            BacklogSprintsSynced = sprintResult.SprintsSynced,
            EpicTicketsDiscovered = epicResult.TicketsDiscovered,
            SprintFailures = sprintResult.Failures.Count,
            EpicFailures = epicResult.Failures,
            Xray = xray
        }, ct);
    }
}
