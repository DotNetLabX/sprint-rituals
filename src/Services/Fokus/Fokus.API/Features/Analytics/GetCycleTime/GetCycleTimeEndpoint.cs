namespace Fokus.API.Features.Analytics.GetCycleTime;

[HttpGet("/api/analytics/cycle-time")]
[Tags("Analytics")]
public class GetCycleTimeEndpoint(
    SprintRepository sprintRepository,
    TicketRepository ticketRepository,
    AppSettingsRepository appSettingsRepository,
    DeveloperRepository developerRepository,
    CycleTimeService cycleTimeService)
    : Endpoint<GetCycleTimeRequest, CycleTimeResponse>
{
    public override async Task HandleAsync(GetCycleTimeRequest req, CancellationToken ct)
    {
        // 1. Load analytics sprints — multi-sprint averaging uses closed sprints only
        var analyticsSprints = await sprintRepository.GetAnalyticsSprintsAsync(ct);
        var ascending = analyticsSprints.Where(s => s.State == SprintState.Closed).OrderBy(s => s.StartDate).ToList();

        // 2. No closed sprints — return empty multi response
        if (ascending.Count == 0)
        {
            await SendOkAsync(new CycleTimeResponse("multi", null, null), ct);
            return;
        }

        // 3. Normalize sub-team
        var subTeam = string.IsNullOrWhiteSpace(req.SubTeam) ? null : req.SubTeam;

        // 4. Load app settings, all developers, and capacity records
        var settings = await appSettingsRepository.GetAsync(ct);
        var allDevelopers = await developerRepository.GetAllAsync(ct);
        var allClosedIds = ascending.Select(s => s.Id).ToList();
        var capacityRecords = await developerRepository.GetCapacitiesForSprintsAsync(allClosedIds, ct);

        // 5. Determine mode
        if (req.SprintId.HasValue)
        {
            // Single-sprint mode — accept active or closed sprint
            var match = analyticsSprints.FirstOrDefault(s => s.Id == req.SprintId.Value);
            if (match is null)
            {
                AddError(r => r.SprintId, "Sprint not found.");
                await SendErrorsAsync(400, ct);
                return;
            }

            // Identify prior sprint (last closed sprint before target by start date)
            var closedIndex = ascending.FindIndex(s => s.Id == match.Id);
            var priorSprintLightweight = closedIndex > 0
                ? ascending[closedIndex - 1]
                : (match.State == SprintState.Active && ascending.Count > 0 ? ascending[^1] : null);

            // Bulk load target and prior sprints with memberships
            var sprintIdsToLoad = new List<int> { match.Id };
            if (priorSprintLightweight is not null)
                sprintIdsToLoad.Add(priorSprintLightweight.Id);

            var loadedSprints = await sprintRepository.GetSprintsWithMembershipsAsync(sprintIdsToLoad, ct);
            var targetSprint = loadedSprints.First(s => s.Id == match.Id);
            var priorSprint = priorSprintLightweight is not null
                ? loadedSprints.FirstOrDefault(s => s.Id == priorSprintLightweight.Id)
                : null;

            // Load status transitions via sprint-based method
            var statusTransitions = await ticketRepository.GetStatusTransitionsForSprintTicketsAsync(sprintIdsToLoad, ct);

            // Compute excluded developer IDs for the target sprint
            var excludedIds = ExcludedDeveloperFilter.GetExcludedDeveloperIds(
                targetSprint, allDevelopers, capacityRecords, statusTransitions, settings);

            var singleResult = cycleTimeService.ComputeSingleSprint(
                targetSprint, priorSprint, statusTransitions, settings, subTeam, excludedIds);

            await SendOkAsync(new CycleTimeResponse("single", singleResult, null), ct);
        }
        else
        {
            // Multi-sprint mode
            // last=0 means "all sprints"; null defaults to all closed sprints
            var last = req.Last ?? 0;
            var targetSprints = last == 0 ? ascending : ascending.TakeLast(last).ToList();
            var targetIds = targetSprints.Select(s => s.Id).ToList();

            var loadedSprints = await sprintRepository.GetSprintsWithMembershipsAsync(targetIds, ct);

            // Load status transitions via sprint-based method
            var statusTransitions = await ticketRepository.GetStatusTransitionsForSprintTicketsAsync(targetIds, ct);

            // Compute excluded developer IDs across all target sprints
            var excludedInAll = loadedSprints
                .SelectMany(sprint =>
                    ExcludedDeveloperFilter.GetExcludedDeveloperIds(sprint, allDevelopers, capacityRecords, statusTransitions, settings))
                .GroupBy(id => id)
                .Where(g => g.Count() == loadedSprints.Count)
                .Select(g => g.Key)
                .ToHashSet();

            var multiResult = cycleTimeService.ComputeMultiSprint(
                loadedSprints, statusTransitions, settings, subTeam, excludedInAll);

            await SendOkAsync(new CycleTimeResponse("multi", null, multiResult), ct);
        }
    }
}
