using Jira.Contracts;

namespace Fokus.API.Features.Sync.GetJiraSprints;

[HttpGet("/api/jira/sprints")]
[Tags("Sync")]
public class GetJiraSprintsEndpoint(IJiraClient jiraClient)
    : Endpoint<GetJiraSprintsQuery, GetJiraSprintsResponse>
{
    public override async Task HandleAsync(GetJiraSprintsQuery query, CancellationToken ct)
    {
        var sprints = await jiraClient.GetSprintsAsync(query.BoardId, ct, SprintState.Active, SprintState.Closed);

        var ordered = sprints
            .OrderBy(s => s.StartDate)
            .Select(s => new JiraSprintDto
            {
                Id = s.Id,
                Name = s.Name,
                StartDate = s.StartDate,
                State = s.State
            })
            .ToList();

        await SendOkAsync(new GetJiraSprintsResponse { Sprints = ordered }, ct);
    }
}
