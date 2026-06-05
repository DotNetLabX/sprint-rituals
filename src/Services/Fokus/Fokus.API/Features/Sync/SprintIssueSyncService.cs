using Fokus.API.Features.Xray;
using Jira.Contracts;

namespace Fokus.API.Features.Sync;

public record SprintIssueSyncResult(int TicketsUpserted, HashSet<string> DeveloperIds, XraySyncResult? XrayResult = null);

public record SprintBatchSyncResult(
    int SprintsSynced,
    int TicketsUpserted,
    int DevelopersDiscovered,
    List<SprintSyncFailure> Failures,
    int XrayTestExecutionsSynced = 0,
    int XrayTestRunsSynced = 0,
    int XrayTestSetsSynced = 0,
    List<string>? XrayWarnings = null);

public record SprintSyncFailure(int SprintId, string Error);

public record EpicDiscoveryResult(int TicketsDiscovered, int Failures);

public class SprintIssueSyncService(
    IJiraClient jiraClient,
    SprintRepository sprintRepository,
    TicketRepository ticketRepository,
    DeveloperRepository developerRepository,
    AppSettingsRepository appSettingsRepository,
    XrayIssueSyncService xrayIssueSyncService,
    ILogger<SprintIssueSyncService> logger)
{
    public async Task<SprintBatchSyncResult> SyncSprintsFromJiraAsync(
        IReadOnlyList<JiraSprint> jiraSprints,
        string boardName,
        bool forcedNotCommitted,
        CancellationToken ct)
    {
        var settings = await appSettingsRepository.GetAsync(ct);
        var sprintsSynced = 0;
        var totalTickets = 0;
        var allDeveloperIds = new HashSet<string>();
        var failures = new List<SprintSyncFailure>();
        var xrayTeSynced = 0;
        var xrayRunSynced = 0;
        var xraySetSynced = 0;
        var xrayWarnings = new List<string>();

        foreach (var jiraSprint in jiraSprints)
        {
            try
            {
                var issues = await jiraClient.GetSprintIssuesAsync(jiraSprint.Id, ct);
                logger.LogWarning("Sprint {SprintId} ({SprintName}): {IssueCount} issues returned by {ClientType}",
                    jiraSprint.Id, jiraSprint.Name, issues.Count, jiraClient.GetType().Name);

                var sprint = Sprint.FromJira(jiraSprint, boardName);
                await sprintRepository.UpsertAsync(sprint, ct);
                await sprintRepository.SaveChangesAsync(ct);

                var result = await SyncAsync(sprint, issues, forcedNotCommitted, settings.PlanningWindowDays, ct);

                sprintsSynced++;
                totalTickets += result.TicketsUpserted;
                allDeveloperIds.UnionWith(result.DeveloperIds);

                if (result.XrayResult is not null)
                {
                    xrayTeSynced += result.XrayResult.TestExecutionsSynced;
                    xrayRunSynced += result.XrayResult.TestRunsSynced;
                    xraySetSynced += result.XrayResult.TestSetsSynced;
                    xrayWarnings.AddRange(result.XrayResult.Warnings);
                }
            }
            catch (Exception ex)
            {
                failures.Add(new SprintSyncFailure(jiraSprint.Id, $"{ex.Message} | {ex.StackTrace}"));
            }
        }

        return new SprintBatchSyncResult(
            sprintsSynced,
            totalTickets,
            allDeveloperIds.Count,
            failures,
            xrayTeSynced,
            xrayRunSynced,
            xraySetSynced,
            xrayWarnings);
    }

    public async Task<EpicDiscoveryResult> SyncEpicDiscoveryAsync(
        CancellationToken ct)
    {
        var epicKeys = await ticketRepository.GetAllKeysWithEpicAsync(ct);
        var ticketsDiscovered = 0;
        var failures = 0;

        foreach (var epicKey in epicKeys)
        {
            try
            {
                var epicIssues = await jiraClient.GetEpicIssuesAsync(epicKey, ct);
                var issueKeys = epicIssues.Select(i => i.Key).ToList();
                var existingKeys = await ticketRepository.GetExistingKeysAsync(issueKeys, ct);

                var newIssues = epicIssues.Where(i => !existingKeys.Contains(i.Key)).ToList();

                foreach (var issue in newIssues)
                {
                    var ticket = Ticket.FromJira(issue);
                    await ticketRepository.UpsertAsync(ticket, ct);

                    var developer = Developer.FromJira(issue);
                    if (developer is not null)
                        await developerRepository.UpsertAsync(developer, ct);
                }

                await ticketRepository.SaveChangesAsync(ct);

                ticketsDiscovered += newIssues.Count;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to sync epic {EpicKey}", epicKey);
                failures++;
            }
        }

        return new EpicDiscoveryResult(ticketsDiscovered, failures);
    }

    public async Task<SprintIssueSyncResult> SyncAsync(
        Sprint sprint,
        IReadOnlyList<JiraIssue> issues,
        bool forcedNotCommitted,
        int planningWindowDays,
        CancellationToken ct)
    {
        var memberships = new List<SprintMembership>();
        var uniqueDeveloperIds = new HashSet<string>();

        foreach (var issue in issues)
        {
            var ticket = Ticket.FromJira(issue);
            await ticketRepository.UpsertAsync(ticket, ct);

            var developer = Developer.FromJira(issue);
            if (developer is not null)
            {
                await developerRepository.UpsertAsync(developer, ct);
                uniqueDeveloperIds.Add(developer.Id);
            }

            var transitions = StatusTransition.ListFromJira(issue);
            await ticketRepository.ReplaceTransitionsAsync(issue.Key, transitions, ct);

            var membership = SprintMembership.FromJira(issue, sprint, forcedNotCommitted, planningWindowDays);
            memberships.Add(membership);
        }

        await ticketRepository.SaveChangesAsync(ct);

        await sprintRepository.UpsertMembershipsAsync(sprint.Id, memberships, ct);
        await sprintRepository.SaveChangesAsync(ct);

        // Piggyback Xray TestSet sync if enabled — TE discovery is project-wide via dedicated endpoint
        XraySyncResult? xrayResult = null;
        var settings2 = await appSettingsRepository.GetAsync(ct);
        if (settings2.XrayEnabled)
        {
            try
            {
                var testSetsSynced = await xrayIssueSyncService.SyncTestSetsFromIssuesAsync(issues.ToList(), ct);
                xrayResult = new XraySyncResult(0, 0, testSetsSynced, []);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Xray piggyback sync failed for sprint {SprintId}", sprint.Id);
                xrayResult = new XraySyncResult(0, 0, 0, [$"Xray sync failed: {ex.Message}"]);
            }
        }

        return new SprintIssueSyncResult(issues.Count, uniqueDeveloperIds, xrayResult);
    }
}
