namespace Fokus.API.Features.Analytics.GetDeveloperThroughput;

[HttpGet("/api/analytics/developer-throughput")]
[Tags("Analytics")]
public class GetDeveloperThroughputEndpoint(
    SprintRepository sprintRepository,
    DeveloperRepository developerRepository,
    AppSettingsRepository appSettingsRepository,
    TicketRepository ticketRepository,
    DeveloperThroughputService developerThroughputService)
    : Endpoint<GetDeveloperThroughputRequest, DeveloperThroughputResponse>
{
    public override async Task HandleAsync(GetDeveloperThroughputRequest req, CancellationToken ct)
    {
        // 1. Load analytics sprints — last-N and rolling average window use closed sprints only
        var analyticsSprints = await sprintRepository.GetAnalyticsSprintsAsync(ct);
        var ascending = analyticsSprints.Where(s => s.State == SprintState.Closed).OrderBy(s => s.StartDate).ToList();

        // 2. No closed sprints — return empty response
        if (ascending.Count == 0)
        {
            await SendOkAsync(new DeveloperThroughputResponse([], []), ct);
            return;
        }

        // 3. Determine target sprint IDs based on mode
        List<int> targetSprintIds;

        if (req.SprintId.HasValue)
        {
            // Single sprint mode — accept active or closed sprint
            var match = analyticsSprints.FirstOrDefault(s => s.Id == req.SprintId.Value);
            if (match is null)
            {
                AddError(r => r.SprintId, "Sprint not found.");
                await SendErrorsAsync(400, ct);
                return;
            }
            targetSprintIds = new List<int> { match.Id };
        }
        else if (req.Last.HasValue)
        {
            // Last N closed sprints
            targetSprintIds = ascending
                .TakeLast(req.Last.Value)
                .Select(s => s.Id)
                .ToList();
        }
        else
        {
            // All closed sprints
            targetSprintIds = ascending.Select(s => s.Id).ToList();
        }

        // 4. Rolling average window expansion: need up to 2 extra closed sprints before the earliest target
        // For an active sprint target, all closed sprints are available as rolling window candidates
        var earliestTargetIndex = ascending.FindIndex(s => s.Id == targetSprintIds[0]);
        var rollingWindowBase = earliestTargetIndex >= 0 ? earliestTargetIndex : ascending.Count;
        var extraSprintIds = ascending
            .Take(rollingWindowBase)
            .TakeLast(2)
            .Select(s => s.Id)
            .ToList();

        var allSprintIds = extraSprintIds.Concat(targetSprintIds).Distinct().ToList();

        // 5. Bulk load sprints with memberships (target + extra window)
        var loadedSprints = await sprintRepository.GetSprintsWithMembershipsAsync(allSprintIds, ct);

        // 6. Load active developers for analytics display; load all for capacity fallback
        var activeDevelopers = await developerRepository.GetActiveDevelopersAsync(ct);
        var allDevelopers = await developerRepository.GetAllAsync(ct);

        // 7. Bulk load capacity records for all loaded sprint IDs
        var capacityRecords = await developerRepository.GetCapacitiesForSprintsAsync(allSprintIds, ct);

        // 8. Load app settings
        var settings = await appSettingsRepository.GetAsync(ct);

        // 9. Normalize sub-team
        var subTeam = string.IsNullOrWhiteSpace(req.SubTeam) ? null : req.SubTeam;

        // 10. Load status transitions for all sprint tickets (covers target + rolling average window)
        var statusTransitions = await ticketRepository.GetStatusTransitionsForSprintTicketsAsync(allSprintIds, ct);

        // 11. Apply cross-cutting exclusion: remove developers excluded in ALL target sprints
        var targetSprintsLoaded = loadedSprints.Where(s => targetSprintIds.Contains(s.Id)).ToList();
        var excludedInAllTargets = activeDevelopers
            .Where(dev => targetSprintsLoaded.All(sprint =>
                ExcludedDeveloperFilter.GetExcludedDeveloperIds(sprint, allDevelopers, capacityRecords, statusTransitions, settings)
                    .Contains(dev.Id)))
            .Select(dev => dev.Id)
            .ToHashSet();
        var filteredActiveDevelopers = activeDevelopers
            .Where(d => !excludedInAllTargets.Contains(d.Id))
            .ToList();

        // 12. Compute throughput
        var result = developerThroughputService.ComputeThroughput(
            loadedSprints,
            targetSprintIds,
            filteredActiveDevelopers,
            capacityRecords,
            settings,
            subTeam,
            allDevelopers,
            statusTransitions);

        await SendOkAsync(result, ct);
    }
}
