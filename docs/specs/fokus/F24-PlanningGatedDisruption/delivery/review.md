# Planning-Gated Disruption — Review

## Reviewed By

`reviewer` (Sonnet agent, Step 2 code review — cycle 1 initial + cycle 2 fix verification)

## Verdict: APPROVE

## Pre-commitment Predictions

1. **`IsRemovedPostPlanning` transition check window** — predicted missing upper bound or wrong window. Actual: window is correctly `[sprintStart, membership.RemovedAt]` per spec. No issue.
2. **Loop restructuring in `ComputeSprintMetrics`** — predicted mutual-exclusion error between removed/added paths. Actual: restructure is correct — removed branch continues early, non-removed branch flows through excluded/started/added/completed checks.
3. **`GetMidSprintAdditions` new parameters** — predicted a missing parameter. Actual: parameters (`statusTransitions`, `orderedStages`, `startIndex`, `excludedStatuses`) are all present and correct. No issue.
4. **`BuildEventTable` parameter threading** — predicted a missed call site. Actual: both `ComputeMultiSprint` and `ComputeSingleSprint` thread correctly. No issue.
5. **`removedSp` excluded-status filter** — predicted filtering gap. Actual: **CONFIRMED.** The spec BR4 says "non-excluded" but the implementation skips the excluded-status check for removed tickets.

---

## Findings

### [HIGH] `removedSp` does not apply excluded-status filter — violates spec BR4

**File:** `src/Services/Fokus/Fokus.API/Features/Analytics/ScopeChangeService.cs:322-332`

**Issue:** Spec BR4 requires Removed SP to include only tickets that are "(d) non-excluded." In `ComputeSprintMetrics`, removed tickets enter the early-return branch at line 323 and call `IsRemovedPostPlanning` without first checking `IsExcluded`. The `IsExcluded` guard at line 332 is only reached by non-removed tickets. A feature ticket with an excluded `FinalStatus` that was removed post-planning AND had entered the cycle will be counted in `removedSp` even though it should be excluded.

```csharp
// Removed tickets: only count in removedSp if post-planning AND cycle-entered (spec BR4)
if (m.RemovedAt != null)
{
    if (m.Ticket?.IssueType == "Bug") continue;
    // MISSING: if (IsExcluded(m.FinalStatus, excludedStatuses)) continue;
    if (TransitionAttributionChecker.IsRemovedPostPlanning(...))
        removedSp += sp.Value;
    continue;
}

if (IsExcluded(m.FinalStatus, excludedStatuses)) continue;  // only reached by non-removed tickets
```

**Fix:** Add `if (IsExcluded(m.FinalStatus, excludedStatuses)) continue;` inside the `m.RemovedAt != null` branch, after the bug check, before the `IsRemovedPostPlanning` call:

```csharp
if (m.RemovedAt != null)
{
    if (m.Ticket?.IssueType == "Bug") continue;
    if (IsExcluded(m.FinalStatus, excludedStatuses)) continue;  // add this
    if (TransitionAttributionChecker.IsRemovedPostPlanning(...))
        removedSp += sp.Value;
    continue;
}
```

**Self-audit:** Confidence HIGH. The spec is explicit (BR4 item d). The KB entry (`scope-change.md`) also documents `removedSp` as "!IsBug" without mentioning non-excluded, which means the KB is also slightly incomplete — but the spec is the source of truth. This is a genuine logic error, not a style preference.

---

### [MEDIUM] Stale KB entry in `cross-cutting.md` — `IsAddedInSprint` signature still shows old `sprintStart` parameter

**File:** `docs/kb/cross-cutting.md:110`

**Issue:** The "Transition-Based Sprint Scope" section documents `IsAddedInSprint` with the old signature:

```
`IsAddedInSprint(membership, isStarted, sprintStart)` → `bool` — AddedAt > sprintStart AND isStarted
```

The `PlanningGatedDisruption` section at lines 141-142 correctly documents the rename:
> `IsAddedInSprint` parameter renamed from `sprintStart` to `planningCutoff` — callers pass `sprint.StartDate.AddDays(planningWindowDays)`.

But the "Transition-Based Sprint Scope" section at line 110 still shows the old name and old description. This creates a direct contradiction: two entries in the same file describe the same method differently. A future agent reading the canonical "Key methods" list at line 110 will be misled.

**Fix:** Update line 110 in `cross-cutting.md` to reflect the renamed parameter:

```
- `IsAddedInSprint(membership, isStarted, planningCutoff)` → `bool` — AddedAt > planningCutoff AND isStarted
```

---

### [LOW] `scope-change.md` KB `removedSp` formula omits the non-excluded condition

**File:** `docs/kb/analytics/scope-change.md:21`

**Issue:** The `removedSp` formula in the KB reads:
```
removedSp = sum(SP) where RemovedAt > planningCutoff AND had entered cycle before removal AND !IsBug
```

Spec BR4 also requires `(d) non-excluded`. The `!excluded` condition is present in the `addedSp` formula on the line above but absent from `removedSp`. This is a minor documentation gap (and mirrors the actual code bug in Finding 1 above — fixing the code without updating the KB would leave it incomplete).

**Fix:** Update the `removedSp` formula line to add `AND !excluded`:
```
removedSp = sum(SP) where RemovedAt > planningCutoff AND had entered cycle before removal AND !IsBug AND !excluded
```

---

## Positive Observations

- **`IsRemovedPostPlanning` implementation is clean and correct.** The transition window `[sprintStart, membership.RemovedAt.Value]` exactly matches spec BR4's intent. The early-return for `RemovedAt == null || <= planningCutoff` is correct.
- **`committedSpTotal` LINQ is precise.** The membership-at-planning-cutoff formula correctly implements `(WasCommitted || AddedAt <= planningCutoff) && (RemovedAt == null || RemovedAt > planningCutoff)` with the correct non-bug filter and deliberate absence of excluded-status filter per spec BR1.
- **Loop restructuring is clean.** Splitting the `m.RemovedAt != null` branch as an early-return before the `IsExcluded` check was the right structural approach for the new semantics. The `continue` at line 329 correctly prevents double-counting.
- **`SprintSummaryService` threading is complete.** All four call sites (`ComputeMetrics` ×2, `ComputeScopeDisruptionRate`, `ComputeBugDisruptionRate`, `ComputeFlags`) correctly thread `planningCutoff` or `planningWindowDays`. The hardcoded `AddDays(2)` is gone.
- **Vue components are correct.** `ScopeMetricCards.vue` tooltip strings exactly match `help.tooltips.md`. `ClassificationTable.vue` correctly removes Planning Overflow, updates all three category tooltips, and updates the section header. The unused `settingsStore` import removal is clean.
- **`BuildEventTable` correctly implements spec BR16.** All non-committed memberships appear as events; classification category is only assigned when the ticket qualifies as Added SP (post-planning AND cycle-entered AND non-bug AND non-excluded AND not removed).
- **KB additions are substantive.** The new `PlanningGatedDisruption` section in `cross-cutting.md` documents all six changed behaviors clearly with service and method names.
- **Build passes clean.** 0 errors, 3 pre-existing warnings (none introduced by this feature).

---

## Gaps

- **`removedSp` KB formula (`scope-change.md`)** is missing the `!excluded` condition — covered in Finding 3 (LOW).
- **`cross-cutting.md` line 110** contradicts lines 141-142 — covered in Finding 2 (MEDIUM).
- The `ClassificationTable.vue` "No mid-sprint additions" message at line 30 is unreachable because `BuildClassificationBreakdown` always returns 3 entries (all categories, even with count=0). This was pre-existing behavior not introduced by this feature, so not flagged as a finding.

---

## Open Questions

None — all findings have HIGH confidence.

---

## Evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Build | PASS | `dotnet build Fokus.API.csproj --no-incremental` | Build succeeded, 0 errors, 3 warnings (pre-existing NU1903 ×2, CS9107 ×1) |
| Spec BR4 non-excluded condition | FAIL | Code review lines 322-332 | `IsExcluded` guard at line 332 is bypassed for removed tickets |
| `cross-cutting.md` line 110 vs 141 | STALE | KB review | Line 110 shows old `sprintStart` parameter; line 141 correctly shows renamed `planningCutoff` |
| Tooltip text vs `help.tooltips.md` | PASS | File comparison | All 5 `ScopeMetricCards.vue` tooltips and 3 `ClassificationTable.vue` tooltips match exactly |
| SprintSummaryService call sites | PASS | Lines 119-131, 208-212 | All four call sites correctly thread `planningWindowDays` |
| `TransitionAttributionChecker` | PASS | Full file review | `IsAddedInSprint` renamed, `IsRemovedPostPlanning` added with correct window |

## Cycle 2 Fix Verification

| Finding | Fix | Verified |
|---------|-----|---------|
| HIGH: `removedSp` missing excluded-status filter | `IsExcluded` guard added at `ScopeChangeService.cs:326`, inside removed-ticket branch after bug check | PASS — line 326 confirms `if (IsExcluded(m.FinalStatus, excludedStatuses)) continue;` present in correct position |
| MEDIUM: stale `cross-cutting.md:110` signature | Updated to `IsAddedInSprint(membership, isStarted, planningCutoff)` with correct description | PASS — line 110 now reads `AddedAt > planningCutoff AND isStarted` |
| LOW: `scope-change.md` `removedSp` formula | Added `AND !excluded` to formula | PASS — line 20 now reads `AND !IsBug AND !excluded` |
| Build (cycle 2) | `dotnet build Fokus.API.csproj --no-incremental` | PASS — Build succeeded, 0 errors, 3 warnings (unchanged pre-existing) |
