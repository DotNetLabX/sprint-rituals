namespace Jira.Contracts;

public class JiraSprint
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int OriginBoardId { get; set; }
    public string? Goal { get; set; }
}
