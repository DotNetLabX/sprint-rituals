namespace Fokus.API.Features.Developers.GetSubTeams;

[HttpGet("/api/developers/sub-teams")]
[Tags("Developers")]
public class GetSubTeamsEndpoint(DeveloperRepository developerRepository)
    : EndpointWithoutRequest<List<string>>
{
    public override async Task HandleAsync(CancellationToken ct)
    {
        var subTeams = await developerRepository.GetDistinctSubTeamsAsync(ct);
        await SendOkAsync(subTeams, ct);
    }
}
