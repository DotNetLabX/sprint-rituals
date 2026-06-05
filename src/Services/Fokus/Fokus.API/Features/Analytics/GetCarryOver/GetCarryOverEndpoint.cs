namespace Fokus.API.Features.Analytics.GetCarryOver;

[HttpGet("/api/analytics/carry-over")]
[Tags("Analytics")]
public class GetCarryOverEndpoint(
    SprintRepository sprintRepository,
    AppSettingsRepository appSettingsRepository,
    DeveloperRepository developerRepository,
    TicketRepository ticketRepository,
    CarryOverService carryOverService)
    : Endpoint<GetCarryOverRequest, CarryOverResponse>
{
    public override async Task HandleAsync(GetCarryOverRequest req, CancellationToken ct)
    {
        // 1. Load analytics sprints — zombie counting baseline uses closed sprints only; multi-sprint is closed-only
        var analyticsSprints = await sprintRepository.GetAnalyticsSprintsAsync(ct);
        var ascending = analyticsSprints.Where(s => s.State == SprintState.Closed).OrderBy(s => s.StartDate).ToList();

        // 2. No closed sprints — return empty multi response
        if (ascending.Count == 0)
        {
            await SendOkAsync(new CarryOverResponse("multi", null, null), ct);
            return;
        }

        // 3. Normalize sub-team
        var subTeam = string.IsNullOrWhiteSpace(req.SubTeam) ? null : req.SubTeam;

        // 4. Load app settings, all developers, and capacity records
        var settings = await appSettingsRepository.GetAsync(ct);
        var allDevelopers = await developerRepository.GetAllAsync(ct);
        var allClosedIds = ascending.Select(s => s.Id).ToList();
        var capacityRecords = await developerRepository.GetCapacitiesForSprintsAsync(allClosedIds, ct);

        // 5. Load ALL closed sprints with memberships — needed for accurate zombie sprint counting (BR7)
        var allLoadedSprints = await sprintRepository.GetSprintsWithMembershipsAsync(allClosedIds, ct);
        var allSprints = allLoadedSprints.OrderBy(s => s.StartDate).ToList();

        // 6. Load status transitions for all closed sprint tickets
        var statusTransitions = await ticketRepository.GetStatusTransitionsForSprintTicketsAsync(allClosedIds, ct);

        // 7. Determine mode
        if (req.SprintId.HasValue)
        {
            // Single-sprint mode — accept active or closed sprint
            var target = analyticsSprints.FirstOrDefault(s => s.Id == req.SprintId.Value);
            if (target is null)
            {
                AddError(r => r.SprintId, "Sprint not found.");
                await SendErrorsAsync(400, ct);
                return;
            }

            // 8b. Identify prior sprint (last closed sprint before the target by start date)
            var targetIndex = allSprints.FindIndex(s => s.Id == target.Id);
            var priorSprint = targetIndex > 0
                ? allSprints[targetIndex - 1]
                : (target.State == SprintState.Active && allSprints.Count > 0 ? allSprints[^1] : null);

            // 9b. Compute excluded developer IDs for the target sprint
            var excludedIds = ExcludedDeveloperFilter.GetExcludedDeveloperIds(
                target, allDevelopers, capacityRecords, statusTransitions, settings);

            // 10b. Compute single-sprint response
            var singleResult = carryOverService.ComputeSingleSprint(
                target, priorSprint, allSprints, statusTransitions, settings, subTeam, excludedIds);

            await SendOkAsync(new CarryOverResponse("single", null, singleResult), ct);
        }
        else
        {
            // Multi-sprint mode
            // last=0 means "all sprints"; null defaults to last 5
            var last = req.Last ?? 5;

            // 8a. Take last N sprints (0 = all)
            var selectedSprints = last == 0 ? allSprints : allSprints.TakeLast(last).ToList();

            // 9a. Compute excluded developer IDs across selected sprints
            var excludedInAll = selectedSprints
                .SelectMany(sprint =>
                    ExcludedDeveloperFilter.GetExcludedDeveloperIds(sprint, allDevelopers, capacityRecords, statusTransitions, settings))
                .GroupBy(id => id)
                .Where(g => g.Count() == selectedSprints.Count)
                .Select(g => g.Key)
                .ToHashSet();

            // 10a. Compute multi-sprint response
            var multiResult = carryOverService.ComputeMultiSprint(
                selectedSprints, allSprints, statusTransitions, settings, subTeam, excludedInAll);

            await SendOkAsync(new CarryOverResponse("multi", multiResult, null), ct);
        }
    }
}
