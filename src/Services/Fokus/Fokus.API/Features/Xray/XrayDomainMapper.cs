namespace Fokus.API.Features.Xray;

/// <summary>
/// Boundary mapper for Xray/TestSet domain-object construction.
/// These three methods take only primitives/enums — there is no external DTO source type,
/// so no Mapster IRegister is used. Plain construction helpers relocated from Fokus.Domain
/// per ADR-006 (all construction of domain types from external data happens at the API boundary).
/// </summary>
public static class XrayDomainMapper
{
    public static TestRun CreateTestRun(
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

    public static TestExecution CreateTestExecution(
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

    public static TestSet CreateTestSet(
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
