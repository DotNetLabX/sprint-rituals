using Jira.Contracts;

namespace Fokus.API.Features.Sync.GetStatuses;

[HttpGet("/api/jira/statuses")]
[Tags("Sync")]
public class GetStatusesEndpoint(IJiraClient jiraClient)
    : EndpointWithoutRequest<GetStatusesResponse>
{
    public override async Task HandleAsync(CancellationToken ct)
    {
        var statuses = await jiraClient.GetStatusesAsync(ct);

        await SendOkAsync(new GetStatusesResponse
        {
            Statuses = statuses.Select(s => new StatusDto
            {
                Name = s.Name,
                CategoryKey = s.CategoryKey
            }).ToList()
        }, ct);
    }
}
