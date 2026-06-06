namespace Jira.Contracts;

public class UpdateJiraSprintCommand
{
    public string? Name { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Goal { get; set; }
}
