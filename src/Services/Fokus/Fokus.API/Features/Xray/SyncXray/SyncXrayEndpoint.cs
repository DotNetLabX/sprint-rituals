using Fokus.API.Features.Xray;
using Jira.RestApi;
using Microsoft.Extensions.Options;

namespace Fokus.API.Features.Xray.SyncXray;

public class SyncXrayRequest
{
    public int[] SprintIds { get; set; } = [];
}

public class SyncXrayRequestValidator : Validator<SyncXrayRequest>
{
    public SyncXrayRequestValidator()
    {
        // SprintIds may be empty — empty means "all synced sprints" (server resolves scope)
        RuleForEach(x => x.SprintIds)
            .GreaterThan(0)
            .WithMessage("Each sprint ID must be greater than 0.");
    }
}

public class SyncXrayResponse
{
    public int TestExecutionsSynced { get; set; }
    public int TestRunsSynced { get; set; }
    public int TestSetsSynced { get; set; }
    public string[] Warnings { get; set; } = [];
}

[HttpPost("/api/xray/sync")]
[Tags("Xray")]
[Authorize(Roles = "Admin")]
public class SyncXrayEndpoint(
    AppSettingsRepository settingsRepository,
    SprintRepository sprintRepository,
    IJiraClient jiraClient,
    XrayIssueSyncService xraySyncService,
    IOptions<JiraOptions> jiraOptions)
    : Endpoint<SyncXrayRequest, SyncXrayResponse>
{
    public override async Task HandleAsync(SyncXrayRequest req, CancellationToken ct)
    {
        var settings = await settingsRepository.GetAsync(ct);
        if (!settings.XrayEnabled)
            throw new BadRequestException("Xray is not enabled.");

        var projectKey = jiraOptions.Value.ProjectKey;
        if (string.IsNullOrEmpty(projectKey))
            throw new BadRequestException("Jira ProjectKey is not configured. Set it in appsettings.json under Jira:ProjectKey.");

        // Empty SprintIds means "all synced sprints"
        List<Sprint> sprints;
        if (req.SprintIds.Length == 0)
            sprints = await sprintRepository.GetAllAsync(ct);
        else
            sprints = await sprintRepository.GetSprintsWithMembershipsAsync(req.SprintIds.ToList(), ct);

        if (sprints.Count == 0)
            throw new BadRequestException("No synced sprints found. Run a Jira sync first.");

        // Re-fetch fresh JiraIssue data for each sprint (issue links live on Jira DTO, not stored Ticket)
        var allJiraIssues = new List<JiraIssue>();
        foreach (var sprint in sprints)
        {
            var issues = await jiraClient.GetSprintIssuesAsync(sprint.Id, ct);
            allJiraIssues.AddRange(issues);
        }

        // Deduplicate — a ticket may appear in multiple sprints
        allJiraIssues = allJiraIssues.DistinctBy(i => i.Key).ToList();

        // Xray-driven TE discovery for the full project
        var teResult = await xraySyncService.SyncTestExecutionsForProjectAsync(projectKey, ct);

        // Jira-driven TestSet discovery — TestSets are only discoverable from Jira issue links
        var testSetsSynced = await xraySyncService.SyncTestSetsFromIssuesAsync(allJiraIssues, ct);

        await SendOkAsync(new SyncXrayResponse
        {
            TestExecutionsSynced = teResult.TestExecutionsSynced,
            TestRunsSynced = teResult.TestRunsSynced,
            TestSetsSynced = testSetsSynced,
            Warnings = teResult.Warnings.ToArray()
        }, ct);
    }
}
