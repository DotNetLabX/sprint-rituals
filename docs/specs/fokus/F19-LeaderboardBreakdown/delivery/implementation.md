# LeaderboardBreakdown — Implementation

## Files Created

- `src/Services/Fokus/Fokus.API/Features/Analytics/LeaderboardService.cs` — multi/single sprint computation service with delta helpers, completed filter, bug classification; follows BugRatioService pattern exactly
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetLeaderboard/GetLeaderboardEndpoint.cs` — GET /api/analytics/leaderboard, mirrors GetBugRatioEndpoint structure
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetLeaderboard/GetLeaderboardQuery.cs` — request model and validator (same rules as GetBugRatioRequestValidator)
- `client/src/components/developers/LeaderboardChart.vue` — stacked horizontal bar chart using inline CSS percentage-width divs; no charting library
- `client/src/components/developers/LeaderboardTable.vue` — metrics table for multi and single modes with delta indicators
- `client/src/components/developers/LeaderboardTab.vue` — L1 container, determines mode, sorts developers by totalSp desc before passing to children
- `docs/kb/analytics/leaderboard.md` — KB entry for leaderboard analytics

## Files Modified

- `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs` — extended `DeveloperSummary` record with `FeatureSp`, `BugSp`, `FeatureTickets`, `BugTickets`; updated `ComputeLeaderboard` to apply excluded-from-scope filter and compute breakdown fields
- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` — registered `LeaderboardService` as scoped
- `client/src/types/index.ts` — extended `DeveloperSummary` interface with breakdown fields; added all Leaderboard* interfaces
- `client/src/api/analytics.ts` — added `getLeaderboard` function; imported `LeaderboardResponse`
- `client/src/stores/developersStore.ts` — extended `activeTab` to include `'leaderboard'`; added `leaderboard`, `leaderboardLoading`, `leaderboardError` state; added `fetchLeaderboard` action; extended `selectSprint`, `selectLastN`, `selectSubTeam`, `switchTab` to handle leaderboard tab
- `client/src/views/DashboardView.vue` — added Features/Bugs toggle with `leaderboardMode` ref; `sortedLeaderboard` computed; updated leaderboard card to show toggle, sorted rows, SP and ticket counts; tooltip wiring from help.tooltips.md
- `client/src/views/DevelopersView.vue` — added third Leaderboard tab button; extended `onTabSwitch` to accept leaderboard; added URL sync for `tab=leaderboard`; added leaderboard loading/error/content rendering; imported `LeaderboardTab`
- `docs/kb/index.md` — added Leaderboard entry under Analytics
- `docs/kb/frontend-map.md` — updated DevelopersView row to mention three tabs and leaderboard endpoint; updated component locations

## Key Decisions

- `ComputeLeaderboard` in SprintSummaryService was updated to apply `excludedFromScopeStatuses` filter — this is an alignment change noted in the plan (Dashboard leaderboard was previously not applying excluded statuses). This matches BugRatioService.CompletedMemberships.
- `LeaderboardService` does not receive `allClosedSprints` parameter (unlike BugRatioService) because there is no alert system in the leaderboard. The method signatures are simpler.
- Total SP delta polarity is hardcoded to `"neutral"` string directly in the delta constructor (not via `DeltaPolarity` helper) since neutral polarity ignores the delta sign and always returns neutral.
- LeaderboardTable uses a type narrowing function `isSingleEntry` to safely discriminate between multi and single entries for delta display.
- Steps 8 and 9 were implemented in reverse order (9 before 8) since LeaderboardTab depends on LeaderboardChart and LeaderboardTable — this is correct sequencing despite the plan numbering.

## Deviations from Plan

- None. All 11 steps implemented as specified.
