using Fokus.API.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Fokus.API.Features.Sync.SyncSprints;

[HttpPost("/api/sync/sprints")]
[Tags("Sync")]
[Authorize(Roles = "Admin")]
public class SyncSprintsEndpoint(
    IJiraClient jiraClient,
    AppSettingsRepository settingsRepository,
    SprintIssueSyncService syncService,
    IHubContext<SprintHub> hubContext,
    ILogger<SyncSprintsEndpoint> logger)
    : Endpoint<SyncSprintsCommand, SyncSprintsResponse>
{
    public override async Task HandleAsync(SyncSprintsCommand command, CancellationToken ct)
    {
        var settings = await settingsRepository.GetAsync(ct);
        if (settings.BoardId is null)
            throw new BadRequestException("BoardId is not configured in settings.");

        var boardId = settings.BoardId.Value;

        var allSprints = await jiraClient.GetSprintsAsync(boardId, ct, SprintState.Active, SprintState.Closed);
        var ordered = allSprints.OrderBy(s => s.StartDate).ToList();

        var fromSprint = ordered.FirstOrDefault(s => s.Id == command.FromSprintId)
            ?? throw new NotFoundException("FromSprintId was not found in the Jira board's started sprints.");
        var toSprint = ordered.FirstOrDefault(s => s.Id == command.ToSprintId)
            ?? throw new NotFoundException("ToSprintId was not found in the Jira board's started sprints.");

        if (fromSprint.StartDate > toSprint.StartDate)
            throw new BadRequestException("FromSprintId must have an earlier or equal start date than ToSprintId.");

        var sprintsInRange = ordered
            .Where(s => s.StartDate >= fromSprint.StartDate && s.StartDate <= toSprint.StartDate)
            .ToList();

        var result = await syncService.SyncSprintsFromJiraAsync(
            sprintsInRange, $"Board {boardId}", forcedNotCommitted: false, ct);

        // Broadcast SprintSynced after successful sync — failure must not break the sync response
        try
        {
            await hubContext.Clients.All.SendAsync(
                SprintHubMethods.SprintSynced,
                new { SprintIds = sprintsInRange.Select(s => s.Id).ToArray() },
                ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SignalR broadcast failed after sprint sync — sync result is unaffected.");
        }

        XraySyncSummary? xray = null;
        if (result.XrayTestExecutionsSynced > 0 || result.XrayTestRunsSynced > 0 || result.XrayTestSetsSynced > 0 || (result.XrayWarnings?.Count > 0))
        {
            xray = new XraySyncSummary
            {
                TestExecutionsSynced = result.XrayTestExecutionsSynced,
                TestRunsSynced = result.XrayTestRunsSynced,
                TestSetsSynced = result.XrayTestSetsSynced,
                Warnings = result.XrayWarnings?.ToArray() ?? []
            };
        }

        await SendOkAsync(new SyncSprintsResponse
        {
            SprintsAttempted = sprintsInRange.Count,
            SprintsSynced = result.SprintsSynced,
            TicketsUpserted = result.TicketsUpserted,
            DevelopersDiscovered = result.DevelopersDiscovered,
            Failures = result.Failures,
            Xray = xray
        }, ct);
    }
}
