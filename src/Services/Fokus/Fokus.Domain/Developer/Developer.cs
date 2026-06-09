namespace Fokus.Domain;

public partial class Developer : Entity<string>
{
    public required string DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? SubTeam { get; set; }
    public bool IsActive { get; set; } = true;
    public string Role { get; set; } = "Developer";
    public int DefaultCapacityPercent { get; set; } = 100;
    /// <summary>
    /// Email address. Populated best-effort from Jira user fetch.
    /// Null when Jira Cloud restricts visibility (most orgs).
    /// </summary>
    public string? Email { get; set; }
}
