# Sync Refactoring

**Feature Spec:** None

## Context

Two problems in `Fokus.API/Features/Sync/`:

1. **Mixed file concerns.** Three endpoints have response/DTO classes in the endpoint file instead of a separate command/query file: `GetSettingsEndpoint`, `GetBoardsEndpoint`, `SyncBacklogEndpoint`.
2. **Duplicated sync logic.** `SyncSprintsEndpoint` and `SyncBacklogEndpoint` both contain the same ~15-line loop: for each Jira issue → upsert ticket, upsert developer, replace transitions, create membership. This needs extraction into a focused service class.

## Scope

**In scope:**
- Extract response/DTO types from 3 endpoint files into sibling command/query files
- Create `SprintIssueSyncService` in `Features/Sync/` to own the shared sync loop
- Register the service in DI
- Amend the "no service layer classes" guardrail in CLAUDE.md to clarify the intent
- Amend the Fokus service CLAUDE.md to document the new service

**Out of scope:**
- Changing any sync behavior or domain logic
- Refactoring the epic sync loop in SyncBacklog (not duplicated — only one call site)
- Adding tests (no test project exists yet)

## Domain Model Changes

None.

## Data Model Changes

None.

## Skill Mapping

| Step | Skill | Disposition |
|------|-------|-------------|
| 1–3 (file org) | `create-feature` | Reference for target two-file layout |
| 4 (service) | None | Inline — no skill covers service extraction |
| 5 (DI) | None | Trivial registration |
| 6 (endpoint refactor) | None | Inline — endpoint-specific changes |
| 7 (guardrail) | None | Documentation change |
| 8 (build) | None | Verification step |

## Implementation Steps

### Step 1: Extract `GetSettingsQuery.cs`

Create `Fokus.API/Features/Settings/GetSettings/GetSettingsQuery.cs` with:
- `GetSettingsResponse` (moved from `GetSettingsEndpoint.cs`)

Remove `GetSettingsResponse` from `GetSettingsEndpoint.cs`. This is a `GET` with no request body, so the file only contains the response class.

**Pattern reference:** `Features/Sync/GetJiraSprints/GetJiraSprintsQuery.cs` — same shape (query with no request body, response + DTOs in query file).

### Step 2: Extract `GetBoardsQuery.cs`

Create `Fokus.API/Features/Sync/GetBoards/GetBoardsQuery.cs` with:
- `GetBoardsResponse`
- `BoardDto`

Remove both from `GetBoardsEndpoint.cs`.

**Pattern reference:** Same as Step 1.

### Step 3: Extract `SyncBacklogCommand.cs`

Create `Fokus.API/Features/Sync/SyncBacklog/SyncBacklogCommand.cs` with:
- `SyncBacklogResponse`

Remove `SyncBacklogResponse` from `SyncBacklogEndpoint.cs`. Note: `SyncBacklogEndpoint` extends `EndpointWithoutRequest<TResponse>` so there is no request class — the file contains only the response.

**Pattern reference:** `Features/Sync/SyncSprints/SyncSprintsCommand.cs` — sibling file with response + DTOs.

### Step 4: Create `SprintIssueSyncService`

Create `Fokus.API/Features/Sync/SprintIssueSyncService.cs`.

**Constructor dependencies:** `SprintRepository`, `TicketRepository`, `DeveloperRepository` (all concrete — no interfaces per guardrails).

**Single method:**
```
Task<SprintIssueSyncResult> SyncAsync(
    Sprint sprint,
    IReadOnlyList<JiraIssue> issues,
    bool forcedNotCommitted,
    CancellationToken ct)
```

**Method body** — extracted from the shared loop in both endpoints:
1. Create empty `List<SprintMembership>` and developer counter
2. For each issue:
   - `Ticket.FromJira(issue)` → `ticketRepository.UpsertAsync`
   - `Developer.FromJira(issue)` → `developerRepository.UpsertAsync` (if not null, increment counter)
   - `StatusTransition.ListFromJira(issue)` → `ticketRepository.ReplaceTransitionsAsync`
   - `SprintMembership.FromJira(issue, sprint, forcedNotCommitted)` → add to list
3. `ticketRepository.SaveChangesAsync`
4. `sprintRepository.UpsertMembershipsAsync(sprint.Id, memberships)`
5. `sprintRepository.SaveChangesAsync`
6. Return `SprintIssueSyncResult` record with `TicketsUpserted` and `DevelopersDiscovered` counts

**Result type** — nested record in the same file:
```
public record SprintIssueSyncResult(int TicketsUpserted, int DevelopersDiscovered);
```

### Step 5: Register in DI

In `Fokus.API/DependencyInjection.cs`, add:
```
services.AddScoped<SprintIssueSyncService>();
```

FastEndpoints auto-discovers endpoints but not plain classes — explicit registration is needed.

### Step 6: Refactor endpoints to use the service

**`SyncSprintsEndpoint.cs`:**
- Add `SprintIssueSyncService` to constructor
- Remove `TicketRepository` and `DeveloperRepository` from constructor (no longer used directly)
- Replace the inner `foreach` loop (lines 64–99) with a call to `syncService.SyncAsync(sprint, issues, forcedNotCommitted: false, ct)`
- Map the result to `response.TicketsUpserted` and `response.DevelopersDiscovered`
- Keep the try/catch per sprint — the service call goes inside the existing try block

**`SyncBacklogEndpoint.cs`:**
- Add `SprintIssueSyncService` to constructor
- Remove `TicketRepository` and `DeveloperRepository` from constructor for the sprint loop portion — but `TicketRepository` is still needed for the epic sync section (`GetAllKeysWithEpicAsync`, `GetExistingKeysAsync`, `UpsertAsync`, `SaveChangesAsync`). Keep it.
- Remove `DeveloperRepository` from constructor only if the epic section also uses it — it does (lines 109-111). Keep `DeveloperRepository` too.
- Replace the future-sprints inner `foreach` loop (lines 59-77) with `syncService.SyncAsync(sprint, issues, forcedNotCommitted: true, ct)`
- The epic sync section (lines 89-122) stays in the endpoint — it's not duplicated

**Net result:** Both endpoints shrink to: validate → fetch from Jira → call service → build response. The epic section in SyncBacklog stays as-is.

### Step 7: Amend guardrails

**`CLAUDE.md` (root):** Under `## Guardrails — DO NOT`, change:
```
- **No service layer classes** (e.g. `ArticleService`). Use: domain methods, handlers, repositories, gRPC clients, infra helpers.
```
To:
```
- **No entity-wrapper service classes** (e.g. `ArticleService`, `SprintService`) — classes that accumulate business logic around a single entity. Focused operation services scoped to a feature area (e.g. `SprintIssueSyncService`) are allowed.
```

**`src/Services/Fokus/CLAUDE.md`:** Under `## Key patterns`, add a bullet:
```
- **Focused operation services:** When handler logic is shared across endpoints in the same feature area, extract into a service class named after the operation (not the entity). Lives in the feature area root (e.g., `Features/Sync/SprintIssueSyncService.cs`). Must stay single-purpose — if it grows beyond one operation, split it.
```

### Step 8: Build verification

Run `dotnet build` on the Fokus solution and confirm zero errors, zero warnings.

## Migration Notes

None — no data model changes.

## Testing Strategy

- Build must pass with zero errors
- Manual smoke test: call `POST /api/sync/sprints` and `POST /api/sync/backlog` — verify same responses as before
- Verify no behavior change: same tickets, developers, transitions, and memberships are persisted

## Open Questions

None.
