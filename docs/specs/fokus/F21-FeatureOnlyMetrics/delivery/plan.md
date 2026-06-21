# Feature-Only Delivery Metrics

**Feature Spec:** `docs/features/FeatureOnlyMetrics/spec.md`

## Context

Delivery metrics currently blend feature work and bug fixes into the same SP numbers. The Dashboard shows inflated SP Completed (features + bugs) and a completion percentage that mixes planned delivery with unplanned bug recovery. This feature separates delivery metrics from bug metrics across three surfaces: Dashboard (SP Completed + Completion %), Burnup Chart (scope and completed lines), and Developer Throughput (all SP/ticket metrics). The principle: delivery surfaces show features, bug surfaces show bugs, the leaderboard bridges both.

**Services impacted:** Fokus (sole service). Changes span 3 analytics services (SprintSummaryService, ScopeChangeService, DeveloperThroughputService), their response records, and 3 frontend surfaces (Dashboard, BurnupChart, DevelopersView).

## Scope

**In scope:**
- SprintSummaryService: SP Completed and Completion % become feature-only; new `BugSpCompleted` field on MetricsResult; health score Completion sub-score uses feature-only completion %
- ScopeChangeService: Burnup chart `totalScopeSp` and `completedSp` become feature-only (single-sprint only); `bugSp` unchanged
- DeveloperThroughputService: All per-developer SP and ticket metrics become feature-only
- Dashboard MetricCard for SP Completed gains "(+X bug SP)" annotation
- Burnup chart legend label "Total Scope SP" becomes "Scope SP"
- Frontend type, store, and component updates for all three surfaces
- KB updates

**Explicitly out of scope:**
- Carry-Over Rate changes (remains total scope including bugs)
- Scope/Bug Disruption Rate changes (already correct per F16)
- Sprints page multi-sprint response fields (remain total-scope)
- Throughput bug toggle
- Historical comparison mode
- Configurable bug inclusion toggle
- Dashboard Top Epics changes (remains total SP)
- Zero-SP developer flag changes (remains total SP)

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | (none) | -- | SprintSummaryService: filter bugs from SP Completed, Completion %, add BugSpCompleted | |
| 2 | (none) | -- | SprintSummaryService: update health score input to use feature-only completion % | |
| 3 | (none) | -- | ScopeChangeService: filter bugs from burnup totalScopeSp and completedSp | |
| 4 | (none) | -- | DeveloperThroughputService: filter bugs from all metrics | |
| 5 | vue-patterns | Follow | MetricsResult type gains bugSpCompleted; MetricCard component gains annotation slot | |
| 6 | vue-patterns | Follow | BurnupChart legend label update; tooltip unchanged (already shows all three) | |
| 7 | vue-patterns | Follow | DevelopersView throughput table already shows features-only (backend change) | |
| 8 | (none) | -- | KB updates for analytics entries | |

## Domain Model Changes

None. No new entities or value objects. Bug classification rule is unchanged (`IssueType == "Bug"`).

## Data Model Changes

None. No migrations needed. All changes are computation-level.

## Implementation Steps

**Cross-cutting filtering rule (applies to Steps 1-4):** The `IsBug(SprintMembership m)` helper already exists in SprintSummaryService (`m.Ticket?.IssueType == "Bug"`). DeveloperThroughputService does not have it yet and will need it. The filter is `!IsBug(m)` for feature-only metrics. This filter applies AFTER existing filters (RemovedAt, doneStatuses, GetEffectiveSp) -- it is an additional predicate, not a replacement.

### Step 1: Make SP Completed and Completion % feature-only in SprintSummaryService

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs`

**Changes:**

1. **Add `BugSpCompleted` to `MetricsResult` record** (line 34). Add a new property `decimal BugSpCompleted` after the existing 5 MetricCard properties. This is a plain decimal, not a MetricCard -- it has no delta, no sparkline.

2. **Modify `ComputeMetrics`** to compute two committed SP values and two completed SP values:
   - `featureCommitted` = sum of effective SP where `WasCommitted && RemovedAt == null && !IsBug(m)` (existing committed filter plus `!IsBug`)
   - `featureCompleted` = sum of effective SP where `doneStatuses.Contains(FinalStatus) && RemovedAt == null && !IsBug(m)`
   - `bugSpCompleted` = sum of effective SP where `doneStatuses.Contains(FinalStatus) && RemovedAt == null && IsBug(m)`
   - Keep existing `committed` (total) unchanged for disruption rate denominators (BR4a)

3. **Update `SprintMetrics` record** to add: `FeatureCommitted`, `FeatureCompleted`, `BugSpCompleted`. Keep existing `SpCommitted` (total) and rename existing `SpCompleted` to track total or replace its usage.

4. **Update `CompletionRate` calculation:** `featureCompleted / featureCommitted * 100` (0 if featureCommitted == 0). This replaces the current `completed / committed * 100`.

5. **Update `SpCompleted` in MetricsResult:** Use `featureCompleted` as the value (not total completed).

6. **Update `ComputeSpCompleted` helper** (used by sparkline): add `!IsBug(m)` filter so it returns feature-only SP.

7. **Update `ComputeCompletionRate` helper** (used by sparkline): must use feature-only committed and feature-only completed.

8. **Build the `MetricsResult` with `BugSpCompleted`:** Pass `selected.BugSpCompleted` (no delta, no sparkline -- it is a static number for the selected sprint per spec BR3/BR15).

9. **SP Completed card delta and sparkline:** These already use the `ComputeSpCompleted` helper for sparklines and `selected.SpCompleted - prior.SpCompleted` for delta. After making `SpCompleted` feature-only, both automatically become feature-only. Same for Completion % sparkline via `ComputeCompletionRate`.

10. **Disruption rates unchanged:** `ScopeDisruptionRate` and `BugDisruptionRate` continue to divide by total `committed` (not featureCommitted). Their sparklines and deltas are unchanged.

11. **CarryOverRate unchanged:** Continues using total scope (committed + added) including bugs.

12. **SP Completed card polarity change:** The spec shows SP Completed delta as feature-only comparison. The existing card uses `"neutral"` polarity. Change to `"positive-up"` since feature SP completed higher = better (spec BR15 implies directionality). Actually -- re-reading existing code, it already uses `"neutral"`. The spec does not mandate a polarity change for SP Completed (only that values are feature-only). Keep `"neutral"`.

**Pattern example:** The existing `IsBug` helper at line 232-233 and the scope/bug disruption split at lines 247-254.

**Depends on:** Nothing.

### Step 2: Update health score to use feature-only completion %

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs`

**Changes:**

The health score's `ComputeHealthScore` receives `SprintMetrics`. After Step 1, `CompletionRate` in `SprintMetrics` already uses feature-only values. The `ComputeHealthScore` method uses `metrics.CompletionRate` for the completion sub-score (line 317).

Verify that `DisruptionRate` (combined scope + bug) still feeds into `ComputeHealthScore` unchanged. The health score uses combined disruption per existing architecture (KB: health-score.md confirms this).

This step is a **verification pass** -- if Step 1 correctly makes `CompletionRate` in `SprintMetrics` feature-only, then the health score automatically consumes it. No additional code change beyond Step 1.

**Depends on:** Step 1.

### Step 3: Make burnup chart lines feature-only in ScopeChangeService

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/ScopeChangeService.cs`

**Changes to `BuildBurnupData` method (line 396-557):**

1. **Starting committed SP becomes feature-only:** Add `&& m.Ticket?.IssueType != "Bug"` to the `startingCommitted` filter (line 415-418). This makes the orange scope line start with feature-only committed SP.

2. **Scope additions become feature-only:** Add `&& m.Ticket?.IssueType != "Bug"` to the `addedToday` filter (line 451-455). Feature additions only step up the scope line.

3. **Scope removals become feature-only:** Add `&& m.Ticket?.IssueType != "Bug"` to the `removedToday` filter (line 458-461). Only feature removals step down the scope line.

4. **Completed SP becomes feature-only:** Add `&& m.Ticket?.IssueType != "Bug"` to the `completedToday` filter (line 526-531). Only feature ticket completions count toward the green line.

5. **Starting ticket counts become feature-only:** Add `&& m.Ticket?.IssueType != "Bug"` to `startingTotalScopeTickets` (line 428-430).

6. **Ticket count additions/removals become feature-only:** Add the bug exclusion to `addedTicketsToday` (line 466-469) and `removedTicketsToday` (line 471-473).

7. **Completed tickets become feature-only:** Add the bug exclusion to `completedTicketsToday` (line 533-537).

8. **Bug SP area unchanged:** `startingBugSp`, `bugAddedToday`, `bugRemovedToday`, `bugCompletedToday`, and `cumulativeBugSp` remain exactly as-is. The red area tracks remaining bug SP per F17.

**Multi-sprint response unchanged:** `ComputePerSprintData` and `ComputeSprintMetrics` (used for multi-sprint mode) are NOT modified. The spec explicitly states multi-sprint fields remain total-scope.

**Single-sprint metric cards unchanged:** The `ScopeChangeSingleSprintMetrics` metric cards (CommittedSpActive, AddedSp, etc.) are NOT modified -- they are part of the Sprints page disruption analysis which uses total scope.

**Depends on:** Nothing.

### Step 4: Make Developer Throughput metrics feature-only

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/DeveloperThroughputService.cs`

**Changes:**

1. **Add `IsBug` helper** (same pattern as SprintSummaryService):
   ```
   private static bool IsBug(SprintMembership m) => m.Ticket?.IssueType == "Bug";
   ```

2. **Filter `spAssigned` (line 100-102):** Add `&& !IsBug(m)` to the existing `m.RemovedAt == null` filter.

3. **Filter `spCompleted` (line 104-106):** Add `&& !IsBug(m)` to the existing `m.RemovedAt == null && doneStatuses.Contains(m.FinalStatus)` filter.

4. **Filter `ticketsDone` (line 110-111):** Add `&& !IsBug(m)`.

5. **Filter `ticketsCarriedOver` (line 113-114):** Add `&& !IsBug(m)`.

6. **Filter prior sprint deltas (lines 139-154):** Apply the same `!IsBug(m)` filter to `priorSpAssigned`, `priorSpCompleted`, `priorTicketsDone`, `priorTicketsCarriedOver`.

7. **Filter `ComputeRollingAverage` (line 263-267):** Add `&& m.Ticket?.IssueType != "Bug"` to the membership filter inside the rolling average computation so it uses feature-only SP completed.

**`completionPercent` formula unchanged:** It already divides `spCompleted / spAssigned * 100`. Since both numerator and denominator now exclude bugs, the formula correctly produces feature completion % against feature assignment (spec BR9).

**Depends on:** Nothing.

### Step 5: Update frontend types and Dashboard MetricCard for bug SP annotation

**Files to modify:**
- `client/src/types/index.ts`
- `client/src/components/dashboard/MetricCard.vue`
- `client/src/views/DashboardView.vue`

**Changes:**

1. **`MetricsResult` interface** (types/index.ts line 84-90): Add `bugSpCompleted: number` after the existing MetricCard properties.

2. **`MetricCard.vue`** -- Update the component to support an optional annotation below the primary value. Two approaches:
   - **Option A (prop-based):** Add an optional `annotation` string prop. The DashboardView passes it only for SP Completed.
   - **Option B (slot-based):** Add a named slot `#annotation` below the display value.
   
   **Decision: Option A** (simpler, consistent with existing prop-based pattern). Add optional prop `annotation?: string`. Render it below the display value in `text-xs text-text-muted` styling when present.

3. **`DashboardView.vue`** (line 128): For the SP Completed card, compute the annotation string:
   ```
   const bugSpAnnotation = computed(() => {
     const bugSp = store.summary?.metrics?.bugSpCompleted
     if (!bugSp || bugSp === 0) return undefined
     return `(+${bugSp} bug SP)`
   })
   ```
   Pass `:annotation="bugSpAnnotation"` to the SP Completed MetricCardComponent.

4. **MetricCard.vue tooltip update:** Update `metricTooltip` for "SP Completed" to: "Feature story points completed this sprint. Bug SP shown separately below."

Follow vue-patterns for prop definitions and computed reactivity.

**Depends on:** Step 1 (backend must return `bugSpCompleted`).

### Step 6: Update BurnupChart legend label

**Files to modify:**
- `client/src/components/sprints/BurnupChart.vue`

**Changes:**

1. **Series name** (line 28): Change `'Total Scope SP'` to `'Scope SP'`.

2. **Tooltip** (line 78): Change the row name from `'Total Scope SP'` to `'Scope SP'`.

The chart already shows three series: Bug SP (red area), Scope SP (orange line), Completed SP (green area). Only the label text changes.

Follow vue-patterns.

**Depends on:** Step 3 (backend burnup data must be feature-only).

### Step 7: Verify Developer Throughput frontend needs no changes

**Files to verify (no modifications expected):**
- `client/src/views/DevelopersView.vue`
- `client/src/stores/developersStore.ts`
- `client/src/types/index.ts` (SprintBreakdown interface)

The DevelopersView already renders `spAssigned`, `spCompleted`, `completionPercent`, `ticketsDone`, `ticketsCarriedOver` from the backend response. Since Step 4 makes the backend return feature-only values with the same field names and types, the frontend requires zero changes. The column headers say "SP Assigned", "SP Completed", etc. -- these are correct for feature-only context since bug work is available on the Bug Ratio and Leaderboard tabs.

**This step is a verification pass.** If the developer identifies any frontend change needed (e.g., tooltip text mentioning "all tickets"), they should update it. Otherwise, mark as complete with no modifications.

**Depends on:** Step 4.

### Step 8: Update Knowledge Base entries

**Files to modify:**
- `docs/kb/analytics/health-score.md` -- Update "Input Metrics" Completion % formula to note it uses feature-only committed/completed SP. Note that combined disruption and carry-over are unchanged.
- `docs/kb/analytics/scope-change.md` -- Add note under "Burnup Chart" that totalScopeSp and completedSp are feature-only (exclude bugs). Multi-sprint fields remain total-scope.
- `docs/kb/analytics/throughput.md` -- Update all formula descriptions to note they exclude bug tickets (`&& IssueType != "Bug"`). Note that rolling average also uses feature-only SP.
- `docs/kb/cross-cutting.md` -- Add section "Feature-Only Metrics (FeatureOnlyMetrics)" documenting: which surfaces are feature-only, which remain total-scope, the filtering rule (`!IsBug(m)`), and the two-committed-SP pattern (feature committed vs total committed).

**Depends on:** Steps 1-7.

## Cross-Service Changes

None. Single-service system.

## Migration Notes

None. No data model changes. No EF migrations needed.

## Testing Strategy

1. **Feature-only SP Completed:** Sprint with 5 feature tickets (20 SP done) and 3 bug tickets (9 SP done). Verify SP Completed card shows 20, annotation shows "(+9 bug SP)".
2. **Feature-only Completion %:** Sprint with 30 SP feature committed, 20 SP feature completed, 10 SP bugs completed. Verify Completion % = 66.7% (20/30), not 75% (30/40).
3. **Bug SP annotation hidden at zero:** Sprint with zero bug tickets. Verify no annotation appears.
4. **All-bug sprint:** Sprint where all completed work is bugs. Verify SP Completed = 0, annotation shows bug total, Completion % = 0%.
5. **Health score uses feature-only:** Verify health score Completion sub-score reflects feature completion % (0% in all-bug sprint should score poorly).
6. **Burnup feature-only scope line:** Sprint with 3 committed features (15 SP) and 2 committed bugs (6 SP). Verify orange scope line starts at 15 (not 21). Bug area starts at 6.
7. **Burnup feature-only completed line:** When a feature completes, green line steps up. When a bug completes, green line does NOT step up (but bug area decreases).
8. **Throughput feature-only:** Developer with 3 features (10 SP) and 2 bugs (6 SP) all completed. Verify SP Assigned and SP Completed show feature-only values. Tickets Done = 3 (not 5).
9. **Throughput rolling average:** Verify rolling average uses feature-only SP across the 3-sprint window.
10. **Sub-team filter consistency:** Select a sub-team. Verify all feature-only metrics and bug annotation scope to that sub-team.
11. **Disruption rates unchanged:** Verify Scope Disruption Rate and Bug Disruption Rate cards show same values as before (they already use total committed SP denominator).
12. **Carry-Over Rate unchanged:** Verify carry-over includes bugs in both numerator and denominator.
13. **Multi-sprint scope change unchanged:** Verify Sprints page multi-sprint view shows total-scope values (committedSpActive, completedSp include bugs).
14. **Division by zero:** Sprint with 0 feature committed SP (all committed work is bugs). Verify Completion % = 0%, no errors.
15. **Delta consistency:** SP Completed delta compares feature-only across sprints. Completion % delta compares feature-only. Sparklines use feature-only.

## Open Questions

None.
