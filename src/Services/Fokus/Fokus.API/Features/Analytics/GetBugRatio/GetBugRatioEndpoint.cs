namespace Fokus.API.Features.Analytics.GetBugRatio;

[HttpGet("/api/analytics/bug-ratio")]
[Tags("Analytics")]
public class GetBugRatioEndpoint(
    SprintRepository sprintRepository,
    AppSettingsRepository appSettingsRepository,
    DeveloperRepository developerRepository,
    TicketRepository ticketRepository,
    BugRatioService bugRatioService)
    : Endpoint<GetBugRatioRequest, BugRatioResponse>
{
    public override async Task HandleAsync(GetBugRatioRequest req, CancellationToken ct)
    {
        // 1. Load analytics sprints — alert baseline uses closed sprints only; multi-sprint averaging is closed-only
        var analyticsSprints = await sprintRepository.GetAnalyticsSprintsAsync(ct);
        var ascending = analyticsSprints.Where(s => s.State == SprintState.Closed).OrderBy(s => s.StartDate).ToList();

        // 2. No closed sprints — return empty multi response
        if (ascending.Count == 0)
        {
            await SendOkAsync(new BugRatioResponse("multi", null, null), ct);
            return;
        }

        // 3. Normalize sub-team
        var subTeam = string.IsNullOrWhiteSpace(req.SubTeam) ? null : req.SubTeam;

        // 4. Load app settings
        var settings = await appSettingsRepository.GetAsync(ct);

        // 5. Load ALL closed sprints with memberships (needed for alert evaluation — BR9)
        var allClosedIds = ascending.Select(s => s.Id).ToList();
        var allLoadedSprints = await sprintRepository.GetSprintsWithMembershipsAsync(allClosedIds, ct);
        var allSprints = allLoadedSprints.OrderBy(s => s.StartDate).ToList();

        // 6. Load active developers and all developers for capacity fallback
        var activeDevelopers = await developerRepository.GetActiveDevelopersAsync(ct);
        var allDevelopers = await developerRepository.GetAllAsync(ct);

        // 7. Load capacity records for all closed sprints
        var capacityRecords = await developerRepository.GetCapacitiesForSprintsAsync(allClosedIds, ct);

        // 8. Load status transitions for all closed sprint tickets
        var statusTransitions = await ticketRepository.GetStatusTransitionsForSprintTicketsAsync(allClosedIds, ct);

        // 9. Determine mode
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

            // Prior sprint is the last closed sprint before the target (by start date)
            var targetIndex = allSprints.FindIndex(s => s.Id == target.Id);
            var priorSprint = targetIndex > 0
                ? allSprints[targetIndex - 1]
                : (target.State == SprintState.Active && allSprints.Count > 0 ? allSprints[^1] : null);

            var excludedIds = ExcludedDeveloperFilter.GetExcludedDeveloperIds(
                target, allDevelopers, capacityRecords, statusTransitions, settings);
            var filteredActive = activeDevelopers.Where(d => !excludedIds.Contains(d.Id)).ToList();

            var singleResult = bugRatioService.ComputeSingleSprint(
                target, priorSprint, allSprints, filteredActive, settings, statusTransitions, subTeam, excludedIds);

            await SendOkAsync(new BugRatioResponse("single", null, singleResult), ct);
        }
        else
        {
            // Multi-sprint mode — default last to 5 when neither param provided
            var last = req.Last ?? 5;
            var selectedSprints = allSprints.TakeLast(last).ToList();

            // For multi-sprint: exclude developers excluded in ALL selected sprints
            var excludedInAll = activeDevelopers
                .Where(dev => selectedSprints.All(sprint =>
                    ExcludedDeveloperFilter.GetExcludedDeveloperIds(sprint, allDevelopers, capacityRecords, statusTransitions, settings)
                        .Contains(dev.Id)))
                .Select(dev => dev.Id)
                .ToHashSet();
            var filteredActive = activeDevelopers.Where(d => !excludedInAll.Contains(d.Id)).ToList();

            var multiResult = bugRatioService.ComputeMultiSprint(
                selectedSprints, allSprints, filteredActive, settings, statusTransitions, subTeam, excludedInAll);

            await SendOkAsync(new BugRatioResponse("multi", multiResult, null), ct);
        }
    }
}
