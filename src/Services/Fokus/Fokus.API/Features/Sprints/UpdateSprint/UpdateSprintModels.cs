namespace Fokus.API.Features.Sprints.UpdateSprint;

public class UpdateSprintRequest
{
    public int SprintId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Goal { get; set; }
}

public class UpdateSprintResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Goal { get; set; }
    public string State { get; set; } = string.Empty;
}
