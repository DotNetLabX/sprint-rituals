namespace Fokus.API.Features.Developers.GetDevelopers;

public class DeveloperDto
{
    public string AccountId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? SubTeam { get; set; }
    public bool IsActive { get; set; }
}

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
        await SendOkAsync(result, ct);
    }
}
