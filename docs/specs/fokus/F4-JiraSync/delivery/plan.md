# Jira Sync

**Feature Spec:** `docs/features/JiraSync/spec.md`

## Context

Fokus needs sprint and backlog data from Jira before any analytics can run. This feature builds the single data ingress path: a typed HTTP client that talks to Jira's REST API, plus four endpoints that orchestrate sync flows. The domain model (Sprint, Ticket, Developer, SprintMembership, StatusTransition) and the Settings system (AppSettings with BoardId) already exist from F2/F3. This feature populates those entities from Jira data.

Service impacted: `Fokus.API` (new feature slices + infrastructure), `Fokus.Persistence` (repository additions for upsert semantics).

## Scope

**In scope:**
- JiraClient typed HttpClient with rate limiting (10 req/s, exponential backoff on 429)
- Jira DTOs matching REST API response shapes
- JiraMapper: Jira DTOs to domain entities with commitment/removal derivation from changelog
- GET /api/boards — proxy to Jira board list
- GET /api/jira/sprints — fetch started sprints for configured board
- POST /api/sync/sprints — sync a range of sprints (sequential, partial failure tolerant)
- POST /api/sync/backlog — sync future-state sprints + epic tickets not in started sprints
- Repository upsert methods for idempotent sync
- JiraOptions configuration POCO bound from `Jira:*` config keys

**Explicitly out of scope:**
- Scheduled/automatic sync
- Multi-board support
- Progress streaming / SignalR
- Custom story point fields
- Sync history/audit log
- Frontend (separate feature)

## Domain Model Changes

None. All entities already exist (Sprint, Ticket, Developer, SprintMembership, StatusTransition). This feature only populates them.

## Data Model Changes

None. Schema from the InitialCreate migration covers all needed tables. No new migration required.

## Implementation Steps

### Step 1: JiraOptions configuration POCO

- Create `src/Services/Fokus/Fokus.API/Infrastructure/Jira/JiraOptions.cs`
- Properties: `InstanceUrl` (string), `Email` (string), `ApiToken` (string)
- Bind from configuration section `"Jira"` in DI registration (step 6)
- Pattern: standard `IOptions<T>` POCO

### Step 2: Jira DTO classes

- Create `src/Services/Fokus/Fokus.API/Infrastructure/Jira/Dtos/JiraBoard.cs`
- Create `src/Services/Fokus/Fokus.API/Infrastructure/Jira/Dtos/JiraSprint.cs`
- Create `src/Services/Fokus/Fokus.API/Infrastructure/Jira/Dtos/JiraIssue.cs`
- Create `src/Services/Fokus/Fokus.API/Infrastructure/Jira/Dtos/JiraChangelog.cs`
- These are deserialization targets for Jira REST API responses. Include pagination wrappers (e.g., `JiraPagedResult<T>` with `maxResults`, `startAt`, `total`, `values`/`issues`).
- Key shapes:
  - `JiraBoard`: Id, Name, Type
  - `JiraSprint`: Id, Name, StartDate, EndDate, State, OriginBoardId
  - `JiraIssue`: Key, Fields (Summary, IssueType.Name, StoryPoints, Epic link, Assignee, Priority.Name, Status.Name, Created, ResolutionDate), Changelog (Histories)
  - `JiraChangelog`: Histories[] → each has Created (timestamp), Author.AccountId, Items[] (field, fromString, toString)

### Step 3: JiraClient typed HttpClient

- Create `src/Services/Fokus/Fokus.API/Infrastructure/Jira/JiraClient.cs`
- Inject `HttpClient` (typed) and `IOptions<JiraOptions>`
- Methods:
  - `GetBoardsAsync()` — GET `/rest/agile/1.0/board`
  - `GetSprintsAsync(int boardId, string? state = null)` — GET `/rest/agile/1.0/board/{boardId}/sprint` with state filter, paginated
  - `GetSprintIssuesAsync(int sprintId)` — GET `/rest/agile/1.0/sprint/{sprintId}/issue` with `expand=changelog&fields=...`, paginated (fetch all pages)
  - `GetBoardBacklogIssuesAsync(int boardId)` — GET `/rest/agile/1.0/board/{boardId}/backlog` with changelog, paginated
  - `GetEpicIssuesAsync(string epicKey)` — GET `/rest/agile/1.0/epic/{epicKey}/issue`, paginated
- Authentication: Basic Auth header (`email:apiToken` base64-encoded) set via `HttpClient` default headers
- Rate limiting: use `System.Threading.RateLimiting.TokenBucketRateLimiter` (10 tokens/sec, 1 token per request). Acquire a permit before each request. On 429 response: exponential backoff (1s, 2s, 4s — max 3 retries).
- All methods throw a typed `JiraApiException` on non-success (wrapping status code + Jira error body)

### Step 4: JiraMapper — Jira DTOs to domain entities

- Create `src/Services/Fokus/Fokus.API/Infrastructure/Jira/Mapping/JiraMapper.cs`
- Static methods (no DI needed):
  - `MapSprint(JiraSprint dto) → Sprint`
  - `MapTicket(JiraIssue dto) → Ticket` (includes EpicKey/EpicName from fields)
  - `MapDeveloper(JiraIssue dto) → Developer?` (null if unassigned)
  - `MapMembership(JiraIssue dto, Sprint sprint) → SprintMembership` — derives:
    - `AddedAt`: find the earliest changelog entry where sprint field changes TO include this sprint. If none found, use sprint start date (was there from the beginning).
    - `WasCommitted`: `AddedAt <= sprint.StartDate`
    - `RemovedAt`: find changelog entry where sprint field changes FROM this sprint to something else. Null if not removed.
    - `FinalStatus`: ticket's current status at time of sync
    - `StoryPoints`: snapshot from ticket's current story points
  - `MapStatusTransitions(JiraIssue dto) → List<StatusTransition>` — iterate changelog histories, filter items where `field == "status"`, map each to a StatusTransition
- Business rules encoded here: commitment derivation (rule 2), removal tracking (rule 13), final status (rule 14), assignee at sync time (rule 11)

### Step 5: JiraApiException

- Create `src/Services/Fokus/Fokus.API/Infrastructure/Jira/JiraApiException.cs`
- Properties: `HttpStatusCode StatusCode`, `string? JiraMessage`
- Used by JiraClient when Jira returns non-success. Endpoints catch this to produce 502/401 responses.

### Step 6: DI registration for Jira infrastructure

- Modify `src/Services/Fokus/Fokus.API/DependencyInjection.cs`
- Add in `AddFokusServices`:
  - `services.Configure<JiraOptions>(configuration.GetSection("Jira"))` — requires adding `IConfiguration` parameter
  - Register `JiraClient` as typed HttpClient: `services.AddHttpClient<JiraClient>(...)` with base address from `JiraOptions`
- Note: `AddFokusServices` signature needs to accept `IConfiguration` (currently takes none). Update call site in `Program.cs` accordingly.

### Step 7: Repository upsert methods

- Modify `src/Services/Fokus/Fokus.Persistence/Repositories/SprintRepository.cs`
  - Add `UpsertAsync(Sprint sprint, CancellationToken ct)` — checks existence by Id, inserts or updates
  - Add `UpsertMembershipsAsync(int sprintId, List<SprintMembership> memberships, CancellationToken ct)` — deletes existing memberships for this sprint, then inserts new ones (clean-slate per sprint for idempotency)
- Modify `src/Services/Fokus/Fokus.Persistence/Repositories/TicketRepository.cs`
  - Add `UpsertAsync(Ticket ticket, CancellationToken ct)` — checks existence by Key, inserts or updates
  - Add `ReplaceTransitionsAsync(string ticketKey, List<StatusTransition> transitions, CancellationToken ct)` — deletes existing transitions for ticket, inserts new ones
- Modify `src/Services/Fokus/Fokus.Persistence/Repositories/DeveloperRepository.cs`
  - Add `UpsertAsync(Developer developer, CancellationToken ct)` — checks existence by AccountId, inserts or updates
- All upserts use EF Core's change tracking (load existing, update properties or add new). Bulk delete-then-insert for child collections (memberships, transitions) ensures idempotency.

### Step 8: GET /api/boards endpoint

- Create `src/Services/Fokus/Fokus.API/Features/Sync/GetBoards/GetBoardsEndpoint.cs`
- Create `src/Services/Fokus/Fokus.API/Features/Sync/GetBoards/GetBoardsResponse.cs`
- Injects `JiraClient`, calls `GetBoardsAsync()`
- Maps to response: list of `{ Id, Name, Type }`
- Error handling: catch `JiraApiException` → if 401 send 401, otherwise send 502 with Jira error message
- Pattern: follow `GetSettingsEndpoint` structure (AllowAnonymous, no request model for parameterless GET)

### Step 9: GET /api/jira/sprints endpoint

- Create `src/Services/Fokus/Fokus.API/Features/Sync/GetJiraSprints/GetJiraSprintsEndpoint.cs`
- Create `src/Services/Fokus/Fokus.API/Features/Sync/GetJiraSprints/GetJiraSprintsRequest.cs`
- Create `src/Services/Fokus/Fokus.API/Features/Sync/GetJiraSprints/GetJiraSprintsResponse.cs`
- Create `src/Services/Fokus/Fokus.API/Features/Sync/GetJiraSprints/GetJiraSprintsValidator.cs`
- Request: `boardId` (query param, required, > 0)
- Calls `JiraClient.GetSprintsAsync(boardId, state: "active,closed")` — started sprints only
- Response: list of `{ Id, Name, StartDate, State }` ordered by StartDate ascending
- Error handling: same JiraApiException catch pattern as step 8
- Validator: `boardId` required and > 0

### Step 10: POST /api/sync/sprints endpoint

- Create `src/Services/Fokus/Fokus.API/Features/Sync/SyncSprints/SyncSprintsEndpoint.cs`
- Create `src/Services/Fokus/Fokus.API/Features/Sync/SyncSprints/SyncSprintsRequest.cs`
- Create `src/Services/Fokus/Fokus.API/Features/Sync/SyncSprints/SyncSprintsResponse.cs`
- Create `src/Services/Fokus/Fokus.API/Features/Sync/SyncSprints/SyncSprintsValidator.cs`
- Request: `{ FromSprintId: int, ToSprintId: int }`
- Logic:
  1. Fetch sprint list from Jira (started sprints for configured board)
  2. Filter to the range [from..to] by start date ordering
  3. Validate range (from sprint's start date <= to sprint's start date, else 400)
  4. For each sprint in range (sequentially):
     a. Fetch sprint issues with changelog from Jira
     b. Map using JiraMapper: sprint, tickets, developers, memberships, transitions
     c. Upsert all via repositories (sprint, then tickets + developers, then memberships + transitions)
     d. On failure: log error, record in failures list, continue to next sprint
  5. Return summary: `{ SprintsAttempted, SprintsSynced, TicketsUpserted, DevelopersDiscovered, Failures: [{ SprintId, Error }] }`
- Error handling: JiraApiException on initial sprint fetch → 502. Per-sprint failures are partial (rule 7).
- Validator: both IDs required and > 0

### Step 11: POST /api/sync/backlog endpoint

- Create `src/Services/Fokus/Fokus.API/Features/Sync/SyncBacklog/SyncBacklogEndpoint.cs`
- Create `src/Services/Fokus/Fokus.API/Features/Sync/SyncBacklog/SyncBacklogResponse.cs`
- Logic:
  1. Load configured `BoardId` from `AppSettingsRepository`
  2. Fetch future-state sprints from Jira: `GetSprintsAsync(boardId, state: "future")`
  3. For each future sprint: fetch issues, map and upsert (same pattern as step 10 but simpler — no commitment derivation needed for future sprints, set WasCommitted = false)
  4. Fetch all synced tickets that have an EpicKey. Get distinct epic keys.
  5. For each epic: call `GetEpicIssuesAsync(epicKey)`, find tickets not already persisted, upsert them (these are backlog tickets belonging to known epics)
  6. Return: `{ BacklogSprintsSynced: int, EpicTicketsDiscovered: int }`
- Error handling: JiraApiException → 502

### Step 12: Add `Microsoft.Extensions.Http.Resilience` or `System.Threading.RateLimiting` package reference

- Modify `src/Services/Fokus/Fokus.API/Fokus.API.csproj`
- Add `<PackageReference Include="System.Threading.RateLimiting" Version="10.0.0-preview.4.25258.110" />` (for TokenBucketRateLimiter used in JiraClient)
- Note: If `System.Threading.RateLimiting` is already part of the shared framework in .NET 10, this step is a no-op. Developer should verify and skip if already available.

## Cross-Service Changes

None. Single-service feature. No gRPC, no integration events.

## Migration Notes

No new migration needed. The existing `InitialCreate` migration covers all tables this feature populates.

## Testing Strategy

Key scenarios to verify:
1. **Board list proxy** — valid credentials return boards; invalid credentials return 401
2. **Sprint list** — returns only started sprints, ordered by start date; missing boardId returns 400
3. **Sprint sync (happy path)** — syncs sprint, tickets, developers, memberships, transitions; verify counts in response
4. **Idempotent re-sync** — sync same sprint twice, verify no duplicates (same row counts)
5. **Commitment derivation** — ticket added before sprint start → WasCommitted=true; after → false
6. **Removal tracking** — ticket removed mid-sprint → RemovedAt populated
7. **Partial failure** — mock one sprint failing mid-range, verify others still synced and failure reported
8. **Rate limit handling** — mock 429 response, verify retry with backoff (no immediate failure)
9. **Backlog sync** — fetches future sprints + epic tickets not in started sprints
10. **Invalid range** — from sprint has later start date than to sprint → 400

## Open Questions

None.
