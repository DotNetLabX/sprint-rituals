# Sprint Summary Card — Review

## Reviewed By
`reviewer` (Sonnet 4.6 agent, Step 2 code review)

## Verdict: APPROVE

## Pre-commitment Predictions

| Prediction | Actual |
|---|---|
| Sparkline window logic duplication between endpoint and service | Confirmed — redundant but harmless (see Finding 1, MEDIUM) |
| Zero-SP flag (BR18) — risk of flagging devs with no sprint work | Implementation correctly scopes to `assigneesInSprint` set; prediction was wrong |
| Health score interpolation — guard for `amber == 0` edge cases | Guards present and correct (`if (amber == 0) return 0`, `cap <= amber` returns 0) |
| Leaderboard hidden when all developers have 0 SP | `v-if="leaderboard.length > 0"` — if active developers exist, list is non-empty even with 0 SP. Only hides when zero active developers exist — acceptable |
| `WasCommitted` definition alignment with BR6 | `WasCommitted = effectiveAddedAt <= sprint.StartDate` in `SprintMembership.FromJira` — matches "present at sprint start" exactly |

---

## Findings

### [MEDIUM] `BuildSparkline` has an unused `doneStatuses` parameter

**File:** `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs:333-346`

**Issue:** The `BuildSparkline` method signature includes `List<string> doneStatuses` as parameter 4, but the method body never references it. The `doneStatuses` value is captured by closure in each lambda passed as `valueSelector`. The parameter exists and is threaded through but is dead code within the method itself.

```csharp
private static List<SparklinePoint> BuildSparkline(
    List<Sprint> window,
    List<Sprint> allWindowSprints,
    string? subTeam,
    List<string> doneStatuses,       // ← never used in method body
    Func<List<SprintMembership>, decimal> valueSelector)
```

**Fix:** Remove the `doneStatuses` parameter from `BuildSparkline` and update the four call sites. The lambdas already close over `doneStatuses` from the outer scope in `ComputeSummary`.

---

### [MEDIUM] `SprintSummaryService.ComputeSummary` throws if selected sprint absent from window

**File:** `src/Services/Fokus/Fokus.API/Features/Analytics/GetSprintSummaryEndpoint.cs:63`

**Issue:** The endpoint calls `windowSprints.First(s => s.Id == selectedSprintInfo.Id)` after a bulk DB load. If the bulk `GetSprintsWithMembershipsAsync` query somehow does not return the selected sprint (e.g., a race condition where a sprint is deleted between the lightweight list load and the bulk load), this throws `InvalidOperationException` — an unhandled 500 to the client.

For a small SQLite single-tenant app this is extremely unlikely, but the `First` has no error path. `FirstOrDefault` with a null check and a meaningful 400/404 would be safer.

**Fix:** Replace `windowSprints.First(...)` with `windowSprints.FirstOrDefault(...)` and return 404 or re-fetch if null.

---

### [LOW] Inaccurate deviation note in implementation.md about GlobalUsings

**File:** `docs/plans/SprintSummaryCard/implementation.md`

**Issue:** The Key Decisions section states "`global using Jira.Contracts;` — the transitive assembly reference already existed." But reading `Fokus.Persistence/GlobalUsings.cs` shows `global using Jira.Contracts;` was already on line 2 — the developer did not add it. The deviation note is misleading and implies a change that wasn't made.

**Fix:** Clarify the note — the using was already present; no change was needed.

---

### [LOW] `BuildSparkline` double-lookup is redundant

**File:** `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs:342`

**Issue:** Inside `BuildSparkline`, `allWindowSprints.First(ws => ws.Id == s.Id)` looks up the sprint object from `allWindowSprints`. But `window` (the first parameter) already contains the Sprint objects directly from the same list — the lookup is finding the same object reference. The `allWindowSprints` parameter could be removed and `s` used directly.

**Fix:** Remove the `allWindowSprints` parameter and use `s` (the sprint from `window`) directly in the lambda. The memberships are already on `s.Memberships`.

---

## Positive Observations

- **Plan conformance is excellent.** All 11 steps match implementation. Every file the plan specified exists at the specified path. No silent deviations.
- **Sub-team filtering (C2) is clean.** `FilterMemberships` and `FilterDevelopers` are two focused private statics at the top of the service — easy for F9-F14 to reference and reuse.
- **Health score interpolation is correct.** Both `ScoreHigherIsBetter` and `ScoreLowerIsBetter` handle the zero-threshold edge cases with explicit guards. The formula is faithful to BR2 including the "bottom at 2× amber" behavior.
- **Zero-SP flag (BR18) is precise.** The `assigneesInSprint` hash set correctly scopes the flag to developers who had actual work in the sprint, not all active developers.
- **Division-by-zero (BR22) is handled in every rate calculation** — committed, denominator, and amber-is-zero cases all have explicit guards.
- **Delta polarity is correct.** `positive-up` for completion, `positive-down` for disruption/carry-over, `neutral` for SP completed — matches BR12 exactly.
- **DI registration follows existing pattern** — `AddScoped<SprintSummaryService>()` placed alongside `SprintIssueSyncService` and `WorkflowDetectionService`.
- **Frontend type alignment.** TypeScript interfaces in `types/index.ts` match C# record shapes exactly including nullable fields.
- **URL sync works correctly.** The `onMounted` seeds `selectedSprintId` before `initialize()` — the first fetch uses the URL-specified sprint rather than defaulting to most recent. The watch on `selectedSprintId` keeps the URL in sync.
- **Validator deviation is appropriate.** `NotEmpty().When(x => x.SubTeam is not null)` correctly treats empty string as null/omitted per plan intent.
- **`GetSubTeamsQuery.cs` placeholder** is a reasonable structural choice that keeps the two-file feature pattern consistent.

---

## Gaps

- **No test coverage exists** (the testing strategy in the plan is manual). The computation logic — especially sparkline window slicing, health score interpolation at boundary values, and delta null-when-one-sprint — has no automated tests. This is not a blocker for this feature but creates regression risk as F9-F14 inherit these patterns.
- **`SpCompleted` display value** formats as `{selected.SpCompleted:0.#}` (e.g., "24") but the spec example shows `"24 / 30 SP"` (completed / committed). The current implementation only shows completed SP, not the fraction. The spec `DisplayValue` example was illustrative but the plan did not specify the exact format string for SP Completed. Consider whether the fraction format would be more useful for users.
- **Leaderboard always hidden if no active developers exist** — this is correct behavior, but there is no fallback message. If a user has configured sub-team filter but that sub-team has no developers at all, the leaderboard card disappears silently. A "No developers in this sub-team" message would be clearer.
- **Error state in DashboardView** — the store tracks `error` state but `DashboardView.vue` never renders it. If `initialize()` or `fetchSummary()` fails (network error), the user sees a blank page with no feedback.

---

## Open Questions

- None — all CRITICAL/HIGH findings from self-audit were cleared or downgraded. The `First` vs `FirstOrDefault` concern (Finding 2) was assessed as MEDIUM because the failure condition requires a deletion race in a single-user SQLite app.

---

## Evidence

| Check | Result | Command | Output summary |
|-------|--------|---------|----------------|
| Backend build | PASS | `dotnet build Fokus.API.csproj` | Build succeeded, 0 errors, 2 pre-existing NU1903 warnings (unrelated vulnerability in Microsoft.Build.Tasks.Core) |
| Frontend build | PASS | `npm run build` (client/) | ✓ 75 modules transformed, built in 510ms, 0 errors, 1 pre-existing chunk size warning (ApexCharts) |
| Plan step coverage | PASS | Manual — all 11 steps verified against implementation files | All steps present |
| Guardrails | PASS | Manual — no repository interfaces, no god folders, no service-entity wrapper, domain rules correct | None violated |
| BR22 division-by-zero | PASS | `SprintSummaryService.cs:225-228` | All three rates guarded |
| BR16 zombie tickets | PASS | `SprintSummaryService.cs:463-474` | Groups by TicketId, counts distinct SprintIds, threshold >= 3 |
| BR17 mid-sprint disruption | PASS | `SprintSummaryService.cs:478-487` | `AddedAt > StartDate + 2 days && RemovedAt == null` |
| BR18 zero-SP flag | PASS | `SprintSummaryService.cs:495-503` | Scoped to `assigneesInSprint` hash set |
| C1 delta null when no prior | PASS | `SprintSummaryService.cs:105-107` | `prior` is null when `selectedIndex == 0`; delta passed as null |
| C2 sub-team filter scope | PASS | `SprintSummaryService.cs:83-84, 176-193` | Applied before all computations via two filter statics |
| Composite RAG thresholds | PASS | `SprintSummaryService.cs:320-321` | Green >= 75, Amber 40-74, Red < 40 — matches BR3 |
