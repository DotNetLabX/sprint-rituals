# Sprint Summary Card

**Feature Spec:** `docs/features/SprintSummaryCard/spec.md`

## Context

The Dashboard is the app's landing page but currently shows only an empty state. This feature fills it with a single-sprint summary view: health score, four metric cards with deltas and sparklines, top epics, developer leaderboard, and flags. It is the first analytics feature (F8) and establishes three cross-cutting patterns that all subsequent analytics features (F9-F14) inherit: the composite health score computation, the delta pattern (C1 -- comparing to the prior sprint), and the sub-team filter data-wiring (C2 -- filtering all content by developer sub-team). It also activates the sprint selector and sub-team filter that F7 left as visual stubs.

**Service impacted:** Fokus (single service). Backend: three new read-only endpoints + a computation service for health score and metric calculations. Frontend: full Dashboard view replacement, new Pinia store, new API module, reusable components.

## Scope

**In scope:**
- `GET /api/analytics/sprint-summary` endpoint returning the full sprint summary (health score, metrics with deltas and sparklines, top epics, leaderboard, flags)
- `GET /api/sprints/closed` endpoint returning closed sprints for the sprint selector
- `GET /api/developers/sub-teams` endpoint returning distinct sub-team names
- Health score computation: weighted composite of three sub-metric scores using configurable thresholds and weights
- Delta computation: comparison to the prior closed sprint (by start date)
- Sparkline computation: 4-sprint trailing window ending at the selected sprint
- Sub-team filtering: all computations scoped to developers in the selected sub-team
- Frontend: sprint selector dropdown (wired, replacing the visual stub)
- Frontend: sub-team filter dropdown (wired, replacing the visual stub)
- Frontend: health score badge, four metric cards, top epics, leaderboard, flags sections
- Frontend: empty state when no closed sprints exist
- Frontend: URL reflects selected sprint

**Out of scope (per spec):**
- Animated page transitions between sprints
- Configurable composite RAG thresholds (fixed at 75/40)
- Sprint comparison mode
- Export (PDF/PNG)
- Real-time updates after sync (no SignalR push)
- Configurable disruption grace period (fixed at 2 days)
- Ticket drill-down from flags
- Aggregate sprint view on Dashboard (single-sprint only)
- Day-level disruption timeline (F10)
- Disruption classification categories (F10)

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | persistence-patterns | Follow | New query methods on SprintRepository for closed sprints and bulk membership loading | |
| 2 | persistence-patterns | Follow | New query method on DeveloperRepository for distinct sub-teams | |
| 3 | extract-feature-service | Follow | SprintSummaryService: metric computation, health score, delta, sparkline, flags | |
| 4 | create-feature | Follow | FastEndpoints GET endpoint for sprint summary, with query params | |
| 5 | create-feature | Follow | FastEndpoints GET endpoint for closed sprints list | |
| 6 | create-feature | Follow | FastEndpoints GET endpoint for sub-teams list | |
| 7 | (none) | -- | Frontend TypeScript types for all API response shapes | Frontend patterns gap |
| 8 | (none) | -- | Frontend API module for analytics and sprint/developer endpoints | Frontend patterns gap |
| 9 | (none) | -- | Pinia dashboard store with sprint selection, sub-team filter, data fetching | Frontend patterns gap |
| 10 | (none) | -- | PageToolbar wiring: sprint selector and sub-team filter dropdowns | Frontend patterns gap |
| 11 | (none) | -- | DashboardView: health score, metric cards, top epics, leaderboard, flags | Frontend patterns gap |

## Domain Model Changes

None. No new entities, value objects, or domain events. All metrics are computed on the fly from existing data (sprints, sprint memberships, tickets, developers, app settings).

## Data Model Changes

None. No new tables, columns, or migrations.

## Implementation Steps

### Step 1: Add analytics query methods to SprintRepository and DeveloperRepository

**What:** Add repository methods that the analytics endpoint needs. These are read-only query methods that return data optimized for the summary computation.

**SprintRepository -- new methods:**

1. `GetClosedSprintsAsync()` -- returns all sprints where `State == SprintState.Closed`, ordered by `StartDate` descending. Returns sprints without memberships (lightweight, for the sprint selector dropdown and sparkline window identification).

2. `GetSprintWithMembershipsAsync(int sprintId)` -- returns a single sprint with its memberships eagerly loaded, including each membership's `Ticket` navigation (needed for `EpicKey`, `EpicName`, `Summary`) and `Ticket.Assignee` navigation (needed for developer attribution). Returns null if not found.

3. `GetSprintsWithMembershipsAsync(List<int> sprintIds)` -- bulk loads multiple sprints with memberships (for sparkline window -- up to 4 sprints). Same include chain as above.

**DeveloperRepository -- new method:**

4. `GetDistinctSubTeamsAsync()` -- returns distinct non-null `SubTeam` values sorted alphabetically. A simple projection query.

5. `GetActiveDevelopersAsync()` -- returns all developers where `IsActive == true`. Needed for the leaderboard (must show all active developers, even those with 0 SP).

**Performance approach:** Each method is a single EF Core query. The bulk sprint load (method 3) uses `Where(s => sprintIds.Contains(s.Id))` -- max 4 IDs, no N+1.

**Files to modify:**
- `src/Services/Fokus/Fokus.Persistence/Repositories/SprintRepository.cs`
- `src/Services/Fokus/Fokus.Persistence/Repositories/DeveloperRepository.cs`

**Skill:** Follow `persistence-patterns` (repository query method pattern).

**Pattern reference:** Existing methods in `SprintRepository.cs` (e.g., `GetByIdAsync`, `GetAllAsync`).

**Dependencies:** None.

---

### Step 2: Create SprintSummaryService

**What:** Create a focused operation service that takes raw sprint data, developer data, and app settings, and computes the full sprint summary response. This is the core computation engine for the feature. It is a pure computation service with no database access -- it receives all data as method parameters.

**Why a service:** The computation logic (health score with interpolation, delta calculation across sprints, sparkline windowing, flag detection) is too complex for inline endpoint handler logic and will be reused by future analytics features. Per the Fokus service convention, extract into a focused operation service named after the operation.

**Location:** `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs`

This establishes the `Features/Analytics/` area for all analytics features (F8-F14).

**Service method signature (conceptual):**
- Input: selected sprint with memberships (including ticket and developer navigations), list of closed sprints with memberships (for delta and sparkline), active developers list, app settings, optional sub-team filter string
- Output: the full sprint summary response object

**Computation responsibilities:**

1. **Sub-team filtering (C2 pattern).** When a sub-team is specified, filter all membership data to only memberships whose ticket's assignee has that sub-team. This scoping applies before every other computation. This pattern must be clean enough that F9-F14 can reuse it.

2. **Core metrics computation.** From the filtered memberships of the selected sprint:
   - **Committed SP:** Sum of `StoryPoints` on memberships where `WasCommitted == true` and `RemovedAt == null` and `StoryPoints != null`.
   - **Completed SP:** Sum of `StoryPoints` on memberships where `FinalStatus` is in `AppSettings.DoneStatuses` and `RemovedAt == null` and `StoryPoints != null`.
   - **Added SP:** Sum of `StoryPoints` on memberships where `WasCommitted == false` and `RemovedAt == null` and `StoryPoints != null`.
   - **Carry-Over SP:** Sum of `StoryPoints` on non-removed memberships whose `FinalStatus` is NOT in the done statuses list and `StoryPoints != null`.
   - **Completion %:** Completed SP / Committed SP * 100. If Committed SP == 0, result is 0 (BR22).
   - **Disruption Rate:** Added SP / Committed SP * 100. If Committed SP == 0, result is 0 (BR22).
   - **Carry-Over Rate:** Carry-Over SP / (Committed SP + Added SP) * 100. If denominator == 0, result is 0 (BR22).

3. **Health score computation.** For each sub-metric (completion, disruption, carry-over):
   - Map the raw rate to a 0-100 sub-score using the configurable threshold bands (BR2):
     - **Completion (higher is better):** >= green threshold -> 100; between amber and green -> linear interpolation 50-99; below amber -> linear interpolation 0-49.
     - **Disruption and carry-over (lower is better):** <= green threshold -> 100; between green and amber -> linear interpolation 99-50; above amber -> linear interpolation 49-0, bottoming at 2x amber threshold (value at 2x amber = 0).
   - Compute per-metric RAG from the configurable per-metric thresholds (BR4).
   - Composite score = weighted average using configurable weights (default: completion 40%, disruption 30%, carry-over 30%).
   - Composite RAG: green >= 75, amber 40-74, red < 40 (BR3, fixed).

4. **Delta computation (C1 pattern).** Identify the prior closed sprint (next-earlier start date relative to selected sprint from the sorted closed sprints list). Compute the same metrics for the prior sprint. Delta = selected metric value - prior metric value. When no prior sprint exists, delta is null (BR11). Delta polarity per metric (BR12):
   - Completion % and SP completed/committed: higher is positive (green up, red down).
   - Disruption rate and carry-over rate: lower is positive (green down, red up).
   - SP completed/committed: neutral (no color).

5. **Sparkline computation.** From the sorted closed sprints list, take the window of up to 4 sprints ending at and including the selected sprint (BR13). Compute the relevant metric value for each sprint in the window. Return as a list of (sprint name, value) pairs.

6. **Top epics.** From the selected sprint's memberships (filtered), group by `Ticket.EpicKey` where not null, sum `StoryPoints` completed (final status in done statuses, not removed) per epic. Take top 3 by SP completed. For each, compute overall progress across ALL sprints (not just selected): sum done SP and total SP from all memberships with that epic key across all provided sprints. Return epic name, SP this sprint, done SP overall, total SP overall, completion %.

7. **Developer leaderboard.** From all active developers (filtered by sub-team if applicable), compute SP completed per developer in the selected sprint. Sort descending. Include developers with 0 SP. Exclude inactive developers (BR15).

8. **Flags computation.**
   - **Zombie tickets (BR16):** From ALL provided sprints' memberships, count distinct sprint appearances per ticket ID. Tickets appearing in 3+ sprints are zombies. Return ticket key, summary (from `Ticket` navigation), and sprint count.
   - **Mid-sprint disruption (BR17):** From selected sprint's memberships, find tickets where `AddedAt > sprint.StartDate + 2 days` and `RemovedAt == null`. Sum their SP and count tickets.
   - **Zero-SP developers (BR18):** From active developers (filtered by sub-team), find those who are assignees on at least one non-removed membership in the selected sprint but completed 0 SP.

**DI registration:** Register as `AddScoped<SprintSummaryService>()` in `DependencyInjection.cs`, consistent with `SprintIssueSyncService` and `WorkflowDetectionService`.

**Files to create:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs` -- service class + all response/DTO records

**Response record hierarchy (all defined in this file):**

```
SprintSummaryResponse
  SprintInfo? Sprint
  HealthScoreResult? HealthScore
  MetricsResult? Metrics
  List<EpicProgress> TopEpics
  List<DeveloperSummary> Leaderboard
  FlagsResult Flags

SprintInfo { int Id, string Name, DateTime StartDate, DateTime EndDate, int DurationDays, DateTime SyncedAt }
HealthScoreResult { decimal CompositeScore, string CompositeRag, decimal CompletionSubScore, string CompletionRag, decimal DisruptionSubScore, string DisruptionRag, decimal CarryOverSubScore, string CarryOverRag }
MetricCard { string Name, decimal Value, string DisplayValue, decimal? Delta, string? DeltaDirection, string? DeltaPolarity, List<SparklinePoint> Sparkline }
MetricsResult { MetricCard SpCompleted, MetricCard CompletionRate, MetricCard DisruptionRate, MetricCard CarryOverRate }
SparklinePoint { string SprintName, decimal Value }
EpicProgress { string EpicName, decimal SpCompletedThisSprint, decimal TotalSp, decimal DoneSp, decimal CompletionPercentage }
DeveloperSummary { string DisplayName, string? AvatarUrl, string? SubTeam, decimal SpCompleted }
FlagsResult { List<ZombieTicket> ZombieTickets, MidSprintDisruption? MidSprintDisruption, List<string> ZeroSpDevelopers, bool HasAnyFlags }
ZombieTicket { string TicketKey, string Summary, int SprintCount }
MidSprintDisruption { decimal TotalSp, int TicketCount }
```

**Skill:** Follow `extract-feature-service` (focused operation service pattern).

**Pattern reference:** `src/Services/Fokus/Fokus.API/Features/Sync/SprintIssueSyncService.cs` (focused operation service in a feature area).

**Dependencies:** None (consumes data passed by the endpoint in Step 4).

---

### Step 3: Create GetSprintSummary endpoint

**What:** Create the `GET /api/analytics/sprint-summary` endpoint. It accepts optional `sprintId` (int) and `subTeam` (string) query parameters. It orchestrates data loading from repositories and delegates computation to `SprintSummaryService`.

**Files to create:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetSprintSummary/GetSprintSummaryEndpoint.cs` -- endpoint class
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetSprintSummary/GetSprintSummaryQuery.cs` -- request + response types (response reuses the records from SprintSummaryService)

**Endpoint details:**
- Extends `Endpoint<GetSprintSummaryRequest, SprintSummaryResponse>`
- `[AllowAnonymous]`, `[HttpGet("/api/analytics/sprint-summary")]`, `[Tags("Analytics")]`
- Constructor dependencies: `SprintRepository`, `DeveloperRepository`, `AppSettingsRepository`, `SprintSummaryService`

**Handler logic:**
1. Load all closed sprints (lightweight, no memberships) via `GetClosedSprintsAsync()`.
2. If no closed sprints exist, return 200 with null sprint/healthScore/metrics and empty lists.
3. Determine selected sprint: if `sprintId` provided, validate it exists in closed sprints list (return 400 if not). If omitted, use the most recent (first in the list).
4. Determine the sparkline window: up to 4 sprints ending at the selected sprint from the sorted closed sprints list.
5. Bulk load sprints with memberships for the window via `GetSprintsWithMembershipsAsync(windowIds)`.
6. Load active developers via `GetActiveDevelopersAsync()`.
7. Load app settings via `GetAsync()`.
8. Call `SprintSummaryService.ComputeSummary(...)` with all data.
9. Return 200 with the result.

**Request type:**
```
GetSprintSummaryRequest { int? SprintId, string? SubTeam }
```

**Validator:** Validate `SprintId` is positive when provided. Validate `SubTeam` is non-empty when provided (empty string treated as null/omitted).

**Performance approach:** 3 parallel-ish DB queries (closed sprints, then bulk memberships + developers + settings). The dataset is small (SQLite, single-team, max ~20 sprints). No caching needed in v1.

**Skill:** Follow `create-feature` (FastEndpoints variant, `Endpoint<TReq, TRes>`).

**Pattern reference:** `src/Services/Fokus/Fokus.API/Features/Settings/GetSettings/GetSettingsEndpoint.cs`.

**Dependencies:** Steps 1 and 2.

---

### Step 4: Create GetClosedSprints endpoint

**What:** Create the `GET /api/sprints/closed` endpoint. Returns a lightweight list of closed sprints for the sprint selector dropdown.

**Files to create:**
- `src/Services/Fokus/Fokus.API/Features/Sprints/GetClosedSprints/GetClosedSprintsEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Sprints/GetClosedSprints/GetClosedSprintsQuery.cs`

**Endpoint details:**
- Extends `EndpointWithoutRequest<List<ClosedSprintItem>>`
- `[AllowAnonymous]`, `[HttpGet("/api/sprints/closed")]`, `[Tags("Sprints")]`
- Constructor dependency: `SprintRepository`
- Handler: call `GetClosedSprintsAsync()`, map to response items, return 200.

**Response item:** `ClosedSprintItem { int Id, string Name, DateTime StartDate, DateTime EndDate, string State }`

**Skill:** Follow `create-feature` (FastEndpoints variant, `EndpointWithoutRequest`).

**Pattern reference:** `src/Services/Fokus/Fokus.API/Features/Settings/GetSettings/GetSettingsEndpoint.cs`.

**Dependencies:** Step 1 (repository method).

---

### Step 5: Create GetSubTeams endpoint

**What:** Create the `GET /api/developers/sub-teams` endpoint. Returns distinct non-null sub-team names for the sub-team filter dropdown.

**Files to create:**
- `src/Services/Fokus/Fokus.API/Features/Developers/GetSubTeams/GetSubTeamsEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Developers/GetSubTeams/GetSubTeamsQuery.cs`

**Endpoint details:**
- Extends `EndpointWithoutRequest<List<string>>`
- `[AllowAnonymous]`, `[HttpGet("/api/developers/sub-teams")]`, `[Tags("Developers")]`
- Constructor dependency: `DeveloperRepository`
- Handler: call `GetDistinctSubTeamsAsync()`, return 200.

**Skill:** Follow `create-feature` (FastEndpoints variant, `EndpointWithoutRequest`).

**Dependencies:** Step 1 (repository method).

---

### Step 6: Register SprintSummaryService in DI

**What:** Add `SprintSummaryService` to the DI container in `DependencyInjection.cs`.

**Files to modify:**
- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` -- add `services.AddScoped<SprintSummaryService>();` and the `using` for the Analytics namespace.

**Pattern reference:** Existing registrations for `SprintIssueSyncService` and `WorkflowDetectionService` in the same file.

**Skill:** Follow `create-feature` (DI registration step).

**Dependencies:** Step 2 (service class must exist).

---

### Step 7: Add frontend TypeScript types

**What:** Add all TypeScript interfaces matching the API response shapes for the three new endpoints.

**Files to modify:**
- `client/src/types/index.ts` -- add all new interfaces

**Types to add:**

```typescript
// Sprint summary response
SprintSummaryResponse { sprint, healthScore, metrics, topEpics, leaderboard, flags }
SprintInfo { id, name, startDate, endDate, durationDays, syncedAt }
HealthScoreResult { compositeScore, compositeRag, completionSubScore, completionRag, disruptionSubScore, disruptionRag, carryOverSubScore, carryOverRag }
MetricCard { name, value, displayValue, delta, deltaDirection, deltaPolarity, sparkline }
MetricsResult { spCompleted, completionRate, disruptionRate, carryOverRate }
SparklinePoint { sprintName, value }
EpicProgress { epicName, spCompletedThisSprint, totalSp, doneSp, completionPercentage }
DeveloperSummary { displayName, avatarUrl, subTeam, spCompleted }
FlagsResult { zombieTickets, midSprintDisruption, zeroSpDevelopers, hasAnyFlags }
ZombieTicket { ticketKey, summary, sprintCount }
MidSprintDisruption { totalSp, ticketCount }

// Closed sprints list
ClosedSprintItem { id, name, startDate, endDate, state }
```

**Skill:** None (frontend patterns gap).

**Pattern reference:** Existing types in `client/src/types/index.ts`.

**Dependencies:** None (types are API contract documentation; can be written before endpoints exist).

---

### Step 8: Add frontend API module for analytics

**What:** Create a new API module for analytics endpoints and add sprint/developer helper API functions.

**Files to create:**
- `client/src/api/analytics.ts` -- sprint summary API function

**Files to modify:**
- `client/src/api/settings.ts` -- no changes needed (sprint/developer endpoints are separate concerns)

**New file: `client/src/api/analytics.ts`:**
- `getSprintSummary(sprintId?: number, subTeam?: string): Promise<SprintSummaryResponse>` -- calls `GET /analytics/sprint-summary` with optional query params
- `getClosedSprints(): Promise<ClosedSprintItem[]>` -- calls `GET /sprints/closed`
- `getSubTeams(): Promise<string[]>` -- calls `GET /developers/sub-teams`

All functions use the existing `apiFetch` from `client/src/api/client.ts`.

**Skill:** None (frontend patterns gap).

**Pattern reference:** `client/src/api/settings.ts` (existing API module pattern).

**Dependencies:** Step 7 (types must exist for return type annotations).

---

### Step 9: Create dashboard Pinia store

**What:** Create a new Pinia store for the Dashboard page that manages sprint selection, sub-team filtering, data fetching, and loading states.

**Files to create:**
- `client/src/stores/dashboardStore.ts`

**Store state:**
- `closedSprints: ClosedSprintItem[]` -- list of closed sprints for the selector
- `subTeams: string[]` -- list of sub-team names for the filter
- `selectedSprintId: number | null` -- currently selected sprint ID
- `selectedSubTeam: string | null` -- currently selected sub-team (null = "All")
- `summary: SprintSummaryResponse | null` -- the current summary data
- `loading: boolean` -- loading state for summary fetch
- `initializing: boolean` -- loading state for initial data load (sprints + sub-teams)

**Store actions:**
- `initialize()` -- fetches closed sprints and sub-teams in parallel. Sets `selectedSprintId` to the first closed sprint (most recent). Then fetches the summary. Called once on Dashboard mount.
- `selectSprint(sprintId: number)` -- updates `selectedSprintId`, fetches new summary.
- `selectSubTeam(subTeam: string | null)` -- updates `selectedSubTeam`, fetches new summary.
- `fetchSummary()` -- internal action that calls `getSprintSummary(selectedSprintId, selectedSubTeam)` and stores the result.

**URL sync:** The store does not own URL sync. The DashboardView component reads the route query param on mount and calls `selectSprint` if a sprint ID is present. On sprint change, the component updates the route.

**Skill:** None (frontend patterns gap).

**Pattern reference:** `client/src/stores/settingsStore.ts` (existing Pinia store pattern).

**Dependencies:** Steps 7 and 8 (types and API functions must exist).

---

### Step 10: Wire PageToolbar with sprint selector and sub-team filter

**What:** Replace the visual-only stubs in `PageToolbar.vue` with functional dropdown components. The toolbar receives its data and callbacks as props from the parent view, keeping it reusable for other pages.

**Files to modify:**
- `client/src/components/PageToolbar.vue` -- replace static markup with functional dropdowns

**Props to accept:**
- `sprints: ClosedSprintItem[]` -- list for the sprint selector
- `selectedSprintId: number | null` -- currently selected sprint
- `subTeams: string[]` -- list for the sub-team filter
- `selectedSubTeam: string | null` -- currently selected sub-team
- `showSubTeamFilter: boolean` (default true) -- allows pages to hide it
- `showAggregateOptions: boolean` (default false) -- future use, suppressed on Dashboard (BR20)

**Events to emit:**
- `update:selectedSprintId(id: number)` -- sprint selection changed
- `update:selectedSubTeam(subTeam: string | null)` -- sub-team filter changed

**Behavior:**
- Sprint selector: native `<select>` element styled to match the existing visual stub. Options are the closed sprints, displaying sprint name. Default selection is the `selectedSprintId` prop.
- Sub-team filter: native `<select>` element. Options are "All" (value null) plus each sub-team name. Default is the `selectedSubTeam` prop.
- Both emit events on change; the parent view handles the state update.
- When `sprints` is empty, the sprint selector shows "No sprints" and is disabled.

**Skill:** None (frontend patterns gap).

**Pattern reference:** Existing `PageToolbar.vue` markup (preserving the icon and layout structure).

**Dependencies:** Step 7 (types for prop typing).

---

### Step 11: Build DashboardView with all summary sections

**What:** Replace the current empty-state-only DashboardView with the full sprint summary display. This is the largest frontend step, composing all visual sections.

**Files to modify:**
- `client/src/views/DashboardView.vue` -- complete rewrite

**Files to create (component extraction for reuse and readability):**
- `client/src/components/dashboard/HealthScoreBadge.vue` -- the composite health score display (number + RAG color circle)
- `client/src/components/dashboard/MetricCard.vue` -- single metric card (big number, delta, sparkline)
- `client/src/components/dashboard/SprintFlags.vue` -- flags section (zombie tickets, disruption, zero-SP)

**DashboardView structure:**

1. **Mount logic:** On mount, call `dashboardStore.initialize()`. Read route query param `sprint` for initial sprint selection. Watch `selectedSprintId` changes to update route query param.

2. **Conditional rendering:**
   - If `initializing`: show a loading skeleton or spinner.
   - If no closed sprints: show the existing `EmptyState` component (already in DashboardView).
   - Otherwise: show the summary content.

3. **Summary content layout (top to bottom):**
   - **PageLayout** with title "Dashboard" and **PageToolbar** in the toolbar slot (wired to the store via props/events).
   - **Sprint header row:** Sprint name, date range (formatted), duration badge.
   - **Health score badge:** `HealthScoreBadge` component showing the composite score number and RAG color.
   - **Metric cards row:** Four `MetricCard` components in a responsive grid (2x2 on small screens, 4x1 on large). Each receives its `MetricCard` data from the store.
   - **Top epics section:** Conditional (hidden when empty per spec). List of up to 3 epic entries showing name, SP this sprint, progress bar with done/total and percentage.
   - **Developer leaderboard:** Table/list of developers with avatar, name, sub-team badge, SP completed. Sorted descending by SP.
   - **Flags section:** `SprintFlags` component. Conditional sections for zombie tickets, mid-sprint disruption, zero-SP developers. "No flags this sprint" positive message when none fire.

4. **Sparkline rendering:** Each `MetricCard` renders a sparkline using `vue3-apexcharts` (already installed in `main.ts`). Configuration: line chart, no axis labels, no grid, minimal -- just the trend line. The sparkline data points come from the `MetricCard.sparkline` array.

**Styling:**
- Use the existing design token classes (`bg-surface-card`, `border-border-default`, `text-text-primary`, etc.)
- RAG colors map to: green -> `text-status-success`, amber -> `text-status-warning`, red -> `text-status-danger`
- Delta arrows: up arrow for "up" direction, down arrow for "down", dash for "flat". Color determined by `deltaPolarity`: positive -> `text-status-success`, negative -> `text-status-danger`, neutral -> `text-text-secondary`.
- Use `BaseCard` component for card containers.

**URL routing:** Add optional `sprint` query param support to the dashboard route. On mount, if `?sprint=123` is present, pass that to `selectSprint`. On sprint change, update `router.replace({ query: { sprint: id } })`.

**Skill:** None (frontend patterns gap).

**Pattern reference:** Existing `DashboardView.vue` (page layout structure), `BaseCard.vue` (card styling), `SettingsView.vue` (store integration pattern).

**Dependencies:** Steps 9 and 10 (store and toolbar must exist).

---

## Cross-Service Changes

None. Single-service feature, read-only.

## Migration Notes

None. No database changes.

## Testing Strategy

**Backend:**
1. **Sprint summary with data.** Sync at least one closed sprint. Call `GET /api/analytics/sprint-summary`. Verify all fields populated: health score in 0-100 range, metrics have values, leaderboard has developers, sparkline has at least 1 point.
2. **Sprint summary -- no closed sprints.** Before syncing, call the endpoint. Verify 200 response with null sprint, null healthScore, null metrics, empty lists.
3. **Sprint summary -- invalid sprintId.** Call with `?sprintId=99999`. Verify 400 response.
4. **Sprint summary -- specific sprint.** Call with `?sprintId={validId}`. Verify data matches that sprint.
5. **Sub-team filtering.** Call with `?subTeam=Frontend`. Verify leaderboard only shows developers with that sub-team. Verify metrics reflect only that sub-team's work.
6. **Delta computation.** With 2+ closed sprints, verify delta values are the difference between selected and prior sprint metrics. With only 1 closed sprint, verify delta is null.
7. **Sparkline window.** With 5+ closed sprints, verify sparkline has exactly 4 points. With 2 closed sprints, verify 2 points. Select an older sprint -- verify the window shifts backward.
8. **Health score thresholds.** Modify health thresholds in settings. Recompute. Verify sub-scores change according to the interpolation formula.
9. **Division by zero.** Create a sprint with 0 committed SP. Verify completion % = 0, disruption rate = 0, no errors.
10. **Closed sprints endpoint.** Call `GET /api/sprints/closed`. Verify only closed sprints appear, ordered by start date descending.
11. **Sub-teams endpoint.** Call `GET /api/developers/sub-teams`. Verify distinct non-null sub-teams, alphabetically sorted.

**Frontend:**
12. **Dashboard loads.** Navigate to `/`. Verify sprint selector populated, most recent sprint selected, summary displayed.
13. **Sprint switching.** Select a different sprint in the dropdown. Verify all content updates. Verify URL updates to `?sprint={id}`.
14. **Sub-team filter.** Select a sub-team. Verify content filters. Select "All". Verify content returns to full scope.
15. **Empty state.** With no closed sprints, verify empty state shows with link to Settings.
16. **Sparklines render.** Verify sparkline charts appear in metric cards with correct data points.
17. **Flags display.** With zombie tickets/disruption/zero-SP conditions met, verify flags appear. With no flags, verify "No flags this sprint" message.

## Open Questions

None.
