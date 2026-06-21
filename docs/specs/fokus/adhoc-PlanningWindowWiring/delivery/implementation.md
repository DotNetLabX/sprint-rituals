# PlanningWindowWiring — Implementation

## Bugs Fixed
- **Bug 5:** Planning window setting not wired — chart and classification used hardcoded 2-day value
- **Bug 6:** Active/Total committed SP always equal — metric definition changed per PO direction

## Files Modified

### Bug 5: Planning window wiring

- `src/Services/Fokus/Fokus.API/Features/Analytics/ScopeChangeService.cs` — Added `int planningWindowDays` parameter to `BuildBurnupData`, replaced hardcoded `dayNumber <= 2` with `dayNumber <= planningWindowDays`. Passed `settings.PlanningWindowDays` at call site.
- `client/src/stores/settingsStore.ts` — Added `planningWindowDays: 2` and `syncBackSprintCount: 20` to default initializer (both were missing).
- `client/src/components/sprints/BurnupChart.vue` — Imported settingsStore, made subtitle text dynamic (`Days 1-N are the planning phase`).
- `client/src/components/sprints/ClassificationTable.vue` — Imported settingsStore, made 4 category tooltip texts dynamic using template literals.
- `client/src/components/dashboard/SprintFlags.vue` — Imported settingsStore, made mid-sprint disruption tooltip dynamic with `:title` binding.

### Bug 6: Active/Total committed SP redefinition

- `src/Services/Fokus/Fokus.API/Features/Analytics/ScopeChangeService.cs` — Changed `ComputeSprintMetrics` to accept `DateTime sprintStartDate` and `int planningWindowDays`. Redefined:
  - **Total** = all originally committed SP (`WasCommitted`, regardless of removal)
  - **Active** = committed SP that survived past the planning window (`WasCommitted && (RemovedAt == null || RemovedAt > planningCutoff)`)
  - Updated `ComputePerSprintData` signature and all call sites to pass new parameters.

## Key Decisions
- **PO option 1 (strict RemovedAt):** Active uses only `RemovedAt` timing, not status-based filtering. Excluded status filter retained for `addedSp` and `completedSp` only.
- **DisruptionRate denominator changed:** Now uses post-planning Active (stabilized scope) instead of status-filtered committed SP. More meaningful but changes historical values.

## Deviations from Plan
- No formal plan — solo bug fix. PO provided direction on bug 6 definition.
