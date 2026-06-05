using Blocks.Exceptions;
using Jira.Contracts;
using Microsoft.Extensions.Logging;

namespace Jira.RestApi;

public class TeamManagedJiraClient(IJiraApi api, string? projectKey, ILogger<TeamManagedJiraClient> logger)
    : RestApiJiraClient(api)
{
    public override async Task<List<JiraIssue>> GetSprintIssuesAsync(int sprintId, CancellationToken ct)
    {
        var issues = await SearchAllIssuesAsync($"sprint = {sprintId}", ct);
        await EnrichChangelogsAsync(issues, ct);
        return issues;
    }

    public override async Task<List<JiraIssue>> GetEpicIssuesAsync(string epicKey, CancellationToken ct)
    {
        List<JiraIssue> issues;
        try
        {
            issues = await SearchAllIssuesAsync($"\"Epic Link\" = {epicKey}", ct);
            if (issues.Count > 0)
            {
                logger.LogDebug("GetEpicIssuesAsync: used 'Epic Link' JQL for {EpicKey}", epicKey);
                await EnrichChangelogsAsync(issues, ct);
                return issues;
            }
        }
        catch (BadGatewayException)
        {
            logger.LogDebug("GetEpicIssuesAsync: 'Epic Link' JQL failed for {EpicKey}, retrying with 'parent'", epicKey);
        }

        issues = await SearchAllIssuesAsync($"parent = {epicKey}", ct);
        logger.LogDebug("GetEpicIssuesAsync: used 'parent' JQL for {EpicKey}", epicKey);
        await EnrichChangelogsAsync(issues, ct);
        return issues;
    }

    public override async Task<List<JiraIssue>> GetBoardBacklogIssuesAsync(int boardId, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(projectKey))
            throw new BadRequestException("ProjectKey is required for team-managed board backlog queries. Set Jira:ProjectKey in configuration.");

        var issues = await SearchAllIssuesAsync($"project = {projectKey} AND sprint is EMPTY", ct);
        await EnrichChangelogsAsync(issues, ct);
        return issues;
    }
}
