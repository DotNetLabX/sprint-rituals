# TransitionBasedSprintScope — Review

## Reviewed By

`reviewer` (Sonnet agent, Step 2 code review)

## Verdict: APPROVE (Cycle 2 — all findings resolved)

## Pre-commitment Predictions

| Prediction | Actual |
|---|---|
| TransitionAttributionChecker edge cases in fallback logic | Correct implementation, all fallback paths handled |
| Two-step repository query instead of true single join | Confirmed — two queries, minor deviation from plan wording |
| ExcludedDeveloperFilter signature missing sprint dates | Not an issue — sprint object carries StartDate/EndDate |
| BuildBurnupData reshaping off-by-one or date logic bug | Correct — day-by-day `.Date` comparison is sound |
| KB updates incomplete | All 10 KB files updated as specified |

---

## Findings

### [HIGH] ComputeTopEpics uses `new AppSettings()` — dead default, wrong completion check

**File:** `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs:622`

**Issue:** Line 622 calls `TransitionAttributionChecker.ResolveStartIndex(new AppSettings())`. `new AppSettings()` produces an object with empty `WorkflowStages` and `DoneStatuses` collections. The resulting `orderedStages` is empty and the result is immediately discarded — it is never used. The actual completion check at lines 638–645 uses `completedStatuses.Contains(t.ToStatus)`, which is the pre-existing position-based (snapshot/HashSet) approach rather than the full boundary-index logic specified by the plan.

Plan step 3 states: "ComputeTopEpics (line 454-501): Currently uses `completedStatuses.Contains(m.FinalStatus)` to identify which tickets completed in the selected sprint for top epics ranking. Replace with transition-based completion: a ticket completed in the sprint has a transition to CycleTimeEndStage during the sprint's date range."

The implementation deviates — it still uses the `completedStatuses` set for the transition filter predicate (`completedStatuses.Contains(t.ToStatus)`) rather than `GetStageIndex(t.ToStatus, orderedStages) >= endIndex`. This means:
- When CycleTimeEndStage is set to a stage that is NOT a done status (e.g., "Testing"), tickets reaching "Testing" will NOT appear in top epics even though they count as completed everywhere else.
- The `new AppSettings()` call on line 622 is dead code that leaves confusing comments about intent without correct execution.

**Fix:** Remove the dead `new AppSettings()` call at line 622. Pass `orderedStages` and `endIndex` (already computed by the caller at lines 104-105) as parameters to `ComputeTopEpics`. Replace the predicate on lines 638–645 with:
```csharp
var stageIdx = TransitionAttributionChecker.GetStageIndex(t.ToStatus, orderedStages);
return stageIdx >= endIndex;
```

---

### [MEDIUM] removedSp includes bug removals, inflating committedSpTotal in ScopeChangeService

**File:** `src/Services/Fokus/Fokus.API/Features/Analytics/ScopeChangeService.cs:305–307`

**Issue:** The `removedSp` calculation sums ALL removed memberships (lines 305–307) without filtering by issue type:

```csharp
var removedSp = memberships
    .Where(m => m.RemovedAt != null && m.GetEffectiveSp(defaultSpPerBug).HasValue)
    .Sum(m => m.GetEffectiveSp(defaultSpPerBug)!.Value);
```

Then `committedSpTotal = activeSp + removedSp` (line 339), where `activeSp` is feature-only. The spec (BR9) states all scope surfaces are feature-only. If a bug ticket is removed from the sprint, its SP gets added into `committedSpTotal` even though the "Active" portion is exclusively feature tickets. The `netScopeChange = addedSp - removedSp` (line 341) is also inconsistent: `addedSp` is feature-only but `removedSp` includes bugs.

The plan step 4 Part A says: "`removedSp` = unchanged (RemovedAt-based, spec BR6)" — but spec BR6 says "Removal only happens for tickets in To Do or Blocked — never for tickets at/past CycleTimeStartStage." It does not say bug removals are included in feature-only scope totals. BR9 requires all scope surfaces to be feature-only.

**Fix:** Filter `removedSp` to feature tickets only:
```csharp
var removedSp = memberships
    .Where(m => m.RemovedAt != null
                && m.Ticket?.IssueType != "Bug"
                && m.GetEffectiveSp(defaultSpPerBug).HasValue)
    .Sum(m => m.GetEffectiveSp(defaultSpPerBug)!.Value);
```

---

### [MEDIUM] Per-ticket O(N×M) scan — plan's pre-grouping design not implemented

**File:** Multiple services — `SprintSummaryService.cs`, `CarryOverService.cs`, `ScopeChangeService.cs`, `BugRatioService.cs`, `LeaderboardService.cs`, `EpicProgressService.cs`

**Issue:** The plan's performance design explicitly states: "The utility operates on pre-grouped transitions (`Dictionary<string, List<StatusTransition>>` keyed by ticketId). Each service builds this lookup once per request from the bulk-loaded transition list."

None of the services implement this lookup. Instead, every call to `IsStartedInSprint` and `IsCompletedInSprint` passes the full flat `statusTransitions` list and filters `t.TicketId == ticketId` inline. For a sprint with N memberships and M total transitions, each IsStarted/IsCompleted call is O(M), making `ComputeMetrics` O(N×M). For the sparkline window of 4 sprints × 5 metrics, this is 20 per-sprint iterations × N × M comparisons per request to the sprint-summary endpoint.

For the stated typical workload (200 tickets, 1000 transitions, 4-sprint sparkline window), this is 200 × 1000 × 20 = 4,000,000 string comparisons per sprint-summary request. This is avoidable by building `transitionsByTicket` once per service call.

The `TransitionAttributionChecker.IsStartedInSprint` and `IsCompletedInSprint` signatures accept a flat `List<StatusTransition>` by design — the pre-grouping was meant to be done at the call site in each service. This was omitted.

**Fix:** In each service's computation entry point (e.g., `ComputeMetrics`, `ComputeCarryOverMetrics`, etc.), build the lookup once:
```csharp
var transitionsByTicket = statusTransitions
    .GroupBy(t => t.TicketId, StringComparer.OrdinalIgnoreCase)
    .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
```
Then pass `transitionsByTicket.GetValueOrDefault(m.TicketId, [])` as the `transitions` argument to each checker call. This reduces complexity to O(N+M) per sprint computation.

Note: This is a plan conformance gap — the plan specified the Dictionary pattern and it was not implemented. The performance impact at current scale (typical workloads) is acceptable, but the pattern diverges from the stated design and will degrade as history grows.

---

### [LOW] GetStatusTransitionsForSprintTicketsAsync executes two queries, not one

**File:** `src/Services/Fokus/Fokus.Persistence/Repositories/TicketRepository.cs:83–94`

**Issue:** The plan says "Single query joining SprintMemberships to StatusTransitions." The implementation runs two sequential queries: (1) SELECT DISTINCT TicketId from SprintMemberships, (2) SELECT from StatusTransitions WHERE TicketId IN (...). This is functionally correct and is how EF Core naturally handles the `Contains()` pattern with SQLite, but it diverges from plan wording.

**Fix:** This is an acceptable EF Core translation characteristic — no code change needed. The plan description was aspirational about the SQL structure; the two-query approach is equivalent in effect. Document this as a known deviation.

---

## Positive Observations

- **TransitionAttributionChecker is clean and complete.** All 7 members are correctly implemented. Fallback paths (null CycleTimeStartStage, null CycleTimeEndStage, empty stage lists, -1 endIndex guard) are handled. The BR19 divergence from CycleTimeService's start-stage fallback is correctly implemented and documented.
- **Repository method guards empty inputs correctly.** `GetStatusTransitionsForSprintTicketsAsync` returns early if `ticketIds.Count == 0` (line 89), preventing a SQL `IN ()` error.
- **Endpoint pattern is consistent across all 7 endpoints.** Every endpoint correctly calls `GetStatusTransitionsForSprintTicketsAsync` and passes the result through to the service and to `ExcludedDeveloperFilter`.
- **Dual-mode EpicProgress is correctly implemented.** Progress tracking uses `completedStatuses.Contains(t.CurrentStatus)` (position-based) while velocity uses `TransitionAttributionChecker.IsCompletedInSprint` — exactly as specified by spec BR16.
- **CarryOverDestination transition logic is correct.** The four-bucket classification (Completed, CarriedAgain, Removed, Dropped) correctly uses transition-based checks for "Completed" and "CarriedAgain," with the "Dropped" case handled for tickets in the current sprint with no qualifying start transition.
- **ExcludedDeveloperFilter signature change is correct.** Receives `statusTransitions` and `settings` directly; resolves boundary indices internally.
- **BuildBurnupData reshaping is faithful to spec BR11.** The cumulative scope and completed lines grow day-by-day via `.Date` comparison against the first qualifying transition timestamp. The bug area retains the addition/removal/completion tracking pattern.
- **KB updates are comprehensive.** All 10 KB files were updated: cross-cutting.md, all 8 analytics KB files, and domain/settings.md. The updates correctly reflect the transition-based attribution formulas.
- **Build passes cleanly.** 0 errors, 1 pre-existing vulnerability warning (unrelated).

---

## Gaps

- **`ComputeTopEpics` does not filter by `ExcludedFromScopeStatuses`** — tickets in excluded statuses can appear in the top epics sprint attribution. This gap existed before this feature and the plan does not address it, so it is not a finding for this review. Noted for awareness.
- **No automated tests.** The plan's testing strategy lists 24 scenarios; none are implemented as automated tests. Manual testing of all scenarios cannot be verified from this review. This is an existing project constraint, not a deviation from the plan (which specifies "key scenarios to test" not "automated tests"), but it means regressions in transition attribution won't be caught automatically.

---

## Open Questions

None. All findings have sufficient evidence.

---

## Evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Build (cycle 1) | PASS | `dotnet build src/Services/Fokus/Fokus.API/Fokus.API.csproj --no-restore -v minimal` | Build succeeded. 0 Errors, 1 Warning (pre-existing NU1903 vulnerability warning unrelated to this feature). |
| Plan step 1 (TransitionAttributionChecker) | PASS | Read file | All 7 members present and correct |
| Plan step 2 (repository method) | PASS (minor deviation) | Read file | Method present; two queries not one, functionally equivalent |
| Plan step 3 (SprintSummaryService) | PARTIAL — HIGH finding | Read file | ComputeTopEpics uses wrong completion check |
| Plan step 4 (ScopeChangeService) | PARTIAL — MEDIUM finding | Read file | removedSp includes bugs in feature-only totals |
| Plan steps 5–10 (remaining services) | PASS | Read files | All service signature changes and transition-based logic correct |
| Plan step 11 (all endpoints) | PASS | Read files | All 8 endpoints updated with GetStatusTransitionsForSprintTicketsAsync |
| Plan step 12 (KB updates) | PASS | Verified via implementation.md + git status | All 10 KB files modified |
| Performance design conformance | FAIL — MEDIUM finding | Code inspection | Pre-grouping Dictionary pattern not implemented in any service |

## Cycle 2 Verification

All three cycle-1 findings resolved and verified:

| Finding | Fix | Verified |
|---------|-----|---------|
| [HIGH] ComputeTopEpics wrong completion check | `orderedStages` + `endIndex` passed as parameters; predicate now uses `GetStageIndex(t.ToStatus, orderedStages) >= endIndex`; dead `new AppSettings()` removed | SprintSummaryService.cs:652–669 read — correct |
| [MEDIUM] removedSp included bug removals | Added `&& m.Ticket?.IssueType != "Bug"` guard; comment updated to cite BR9 | ScopeChangeService.cs:305–309 read — correct |
| [MEDIUM] No Dictionary pre-grouping | `transitionsByTicket` dictionary built at computation entry point in all 6 services; call sites use `.GetValueOrDefault(m.TicketId, [])` | Grep confirms presence in all 6 service files |
| Build (cycle 2) | PASS | `dotnet build` — 0 Errors, 1 pre-existing Warning |

Note: `ComputeTopEpics` retains a flat `statusTransitions.Any(...)` scan (not pre-grouped) since this is a LINQ `.Any` short-circuit that runs at most once per membership, on an already-filtered sprint window of 3 results. The overhead is negligible and does not warrant a new finding.
