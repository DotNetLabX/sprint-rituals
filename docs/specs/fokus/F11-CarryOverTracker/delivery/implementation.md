# Carry-Over Tracker — Implementation

## Files Created

- `src/Services/Fokus/Fokus.API/Features/Analytics/CarryOverService.cs` — All carry-over response records plus CarryOverService class with ComputeMultiSprint and ComputeSingleSprint methods. Includes sub-team filtering, carry-over identification, status distribution, issue type breakdown, zombie detection, carry-over destination, zombie trajectory, and metric card builder (duplicated from ScopeChangeService as recommended by plan).
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetCarryOver/GetCarryOverQuery.cs` — GetCarryOverRequest and GetCarryOverRequestValidator (mirrors GetScopeChangeRequestValidator pattern).
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetCarryOver/GetCarryOverEndpoint.cs` — GET /api/analytics/carry-over endpoint, loads all closed sprints with memberships upfront for zombie detection accuracy, delegates to CarryOverService.
- `client/src/components/sprints/CarryOverMetricCards.vue` — Metric cards for both multi (3 summary cards) and single (3 delta cards) modes.
- `client/src/components/sprints/CarryOverRateChart.vue` — Line chart showing carry-over rate per sprint with sprint-click emit.
- `client/src/components/sprints/CarryOverStackedChart.vue` — Stacked bar chart of carry-over SP by workflow stage per sprint with sprint-click emit.
- `client/src/components/sprints/IssueTypeBreakdown.vue` — Table of carry-over tickets by raw issue type (both modes).
- `client/src/components/sprints/ZombieSummaryTable.vue` — Zombie tickets summary table (multi-sprint mode).
- `client/src/components/sprints/StatusDistributionChart.vue` — Donut chart of carry-over by workflow stage (single-sprint mode).
- `client/src/components/sprints/CarryOverDestination.vue` — Prior sprint outcomes with horizontal stacked bar (single-sprint mode).
- `client/src/components/sprints/CarryOverTicketTable.vue` — Full carry-over ticket table grouped by workflow stage with zombie and excluded indicators (single-sprint mode). Flat table fallback (BR20) when all tickets map to "Other" (no workflow stages configured).
- `client/src/components/sprints/ZombieTrajectorySection.vue` — Zombie tickets with sprint-by-sprint trajectory (single-sprint mode).

## Files Modified

- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` — Added `services.AddScoped<CarryOverService>();` after ScopeChangeService registration.
- `client/src/types/index.ts` — Added all carry-over TypeScript interfaces before the Sync types section.
- `client/src/api/analytics.ts` — Added CarryOverResponse import and getCarryOver() function.
- `client/src/stores/sprintsStore.ts` — Added carryOver ref, replaced fetchScopeChange with fetchAllData that calls getScopeChange and getCarryOver in parallel via Promise.all.
- `client/src/views/SprintsView.vue` — Imported all 9 carry-over components, added "Carry-Over Analysis" section below scope change with conditional multi/single rendering.

## Key Decisions

- `GetZombieTicketKeys` returns a `Dictionary<string, int>` (ticket key → sprint count) for all zombies. This lets both modes determine zombie status and sprint count in a single pass over `allSyncedSprints`.
- `GetCarryOverEndpoint` loads ALL closed sprints with memberships upfront (one query) rather than separate queries for selected range + full history. The plan explicitly called this out as simpler and appropriate for the dataset size.
- `fetchScopeChange` in the store was renamed to `fetchAllData` as recommended by the plan. The old name `fetchScopeChange` is no longer exported — callers in the view used the store actions (`selectSprint`, `selectLastN`, `selectSubTeam`) which are unchanged.
- Zombie detection counts all sprint appearances across `allSyncedSprints`, not just the selected range, matching BR7.

## Post-Review Fixes (critic round)

- `CarryOverService.cs` — `BuildIssueTypeBreakdown`: excluded-from-scope tickets now excluded from issue type counts and percentages (BR3). The original implementation included them in counts but only excluded them from SP totals.
- `CarryOverTicketTable.vue` — Added `isFlat` computed property detecting when all tickets have `workflowStage === 'Other'`. When true, renders a flat table without stage group headers or workflow stage column (BR20).

## Post-Review Fixes (reviewer round 1)

- `CarryOverService.cs` — `BuildStatusDistribution`: excluded-from-scope tickets are now filtered out before computing `total` and stage groups. They are invisible in status distribution entirely (not just excluded from SP), so the donut chart only shows non-excluded carry-over tickets.
- `CarryOverService.cs` — `BuildCarryOverDestination`: added `excludedStatuses` parameter. `priorCarryOverSp` now filters out tickets with excluded-from-scope final statuses before summing SP.

## Deviations from Plan

- None. All 6 steps implemented as specified.
