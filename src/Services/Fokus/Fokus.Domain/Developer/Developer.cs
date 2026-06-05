namespace Fokus.Domain;

public partial class Developer : Entity<string>
{
    public required string DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? SubTeam { get; set; }
    public bool IsActive { get; set; } = true;
    public string Role { get; set; } = "Developer";
    public int DefaultCapacityPercent { get; set; } = 100;
}
