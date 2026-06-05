namespace Jira.Contracts;

public class JiraPagedResult<T>
{
    public int MaxResults { get; set; }
    public int StartAt { get; set; }
    public int Total { get; set; }
    public bool IsLast { get; set; }
    public List<T> Values { get; set; } = [];
}

public class JiraIssuePagedResult
{
    public int MaxResults { get; set; }
    public int StartAt { get; set; }
    public int Total { get; set; }
    public string? NextPageToken { get; set; }
    public List<JiraIssue> Issues { get; set; } = [];
}
