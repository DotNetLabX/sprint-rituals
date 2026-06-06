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
