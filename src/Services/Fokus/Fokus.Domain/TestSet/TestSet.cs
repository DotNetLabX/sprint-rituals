namespace Fokus.Domain;

public partial class TestSet : Entity<string>
{
    public required string IssueKey { get; set; }
    public required string Summary { get; set; }
    public string? AssigneeId { get; set; }
    public required string Status { get; set; }

    public Developer? Assignee { get; set; }
}
