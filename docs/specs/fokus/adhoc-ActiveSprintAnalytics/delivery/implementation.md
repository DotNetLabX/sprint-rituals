# ActiveSprintAnalytics — Implementation

## Files Created
- `src/Services/Fokus/Fokus.API/Features/Sprints/GetSprints/GetSprintsEndpoint.cs` — new endpoint GET /api/sprints returning active + closed sprints as `SprintItem[]`
- `src/Services/Fokus/Fokus.API/Features/Sprints/GetSprints/GetSprintsModels.cs` — `SprintItem` DTO with `State` field added vs the old `ClosedSprintItem`

## Files Modified

### Step 1 — Repository
- `src/Services/Fokus/Fokus.Persistence/Repositories/SprintRepository.cs` — renamed `GetClosedSprintsAsync` → `GetAnalyticsSprintsAsync` (filter: Closed OR Active) and `GetAllClosedSprintMembershipsAsync` → `GetAllAnalyticsSprintMembershipsAsync` (same filter)

### Step 2 — Sprint selector endpoint
- Deleted `src/Services/Fokus/Fokus.API/Features/Sprints/GetClosedSprints/` folder (endpoint + models)
- Created replacement under `GetSprints/` at route `/api/sprints`

### Step 3 — All 15 analytics endpoints updated

**Category A — repo call rename only (no sprint-lookup logic):**
- `GetFailingTicketsEndpoint.cs` — `GetClosedSprintsAsync` → `GetAnalyticsSprintsAsync`, variable renamed
- `GetUntestedTicketsEndpoint.cs` — same
- `GetTestTimelineEndpoint.cs` — same

**Category B — multi-repo rename:**
- `GetEpicProgressEndpoint.cs` — both repo methods renamed, variables renamed to `analyticsSprints`/`analyticsMemberships`

**Category C — sparkline anchor for active sprint:**
- `GetSprintSummaryEndpoint.cs` — sparkline uses closed-only `ascending`; anchor index = `selectedIndex >= 0 ? selectedIndex : ascending.Count - 1`; error message updated
- `GetQaMetricsEndpoint.cs` — same pattern with `closedAscending`/`closedIndex`

**Category D — full active-sprint treatment (prior sprint + anchor):**
- `GetQaTrendsEndpoint.cs` — closed-only ascending for windows
- `GetQaWorkloadEndpoint.cs` — same; error message updated
- `GetDeveloperQualityEndpoint.cs` — single-sprint lookup from `allSprints`; prior = `ascending[ascending.Count - 1]` when active; anchor = `ascending.Count - 1` when target active
- `GetBugRatioEndpoint.cs` — prior = `allSprints[^1]` when target is active; alert baseline closed-only
- `GetLeaderboardEndpoint.cs` — same prior-sprint pattern
- `GetCarryOverEndpoint.cs` — same; zombie baseline closed-only
- `GetScopeChangeEndpoint.cs` — `closedIndex` used for prior-sprint resolution; prior = `ascending[^1]` when active
- `GetCycleTimeEndpoint.cs` — same pattern as ScopeChange
- `GetDeveloperThroughputEndpoint.cs` — rolling window base = `ascending.Count` when active (all closed sprints feed window)

**Error message updated in all 9 Category C+D endpoints:** `"Sprint not found or is not a closed sprint."` → `"Sprint not found."`

### Step 4 — Frontend

- `client/src/types/index.ts` — renamed `ClosedSprintItem` → `SprintItem`; comment updated
- `client/src/api/analytics.ts` — renamed `getClosedSprints` → `getSprints`, route `/sprints/closed` → `/sprints`, import type updated
- `client/src/stores/dashboardStore.ts` — `ClosedSprintItem` → `SprintItem`, `getClosedSprints` → `getSprints`, state `closedSprints` → `sprints`, return key renamed
- `client/src/stores/sprintsStore.ts` — same renames including `refreshSprints` body
- `client/src/stores/developersStore.ts` — same renames
- `client/src/stores/cycleTimeStore.ts` — same renames; default sprint selection comment updated
- `client/src/stores/qaTrendsStore.ts` — same renames
- `client/src/components/PageToolbar.vue` — prop type `ClosedSprintItem[]` → `SprintItem[]`; active sprint visual indicator: `sprint.state === 'Active'` appends `" (In Progress)"` to the label in `sprintOptions`
- `client/src/components/sprints/SprintEditDialog.vue` — prop type `ClosedSprintItem` → `SprintItem`
- `client/src/views/DashboardView.vue` — `store.closedSprints` → `store.sprints`
- `client/src/views/SprintsView.vue` — `store.closedSprints` → `store.sprints` (toolbar + empty-state guard)
- `client/src/views/CycleTimeView.vue` — same
- `client/src/views/DevelopersView.vue` — same

## Key Decisions
- **Closed-only averaging:** All last-N windows, sparklines, alert baselines, and streak counters operate exclusively on the closed-sprint `ascending` list. Active sprint only appears in single-sprint views and the selector.
- **Prior sprint for active target:** When the selected sprint is active (not in `ascending`), prior = `ascending[^1]` (most recent closed sprint). This avoids null delta cards when viewing the active sprint.
- **Sparkline anchor for active sprint:** `sparklineAnchorIndex = ascending.Count - 1` ensures the full closed-sprint history feeds the sparkline window.
- **Rolling window for throughput:** `rollingWindowBase = ascending.Count` when active so all closed sprints remain candidates for the 2-sprint expansion.
- **`SprintItem` carries `State`:** The old `ClosedSprintItem` had no `State` field. Adding it lets the frontend label active sprints without a separate API call.

## Deviations from Plan
- Q1 (plan error): Plan said "except GetScopeChange" for error message update. Architect confirmed all 9 Category C+D endpoints should get the update. Applied to all 9 including GetScopeChange.

## Review Cycle 1 Fixes

### CRITICAL — GetQaMetricsEndpoint active sprint crash
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetQaMetrics/GetQaMetricsEndpoint.cs` — when `closedIndex < 0` (active sprint), the active sprint is no longer assumed to be in `windowSprints`. It is loaded separately via `GetSprintsWithMembershipsAsync([req.SprintId])`. Status transitions now include the active sprint ID (`transitionIds`). Prior sprint for active target is `sortedWindow[^1]` (last closed in window). Sparkline base for active uses `sortedWindow.TakeLast(4)` instead of `sortedWindow.Take(-1 + 1)` which would be empty.

### HIGH — GetQaWorkloadEndpoint active sprint rejected with 400
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetQaWorkload/GetQaWorkloadEndpoint.cs` — `HandleSingleSprintAsync` now receives `allSprints` as an additional parameter. Sprint lookup at line 112 changed from `ascending.FirstOrDefault` to `allSprints.FirstOrDefault` so active sprints are accepted. Prior sprint resolution uses a computed `priorIndex` that falls back to `ascending.Count - 1` when target is active. Sparkline anchor uses `sparklineAnchorIndex = ascending.Count - 1` when `targetIndex < 0`.

### MEDIUM — KB not updated
- `docs/kb/domain/sprint.md` — added "Analytics Query Scope" section documenting `GetAnalyticsSprintsAsync` filter (Active + Closed), closed-only guard rule for windows/baselines/streaks, active-sprint prior-sprint resolution pattern, sparkline anchor pattern, and single-sprint lookup rule.

### LOW — SprintEditDialog isClosed case mismatch
- `client/src/components/sprints/SprintEditDialog.vue:26` — changed `=== 'closed'` to `=== 'Closed'` to match backend `.ToString()` serialization of `SprintState`.
