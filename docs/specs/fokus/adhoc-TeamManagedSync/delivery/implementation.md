# TeamManagedSync — Implementation

## Files Modified

- `src/Modules/Jira/Jira.RestApi/JiraOptions.cs` — added `IsTeamManaged` (bool, default false) and `ProjectKey` (string?, optional) properties
- `src/Modules/Jira/Jira.Contracts/JiraChangelog.cs` — added `MaxResults`, `Total`, `StartAt` properties for truncation detection
- `src/Modules/Jira/Jira.RestApi/IJiraApi.cs` — added `GetIssueChangelogPageAsync` endpoint returning `JiraPagedResult<JiraHistory>`
- `src/Modules/Jira/Jira.RestApi/RestApiJiraClient.cs` — removed try/fallback from `GetSprintIssuesAsync` (reverted to pure Agile API); made `GetSprintIssuesAsync`, `GetBoardBacklogIssuesAsync`, `GetEpicIssuesAsync` virtual; added `protected IJiraApi Api` property; made `RequestAsync` and `SearchAllIssuesAsync` protected; added `GetFullChangelogAsync` and `EnrichChangelogsAsync` protected helpers; updated all internal `api` field references to use `Api` property
- `src/Modules/Jira/Jira.RestApi/DependencyInjection.cs` — replaced `AddTransient<IJiraClient, RestApiJiraClient>()` with a factory that reads `JiraOptions.IsTeamManaged` and registers either `TeamManagedJiraClient` or `RestApiJiraClient`
- `docs/jira-team-managed-workarounds.md` — corrected changelog cap (100 items on Cloud, not 20); documented inheritance architecture; documented changelog enrichment strategy; documented `IsTeamManaged`/`ProjectKey` configuration; added epic deprecation note (May 2026 / November 2026); added Agile API deprecation migration note

## Files Created

- `src/Modules/Jira/Jira.RestApi/TeamManagedJiraClient.cs` — subclass of `RestApiJiraClient` with JQL overrides for `GetSprintIssuesAsync`, `GetEpicIssuesAsync`, `GetBoardBacklogIssuesAsync`; changelog enrichment applied after every JQL fetch; epic JQL tries `"Epic Link"` first and falls back to `parent` on failure; backlog guard throws `BadRequestException` when `ProjectKey` is null/empty

## Key Decisions

- `ILogger<TeamManagedJiraClient>` added to `TeamManagedJiraClient` constructor for epic JQL fallback debug logging; `Microsoft.Extensions.Logging` is available transitively via `Refit.HttpClientFactory` — no new `PackageReference` needed
- `GetEpicIssuesAsync` catches `BadGatewayException` (not a specific JQL parse error type) for the `"Epic Link"` fallback, since `RequestAsync` maps all non-success Jira API responses to `BadGatewayException`. This matches the plan's intent of catching JQL parse errors.
- The primary constructor parameter `api` in `RestApiJiraClient` is now used only to initialize `protected IJiraApi Api { get; } = api;` — all internal calls use `Api`. This enables the subclass to share the same Refit client instance without field duplication.

## Deviations from Plan

None.
