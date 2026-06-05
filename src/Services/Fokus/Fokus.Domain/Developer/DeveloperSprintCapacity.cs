namespace Fokus.Domain;

public class DeveloperSprintCapacity
{
    public required string DeveloperAccountId { get; set; }
    public int SprintId { get; set; }
    public int CapacityPercent { get; set; } = 100;

    public Developer Developer { get; set; } = null!;
    public Sprint Sprint { get; set; } = null!;
}
