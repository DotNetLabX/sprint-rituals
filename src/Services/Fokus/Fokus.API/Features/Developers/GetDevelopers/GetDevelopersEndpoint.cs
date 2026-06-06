namespace Fokus.API.Features.Developers.GetDevelopers;

[HttpGet("/api/developers")]
[Tags("Developers")]
public class GetDevelopersEndpoint(DeveloperRepository developerRepository)
    : EndpointWithoutRequest<List<DeveloperDto>>
{
    public override async Task HandleAsync(CancellationToken ct)
    {
        var developers = await developerRepository.GetAllAsync(ct);
        var result = developers.Select(d => new DeveloperDto
        {
            AccountId = d.Id,
            DisplayName = d.DisplayName,
            AvatarUrl = d.AvatarUrl,
            SubTeam = d.SubTeam,
            IsActive = d.IsActive
        }).ToList();
        await Send.OkAsync(result, ct);
    }
}
