# Leaderboard Breakdown

**Feature Spec:** `docs/features/LeaderboardBreakdown/spec.md`

## Context

The Dashboard leaderboard currently shows a single "SP completed" total per developer, blending feature work and bug fixes into one number. This hides work composition. This feature splits the leaderboard into Features/Bugs modes on the Dashboard, extends the sprint summary response with the breakdown data, and adds a dedicated Leaderboard tab on the Developers page with multi-sprint aggregation, stacked bar charts, and delta indicators.

**Service impacted:** Fokus (single service — API, Domain, Persistence, frontend)

## Scope

**In scope:**
- Extend `SprintSummaryService.ComputeLeaderboard` to split SP/tickets by bug vs non-bug, applying excluded-from-scope statuses
- Extend `DeveloperSummary` response record with breakdown fields (featureSp, bugSp, featureTickets, bugTickets)
- New `LeaderboardService` computation service (multi-sprint aggregation, single-sprint with deltas)
- New `GET /api/analytics/leaderboard` endpoint with query params (sprintId, last, subTeam)
- Frontend types for leaderboard API responses
- API module function `getLeaderboard` in `analytics.ts`
- Extend `developersStore` with Leaderboard tab state and data fetching
- Dashboard leaderboard Features/Bugs toggle with frontend-only sorting
- Developers page Leaderboard tab: stacked horizontal bar chart, metrics table
- Tooltip wiring from `help.tooltips.md`

**Out of scope:** Investment profile categories, per-developer detail page, trend line chart, gamification, export, real-time updates.

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | (none) | — | Extend existing DeveloperSummary record and ComputeLeaderboard method — modifications to an existing service, not creation | |
| 2 | extract-feature-service | Follow | LeaderboardService in Features/Analytics/, multi/single response records | |
| 3 | create-feature | Follow | FastEndpoints GET endpoint, GetLeaderboardRequest + validator, route `/api/analytics/leaderboard` | |
| 4 | create-vue-feature | Follow | Types for LeaderboardResponse in index.ts | |
| 5 | create-vue-feature | Follow | API function in analytics.ts, same pattern as getBugRatio | |
| 6 | pinia-patterns | Follow | Extend developersStore with leaderboard tab state, fetchLeaderboard action | |
| 7 | vue-component-architecture | Follow | Dashboard leaderboard toggle, re-sort logic, L1 feature component | |
| 8 | vue-component-architecture | Follow | LeaderboardTab L1 container with stacked bar chart + table children | |
| 9 | vue-patterns | Follow | L3 components: LeaderboardChart, LeaderboardTable | |
| 10 | (none) | — | DevelopersView tab bar extension (third tab) and URL sync | |
| 11 | (none) | — | KB update | |

## Domain Model Changes

None. No new aggregates, entities, value objects, or domain events. All data is computed from existing sprint memberships and tickets.

## Data Model Changes

None. No schema migrations required.

## Implementation Steps

### Step 1: Extend SprintSummaryService leaderboard computation

**What:** Modify `DeveloperSummary` record to add breakdown fields and update `ComputeLeaderboard` to split completed work by bug vs non-bug. Apply excluded-from-scope statuses (aligning Dashboard leaderboard with BugRatio pattern).

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs` — extend `DeveloperSummary` record, modify `ComputeLeaderboard` method (lines 484-504)

**Feature-specific inputs:**

Extend `DeveloperSummary` record:
- Add `FeatureSp` (decimal) — SP from non-bug completed tickets
- Add `BugSp` (decimal) — SP from bug completed tickets
- Add `FeatureTickets` (int) — count of non-bug completed tickets (regardless of SP)
- Add `BugTickets` (int) — count of bug completed tickets (regardless of SP)
- Retain existing `SpCompleted` for backward compatibility (total = FeatureSp + BugSp)

Computation rules (from spec BR1-5, BR19):
- Bug classification: `IssueType == "Bug"` (exact, case-sensitive)
- Completed filter: `FinalStatus IN doneStatuses AND RemovedAt == null AND FinalStatus NOT IN excludedStatuses` (the current Dashboard leaderboard does NOT apply excluded-from-scope; this step adds that alignment)
- SP uses `GetEffectiveSp(defaultSpPerBug)` — null SP tickets excluded from SP sums
- Ticket counts include ALL done tickets regardless of SP
- Sort remains by total SP descending (API-level) — frontend re-sorts per mode

**Pattern reference:** `BugRatioService.CompletedMemberships` (line 383-393) for the completed+excluded filter pattern.

**Dependencies:** None

---

### Step 2: Create LeaderboardService

**What:** Create a new focused operation service for the Developers page Leaderboard tab. Handles both multi-sprint aggregation and single-sprint with delta computation. Follow `extract-feature-service` skill.

**Files to create:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/LeaderboardService.cs`

**Feature-specific inputs:**

Response records (same file, above service class):
```
LeaderboardSprintInfo(int Id, string Name, DateTime StartDate, DateTime EndDate)

LeaderboardDeveloperSprintBreakdown(int SprintId, decimal FeatureSp, decimal BugSp, decimal TotalSp, int FeatureTickets, int BugTickets)

LeaderboardDeveloperEntry(string AccountId, string DisplayName, string? SubTeam, string? AvatarUrl, decimal FeatureSp, decimal BugSp, decimal TotalSp, int FeatureTickets, int BugTickets, int TotalTickets, List<LeaderboardDeveloperSprintBreakdown> SprintBreakdowns)

LeaderboardMultiSprintResponse(List<LeaderboardSprintInfo> Sprints, List<LeaderboardDeveloperEntry> Developers)

LeaderboardDeveloperDelta(decimal FeatureSpDelta, string FeatureSpDeltaDirection, string FeatureSpDeltaPolarity, decimal BugSpDelta, string BugSpDeltaDirection, string BugSpDeltaPolarity, decimal TotalSpDelta, string TotalSpDeltaDirection, string TotalSpDeltaPolarity, int FeatureTicketsDelta, string FeatureTicketsDeltaDirection, string FeatureTicketsDeltaPolarity, int BugTicketsDelta, string BugTicketsDeltaDirection, string BugTicketsDeltaPolarity)

LeaderboardDeveloperSingleEntry(string AccountId, string DisplayName, string? SubTeam, string? AvatarUrl, decimal FeatureSp, decimal BugSp, decimal TotalSp, int FeatureTickets, int BugTickets, int TotalTickets, LeaderboardDeveloperDelta? Delta)

LeaderboardSingleSprintResponse(LeaderboardSprintInfo Sprint, List<LeaderboardDeveloperSingleEntry> Developers)

LeaderboardResponse(string Mode, LeaderboardMultiSprintResponse? MultiSprint, LeaderboardSingleSprintResponse? SingleSprint)
```

Service methods:
- `ComputeMultiSprint(List<Sprint> targetSprints, List<Developer> activeDevelopers, AppSettings settings, string? subTeam, HashSet<string>? excludedDeveloperIds)` — aggregates across sprints, sorted by total SP descending
- `ComputeSingleSprint(Sprint targetSprint, Sprint? priorSprint, List<Developer> activeDevelopers, AppSettings settings, string? subTeam, HashSet<string>? excludedDeveloperIds)` — includes delta vs prior

Computation rules:
- Same completed filter as Step 1 (done + not removed + not excluded-from-scope)
- Bug classification: `IssueType == "Bug"`
- SP via `GetEffectiveSp(defaultSpPerBug)` with DefaultSpPerBug from settings (BR19)
- Multi-sprint: straight sums across sprints (BR12)
- Developers sorted by total SP descending (BR13)
- Delta polarity: FeatureSp=positive-up, BugSp=positive-down, TotalSp=neutral, FeatureTickets=positive-up, BugTickets=positive-down (BR15)
- Only active developers shown (BR9), zero-value visible (BR8)

**Pattern reference:** `BugRatioService` for multi/single sprint pattern, delta helpers, CompletedMemberships filter.

**Dependencies:** None (independent of Step 1)

---

### Step 3: Create GetLeaderboard endpoint

**What:** Create the new `GET /api/analytics/leaderboard` endpoint with request model, validator, and handler. Follow `create-feature` skill (FastEndpoints variant).

**Files to create:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetLeaderboard/GetLeaderboardEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetLeaderboard/GetLeaderboardQuery.cs`

**Feature-specific inputs:**

Request model (`GetLeaderboardQuery.cs`):
- `SprintId` (int?, optional)
- `Last` (int?, optional)
- `SubTeam` (string?, optional)

Validator rules:
- `SprintId` > 0 when present
- `Last` >= 1 when present
- SprintId and Last cannot both be provided (400)
- SubTeam not empty when provided

Endpoint handler pattern (mirrors `GetBugRatioEndpoint`):
- Load closed sprints, determine mode (single vs multi)
- Single sprint: validate sprintId matches a closed sprint (400 if not), find prior sprint, compute excluded developers, call `LeaderboardService.ComputeSingleSprint`
- Multi sprint: default `last` to 5, take last N, compute excluded (excluded-in-all pattern for multi), call `LeaderboardService.ComputeMultiSprint`
- Empty sprints: return empty multi response

DI: `LeaderboardService` injected via primary constructor (alongside `SprintRepository`, `DeveloperRepository`, `AppSettingsRepository`).

**Pattern reference:** `GetBugRatioEndpoint.cs` — exact same structure.

**Dependencies:** Step 2 (LeaderboardService must exist)

---

### Step 4: Add frontend TypeScript types

**What:** Add TypeScript interfaces for the new leaderboard API response and extend the existing `DeveloperSummary` interface. Follow `create-vue-feature` skill (types step).

**Files to modify:**
- `client/src/types/index.ts`

**Feature-specific inputs:**

Extend existing `DeveloperSummary`:
- Add `featureSp: number`
- Add `bugSp: number`
- Add `featureTickets: number`
- Add `bugTickets: number`

New interfaces (matching backend records):
- `LeaderboardSprintInfo` — id, name, startDate, endDate
- `LeaderboardDeveloperSprintBreakdown` — sprintId, featureSp, bugSp, totalSp, featureTickets, bugTickets
- `LeaderboardDeveloperEntry` — accountId, displayName, subTeam, avatarUrl, featureSp, bugSp, totalSp, featureTickets, bugTickets, totalTickets, sprintBreakdowns
- `LeaderboardMultiSprintResponse` — sprints, developers
- `LeaderboardDeveloperDelta` — all delta fields with direction and polarity
- `LeaderboardDeveloperSingleEntry` — same as multi entry + delta
- `LeaderboardSingleSprintResponse` — sprint, developers
- `LeaderboardResponse` — mode, multiSprint, singleSprint

**Dependencies:** None

---

### Step 5: Add API module function

**What:** Add `getLeaderboard` function to the analytics API module. Follow `create-vue-feature` skill (API step).

**Files to modify:**
- `client/src/api/analytics.ts`

**Feature-specific inputs:**

Function signature: `getLeaderboard(sprintId?: number, last?: number, subTeam?: string): Promise<LeaderboardResponse>`

Pattern: identical to `getBugRatio` — builds URLSearchParams, calls `apiFetch<LeaderboardResponse>('/analytics/leaderboard?...')`.

**Dependencies:** Step 4 (types must exist for import)

---

### Step 6: Extend developersStore with Leaderboard tab

**What:** Add leaderboard state, loading/error refs, fetchLeaderboard action, and extend the tab switching mechanism. Follow `pinia-patterns` skill.

**Files to modify:**
- `client/src/stores/developersStore.ts`

**Feature-specific inputs:**

New state:
- `leaderboard: ref<LeaderboardResponse | null>(null)`
- `leaderboardLoading: ref(false)`
- `leaderboardError: ref<string | null>(null)`

Extend `activeTab` type: `'throughput' | 'bugRatio' | 'leaderboard'`

New action: `fetchLeaderboard()` — mirrors `fetchBugRatio()` pattern (single vs multi mode dispatch)

Extend `switchTab`: add `'leaderboard'` case that calls `fetchLeaderboard()`

Extend `selectSprint`, `selectLastN`, `selectSubTeam`: if `activeTab === 'leaderboard'`, also call `fetchLeaderboard()`

Export new refs and action.

**Dependencies:** Steps 4, 5 (types and API function must exist)

---

### Step 7: Dashboard leaderboard Features/Bugs toggle

**What:** Add a Features/Bugs toggle to the Dashboard leaderboard card. The toggle controls frontend-only sorting/display. The API already returns the full breakdown (after Step 1). Follow `vue-component-architecture` skill for component structure.

**Files to modify:**
- `client/src/views/DashboardView.vue` — add toggle state (defaults to 'features', resets on mount), replace the leaderboard display section (lines 154-189)

**Feature-specific inputs:**

Local ref: `leaderboardMode: ref<'features' | 'bugs'>('features')` — resets to 'features' on each `onMounted` (BR6)

Computed `sortedLeaderboard`: re-sorts `store.summary.leaderboard` based on active mode:
- Features mode: sort by `featureSp` descending, then displayName ascending
- Bugs mode: sort by `bugSp` descending, then displayName ascending

Display per row: rank, avatar, displayName, sub-team badge, SP value (featureSp or bugSp), tickets value (featureTickets or bugTickets)

Toggle UI: two-button toggle group above the leaderboard list ("Features" / "Bugs")

Tooltip wiring (from help.tooltips.md):
- Toggle info icon: "Switch between feature work and bug fix rankings. Features shows planned delivery; Bugs shows rework."
- Leaderboard title (features mode): "Active developers ranked by feature story points completed this sprint. Bug fixes excluded."
- Leaderboard title (bugs mode): "Active developers ranked by bug fix story points completed this sprint. Feature work excluded."
- SP column: "Story points completed for the selected work type. Excludes unestimated tickets."
- Tickets column: "Number of done tickets for the selected work type. Includes tickets with no story points."

**Dependencies:** Step 1 (backend returns breakdown fields), Step 4 (extended DeveloperSummary type)

---

### Step 8: Create LeaderboardTab component

**What:** Create the Leaderboard tab container (L1) for the Developers page. Contains a stacked horizontal bar chart and a metrics table. Follow `vue-component-architecture` skill.

**Files to create:**
- `client/src/components/developers/LeaderboardTab.vue`

**Feature-specific inputs:**

Props: `data: LeaderboardResponse`

Component structure (L1 container, <200 lines):
- Determines mode from `data.mode`
- Multi-sprint: shows `LeaderboardChart` + `LeaderboardTable` (multi variant)
- Single-sprint: shows `LeaderboardChart` + `LeaderboardTable` (single variant with deltas)
- Sorts developers by totalSp descending before passing to children (BR13)

Tooltip wiring:
- Tab area: "Feature vs bug work composition per developer. Stacked bars show where sprint capacity went."

**Dependencies:** Step 9 (child components)

---

### Step 9: Create LeaderboardChart and LeaderboardTable components

**What:** Create L3 props-only display components for the Leaderboard tab. Follow `vue-patterns` skill.

**Files to create:**
- `client/src/components/developers/LeaderboardChart.vue`
- `client/src/components/developers/LeaderboardTable.vue`

**Feature-specific inputs:**

`LeaderboardChart.vue` (L3, props-only):
- Props: `developers: Array<{displayName, featureSp, bugSp, totalSp}>`, sorted by totalSp desc
- Renders stacked horizontal bars using inline CSS (no charting library needed — simple percentage-width divs)
- Blue segment: featureSp proportion, Red segment: bugSp proportion
- Each bar labeled with developer name on left, totalSp on right
- Tooltip: "Blue segments are feature SP; red segments are bug SP. Sorted by total SP descending."

`LeaderboardTable.vue` (L3, props-only):
- Props: `developers: multi or single entries`, `mode: 'multi' | 'single'`
- Multi-sprint columns: Name, Sub-Team, Feature SP, Bug SP, Total SP, Feature Tickets, Bug Tickets, Total Tickets
- Single-sprint columns: same + delta indicators per metric
- Delta display: icon (up/down/flat) + value, colored by polarity (feature SP positive-up, bug SP positive-down, total SP neutral, feature tickets positive-up, bug tickets positive-down)
- Uses same `deltaIcon` and `deltaClass` helpers as DevelopersView (extract or duplicate)

Tooltip wiring (from help.tooltips.md):
- Feature SP column: "Story points completed on non-bug tickets across the selected sprints."
- Bug SP column: "Story points completed on bug tickets across the selected sprints."
- Total SP column: "Combined feature and bug story points completed across the selected sprints."
- Feature Tickets column: "Number of completed non-bug tickets. Includes tickets with no story point estimate."
- Bug Tickets column: "Number of completed bug tickets. Includes tickets with no story point estimate."
- Total Tickets column: "Total completed tickets (features + bugs) across the selected sprints. Includes unestimated tickets."
- Delta indicators: "Change versus the prior sprint. Feature SP: higher is green. Bug SP: lower is green. Total SP: neutral."

**Dependencies:** None (props-only, can be developed in parallel with Step 8)

---

### Step 10: Wire Leaderboard tab into DevelopersView

**What:** Add the third tab "Leaderboard" to the DevelopersView tab bar, update URL sync to handle it, and render the LeaderboardTab component.

**Files to modify:**
- `client/src/views/DevelopersView.vue`

**Feature-specific inputs:**

Tab bar: Add third button "Leaderboard" after "Bug Ratio" (tab order: Throughput, Bug Ratio, Leaderboard — BR11)

Tab switching: extend `onTabSwitch` to accept `'leaderboard'`

URL sync: `tab=leaderboard` in query params

On mount: if `tabParam === 'leaderboard'`, set `store.activeTab = 'leaderboard'` and fetch on init

Render: `<LeaderboardTab v-else-if="store.activeTab === 'leaderboard'" :data="store.leaderboard" />` with loading/error states matching bugRatio pattern

Sprint selector and sub-team filter persist across all three tabs (already handled by store extension in Step 6).

Default sprint selection for Leaderboard tab: "Last 5" (multi-sprint mode, `last=5`) — when user switches to Leaderboard tab while in single-sprint mode, the sprint selector does NOT change; the endpoint handles either mode.

**Dependencies:** Steps 6, 8 (store and component must exist)

---

### Step 11: Update Knowledge Base

**What:** Add a KB entry for the leaderboard analytics and update the frontend map.

**Files to create:**
- `docs/kb/analytics/leaderboard.md`

**Files to modify:**
- `docs/kb/index.md` — add entry under Analytics
- `docs/kb/frontend-map.md` — update DevelopersView row to mention third tab and leaderboard endpoint

**Dependencies:** All previous steps complete

## Cross-Service Changes

None. Single-service feature, no integration events or gRPC contracts.

## Migration Notes

No EF Core migrations required. No schema changes.

## Testing Strategy

**Backend:**
- Verify sprint summary response includes featureSp, bugSp, featureTickets, bugTickets per developer
- Verify excluded-from-scope statuses now apply to Dashboard leaderboard (alignment change)
- GET /api/analytics/leaderboard with no params returns last 5 sprints aggregated
- GET /api/analytics/leaderboard?sprintId={id} returns single sprint with deltas
- GET /api/analytics/leaderboard?last=3 returns 3 sprints aggregated
- 400 when both sprintId and last provided
- 400 when sprintId not a closed sprint
- 400 when last < 1
- Sub-team filter scopes to developers in that sub-team
- Zero-SP developers visible with zero values
- Only active developers in results
- Cross-cutting exclusion (0% capacity + 0 tickets) applies
- DefaultSpPerBug applies to unestimated bugs

**Frontend:**
- Dashboard leaderboard toggle defaults to Features
- Features mode shows featureSp sorted descending
- Bugs mode shows bugSp sorted descending
- Toggle resets to Features on navigation away and back
- Developers page shows three tabs: Throughput, Bug Ratio, Leaderboard
- Leaderboard tab defaults to Last 5 sprint range
- Stacked bar chart shows blue (feature) and red (bug) segments
- Chart sorted by total SP descending
- Table shows all columns with correct values
- Single-sprint view shows delta indicators with correct polarity colors
- Sub-team filter and sprint selector persist across tabs
- Tooltips match help.tooltips.md content

## Open Questions

None.
