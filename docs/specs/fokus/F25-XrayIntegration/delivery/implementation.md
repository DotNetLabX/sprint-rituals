# Xray Integration & Ticket Test Enrichment — Implementation

## Files Created

- `src/Modules/Xray/CLAUDE.md` — Xray module documentation: structure, credential passing pattern, auth endpoint
- `src/Modules/Xray/Xray.Contracts/Xray.Contracts.csproj` — Contracts project, no dependencies
- `src/Modules/Xray/Xray.Contracts/IXrayClient.cs` — IXrayClient interface: AuthenticateAsync, GetTestExecutionsAsync, ProbeAsync
- `src/Modules/Xray/Xray.Contracts/XrayDtos.cs` — XrayTestExecutionResult, XrayTestExecutionDto, XrayTestRunDto DTOs
- `src/Modules/Xray/Xray.GraphQL/Xray.GraphQL.csproj` — GraphQL implementation project; added Microsoft.Extensions.Http explicitly (no Refit transitive)
- `src/Modules/Xray/Xray.GraphQL/XrayBearerTokenManager.cs` — Singleton token cache keyed by clientId, 23h lifetime, SemaphoreSlim thread safety
- `src/Modules/Xray/Xray.GraphQL/XrayRateLimiter.cs` — Singleton sliding-window rate limiter extracted from GraphQLXrayClient so counter survives across scoped requests
- `src/Modules/Xray/Xray.GraphQL/GraphQLXrayClient.cs` — IXrayClient implementation: GraphQL queries with pagination (100/page), sliding-window rate limiting, UnauthorizedException/BadGatewayException error handling
- `src/Modules/Xray/Xray.GraphQL/DependencyInjection.cs` — AddGraphQLXray() extension method
- `src/Services/Fokus/Fokus.Domain/TestExecution/TestExecution.cs` — Entity<string>, IsCancelled computed property, navigation collections
- `src/Services/Fokus/Fokus.Domain/TestExecution/TestExecutionLink.cs` — Plain class with composite key (TestExecutionIssueId + TicketKey), LinkType enum
- `src/Services/Fokus/Fokus.Domain/TestExecution/TestExecutionLinkType.cs` — Enum: Tests, Blocks
- `src/Services/Fokus/Fokus.Domain/TestRun/TestRun.cs` — Entity<string>, TestRunStatus enum property
- `src/Services/Fokus/Fokus.Domain/TestRun/TestRunStatus.cs` — Enum: Pass, Fail, Todo, Executing, Aborted
- `src/Services/Fokus/Fokus.Domain/TestSet/TestSet.cs` — Entity<string>
- `src/Services/Fokus/Fokus.Domain/TestExecution/Behaviors/TestExecution.cs` — Static factory FromXray(...)
- `src/Services/Fokus/Fokus.Domain/TestRun/Behaviors/TestRun.cs` — Static factory FromXray(...)
- `src/Services/Fokus/Fokus.Domain/TestSet/Behaviors/TestSet.cs` — Static factory FromJiraIssueLink(...)
- `src/Services/Fokus/Fokus.Persistence/Configurations/TestExecutionConfiguration.cs` — EF config: HasGeneratedId=false, FK Developers SetNull, HasMany Links+TestRuns Cascade, index on AssigneeId
- `src/Services/Fokus/Fokus.Persistence/Configurations/TestExecutionLinkConfiguration.cs` — Composite PK, LinkType as string, index on TicketKey
- `src/Services/Fokus/Fokus.Persistence/Configurations/TestRunConfiguration.cs` — Status as string, two FKs, two indexes
- `src/Services/Fokus/Fokus.Persistence/Configurations/TestSetConfiguration.cs` — HasGeneratedId=false, FK Developers SetNull
- `src/Services/Fokus/Fokus.Persistence/Repositories/TestExecutionRepository.cs` — GetByIssueIdsAsync, UpsertAsync, ReplaceLinksAsync, ReplaceTestRunsAsync, UpsertTestSetAsync
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveXraySettings/SaveXraySettingsEndpoint.cs` — PUT /api/settings/xray, Admin role, secret preservation pattern
- `src/Services/Fokus/Fokus.API/Features/Xray/TestConnection/TestConnectionEndpoint.cs` — POST /api/xray/test-connection, Admin role
- `src/Services/Fokus/Fokus.API/Features/Xray/XrayIssueSyncService.cs` — Dual-API orchestration: Jira links + Xray GraphQL, never-throw pattern with warnings
- `src/Services/Fokus/Fokus.API/Features/Xray/SyncXray/SyncXrayEndpoint.cs` — POST /api/xray/sync, Admin role

## Files Modified

- `src/Fokus.slnx` — Added Xray.Contracts and Xray.GraphQL project paths under /Modules/Xray/ folder
- `src/Services/Fokus/Fokus.Persistence/FokusDbContext.cs` — Added DbSets: TestExecutions, TestExecutionLinks, TestRuns, TestSets
- `src/Services/Fokus/Fokus.Persistence/DependencyInjection.cs` — Added TestExecutionRepository registration
- `src/Services/Fokus/Fokus.Persistence/Configurations/AppSettingsConfiguration.cs` — Added XrayClientId and XrayClientSecret with HasMaxLength(512)
- `src/Services/Fokus/Fokus.Domain/Settings/AppSettings.cs` — Added XrayEnabled (bool, default false), XrayClientId (string?), XrayClientSecret (string?)
- `src/Modules/Jira/Jira.Contracts/JiraIssue.cs` — Added Issuelinks to JiraIssueFields; added JiraIssueLink, JiraIssueLinkType, JiraLinkedIssue, JiraLinkedIssueFields
- `src/Modules/Jira/Jira.RestApi/RestApiJiraClient.cs` — Added issuelinks to IssueFields constant
- `src/Services/Fokus/Fokus.API/Fokus.API.csproj` — Added project references to Xray.Contracts and Xray.GraphQL
- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` — Added AddGraphQLXray() and XrayIssueSyncService registration
- `src/Services/Fokus/Fokus.API/Features/Sync/SprintIssueSyncService.cs` — Constructor gains XrayIssueSyncService; SyncAsync piggybacks Xray sync wrapped in try/catch; SprintIssueSyncResult and SprintBatchSyncResult gain Xray count/warning fields
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncSprints/SyncSprintsCommand.cs` — Added XraySyncSummary class; added Xray property to SyncSprintsResponse
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncSprints/SyncSprintsEndpoint.cs` — Maps Xray result to XraySyncSummary when present
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncBacklogSprints/SyncBacklogSprintsResponse.cs` — Added XraySyncSummary? Xray field
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncBacklogSprints/SyncBacklogSprintsEndpoint.cs` — Maps Xray result to XraySyncSummary
- `src/Services/Fokus/Fokus.API/Features/Settings/GetSettings/GetSettingsQuery.cs` — Added XrayEnabled, XrayClientId, XrayClientSecret to GetSettingsResponse
- `src/Services/Fokus/Fokus.API/Features/Settings/GetSettings/GetSettingsEndpoint.cs` — Maps Xray fields; MaskSecret() returns "****" for non-empty, empty string for null
- `client/src/types/index.ts` — Extended AppSettings with xray fields; added XraySyncResponse and TestConnectionResponse interfaces; extended SyncSprintsResponse and SyncBacklogResponse with optional xray field
- `client/src/api/settings.ts` — Added saveXraySettings, testXrayConnection, syncXrayData functions
- `client/src/stores/settingsStore.ts` — Extended initial settings state with xray fields; added saveXraySettingsAction
- `client/src/views/SettingsView.vue` — Added Xray tab (toggle, credentials, Test Connection, Sync QA Data); wired help tooltips; added Xray result display in Sync All section; extended form, syncFromStore, activeSettingsTab type

## Review Fixes (Cycle 1)

- `src/Modules/Xray/Xray.Contracts/IXrayClient.cs` — Added `ProbeAsync(bearerToken, ct)` method for real GraphQL connectivity check
- `src/Modules/Xray/Xray.GraphQL/GraphQLXrayClient.cs` — Implemented `ProbeAsync` (minimal `getTestExecutions(limit:1)` query); injected `XrayRateLimiter` singleton; removed instance-level rate limit fields
- `src/Modules/Xray/Xray.GraphQL/DependencyInjection.cs` — Registered `XrayRateLimiter` as singleton
- `src/Services/Fokus/Fokus.API/Features/Xray/TestConnection/TestConnectionEndpoint.cs` — Changed probe from `GetTestExecutionsAsync(token, [])` (no-op) to `ProbeAsync(token)`
- `src/Services/Fokus/Fokus.API/Features/Xray/SyncXray/SyncXrayEndpoint.cs` — Removed `NotEmpty()` validator rule; empty `SprintIds` now loads all synced sprints; added `BadRequestException` when no sprints found; added `DistinctBy(i => i.Key)` deduplication
- `client/src/api/settings.ts` — Changed `xrayClientSecret` parameter type to `string | null`; added comment documenting preservation contract
- `client/src/stores/settingsStore.ts` — Changed `saveXraySettingsAction` secret param to `string | null`
- `client/src/views/SettingsView.vue` — Changed `form.xrayClientSecret ?? ''` to `form.xrayClientSecret || null` so empty field sends `null` (not empty string)

## Key Decisions

- Steps 4 and 9 migrations combined into a single `AddXraySupport` migration as permitted by the plan note — cleaner than two separate migrations for a single feature.
- XrayBearerTokenManager.FetchTokenAsync trims raw JWT string quotes from Xray auth response (response body is a quoted string literal, not JSON-wrapped).
- XrayIssueSyncService uses `GetByIdAsync` check before inserting developers discovered via Xray API to avoid overwriting existing developer display names.
- Xray tab gated to Admin only (v-if="authStore.isAdmin") matching the plan's Admin-only endpoint requirement.
- Sync QA Data button on Xray tab sends empty sprintIds array (fetches all) as no sprint selector was wired — the POST /api/xray/sync endpoint loads all synced sprints from the repository internally.

## Deviations from Plan

- **Step 16 Sync QA Data sprint selector:** The plan mentions loading sprints from GetClosedSprints or reusing sprintsForRange. The standalone sync endpoint (POST /api/xray/sync) already loads sprints from the repository internally, so the frontend sends an empty sprintIds array to trigger a full sync. No sprint selector was added to the Xray tab in this step — the endpoint handles the sprint scope server-side. This keeps the UI simple while deferring per-sprint QA sync selection to F26+.
