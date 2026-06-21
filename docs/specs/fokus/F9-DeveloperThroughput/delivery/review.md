# Developer Throughput — Review

## Reviewed By
`reviewer` (Sonnet agent, claude-sonnet-4-6). No Codex cross-validation requested.

## Verdict: APPROVE

---

## Pre-commitment Predictions

| Prediction | Actual |
|------------|--------|
| Rolling average edge case (window bounds) | Not a problem — `ComputeRollingAverage` collects currentIndex downward inclusive, correct per BR8 |
| Delta null handling (no prior sprint) | Correct — `priorSprint` is null-checked at line 134; all delta fields stay null |
| Sub-team double-filter (FilterDevelopers + GetDeveloperMemberships) | Redundant but harmless — Assignee IS Developer, same SubTeam value |
| `sprints[0]` picks wrong sprint (oldest vs newest) | Correct — `GetClosedSprintsAsync` returns `OrderByDescending(StartDate)`, so index 0 is most recent |
| PageToolbar backward compatibility | Confirmed — `showAggregateOptions` defaults to false, DashboardView unchanged |

All 5 predictions investigated; none constitute findings.

---

## Findings

### [LOW] `GetDeveloperMemberships` applies redundant sub-team check
**File:** `src/Services/Fokus/Fokus.API/Features/Analytics/DeveloperThroughputService.cs:218-219`
**Issue:** `GetDeveloperMemberships` checks both `m.Ticket?.AssigneeId == developerId` AND `m.Ticket?.Assignee?.SubTeam == subTeam`. Since `FilterDevelopers` already restricts to developers in the target sub-team, and `Ticket.Assignee` is the same Developer entity, the second clause is always true when the first is true. It is logically redundant.
**Fix:** Remove `(string.IsNullOrWhiteSpace(subTeam) || m.Ticket?.Assignee?.SubTeam == subTeam)` from `GetDeveloperMemberships`. This is optional — the result is identical, and leaving it is defensively correct.

### [LOW] `DeveloperSprintCapacity` — `SprintId` property lacks `required`
**File:** `src/Services/Fokus/Fokus.Domain/Developer/DeveloperSprintCapacity.cs:6`
**Issue:** `DeveloperAccountId` is marked `required` but `SprintId` (int) and `CapacityPercent` (int with default) are not. For a plain data entity following `required init` convention, `SprintId` should be `required` since it forms half the composite key. The pattern reference `SprintMembership` uses `required string TicketId`. `SprintId` is an int so it always has a default (0) and could silently produce an invalid record.
**Fix:** Consider `public required int SprintId { get; set; }` to match the `required init` pattern for composite key members. Low severity since EF Core constraint will catch 0 as a valid FK only if sprint ID 0 exists.

---

## Positive Observations

- **Plan conformance is complete.** All 12 steps have matching implementation. No plan steps were skipped silently.
- **Rolling average window expansion** is implemented correctly in the endpoint (Step 5, lines 56-63): takes up to 2 extra sprints before the earliest target. Single query, no N+1.
- **Delta polarity is exact per spec.** `ticketsCarriedOverDeltaPolarity` uses `positiveUp: false` (BR7 inverted polarity). All five polarities match the spec table.
- **Mutual exclusivity validator** for `SprintId`/`Last` is implemented at the validator level (not handler), consistent with FastEndpoints conventions.
- **Optimistic UI with revert** in `updateCapacity` correctly saves `previousCapacity` before the optimistic write and reverts on error. The `throughput.value === null` branch also covers the case where throughput hasn't loaded.
- **`encodeURIComponent(accountId)`** in `developers.ts` correctly handles Jira accountIds that may contain special characters.
- **URL sync** in `DevelopersView` seeds state from URL before calling `initialize()` (onMounted reads query params first), so deep-link navigation works as specified.
- **Backward compatibility** of `PageToolbar`: `showAggregateOptions` defaults to false; `currentSelectValue` is a plain function receiving a props object (not a computed that destructures props), keeping reactivity intact.
- **Migration** matches the EF Core configuration exactly: composite PK, both cascade FKs, named index `IX_DeveloperSprintCapacity_SprintId`.
- **`DeveloperThroughputEntry` gains `AccountId`** — the known deviation is valid. The plan response shape omitted it but capacity editing from the frontend requires it. The fix was minimal and correct.

---

## Gaps

- **No validation that `sprintId` in `SetCapacityRequest` exists as a real sprint.** The endpoint validates `sprintId > 0` and that the developer exists, but does not verify the sprint exists in the database. A PUT to a valid developer with a non-existent sprint ID will succeed and create an orphaned capacity record that can never be displayed. FK constraint will reject it at DB level (Cascade FK), but the 500 will surface as an unhandled exception rather than a 400. This is an accepted gap for v1 (spec does not require sprint validation on this endpoint).
- **`GetCapacity` endpoint has no validator class.** The plan says "FastEndpoints: validator as sibling in same folder." `GetCapacityQuery.cs` defines `GetCapacityRequest` but no `Validator<GetCapacityRequest>`. The request only has an optional `int? SprintId`, so there is nothing meaningful to validate (no required fields, no range checks needed). This is appropriate — an empty validator would be noise.
- **Multi-sprint table shows averaged completion %** computed client-side by averaging per-sprint `completionPercent` values. This is arithmetically correct only if all sprints have equal SP-assigned weight. A weighted average (total SP completed / total SP assigned × 100) would be more accurate. Not a bug per spec, which says "averaged values," but noted as a potential UX gap.
- **Chart gap handling for null rolling averages.** ApexCharts receives `null` for sprints with fewer than 3 qualifying data points. ApexCharts line charts render gaps for `null` data points, which is the intended behavior per the plan. This works correctly.

---

## Open Questions

None. All CRITICAL/HIGH candidates were investigated and refuted.

---

## Evidence

| Check | Result | Command | Output summary |
|-------|--------|---------|----------------|
| .NET build | **PASS** | `dotnet build src/Services/Fokus/Fokus.API/Fokus.API.csproj` | Build succeeded. 0 errors, 2 warnings (pre-existing NuGet vulnerability advisory unrelated to this feature) |
| Frontend build | **PASS** | `npm run build` (in `client/`) | `vue-tsc -b && vite build` — 0 errors, 77 modules transformed, all assets emitted. Chunk size advisory is pre-existing (ApexCharts bundle). |
| Migration exists | **PASS** | Glob | `20260508204811_AddDeveloperSprintCapacity.cs` present; Up/Down correct |
| Plan step coverage | **PASS** | Manual | All 12 steps accounted for in implementation.md and verified in source |
| Guardrails | **PASS** | Manual | No repository interfaces, no god folders, no entity-wrapper service class, no domain rule bypass |
| Conventions | **PASS** | Manual | FastEndpoints validators as siblings, repositories wrap SaveChangesAsync, project references correct (API→Domain, API→Persistence→Domain) |
