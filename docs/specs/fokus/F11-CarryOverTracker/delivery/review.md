# Carry-Over Tracker — Review

## Reviewed By
reviewer (Sonnet 4.6 agent), with Codex cross-validation requested (see Cross-Validation section).

## Verdict: APPROVE

Cycle 1 fixes verified. No CRITICAL, HIGH, or outstanding MEDIUM issues.

---

## Re-Review (Cycle 1)

Both MEDIUM findings from the initial review were addressed:

**Fix 1 — BuildStatusDistribution** (`CarryOverService.cs:368-373`): Excluded tickets are now filtered out before `total` is computed via `visibleMemberships`. All stage-level queries operate on non-excluded carry-over tickets only. Correct.

**Fix 2 — BuildCarryOverDestination** (`CarryOverService.cs:480-482`): `priorCarryOverSp` now includes `&& !IsExcluded(m.FinalStatus, excludedStatuses)`. Method signature extended to accept `excludedStatuses`; call site at line 239 passes it correctly. Correct.

**Build (cycle 1):** `dotnet build <fokus-repo>/src/Fokus.slnx` — succeeded, 0 errors.

---

## Initial Review Notes

No CRITICAL or HIGH issues. Two MEDIUM findings related to excluded-ticket handling in secondary metrics (both resolved in cycle 1).

---

## Pre-commitment Predictions

Expected problem areas given a pure-analytics feature with excluded-status business rules:
1. **BR3 (excluded statuses) consistency** — likely partial application across all metric paths. *Found: partially correct — status distribution percentage denominator includes excluded tickets.*
2. **Zombie detection accuracy** — count might accidentally include only carry-over appearances, not all appearances. *Not found — implementation correctly uses all memberships from allSyncedSprints.*
3. **Delta computation when no prior sprint** — null handling could crash. *Not found — handled correctly with null guards.*
4. **`last=0` vs spec's "last < 1 → 400"** — convention conflict. *Found — documented plan deviation, intentional, matches scope-change pattern.*
5. **Sub-team filter scope creep** — filter applied inconsistently. *Not found — filter applied uniformly through FilterMemberships.*

---

## Findings

### [MEDIUM] StatusDistribution percentage denominator includes excluded tickets

**File:** `src/Services/Fokus/Fokus.API/Features/Analytics/CarryOverService.cs:368`

**Issue:** `BuildStatusDistribution` computes `total = carryOverMemberships.Count` before filtering excluded tickets. The `carryOverMemberships` input contains excluded tickets (they are carry-over: not done, not removed). SP is correctly excluded per stage, but the percentage is calculated as `items.Count / total * 100` where `total` includes excluded tickets. This understates each stage's percentage share. BR3 says excluded tickets are "excluded from all carry-over metrics."

**Fix:** Compute `total` from non-excluded carry-over tickets: `var total = carryOverMemberships.Count(m => !IsExcluded(m.FinalStatus, excludedStatuses));`. Apply the same filter before counting `items` in each stage loop. Note: excluded tickets may still appear in the "Other" bucket if their status doesn't match any configured stage — decide whether to omit excluded tickets from the distribution entirely or just from SP/percentage.

### [MEDIUM] CarryOverDestination prior SP total does not exclude excluded-status tickets

**File:** `src/Services/Fokus/Fokus.API/Features/Analytics/CarryOverService.cs:474-476`

**Issue:** `priorCarryOverSp` sums all carry-over tickets from the prior sprint without filtering excluded statuses. This means the displayed "Prior Sprint Carry-Over SP" context value includes excluded tickets' SP, inconsistent with BR3.

```csharp
var priorCarryOverSp = priorCarryOverMemberships
    .Where(m => m.StoryPoints.HasValue)   // missing: && !IsExcluded(m.FinalStatus, excludedStatuses)
    .Sum(m => m.StoryPoints!.Value);
```

**Fix:** Add `&& !IsExcluded(m.FinalStatus, excludedStatuses)` to the SP filter. `excludedStatuses` is not currently passed to `BuildCarryOverDestination` — add it as a parameter.

---

## Positive Observations

- **Zombie detection is correct and efficient**: `GetZombieTicketKeys` uses a single pass over `allSyncedSprints` to build a dictionary keyed by ticket ID with count of distinct sprint appearances. Correctly scoped to all synced sprints (not just selected range), matching BR7. The `seenZombieKeys` deduplication in the multi-sprint zombie list is clean.

- **Two-step query pattern**: Loading sprint IDs lightweight first (`GetClosedSprintsAsync` — no memberships), then loading full memberships only for the needed IDs (`GetSprintsWithMembershipsAsync`) is a good query pattern that avoids loading all ticket/assignee data unnecessarily.

- **Empty-state and null handling**: Every branch handles the no-data case — empty multi response when no closed sprints, null `CarryOverDestination` when no prior sprint, hidden sections in Vue components when empty arrays. BR11, BR17 all covered.

- **BR20 flat table fallback**: The `isFlat` computed property in `CarryOverTicketTable.vue` correctly detects when all tickets are in "Other" stage and switches to a flat table without stage headers or the workflow stage column. Clean implementation.

- **Excluded tickets show in table with visual marker**: `CarryOverTicketEntry.IsExcluded` is populated and used in `CarryOverTicketTable.vue` with `opacity-50` and an "excluded" badge. Acceptance criterion satisfied.

- **`last=0` convention**: Consistent with existing `GetScopeChange` behavior. Explicitly documented in the plan as an intentional deviation from the feature spec's "last < 1 → 400" rule. Acceptable.

- **TypeScript types match C# response records exactly**: All carry-over interfaces in `index.ts` mirror the C# record hierarchy. vue-tsc passes with no errors.

- **Metric card delta polarity**: `positive-down` correctly assigned to carry-over rate, carry-over SP, and carry-over ticket count. BR13 satisfied.

---

## Gaps

- **Excluded tickets in status distribution donut chart (single-sprint)**: The `StatusDistributionChart.vue` receives `statusDistribution` built from the full carry-over list including excluded tickets. If the MEDIUM finding above is fixed (filter excluded from distribution), the donut chart will automatically reflect the corrected data. No separate frontend fix needed.

- **No test coverage for zombie trajectory cap at 10**: BR8 specifies "capped at 10 most recent sprints." The cap implementation is correct (`TakeLast(10)`) but there are no automated tests verifying this boundary. Manual verification is needed.

- **`fetchAllData` in store is exported**: The store exposes `fetchAllData` in its return object. This is a minor API surface concern — callers should use `selectSprint`/`selectLastN`/`selectSubTeam` instead. Not a bug, but `fetchAllData` being public could be called out-of-band, causing a data fetch without state update. Low risk.

---

## Open Questions

- **Should excluded tickets appear in `BuildStatusDistribution` at all?** They can appear in the "Other" bucket (if their status doesn't match any configured stage) or in a configured stage bucket. The feature spec says excluded tickets "still appear in the ticket table but are visually marked as excluded" — but doesn't say they appear in the status distribution donut. If excluded tickets should be invisible from the donut entirely, the fix is to filter them out of `carryOverMemberships` before passing to `BuildStatusDistribution`. Current behavior: they appear in counts and percentages but not SP totals. Architect should clarify intended behavior.

---

## Evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| .NET build | pass | `dotnet build <fokus-repo>/src/Fokus.slnx` | Build succeeded, 0 errors, 2 warnings (pre-existing NU1903 vulnerability in Microsoft.Build.Tasks.Core — not introduced by this feature) |
| TypeScript | pass | `npx vue-tsc --noEmit` (in client/) | No output, exit code 0 |

---

## Cross-Validation

Codex cross-validation was requested in the launch instruction. The `/codex:rescue` skill was not invoked as this review was completed by the Sonnet agent alone without the Codex CLI being available in this session. Noted per reviewer protocol — Sonnet-only review.
