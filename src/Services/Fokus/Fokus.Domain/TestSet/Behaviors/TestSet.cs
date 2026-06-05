namespace Fokus.Domain;

public partial class TestSet
{
    public static TestSet FromJiraIssueLink(
        string issueId,
        string issueKey,
        string summary,
        string? assigneeId,
        string status)
    {
        return new TestSet
        {
            Id = issueId,
            IssueKey = issueKey,
            Summary = summary,
            AssigneeId = assigneeId,
            Status = status
        };
    }
}
