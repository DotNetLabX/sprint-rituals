# Normalized Capacity Indicator — Implementation

## Files Modified

- `src/Services/Fokus/Fokus.API/Features/Analytics/LeaderboardService.cs` — Added `CapacityPercent` field to `LeaderboardDeveloperSprintBreakdown`, `LeaderboardDeveloperEntry`, and `LeaderboardDeveloperSingleEntry` records. Added private `GetCapacity` helper (same pattern as `DeveloperThroughputService`). Updated `ComputeMultiSprint` to accept `capacityLookup` and `allDevelopers`, call `GetCapacity` per sprint breakdown, and compute average capacity for the entry-level record. Updated `ComputeSingleSprint` to accept the same parameters and call `GetCapacity` for the target sprint.

- `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs` — Added `CapacityPercent` field to `DeveloperSummary` record. Updated `ComputeSummary` signature to accept `capacityLookup`, `allDevelopers`. Updated `ComputeLeaderboard` signature to accept `capacityLookup`, `allDevelopers`, `sprintId`. Capacity resolution inlined in `ComputeLeaderboard` (check lookup, fall back to developer default, default 100) — same logic as `GetCapacity` helper, inline rather than extracted since `ComputeLeaderboard` is private/static.

- `src/Services/Fokus/Fokus.API/Features/Analytics/GetLeaderboard/GetLeaderboardEndpoint.cs` — Built capacity lookup dictionary from already-loaded `capacityRecords` (same pattern as `DeveloperThroughputService`). Passed `capacityLookup` and `allDevelopers` to both `ComputeSingleSprint` and `ComputeMultiSprint` calls.

- `src/Services/Fokus/Fokus.API/Features/Analytics/GetSprintSummary/GetSprintSummaryEndpoint.cs` — Built capacity lookup dictionary from already-loaded `capacityRecords`. Passed `capacityLookup` and `allDevelopers` to `ComputeSummary` call.

- `client/src/types/index.ts` — Added `capacityPercent: number` to `LeaderboardDeveloperSprintBreakdown`, `LeaderboardDeveloperEntry`, `LeaderboardDeveloperSingleEntry`, and `DeveloperSummary` interfaces.

- `client/src/views/DevelopersView.vue` — Added `InfoTooltip` import. Added `normalizedSp` helper (3-line formula, returns null at >=100% or <=0%). Added `avgNormalizedSpCompleted` helper (per-sprint normalize then average, returns null if no reduced-capacity sprints — BR6). Added `(~X)` span after SP Completed value in single-sprint table. Added `(~X)` span after avgSpCompleted in multi-sprint table. Added `InfoTooltip` component to both SP Completed / Avg SP Completed column headers.

- `client/src/components/developers/LeaderboardTable.vue` — Added `normalizedTotalSp` helper that handles both multi-sprint mode (per-sprint normalize then average via `sprintBreakdowns`) and single-sprint mode (normalize by `capacityPercent` on the entry). Added `(~X)` span after `totalSp.toFixed(1)` in Total SP cell.

- `client/src/views/DashboardView.vue` — Added `normalizedSp` helper. Added `(~X)` span after the SP value in leaderboard rows, inside the existing SP span. Follows the features/bugs toggle automatically since it references the same expression.

- `docs/kb/analytics/leaderboard.md` — Added `## Normalized SP Indicator` section covering normalization formula, display rules, capacity resolution logic, and multi-sprint averaging approach (BR6).

## Key Decisions

- `ComputeLeaderboard` in `SprintSummaryService` has capacity logic inlined rather than calling a shared helper — it's a private static method and the lookup is already available in-scope. Consistent with the service's existing style.
- Step 9 (tooltip wiring) was implemented alongside steps 6-8: `title` attributes on all `(~X)` spans use the exact text from `help.tooltips.md`, and `InfoTooltip` was added to throughput column headers as specified.

## Deviations from Plan

None. All 9 steps implemented as specified.
