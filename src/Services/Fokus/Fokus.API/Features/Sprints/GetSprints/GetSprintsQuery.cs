namespace Fokus.API.Features.Sprints.GetSprints;

public class SprintItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string State { get; set; } = string.Empty;
    public string? Goal { get; set; }
}
