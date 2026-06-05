namespace Fokus.Domain;

public partial class TestRun : Entity<string>
{
    public required string TestExecutionIssueId { get; set; }
    public TestRunStatus Status { get; set; }
    public required string StatusName { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string? ExecutedById { get; set; }

    public TestExecution TestExecution { get; set; } = null!;
    public Developer? ExecutedBy { get; set; }
}
