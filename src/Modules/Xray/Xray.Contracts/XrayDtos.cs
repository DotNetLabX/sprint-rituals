namespace Xray.Contracts;

public class XrayTestExecutionResult
{
    public List<XrayTestExecutionDto> TestExecutions { get; set; } = [];
}

public class XrayTestExecutionDto
{
    public string IssueId { get; set; } = string.Empty;
    public string? IssueKey { get; set; }
    public string? Summary { get; set; }
    public string? Status { get; set; }
    public string? AssigneeId { get; set; }
    public DateTime? CreatedDate { get; set; }
    public List<XrayTestRunDto> TestRuns { get; set; } = [];
    public List<XrayTestCaseDto> TestCases { get; set; } = [];
    public List<XrayJiraIssueLinkDto> IssueLinks { get; set; } = [];
}

public class XrayTestRunDto
{
    public string Id { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string? ExecutedById { get; set; }
}

public class XrayTestCaseDto
{
    public string? IssueKey { get; set; }
    public List<XrayJiraIssueLinkDto> IssueLinks { get; set; } = [];
}

public class XrayJiraIssueLinkDto
{
    public string LinkTypeName { get; set; } = string.Empty;
    public string? OutwardIssueKey { get; set; }
    public string? InwardIssueKey { get; set; }
}
