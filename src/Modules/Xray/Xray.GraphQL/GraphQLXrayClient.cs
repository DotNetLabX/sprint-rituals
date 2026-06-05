using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Blocks.Exceptions;
using Xray.Contracts;

namespace Xray.GraphQL;

public class GraphQLXrayClient(HttpClient httpClient, XrayBearerTokenManager tokenManager, XrayRateLimiter rateLimiter) : IXrayClient
{
    private const string GraphQLEndpoint = "/api/v2/graphql";
    private const int PageSize = 100;

    public async Task<string> AuthenticateAsync(string clientId, string clientSecret, CancellationToken ct)
    {
        return await tokenManager.GetTokenAsync(clientId, clientSecret, ct);
    }

    public async Task ProbeAsync(string bearerToken, CancellationToken ct)
    {
        // Minimal getTestExecutions query — limit 1, no JQL filter — verifies GraphQL access
        const string probeQuery = """{"query":"{ getTestExecutions(limit: 1, start: 0) { total } }"}""";
        await PostGraphQLAsync(bearerToken, probeQuery, ct);
    }

    public async Task<XrayTestExecutionResult> GetTestExecutionsAsync(string bearerToken, List<string> issueKeys, CancellationToken ct)
    {
        var result = new XrayTestExecutionResult();

        if (issueKeys.Count == 0)
            return result;

        // Batch into groups of 100 due to JQL constraint
        var batches = issueKeys
            .Select((key, index) => new { key, index })
            .GroupBy(x => x.index / PageSize)
            .Select(g => g.Select(x => x.key).ToList())
            .ToList();

        foreach (var batch in batches)
        {
            var batchResult = await FetchTestExecutionsBatchAsync(bearerToken, batch, ct);
            result.TestExecutions.AddRange(batchResult);
        }

        return result;
    }

    private async Task<List<XrayTestExecutionDto>> FetchTestExecutionsBatchAsync(
        string bearerToken,
        List<string> issueKeys,
        CancellationToken ct)
    {
        var allExecutions = new List<XrayTestExecutionDto>();
        var start = 0;
        int total;

        do
        {
            await EnforceRateLimitAsync(ct);

            var jql = $"issue in ({string.Join(", ", issueKeys)})";
            var query = BuildTestExecutionsQuery(jql, start, PageSize);

            var response = await PostGraphQLAsync(bearerToken, query, ct);
            var parsed = ParseTestExecutionsResponse(response);

            allExecutions.AddRange(parsed.executions);
            total = parsed.total;
            start += parsed.executions.Count;

        } while (start < total);

        return allExecutions;
    }

    private static readonly Regex ProjectKeyFormat = new(@"^[A-Z0-9_]+$", RegexOptions.Compiled);

    public async Task<XrayTestExecutionResult> GetAllProjectTestExecutionsAsync(
        string bearerToken,
        string projectKey,
        CancellationToken ct)
    {
        if (!ProjectKeyFormat.IsMatch(projectKey))
            throw new BadRequestException($"Invalid Jira project key format: '{projectKey}'. Expected uppercase letters, digits, and underscores only.");

        var allExecutions = new List<XrayTestExecutionDto>();
        var start = 0;
        int total;

        do
        {
            await EnforceRateLimitAsync(ct);

            var query = BuildProjectTestExecutionsQuery(projectKey, start, PageSize);
            var response = await PostGraphQLAsync(bearerToken, query, ct);
            var parsed = ParseProjectTestExecutionsResponse(response);

            allExecutions.AddRange(parsed.executions);
            total = parsed.total;
            start += parsed.executions.Count;

        } while (start < total);

        return new XrayTestExecutionResult { TestExecutions = allExecutions };
    }

    private static string BuildProjectTestExecutionsQuery(string projectKey, int start, int limit) => $$"""
        {
          "query": "{ getTestExecutions(jql: \"project = {{projectKey}}\", limit: {{limit}}, start: {{start}}) { total results { issueId jira(fields: [\"key\", \"summary\", \"status\", \"assignee\", \"created\", \"issuelinks\"]) testRuns(limit: {{limit}}) { total results { id status { name } startedOn finishedOn assigneeId } } tests(limit: {{limit}}) { total results { jira(fields: [\"key\", \"issuelinks\"]) } } } } }"
        }
        """;

    private static (List<XrayTestExecutionDto> executions, int total) ParseProjectTestExecutionsResponse(string json)
    {
        var executions = new List<XrayTestExecutionDto>();
        int total = 0;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("errors", out var errors) && errors.GetArrayLength() > 0)
            {
                var errorMsg = errors[0].TryGetProperty("message", out var msg) ? msg.GetString() : "Unknown GraphQL error";
                throw new BadGatewayException($"Xray GraphQL error: {errorMsg}");
            }

            if (!root.TryGetProperty("data", out var data)) return (executions, total);
            if (!data.TryGetProperty("getTestExecutions", out var getTE)) return (executions, total);

            if (getTE.TryGetProperty("total", out var totalEl))
                total = totalEl.GetInt32();

            if (!getTE.TryGetProperty("results", out var results)) return (executions, total);

            foreach (var teEl in results.EnumerateArray())
            {
                var issueId = teEl.TryGetProperty("issueId", out var idEl) ? idEl.GetString() ?? "" : "";

                string? issueKey = null;
                string? summary = null;
                string? status = null;
                string? assigneeId = null;
                DateTime? createdDate = null;
                var teIssueLinks = new List<XrayJiraIssueLinkDto>();

                if (teEl.TryGetProperty("jira", out var jira))
                {
                    if (jira.TryGetProperty("key", out var keyEl)) issueKey = keyEl.GetString();
                    if (jira.TryGetProperty("summary", out var summaryEl)) summary = summaryEl.GetString();
                    if (jira.TryGetProperty("status", out var statusEl) && statusEl.TryGetProperty("name", out var statusName))
                        status = statusName.GetString();
                    if (jira.TryGetProperty("assignee", out var assigneeEl) && assigneeEl.ValueKind != JsonValueKind.Null)
                    {
                        if (assigneeEl.TryGetProperty("accountId", out var accountIdEl))
                            assigneeId = accountIdEl.GetString();
                    }
                    if (jira.TryGetProperty("created", out var createdEl) && createdEl.ValueKind != JsonValueKind.Null)
                    {
                        var raw = createdEl.GetString();
                        if (raw is not null && DateTimeOffset.TryParse(raw, out var dto))
                            createdDate = dto.UtcDateTime;
                    }
                    if (jira.TryGetProperty("issuelinks", out var issuelinksEl) && issuelinksEl.ValueKind == JsonValueKind.Array)
                        teIssueLinks = ParseJiraIssueLinks(issuelinksEl);
                }

                var testRuns = new List<XrayTestRunDto>();
                if (teEl.TryGetProperty("testRuns", out var testRunsEl) && testRunsEl.TryGetProperty("results", out var runResults))
                {
                    foreach (var runEl in runResults.EnumerateArray())
                    {
                        var runId = runEl.TryGetProperty("id", out var runIdEl) ? runIdEl.GetString() ?? "" : "";
                        string runStatusName = "";
                        if (runEl.TryGetProperty("status", out var runStatusEl) && runStatusEl.TryGetProperty("name", out var runStatusNameEl))
                            runStatusName = runStatusNameEl.GetString() ?? "";

                        DateTime? startedAt = null;
                        if (runEl.TryGetProperty("startedOn", out var startedEl) && startedEl.ValueKind != JsonValueKind.Null)
                            if (startedEl.TryGetDateTime(out var sdt)) startedAt = sdt;

                        DateTime? finishedAt = null;
                        if (runEl.TryGetProperty("finishedOn", out var finishedEl) && finishedEl.ValueKind != JsonValueKind.Null)
                            if (finishedEl.TryGetDateTime(out var fdt)) finishedAt = fdt;

                        string? executedById = null;
                        if (runEl.TryGetProperty("assigneeId", out var execByEl) && execByEl.ValueKind != JsonValueKind.Null)
                            executedById = execByEl.GetString();

                        testRuns.Add(new XrayTestRunDto
                        {
                            Id = runId,
                            StatusName = runStatusName,
                            StartedAt = startedAt,
                            FinishedAt = finishedAt,
                            ExecutedById = executedById
                        });
                    }
                }

                var testCases = new List<XrayTestCaseDto>();
                if (teEl.TryGetProperty("tests", out var testsEl) && testsEl.TryGetProperty("results", out var testResults))
                {
                    foreach (var testEl in testResults.EnumerateArray())
                    {
                        string? tcKey = null;
                        var tcIssueLinks = new List<XrayJiraIssueLinkDto>();

                        if (testEl.TryGetProperty("jira", out var tcJira))
                        {
                            if (tcJira.TryGetProperty("key", out var tcKeyEl)) tcKey = tcKeyEl.GetString();
                            if (tcJira.TryGetProperty("issuelinks", out var tcLinksEl) && tcLinksEl.ValueKind == JsonValueKind.Array)
                                tcIssueLinks = ParseJiraIssueLinks(tcLinksEl);
                        }

                        testCases.Add(new XrayTestCaseDto { IssueKey = tcKey, IssueLinks = tcIssueLinks });
                    }
                }

                executions.Add(new XrayTestExecutionDto
                {
                    IssueId = issueId,
                    IssueKey = issueKey,
                    Summary = summary,
                    Status = status,
                    AssigneeId = assigneeId,
                    CreatedDate = createdDate,
                    TestRuns = testRuns,
                    TestCases = testCases,
                    IssueLinks = teIssueLinks
                });
            }
        }
        catch (JsonException ex)
        {
            throw new BadGatewayException($"Failed to parse Xray GraphQL response: {ex.Message}");
        }

        return (executions, total);
    }

    private static List<XrayJiraIssueLinkDto> ParseJiraIssueLinks(JsonElement issuelinksEl)
    {
        var links = new List<XrayJiraIssueLinkDto>();
        foreach (var linkEl in issuelinksEl.EnumerateArray())
        {
            string linkTypeName = "";
            if (linkEl.TryGetProperty("type", out var typeEl) && typeEl.TryGetProperty("name", out var typeNameEl))
                linkTypeName = typeNameEl.GetString() ?? "";

            string? outwardKey = null;
            if (linkEl.TryGetProperty("outwardIssue", out var outEl) && outEl.ValueKind != JsonValueKind.Null)
                if (outEl.TryGetProperty("key", out var outKeyEl)) outwardKey = outKeyEl.GetString();

            string? inwardKey = null;
            if (linkEl.TryGetProperty("inwardIssue", out var inEl) && inEl.ValueKind != JsonValueKind.Null)
                if (inEl.TryGetProperty("key", out var inKeyEl)) inwardKey = inKeyEl.GetString();

            links.Add(new XrayJiraIssueLinkDto
            {
                LinkTypeName = linkTypeName,
                OutwardIssueKey = outwardKey,
                InwardIssueKey = inwardKey
            });
        }
        return links;
    }

    private Task EnforceRateLimitAsync(CancellationToken ct) =>
        rateLimiter.WaitForSlotAsync(ct);

    private static string BuildTestExecutionsQuery(string jql, int start, int limit)
    {
        return $$"""
        {
          "query": "{ getTestExecutions(jql: \"{{jql}}\", limit: {{limit}}, start: {{start}}) { total results { issueId jira(fields: [\"key\", \"summary\", \"status\", \"assignee\", \"created\"]) testRuns(limit: {{limit}}) { total results { id status { name } startedOn finishedOn assigneeId } } } } }"
        }
        """;
    }

    private async Task<string> PostGraphQLAsync(string bearerToken, string queryBody, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, GraphQLEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        request.Content = new StringContent(queryBody, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        var delay = 1000;

        for (var attempt = 0; attempt <= 3; attempt++)
        {
            response = await httpClient.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
                return await response.Content.ReadAsStringAsync(ct);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new UnauthorizedException("Xray bearer token rejected — re-authenticate.");

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests && attempt < 3)
            {
                await Task.Delay(delay, ct);
                delay *= 2;
                // Recreate request for retry
                request = new HttpRequestMessage(HttpMethod.Post, GraphQLEndpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
                request.Content = new StringContent(queryBody, Encoding.UTF8, "application/json");
                continue;
            }

            var errorBody = await response.Content.ReadAsStringAsync(ct);
            throw new BadGatewayException($"Xray GraphQL returned {(int)response.StatusCode}: {errorBody[..Math.Min(200, errorBody.Length)]}");
        }

        throw new BadGatewayException("Xray GraphQL rate limit exceeded after retries.");
    }

    private static (List<XrayTestExecutionDto> executions, int total) ParseTestExecutionsResponse(string json)
    {
        var executions = new List<XrayTestExecutionDto>();
        int total = 0;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Check for GraphQL errors
            if (root.TryGetProperty("errors", out var errors) && errors.GetArrayLength() > 0)
            {
                var errorMsg = errors[0].TryGetProperty("message", out var msg) ? msg.GetString() : "Unknown GraphQL error";
                throw new BadGatewayException($"Xray GraphQL error: {errorMsg}");
            }

            if (!root.TryGetProperty("data", out var data)) return (executions, total);
            if (!data.TryGetProperty("getTestExecutions", out var getTE)) return (executions, total);

            if (getTE.TryGetProperty("total", out var totalEl))
                total = totalEl.GetInt32();

            if (!getTE.TryGetProperty("results", out var results)) return (executions, total);

            foreach (var teEl in results.EnumerateArray())
            {
                var issueId = teEl.TryGetProperty("issueId", out var idEl) ? idEl.GetString() ?? "" : "";

                string? issueKey = null;
                string? summary = null;
                string? status = null;
                string? assigneeId = null;
                DateTime? createdDate = null;

                if (teEl.TryGetProperty("jira", out var jira))
                {
                    if (jira.TryGetProperty("key", out var keyEl)) issueKey = keyEl.GetString();
                    if (jira.TryGetProperty("summary", out var summaryEl)) summary = summaryEl.GetString();
                    if (jira.TryGetProperty("status", out var statusEl) && statusEl.TryGetProperty("name", out var statusName))
                        status = statusName.GetString();
                    if (jira.TryGetProperty("assignee", out var assigneeEl) && assigneeEl.ValueKind != JsonValueKind.Null)
                    {
                        if (assigneeEl.TryGetProperty("accountId", out var accountIdEl))
                            assigneeId = accountIdEl.GetString();
                    }
                    if (jira.TryGetProperty("created", out var createdEl) && createdEl.ValueKind != JsonValueKind.Null)
                    {
                        var raw = createdEl.GetString();
                        if (raw is not null && DateTimeOffset.TryParse(raw, out var dto))
                            createdDate = dto.UtcDateTime;
                    }
                }

                var testRuns = new List<XrayTestRunDto>();
                if (teEl.TryGetProperty("testRuns", out var testRunsEl))
                {
                    if (testRunsEl.TryGetProperty("results", out var runResults))
                    {
                        foreach (var runEl in runResults.EnumerateArray())
                        {
                            var runId = runEl.TryGetProperty("id", out var runIdEl) ? runIdEl.GetString() ?? "" : "";
                            string statusName2 = "";
                            if (runEl.TryGetProperty("status", out var runStatusEl) && runStatusEl.TryGetProperty("name", out var runStatusName))
                                statusName2 = runStatusName.GetString() ?? "";

                            DateTime? startedAt = null;
                            if (runEl.TryGetProperty("startedOn", out var startedEl) && startedEl.ValueKind != JsonValueKind.Null)
                                if (startedEl.TryGetDateTime(out var sdt)) startedAt = sdt;

                            DateTime? finishedAt = null;
                            if (runEl.TryGetProperty("finishedOn", out var finishedEl) && finishedEl.ValueKind != JsonValueKind.Null)
                                if (finishedEl.TryGetDateTime(out var fdt)) finishedAt = fdt;

                            string? executedById = null;
                            if (runEl.TryGetProperty("assigneeId", out var execByEl) && execByEl.ValueKind != JsonValueKind.Null)
                                executedById = execByEl.GetString();

                            testRuns.Add(new XrayTestRunDto
                            {
                                Id = runId,
                                StatusName = statusName2,
                                StartedAt = startedAt,
                                FinishedAt = finishedAt,
                                ExecutedById = executedById
                            });
                        }
                    }
                }

                executions.Add(new XrayTestExecutionDto
                {
                    IssueId = issueId,
                    IssueKey = issueKey,
                    Summary = summary,
                    Status = status,
                    AssigneeId = assigneeId,
                    CreatedDate = createdDate,
                    TestRuns = testRuns
                });
            }
        }
        catch (JsonException ex)
        {
            throw new BadGatewayException($"Failed to parse Xray GraphQL response: {ex.Message}");
        }

        return (executions, total);
    }
}
