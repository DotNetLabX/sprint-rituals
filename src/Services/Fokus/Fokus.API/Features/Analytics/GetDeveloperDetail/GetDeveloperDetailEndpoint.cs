using Jira.RestApi;
using Microsoft.Extensions.Options;

namespace Fokus.API.Features.Analytics.GetDeveloperDetail;

[HttpGet("/api/analytics/developer-detail/{accountId}")]
[Tags("Analytics")]
public class GetDeveloperDetailEndpoint(
    SprintRepository sprintRepository,
    DeveloperRepository developerRepository,
    AppSettingsRepository appSettingsRepository,
    TicketRepository ticketRepository,
    DeveloperDetailService developerDetailService,
    IOptions<JiraOptions> jiraOptions)
    : Endpoint<GetDeveloperDetailRequest, DeveloperDetailResponse>
{
    public override async Task HandleAsync(GetDeveloperDetailRequest req, CancellationToken ct)
    {
        // 1. Look up developer
        var allDevelopers = await developerRepository.GetAllAsync(ct);
        var developer = allDevelopers.FirstOrDefault(d => d.Id == req.AccountId);
        if (developer is null || !developer.IsActive)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        // 2. Load analytics sprints (active + closed)
        var analyticsSprints = await sprintRepository.GetAnalyticsSprintsAsync(ct);
        var closedSprintInfos = analyticsSprints
            .Where(s => s.State == SprintState.Closed)
            .OrderBy(s => s.StartDate)
            .ToList();

        // 3. Load settings
        var settings = await appSettingsRepository.GetAsync(ct);

        // 4. Load all closed sprint IDs with this developer's memberships — we need memberships to filter
        var allClosedIds = closedSprintInfos.Select(s => s.Id).ToList();
        var allLoadedSprints = await sprintRepository.GetSprintsWithMembershipsAsync(allClosedIds, ct);
        var closedSprints = allLoadedSprints.OrderBy(s => s.StartDate).ToList();

        // 5. Load capacities and all developers for capacity fallback
        var capacityRecords = await developerRepository.GetCapacitiesForSprintsAsync(allClosedIds, ct);

        // 6. Build capacity lookup: accountId -> sprintId -> capacityPercent
        var capacityLookup = capacityRecords
            .GroupBy(c => c.DeveloperAccountId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(c => c.SprintId, c => c.CapacityPercent));

        // 7. Load status transitions for all closed sprint tickets
        var statusTransitions = await ticketRepository.GetStatusTransitionsForSprintTicketsAsync(allClosedIds, ct);

        // 8. Excluded developer check: 0% capacity AND 0 completed in all sprints
        var (orderedStages, _) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, orderedStages);

        var hasAnyActivity = closedSprints.Any(sprint =>
        {
            var effectiveCapacity = capacityLookup.TryGetValue(req.AccountId, out var sprintMap) &&
                                    sprintMap.TryGetValue(sprint.Id, out var cap)
                ? cap
                : developer.DefaultCapacityPercent;

            if (effectiveCapacity > 0) return true;

            return sprint.Memberships.Any(m =>
            {
                if (m.Ticket?.AssigneeId != req.AccountId) return false;
                if (m.RemovedAt != null) return false;
                var (isCompleted, _) = TransitionAttributionChecker.IsCompletedInSprint(
                    m.TicketId, statusTransitions, sprint.StartDate, sprint.EndDate, orderedStages, endIndex);
                return isCompleted;
            });
        });

        if (!hasAnyActivity)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        // 9. Load active sprint with memberships (if any)
        var activeSprintInfo = analyticsSprints.FirstOrDefault(s => s.State == SprintState.Active);
        Sprint? activeSprint = null;
        List<DeveloperSprintCapacity> activeCapacityRecords = [];

        if (activeSprintInfo is not null)
        {
            activeSprint = await sprintRepository.GetSprintWithMembershipsAsync(activeSprintInfo.Id, ct);
            if (activeSprint is not null)
            {
                activeCapacityRecords = await developerRepository.GetCapacitiesForSprintsAsync([activeSprintInfo.Id], ct);

                // Load transitions for active sprint tickets too
                var activeTransitions = await ticketRepository.GetStatusTransitionsForSprintTicketsAsync([activeSprintInfo.Id], ct);
                statusTransitions = [..statusTransitions, ..activeTransitions];
            }
        }

        // 10. Determine last (sprint range)
        var last = req.Last ?? 10;

        // 11. Compute and return
        var result = developerDetailService.ComputeDetail(
            req.AccountId,
            developer,
            closedSprints,
            activeSprint,
            capacityLookup,
            activeCapacityRecords,
            settings,
            statusTransitions,
            last,
            settings.BugRatioTarget,
            jiraOptions.Value.InstanceUrl);

        await SendOkAsync(result, ct);
    }
}
