using Blocks.Contracts.Grpc;
using Blocks.Contracts.Grpc.DataContracts;
using Blocks.Contracts.IntegrationEvents;
using Fokus.API.Features.Xray;
using Jira.Contracts;
using MassTransit;

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
    IPublishEndpoint publishEndpoint,
    IPersonService personService,
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

                var sprint = JiraDomainMapper.MapSprint(jiraSprint, boardName);
                await sprintRepository.UpsertAsync(sprint, ct);
                await sprintRepository.SaveChangesAsync(ct);

                var result = await SyncAsync(sprint, issues, forcedNotCommitted, settings.Sync.PlanningWindowDays, ct);

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

                // Publish integration event — one per successfully-synced sprint
                await publishEndpoint.Publish(new SprintSynced(
                    sprint.Id,
                    sprint.Name,
                    sprint.BoardId,
                    sprint.SyncedAt,
                    sprint.State.ToString()), ct);
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
                    var ticket = JiraDomainMapper.MapTicket(issue);
                    await ticketRepository.UpsertAsync(ticket, ct);

                    var developer = JiraDomainMapper.MapDeveloper(issue);
                    if (developer is not null)
                    {
                        await developerRepository.UpsertAsync(developer, ct);
                        await UpsertPersonBestEffortAsync(developer, ct);
                    }
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
            var ticket = JiraDomainMapper.MapTicket(issue);
            await ticketRepository.UpsertAsync(ticket, ct);

            var developer = JiraDomainMapper.MapDeveloper(issue);
            if (developer is not null)
            {
                await developerRepository.UpsertAsync(developer, ct);
                uniqueDeveloperIds.Add(developer.Id);

                // Reconcile developer as a Person in Identity (best-effort — gRPC failure must not abort sync).
                await UpsertPersonBestEffortAsync(developer, ct);
            }

            var transitions = JiraDomainMapper.MapStatusTransitions(issue);
            await ticketRepository.ReplaceTransitionsAsync(issue.Key, transitions, ct);

            var membership = JiraDomainMapper.MapSprintMembership(issue, sprint, forcedNotCommitted, planningWindowDays);
            memberships.Add(membership);
        }

        await ticketRepository.SaveChangesAsync(ct);

        await sprintRepository.UpsertMembershipsAsync(sprint.Id, memberships, ct);
        await sprintRepository.SaveChangesAsync(ct);

        // Piggyback Xray TestSet sync if enabled — TE discovery is project-wide via dedicated endpoint
        XraySyncResult? xrayResult = null;
        var settings2 = await appSettingsRepository.GetAsync(ct);
        if (settings2.Xray.Enabled)
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

    /// <summary>
    /// Upserts a Jira-sourced developer as a Person in Identity via gRPC.
    /// Best-effort: any gRPC or transport failure is swallowed and logged — a Person reconciliation
    /// failure must never abort the sprint sync (the Fokus data is already saved).
    /// </summary>
    private async Task UpsertPersonBestEffortAsync(Developer developer, CancellationToken ct)
    {
        try
        {
            var (firstName, lastName) = SplitDisplayName(developer.DisplayName);
            await personService.UpsertPersonAsync(new UpsertPersonRequest
            {
                ExternalId = developer.Id,
                Source = "Jira",
                Email = developer.Email,
                FirstName = firstName,
                LastName = lastName,
                PictureUrl = developer.AvatarUrl,
            }, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Person upsert failed for developer {DeveloperId} — sync continues", developer.Id);
        }
    }

    private static (string firstName, string lastName) SplitDisplayName(string displayName)
    {
        var idx = displayName.IndexOf(' ');
        return idx >= 0
            ? (displayName[..idx], displayName[(idx + 1)..])
            : (displayName, string.Empty);
    }
}
