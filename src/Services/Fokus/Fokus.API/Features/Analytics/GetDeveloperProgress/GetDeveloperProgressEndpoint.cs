namespace Fokus.API.Features.Analytics.GetDeveloperProgress;

[HttpGet("/api/analytics/developer-progress")]
[Tags("Analytics")]
public class GetDeveloperProgressEndpoint(
    SprintRepository sprintRepository,
    DeveloperRepository developerRepository,
    AppSettingsRepository appSettingsRepository,
    TicketRepository ticketRepository,
    DeveloperProgressService developerProgressService)
    : Endpoint<GetDeveloperProgressRequest, DeveloperProgressResponse>
{
    public override async Task HandleAsync(GetDeveloperProgressRequest req, CancellationToken ct)
    {
        // 1. Load analytics sprints (active + closed)
        var allSprints = await sprintRepository.GetAnalyticsSprintsAsync(ct);

        // 2. Find the active sprint
        var activeSprint = allSprints.FirstOrDefault(s => s.State == SprintState.Active);

        // 3. No active sprint — return early with empty response
        if (activeSprint is null)
        {
            await SendOkAsync(new DeveloperProgressResponse(false, null, 0, 0, false, [], []), ct);
            return;
        }

        // 4. Load the active sprint with memberships
        var sprintWithMemberships = await sprintRepository.GetSprintWithMembershipsAsync(activeSprint.Id, ct);
        if (sprintWithMemberships is null)
        {
            await SendOkAsync(new DeveloperProgressResponse(false, null, 0, 0, false, [], []), ct);
            return;
        }

        // 5. Bulk load supporting data
        var activeDevelopers = await developerRepository.GetActiveDevelopersAsync(ct);
        var allDevelopers = await developerRepository.GetAllAsync(ct);
        var capacityRecords = await developerRepository.GetCapacitiesForSprintsAsync([activeSprint.Id], ct);
        var settings = await appSettingsRepository.GetAsync(ct);
        var statusTransitions = await ticketRepository.GetStatusTransitionsForSprintTicketsAsync([activeSprint.Id], ct);

        // 6. Compute excluded developer IDs
        var excludedDeveloperIds = ExcludedDeveloperFilter.GetExcludedDeveloperIds(
            sprintWithMemberships, allDevelopers, capacityRecords, statusTransitions, settings);

        // 7. Normalize sub-team
        var subTeam = string.IsNullOrWhiteSpace(req.SubTeam) ? null : req.SubTeam;

        // 8. Compute and return
        var result = developerProgressService.ComputeProgress(
            sprintWithMemberships,
            activeDevelopers,
            capacityRecords,
            settings,
            subTeam,
            allDevelopers,
            statusTransitions,
            excludedDeveloperIds);

        await SendOkAsync(result, ct);
    }
}
