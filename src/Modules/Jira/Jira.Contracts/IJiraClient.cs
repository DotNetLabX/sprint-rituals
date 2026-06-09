namespace Jira.Contracts;

public interface IJiraClient
{
    Task<List<JiraBoard>> GetBoardsAsync(CancellationToken ct);
    Task<List<JiraStatus>> GetStatusesAsync(CancellationToken ct);
    Task<List<JiraSprint>> GetSprintsAsync(int boardId, CancellationToken ct, params SprintState[] states);
    Task<List<JiraIssue>> GetSprintIssuesAsync(int sprintId, CancellationToken ct);
    Task<List<JiraIssue>> GetBoardBacklogIssuesAsync(int boardId, CancellationToken ct);
    Task<List<JiraIssue>> GetEpicIssuesAsync(string epicKey, CancellationToken ct);
    Task<JiraSprint> UpdateSprintAsync(int sprintId, string name, DateTime startDate, DateTime endDate, string? goal, CancellationToken ct);

    /// <summary>
    /// Fetches a Jira user by account ID. Returns null when the user is not found or the API call fails.
    /// EmailAddress on the returned JiraUser is null when Jira Cloud restricts visibility.
    /// </summary>
    Task<JiraUser?> GetUserAsync(string accountId, CancellationToken ct);
}
