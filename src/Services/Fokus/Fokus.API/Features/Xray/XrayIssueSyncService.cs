using Microsoft.Extensions.Logging;
using Xray.Contracts;

namespace Fokus.API.Features.Xray;

public record XraySyncResult(
    int TestExecutionsSynced,
    int TestRunsSynced,
    int TestSetsSynced,
    List<string> Warnings);

public class XrayIssueSyncService(
    IXrayClient xrayClient,
    AppSettingsRepository appSettingsRepository,
    TestExecutionRepository testExecutionRepository,
    TicketRepository ticketRepository,
    DeveloperRepository developerRepository,
    ILogger<XrayIssueSyncService> logger)
{
    public async Task<XraySyncResult> SyncTestExecutionsForProjectAsync(
        string projectKey,
        CancellationToken ct)
    {
        var warnings = new List<string>();

        var settings = await appSettingsRepository.GetAsync(ct);
        if (!settings.XrayEnabled || string.IsNullOrEmpty(settings.XrayClientId) || string.IsNullOrEmpty(settings.XrayClientSecret))
            return new XraySyncResult(0, 0, 0, warnings);

        var bearerToken = await AuthenticateAsync(settings, warnings, ct);
        if (bearerToken is null)
            return new XraySyncResult(0, 0, 0, warnings);

        XrayTestExecutionResult xrayResult;
        try
        {
            xrayResult = await xrayClient.GetAllProjectTestExecutionsAsync(bearerToken, projectKey, ct);
        }
        catch (Exception ex)
        {
            var warning = $"Xray project TE fetch failed: {ex.Message}";
            logger.LogWarning(warning);
            warnings.Add(warning);
            return new XraySyncResult(0, 0, 0, warnings);
        }

        if (xrayResult.TestExecutions.Count == 0)
            return new XraySyncResult(0, 0, 0, warnings);

        // Collect all candidate ticket keys referenced in issuelinks across TEs and their test cases
        var candidateKeys = xrayResult.TestExecutions
            .SelectMany(te => te.IssueLinks.Select(l => l.OutwardIssueKey ?? l.InwardIssueKey)
                .Concat(te.TestCases.SelectMany(tc => tc.IssueLinks.Select(l => l.OutwardIssueKey ?? l.InwardIssueKey))))
            .Where(k => !string.IsNullOrEmpty(k))
            .Select(k => k!)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var knownTicketKeys = await ticketRepository.GetExistingKeysAsync(candidateKeys, ct);

        var teLinkMap = BuildLinkMap(xrayResult.TestExecutions, knownTicketKeys);

        var teIssueKeys = xrayResult.TestExecutions
            .Select(te => te.IssueKey ?? te.IssueId)
            .Where(k => !string.IsNullOrEmpty(k))
            .ToList();

        var xrayTeByKey = xrayResult.TestExecutions.ToDictionary(t => t.IssueKey ?? t.IssueId, t => t);

        var (testExecutionsSynced, testRunsSynced) = await SyncTestExecutionsAsync(
            teIssueKeys, teLinkMap, xrayTeByKey, warnings, ct);

        return new XraySyncResult(testExecutionsSynced, testRunsSynced, 0, warnings);
    }

    public async Task<int> SyncTestSetsFromIssuesAsync(
        List<JiraIssue> jiraIssues,
        CancellationToken ct)
    {
        var warnings = new List<string>();

        var settings = await appSettingsRepository.GetAsync(ct);
        if (!settings.XrayEnabled)
            return 0;

        var testSets = new Dictionary<string, (string Id, string Key, string Summary, string? AssigneeId, string Status)>();

        foreach (var issue in jiraIssues)
        {
            if (issue.Fields.Issuelinks is null) continue;

            foreach (var link in issue.Fields.Issuelinks)
            {
                var linkTypeName = link.Type?.Name ?? "";
                if (!linkTypeName.Equals("Test", StringComparison.OrdinalIgnoreCase)) continue;

                var linkedIssue = link.OutwardIssue ?? link.InwardIssue;
                if (linkedIssue is null) continue;

                var linkedType = linkedIssue.Fields.Issuetype?.Name ?? "";
                if (!linkedType.Equals("Test Set", StringComparison.OrdinalIgnoreCase)) continue;

                var tsKey = linkedIssue.Key;
                if (!testSets.ContainsKey(tsKey))
                {
                    testSets[tsKey] = (
                        linkedIssue.Id,
                        tsKey,
                        linkedIssue.Fields.Summary,
                        linkedIssue.Fields.Assignee?.AccountId,
                        linkedIssue.Fields.Status?.Name ?? "Unknown"
                    );
                }
            }
        }

        return await SyncTestSetsAsync(testSets, warnings, ct);
    }

    private static Dictionary<string, List<(string TicketKey, TestExecutionLinkType LinkType)>> BuildLinkMap(
        List<XrayTestExecutionDto> executions,
        HashSet<string> knownTicketKeys)
    {
        var linkMap = new Dictionary<string, List<(string TicketKey, TestExecutionLinkType LinkType)>>(StringComparer.OrdinalIgnoreCase);

        foreach (var te in executions)
        {
            var teKey = te.IssueKey ?? te.IssueId;
            if (string.IsNullOrEmpty(teKey)) continue;

            if (!linkMap.ContainsKey(teKey))
                linkMap[teKey] = [];

            // Test case issuelinks: "Test" link type → Tests (test-case-mediated coverage)
            foreach (var tc in te.TestCases)
            {
                foreach (var link in tc.IssueLinks)
                {
                    if (!link.LinkTypeName.Equals("Test", StringComparison.OrdinalIgnoreCase)) continue;
                    var ticketKey = link.OutwardIssueKey ?? link.InwardIssueKey;
                    if (string.IsNullOrEmpty(ticketKey) || !knownTicketKeys.Contains(ticketKey)) continue;
                    if (!linkMap[teKey].Any(l => l.TicketKey == ticketKey && l.LinkType == TestExecutionLinkType.Tests))
                        linkMap[teKey].Add((ticketKey, TestExecutionLinkType.Tests));
                }
            }

            // TE-level issuelinks: "Test" → Tests, "Blocks" → Blocks
            foreach (var link in te.IssueLinks)
            {
                var isTest = link.LinkTypeName.Equals("Test", StringComparison.OrdinalIgnoreCase);
                var isBlocks = link.LinkTypeName.Equals("Blocks", StringComparison.OrdinalIgnoreCase);
                if (!isTest && !isBlocks) continue;

                var ticketKey = link.OutwardIssueKey ?? link.InwardIssueKey;
                if (string.IsNullOrEmpty(ticketKey) || !knownTicketKeys.Contains(ticketKey)) continue;

                var linkType = isTest ? TestExecutionLinkType.Tests : TestExecutionLinkType.Blocks;
                if (!linkMap[teKey].Any(l => l.TicketKey == ticketKey && l.LinkType == linkType))
                    linkMap[teKey].Add((ticketKey, linkType));
            }
        }

        return linkMap;
    }

    private async Task<string?> AuthenticateAsync(AppSettings settings, List<string> warnings, CancellationToken ct)
    {
        try
        {
            return await xrayClient.AuthenticateAsync(settings.XrayClientId!, settings.XrayClientSecret!, ct);
        }
        catch (Exception ex)
        {
            var warning = $"Xray authentication failed: {ex.Message}";
            logger.LogWarning(warning);
            warnings.Add(warning);
            return null;
        }
    }

    private async Task<(int TestExecutionsSynced, int TestRunsSynced)> SyncTestExecutionsAsync(
        List<string> teIssueKeys,
        Dictionary<string, List<(string TicketKey, TestExecutionLinkType LinkType)>> teLinkMap,
        Dictionary<string, XrayTestExecutionDto> xrayTeByKey,
        List<string> warnings,
        CancellationToken ct)
    {
        var testExecutionsSynced = 0;
        var testRunsSynced = 0;

        foreach (var teKey in teIssueKeys)
        {
            try
            {
                xrayTeByKey.TryGetValue(teKey, out var xrayTe);

                var issueId = xrayTe?.IssueId;
                if (string.IsNullOrEmpty(issueId))
                {
                    warnings.Add($"Could not determine issue ID for Test Execution {teKey} — skipped.");
                    continue;
                }

                var runsCount = await SyncSingleTestExecutionAsync(teKey, issueId, xrayTe, teLinkMap, ct);
                testExecutionsSynced++;
                testRunsSynced += runsCount;
            }
            catch (Exception ex)
            {
                var warning = $"Failed to sync Test Execution {teKey}: {ex.Message}";
                logger.LogWarning(warning);
                warnings.Add(warning);
            }
        }

        return (testExecutionsSynced, testRunsSynced);
    }

    private async Task<int> SyncSingleTestExecutionAsync(
        string teKey,
        string issueId,
        XrayTestExecutionDto? xrayTe,
        Dictionary<string, List<(string TicketKey, TestExecutionLinkType LinkType)>> teLinkMap,
        CancellationToken ct)
    {
        var assigneeId = xrayTe?.AssigneeId;
        if (!string.IsNullOrEmpty(assigneeId))
            await UpsertDeveloperFromIdAsync(assigneeId, ct);

        var te = TestExecution.FromXray(
            issueId: issueId,
            issueKey: teKey,
            summary: xrayTe?.Summary ?? teKey,
            status: xrayTe?.Status ?? "Unknown",
            assigneeId: assigneeId,
            createdDate: xrayTe?.CreatedDate ?? DateTime.UtcNow
        );

        await testExecutionRepository.UpsertAsync(te, ct);
        await testExecutionRepository.SaveChangesAsync(ct);

        var links = new List<TestExecutionLink>();
        if (teLinkMap.TryGetValue(teKey, out var ticketLinks))
        {
            foreach (var (ticketKey, linkType) in ticketLinks)
                links.Add(new TestExecutionLink { TestExecutionIssueId = issueId, TicketKey = ticketKey, LinkType = linkType });
        }

        await testExecutionRepository.ReplaceLinksAsync(issueId, links, ct);
        await testExecutionRepository.SaveChangesAsync(ct);

        var runs = new List<TestRun>();
        if (xrayTe?.TestRuns is not null)
        {
            foreach (var runDto in xrayTe.TestRuns)
            {
                if (!string.IsNullOrEmpty(runDto.ExecutedById))
                    await UpsertDeveloperFromIdAsync(runDto.ExecutedById, ct);

                runs.Add(TestRun.FromXray(
                    id: runDto.Id,
                    testExecutionIssueId: issueId,
                    status: ParseTestRunStatus(runDto.StatusName),
                    statusName: runDto.StatusName,
                    startedAt: runDto.StartedAt,
                    finishedAt: runDto.FinishedAt,
                    executedById: runDto.ExecutedById
                ));
            }
        }

        await testExecutionRepository.ReplaceTestRunsAsync(issueId, runs, ct);
        await testExecutionRepository.SaveChangesAsync(ct);

        return runs.Count;
    }

    private async Task<int> SyncTestSetsAsync(
        Dictionary<string, (string Id, string Key, string Summary, string? AssigneeId, string Status)> testSets,
        List<string> warnings,
        CancellationToken ct)
    {
        var testSetsSynced = 0;
        foreach (var (_, ts) in testSets)
        {
            try
            {
                if (!string.IsNullOrEmpty(ts.AssigneeId))
                    await UpsertDeveloperFromIdAsync(ts.AssigneeId, ct);

                var testSet = TestSet.FromJiraIssueLink(
                    issueId: ts.Id,
                    issueKey: ts.Key,
                    summary: ts.Summary,
                    assigneeId: ts.AssigneeId,
                    status: ts.Status
                );

                await testExecutionRepository.UpsertTestSetAsync(testSet, ct);
                await testExecutionRepository.SaveChangesAsync(ct);
                testSetsSynced++;
            }
            catch (Exception ex)
            {
                var warning = $"Failed to sync Test Set {ts.Key}: {ex.Message}";
                logger.LogWarning(warning);
                warnings.Add(warning);
            }
        }
        return testSetsSynced;
    }

    private async Task UpsertDeveloperFromIdAsync(string accountId, CancellationToken ct)
    {
        var existing = await developerRepository.GetByIdAsync(accountId, ct);
        if (existing is null)
        {
            var dev = new Developer
            {
                Id = accountId,
                DisplayName = accountId, // Display name not available without separate Jira call
                IsActive = true
            };
            await developerRepository.UpsertAsync(dev, ct);
            await developerRepository.SaveChangesAsync(ct);
        }
    }

    private static TestRunStatus ParseTestRunStatus(string statusName) =>
        statusName.ToUpperInvariant() switch
        {
            "PASS" or "PASSED" => TestRunStatus.Pass,
            "FAIL" or "FAILED" => TestRunStatus.Fail,
            "EXECUTING" or "IN PROGRESS" => TestRunStatus.Executing,
            "ABORTED" or "ABANDONED" => TestRunStatus.Aborted,
            _ => TestRunStatus.Todo
        };
}
