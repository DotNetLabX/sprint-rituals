namespace Fokus.Domain;

public partial class TestRun
{
    public static TestRun FromXray(
        string id,
        string testExecutionIssueId,
        TestRunStatus status,
        string statusName,
        DateTime? startedAt,
        DateTime? finishedAt,
        string? executedById)
    {
        return new TestRun
        {
            Id = id,
            TestExecutionIssueId = testExecutionIssueId,
            Status = status,
            StatusName = statusName,
            StartedAt = startedAt,
            FinishedAt = finishedAt,
            ExecutedById = executedById
        };
    }
}
