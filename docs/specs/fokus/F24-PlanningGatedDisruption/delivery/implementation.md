# Planning-Gated Disruption — Implementation

## Files Created

None.

## Files Modified

- `src/Services/Fokus/Fokus.API/Features/Analytics/TransitionAttributionChecker.cs` — Renamed `IsAddedInSprint` parameter from `sprintStart` to `planningCutoff` (semantic change only, body unchanged). Added `IsRemovedPostPlanning` method checking `RemovedAt > planningCutoff` AND qualifying start transition in `[sprintStart, RemovedAt]`. Updated XML doc comments on both methods.

- `src/Services/Fokus/Fokus.API/Features/Analytics/ScopeChangeService.cs` — Step 2: Restructured `ComputeSprintMetrics` — removed blanket LINQ for `removedSp`, split removed-ticket handling into the main loop using `IsRemovedPostPlanning`, changed `IsAddedInSprint` to pass `planningCutoff`, replaced `committedSpTotal = activeSp + removedSp` with membership-at-planning-cutoff LINQ (no excluded-status filter per spec BR1). Step 3: Updated `GetMidSprintAdditions` with additional parameters (`statusTransitions`, `orderedStages`, `startIndex`, `excludedStatuses`) and new filter logic (post-planning AND cycle-entered AND non-bug AND non-excluded); removed `ClassifyAddition` Planning Overflow branch; updated `BuildClassificationBreakdown` categories to 3. Step 4: Updated `BuildEventTable` with additional parameters; classification category now only assigned when ticket qualifies as Added SP (post-planning AND cycle-entered AND non-bug AND non-excluded AND not removed). Updated both call sites in `ComputeMultiSprint` and `ComputeSingleSprint`.

- `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs` — Step 5: Added `planningWindowDays` parameter to `ComputeMetrics`, computing `planningCutoff` and passing it to `IsAddedInSprint`. Updated `ComputeScopeDisruptionRate` and `ComputeBugDisruptionRate` sparkline helpers to pass `planningCutoff` instead of `sprint.StartDate`. Added `planningWindowDays` parameter to `ComputeFlags`, replaced hardcoded `AddDays(2)` with `AddDays(planningWindowDays)`. Updated all call sites to thread `settings.PlanningWindowDays`.

- `client/src/components/sprints/ScopeMetricCards.vue` — Updated `singleCardTooltip` strings for Committed SP (Total), Added SP, Removed SP, Net Scope Change, and Disruption Rate to planning-gated semantics per `help.tooltips.md`.

- `client/src/components/sprints/ClassificationTable.vue` — Removed Planning Overflow branch from `categoryTooltip`. Updated tooltips for Unplanned Bug, Priority Escalation, Scope Injection to planning-window-agnostic text per `help.tooltips.md`. Updated section header tooltip to "How post-planning additions that entered the work cycle are classified by type." Removed unused `settingsStore` import and instantiation.

- `docs/kb/analytics/scope-change.md` — Updated Core Formulas section: new `committedSpTotal` formula, updated `addedSp` and `removedSp` formulas with planning-cutoff gating. Updated Classification section: removed Planning Overflow, 3 categories, added note that classification only applies to Added SP-qualifying tickets.

- `docs/kb/cross-cutting.md` — Added Planning-Gated Disruption section documenting all Dashboard service changes: `planningCutoff` usage in disruption rate helpers, `planningWindowDays` in `ComputeFlags`, renamed `IsAddedInSprint` parameter, new `IsRemovedPostPlanning` method.

## Key Decisions

- `GetMidSprintAdditions` now excludes bugs and excluded-status tickets before cycle-entry check, matching the Added SP eligibility criteria exactly (spec BR3).
- `BuildEventTable` still shows ALL non-committed memberships (spec BR16 — all add/remove events chronologically), but only assigns a classification category to qualifying tickets. Pre-planning additions appear with `category = null`.
- `committedSpTotal` uses no excluded-status filter per spec BR1 ("matching the dashed line behavior").
- The `ClassifyAddition` method lost its `planningCutoff` parameter since all tickets reaching it are already post-planning gated.

## Deviations from Plan

None. All steps implemented as specified.

## Review Fixes (Cycle 1)

- `src/Services/Fokus/Fokus.API/Features/Analytics/ScopeChangeService.cs` — Added `if (IsExcluded(m.FinalStatus, excludedStatuses)) continue;` inside the `m.RemovedAt != null` branch, after the bug check and before `IsRemovedPostPlanning`. Fixes HIGH finding: removed tickets were bypassing the excluded-status filter entirely.

- `docs/kb/cross-cutting.md` — Updated `IsAddedInSprint` signature in the "Transition-Based Sprint Scope" Key methods list from old `sprintStart` parameter to `planningCutoff`. Fixes MEDIUM finding: contradiction between line 110 (stale) and the new PlanningGatedDisruption section at line 141.

- `docs/kb/analytics/scope-change.md` — Added `AND !excluded` to the `removedSp` formula line. Fixes LOW finding: formula was missing the non-excluded condition that now exists in code.
