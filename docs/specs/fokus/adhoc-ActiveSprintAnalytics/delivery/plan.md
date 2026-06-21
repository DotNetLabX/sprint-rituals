# Include Active Sprint in All Analytics Views

**Feature Spec:** None

## Context

Every analytics endpoint and UI view is built on `SprintRepository.GetClosedSprintsAsync`, which filters to `State == Closed`. Users cannot see real-time data for the current sprint anywhere — not in the dashboard, sprint views, developer analytics, or QA trends. `Sprint.State` already exists and is persisted; this is purely a query scope change.

**Services impacted:** Fokus (the only service). Repository → 16 analytics endpoints → 6 frontend stores.

## Scope

**In scope:**
- Widen `GetClosedSprintsAsync` to include Active sprints (rename to `GetAnalyticsSprintsAsync`)
- Widen `GetAllClosedSprintMembershipsAsync` to include Active sprint memberships (rename to `GetAllAnalyticsSprintMembershipsAsync`)
- Rename `ClosedSprintItem` → `SprintItem` (backend DTO + frontend type)
- Update all 16 analytics endpoint consumers to use the renamed method
- Guard all last-N averaging, sparkline windows, and alert baselines to count from closed sprints only
- Update frontend types, API calls, stores, and PageToolbar
- Add visual indicator for the active sprint in the sprint selector dropdown

**Out of scope:**
- Active sprint as a sparkline data point (sparklines remain closed-only per user decision)
- Domain model changes (Sprint.State already exists)
- Migrations (no schema change)
- Future sprints (only Active and Closed are included)

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | (none) | — | Repository method rename + filter widening | |
| 2 | (none) | — | Endpoint + DTO rename, folder rename | |
| 3 | (none) | — | 15 analytics endpoints, categorized by treatment | |
| 4 | (none) | — | Frontend type/store/API rename + active indicator | |

No existing skills cover repository query widening, endpoint renaming, or consumer migration. All steps are refactoring of existing code.

## Domain Model Changes

None. `Sprint.State` (enum: Active, Closed, Future) already exists and is persisted.

## Data Model Changes

None. No schema or migration changes.

## Implementation Steps

### Step 1: Repository — Widen and rename query methods

Rename and widen the two query methods that gate analytics data. The filter changes from `State == Closed` to `State == Closed || State == Active`. Ordering stays `StartDate` descending (active sprint appears first in the list).

**No matching skill** — repository method refactoring.

**Files:**
- Modify: `src/Services/Fokus/Fokus.Persistence/Repositories/SprintRepository.cs`
  - `GetClosedSprintsAsync` → `GetAnalyticsSprintsAsync`: change filter from `s.State == SprintState.Closed` to include `SprintState.Active`
  - `GetAllClosedSprintMembershipsAsync` → `GetAllAnalyticsSprintMembershipsAsync`: same filter change on `sm.Sprint.State`

**Accept:**
- `GetAnalyticsSprintsAsync` returns both Closed and Active sprints, ordered by `StartDate` descending
- `GetAllAnalyticsSprintMembershipsAsync` returns memberships for both Closed and Active sprints
- Old method names no longer exist (compiler will flag all 16 consumers)
- Future sprints are still excluded
- When no active sprint exists, behavior is identical to today (only closed sprints returned)

**Pattern reference:** `SprintRepository.cs:17-21` (current implementation)

**Dependencies:** None

---

### Step 2: Sprint selector endpoint — Rename DTO, endpoint, and route

Rename the sprint list endpoint and its DTO to reflect the broader scope. This is the canonical source for the frontend sprint picker.

**No matching skill** — endpoint renaming.

**Files:**
- Rename folder: `src/Services/Fokus/Fokus.API/Features/Sprints/GetClosedSprints/` → `GetSprints/`
- Rename + modify: `GetClosedSprintsEndpoint.cs` → `GetSprintsEndpoint.cs`
  - Class name: `GetClosedSprintsEndpoint` → `GetSprintsEndpoint`
  - Route: `/api/sprints/closed` → `/api/sprints`
  - Update repository call to `GetAnalyticsSprintsAsync`
- Rename + modify: `GetClosedSprintsQuery.cs` → `GetSprintsModels.cs`
  - DTO: `ClosedSprintItem` → `SprintItem`
  - Properties unchanged (Id, Name, StartDate, EndDate, State, Goal)

**Accept:**
- `GET /api/sprints` returns both Closed and Active sprints
- `SprintItem` DTO carries `State` as a string (`"Active"` or `"Closed"`)
- Old route `/api/sprints/closed` no longer exists
- Old type name `ClosedSprintItem` no longer exists (compiler flags all backend consumers)

**Pattern reference:** `GetClosedSprintsEndpoint.cs` (current implementation)

**Dependencies:** Step 1

---

### Step 3: Analytics endpoints — Update all consumers

Update all 15 remaining analytics endpoints that call `GetClosedSprintsAsync`. Each endpoint falls into one of four categories with a different treatment. The key constraint: **last-N averaging, sparkline windows, and alert baselines must count from closed sprints only.** The active sprint appears in per-sprint breakdowns and single-sprint views, but is excluded from aggregate computations.

**No matching skill** — consumer migration across multiple endpoints.

**Category A — 404 guard only (2 endpoints):**
Accept the active sprint ID as valid. No averaging logic to guard.

- Modify: `src/Services/Fokus/Fokus.API/Features/Analytics/GetFailingTickets/GetFailingTicketsEndpoint.cs`
- Modify: `src/Services/Fokus/Fokus.API/Features/Analytics/GetUntestedTickets/GetUntestedTicketsEndpoint.cs`

**Category B — Single-sprint view, no N-sprint averaging (2 endpoints):**
Update repository call. Active sprint shows partial data — that is the intended behavior. For `GetEpicProgress`, also update the membership query to `GetAllAnalyticsSprintMembershipsAsync`.

- Modify: `src/Services/Fokus/Fokus.API/Features/Sprints/GetTestTimeline/GetTestTimelineEndpoint.cs`
- Modify: `src/Services/Fokus/Fokus.API/Features/Analytics/GetEpicProgress/GetEpicProgressEndpoint.cs`

**Category C — Hardcoded sparkline windows (2 endpoints):**
Update repository call. Sparklines remain closed-only: filter to `State == Closed` before building the sparkline window (`TakeLast(4)`). The active sprint is still accessible for single-sprint selection but does not appear in sparkline data points.

- Modify: `src/Services/Fokus/Fokus.API/Features/Analytics/GetSprintSummary/GetSprintSummaryEndpoint.cs`
- Modify: `src/Services/Fokus/Fokus.API/Features/Analytics/GetQaMetrics/GetQaMetricsEndpoint.cs`

**Category D — Last-N averaging (7 endpoints):**
Update repository call. Add guard: count N from closed sprints only (filter to `State == Closed` before `TakeLast(N)`). The active sprint is included in per-sprint breakdowns when selected individually, but excluded from aggregate averages, alert baselines, and rolling windows. Each of these endpoints has its own windowing logic — apply the closed-only guard at the point where N is counted.

- Modify: `src/Services/Fokus/Fokus.API/Features/Analytics/GetQaTrends/GetQaTrendsEndpoint.cs` — also guard the N+1 bug-ratio correlation extension
- Modify: `src/Services/Fokus/Fokus.API/Features/Analytics/GetQaWorkload/GetQaWorkloadEndpoint.cs` — also guard the 4-sprint sparkline in single mode and the alert baseline
- Modify: `src/Services/Fokus/Fokus.API/Features/Analytics/GetDeveloperQuality/GetDeveloperQualityEndpoint.cs` — also guard the 4-sprint sparkline and 12-sprint streak window
- Modify: `src/Services/Fokus/Fokus.API/Features/Analytics/GetBugRatio/GetBugRatioEndpoint.cs` — also guard the alert baseline (all-sprints load)
- Modify: `src/Services/Fokus/Fokus.API/Features/Analytics/GetLeaderboard/GetLeaderboardEndpoint.cs` — also guard the alert baseline
- Modify: `src/Services/Fokus/Fokus.API/Features/Analytics/GetCarryOver/GetCarryOverEndpoint.cs` — also guard zombie sprint counting
- Modify: `src/Services/Fokus/Fokus.API/Features/Analytics/GetScopeChange/GetScopeChangeEndpoint.cs`
- Modify: `src/Services/Fokus/Fokus.API/Features/Analytics/GetCycleTime/GetCycleTimeEndpoint.cs`
- Modify: `src/Services/Fokus/Fokus.API/Features/Analytics/GetDeveloperThroughput/GetDeveloperThroughputEndpoint.cs` — also guard the +2 rolling average window expansion

**Error messages:** Nine endpoints in Category C and D contain the error string `"Sprint not found or is not a closed sprint."`. Update all nine to `"Sprint not found."` since active sprints are now valid selections. The nine are: GetSprintSummary, GetQaWorkload, GetDeveloperQuality, GetBugRatio, GetLeaderboard, GetCarryOver, GetScopeChange, GetCycleTime, GetDeveloperThroughput.

**Accept:**
- All 15 endpoints compile with the renamed repository method
- Selecting the active sprint in any single-sprint view shows its current (partial) data
- "Last N" multi-sprint views count N from closed sprints only — active sprint data never appears in aggregate averages
- Sparkline windows use closed sprints only
- Alert baselines use closed sprints only
- `GetQaTrends` N+1 extension and `GetDeveloperThroughput` +2 rolling window only pull from closed sprints

**Pattern reference:** Each endpoint follows the same pattern — `var sprints = await sprintRepository.GetAnalyticsSprintsAsync(ct);` then partitions the list by state as needed.

**Dependencies:** Step 1

---

### Step 4: Frontend — Rename types, update stores and API, add active indicator

Rename `ClosedSprintItem` → `SprintItem` across the frontend. Update the API call to the new route. Update all stores that hold sprint lists. Add a visual indicator so users can distinguish the active sprint in dropdowns.

**No matching skill** — frontend refactoring.

**Files — Type rename:**
- Modify: `client/src/types/index.ts` — rename `ClosedSprintItem` to `SprintItem`

**Files — API module:**
- Modify: the API module that defines `getClosedSprints()` — rename function to `getSprints()`, update route from `/sprints/closed` to `/sprints`

**Files — Store updates (rename `closedSprints` → `sprints` in state, update API calls):**
- Modify: `client/src/stores/dashboardStore.ts`
- Modify: `client/src/stores/sprintsStore.ts`
- Modify: `client/src/stores/developersStore.ts`
- Modify: `client/src/stores/cycleTimeStore.ts`
- Modify: `client/src/stores/qaTrendsStore.ts`

**Files — View updates (rename `store.closedSprints` → `store.sprints` in template bindings):**
- Modify: `client/src/views/DashboardView.vue`
- Modify: `client/src/views/SprintsView.vue`
- Modify: `client/src/views/DevelopersView.vue`
- Modify: `client/src/views/CycleTimeView.vue`

**Files — Component updates:**
- Modify: `client/src/components/PageToolbar.vue` — update prop type from `ClosedSprintItem[]` to `SprintItem[]`. Add visual indicator for active sprint in the dropdown options (e.g., append " (In Progress)" to the option label when `sprint.state === 'Active'`, or use a distinct CSS class).
- Modify: `client/src/components/settings/SyncTab.vue` — update inline sprint type if it references `ClosedSprintItem`
- Modify: `client/src/components/SprintEditDialog.vue` — update prop type
- Modify: `client/src/composables/useQualityAverages.ts` — verify it operates on server-provided breakdowns only (no client-side filtering needed if Step 3 already excludes active sprint from multi-sprint aggregates)

**Grep check:** Search for `ClosedSprintItem`, `closedSprints`, and `getClosedSprints` across `client/src/` to catch any references not listed above.

**Accept:**
- `ClosedSprintItem` type no longer exists in the frontend — all references use `SprintItem`
- `closedSprints` state property renamed to `sprints` in all stores
- API calls hit `/api/sprints` (new route)
- Active sprint appears in the sprint selector dropdown with a visual indicator
- All stores and views compile and render correctly with the active sprint in the list
- Frontend averages in `useQualityAverages.ts` are unaffected (they operate on server-provided per-sprint breakdowns)

**Dependencies:** Steps 2, 3

---

## Cross-Service Changes

None. Fokus is a single-service system.

## Migration Notes

None. No schema changes.

## Testing Strategy

### Backend — Repository
- `GetAnalyticsSprintsAsync` returns Active + Closed sprints, excludes Future
- Ordering: Active sprint first (most recent StartDate), then Closed by StartDate desc

### Backend — Analytics endpoints
- For each Category D endpoint: call with `last=3` when there are 5 closed + 1 active sprint. Verify the response includes exactly 3 closed sprints in the aggregate, not the active sprint.
- For Category C endpoints: verify sparkline has exactly 4 points, all from closed sprints
- For single-sprint views: select the active sprint by ID, verify it returns data
- For 404-guard endpoints: verify active sprint ID is accepted (not 404)

### Frontend
- Sprint selector shows active sprint with visual indicator
- Selecting active sprint navigates to single-sprint view with partial data
- "Last 5 Sprints" aggregate view excludes active sprint from averages
- "All Sprints" aggregate view excludes active sprint from averages

## KB Impact

Update `docs/kb/sprint-analytics.md` (or the relevant KB entry covering sprint queries) to document that analytics queries now include Active sprints, with the closed-only guard for averaging.

## Open Questions

None.
