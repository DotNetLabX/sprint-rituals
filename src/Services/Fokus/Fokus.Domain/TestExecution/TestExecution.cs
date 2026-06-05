namespace Fokus.Domain;

public partial class TestExecution : Entity<string>
{
    public required string IssueKey { get; set; }
    public required string Summary { get; set; }
    public required string Status { get; set; }
    public string? AssigneeId { get; set; }
    public DateTime CreatedDate { get; set; }

    public bool IsCancelled => Status == "Cancelled";

    public Developer? Assignee { get; set; }

    private readonly List<TestExecutionLink> _links = [];
    public IReadOnlyList<TestExecutionLink> Links => _links.AsReadOnly();

    private readonly List<TestRun> _testRuns = [];
    public IReadOnlyList<TestRun> TestRuns => _testRuns.AsReadOnly();
}
