namespace Fokus.Domain;

public partial class TestExecution
{
    public static TestExecution FromXray(
        string issueId,
        string issueKey,
        string summary,
        string status,
        string? assigneeId,
        DateTime createdDate)
    {
        return new TestExecution
        {
            Id = issueId,
            IssueKey = issueKey,
            Summary = summary,
            Status = status,
            AssigneeId = assigneeId,
            CreatedDate = createdDate
        };
    }
}
