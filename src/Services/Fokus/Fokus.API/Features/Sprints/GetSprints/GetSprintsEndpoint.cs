namespace Fokus.API.Features.Sprints.GetSprints;

[HttpGet("/api/sprints")]
[Tags("Sprints")]
public class GetSprintsEndpoint(SprintRepository sprintRepository)
    : EndpointWithoutRequest<List<SprintItem>>
{
    public override async Task HandleAsync(CancellationToken ct)
    {
        var sprints = await sprintRepository.GetAnalyticsSprintsAsync(ct);

        var response = sprints.Select(s => new SprintItem
        {
            Id = s.Id,
            Name = s.Name,
            StartDate = s.StartDate,
            EndDate = s.EndDate,
            State = s.State.ToString(),
            Goal = s.Goal
        }).ToList();

        await SendOkAsync(response, ct);
    }
}
