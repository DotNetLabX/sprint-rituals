# Sync Refactoring — Review

## Reviewed By

`reviewer` (Sonnet agent, claude-sonnet-4-6)

## Verdict: APPROVE

## Pre-commitment Predictions

| Prediction | Actual |
|------------|--------|
| Loop logic in service may not exactly match both source endpoints (counter differences, save order) | Confirmed correct — `issues.Count` for `TicketsUpserted` matches old `SyncSprints` exactly; `developerCount` increment pattern matches old `newDeveloperCount` |
| Repositories may have been wrongly added/removed from endpoints | `SyncSprintsEndpoint` correctly drops `TicketRepository`/`DeveloperRepository`; `SyncBacklogEndpoint` correctly retains both for the epic section |
| Guardrail amendment wording may have issues | Amendment is precise and well-scoped; distinguishes entity-wrapper (prohibited) from focused-operation (allowed) |
| Namespace/using declarations may be missing or wrong in refactored endpoints | Both endpoints correctly add `using Fokus.API.Features.Sync;`; no missing usings |
| `SprintIssueSyncResult` counts may not map to response fields correctly | Counts map correctly; `TicketsUpserted = issues.Count`, `DevelopersDiscovered = developerCount` |

## Findings

No CRITICAL or HIGH findings.

### LOW: `GetSettingsQuery.cs` contains no query class — naming is slightly misleading

**File:** `src/Services/Fokus/Fokus.API/Features/Settings/GetSettings/GetSettingsQuery.cs:1`
**Issue:** The file is named `GetSettingsQuery.cs` but contains only `GetSettingsResponse`. The pattern reference (`GetJiraSprintsQuery.cs`) contains both a request class and a response. Since `GetSettings` extends `EndpointWithoutRequest`, there is no query object — the file is effectively a response-only file named "Query". The plan calls this out implicitly by saying "This is a GET with no request body, so the file only contains the response class."
**Fix:** Optional — consider renaming to `GetSettingsResponse.cs` to match its content. This is a style preference; the current name follows the plan's instruction and the file functions correctly.

## Positive Observations

- **Removed `developerRepository.SaveChangesAsync` from epic section correctly.** The old code called `developerRepository.SaveChangesAsync(ct)` after `ticketRepository.SaveChangesAsync(ct)` in the epic loop — a redundant double-save since both repositories share the same scoped `FokusDbContext`. The refactoring removed it cleanly. This is an improvement, not a regression.
- **Save granularity improvement done safely.** The old sprint-issue loops in both endpoints called `SaveChangesAsync` per issue (multiple times per issue in the old backlog code). The new service batches all ticket/developer saves to a single `ticketRepository.SaveChangesAsync` after the full loop, then a single `sprintRepository.SaveChangesAsync` for memberships. Idempotent upsert semantics mean this produces identical final state.
- **No behavior change in the epic section.** The epic sync logic (lines 62–103 of `SyncBacklogEndpoint.cs`) is untouched except for switching from `JiraMapper.*` static methods to `Ticket.FromJira` / `Developer.FromJira` domain factory methods — a cleanup that was already done in the prior Jira refactoring commit and is not part of this diff's scope beyond compile correctness.
- **`SprintIssueSyncResult` is a record, correctly placed as a top-level type in the service file** — not a nested type, which would have required outer class qualification. Clean.
- **DI registration uses `AddScoped`** — correct lifetime given the service depends on scoped repositories.
- **Namespace placement** (`Fokus.API.Features.Sync`) is consistent across service, both endpoints' `using` statements, and `DependencyInjection.cs`. No leakage into sub-namespaces.
- **`SyncBacklogEndpoint` result from `syncService.SyncAsync` is intentionally discarded** — the backlog response doesn't report per-sprint ticket/developer counts, so discarding the result is correct and matches the original response shape.
- **Plan Step 7 (guardrail amendment) executed precisely** — both `CLAUDE.md` and `src/Services/Fokus/CLAUDE.md` updated exactly as specified.

## Gaps

- **No transition-save ordering risk**: `ReplaceTransitionsAsync` uses `RemoveRange`/`AddRange` against the EF change tracker without an intermediate `SaveChangesAsync`. This is correct — all operations execute in a single `SaveChangesAsync` call after the loop. Verified safe.
- **Epic developer saves**: Old code had `developerRepository.SaveChangesAsync(ct)` inside the epic loop. Its removal is safe because all repositories share the same `FokusDbContext` (all registered `AddScoped`, single instance per request). `ticketRepository.SaveChangesAsync(ct)` flushes all pending developer changes.
- No testing strategy gaps beyond what plan acknowledges — no test project exists, build + manual smoke test is the documented strategy.

## Open Questions

None. All low-confidence items resolved by evidence.

## Evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Build | PASS | `dotnet build src/Services/Fokus/Fokus.API/Fokus.API.csproj` | 0 errors, 2 warnings (pre-existing NU1903 `Microsoft.Build.Tasks.Core` vulnerability — noted in implementation.md, unrelated to this change) |
| Step 1: `GetSettingsQuery.cs` created | PASS | Read file | `GetSettingsResponse` present; `GetSettingsEndpoint.cs` no longer defines it |
| Step 2: `GetBoardsQuery.cs` created | PASS | Read file | `GetBoardsResponse` + `BoardDto` present; endpoint cleaned up |
| Step 3: `SyncBacklogCommand.cs` created | PASS | Read file | `SyncBacklogResponse` with correct 4 properties |
| Step 4: `SprintIssueSyncService.cs` created | PASS | Read file | Correct constructor, `SyncAsync` signature, loop body, result record |
| Step 5: DI registration | PASS | Read `DependencyInjection.cs` | `services.AddScoped<SprintIssueSyncService>()` present |
| Step 6a: `SyncSprintsEndpoint` refactored | PASS | Read file + git diff | `TicketRepository`/`DeveloperRepository` removed; `syncService.SyncAsync` called with `forcedNotCommitted: false`; result mapped to response |
| Step 6b: `SyncBacklogEndpoint` refactored | PASS | Read file + git diff | `SprintIssueSyncService` added; both `TicketRepository` and `DeveloperRepository` retained for epic section; `syncService.SyncAsync` called with `forcedNotCommitted: true` |
| Step 7a: Root `CLAUDE.md` amended | PASS | Read file | Guardrail updated from "No service layer classes" to entity-wrapper vs. focused-operation distinction |
| Step 7b: Fokus `CLAUDE.md` amended | PASS | Read file | "Focused operation services" bullet added under Key patterns |
| Repository DbContext sharing | VERIFIED | Read `DependencyInjection.cs` (Persistence) | All repositories `AddScoped`, single `FokusDbContext` per request — confirms safe removal of redundant `developerRepository.SaveChangesAsync` |
| Counter logic parity | VERIFIED | git show HEAD~1 (original SyncSprints) | Original used `issues.Count` for tickets and `newDeveloperCount++` only when non-null — service matches exactly |
