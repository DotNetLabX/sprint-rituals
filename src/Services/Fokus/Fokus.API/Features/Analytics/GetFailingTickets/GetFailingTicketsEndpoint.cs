namespace Fokus.API.Features.Analytics.GetFailingTickets;

[HttpGet("/api/sprints/{sprintId}/qa-metrics/failing")]
[Tags("Analytics")]
public class GetFailingTicketsEndpoint(
    SprintRepository sprintRepository,
    AppSettingsRepository appSettingsRepository,
    TestExecutionRepository testExecutionRepository)
    : Endpoint<GetFailingTicketsRequest, FailingTicketsResponse>
{
    public override async Task HandleAsync(GetFailingTicketsRequest req, CancellationToken ct)
    {
        // 1. Load settings
        var settings = await appSettingsRepository.GetAsync(ct);

        // 2. Find the requested sprint
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
            await SendOkAsync(new FailingTicketsResponse(), ct);
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

        // 7. Get feature tickets with coverage and run count data
        var tickets = await testExecutionRepository.GetFeatureTicketsWithCoverageAsync(
            req.SprintId,
            settings.ExcludedFromScopeStatuses,
            orderedStages,
            startIndex,
            sprint.StartDate,
            sprint.EndDate,
            settings.DefaultSpPerBug,
            ct);

        // 8. Filter to failing tickets, apply sub-team filter, sort by failed run count descending (BR26)
        var failing = tickets
            .Where(t => t.FailedRunCount > 0)
            .Where(t => string.IsNullOrWhiteSpace(subTeam) || IsInSubTeam(t, sprint, subTeam))
            .OrderByDescending(t => t.FailedRunCount)
            .Select(t => new FailingTicketItem
            {
                TicketKey = t.TicketKey,
                Summary = t.Summary,
                AssigneeName = t.AssigneeName,
                StoryPoints = t.StoryPoints,
                FailedRunCount = t.FailedRunCount,
                TotalRunCount = t.TotalRunCount
            })
            .ToList();

        await SendOkAsync(new FailingTicketsResponse { Tickets = failing }, ct);
    }

    private static bool IsInSubTeam(TicketCoverageInfo ticket, Sprint sprint, string subTeam)
    {
        var membership = sprint.Memberships.FirstOrDefault(m =>
            string.Equals(m.TicketId, ticket.TicketKey, StringComparison.OrdinalIgnoreCase));
        return membership?.Ticket?.Assignee?.SubTeam == subTeam;
    }
}
