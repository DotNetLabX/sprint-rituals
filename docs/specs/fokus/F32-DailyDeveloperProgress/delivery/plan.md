# Daily Developer Progress

**Feature Spec:** `docs/specs/F32-DailyDeveloperProgress/definition/spec.md`

## Context

Fokus shows per-developer analytics aggregated at sprint level -- how many SP a developer completed over an entire sprint. During an active sprint, Scrum Masters need to know where each developer is right now: who is on track, who is falling behind, and which tickets are stalled. The Daily Progress tab gives Scrum Masters a live signal for mid-sprint course correction.

This feature adds: (1) a backend computation service and endpoint for active-sprint daily progress, (2) a new "Daily Progress" tab on the Developers page with card grid, mini burnup charts, alert banner, and stall detection, (3) the first SignalR infrastructure in the codebase for auto-refresh on sync.

**Services impacted:** Fokus (single service). Backend: new analytics service + endpoint + SignalR hub. Frontend: new store, new tab components, SignalR client subscription.

## Scope

**In scope:**
- Backend: `DeveloperProgressService` computation service
- Backend: `GET /api/analytics/developer-progress?subTeam` endpoint
- Backend: SignalR hub (`SprintHub`) with sync-complete broadcast
- Backend: Inject hub context into sync endpoints to broadcast after sync
- Frontend: `dailyProgressStore` (dedicated store, separate from developersStore)
- Frontend: `DailyProgressTab` with alert banner, developer card grid, mini burnup charts
- Frontend: Day drill-down tooltip, stall badge popover
- Frontend: Sprint selector disable behavior on this tab
- Frontend: SignalR client subscription for auto-refresh
- Frontend: `@microsoft/signalr` npm dependency
- Architecture doc amendment: remove SignalR from out-of-scope
- Help tooltips from `docs/specs/F32-DailyDeveloperProgress/definition/help.tooltips.md`

**Out of scope:**
- Closed sprint daily progress (spec: future enhancement)
- Automated notifications (Slack, email, push)
- Git activity signal
- WIP limits, burnout detection
- Configurable stall threshold or grace period
- Multi-sprint trend view

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | (none) | -- | DeveloperProgressService: pace computation, stall detection, daily breakdown, alert generation | Log: no skill for metric computation patterns |
| 2 | create-feature | Follow | GetDeveloperProgress endpoint (GET, FastEndpoints, active-sprint-only query) | |
| 3 | (none) | -- | SignalR hub creation, DI registration, hub context injection into sync endpoints | Log: no skill for SignalR infrastructure |
| 4 | pinia-patterns | Follow | dailyProgressStore: dedicated store with SignalR lifecycle, sub-team filter via cross-store ref | |
| 5 | vue-patterns | Follow | DailyProgressTab: card grid, alert banner, empty states | |
| 6 | vue-patterns | Follow | DeveloperProgressCard: mini burnup chart (ApexCharts sparkline), stall badge, day tooltip | |
| 7 | vue-patterns | Follow | DevelopersView integration: tab wiring, sprint selector disable, URL sync | |
| 8 | (none) | -- | Architecture doc amendment: remove SignalR from out-of-scope, add SignalR section | |

## Domain Model Changes

None. This feature reads from existing entities: Sprint, SprintMembership, Ticket, StatusTransition, Developer, DeveloperSprintCapacity, AppSettings. No new entities, value objects, or domain events.

## Data Model Changes

None. No new tables, columns, or migrations.

## Implementation Steps

### Step 1: Create DeveloperProgressService

Create the backend computation service that computes per-developer daily progress for the active sprint, including pace calculation, behind-pace alerting, stall detection, and daily breakdown.

**No matching skill** -- metric computation patterns are a gap.

**Files:**
- Create: `src/Services/Fokus/Fokus.API/Features/Analytics/DeveloperProgressService.cs`
- Modify: `src/Services/Fokus/Fokus.API/DependencyInjection.cs` -- register `DeveloperProgressService` as scoped

**Records to define** (in the same file, above the service class -- following QaTrendsService/DeveloperThroughputService pattern):

- `DeveloperProgressSprintInfo(int Id, string Name, DateTime StartDate, DateTime EndDate)`
- `DeveloperProgressAlert(string AccountId, string DisplayName, string? AvatarUrl, decimal GapSp, decimal GapDays)`
- `CompletedTicketEntry(string Key, string Summary, decimal? StoryPoints, string IssueType)`
- `DayBreakdownEntry(int Day, DateTime Date, List<CompletedTicketEntry> CompletedTickets, decimal CumulativeSp, decimal ExpectedCumulativeSp)`
- `StalledTicketEntry(string Key, string Summary, string CurrentStatus, string IssueType, decimal? StoryPoints, int DaysSinceLastTransition)`
- `DeveloperProgressEntry(string AccountId, string DisplayName, string? SubTeam, string? AvatarUrl, decimal AssignedSp, decimal CompletedSp, decimal CompletionPercent, int CapacityPercent, decimal DailyPace, bool IsBehindPace, decimal? PaceGapSp, List<StalledTicketEntry> StalledTickets, List<DayBreakdownEntry> DailyBreakdown)`
- `DeveloperProgressResponse(bool HasActiveSprint, DeveloperProgressSprintInfo? Sprint, int CurrentDay, int TotalDays, bool IsGracePeriod, List<DeveloperProgressAlert> Alerts, List<DeveloperProgressEntry> Developers)`

**Service shape:**

Class `DeveloperProgressService` -- pure computation, receives pre-loaded data (same pattern as `DeveloperThroughputService`).

Public method signature:
```
DeveloperProgressResponse ComputeProgress(
    Sprint activeSprint,
    List<Developer> activeDevelopers,
    List<DeveloperSprintCapacity> capacityRecords,
    AppSettings settings,
    string? subTeam,
    List<Developer> allDevelopers,
    List<StatusTransition> statusTransitions,
    HashSet<string> excludedDeveloperIds)
```

**Key computation rules (all from spec business rules):**

Ticket scope (BR1-7):
- All ticket types included (features, bugs, tasks) -- NOT feature-only
- Assigned scope = all non-removed memberships for the developer (`RemovedAt == null`), regardless of start transition -- different from Throughput which requires transition to start stage
- Completed = transitioned to CycleTimeEndStage or beyond during `[sprint.StartDate, sprint.EndDate]` -- use `TransitionAttributionChecker.IsCompletedInSprint`
- SP computation: `membership.GetEffectiveSp(settings.DefaultSpPerBug)` -- null SP excluded from sums
- Developer exclusion: apply `ExcludedDeveloperFilter` + inactive filter + sub-team filter
- ExcludedFromScopeStatuses NOT applied (BR7, same as Throughput)

Day and pace (BR8-10, BR26-27):
- `totalDays = max(1, (sprint.EndDate.Date - sprint.StartDate.Date).Days + 1)` -- +1 for inclusive, min 1 for degenerate sprint
- `currentDay = min(totalDays, max(1, (DateTime.UtcNow.Date - sprint.StartDate.Date).Days + 1))` -- clamped to [1, totalDays]
- `dailyPace = assignedSp * (capacityPercent / 100m) / totalDays`
- `expectedCumulativeSp(day) = dailyPace * day`
- Zero assigned SP: dailyPace = 0, completionPercent = 0%, isBehindPace = false

Daily breakdown (BR3):
- For each day 1..currentDay: date = sprint.StartDate.Date.AddDays(day - 1)
- A ticket's completion day = `(completedAt.Date - sprint.StartDate.Date).Days + 1` where completedAt comes from `IsCompletedInSprint` timestamp
- Cumulative SP = running sum through that day
- Tickets with null effective SP appear in completedTickets list but don't add to cumulative SP

Behind-pace alerting (BR11-14):
- `isGracePeriod = currentDay <= 2`
- During grace period: isBehindPace = false, paceGapSp = null for all developers, alerts list empty
- `paceGapSp = expectedCumulativeSp(currentDay) - cumulativeSp(currentDay)` (positive = behind)
- `isBehindPace = paceGapSp > dailyPace` (behind by more than one day's worth)
- `gapDays = paceGapSp / dailyPace` rounded to 1 decimal (only computed when isBehindPace = true, so dailyPace > 0 is guaranteed)

Stall detection (BR15-17):
- A ticket is stalled when: (a) non-removed membership, (b) started (transitioned to CycleTimeStartStage or beyond via `IsStartedInSprint`), (c) NOT completed, (d) most recent StatusTransition timestamp for that ticket is more than 2 business days ago
- Business days = Monday through Friday. Count business days from most recent transition date to today, excluding the transition day itself and weekends
- Build a `CountBusinessDays(DateTime from, DateTime to)` utility method (private, within the service)

Capacity resolution: sprint-specific `DeveloperSprintCapacity` record first, fallback to `Developer.DefaultCapacityPercent`. Same pattern as `DeveloperThroughputService`.

**Accept:**
- Returns `HasActiveSprint = false` with empty developers/alerts when no active sprint is passed
- All ticket types included in SP sums (not feature-only)
- Assigned scope is all non-removed tickets (not transition-gated)
- dailyPace uses calendar days (not business days)
- Stall detection uses business days (Mon-Fri)
- Grace period suppresses all behind-pace signals
- Division by zero handled per BR26-27
- Completion percent can exceed 100% per BR25

**Pattern reference:** `src/Services/Fokus/Fokus.API/Features/Analytics/DeveloperThroughputService.cs` (record hierarchy + service structure), `ExcludedDeveloperFilter.cs` (capacity resolution pattern)

**Dependencies:** None

---

### Step 2: Create GetDeveloperProgress endpoint

Create the FastEndpoints endpoint that serves daily progress data for the active sprint.

**Follow create-feature.** FastEndpoints variant.

**Feature-specific inputs:**
- Route: `GET /api/analytics/developer-progress`
- Tags: `Analytics`
- Request: `GetDeveloperProgressRequest` with optional `SubTeam` (string?)
- Response: `DeveloperProgressResponse` (from Step 1)
- No validator needed (only optional query param)
- Folder: `src/Services/Fokus/Fokus.API/Features/Analytics/GetDeveloperProgress/`

**Files:**
- Create: `src/Services/Fokus/Fokus.API/Features/Analytics/GetDeveloperProgress/GetDeveloperProgressEndpoint.cs`

**Endpoint handler logic (what to load, not how to structure):**

1. Load analytics sprints via `sprintRepository.GetAnalyticsSprintsAsync(ct)`
2. Find the active sprint: `allSprints.FirstOrDefault(s => s.State == SprintState.Active)`
3. If no active sprint: return 200 with `DeveloperProgressResponse(false, null, 0, 0, false, [], [])`
4. Load the active sprint with memberships: `sprintRepository.GetSprintWithMembershipsAsync(activeSprint.Id, ct)`
5. Load active developers, all developers, capacity records for the active sprint, app settings, status transitions for the active sprint -- same bulk loading pattern as `GetDeveloperThroughputEndpoint`
6. Compute excluded developer IDs via `ExcludedDeveloperFilter`
7. Normalize sub-team (null if empty/whitespace)
8. Call `DeveloperProgressService.ComputeProgress(...)` and return result

**Accept:**
- 200 with progress data when active sprint exists
- 200 with `hasActiveSprint: false` when no active sprint
- Sub-team filter works
- No 400/404 -- this endpoint always returns 200

**Pattern reference:** `src/Services/Fokus/Fokus.API/Features/Analytics/GetDeveloperThroughput/GetDeveloperThroughputEndpoint.cs` (data loading + service delegation pattern)

**Dependencies:** Step 1

---

### Step 3: Create SignalR hub and wire sync broadcast

Build the initial SignalR infrastructure: a hub that the frontend subscribes to, and hub context injection into sync endpoints to broadcast after sync completes.

**No matching skill** -- SignalR infrastructure is a gap.

**Files:**
- Create: `src/Services/Fokus/Fokus.API/Hubs/SprintHub.cs`
- Modify: `src/Services/Fokus/Fokus.API/DependencyInjection.cs` -- add `services.AddSignalR()`
- Modify: `src/Services/Fokus/Fokus.API/Program.cs` -- add `app.MapHub<SprintHub>("/hubs/sprint")` after `UseFokusMiddleware()` and before `MapFallbackToFile`
- Modify: `src/Services/Fokus/Fokus.API/Features/Sync/SyncSprints/SyncSprintsEndpoint.cs` -- inject `IHubContext<SprintHub>`, call broadcast after successful sync

**Not modified:** `SyncBacklogSprintsEndpoint` -- backlog sync only touches `SprintState.Future` sprints, which can never be the active sprint. Broadcasting from it would be semantically misleading and a no-op for Daily Progress clients.

**SprintHub:**
- Hub class decorated with `[Authorize]` (authenticated users only -- consistent with all other endpoints requiring auth).
- No client-to-server methods needed in v1. The hub exists only so clients can subscribe and receive server-pushed notifications.
- Define a static class `SprintHubMethods` with `public const string SprintSynced = "SprintSynced"` for the method name contract.

**Sync endpoint broadcast:**
- After successful sync response is built (after `syncService.SyncSprintsFromJiraAsync` returns), call `hubContext.Clients.All.SendAsync(SprintHubMethods.SprintSynced, new { SprintIds = sprintsInRange.Select(s => s.Id).ToArray() }, ct)`
- The broadcast payload is lightweight -- just the sprint IDs that were synced. The frontend decides whether to refresh based on whether the active sprint ID is in the list.
- Broadcast happens AFTER the sync is persisted, BEFORE the HTTP response is sent. If the broadcast fails, the sync is still successful -- wrap in try/catch with a log warning.

**Accept:**
- `AddSignalR()` registered in DI
- `SprintHub` mapped at `/hubs/sprint`
- `SprintHub` decorated with `[Authorize]` -- unauthenticated clients cannot connect
- `SyncSprintsEndpoint` broadcasts `SprintSynced` event with sprint IDs after successful sync
- Hub is empty (no client-to-server methods)
- SignalR broadcast failure does not break the sync response

**Pattern reference:** ASP.NET Core SignalR documentation. No existing codebase pattern -- this is the first hub.

**Dependencies:** None (can be done in parallel with Steps 1-2)

---

### Step 4: Create dailyProgressStore and API module

Create the dedicated Pinia store and API function for the Daily Progress tab.

**Follow pinia-patterns.**

**Feature-specific inputs:**

**API function** -- add to `client/src/api/analytics.ts`:
- `getDeveloperProgress(subTeam?: string): Promise<DeveloperProgressResponse>`
- Route: `/analytics/developer-progress?subTeam`

**TypeScript types** -- add to `client/src/types/index.ts`:
- `DeveloperProgressResponse` with all nested types matching the backend response shape (Step 1 records)
- `DeveloperProgressSprintInfo`, `DeveloperProgressAlert`, `CompletedTicketEntry`, `DayBreakdownEntry`, `StalledTicketEntry`, `DeveloperProgressEntry`

**Store** -- create `client/src/stores/dailyProgressStore.ts`:
- State: `data` (DeveloperProgressResponse | null), `loading`, `error`, `signalRConnected`
- Actions: `fetchProgress(subTeam?)`, `startSignalR()`, `stopSignalR()`
- The store reads `selectedSubTeam` from `useDevelopersStore()` via cross-store reference when fetching -- it does not duplicate the sub-team state
- SignalR lifecycle: `startSignalR()` creates a `HubConnection` to `/hubs/sprint`, subscribes to `SprintSynced`, and on receive calls `fetchProgress()` to refresh data. `stopSignalR()` disposes the connection.
- Install `@microsoft/signalr` npm package: `npm install @microsoft/signalr`

**Accept:**
- `getDeveloperProgress` API function works with optional subTeam
- TypeScript types match backend response shape exactly
- Store fetches data, manages loading/error state
- SignalR connection is established when `startSignalR()` is called
- On `SprintSynced` event, store re-fetches progress data automatically
- `stopSignalR()` cleanly disposes the connection

**Pattern reference:** `client/src/stores/developersStore.ts` (store structure), `client/src/api/analytics.ts` (API function pattern)

**Dependencies:** Steps 1, 2, 3

---

### Step 5: Create DailyProgressTab component

Create the main tab component with the alert banner, developer card grid, and empty states.

**Follow vue-patterns.**

**Files:**
- Create: `client/src/components/developers/DailyProgressTab.vue`

**Feature-specific inputs:**

**Props:** `data: DeveloperProgressResponse`

**Structure:**
- Alert banner section (top): rendered when `!data.isGracePeriod && data.alerts.length > 0`. Lists behind-pace developers with name, SP gap (`gapSp`), and days behind (`gapDays`). Style: warning-colored bar with developer entries. Tooltip text from `help.tooltips.md` "Alert Banner" entry.
- Grace period info: when `data.isGracePeriod`, optionally show a subtle info note. Tooltip from `help.tooltips.md` "Grace Period" entry.
- Card grid: `grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3` layout. One `DeveloperProgressCard` per developer entry.
- Empty states per spec Flow 6:
  - `!data.hasActiveSprint`: "No active sprint. Sync a sprint to get started."
  - `data.hasActiveSprint && data.developers.length === 0`: "No developers with assigned tickets in this sprint."

**Accept:**
- Alert banner shows behind-pace developers with SP gap and days behind
- Alert banner hidden during grace period
- Card grid renders one card per developer
- Empty states match spec Flow 6
- Help tooltips wired on matching elements

**Pattern reference:** `client/src/components/developers/LeaderboardTab.vue` (tab container pattern), `client/src/components/EmptyState.vue` (empty state pattern)

**Dependencies:** Step 4 (types), Step 6 (DeveloperProgressCard component)

---

### Step 6: Create DeveloperProgressCard component

Create the card component with mini burnup chart, SP progress, stall badge, and day drill-down tooltip.

**Follow vue-patterns.**

**Files:**
- Create: `client/src/components/developers/DeveloperProgressCard.vue`

**Feature-specific inputs:**

**Props:** `developer: DeveloperProgressEntry`, `isGracePeriod: boolean`

**Card layout:**
- Header: developer avatar + display name + sub-team badge
- SP line: "X / Y SP (Z%)" with "(all types)" label per BR1. Tooltip from `help.tooltips.md` "SP Completed / Assigned" entry.
- Mini burnup chart: ApexCharts area/line chart in sparkline-like dimensions (~200x80px).
  - Actual line (solid, colored): `dailyBreakdown.map(d => d.cumulativeSp)` -- cumulative SP per day
  - Expected pace line (dashed, gray): `dailyBreakdown.map(d => d.expectedCumulativeSp)` -- straight diagonal
  - X-axis: day numbers (1 to currentDay)
  - Y-axis: fixed scale 0 to `assignedSp * capacityPercent / 100` per BR24 (the pace line target)
  - Day drill-down: on click/hover of a data point, show tooltip with completed tickets for that day (key, summary, SP, issue type icon). "No completions" if empty. Tooltip text from `help.tooltips.md` "Day Drill-Down" entry.
  - Tooltip from `help.tooltips.md` "Burnup Chart" and "Expected Pace Line" entries.
- Behind-pace indicator: visual warning (border color change or icon) when `developer.isBehindPace && !isGracePeriod`. Tooltip from `help.tooltips.md` "Behind-Pace Indicator" entry.
- Stall badge: shown when `developer.stalledTickets.length > 0`. Displays count. On click, expand/popover showing stalled tickets (key, summary, current status, days since last transition). Tooltip from `help.tooltips.md` "Stall Badge" and "Stalled Ticket -- Days Since Last Transition" entries.

**Accept:**
- Card displays avatar, name, sub-team, SP progress with "(all types)" label
- Mini burnup chart has actual line (solid) and pace line (dashed)
- Y-axis scale fixed to assignedSp * capacity%
- Day drill-down shows completed tickets on hover/click
- Behind-pace visual indicator shown (hidden during grace period)
- Stall badge with expandable stalled ticket list
- All help tooltips wired

**Pattern reference:** `client/src/components/developers/LeaderboardChart.vue` (card-embedded chart pattern), `client/src/components/BaseCard.vue` (card structure)

**Dependencies:** Step 4 (types)

---

### Step 7: Wire Daily Progress tab into DevelopersView

Integrate the tab into the Developers page with sprint selector disable behavior, URL sync, SignalR lifecycle, and store coordination for sub-team/sprint changes.

**Follow vue-patterns.**

**Files:**
- Modify: `client/src/stores/developersStore.ts` -- update `activeTab` type, `switchTab` signature, `selectSubTeam`, `selectSprint`, `selectLastN`
- Modify: `client/src/views/DevelopersView.vue` -- add Daily Progress tab, wire store, manage SignalR lifecycle, loading/error states, update `onTabSwitch` signature
- Modify: `client/src/components/PageToolbar.vue` -- add `disabled` prop for sprint selector

**developersStore changes (follow existing tab coordination pattern):**

1. Update the `activeTab` type union from `'throughput' | 'bugRatio' | 'leaderboard' | 'quality' | 'qaWorkload'` to include `'dailyProgress'`. This affects the `ref` declaration (line 19) and the `switchTab` parameter type (line 108).

2. Update `switchTab` function: add a `'dailyProgress'` branch. Import `useDailyProgressStore` and in the dailyProgress branch call `const dailyProgressStore = useDailyProgressStore(); await dailyProgressStore.fetchProgress(selectedSubTeam.value)` and `dailyProgressStore.startSignalR()`. Also add cleanup: when switching FROM dailyProgress (detect by checking the previous tab value before setting `activeTab.value = tab`), call `dailyProgressStore.stopSignalR()`.

3. Update `selectSubTeam` function (lines 91-106): add `if (activeTab.value === 'dailyProgress') { const dailyProgressStore = useDailyProgressStore(); await dailyProgressStore.fetchProgress(subTeam) }` following the existing pattern where each tab has its own re-fetch branch.

4. Update `selectSprint` and `selectLastN` functions: these should be no-ops when `activeTab.value === 'dailyProgress'`. The Daily Progress tab has no sprint selection -- add an early return guard: `if (activeTab.value === 'dailyProgress') return` at the top of both functions. This prevents the sprint selector from mutating state even if the disable mechanism is bypassed.

**DevelopersView changes:**

1. Import `DailyProgressTab` and `useDailyProgressStore`.
2. Update `onTabSwitch` function signature (line 106) to include `'dailyProgress'` in the union type.
3. Add URL sync: in the `watch` callback and `onMounted` tab parsing, handle `tab=daily-progress` mapping to `'dailyProgress'`.
4. Add the "Daily Progress" tab button as the LAST tab (after QA Workload if xray enabled, after Leaderboard if not) per BR23. Always visible regardless of Xray status.
5. Add tab content template with **loading and error states** following the existing pattern (e.g., bugRatio tab at lines 225-229):
   ```
   <template v-else-if="store.activeTab === 'dailyProgress'">
     <div v-if="dailyProgressStore.loading" class="text-xs text-text-muted">Loading...</div>
     <div v-else-if="dailyProgressStore.error" class="text-sm text-status-danger">{{ dailyProgressStore.error }}</div>
     <DailyProgressTab v-else-if="dailyProgressStore.data" :data="dailyProgressStore.data" />
   </template>
   ```
6. On `onMounted`: if `tabParam === 'daily-progress'`, set `store.activeTab = 'dailyProgress'` and after `initialize()` completes, fetch progress data and start SignalR.
7. On `onUnmounted`: if dailyProgress tab is active, call `dailyProgressStore.stopSignalR()`.

**Sprint selector disable (BR22):**
- Add a `disabled` boolean prop to `PageToolbar.vue` (default false)
- When `disabled` is true: add `opacity-50 pointer-events-none` classes to the sprint `BaseSelect`, preventing interaction
- In `DevelopersView`: bind `:disabled="store.activeTab === 'dailyProgress'"` on the PageToolbar
- The sprint selector does NOT mutate shared state when disabled -- the previously selected sprint is preserved and restored when switching to another tab. Since the store already preserves `selectedSprintId` / `sprintMode` / `selectedLast` across tab switches (tab switching only changes `activeTab`), no additional preservation logic is needed.
- The `selectSprint`/`selectLastN` early-return guards (above) provide a safety net even if the UI disable is bypassed.

**Accept:**
- "Daily Progress" tab appears last on the Developers page
- Tab always visible regardless of Xray status
- URL updates to `?tab=daily-progress`
- Sprint selector disabled (grayed out) on Daily Progress tab
- `selectSprint` and `selectLastN` are no-ops when dailyProgress tab is active
- Switching away restores sprint selector and previously selected sprint
- Data fetches on tab activation with current sub-team filter
- Sub-team filter changes re-fetch progress data when tab is active (via `selectSubTeam` branch in developersStore)
- `switchTab` signature includes `'dailyProgress'`, with SignalR start on enter and stop on leave
- Loading and error states render in the DevelopersView template following existing tab pattern
- SignalR connects on tab activation, disconnects on tab deactivation/unmount
- Auto-refresh works: sync in another browser triggers data refresh

**Pattern reference:** `client/src/views/DevelopersView.vue` (existing tab wiring pattern), `client/src/stores/developersStore.ts` lines 91-122 (sub-team/sprint change handlers with per-tab re-fetch branches)

**Dependencies:** Steps 4, 5, 6

---

### Step 8: Amend architecture doc

Remove SignalR from the out-of-scope list and add a SignalR section under Cross-Cutting Concerns.

**No matching skill** -- documentation amendment.

**Files:**
- Modify: `docs/architecture/v1.md`

**Changes:**
1. In `## What's Explicitly Out of Scope (v2)`: remove the line `- SignalR / real-time updates`
2. Add a new subsection under `## Cross-Cutting Concerns`:

```
### Real-Time Updates (SignalR)

**Approach:** SignalR hub for server-to-browser push notifications. Single hub (`SprintHub`) broadcasts after sync operations complete. Clients subscribe to event channels and refresh data on notification -- no streaming or per-user channels in v1.
**Skill reference:** None (see Gaps)
**Not specified here:** N/A -- no skill covers SignalR patterns.
```

3. Add to the Gaps table:

```
| SignalR patterns | Hub creation, client subscription, broadcast patterns, group management | `signalr-patterns` |
```

4. Update the Skill Inventory table if needed (no new skills to add, but the gap is logged).

**Accept:**
- SignalR no longer listed as out of scope
- Cross-Cutting Concerns section includes SignalR
- Gap logged for future skill creation

**Dependencies:** Step 3

---

## Cross-Service Changes

None. Fokus is a single-service system.

## Migration Notes

None. No schema changes.

## Testing Strategy

### Backend -- DeveloperProgressService
- Active sprint with developers at various completion levels: verify pace, behind-pace, and stall calculations
- Grace period (day 1 and 2): verify no behind-pace alerts, paceGapSp = null
- Zero assigned SP: verify dailyPace = 0, completionPercent = 0%, isBehindPace = false
- Stall detection: ticket with last transition 3+ business days ago (Mon-Fri counting), verify stalled. Ticket with transition 1 business day ago, verify not stalled. Transition on Friday, check on Monday = 0 business days (not stalled).
- Completion percent exceeding 100%: developer completes carry-over ticket, verify completionPercent > 100
- All ticket types: verify bugs and tasks included in SP sums (unlike Throughput)
- Sub-team filter: verify only matching developers returned, alerts recalculated

### Backend -- Endpoint
- 200 with progress data when active sprint exists
- 200 with `hasActiveSprint: false` when no active sprint
- Sub-team filter works

### Backend -- SignalR
- Hub maps successfully at `/hubs/sprint`
- Sync endpoint broadcasts SprintSynced event with sprint IDs
- Broadcast failure does not break sync response

### Frontend
- Daily Progress tab appears last, always visible
- Sprint selector disabled on this tab, restored on tab switch
- Alert banner visible after grace period when developers are behind
- Alert banner hidden during grace period
- Mini burnup chart shows actual vs expected pace lines
- Day drill-down shows completed tickets
- Stall badge shows stalled tickets with business day count
- Sub-team filter re-fetches and recalculates
- SignalR auto-refresh: trigger sync, verify tab data updates without manual refresh
- Empty states: no active sprint message, no assigned developers message

## KB Impact

- Create: `docs/kb/analytics/daily-progress.md` -- pace computation, stall detection business days, assigned scope (all types, not feature-only), behind-pace threshold, grace period
- Update: `docs/kb/frontend-map.md` -- add DailyProgressTab, dailyProgressStore, SignalR subscription to the view/store/API mapping table
- Update: `docs/kb/cross-cutting.md` -- note that Daily Progress uses all-ticket-types scope (not feature-only), and assigned scope is all non-removed (not transition-gated)

## Open Questions

None -- all resolved during analysis.
