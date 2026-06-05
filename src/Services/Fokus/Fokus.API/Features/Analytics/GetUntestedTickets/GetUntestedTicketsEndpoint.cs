namespace Fokus.API.Features.Analytics.GetUntestedTickets;

[HttpGet("/api/sprints/{sprintId}/qa-metrics/untested")]
[Tags("Analytics")]
public class GetUntestedTicketsEndpoint(
    SprintRepository sprintRepository,
    AppSettingsRepository appSettingsRepository,
    TestExecutionRepository testExecutionRepository)
    : Endpoint<GetUntestedTicketsRequest, UntestedTicketsResponse>
{
    public override async Task HandleAsync(GetUntestedTicketsRequest req, CancellationToken ct)
    {
        // 1. Load settings
        var settings = await appSettingsRepository.GetAsync(ct);

        // 2. Load the sprint (lightweight, no memberships needed for existence check)
        var sprints = await sprintRepository.GetAnalyticsSprintsAsync(ct);
        var sprintInfo = sprints.FirstOrDefault(s => s.Id == req.SprintId);
        if (sprintInfo is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        // 3. If Xray disabled, return empty list
        if (!settings.XrayEnabled)
        {
            await SendOkAsync(new UntestedTicketsResponse(), ct);
            return;
        }

        // 4. Load sprint with memberships to get sprint dates
        var sprint = await sprintRepository.GetSprintWithMembershipsAsync(req.SprintId, ct);
        if (sprint is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        // 5. Resolve transition-based stage boundaries from settings
        var (orderedStages, startIndex) = TransitionAttributionChecker.ResolveStartIndex(settings);

        // 6. Normalize sub-team
        var subTeam = string.IsNullOrWhiteSpace(req.SubTeam) ? null : req.SubTeam;

        // 7. Get feature tickets with coverage data
        var tickets = await testExecutionRepository.GetFeatureTicketsWithCoverageAsync(
            req.SprintId,
            settings.ExcludedFromScopeStatuses,
            orderedStages,
            startIndex,
            sprint.StartDate,
            sprint.EndDate,
            settings.DefaultSpPerBug,
            ct);

        // 8. Filter to untested tickets, apply sub-team filter, sort by SP descending (BR25)
        var untested = tickets
            .Where(t => !t.HasCoverage)
            .Where(t => string.IsNullOrWhiteSpace(subTeam) || IsInSubTeam(t, sprint, subTeam))
            .OrderByDescending(t => t.StoryPoints ?? 0)
            .Select(t => new UntestedTicketItem
            {
                TicketKey = t.TicketKey,
                Summary = t.Summary,
                AssigneeName = t.AssigneeName,
                StoryPoints = t.StoryPoints
            })
            .ToList();

        await SendOkAsync(new UntestedTicketsResponse { Tickets = untested }, ct);
    }

    private static bool IsInSubTeam(TicketCoverageInfo ticket, Sprint sprint, string subTeam)
    {
        var membership = sprint.Memberships.FirstOrDefault(m =>
            string.Equals(m.TicketId, ticket.TicketKey, StringComparison.OrdinalIgnoreCase));
        return membership?.Ticket?.Assignee?.SubTeam == subTeam;
    }
}
