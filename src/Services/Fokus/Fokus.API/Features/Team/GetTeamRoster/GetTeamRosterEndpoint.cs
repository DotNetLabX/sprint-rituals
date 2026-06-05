namespace Fokus.API.Features.Team.GetTeamRoster;

public class TeamDeveloperDto
{
    public string AccountId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = string.Empty;
    public int DefaultCapacityPercent { get; set; }
    public string? SubTeam { get; set; }
    public bool IsActive { get; set; }
}

public class TeamRosterResponse
{
    public List<TeamDeveloperDto> Developers { get; set; } = new();
    public List<string> SubTeams { get; set; } = new();
}

[HttpGet("/api/team")]
[Tags("Team")]
public class GetTeamRosterEndpoint(DeveloperRepository developerRepository)
    : EndpointWithoutRequest<TeamRosterResponse>
{
    public override async Task HandleAsync(CancellationToken ct)
    {
        var developers = await developerRepository.GetAllAsync(ct);
        var subTeams = await developerRepository.GetDistinctSubTeamsAsync(ct);

        var dtos = developers.Select(d => new TeamDeveloperDto
        {
            AccountId = d.Id,
            DisplayName = d.DisplayName,
            AvatarUrl = d.AvatarUrl,
            Role = d.Role,
            DefaultCapacityPercent = d.DefaultCapacityPercent,
            SubTeam = d.SubTeam,
            IsActive = d.IsActive
        }).ToList();

        await SendOkAsync(new TeamRosterResponse
        {
            Developers = dtos,
            SubTeams = subTeams
        }, ct);
    }
}
