using Fokus.API.Features.Analytics;

namespace Fokus.API.Features.Sprints.GetTestTimeline;

[HttpGet("/api/sprints/{sprintId}/test-timeline")]
[Tags("Sprints")]
public class GetTestTimelineEndpoint(
    SprintRepository sprintRepository,
    AppSettingsRepository appSettingsRepository,
    TestExecutionRepository testExecutionRepository,
    TicketRepository ticketRepository,
    TestTimelineService testTimelineService)
    : Endpoint<GetTestTimelineRequest, TestTimelineResponse>
{
    public override async Task HandleAsync(GetTestTimelineRequest req, CancellationToken ct)
    {
        // 1. Load settings
        var settings = await appSettingsRepository.GetAsync(ct);

        // 2. Load all analytics sprints (lightweight, ascending)
        var sprints = await sprintRepository.GetAnalyticsSprintsAsync(ct);
        var ascending = sprints.OrderBy(s => s.StartDate).ToList();

        // 3. Find requested sprint — 404 if not found
        var sprintInfo = ascending.FirstOrDefault(s => s.Id == req.SprintId);
        if (sprintInfo is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        // 4. Xray disabled — return hasQaData=false, isXrayEnabled=false
        if (!settings.XrayEnabled)
        {
            await SendOkAsync(BuildEmptyResponse(sprintInfo, settings, isXrayEnabled: false), ct);
            return;
        }

        // 5. Load sprint with memberships
        var sprintLoaded = await sprintRepository.GetSprintsWithMembershipsAsync([sprintInfo.Id], ct);
        var sprint = sprintLoaded[0];

        // 6. Load TEs for the sprint
        var sprintTEs = await testExecutionRepository.GetTestExecutionsForSprintAsync(sprint.Id, ct);

        // 7. No TEs → hasQaData=false, isXrayEnabled=true (Xray on, just not synced)
        var hasAnyRuns = sprintTEs.Any(te => te.TestRuns.Any());
        if (!hasAnyRuns)
        {
            await SendOkAsync(BuildEmptyResponse(sprintInfo, settings, isXrayEnabled: true), ct);
            return;
        }

        // 8. Normalize sub-team
        var subTeam = string.IsNullOrWhiteSpace(req.SubTeam) ? null : req.SubTeam;

        // 9. Resolve prior sprint for delta computation
        var selectedIndex = ascending.FindIndex(s => s.Id == sprint.Id);
        Sprint? priorSprint = null;
        List<TestExecution>? priorTEs = null;
        List<SprintMembership>? priorMemberships = null;

        if (selectedIndex > 0)
        {
            var priorInfo = ascending[selectedIndex - 1];
            var priorLoaded = await sprintRepository.GetSprintsWithMembershipsAsync([priorInfo.Id], ct);
            priorSprint = priorLoaded[0];
            priorTEs = await testExecutionRepository.GetTestExecutionsForSprintAsync(priorSprint.Id, ct);
            priorMemberships = priorSprint.Memberships.ToList();
        }

        // 10. Load status transitions — current sprint + prior sprint (needed for gap delta)
        var sprintIdsForTransitions = priorSprint is not null
            ? new List<int> { sprint.Id, priorSprint.Id }
            : new List<int> { sprint.Id };
        var allTransitions = await ticketRepository.GetStatusTransitionsForSprintTicketsAsync(sprintIdsForTransitions, ct);

        // Split into per-sprint dictionaries by ticket membership
        var currentTicketIds = sprint.Memberships.Select(m => m.TicketId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var statusTransitions = allTransitions.Where(t => currentTicketIds.Contains(t.TicketId)).ToList();

        List<StatusTransition>? priorStatusTransitions = null;
        if (priorSprint is not null)
        {
            var priorTicketIds = priorSprint.Memberships.Select(m => m.TicketId).ToHashSet(StringComparer.OrdinalIgnoreCase);
            priorStatusTransitions = allTransitions.Where(t => priorTicketIds.Contains(t.TicketId)).ToList();
        }

        // 11. Compute and return
        var result = testTimelineService.ComputeTimeline(
            sprintTEs,
            sprint.Memberships.ToList(),
            statusTransitions,
            sprint,
            priorSprint,
            priorTEs,
            priorMemberships,
            priorStatusTransitions,
            settings,
            subTeam);

        await SendOkAsync(result, ct);
    }

    private static TestTimelineResponse BuildEmptyResponse(Sprint sprint, AppSettings settings, bool isXrayEnabled) =>
        new(
            HasQaData: false,
            IsXrayEnabled: isXrayEnabled,
            SprintStartDate: sprint.StartDate,
            SprintEndDate: sprint.EndDate,
            PlanningWindowDays: settings.PlanningWindowDays,
            BurnupData: [],
            ScopeChangeOverlay: [],
            TestingCrunch: new TestingCrunchResult(false, null, 0, 0, []),
            PostSprintTesting: new PostSprintTestingResult(false, null, 0, 0, []),
            UntestedAtClose: new UntestedAtCloseResult(false, 0, []),
            DevToTestGap: new DevToTestGapResult(null, null, null, []));
}
