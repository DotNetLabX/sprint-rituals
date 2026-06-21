# Planning-Gated Disruption

**Feature Spec:** `docs/features/PlanningGatedDisruption/spec.md`

## Context

The Committed SP cards and the Scope Burnup chart tell inconsistent stories. The Total card uses `activeSp + removedSp` -- a synthetic number that does not correspond to a real-world question. Added SP and Removed SP count activity from sprint start, mixing planning-window adjustments with genuine mid-sprint disruption. Classification includes a "Planning Overflow" category for early additions -- but if the planning window filters them out, the category is meaningless.

This feature aligns all disruption metrics to a coherent system gated by the planning window:

- **Total** = membership at planning close (what we committed to)
- **Active** = transition-based, unchanged (what we worked on)
- **Added / Removed** = post-planning, cycle-filtered (what disrupted us)
- **Disruption Rate** = derived from the narrowed inputs

Changes span `ScopeChangeService` (primary), `SprintSummaryService` (dashboard disruption rates + mid-sprint flag), `TransitionAttributionChecker` (new method), classification logic, event table logic, and frontend tooltip updates. No new endpoints, no new entities, no data model changes.

## Scope

**In scope:**
- Change `committedSpTotal` from `activeSp + removedSp` to membership at planning cutoff
- Change `addedSp` threshold from `sprintStart` to `planningCutoff`, retain cycle-entry requirement
- Change `removedSp` to require post-planningCutoff AND cycle-entry before removal
- Remove "Planning Overflow" classification category (3 categories remain)
- Update `SprintSummaryService` scope/bug disruption rates and sparklines to use `planningCutoff`
- Update `SprintSummaryService.ComputeFlags` mid-sprint disruption flag to use `planningWindowDays`
- Update `TransitionAttributionChecker.IsAddedInSprint` to accept `planningCutoff` instead of `sprintStart`
- Update frontend tooltips from `help.tooltips.md`
- Update KB entries
- No excluded-status filter on Total (matches dashed line behavior)

**Explicitly out of scope:**
- Burnup chart changes (dashed line, scope line, completed line, bug area all unchanged)
- Carry-over formula changes
- New settings UI (planningWindowDays already exists)
- Per-developer disruption attribution
- Churn rate composite metric

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | (none) | -- | Modify TransitionAttributionChecker: update IsAddedInSprint signature, add IsRemovedPostPlanning | Cross-cutting analytics utility -- no skill |
| 2 | (none) | -- | Update ScopeChangeService.ComputeSprintMetrics: new Total, Added, Removed formulas | Service-specific inline changes |
| 3 | (none) | -- | Update ScopeChangeService classification: remove Planning Overflow, update GetMidSprintAdditions | Service-specific inline changes |
| 4 | (none) | -- | Update ScopeChangeService.BuildEventTable: new classification rules for event categories | Service-specific inline changes |
| 5 | (none) | -- | Update SprintSummaryService: disruption rate methods + ComputeFlags | Service-specific inline changes |
| 6 | (none) | -- | Update frontend tooltips and classification component | No frontend skill covers tooltip text updates |
| 7 | (none) | -- | Update KB entries | Documentation |

No skills exist for cross-cutting analytics computation changes or inline predicate replacement. All steps are "None" disposition because this feature modifies existing computation logic inside two existing services. Same pattern as TransitionBasedSprintScope plan.

## Domain Model Changes

None. No new entities, value objects, or domain events. All changes are computation-level -- reinterpreting existing data with planning-cutoff gating.

## Data Model Changes

None. No migrations needed. `planningWindowDays` already exists on `AppSettings`. `AddedAt`, `RemovedAt`, `WasCommitted` already exist on `SprintMembership`. StatusTransitions already loaded by all analytics endpoints.

## Implementation Steps

### Step 1: Update TransitionAttributionChecker

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/TransitionAttributionChecker.cs`

**What to do:**

1a. Change `IsAddedInSprint` signature from `(SprintMembership membership, bool isStarted, DateTime sprintStart)` to `(SprintMembership membership, bool isStarted, DateTime planningCutoff)`. The body remains the same (`membership.AddedAt > planningCutoff && isStarted`) -- only the parameter name changes to reflect the new semantic. The caller is responsible for passing the correct cutoff value.

Current signature (line 148):
```
public static bool IsAddedInSprint(SprintMembership membership, bool isStarted, DateTime sprintStart) =>
    membership.AddedAt > sprintStart && isStarted;
```

New signature:
```
public static bool IsAddedInSprint(SprintMembership membership, bool isStarted, DateTime planningCutoff) =>
    membership.AddedAt > planningCutoff && isStarted;
```

1b. Add a new method `IsRemovedPostPlanning` that checks removal eligibility per spec BR4:
- `membership.RemovedAt != null`
- `membership.RemovedAt > planningCutoff`
- The ticket has a transition to CycleTimeStartStage (or beyond) with timestamp within `[sprintStart, membership.RemovedAt]` -- meaning it entered the cycle during the sprint before being removed

Signature:
```
public static bool IsRemovedPostPlanning(
    SprintMembership membership,
    List<StatusTransition> ticketTransitions,
    DateTime sprintStart,
    DateTime planningCutoff,
    List<string> orderedStages,
    int startIndex) => ...
```

Return false if `RemovedAt` is null or `<= planningCutoff`. Otherwise check for a qualifying start transition where `t.Timestamp >= sprintStart && t.Timestamp <= membership.RemovedAt.Value` and `GetStageIndex(t.ToStatus, orderedStages) >= startIndex`.

1c. Update the XML doc comments on both methods to reflect the planning-gated semantics.

**Callers to update (in subsequent steps):** `ScopeChangeService.ComputeSprintMetrics` (step 2), `SprintSummaryService.ComputeMetrics` (step 5), `SprintSummaryService.ComputeScopeDisruptionRate` (step 5), `SprintSummaryService.ComputeBugDisruptionRate` (step 5).

### Step 2: Update ScopeChangeService core metrics

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/ScopeChangeService.cs`

**What to do:**

2a. **Change `ComputeSprintMetrics` -- committedSpTotal formula (spec BR1).** Replace `var committedSpTotal = activeSp + removedSp;` (line 348) with membership-at-planning-cutoff logic:

Compute `planningCutoff = sprint.StartDate.AddDays(planningWindowDays)`. Sum effective SP for all memberships where:
- `Ticket.IssueType != "Bug"` (non-bug filter)
- `GetEffectiveSp(defaultSpPerBug).HasValue`
- Present at planning cutoff: `(m.WasCommitted || m.AddedAt <= planningCutoff)` AND `(m.RemovedAt == null || m.RemovedAt > planningCutoff)`
- NO excluded-status filter (spec BR1: "No excluded-status filter applied -- Total is raw membership, matching the dashed line")

This replaces the `activeSp + removedSp` computation at line 348.

2b. **Change `ComputeSprintMetrics` -- addedSp (spec BR3).** The `IsAddedInSprint` call at line 329 currently passes `sprintStart`. Change to pass `planningCutoff` (`sprint.StartDate.AddDays(planningWindowDays)`). The cycle-entry requirement is already handled by `isStarted`. The "not removed" filter is already handled by the `if (m.RemovedAt != null) continue;` at line 319.

2c. **Change `ComputeSprintMetrics` -- removedSp (spec BR4).** Replace the current removed SP computation (lines 307-311) which sums all removed non-bug memberships. The new logic:

Remove the blanket `removedSp` LINQ at lines 307-311. Instead, compute removed SP inside the per-membership loop. For each membership with `m.RemovedAt != null`:
- Skip if bug (`m.Ticket?.IssueType == "Bug"`)
- Skip if no effective SP
- Skip if excluded status
- Use `TransitionAttributionChecker.IsRemovedPostPlanning(m, ticketTransitions, sprintStart, planningCutoff, orderedStages, startIndex)` to check eligibility
- If eligible, add to `removedSp`

This means removed tickets are now processed inside the main loop rather than in a separate LINQ. The main loop currently skips removed tickets at line 319 (`if (m.RemovedAt != null) continue;`). Restructure: split the loop logic so removed tickets go through the removal check, while non-removed tickets go through the existing started/added/completed checks.

2d. **Verify derived formulas remain correct.** `netScopeChange = addedSp - removedSp` (line 350) -- unchanged, inputs narrowed. `disruptionRate = addedSp / activeSp * 100` (line 351) -- unchanged, inputs narrowed.

**Pattern reference:** Follow the existing loop structure in `ComputeSprintMetrics` (lines 286-356). The `ScopeSprintMetrics` record does not change shape.

### Step 3: Update ScopeChangeService classification

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/ScopeChangeService.cs`

**What to do:**

3a. **Update `GetMidSprintAdditions` (lines 367-377).** Currently filters `!m.WasCommitted && m.RemovedAt == null`. The new filter must match Added SP eligibility (spec BR3): post-planning AND cycle-entered. Change to:

- Filter: `m.AddedAt > planningCutoff && m.RemovedAt == null`
- For each qualifying membership, check cycle-entry using `TransitionAttributionChecker.IsStartedInSprint`. This means `GetMidSprintAdditions` needs access to `statusTransitions`, `orderedStages`, `startIndex`, `sprintStart`, and `sprintEnd` parameters.
- Only include memberships that are started (cycle-entered)
- Non-bug filter: classification only applies to feature tickets (spec BR3: "non-bug")
- Non-excluded filter: spec BR3 requires "non-excluded"

Update the method signature to accept the additional parameters. Update call sites in `ComputeSingleSprint` (line 211) and `ComputeMultiSprint` (line 131).

3b. **Update `ClassifyAddition` (lines 379-391).** Remove the Planning Overflow branch. New priority order (spec BR8):
1. `Ticket.IssueType == "Bug"` -> "Unplanned Bug"
2. `Ticket.CreatedDate < sprint.StartDate` -> "Priority Escalation"
3. Everything else -> "Scope Injection"

The `planningCutoff` parameter is no longer needed by `ClassifyAddition` since all tickets reaching this function are already post-planning.

3c. **Update `BuildClassificationBreakdown` (lines 393-410).** Change the `categories` array from `{ "Planning Overflow", "Unplanned Bug", "Priority Escalation", "Scope Injection" }` to `{ "Unplanned Bug", "Priority Escalation", "Scope Injection" }`.

### Step 4: Update ScopeChangeService event table

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/ScopeChangeService.cs`

**What to do:**

4a. **Update `BuildEventTable` (lines 414-453).** The event table continues showing ALL add/remove events chronologically (spec BR16). Classification categories only appear for tickets that qualify as Added SP (post-planning AND cycle-entered). All other events show `null` category.

Current logic (line 433-439): all non-committed memberships get a classification category via `ClassifyAddition`. Change to:
- For each added event (`!m.WasCommitted`): only assign a classification category if the ticket qualifies as Added SP: `m.AddedAt > planningCutoff` AND cycle-entered (check `IsStartedInSprint`) AND `!IsBug` AND `!IsExcluded` AND `m.RemovedAt == null`. Otherwise, category = `null`.
- For removed events: category remains `null` (unchanged).

This means `BuildEventTable` needs additional parameters: `statusTransitions`, `orderedStages`, `startIndex`, `sprintStart`, `sprintEnd`. Update the method signature and call site (line 215).

### Step 5: Update SprintSummaryService

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs`

**What to do:**

5a. **Update `ComputeMetrics` (lines 272-331).** Pass `planningCutoff` to `IsAddedInSprint` instead of `sprintStart`. Currently line 307 passes `sprintStart`:
```
var isAdded = TransitionAttributionChecker.IsAddedInSprint(m, isStarted, sprintStart);
```
Change to:
```
var planningCutoff = sprintStart.AddDays(planningWindowDays);
var isAdded = TransitionAttributionChecker.IsAddedInSprint(m, isStarted, planningCutoff);
```
This requires adding `int planningWindowDays` to the `ComputeMetrics` parameter list. Thread it from `settings.PlanningWindowDays` at call sites (lines 119-123 and 126-131).

5b. **Update `ComputeScopeDisruptionRate` sparkline helper (lines 397-429).** Currently passes `sprint.StartDate` to `IsAddedInSprint` (line 422). Change to pass `sprint.StartDate.AddDays(settings.PlanningWindowDays)`.

5c. **Update `ComputeBugDisruptionRate` sparkline helper (lines 431-474).** Currently passes `sprint.StartDate` to `IsAddedInSprint` (line 469). Change to pass `sprint.StartDate.AddDays(settings.PlanningWindowDays)`.

5d. **Update `ComputeFlags` mid-sprint disruption (lines 803-811).** Currently uses hardcoded 2 days: `var disruptionCutoff = selectedSprint.StartDate.AddDays(2);` (line 804). Change to use `planningWindowDays`:
```
var disruptionCutoff = selectedSprint.StartDate.AddDays(planningWindowDays);
```
Add `int planningWindowDays` to `ComputeFlags` parameter list. Thread from `settings.PlanningWindowDays` at the call site (line 208-212).

### Step 6: Update frontend tooltips and classification

**Files to modify:**
- `client/src/components/sprints/ScopeMetricCards.vue`
- `client/src/components/sprints/ClassificationTable.vue`

**What to do:**

6a. **Update `ScopeMetricCards.vue` tooltip text (lines 36-45).** Replace tooltip strings with text from `docs/features/PlanningGatedDisruption/help.tooltips.md`:
- "Committed SP (Total)" -> "Feature story points in the sprint when the planning window closed -- what the team committed to going into execution."
- "Added SP" -> "Feature story points added after planning ended that entered the work cycle. Measures real disruption, not planning adjustments."
- "Removed SP" -> "Feature story points removed after planning ended that had entered the work cycle. Only counts started work that was pulled."
- "Net Scope Change" -> "Added SP minus Removed SP, both measured after planning ended. Positive means execution scope grew."
- "Disruption Rate" -> "Post-planning Added SP as a percentage of Active SP. Only counts additions that entered the cycle."

6b. **Update `ClassificationTable.vue` (lines 12-18).** Remove the "Planning Overflow" tooltip branch. Update remaining tooltips from `help.tooltips.md`:
- "Unplanned Bug" -> "Bug-type tickets added after the planning window that entered the work cycle -- reactive quality work."
- "Priority Escalation" -> "Pre-existing tickets (created before sprint start) pulled in after the planning window -- reprioritized work."
- "Scope Injection" -> "New work added after the planning window that entered the cycle -- truly unplanned scope."

6c. **Update `ClassificationTable.vue` section header tooltip (line 28).** Change from "How mid-sprint additions are categorized by timing and type." to reflect planning-gated semantics: "How post-planning additions that entered the work cycle are classified by type."

### Step 7: Update KB entries

**Files to modify:**
- `docs/kb/analytics/scope-change.md`

**What to do:**

7a. **Update Core Formulas section.** Change:
- `committedSpTotal` formula from `activeSp + removedSp` to "membership at planning cutoff (feature tickets present at planningCutoff, non-bug, no excluded-status filter)"
- `addedSp` formula from `AddedAt > sprintStart AND isStarted` to `AddedAt > planningCutoff AND isStarted`
- `removedSp` formula from `RemovedAt-based, unchanged` to `RemovedAt > planningCutoff AND had entered cycle before removal`

7b. **Update Classification section.** Remove Planning Overflow. Update to 3 categories with new priority order:
1. Unplanned Bug (IssueType == "Bug")
2. Priority Escalation (CreatedDate < sprintStart)
3. Scope Injection (everything else)

Note: classification only applies to tickets that qualify as Added SP (post-planning, cycle-entered, non-bug, non-excluded).

7c. **Add a note** in the Dashboard section of `docs/kb/cross-cutting.md` or at the end of `scope-change.md` noting that Dashboard disruption rates (SprintSummaryService) now use `planningCutoff` as the addition threshold, and the mid-sprint disruption flag uses `planningWindowDays` instead of hardcoded 2 days.

## Cross-Service Changes

None. Single-service system.

## Migration Notes

None. No database changes.

## Testing Strategy

**Manual verification scenarios:**

1. **Total card value = dashed line at planning cutoff day.** Open a single sprint, verify the Committed SP (Total) card value matches the dashed line's Y-value on the planning cutoff day in the burnup chart.

2. **Planning-window additions invisible in Added SP.** Add a ticket during the planning window (day 1-2 with default settings). Verify it does NOT appear in Added SP, even if it enters the cycle.

3. **Post-planning cycle-entered addition counts.** Add a ticket after planning window, transition it to start stage. Verify it appears in Added SP and has a classification category.

4. **Post-planning non-started addition ignored.** Add a ticket after planning window but never start it. Verify it does NOT appear in Added SP or classification. It should appear in the event table with no category.

5. **Removed SP requires cycle entry.** Remove a ticket post-planning that was never started. Verify it does NOT count in Removed SP. The dashed line in the burnup chart should show the removal.

6. **Removed SP counts cycle-entered removals.** Remove a ticket post-planning that had been started. Verify it counts in Removed SP.

7. **Planning Overflow gone from classification.** Verify only 3 categories: Unplanned Bug, Priority Escalation, Scope Injection.

8. **Dashboard disruption rates match Sprints page.** Compare Scope Disruption Rate on Dashboard with Disruption Rate on Sprints page for the same sprint -- they should use the same planning cutoff.

9. **Mid-sprint disruption flag uses planningWindowDays.** Set planningWindowDays to 3. Add a ticket on day 3. Verify the Dashboard does NOT flag it as mid-sprint disruption.

10. **planningWindowDays = 0 edge case.** Set to 0. Verify planningCutoff = sprintStart, so all post-start cycle-entered additions count as disruption.

11. **Multi-sprint mode.** View last 5 sprints. Verify per-sprint data uses new formulas. Classification breakdown shows 3 categories.

## Open Questions

None.
