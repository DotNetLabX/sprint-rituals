# Carry-Over Tracker

**Feature Spec:** `docs/features/CarryOverTracker/spec.md`

## Context

The Sprint Summary Card (F8) shows a single carry-over rate number. The Sprints page (F10) shows scope change analytics. This feature (F11) adds the analytical depth for carry-over: which tickets didn't finish, where they're stuck, how long they've been stuck, and whether carry-over tickets from prior sprints eventually get completed. It answers "what keeps rolling from sprint to sprint and why isn't it getting done?"

F11 extends the Sprints page built by F10. It adds carry-over sections below the scope change content on the same scrollable page, sharing F10's sprint selector and sub-team filter. No new domain entities, no new settings, no new migrations. Purely query-side analytics over existing data.

**Service impacted:** Fokus (single service). Backend: one new analytics computation service, one new analytics endpoint. Frontend: extend the Sprints page store, view, API client, and types with carry-over data.

## Scope

**In scope:**
- `GET /api/analytics/carry-over` endpoint with `sprintId`, `last`, and `subTeam` query params
- `CarryOverService` -- focused operation service for carry-over computation
- Multi-sprint mode: summary metric cards (avg carry-over rate, avg carry-over SP, total zombie tickets), carry-over rate trend line chart, stacked bar chart (carry-over SP by workflow stage), issue type breakdown, zombie tickets summary table
- Single-sprint mode: metric cards with deltas (carry-over rate, carry-over SP, carry-over ticket count), status distribution donut chart, issue type breakdown, carry-over destination section (prior sprint outcomes), full carry-over ticket table grouped by workflow stage, zombie tickets with trajectory
- Sub-team filtering across all computations
- Excluded-from-scope statuses affect all carry-over metrics (introduced by F10, already exists)

**Out of scope (per spec):**
- Root cause analysis
- Per-developer carry-over attribution (covered by F9)
- Configurable zombie threshold (fixed at 3)
- Carry-over prediction
- Aging WIP chart
- Carry-over destination beyond immediate prior sprint
- Manual carry-over tagging
- Export (PDF/PNG)
- Real-time updates (SignalR)

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | extract-feature-service | Follow | CarryOverService: carry-over metrics, status distribution, issue type breakdown, zombie detection/trajectory, carry-over destination | |
| 2 | create-feature | Follow | FastEndpoints GET endpoint for carry-over analytics, `Endpoint<TReq, TRes>` with validator | |
| 3 | (none) | -- | Frontend TypeScript types for carry-over response | Frontend patterns gap |
| 4 | (none) | -- | Frontend API function for carry-over endpoint | Frontend patterns gap |
| 5 | (none) | -- | Extend Pinia sprintsStore with carry-over data fetching | Frontend patterns gap |
| 6 | (none) | -- | Carry-over Vue components and SprintsView integration | Frontend patterns gap |

## Domain Model Changes

None. This feature introduces no new domain entities or settings. It queries existing data:
- Sprint memberships (WasCommitted, FinalStatus, StoryPoints, AddedAt, RemovedAt)
- Tickets (Key, Summary, IssueType, StoryPoints, AssigneeId)
- Sprints (Id, Name, StartDate, EndDate, State)
- Developers (SubTeam, IsActive)
- App settings (DoneStatuses, WorkflowStages, ExcludedFromScopeStatuses)

## Data Model Changes

None. No new tables, columns, or migrations.

## Implementation Steps

### Step 1: Create CarryOverService

**What:** Create a focused operation service that computes carry-over analytics for both multi-sprint and single-sprint modes. Pure computation service with no database access -- receives all data as method parameters. Define all response/DTO records in the same file.

**Files to create:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/CarryOverService.cs` -- service class + all response/DTO records

**Files to modify:**
- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` -- add `services.AddScoped<CarryOverService>();` alongside existing analytics services

**Skill:** Follow `extract-feature-service` (focused operation service pattern).

**Pattern reference:** `src/Services/Fokus/Fokus.API/Features/Analytics/ScopeChangeService.cs` (pure computation service, record hierarchy in same file, sub-team filtering via `FilterMemberships`, mode-based methods).

**DI registration location:** `src/Services/Fokus/Fokus.API/DependencyInjection.cs` line 25, add after `services.AddScoped<ScopeChangeService>();`

**Service methods:**

**`ComputeMultiSprint(List<Sprint> selectedSprints, List<Sprint> allSyncedSprints, AppSettings settings, string? subTeam)`**
- Returns: `CarryOverMultiSprintResponse`
- `selectedSprints` is the range being displayed (last N). `allSyncedSprints` is every closed sprint with memberships -- needed for accurate zombie sprint counting per BR7 ("The count includes all synced sprints, not just the selected range").

**`ComputeSingleSprint(Sprint targetSprint, Sprint? priorSprint, List<Sprint> allSyncedSprints, AppSettings settings, string? subTeam)`**
- Returns: `CarryOverSingleSprintResponse`
- `allSyncedSprints` is needed for zombie trajectory -- the trajectory shows all sprints a ticket appeared in (not just the selected range)

**Computation responsibilities:**

1. **Sub-team filtering (C2 pattern).** Reuse the established pattern from `ScopeChangeService.FilterMemberships`: filter memberships to those where `Ticket.Assignee.SubTeam == subTeam`. When subTeam is null/empty, no filtering.

2. **Excluded-from-scope filtering.** Tickets whose `FinalStatus` matches an entry in `ExcludedFromScopeStatuses` (case-insensitive via `StringComparer.OrdinalIgnoreCase`) are excluded from all carry-over SP metrics. They still appear in the ticket table but are marked `isExcluded = true`. Same `IsExcluded` helper pattern as `ScopeChangeService`.

3. **Carry-over identification (BR1).** A ticket is carry-over if:
   - `FinalStatus` is NOT in `DoneStatuses` (case-insensitive)
   - `RemovedAt` is null (not removed from sprint)
   This matches the carry-over definition in F8's `SprintSummaryService.ComputeMetrics`.

4. **Carry-over metrics per sprint (BR2, BR16, BR17).**
   - **Carry-over SP:** Sum of `StoryPoints` on carry-over tickets (where `StoryPoints.HasValue` AND not excluded).
   - **Carry-over ticket count:** Count of carry-over tickets (regardless of SP, regardless of exclusion -- ticket count metric).
   - **Total scope SP (denominator):** Active committed SP (committed, not removed, has SP, not excluded) + added SP (not committed, not removed, has SP, not excluded). Same formula as F8.
   - **Carry-over rate:** `carryOverSp / totalScopeSp * 100`. When denominator is 0, rate is 0 (BR17).

5. **Status distribution (BR4, BR5, BR20).** Group carry-over tickets by workflow stage:
   - Map each ticket's `FinalStatus` to a workflow stage by checking which stage in `WorkflowStages` matches. `WorkflowStages` is an ordered `List<string>` of stage names. In the current implementation, stages are flat names (not nested status lists), and each `FinalStatus` is matched against the stage names directly (case-insensitive).
   - Statuses not matching any configured stage go into "Other".
   - "Other" appears last. Configured stages appear in configured order.
   - When `WorkflowStages` is empty, all tickets go into "Other" (BR20).
   - Each group: stage name, ticket count, SP total (from tickets with SP, not excluded), percentage of total carry-over.

6. **Issue type breakdown (BR6).** Group carry-over tickets by raw `Ticket.IssueType` (Story, Bug, Task, etc.). **Exclude tickets with excluded-from-scope statuses** from issue type counts per BR3. Each group: type name, ticket count, SP total, percentage. No mapping or grouping -- raw Jira types.

7. **Zombie detection (BR7).** A ticket appearing in `SprintMembership` for 3+ distinct sprints is a zombie. Count uses ALL synced sprints, not just the selected range. Both modes receive `allSyncedSprints` -- use it to count distinct sprint appearances per ticket. A ticket is zombie if `allSyncedSprints.SelectMany(s => s.Memberships).Where(m => m.TicketId == ticketId).Select(m => m.SprintId).Distinct().Count() >= 3`.

8. **Multi-sprint summary metrics (BR14).**
   - Average carry-over rate across selected sprints.
   - Average carry-over SP across selected sprints.
   - Total zombie ticket count (distinct zombies across the selected range).

9. **Multi-sprint per-sprint data.** For each sprint in the selected range: carry-over SP, carry-over ticket count, carry-over rate, total scope SP, status distribution.

10. **Single-sprint delta computation (BR12, BR13, C1 pattern).** Compare each metric to the prior closed sprint:
    - Carry-over rate: polarity `positive-down` (lower is better, green when decreasing).
    - Carry-over SP: polarity `positive-down`.
    - Carry-over ticket count: polarity `positive-down`.
    - When no prior sprint exists, delta is null.
    - Use `ScopeMetricCard` record (same as `ScopeChangeService`) for consistency.

11. **Carry-over destination (BR9, BR10, BR11, single-sprint only).** Compare the prior sprint's carry-over tickets to the current sprint:
    - **Completed:** ticket is in current sprint AND `FinalStatus` in `DoneStatuses`.
    - **Carried again:** ticket is in current sprint, `FinalStatus` NOT in `DoneStatuses`, `RemovedAt` is null.
    - **Removed:** ticket is in current sprint but `RemovedAt` is not null.
    - **Dropped:** ticket does NOT appear in current sprint's memberships at all.
    - Each bucket: count and SP total.
    - When no prior sprint exists, destination is null. When prior sprint had zero carry-over, return a marker object (priorCarryOverCount = 0).

12. **Carry-over ticket table (BR19, single-sprint only).** List every carry-over ticket:
    - Ticket key, summary, issue type, story points (nullable), final status, workflow stage (mapped or "Other"), sprint count (across all synced sprints), is zombie (sprint count >= 3), is excluded.
    - Grouped by workflow stage (frontend handles grouping using the stage field).

13. **Zombie trajectory (BR8, single-sprint only).** For each zombie ticket in the selected sprint:
    - Find all sprints the ticket appeared in (from `allSyncedSprints`), ordered by `StartDate`.
    - Cap at 10 most recent sprints.
    - Each trajectory entry: sprint ID, sprint name, final status in that sprint.
    - Order zombies by sprint count descending.
    - When no zombie tickets exist, return empty list (frontend hides section).

**Response record hierarchy (defined in the service file):**

```
CarryOverSprintInfo { int Id, string Name, DateTime StartDate, DateTime EndDate }

// --- Multi-sprint ---
CarryOverSummaryMetrics { decimal AverageCarryOverRate, decimal AverageCarryOverSp, int TotalZombieTickets }

CarryOverStatusDistributionEntry { string StageName, int TicketCount, decimal SpTotal, decimal Percentage }

CarryOverPerSprintData {
    int SprintId,
    decimal CarryOverSp,
    int CarryOverTicketCount,
    decimal CarryOverRate,
    decimal TotalScopeSp,
    List<CarryOverStatusDistributionEntry> StatusDistribution
}

CarryOverIssueTypeEntry { string IssueType, int TicketCount, decimal SpTotal, decimal Percentage }

CarryOverZombieSummary { string TicketKey, string Summary, string IssueType, string CurrentStatus, decimal? StoryPoints, int SprintCount }

CarryOverMultiSprintResponse {
    List<CarryOverSprintInfo> Sprints,
    CarryOverSummaryMetrics SummaryMetrics,
    List<CarryOverPerSprintData> PerSprintData,
    List<CarryOverIssueTypeEntry> IssueTypeBreakdown,
    List<CarryOverZombieSummary> ZombieTickets
}

// --- Single-sprint ---
CarryOverSingleSprintMetrics {
    ScopeMetricCard CarryOverRate,
    ScopeMetricCard CarryOverSp,
    ScopeMetricCard CarryOverTicketCount
}

CarryOverDestinationBucket { int Count, decimal Sp }

CarryOverDestination {
    int PriorSprintId,
    string PriorSprintName,
    int PriorCarryOverCount,
    decimal PriorCarryOverSp,
    CarryOverDestinationBucket Completed,
    CarryOverDestinationBucket CarriedAgain,
    CarryOverDestinationBucket Removed,
    CarryOverDestinationBucket Dropped
}

CarryOverTicketEntry {
    string TicketKey,
    string Summary,
    string IssueType,
    decimal? StoryPoints,
    string FinalStatus,
    string WorkflowStage,
    int SprintCount,
    bool IsZombie,
    bool IsExcluded
}

ZombieTrajectorySprintEntry { int SprintId, string SprintName, string FinalStatus }

ZombieTrajectoryEntry {
    string TicketKey,
    string Summary,
    string IssueType,
    decimal? StoryPoints,
    string CurrentStatus,
    int SprintCount,
    List<ZombieTrajectorySprintEntry> Sprints
}

CarryOverSingleSprintResponse {
    CarryOverSprintInfo Sprint,
    CarryOverSingleSprintMetrics Metrics,
    List<CarryOverStatusDistributionEntry> StatusDistribution,
    List<CarryOverIssueTypeEntry> IssueTypeBreakdown,
    CarryOverDestination? CarryOverDestination,
    List<CarryOverTicketEntry> Tickets,
    List<ZombieTrajectoryEntry> ZombieTrajectories
}

// --- Response wrapper ---
CarryOverResponse {
    string Mode,    // "multi" or "single"
    CarryOverMultiSprintResponse? MultiSprint,
    CarryOverSingleSprintResponse? SingleSprint
}
```

**Reuse `ScopeMetricCard` from `ScopeChangeService.cs`** for the single-sprint metric cards. It's already a public record in the same namespace (`Fokus.API.Features.Analytics`). Also reuse the `BuildMetricCard` helper -- extract it to a shared static method or duplicate it (it's 15 lines). Recommendation: duplicate in `CarryOverService` as a private static method, same as `ScopeChangeService` does. Avoid coupling the two services.

**Performance approach:** Dataset is small (SQLite, single team). Both modes load ALL closed sprints with memberships (max ~20 sprints) -- needed for accurate zombie sprint counting per BR7. The selected range determines which sprints are displayed, but zombie detection needs the full history. Single-sprint mode additionally uses this full set for zombie trajectory. No caching needed.

**Dependencies:** None.

---

### Step 2: Create GetCarryOver endpoint

**What:** Create the `GET /api/analytics/carry-over` endpoint. Accepts optional `sprintId` (int), `last` (int), and `subTeam` (string) query parameters. Orchestrates data loading from repositories and delegates computation to `CarryOverService`.

**Files to create:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetCarryOver/GetCarryOverEndpoint.cs` -- endpoint class
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetCarryOver/GetCarryOverQuery.cs` -- request + validator

**Skill:** Follow `create-feature` (FastEndpoints variant, `Endpoint<TReq, TRes>` with validator).

**Pattern reference:** `src/Services/Fokus/Fokus.API/Features/Analytics/GetScopeChange/GetScopeChangeEndpoint.cs` (analytics endpoint, mode-based logic, repository orchestration, service delegation).

**Endpoint details:**
- Extends `Endpoint<GetCarryOverRequest, CarryOverResponse>`
- `[AllowAnonymous]`, `[HttpGet("/api/analytics/carry-over")]`, `[Tags("Analytics")]`
- Constructor dependencies: `SprintRepository`, `AppSettingsRepository`, `CarryOverService`

**Request type:**
```
GetCarryOverRequest { int? SprintId, int? Last, string? SubTeam }
```

**Validator** (same pattern as `GetScopeChangeRequestValidator`):
- `SprintId` must be > 0 when provided.
- `Last` must be >= 0 when provided (0 means all sprints, matching scope change convention).
- Custom rule: `SprintId` and `Last` must not both be provided (400).
- `SubTeam` must be non-empty when provided.

**Handler logic:**
1. Load all closed sprints (lightweight, no memberships) via `GetClosedSprintsAsync()`.
2. If no closed sprints exist, return 200 with empty response (mode = "multi", null data).
3. Normalize sub-team (empty string -> null).
4. Load app settings via `GetAsync()`.
5. Load ALL closed sprints with memberships via `GetSprintsWithMembershipsAsync(allClosedIds)`, ordered ascending by `StartDate`. Both modes need the full history for accurate zombie sprint counting (BR7: "The count includes all synced sprints, not just the selected range").
6. Determine mode:

**Multi-sprint mode (last or default):**
7a. `last = req.Last ?? 5` (default last 5). `last == 0` means all.
8a. Identify the selected sprints from the full loaded set (take last N ascending).
9a. Call `CarryOverService.ComputeMultiSprint(selectedSprints, allLoadedSprints, settings, subTeam)`.
10a. Return wrapped response with mode = "multi".

**Single-sprint mode (sprintId):**
7b. Find the target sprint in the loaded set. 400 if not found.
8b. Identify prior sprint (next-earlier by start date in the ascending list).
9b. Call `CarryOverService.ComputeSingleSprint(targetSprint, priorSprint, allLoadedSprints, settings, subTeam)`.
10b. Return wrapped response with mode = "single".

**Performance note:** Loading all closed sprints with memberships is heavier than the scope change endpoint (which only loads the selected range). However, the dataset is small (SQLite, single team, typically 10-20 sprints). Both modes require the full history for zombie detection, so a single upfront load is simpler than two separate queries. If this becomes a concern, an optimization is to load lightweight sprints for sprint-count computation and full memberships only for the display range -- but defer this to if/when performance is measured.

**Error reporting:**
- 400 with validation errors for bad input (both params, invalid sprint).
- 200 with mode="multi", null data when no closed sprints exist.

**Dependencies:** Step 1.

---

### Step 3: Add frontend TypeScript types for carry-over response

**What:** Add all TypeScript interfaces matching the API response shapes for the carry-over endpoint.

**Files to modify:**
- `client/src/types/index.ts` -- add new interfaces at the end, after the scope change types

**Skill:** None (frontend patterns gap).

**Pattern reference:** Existing scope change types in `client/src/types/index.ts` (lines 208-308).

**Types to add:**

```typescript
// Carry-over types
export interface CarryOverSprintInfo {
  id: number
  name: string
  startDate: string
  endDate: string
}

export interface CarryOverSummaryMetrics {
  averageCarryOverRate: number
  averageCarryOverSp: number
  totalZombieTickets: number
}

export interface CarryOverStatusDistributionEntry {
  stageName: string
  ticketCount: number
  spTotal: number
  percentage: number
}

export interface CarryOverPerSprintData {
  sprintId: number
  carryOverSp: number
  carryOverTicketCount: number
  carryOverRate: number
  totalScopeSp: number
  statusDistribution: CarryOverStatusDistributionEntry[]
}

export interface CarryOverIssueTypeEntry {
  issueType: string
  ticketCount: number
  spTotal: number
  percentage: number
}

export interface CarryOverZombieSummary {
  ticketKey: string
  summary: string
  issueType: string
  currentStatus: string
  storyPoints: number | null
  sprintCount: number
}

export interface CarryOverMultiSprintResponse {
  sprints: CarryOverSprintInfo[]
  summaryMetrics: CarryOverSummaryMetrics
  perSprintData: CarryOverPerSprintData[]
  issueTypeBreakdown: CarryOverIssueTypeEntry[]
  zombieTickets: CarryOverZombieSummary[]
}

export interface CarryOverSingleSprintMetrics {
  carryOverRate: ScopeMetricCard
  carryOverSp: ScopeMetricCard
  carryOverTicketCount: ScopeMetricCard
}

export interface CarryOverDestinationBucket {
  count: number
  sp: number
}

export interface CarryOverDestination {
  priorSprintId: number
  priorSprintName: string
  priorCarryOverCount: number
  priorCarryOverSp: number
  completed: CarryOverDestinationBucket
  carriedAgain: CarryOverDestinationBucket
  removed: CarryOverDestinationBucket
  dropped: CarryOverDestinationBucket
}

export interface CarryOverTicketEntry {
  ticketKey: string
  summary: string
  issueType: string
  storyPoints: number | null
  finalStatus: string
  workflowStage: string
  sprintCount: number
  isZombie: boolean
  isExcluded: boolean
}

export interface ZombieTrajectorySprintEntry {
  sprintId: number
  sprintName: string
  finalStatus: string
}

export interface ZombieTrajectoryEntry {
  ticketKey: string
  summary: string
  issueType: string
  storyPoints: number | null
  currentStatus: string
  sprintCount: number
  sprints: ZombieTrajectorySprintEntry[]
}

export interface CarryOverSingleSprintResponse {
  sprint: CarryOverSprintInfo
  metrics: CarryOverSingleSprintMetrics
  statusDistribution: CarryOverStatusDistributionEntry[]
  issueTypeBreakdown: CarryOverIssueTypeEntry[]
  carryOverDestination: CarryOverDestination | null
  tickets: CarryOverTicketEntry[]
  zombieTrajectories: ZombieTrajectoryEntry[]
}

export interface CarryOverResponse {
  mode: 'multi' | 'single'
  multiSprint: CarryOverMultiSprintResponse | null
  singleSprint: CarryOverSingleSprintResponse | null
}
```

**Note:** `ScopeMetricCard` is already defined in the types file (line 249) and is reused for carry-over single-sprint metrics.

**Dependencies:** None.

---

### Step 4: Add frontend API function for carry-over endpoint

**What:** Add the API function for the carry-over analytics endpoint.

**Files to modify:**
- `client/src/api/analytics.ts` -- add `getCarryOver` function

**Skill:** None (frontend patterns gap).

**Pattern reference:** `getScopeChange` function in `client/src/api/analytics.ts` (lines 29-36).

**Function to add:**

```typescript
export function getCarryOver(sprintId?: number, last?: number, subTeam?: string): Promise<CarryOverResponse> {
  const params = new URLSearchParams()
  if (sprintId !== undefined) params.set('sprintId', String(sprintId))
  if (last !== undefined) params.set('last', String(last))
  if (subTeam) params.set('subTeam', subTeam)
  const query = params.toString()
  return apiFetch<CarryOverResponse>(`/analytics/carry-over${query ? `?${query}` : ''}`)
}
```

Import `CarryOverResponse` from `'../types'`.

**Dependencies:** Step 3 (types for return type annotation).

---

### Step 5: Extend sprintsStore with carry-over data fetching

**What:** Extend the existing `sprintsStore.ts` to fetch carry-over data alongside scope change data. Both endpoints share the same query parameters (sprintId, last, subTeam) and should be fetched in parallel.

**Files to modify:**
- `client/src/stores/sprintsStore.ts`

**Skill:** None (frontend patterns gap).

**Pattern reference:** Existing `sprintsStore.ts` (lines 53-77, `fetchScopeChange` action).

**Changes:**

1. Add `carryOver` ref: `const carryOver = ref<CarryOverResponse | null>(null)`
2. Add a `fetchCarryOver()` action that mirrors `fetchScopeChange()` -- same mode/param logic, calls `getCarryOver(...)`.
3. Modify `fetchScopeChange()` to also call `fetchCarryOver()` in parallel. Rename the internal fetch to `fetchData()` or keep both calls in the existing action. Recommendation: create a new `fetchAllData()` that calls both `getScopeChange` and `getCarryOver` with `Promise.all`, and have `selectSprint`, `selectLastN`, and `selectSubTeam` call `fetchAllData()` instead of `fetchScopeChange()`.
4. Export `carryOver` from the store return.

**Why parallel:** Both endpoints use the same query params and are independent -- fetching them in parallel halves the perceived load time.

**Dependencies:** Step 4 (API function).

---

### Step 6: Build carry-over Vue components and integrate into SprintsView

**What:** Create carry-over display components and add them to the SprintsView below the scope change sections. The carry-over sections render conditionally based on the same mode (multi vs single) as scope change.

**Files to create:**
- `client/src/components/sprints/CarryOverMetricCards.vue` -- summary metric cards (multi) or detail metric cards with deltas (single)
- `client/src/components/sprints/CarryOverRateChart.vue` -- carry-over rate trend line chart (multi-sprint)
- `client/src/components/sprints/CarryOverStackedChart.vue` -- stacked bar chart of carry-over SP by workflow stage per sprint (multi-sprint)
- `client/src/components/sprints/IssueTypeBreakdown.vue` -- carry-over ticket counts by raw issue type (both modes)
- `client/src/components/sprints/ZombieSummaryTable.vue` -- zombie tickets summary table (multi-sprint)
- `client/src/components/sprints/StatusDistributionChart.vue` -- donut chart of carry-over by workflow stage (single-sprint)
- `client/src/components/sprints/CarryOverDestination.vue` -- prior sprint outcomes with horizontal stacked bar (single-sprint)
- `client/src/components/sprints/CarryOverTicketTable.vue` -- full carry-over ticket table grouped by workflow stage (single-sprint)
- `client/src/components/sprints/ZombieTrajectorySection.vue` -- zombie tickets with sprint-by-sprint history (single-sprint)

**Files to modify:**
- `client/src/views/SprintsView.vue` -- add carry-over sections below scope change sections

**Skill:** None (frontend patterns gap).

**Pattern reference:** Existing scope change components in `client/src/components/sprints/` (ScopeMetricCards.vue, ScopeChangeChart.vue, ClassificationTable.vue, etc.) for component structure, props patterns, chart integration, and styling tokens.

**SprintsView integration:**

Add a section separator (heading "Carry-Over Analysis") between scope change and carry-over sections. Then render carry-over components conditionally:

**Multi-sprint mode (when `store.carryOver?.mode === 'multi'`):**
1. `CarryOverMetricCards` -- 3 cards: Avg Carry-Over Rate, Avg Carry-Over SP, Total Zombie Tickets
2. `CarryOverRateChart` -- line chart showing carry-over rate per sprint (vue3-apexcharts, type: line)
3. `CarryOverStackedChart` -- stacked bar chart showing carry-over SP segmented by workflow stage per sprint (vue3-apexcharts, type: bar, stacked)
4. `IssueTypeBreakdown` -- table of carry-over by issue type
5. `ZombieSummaryTable` -- table of zombie tickets (key, summary, issue type, current status, sprint count)

**Single-sprint mode (when `store.carryOver?.mode === 'single'`):**
1. `CarryOverMetricCards` -- 3 metric cards with deltas (carry-over rate, carry-over SP, carry-over ticket count)
2. `StatusDistributionChart` -- donut chart (vue3-apexcharts, type: donut) of carry-over by workflow stage
3. `IssueTypeBreakdown` -- table of carry-over by issue type for this sprint
4. `CarryOverDestination` -- prior sprint outcomes (completed, carried again, removed, dropped) with horizontal stacked bar. Hidden when no prior sprint. Shows "No carry-over from prior sprint" when prior had zero carry-over.
5. `CarryOverTicketTable` -- full ticket table, grouped by workflow stage. Each group header shows stage name, ticket count, SP subtotal. Zombie tickets highlighted with a badge or distinct background (`bg-status-warning/10` or similar). Excluded tickets shown with `opacity-50` + "excluded" badge. **When no workflow stages are configured (all tickets under "Other"):** omit the workflow stage column and do not group — show a flat table (BR20).
6. `ZombieTrajectorySection` -- for each zombie ticket: key, summary, issue type, SP, then a trajectory row showing ordered sprint names with final status in each. Hidden when no zombies exist. Capped at 10 most recent sprints per ticket. Ordered by sprint count descending.

**Chart click interaction:** Clicking a sprint bar in `CarryOverStackedChart` (multi-sprint) navigates to single-sprint mode, same as `ScopeChangeChart` does via `@sprint-click` emit.

**Styling:**
- Reuse existing design tokens (`bg-surface-card`, `border-border-default`, `text-text-primary`, etc.)
- Delta colors: reuse `ScopeMetricCards.vue` delta coloring pattern
- Zombie badge: `bg-status-warning/10 text-status-warning` with "Zombie" text or skull indicator
- Excluded tickets: `opacity-50` + small "excluded" badge (same pattern as `EventTable.vue`)
- Section heading: "Carry-Over Analysis" using same heading style as section headers in the page
- All sections use `BaseCard` as container (if used by scope change components)

**Dependencies:** Step 5 (store must have carry-over data).

---

## Cross-Service Changes

None. Single-service feature, read-only analytics.

## Migration Notes

None. No new tables, columns, or migrations.

## Testing Strategy

**Backend:**
1. **Multi-sprint default.** Call `GET /api/analytics/carry-over` with no params. Verify mode="multi", last 5 closed sprints returned, per-sprint data with carry-over metrics.
2. **Multi-sprint with last.** Call with `last=3`. Verify exactly 3 sprints in response.
3. **Single sprint.** Call with `sprintId={validId}`. Verify mode="single", single-sprint response with metrics, status distribution, ticket table, issue type breakdown.
4. **Mutual exclusivity.** Call with both `sprintId` and `last`. Verify 400.
5. **Invalid sprint.** Call with `sprintId=99999`. Verify 400.
6. **Carry-over identification.** Sprint with tickets in various states: done (should NOT be carry-over), not done + not removed (should be carry-over), removed (should NOT be carry-over). Verify correct identification.
7. **Carry-over rate formula.** Sprint with known committed SP and added SP. Verify rate = carryOverSP / (committedSP + addedSP) * 100. Verify division by zero produces 0.
8. **Delta computation.** With 2+ closed sprints, verify delta values in single-sprint mode. With only 1 closed sprint, verify deltas are null.
9. **Status distribution.** Configure workflow stages. Create carry-over tickets with statuses matching different stages. Verify correct grouping. Verify "Other" appears last for unmatched statuses.
10. **No workflow stages.** Clear workflow stages. Verify all tickets group under "Other".
11. **Issue type breakdown.** Create carry-over tickets of different issue types. Verify correct grouping with raw Jira types.
12. **Zombie detection.** Create a ticket appearing in 3+ sprints. Verify it's flagged as zombie with correct sprint count. Verify tickets in 1-2 sprints are not flagged.
13. **Zombie trajectory.** For a zombie ticket, verify trajectory shows all sprints with correct final status per sprint, ordered chronologically, capped at 10.
14. **Carry-over destination -- completed.** Prior sprint carry-over ticket reaches done status in current sprint. Verify counted in "completed" bucket.
15. **Carry-over destination -- carried again.** Prior carry-over ticket still not done in current sprint. Verify "carried again" bucket.
16. **Carry-over destination -- removed.** Prior carry-over ticket removed from current sprint. Verify "removed" bucket.
17. **Carry-over destination -- dropped.** Prior carry-over ticket not present at all in current sprint. Verify "dropped" bucket.
18. **Carry-over destination -- no prior sprint.** First synced sprint. Verify destination is null.
19. **Carry-over destination -- prior had zero carry-over.** Verify destination returned with priorCarryOverCount = 0.
20. **Excluded-from-scope effect.** Configure excluded statuses. Verify carry-over SP excludes matching tickets. Verify ticket table shows them with `isExcluded=true`. Verify carry-over rate denominator excludes them.
21. **Sub-team filter.** Call with `subTeam=Frontend`. Verify all metrics scoped to that sub-team's developers.
22. **Tickets with no SP.** Verify they count in ticket count metrics but not in SP metrics.

**Frontend:**
23. **Carry-over sections appear.** Navigate to `/sprints`. Verify carry-over sections appear below scope change sections.
24. **Multi-sprint carry-over.** Verify metric cards, trend chart, stacked bar chart, issue type table, zombie table all render.
25. **Single-sprint carry-over.** Select a single sprint. Verify metric cards with deltas, donut chart, ticket table, destination section, zombie trajectories.
26. **Chart click navigation.** Click a sprint bar in carry-over stacked chart. Verify navigation to single-sprint mode.
27. **Sub-team filter.** Select a sub-team. Verify carry-over content filters.
28. **Zombie section hidden.** When no zombies exist, verify zombie section is not rendered.
29. **Destination hidden.** When no prior sprint, verify destination section hidden. When prior has zero carry-over, verify "No carry-over from prior sprint" message.
30. **Excluded tickets visual.** Verify excluded tickets in ticket table show dimmed with badge.

## Open Questions

None.
