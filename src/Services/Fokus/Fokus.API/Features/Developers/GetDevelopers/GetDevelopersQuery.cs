namespace Fokus.API.Features.Developers.GetDevelopers;

public class DeveloperDto
{
    public string AccountId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? SubTeam { get; set; }
    public bool IsActive { get; set; }
}
