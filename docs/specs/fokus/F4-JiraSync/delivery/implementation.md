# JiraSync — Implementation

## Files Created

- `src/Services/Fokus/Fokus.API/Infrastructure/Jira/JiraOptions.cs` — POCO bound from `Jira:*` config section
- `src/Services/Fokus/Fokus.API/Infrastructure/Jira/JiraApiException.cs` — typed exception carrying StatusCode + Jira error message
- `src/Services/Fokus/Fokus.API/Infrastructure/Jira/JiraClient.cs` — typed HttpClient with Basic Auth, TokenBucketRateLimiter (10 req/s), exponential backoff on 429, pagination for all endpoints
- `src/Services/Fokus/Fokus.API/Infrastructure/Jira/Dtos/JiraPagedResult.cs` — generic paged result wrapper + issue-specific paged result
- `src/Services/Fokus/Fokus.API/Infrastructure/Jira/Dtos/JiraBoard.cs` — board DTO
- `src/Services/Fokus/Fokus.API/Infrastructure/Jira/Dtos/JiraSprint.cs` — sprint DTO
- `src/Services/Fokus/Fokus.API/Infrastructure/Jira/Dtos/JiraIssue.cs` — issue DTO with nested fields, user, type, status, priority
- `src/Services/Fokus/Fokus.API/Infrastructure/Jira/Dtos/JiraChangelog.cs` — changelog DTO with history + change items; `ToString` renamed to `ToStringValue` with `[JsonPropertyName("toString")]` to avoid hiding `object.ToString()`
- `src/Services/Fokus/Fokus.API/Infrastructure/Jira/Mapping/JiraMapper.cs` — static mapper: MapSprint, MapTicket, MapDeveloper, MapMembership (commitment + removal derivation from changelog), MapStatusTransitions
- `src/Services/Fokus/Fokus.API/Features/Sync/GetBoards/GetBoardsResponse.cs` — response with BoardDto list
- `src/Services/Fokus/Fokus.API/Features/Sync/GetBoards/GetBoardsEndpoint.cs` — GET /api/boards, proxies to Jira board list
- `src/Services/Fokus/Fokus.API/Features/Sync/GetJiraSprints/GetJiraSprintsRequest.cs` — boardId query param
- `src/Services/Fokus/Fokus.API/Features/Sync/GetJiraSprints/GetJiraSprintsResponse.cs` — list of JiraSprintDto
- `src/Services/Fokus/Fokus.API/Features/Sync/GetJiraSprints/GetJiraSprintsValidator.cs` — boardId > 0
- `src/Services/Fokus/Fokus.API/Features/Sync/GetJiraSprints/GetJiraSprintsEndpoint.cs` — GET /api/jira/sprints, returns active+closed sprints ordered by StartDate
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncSprints/SyncSprintsRequest.cs` — FromSprintId + ToSprintId
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncSprints/SyncSprintsResponse.cs` — sync summary with failure list
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncSprints/SyncSprintsValidator.cs` — both IDs > 0
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncSprints/SyncSprintsEndpoint.cs` — POST /api/sync/sprints, validates range, syncs sequentially with partial failure tolerance
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncBacklog/SyncBacklogResponse.cs` — BacklogSprintsSynced + EpicTicketsDiscovered
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncBacklog/SyncBacklogEndpoint.cs` — POST /api/sync/backlog, fetches future sprints + epic tickets not already persisted

## Files Modified

- `src/Services/Fokus/Fokus.Domain/Enums/SprintState.cs` — added `Future` value for future-state sprints used in backlog sync
- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` — added `IConfiguration` parameter, registered `JiraOptions` from `"Jira"` section, registered `JiraClient` as typed HttpClient with base address from options
- `src/Services/Fokus/Fokus.API/Program.cs` — updated `AddFokusServices` call to pass `builder.Configuration`
- `src/Services/Fokus/Fokus.Persistence/Repositories/SprintRepository.cs` — added `UpsertAsync` (load-and-update or add) and `UpsertMembershipsAsync` (delete-then-insert for idempotency)
- `src/Services/Fokus/Fokus.Persistence/Repositories/TicketRepository.cs` — added `UpsertAsync`, `ReplaceTransitionsAsync`, `GetAllKeysWithEpicAsync`, `GetExistingKeysAsync`
- `src/Services/Fokus/Fokus.Persistence/Repositories/DeveloperRepository.cs` — added `UpsertAsync`

## Key Decisions

- `System.Threading.RateLimiting` is part of the .NET 10 shared framework — no package reference added (Step 12 is a no-op)
- `JiraChangeItem.ToString` renamed to `ToStringValue` with `[JsonPropertyName("toString")]` to avoid hiding `object.ToString()` — deserialization still works correctly
- Board name in sprint mapping uses `"Board {boardId}"` placeholder — Jira sprint DTOs do not carry the board name; a separate board fetch would add a call per sync which is unnecessary overhead
- `SaveChangesAsync` is called after each ticket/developer upsert within a sprint loop to keep the unit of work granular; this ensures FK constraints are satisfied before memberships are inserted
- `GetExistingKeysAsync` added to `TicketRepository` to support backlog epic deduplication without loading full ticket entities

## Deviations from Plan

- Step 5 (JiraApiException) was implemented before Step 3 (JiraClient) since JiraClient depends on it — numbering order in the plan is non-blocking, both are complete
- `MapSprint` signature takes `boardName string` as a second parameter (not in plan) because `JiraSprint` DTO does not carry the board name; the endpoint passes it from context
- `MapMembership` takes an optional `forcedNotCommitted` parameter to handle future sprints in backlog sync where commitment is not applicable

## Review Cycle 1 Fixes

- **[HIGH] Custom fields never deserialize** — Added `[JsonPropertyName("customfield_10016")]` to `StoryPoints`, `[JsonPropertyName("customfield_10008")]` to `EpicKey`, `[JsonPropertyName("customfield_10014")]` to `EpicName` in `JiraIssueFields`. Added `using System.Text.Json.Serialization;`.
- **[HIGH] Rate limiter lease scope** — Moved `AcquireAsync` inside the retry loop so each HTTP attempt acquires its own token. Added `IsAcquired` guard that throws `JiraApiException(TooManyRequests)` if the queue is full.
- **[MEDIUM] SyncBacklog silent failures** — Added `ILogger<SyncBacklogEndpoint>`, `LogError` in each catch block, failure counters `SprintFailures` and `EpicFailures` added to `SyncBacklogResponse`.
- **[MEDIUM] JiraOptions startup validation** — Replaced `Configure<JiraOptions>` with `AddOptions<JiraOptions>().Bind(...).ValidateDataAnnotations().ValidateOnStart()`. Added `[Required]` to all three `JiraOptions` properties.
