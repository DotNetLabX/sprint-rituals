using Jira.Contracts;
using Refit;

namespace Jira.RestApi;

public interface IJiraApi
{
    [Get("/rest/agile/1.0/board")]
    Task<IApiResponse<JiraPagedResult<JiraBoard>>> GetBoardsPageAsync(int startAt, int maxResults, CancellationToken ct);

    [Get("/rest/api/3/status")]
    Task<IApiResponse<List<JiraStatus>>> GetStatusesAsync(CancellationToken ct);

    [Get("/rest/agile/1.0/board/{boardId}/sprint")]
    Task<IApiResponse<JiraPagedResult<JiraSprint>>> GetSprintsPageAsync(int boardId, int startAt, int maxResults, [Query] string? state, CancellationToken ct);

    [Get("/rest/agile/1.0/sprint/{sprintId}/issue")]
    Task<IApiResponse<JiraIssuePagedResult>> GetSprintIssuesPageAsync(int sprintId, int startAt, int maxResults, string expand, string fields, CancellationToken ct);

    [Get("/rest/agile/1.0/board/{boardId}/backlog")]
    Task<IApiResponse<JiraIssuePagedResult>> GetBoardBacklogIssuesPageAsync(int boardId, int startAt, int maxResults, string expand, string fields, CancellationToken ct);

    [Get("/rest/agile/1.0/epic/{epicKey}/issue")]
    Task<IApiResponse<JiraIssuePagedResult>> GetEpicIssuesPageAsync(string epicKey, int startAt, int maxResults, string expand, string fields, CancellationToken ct);

    [Get("/rest/api/3/search/jql")]
    Task<IApiResponse<JiraIssuePagedResult>> SearchIssuesAsync([Query] string jql, [Query] int maxResults, [Query] string expand, [Query] string fields, [Query] string? nextPageToken, CancellationToken ct);

    [Get("/rest/api/3/issue/{issueIdOrKey}/changelog")]
    Task<IApiResponse<JiraPagedResult<JiraHistory>>> GetIssueChangelogPageAsync(string issueIdOrKey, [Query] int startAt, [Query] int maxResults, CancellationToken ct);

    [Post("/rest/agile/1.0/sprint/{sprintId}")]
    Task<IApiResponse<JiraSprint>> UpdateSprintAsync(int sprintId, [Body] UpdateJiraSprintRequest request, CancellationToken ct);
}
