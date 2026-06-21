# ExtractJiraModule — Implementation

## Files Created

- `src/Modules/Jira/Jira.Contracts/Jira.Contracts.csproj` — references Fokus.JiraContracts + Fokus.Domain
- `src/Modules/Jira/Jira.Contracts/IJiraClient.cs` — interface with 5 methods extracted from JiraClient public surface
- `src/Modules/Jira/Jira.RestApi/Jira.RestApi.csproj` — references Jira.Contracts + Fokus.JiraContracts + Refit.HttpClientFactory
- `src/Modules/Jira/Jira.RestApi/RestApiJiraClient.cs` — concrete implementation of IJiraClient; content from JiraClient.cs with class renamed and namespace updated to Jira.RestApi
- `src/Modules/Jira/Jira.RestApi/IJiraApi.cs` — Refit interface; content from Infrastructure/Jira/IJiraApi.cs with namespace updated to Jira.RestApi
- `src/Modules/Jira/Jira.RestApi/JiraOptions.cs` — options class; content from Infrastructure/Jira/JiraOptions.cs with namespace updated to Jira.RestApi
- `src/Modules/Jira/Jira.RestApi/DependencyInjection.cs` — AddRestApiJira() extension method; registration logic moved verbatim from Fokus.API/DependencyInjection.cs; AddTransient<JiraClient> changed to AddTransient<IJiraClient, RestApiJiraClient>

## Files Modified

- `src/Services/Fokus/Fokus.API/Fokus.API.csproj` — removed Refit.HttpClientFactory package reference; added ProjectReferences to Jira.Contracts and Jira.RestApi
- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` — removed inline Jira DI registration (JiraOptions, Refit client, JiraClient); replaced with services.AddRestApiJira(configuration); removed related usings
- `src/Services/Fokus/Fokus.API/Features/Sync/GetBoards/GetBoardsEndpoint.cs` — using Fokus.API.Infrastructure.Jira → using Jira.Contracts; JiraClient → IJiraClient
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncSprints/SyncSprintsEndpoint.cs` — using Fokus.API.Infrastructure.Jira → using Jira.Contracts; JiraClient → IJiraClient
- `src/Services/Fokus/Fokus.API/Features/Sync/GetJiraSprints/GetJiraSprintsEndpoint.cs` — using Fokus.API.Infrastructure.Jira → using Jira.Contracts; JiraClient → IJiraClient
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncBacklog/SyncBacklogEndpoint.cs` — using Fokus.API.Infrastructure.Jira → using Jira.Contracts; JiraClient → IJiraClient
- `src/Fokus.slnx` — added Modules/Jira solution folder with Jira.Contracts and Jira.RestApi projects (via dotnet sln add)

## Files Deleted

- `src/Services/Fokus/Fokus.API/Infrastructure/Jira/JiraClient.cs`
- `src/Services/Fokus/Fokus.API/Infrastructure/Jira/IJiraApi.cs`
- `src/Services/Fokus/Fokus.API/Infrastructure/Jira/JiraOptions.cs`
- `src/Services/Fokus/Fokus.API/Infrastructure/Jira/` (directory)

## Key Decisions

- Old Step 3 (absorb Fokus.JiraContracts) was abandoned mid-step after discovering a circular dependency: Jira.Contracts → Fokus.Domain → Fokus.JiraContracts, and Fokus.Domain uses JiraIssue/JiraSprint in FromJira() factory methods. Architect resolved: keep Fokus.JiraContracts as BuildingBlock. The 5 absorbed contract files written to Jira.Contracts were deleted before proceeding.
- Jira.RestApi.csproj also references Fokus.JiraContracts directly (not just via Jira.Contracts) because IJiraApi uses JiraPagedResult<T> and JiraIssuePagedResult as return types.
- DI registration uses AddTransient<IJiraClient, RestApiJiraClient> (not AddTransient<RestApiJiraClient>) — the original registered the concrete type directly, but this step wires the interface binding so DI resolves IJiraClient correctly.

## Deviations from Plan

- Step 3 (absorb Fokus.JiraContracts) was replaced by architect with "keep as BuildingBlock" — plan updated accordingly before implementation continued. All remaining steps renumbered in the updated plan match what was implemented.
- The scaffold did not create a RestApiJiraOptions.cs stub (plan said to delete it if present) — no action needed.
