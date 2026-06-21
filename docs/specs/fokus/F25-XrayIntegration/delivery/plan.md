# Xray Integration & Ticket Test Enrichment

**Feature Spec:** `docs/specs/F25-XrayIntegration/definition/spec.md`

## Context

Fokus v1 delivers sprint health analytics for delivery metrics only. Scrum Masters cannot answer "was this sprint tested?" or "did the tests pass?" without manually checking Xray. This feature adds the foundation for QA analytics by integrating with Xray Cloud: a new Xray module (authentication + GraphQL queries), four new domain entities for test data, a dual-API sync pipeline (Jira REST for coverage links + Xray GraphQL for test run results), and a Settings UI for configuring the integration. The entire QA module is gated behind a feature flag.

This is infrastructure only -- no QA metrics or visualizations appear in existing pages. All analytics display is deferred to F26-F31.

**Services impacted:** Fokus (single service). **Modules impacted:** Jira module (extend to read issue links), new Xray module.

## Scope

**In scope:**
- Xray module: authentication (bearer token with 23h proactive refresh), GraphQL client for test execution queries
- Jira module extension: add `issuelinks` to fetched fields, parse Test/Blocks link types
- Four domain entities: TestExecution, TestExecutionLink, TestRun, TestSet
- Three AppSettings fields: XrayEnabled, XrayClientId, XrayClientSecret
- Settings UI: Xray section with toggle, credentials, Test Connection, Sync QA Data
- Piggyback sync: sprint sync gains optional Xray data fetch when enabled
- Standalone QA sync endpoint: POST /api/xray/sync
- Connection test endpoint: POST /api/xray/test-connection
- Xray settings save endpoint: PUT /api/settings/xray
- GET /api/settings response extension (masked secret)
- Help tooltips on Xray settings UI elements
- Feature flag gating: when disabled, zero Xray behavior

**Out of scope:**
- QA metrics display (F26-F31)
- Quality health thresholds in Settings (F26)
- Test case management, automated test import, flakiness detection
- Xray Server/DC, multi-project support
- Test Plan traversal, Test Set analytics

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | create-module-claude-md | Follow | Xray module, Component archetype, IXrayClient contract, GraphQL implementation | |
| 2 | create-module | Follow | Xray module skeleton from CLAUDE.md | |
| 3 | domain-patterns | Follow | TestExecution (Entity\<string\>), TestRun (Entity\<string\>), TestSet (Entity\<string\>), TestExecutionLink (plain class, composite key), TestRunStatus enum | |
| 4 | persistence-patterns | Follow | EF configs for 4 entities, DbContext DbSets, TestExecutionRepository, migration | |
| 5 | (none) | -- | Extend JiraIssueFields with IssueLinks, add issuelinks to IssueFields constant | Log: no skill for extending existing module contracts |
| 6 | (none) | -- | Xray bearer token management, GraphQL HTTP POST client, rate limiting | Log: no skill for external API client with token auth |
| 7 | (none) | -- | Wire Xray module csproj refs + DI registration in host, must precede endpoints that inject IXrayClient | |
| 8 | domain-patterns | Follow | AppSettings property extensions (XrayEnabled, XrayClientId, XrayClientSecret) | |
| 9 | persistence-patterns | Follow | AppSettings EF config update, migration | |
| 10 | create-feature | Follow | SaveXraySettings endpoint (PUT /api/settings/xray), FastEndpoints | |
| 11 | create-feature | Follow | TestConnection endpoint (POST /api/xray/test-connection), FastEndpoints | |
| 12 | (none) | -- | XrayIssueSyncService: dual-API orchestration (Jira links + Xray GraphQL), upsert logic. Accepts List\<JiraIssue\> | Log: no skill for cross-API sync orchestration |
| 13 | create-feature | Follow | StandaloneXraySync endpoint (POST /api/xray/sync), FastEndpoints | |
| 14 | (none) | -- | Extend SprintIssueSyncService + SyncSprintsEndpoint + SyncBacklogSprintsEndpoint for piggyback Xray sync | Log: no skill for extending existing sync pipeline |
| 15 | create-feature | Follow | GetSettings extension (add Xray fields to response, mask secret) | |
| 16 | vue-patterns, pinia-patterns | Follow | Settings store Xray actions, SettingsView Xray section, tooltips | |

## Domain Model Changes

### New Entities

**TestExecution** -- `Entity<string>` (PK = Xray IssueId)
- IssueId (string, PK)
- IssueKey (string) -- e.g. "ABC-102"
- Summary (string)
- Status (string) -- Jira issue status: To Do, In Progress, Done, Cancelled
- AssigneeId (string?, FK to Developer)
- CreatedDate (DateTime)
- Computed: `bool IsCancelled => Status == "Cancelled"` -- per BR 7, cancelled TEs are stored but excluded from metrics. F26+ analytics features filter on this property.
- Navigation: Assignee (Developer), Links (List\<TestExecutionLink\>), TestRuns (List\<TestRun\>)

**TestExecutionLink** -- plain class, composite key (TestExecutionIssueId, TicketKey)
- TestExecutionIssueId (string, FK to TestExecution)
- TicketKey (string, FK to Ticket)
- LinkType (TestExecutionLinkType enum: Tests, Blocks)
- Navigation: TestExecution, Ticket

**TestRun** -- `Entity<string>` (PK = Xray test run ID)
- Id (string, PK)
- TestExecutionIssueId (string, FK to TestExecution)
- Status (TestRunStatus enum: Pass, Fail, Todo, Executing, Aborted)
- StatusName (string) -- display name from Xray
- StartedAt (DateTime?)
- FinishedAt (DateTime?)
- ExecutedById (string?, FK to Developer)
- Navigation: TestExecution, ExecutedBy (Developer)

**TestSet** -- `Entity<string>` (PK = Xray IssueId)
- IssueId (string, PK)
- IssueKey (string)
- Summary (string)
- AssigneeId (string?, FK to Developer)
- Status (string)
- Navigation: Assignee (Developer)

### New Enums

**TestRunStatus** -- Pass, Fail, Todo, Executing, Aborted

**TestExecutionLinkType** -- Tests, Blocks

### Modified Entities

**AppSettings** -- gains three properties:
- XrayEnabled (bool, default false)
- XrayClientId (string?)
- XrayClientSecret (string?)

## Data Model Changes

### New Tables

| Table | PK | FKs | Indexes |
|-------|-----|-----|---------|
| TestExecutions | IssueId (string) | AssigneeId -> Developers | IX_AssigneeId |
| TestExecutionLinks | Composite (TestExecutionIssueId, TicketKey) | TestExecutionIssueId -> TestExecutions, TicketKey -> Tickets | IX_TicketKey |
| TestRuns | Id (string) | TestExecutionIssueId -> TestExecutions, ExecutedById -> Developers | IX_TestExecutionIssueId, IX_ExecutedById |
| TestSets | IssueId (string) | AssigneeId -> Developers | IX_AssigneeId |

### Modified Tables

| Table | Change |
|-------|--------|
| AppSettings | Add columns: XrayEnabled (bool, default false), XrayClientId (string, nullable), XrayClientSecret (string, nullable) |

### Migrations

Two migrations:
1. `AddXrayEntities` -- creates TestExecutions, TestExecutionLinks, TestRuns, TestSets tables
2. `AddXraySettings` -- adds XrayEnabled, XrayClientId, XrayClientSecret to AppSettings

These can be combined into a single migration if preferred. Separate migrations give cleaner rollback granularity.

## Implementation Steps

### Step 1: Create Xray Module CLAUDE.md

Write `src/Modules/Xray/CLAUDE.md` for the new Xray module.

**Follow** `create-module-claude-md`.

**Axes:**
- Name: Xray
- Archetype: Component
- Contract: `IXrayClient` in `Xray.Contracts`
- First implementation: `Xray.GraphQL` -- typed HttpClient for Xray Cloud GraphQL API
- Purpose: Authentication (bearer token) + GraphQL queries for test execution data
- Host wiring: `AddGraphQLXray()` DI extension, consuming Xray credentials from AppSettings (not from appsettings.json -- credentials are runtime-configurable)

**Files:**
- Create: `src/Modules/Xray/CLAUDE.md`

---

### Step 2: Scaffold Xray Module

Scaffold the module skeleton from the CLAUDE.md written in Step 1.

**Follow** `create-module`.

**Files:**
- Create: `src/Modules/Xray/Xray.Contracts/Xray.Contracts.csproj`
- Create: `src/Modules/Xray/Xray.Contracts/IXrayClient.cs`
- Create: `src/Modules/Xray/Xray.GraphQL/Xray.GraphQL.csproj`
- Create: `src/Modules/Xray/Xray.GraphQL/DependencyInjection.cs`
- Update: solution file (add both csproj)

**IXrayClient interface shape:**
```
Task<string> AuthenticateAsync(string clientId, string clientSecret, CancellationToken ct)
Task<XrayTestExecutionResult> GetTestExecutionsAsync(string bearerToken, List<string> issueKeys, CancellationToken ct)
```

**Xray.Contracts DTOs:**
- `XrayTestExecutionResult` -- container for test execution data from GraphQL
- `XrayTestRunDto` -- individual test run: id, status, statusName, startedAt, finishedAt, executedById
- `XrayTestExecutionDto` -- test execution from GraphQL response: issueId, testRuns list

**Dependencies:** Step 1

---

### Step 3: Create Domain Entities

Create the four QA domain entities and two enums in `Fokus.Domain`.

**Follow** `domain-patterns` -- use Entity\<string\> for entities with string PKs. Use partial class split (state file + behaviors file). TestExecutionLink uses the plain class pattern (same as SprintMembership -- no base class, composite key).

**Files:**
- Create: `src/Services/Fokus/Fokus.Domain/TestExecution/TestExecution.cs` -- state (properties, backing collections for Links and TestRuns). Include computed property `bool IsCancelled => Status == "Cancelled"` for BR 7 metric exclusion filtering.
- Create: `src/Services/Fokus/Fokus.Domain/TestExecution/TestExecutionLink.cs` -- plain class with TestExecutionIssueId, TicketKey, LinkType, navigations
- Create: `src/Services/Fokus/Fokus.Domain/TestExecution/TestExecutionLinkType.cs` -- enum (Tests, Blocks)
- Create: `src/Services/Fokus/Fokus.Domain/TestRun/TestRun.cs` -- state
- Create: `src/Services/Fokus/Fokus.Domain/TestRun/TestRunStatus.cs` -- enum (Pass, Fail, Todo, Executing, Aborted)
- Create: `src/Services/Fokus/Fokus.Domain/TestSet/TestSet.cs` -- state
- Create: `src/Services/Fokus/Fokus.Domain/TestExecution/Behaviors/TestExecution.cs` -- static factory `FromXray(...)` for mapping from Xray DTOs + Jira issue link data
- Create: `src/Services/Fokus/Fokus.Domain/TestRun/Behaviors/TestRun.cs` -- static factory `FromXray(XrayTestRunDto dto, string testExecutionIssueId)`
- Create: `src/Services/Fokus/Fokus.Domain/TestSet/Behaviors/TestSet.cs` -- static factory `FromJiraIssueLink(...)` for mapping from Jira issue link data

**Pattern reference:** `src/Services/Fokus/Fokus.Domain/Ticket/Ticket.cs` (state) + `src/Services/Fokus/Fokus.Domain/Ticket/Behaviors/Ticket.cs` (factory)

**No dependencies on prior steps** (domain has no reference to Xray module -- factories take primitive parameters or Xray.Contracts DTOs which Fokus.Domain does NOT reference). The factories take raw parameters (strings, enums), not Xray DTOs. Mapping from Xray DTOs to factory parameters happens in the sync service (API layer).

---

### Step 4: Create EF Configurations, Repository, DbContext, Migration

Add EF Core configurations for all four entities, extend FokusDbContext with new DbSets, create TestExecutionRepository, and generate the migration.

**Follow** `persistence-patterns`.

**Files:**
- Create: `src/Services/Fokus/Fokus.Persistence/Configurations/TestExecutionConfiguration.cs`
  - PK: IssueId (string, ValueGeneratedNever)
  - FK: AssigneeId -> Developers (optional)
  - HasMany(Links).WithOne(TestExecution) + HasMany(TestRuns).WithOne(TestExecution)
- Create: `src/Services/Fokus/Fokus.Persistence/Configurations/TestExecutionLinkConfiguration.cs`
  - Composite PK: (TestExecutionIssueId, TicketKey)
  - FK: TestExecutionIssueId -> TestExecutions, TicketKey -> Tickets
  - Index: IX_TicketKey on TicketKey
  - Store LinkType as string conversion
- Create: `src/Services/Fokus/Fokus.Persistence/Configurations/TestRunConfiguration.cs`
  - PK: Id (string, ValueGeneratedNever)
  - FK: TestExecutionIssueId -> TestExecutions, ExecutedById -> Developers (optional)
  - Index: IX_TestExecutionIssueId, IX_ExecutedById
  - Store Status as string conversion
- Create: `src/Services/Fokus/Fokus.Persistence/Configurations/TestSetConfiguration.cs`
  - PK: IssueId (string, ValueGeneratedNever)
  - FK: AssigneeId -> Developers (optional)
- Create: `src/Services/Fokus/Fokus.Persistence/Repositories/TestExecutionRepository.cs`
  - Extends `RepositoryBase<FokusDbContext, TestExecution, string>`
  - Query() includes Links and TestRuns
  - `GetByIssueIdsAsync(List<string> issueIds)` -- bulk fetch for sync
  - `UpsertAsync` override (same pattern as TicketRepository.UpsertAsync)
  - `ReplaceLinksAsync(string testExecutionIssueId, List<TestExecutionLink> links)` -- same pattern as TicketRepository.ReplaceTransitionsAsync
  - `ReplaceTestRunsAsync(string testExecutionIssueId, List<TestRun> runs)` -- same pattern
  - Methods for bulk upsert of TestSets, bulk replace of TestRuns
- Modify: `src/Services/Fokus/Fokus.Persistence/FokusDbContext.cs` -- add DbSets: TestExecutions, TestExecutionLinks, TestRuns, TestSets
- Modify: `src/Services/Fokus/Fokus.Persistence/DependencyInjection.cs` -- register TestExecutionRepository as scoped

**Pattern reference:** `src/Services/Fokus/Fokus.Persistence/Configurations/TicketConfiguration.cs`, `src/Services/Fokus/Fokus.Persistence/Repositories/TicketRepository.cs`

**Migration:** `dotnet ef migrations add AddXrayEntities -p src/Services/Fokus/Fokus.Persistence -s src/Services/Fokus/Fokus.API`

**Dependencies:** Step 3

---

### Step 5: Extend Jira Module for Issue Link Reading

Add `issuelinks` to the fields fetched during sprint issue sync. Create DTOs for Jira issue links. This is the Jira-side of the dual-API sync.

**No matching skill** -- this is an extension to an existing module's contract and implementation, not a new module or feature. Full inline detail required.

**Files:**
- Modify: `src/Modules/Jira/Jira.Contracts/JiraIssue.cs`
  - Add to `JiraIssueFields`: `public List<JiraIssueLink>? Issuelinks { get; set; }`
  - Add new classes in the same file (following existing pattern where JiraUser, JiraPriority etc. are co-located):
    - `JiraIssueLink` -- `Type` (JiraIssueLinkType), `OutwardIssue` (JiraLinkedIssue?), `InwardIssue` (JiraLinkedIssue?)
    - `JiraIssueLinkType` -- `Name` (string), `Inward` (string), `Outward` (string)
    - `JiraLinkedIssue` -- `Id` (string), `Key` (string), `Fields` (JiraLinkedIssueFields)
    - `JiraLinkedIssueFields` -- `Summary` (string), `Status` (JiraStatus?), `Issuetype` (JiraIssueType?), `Assignee` (JiraUser?), `Created` (DateTime?)
- Modify: `src/Modules/Jira/Jira.RestApi/RestApiJiraClient.cs`
  - Add `issuelinks` to the `IssueFields` constant string: change from `"summary,issuetype,..."` to `"summary,issuetype,...,issuelinks"`

**Dependencies:** None (can be done in parallel with Steps 1-4)

---

### Step 6: Implement Xray GraphQL Client

Implement `IXrayClient` in `Xray.GraphQL` project. This is the Xray-side of the dual-API sync.

**No matching skill** -- external API client with bearer token management and GraphQL is a gap. Full inline detail required.

**Files:**
- Create: `src/Modules/Xray/Xray.GraphQL/XrayBearerTokenManager.cs`
  - In-memory token cache: stores token + expiry timestamp
  - `GetTokenAsync(string clientId, string clientSecret, CancellationToken ct)` -- returns cached token if valid (< 23 hours old), otherwise authenticates and caches
  - `InvalidateToken()` -- force re-auth on next call
  - Thread-safe (SemaphoreSlim for concurrent sync requests)
  - Authentication: POST `https://xray.cloud.getxray.app/api/v2/authenticate` with `{ "client_id": "...", "client_secret": "..." }` -- response body is a raw JWT string (not JSON-wrapped)
- Create: `src/Modules/Xray/Xray.GraphQL/GraphQLXrayClient.cs`
  - Implements `IXrayClient`
  - Constructor: `HttpClient` (DI-injected typed client)
  - `AuthenticateAsync` -- delegates to XrayBearerTokenManager, returns the token
  - `GetTestExecutionsAsync` -- builds GraphQL query using `getTestExecutions` with JQL filter `issue in (KEY-1, KEY-2, ...)`. Paginates results (limit 100 per page, per Xray constraints). Returns `XrayTestExecutionResult` with list of test executions and their nested test runs.
  - GraphQL query shape (POST to `https://xray.cloud.getxray.app/api/v2/graphql`):
    ```graphql
    {
      getTestExecutions(jql: "issue in (...)", limit: 100, start: 0) {
        total
        results {
          issueId
          jira(fields: ["key", "summary", "status", "assignee", "created"])
          testRuns(limit: 100) {
            total
            results {
              id
              status { name description }
              startedAt
              finishedAt
              executedById
            }
          }
        }
      }
    }
    ```
  - Rate limiting: track request count per 5-minute window. If approaching 300 (Standard tier), delay with exponential backoff. Use a simple sliding window counter.
  - Error handling: throw `UnauthorizedException` for 401, `BadGatewayException` for network/API errors. Parse GraphQL error responses.
- Modify: `src/Modules/Xray/Xray.GraphQL/DependencyInjection.cs`
  - `AddGraphQLXray(IServiceCollection services)` extension method
  - Register `XrayBearerTokenManager` as singleton (token cache is cross-request)
  - Register `HttpClient` for `GraphQLXrayClient` with base address `https://xray.cloud.getxray.app`
  - Register `IXrayClient` as `GraphQLXrayClient` (scoped)

**Pattern reference:** `src/Modules/Jira/Jira.RestApi/RestApiJiraClient.cs` (error handling, retry pattern), `src/Modules/Jira/Jira.RestApi/DependencyInjection.cs` (DI wiring)

**Note on credential passing:** Unlike JiraClient (which reads credentials from IOptions at startup), XrayClient receives credentials per-call because they are runtime-configurable via Settings. The bearer token manager caches tokens keyed by clientId to handle credential changes without restart.

**Dependencies:** Step 2

---

### Step 7: Wire Xray Module in Host DI

Register the Xray module in the host application. This must happen before any endpoint that injects `IXrayClient` (Steps 11, 12, 13, 14).

**Files:**
- Modify: `src/Services/Fokus/Fokus.API/Fokus.API.csproj`
  - Add project references: `Xray.Contracts` and `Xray.GraphQL`
- Modify: `src/Services/Fokus/Fokus.API/DependencyInjection.cs`
  - Add: `services.AddGraphQLXray();` (from Xray.GraphQL DependencyInjection)
  - Add using: `using Xray.GraphQL;`
- Verify: `dotnet build` from solution root

**Dependencies:** Steps 2, 6

---

### Step 8: Add Xray Settings to AppSettings Entity

Extend the AppSettings domain entity with three new properties for Xray configuration.

**Follow** `domain-patterns` -- simple property additions to an existing entity. Same pattern as existing AppSettings properties.

**Files:**
- Modify: `src/Services/Fokus/Fokus.Domain/Settings/AppSettings.cs`
  - Add: `public bool XrayEnabled { get; set; } = false;`
  - Add: `public string? XrayClientId { get; set; }`
  - Add: `public string? XrayClientSecret { get; set; }`
  - Update `CreateDefault()` to include: `XrayEnabled = false, XrayClientId = null, XrayClientSecret = null`

**Dependencies:** None

---

### Step 9: Update AppSettings EF Configuration and Migration

Add the new AppSettings columns to the EF configuration and create a migration.

**Follow** `persistence-patterns`.

**Files:**
- Modify: `src/Services/Fokus/Fokus.Persistence/Configurations/AppSettingsConfiguration.cs`
  - Add: `builder.Property(s => s.XrayClientId).HasMaxLength(512);`
  - Add: `builder.Property(s => s.XrayClientSecret).HasMaxLength(512);`
  - XrayEnabled needs no special config (bool maps directly)
- Modify: `src/Services/Fokus/Fokus.Persistence/Repositories/AppSettingsRepository.cs`
  - Update `SaveAsync` method: the `SetValues` call handles simple properties automatically. No additional manual mapping needed (unlike JSON columns like HealthThresholds).

**Migration:** `dotnet ef migrations add AddXraySettings -p src/Services/Fokus/Fokus.Persistence -s src/Services/Fokus/Fokus.API`

**Note:** This migration can be combined with Step 4's migration into a single migration `AddXraySupport` if both steps are implemented before running `migrations add`. Developer discretion.

**Dependencies:** Step 8

---

### Step 10: Create SaveXraySettings Endpoint

Create the PUT /api/settings/xray endpoint for saving Xray configuration.

**Follow** `create-feature` (FastEndpoints variant).

**Files:**
- Create: `src/Services/Fokus/Fokus.API/Features/Settings/SaveXraySettings/SaveXraySettingsEndpoint.cs`
  - Route: `PUT /api/settings/xray`
  - Tags: "Settings"
  - Auth: `[Authorize(Roles = "Admin")]`
  - Request: `SaveXraySettingsRequest` with `XrayEnabled` (bool), `XrayClientId` (string?), `XrayClientSecret` (string?)
  - Validator: when XrayEnabled is true, XrayClientId is required (non-empty). XrayClientSecret is NOT required in the validator -- it may be omitted when the user saves settings without changing the secret (the existing secret is preserved).
  - Handler logic (order matters for secret preservation):
    1. Read existing AppSettings via `repository.GetAsync(ct)`
    2. Set `existing.XrayEnabled = req.XrayEnabled`
    3. Set `existing.XrayClientId = req.XrayClientId`
    4. **Secret preservation:** Only overwrite the secret if the request provides a non-null, non-empty value: `if (!string.IsNullOrEmpty(req.XrayClientSecret)) existing.XrayClientSecret = req.XrayClientSecret;` -- this MUST happen before `repository.SaveAsync` because `SetValues` in the repository would otherwise overwrite the existing secret with null.
    5. Call `repository.SaveAsync(existing, ct)`
    6. Return `SaveXraySettingsResponse { Success = true }`
  - Response: `SaveXraySettingsResponse` with `Success` (bool)

**Pattern reference:** `src/Services/Fokus/Fokus.API/Features/Settings/SaveBoard/SaveBoardEndpoint.cs` (per-section save pattern)

**Dependencies:** Steps 8, 9

---

### Step 11: Create TestConnection Endpoint

Create the POST /api/xray/test-connection endpoint that validates Xray credentials.

**Follow** `create-feature` (FastEndpoints variant).

**Files:**
- Create: `src/Services/Fokus/Fokus.API/Features/Xray/TestConnection/TestConnectionEndpoint.cs`
  - Route: `POST /api/xray/test-connection`
  - Tags: "Xray"
  - Auth: `[Authorize(Roles = "Admin")]`
  - Request: `EndpointWithoutRequest` (reads credentials from saved settings)
  - Handler logic:
    1. Read AppSettings. If not XrayEnabled or credentials missing, throw `BadRequestException("Xray is not enabled or credentials are not configured")`
    2. Call `IXrayClient.AuthenticateAsync(clientId, clientSecret)` -- if fails, the client throws UnauthorizedException (401) or BadGatewayException (502)
    3. Make a probe query: call `IXrayClient.GetTestExecutionsAsync(token, limit: 1)` with an empty JQL to verify data access
    4. Return `TestConnectionResponse { Success = true, Message = "Connected -- Xray access verified" }`
  - Response: `TestConnectionResponse` with `Success` (bool), `Message` (string)

**Dependencies:** Steps 7, 8, 9

---

### Step 12: Create XrayIssueSyncService

Create the sync orchestration service that coordinates the dual-API sync: reads Jira issue links to discover TEs and Test Sets, then queries Xray GraphQL for test run results, and persists all QA entities.

**No matching skill** -- cross-API sync orchestration is a gap. Full inline detail required.

**Files:**
- Create: `src/Services/Fokus/Fokus.API/Features/Xray/XrayIssueSyncService.cs`
  - Constructor dependencies: `IXrayClient`, `AppSettingsRepository`, `TestExecutionRepository`, `DeveloperRepository`, `ILogger<XrayIssueSyncService>`
  - Main method: `SyncXrayForIssuesAsync(IReadOnlyList<JiraIssue> jiraIssues, CancellationToken ct) -> XraySyncResult`
    - **Accepts `List<JiraIssue>`, not `List<Ticket>`.** Issue links live on the Jira DTO (`JiraIssueFields.Issuelinks`), not on the domain Ticket entity. Both callers (piggyback sync in Step 14 and standalone sync in Step 13) have access to `JiraIssue` objects with populated issue links.
    - `XraySyncResult` record: `TestExecutionsSynced` (int), `TestRunsSynced` (int), `TestSetsSynced` (int), `Warnings` (List\<string\>)
  - Logic flow:
    1. Read AppSettings. If not XrayEnabled or credentials missing, return empty result.
    2. **Extract issue links from JiraIssues.** For each JiraIssue, read its `Fields.Issuelinks`. Filter for link type name "Test" (outward label "tests" or inward label "is tested by") and "Blocks" (outward "blocks" or inward "is blocked by"). Collect:
       - Discovered Test Execution issue keys/IDs (from "Test" links where the linked issue type is "Test Execution")
       - Discovered Test Set issue keys/IDs (from "Test" links where the linked issue type is "Test Set")
       - Bug links (from "Blocks" links)
    3. **Authenticate with Xray.** Call `IXrayClient.AuthenticateAsync(clientId, clientSecret)`. If fails, add warning and return (graceful degradation -- Jira data already saved).
    4. **Fetch test runs from Xray GraphQL.** Call `IXrayClient.GetTestExecutionsAsync(token, teIssueKeys)` for discovered TE keys. Paginate if needed (batch into groups of 100 keys per JQL query constraint).
    5. **Map and persist Test Executions.** For each discovered TE: create/upsert TestExecution entity. If the TE has an assignee from the Jira issue link data, upsert the Developer entity.
    6. **Map and persist Test Execution Links.** For each ticket-TE pair from step 2, create TestExecutionLink with appropriate LinkType (Tests or Blocks). Bulk replace per TE (same pattern as SprintMembership upsert -- delete existing links for TE, add new ones).
    7. **Map and persist Test Runs.** From the Xray GraphQL response, create TestRun entities for each run within each TE. Bulk replace per TE. If a run has an ExecutedById, upsert the Developer.
    8. **Map and persist Test Sets.** For discovered Test Sets from step 2, create/upsert TestSet entities from the Jira issue link data (key, summary, status, assignee from the linked issue fields).
    9. **Return counts.**
  - Error handling: wrap Xray API calls in try/catch. On failure, add to warnings list and return partial result. Never throw -- the caller (sprint sync) must not be blocked by Xray failures.
  - Sub-task handling: JiraIssues with IssueType "Sub-task" are processed the same way -- their own issue links are read. The spec says "sub-tasks with their own linked TEs contribute those TEs independently."

**Note on sprint derivation for TEs:** The spec says "A TE's sprint membership is derived from its linked stories' sprint membership." This is NOT modeled as a separate entity or table. The TE-to-sprint relationship is derived at query time via: TestExecutionLink -> Ticket -> SprintMembership -> Sprint. F25 only stores the raw link data.

**Forward reference -- BR 3 tiebreaker (F26 responsibility):** When a TE links to stories in multiple sprints, it belongs to the most recent sprint. The tiebreaker query will be implemented as a repository method on `TestExecutionRepository` in F26 (the first feature that needs sprint-scoped QA aggregation). The method signature will be: `GetTestExecutionsForSprintAsync(int sprintId)` which joins TestExecutionLink -> SprintMembership, groups by TE, and selects TEs where the max SprintId across linked stories equals the requested sprintId. F25 does not need this query because it does not display any sprint-scoped QA analytics.

- Modify: `src/Services/Fokus/Fokus.API/DependencyInjection.cs` -- register `XrayIssueSyncService` as scoped

**Dependencies:** Steps 3, 4, 5, 7

---

### Step 13: Create Standalone Xray Sync Endpoint

Create the POST /api/xray/sync endpoint for syncing QA data independently of sprint sync.

**Follow** `create-feature` (FastEndpoints variant).

**Files:**
- Create: `src/Services/Fokus/Fokus.API/Features/Xray/SyncXray/SyncXrayEndpoint.cs`
  - Route: `POST /api/xray/sync`
  - Tags: "Xray"
  - Auth: `[Authorize(Roles = "Admin")]`
  - Request: `SyncXrayRequest` with `SprintIds` (int[])
  - Validator: SprintIds must be non-empty, each > 0
  - Handler logic:
    1. Read AppSettings. If not XrayEnabled, throw `BadRequestException("Xray is not enabled")`
    2. Load sprints with memberships for the requested sprint IDs (via SprintRepository.GetSprintsWithMembershipsAsync)
    3. Re-fetch fresh JiraIssue data from Jira for each sprint via `IJiraClient.GetSprintIssuesAsync(sprintId)`. This is necessary because issue links live on the Jira DTO (`JiraIssueFields.Issuelinks`), not on the stored Ticket entity. Collect all `JiraIssue` objects across the requested sprints.
    4. Call `XrayIssueSyncService.SyncXrayForIssuesAsync(jiraIssues, ct)` -- the service reads issue links from the JiraIssue DTOs
    5. Return `SyncXrayResponse` with counts and warnings
  - Response: `SyncXrayResponse` with `TestExecutionsSynced`, `TestRunsSynced`, `TestSetsSynced`, `Warnings` (string[])

**Dependencies:** Steps 5, 7, 12

---

### Step 14: Extend Sprint Sync for Piggyback Xray

Modify the existing sprint sync pipeline to optionally include Xray data when XrayEnabled is true.

**No matching skill** -- extending existing sync pipeline.

**Files:**
- Modify: `src/Services/Fokus/Fokus.API/Features/Sync/SprintIssueSyncService.cs`
  - Add `XrayIssueSyncService` to constructor dependencies
  - In `SyncAsync()` method: after syncing tickets, transitions, and memberships, if AppSettings.XrayEnabled is true, call `XrayIssueSyncService.SyncXrayForIssuesAsync(issues, ct)` passing the `IReadOnlyList<JiraIssue>` that the method already receives as a parameter (which now includes issue links after Step 5). Wrap in try/catch -- if Xray sync fails, add warning but don't fail the sprint sync.
  - Modify `SprintIssueSyncResult` record: add `XraySyncResult?` field (null when Xray disabled or not attempted)
  - Modify `SprintBatchSyncResult` record: add aggregated Xray counts and warnings list

- Modify: `src/Services/Fokus/Fokus.API/Features/Sync/SyncSprints/SyncSprintsCommand.cs`
  - Extend `SyncSprintsResponse`: add optional `Xray` object with `TestExecutionsSynced`, `TestRunsSynced`, `TestSetsSynced`, `Warnings` (string[]). Null when Xray disabled.

- Modify: `src/Services/Fokus/Fokus.API/Features/Sync/SyncSprints/SyncSprintsEndpoint.cs`
  - Map the new Xray result fields from `SprintBatchSyncResult` to `SyncSprintsResponse.Xray`

- Modify: `src/Services/Fokus/Fokus.API/Features/Sync/SyncBacklogSprints/SyncBacklogSprintsResponse.cs`
  - Extend `SyncBacklogSprintsResponse`: add optional `Xray` object (same shape)

- Modify: `src/Services/Fokus/Fokus.API/Features/Sync/SyncBacklogSprints/SyncBacklogSprintsEndpoint.cs`
  - Map the Xray result fields

**Note on JiraIssue flow:** The `SyncAsync` method already receives `IReadOnlyList<JiraIssue>`. After Step 5, each JiraIssue now has `Issuelinks` populated. `XrayIssueSyncService.SyncXrayForIssuesAsync` accepts `IReadOnlyList<JiraIssue>` directly -- no type conversion needed. Both callers (this piggyback path and the standalone sync in Step 13) pass `JiraIssue` objects with populated issue links.

**Dependencies:** Steps 5, 12

---

### Step 15: Extend GetSettings Endpoint for Xray Fields

Add Xray fields to the GET /api/settings response. The Client Secret must be masked.

**Follow** `create-feature` (modification to existing endpoint).

**Files:**
- Modify: `src/Services/Fokus/Fokus.API/Features/Settings/GetSettings/GetSettingsQuery.cs`
  - Add to `GetSettingsResponse`: `XrayEnabled` (bool), `XrayClientId` (string?), `XrayClientSecret` (string?) -- note: this is the masked value
- Modify: `src/Services/Fokus/Fokus.API/Features/Settings/GetSettings/GetSettingsEndpoint.cs`
  - Map: `XrayEnabled = settings.XrayEnabled`, `XrayClientId = settings.XrayClientId`, `XrayClientSecret = MaskSecret(settings.XrayClientSecret)` where `MaskSecret` returns `"****"` if the secret is non-null/non-empty, otherwise empty string. Private helper method on the endpoint class.

**Dependencies:** Steps 8, 9

---

### Step 16: Frontend -- Settings UI Xray Section

Add the Xray configuration section to the Settings page. Includes toggle, credential fields, Test Connection button, and Sync QA Data button. Wire help tooltips.

**Follow** `vue-patterns` for component patterns. **Follow** `pinia-patterns` for store extensions.

**Files:**
- Modify: `client/src/types/index.ts`
  - Extend `AppSettings` interface: add `xrayEnabled` (boolean), `xrayClientId` (string | null), `xrayClientSecret` (string | null)
  - Add `XraySyncResponse` interface: `testExecutionsSynced` (number), `testRunsSynced` (number), `testSetsSynced` (number), `warnings` (string[])
  - Add `TestConnectionResponse` interface: `success` (boolean), `message` (string)
  - Extend `SyncSprintsResponse` interface: add optional `xray` (XraySyncResponse | null)
  - Extend `SyncBacklogResponse` interface: add optional `xray` (XraySyncResponse | null)

- Modify: `client/src/api/settings.ts`
  - Add: `saveXraySettings(xrayEnabled: boolean, xrayClientId: string, xrayClientSecret: string): Promise<{ success: boolean }>` -- PUT /api/settings/xray
  - Add: `testXrayConnection(): Promise<TestConnectionResponse>` -- POST /api/xray/test-connection
  - Add: `syncXrayData(sprintIds: number[]): Promise<XraySyncResponse>` -- POST /api/xray/sync

- Modify: `client/src/stores/settingsStore.ts`
  - Extend initial `settings` ref with: `xrayEnabled: false, xrayClientId: null, xrayClientSecret: null`
  - Add `syncFromStore` field mapping for xray fields (this happens in SettingsView but the store needs the state shape)
  - Add action: `saveXraySettingsAction(xrayEnabled, xrayClientId, xrayClientSecret)` -- calls `saveXraySettings`, updates local state on success

- Modify: `client/src/views/SettingsView.vue`
  - Add "Xray" tab to the settings tab bar (after "Sync", before "Users")
  - Tab value: `'xray'` -- extend the `activeSettingsTab` type union
  - Add Xray tab content section with:
    - **Xray Integration toggle** -- checkbox/switch that controls `form.xrayEnabled`. Tooltip: "Enable to connect Fokus with Xray Cloud for test coverage and execution data." (from help.tooltips.md)
    - **When toggle is on:** show Client ID field, Client Secret field (type="password"), Test Connection button, Sync QA Data section
    - **When toggle is off:** hide credential fields and buttons (same pattern as Flow 4 in spec)
    - **Client ID field** -- text input. Tooltip: "Your Xray API Client ID from Xray Global Settings > API Keys."
    - **Client Secret field** -- password input. Tooltip: "Your Xray API Client Secret from Xray Global Settings > API Keys."
    - **Save Xray Settings button** -- saves toggle + credentials via PUT /api/settings/xray. Per-panel save pattern (same as saveBoardPanel).
    - **Test Connection button** -- calls POST /api/xray/test-connection. Shows success message or error. Tooltip: "Verifies your Xray credentials and confirms Fokus can access test data." Disabled when credentials are not saved (xrayClientId is null in store.settings).
    - **Sync QA Data section** -- dropdown of synced sprints (load from GetClosedSprints or SprintRepository), multi-select or defaults to most recent. "Sync QA Data" button calls POST /api/xray/sync with selected sprint IDs. Shows result counts or error. Tooltip: "Fetches latest test execution results from Xray for selected sprints." Disabled until Xray is enabled and credentials are saved.
  - Add form fields: `form.xrayEnabled`, `form.xrayClientId`, `form.xrayClientSecret`
  - Add save state refs: `xraySaving`, `xraySaved`, `xrayError`
  - Add test connection state refs: `testingConnection`, `connectionResult`, `connectionError`
  - Add xray sync state refs: `xraySyncing`, `xraySyncResult`, `xraySyncError`, `selectedXraySprints`
  - Wire the `syncFromStore` function to include xray fields
  - For the Sync QA Data sprint selector: reuse the existing `sprintsForRange` data (already loaded for Sync Custom Range), or load closed sprints from the existing `GetClosedSprints` endpoint

- Modify: `client/src/views/SettingsView.vue` (sync results display)
  - In the "Sync All" result display section, add Xray result display when `syncResult.sprints.xray` is present: show TE synced count, Test Runs synced count, Test Sets synced count, and any warnings as amber alerts

**Help tooltips source:** `docs/specs/F25-XrayIntegration/definition/help.tooltips.md` -- wire the tooltip text to each matching UI element using the `title` attribute (same pattern as existing tooltips in the Settings page, e.g., the Jira Board section's `title` attributes).

**Help page note:** `docs/specs/F25-XrayIntegration/definition/help.page.md` contains extended documentation for each Xray setting. This content is NOT wired into the UI in F25 -- it serves as reference documentation for the user guide. If a contextual help panel or "Learn more" links are added in a future feature, this file provides the source content. No action needed in this step.

**Dependencies:** Steps 10, 11, 13, 15

---

## Cross-Service Changes

None. Single-service application.

## Migration Notes

Run migrations after Steps 4 and 9 (or combined):

```
dotnet ef migrations add AddXraySupport -p src/Services/Fokus/Fokus.Persistence -s src/Services/Fokus/Fokus.API
```

No seed data needed -- Xray entities are populated via sync.

The migration adds 4 new tables and 3 new columns to AppSettings. No existing data is affected. The migration is additive-only (no drops, no renames).

## Testing Strategy

### Connection Test
- Valid credentials -> 200 with success message
- Invalid Client ID -> 401
- Invalid Client Secret -> 401
- Xray not enabled -> 400
- Credentials not configured -> 400
- Xray unreachable -> 502

### Settings
- Save Xray settings with all fields -> 200
- GET settings returns masked secret (never plaintext)
- Toggle off then on -> credentials preserved
- Save with empty secret -> existing secret preserved

### Sprint Sync with Xray Enabled
- Sync sprints -> response includes `xray` object with counts
- Xray sync failure -> Jira sync completes, warning in response
- Xray disabled -> response has no `xray` object

### Standalone QA Sync
- Sync specific sprints -> correct counts returned
- Xray not enabled -> 400
- Re-sync same sprint -> data overwritten (idempotent)

### Data Model
- TE linked to multiple tickets -> one TestExecutionLink per ticket
- Ticket with multiple TEs -> multiple links created
- Sub-task with own TEs -> links created independently
- TE with Cancelled status -> stored (not filtered at sync time)

### Feature Flag
- Xray disabled -> no Xray API calls during sprint sync
- Xray disabled -> previously synced data retained in DB
- Re-enable Xray -> data accessible without re-sync

### Bearer Token
- Token cached and reused within 23h window
- Token refreshed proactively before 24h expiry
- Token refresh failure during sync -> warning reported

## Open Questions

1. **Xray license tier** -- assuming Standard (300 req/5min). If Enterprise is confirmed, the rate limiter threshold in Step 6 can be adjusted (change the constant from 300 to 1000). No architectural change needed.
