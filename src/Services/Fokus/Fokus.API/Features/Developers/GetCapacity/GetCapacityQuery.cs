namespace Fokus.API.Features.Developers.GetCapacity;

public class GetCapacityRequest
{
    public string AccountId { get; set; } = string.Empty;
    public int? SprintId { get; set; }
}

public class CapacityEntry
{
    public int SprintId { get; set; }
    public int CapacityPercent { get; set; }
}
