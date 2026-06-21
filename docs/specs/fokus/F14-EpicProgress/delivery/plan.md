# Epic Progress (F14)

**Feature Spec:** `docs/features/EpicProgress/spec.md`

## Context

Scrum Masters need a dedicated Epics page that answers "how is this epic actually going?" with data. Jira shows per-ticket status but no cross-sprint epic velocity, projected completion, or cumulative progress. The Sprint Summary Card (F8) surfaces a lightweight "Top 3 epics progressed" per sprint — this feature provides the full analytical depth: total scope including backlog tickets, dual completion tracking (ticket count and story points), imputed SP for unestimated tickets, epic-level velocity, projected sprints to completion, and per-ticket drill-down. It also aligns F8's Top Epics completion calculation with F14's cumulative ticket-status-based approach (BR20).

**Service impacted:** Fokus (single service — API, Domain, Persistence, frontend)

## Scope

**In scope:**
- `EpicProgressService` computation service with response records
- `GET /api/analytics/epic-progress` endpoint with `subTeam` query parameter
- Repository query methods for loading tickets with epic keys and sprint memberships for velocity
- Frontend: types, API function, new epics store, EpicsView (replacing placeholder), epic table components, summary cards, expanded ticket detail, active/completed toggle, sub-team filter
- F8 Top Epics alignment: update `SprintSummaryService.ComputeTopEpics()` to use cumulative ticket-status-based completion
- Help tooltips wired to UI elements per `docs/features/EpicProgress/help.tooltips.md`

**Out of scope:** Epic burndown/burnup chart, epic workflow states as stored concept, per-sprint epic breakdown, epic-level SP editing, epic grouping/hierarchy, configurable velocity window, export, real-time updates, column sorting, search/filter within epics, delta pattern (C1), multi-sprint selection (C3).

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | persistence-patterns | Follow | New query methods on TicketRepository and SprintRepository | |
| 2 | extract-feature-service | Follow | EpicProgressService in Features/Analytics/, response records in same file | |
| 3 | create-feature | Follow | FastEndpoints GET endpoint, query+validator, DI registration | |
| 4 | (none) | — | Update SprintSummaryService.ComputeTopEpics() — F8 alignment, logic change | Log: no "metric-refactor" skill |
| 5 | create-vue-feature | Follow | Types in index.ts matching backend response records | |
| 6 | create-vue-feature | Follow | API function in analytics.ts | |
| 7 | pinia-patterns | Follow | New epicsStore with sub-team filter and active/completed toggle | |
| 8 | vue-component-architecture | Follow | EpicsView replacing placeholder, L0 view with L1 container | |
| 9 | vue-patterns | Follow | L3 props-only components: summary cards, epic table, expanded ticket table. Includes tooltip wiring per help.tooltips.md | |
| 10 | (none) | — | Build verification and manual testing | |

## Domain Model Changes

None. This feature queries existing Ticket, SprintMembership, Developer, and AppSettings entities. No new aggregates, entities, value objects, or domain events.

## Data Model Changes

None. No new tables, columns, relationships, or migrations. All data is queried from existing tables.

## Implementation Steps

### Step 1: Add repository query methods for epic progress data

**What:** Add query methods to TicketRepository and SprintRepository that the EpicProgressService needs. The endpoint needs all tickets grouped by epic (including backlog tickets with no sprint membership), and sprint membership data for velocity calculations.

**Files to modify:**
- `src/Services/Fokus/Fokus.Persistence/Repositories/TicketRepository.cs` — add:
  - `GetTicketsWithEpicAsync(CancellationToken ct)` — returns all tickets where `EpicKey != null`, including navigation to `Assignee`. This is the primary data source for epic scope (BR1).
  - `GetTicketsWithoutEpicInSprintsAsync(CancellationToken ct)` — returns tickets where `EpicKey == null` that have at least one sprint membership (for unlinked work count, BR14). Include navigation to `Assignee` (needed for sub-team filtering on unlinked work — BR13, AC27).
- `src/Services/Fokus/Fokus.Persistence/Repositories/SprintRepository.cs` — add:
  - `GetAllClosedSprintMembershipsAsync(CancellationToken ct)` — returns all SprintMemberships from closed sprints, including navigation to Ticket (for epic key lookup) AND Ticket.Assignee (for sub-team filtering on velocity data — BR13). Used for velocity computation across all sprints (BR7, BR8, BR16, BR24). The query must `.Include(m => m.Ticket).ThenInclude(t => t.Assignee)` — without the Assignee navigation, sub-team filtering on velocity memberships would fail silently.

**Pattern to follow:** Existing query methods in `TicketRepository.cs` (e.g., `GetAllKeysWithEpicAsync`) and `SprintRepository.cs` (e.g., `GetSprintsWithMembershipsAsync`).

**Follow:** persistence-patterns

**Dependencies:** None

### Step 2: Create EpicProgressService with response records

**What:** Create the computation service that takes loaded tickets, sprint memberships, developers, and settings, and produces the epic progress response. All computation is in-memory after data is loaded.

**Files to create:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/EpicProgressService.cs` — service class + all response record types

**Response records (defined in same file, above the service class):**

```
EpicProgressSummaryMetrics(
    int ActiveEpicCount,
    int CompletedEpicCount,
    decimal AverageCompletionPercentage)

EpicProgressTicketEntry(
    string TicketKey,
    string Summary,
    string IssueType,
    decimal? StoryPoints,
    string CurrentStatus,
    string? AssigneeDisplayName,
    bool IsDone)

EpicProgressEntry(
    string EpicKey,
    string EpicName,
    int TotalTickets,
    int DoneTickets,
    int RemainingTickets,
    decimal TicketCompletionPercentage,
    decimal TotalSp,
    decimal DoneSp,
    decimal RemainingSp,
    decimal? ImputedSp,
    decimal? AdjustedTotalSp,
    decimal? SpCompletionPercentage,
    int UnestimatedTicketCount,
    decimal? Velocity,
    decimal? ProjectedSprintsRemaining,
    string? ProjectionConfidence,
    int ActiveSprintCount,
    bool IsCompleted,
    List<EpicProgressTicketEntry> Tickets)

EpicProgressUnlinkedWork(
    int TicketCount,
    decimal TotalSp)

EpicProgressResponse(
    EpicProgressSummaryMetrics SummaryMetrics,
    List<EpicProgressEntry> Epics,
    EpicProgressUnlinkedWork UnlinkedWork)
```

**Service method:**
- `ComputeEpicProgress(List<Ticket> epicTickets, List<SprintMembership> allClosedMemberships, List<Ticket> unlinkedTickets, AppSettings settings, string? subTeam)` — returns `EpicProgressResponse`

**Key computation logic (from spec business rules):**

1. **Sub-team filtering (BR13):** When subTeam is provided, filter epicTickets to those where `Assignee?.SubTeam == subTeam`. Filter unlinkedTickets similarly. Filter sprint memberships to those whose ticket is assigned to a developer in the sub-team. All downstream computations use filtered sets.

2. **Per-epic grouping:** Group filtered tickets by `EpicKey`. For each epic:
   - Epic name = first ticket's `EpicName ?? EpicKey` (BR22)
   - Done = ticket where `CurrentStatus` is in `settings.DoneStatuses` (BR2)
   - Ticket counts and ticket completion percentage (BR3)
   - SP sums: total SP, done SP, remaining SP from estimated tickets (BR15)
   - Imputed SP: average SP of estimated tickets in this epic, applied only to remaining unestimated tickets (BR4, BR5, BR6). Null when zero estimated tickets exist (BR4).
   - Adjusted total SP = total SP + imputed SP (BR4)
   - SP completion % = done SP / adjusted total SP (BR3, BR19)
   - Unestimated ticket count (BR15)

3. **Velocity (BR7, BR8, BR16, BR24):** From allClosedMemberships, filter to memberships where the ticket's current EpicKey matches this epic. Group by SprintId. For each sprint, sum SP where `FinalStatus` is in doneStatuses AND `StoryPoints.HasValue` AND `RemovedAt == null`. Only sprints with > 0 completed SP count as data points. Take the 3 most recent data points (by sprint start date). Velocity = average of those data points. Null when no data points.

4. **Projection (BR9, BR10):** Projected sprints = (remaining SP + imputed SP) / velocity. Null when velocity is null or zero. Confidence = "low" when fewer than 3 data points, null otherwise.

5. **Active sprint count (BR17):** Count of distinct SprintIds where this epic had at least one ticket in a sprint membership.

6. **Is completed (BR11):** All tickets have a done current status.

7. **Ticket list:** All tickets in the epic with key, summary, issue type, story points (actual, not imputed — BR spec: "no imputed value shown at ticket level"), current status, assignee display name (or null), is done flag. Sorted: remaining first (grouped by status), then done (BR spec Flow 2 step 3). Unassigned tickets: assignee display name = null (frontend handles "Unassigned" display — BR23).

8. **Summary metrics:** Active epic count, completed epic count, average completion % across active epics weighted by adjusted total SP (BR spec acceptance criteria). When all active epics have null adjusted total SP, average = 0.

9. **Unlinked work (BR14):** Count and total SP of tickets without an epic key that appear in at least one sprint membership.

10. **Division by zero (BR19):** Adjusted total SP = 0 produces SP completion % = 0. Velocity = 0 or null produces projection = null.

11. **Sorting (BR12):** The service returns two separate lists or the endpoint/frontend filters. Active epics (isCompleted = false) are sorted by SP completion percentage ascending (least-done first); when SP completion is null, sort by ticket completion percentage ascending. Completed epics (isCompleted = true) are sorted by epic name alphabetically ascending. The service should return ALL epics in one list and let the frontend filter by active/completed — but the service must sort the list: active epics first (by SP completion % asc), then completed epics (by name asc).

**Pattern to follow:** `SprintSummaryService.cs` for service structure with response records in same file. `BugRatioService.cs` for sub-team filtering pattern.

**Follow:** extract-feature-service

**Dependencies:** Step 1 (repository query methods)

### Step 3: Create GetEpicProgress endpoint

**What:** Create the FastEndpoints GET endpoint with request/validator.

**Files to create:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetEpicProgress/GetEpicProgressQuery.cs` — request class + validator
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetEpicProgress/GetEpicProgressEndpoint.cs` — endpoint class

**Files to modify:**
- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` — register `EpicProgressService` as scoped

**Request class `GetEpicProgressRequest`:**
- `SubTeam` (string?, optional)

**Validator `GetEpicProgressRequestValidator`:**
- SubTeam not empty when provided

**Endpoint route:** `GET /api/analytics/epic-progress`, Tags: "Analytics", AllowAnonymous

**Endpoint `HandleAsync` orchestration:**
1. Normalize subTeam (empty string to null)
2. Load app settings via `appSettingsRepository.GetAsync(ct)`
3. Load all tickets with epic key via `ticketRepository.GetTicketsWithEpicAsync(ct)`
4. If no epic tickets, return empty response with zero summary metrics and empty lists
5. Load all closed sprint memberships via `sprintRepository.GetAllClosedSprintMembershipsAsync(ct)`
6. Load unlinked tickets via `ticketRepository.GetTicketsWithoutEpicInSprintsAsync(ct)`
7. Call `epicProgressService.ComputeEpicProgress(epicTickets, closedMemberships, unlinkedTickets, settings, subTeam)`
8. Return result

**Performance note:** This loads all tickets with epic keys and all closed sprint memberships. For v1 with SQLite and small datasets (hundreds of tickets, dozens of sprints), this is acceptable. If performance degrades, targeted queries can be added later.

**Pattern to follow:** `GetScopeChangeEndpoint.cs` for endpoint structure, `GetSprintSummaryQuery.cs` for validator pattern.

**Follow:** create-feature (FastEndpoints variant)

**Dependencies:** Step 2 (EpicProgressService)

### Step 4: Align F8 Top Epics with cumulative ticket-status-based completion

**What:** Update `SprintSummaryService.ComputeTopEpics()` to compute epic completion using cumulative ticket status (all tickets with that epic key) instead of sprint-membership-only data. This ensures the Dashboard's "Top 3 Epics" completion percentages match the Epics page (BR20).

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs` — modify `ComputeTopEpics()` method
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetSprintSummary/GetSprintSummaryEndpoint.cs` — add TicketRepository injection, load epic tickets, pass to ComputeTopEpics

**Current behavior (lines 378-423 of SprintSummaryService.cs):**
- Groups selected sprint memberships by epic key to find top 3 by SP completed this sprint
- Computes overall progress from sprint memberships across all loaded sprints (membership-based scope)

**New behavior:**
- Step 1 (top 3 selection) stays the same: group selected sprint memberships by epic key, sort by SP completed this sprint, take 3
- Step 2 (completion computation) changes: for each of the top 3 epics, compute completion from ALL tickets with that epic key (using ticket current status against done statuses), not from sprint memberships
- `ComputeTopEpics` signature changes to accept an additional `List<Ticket> allEpicTickets` parameter
- The `EpicProgress` record (line 40-45) stays unchanged — its fields (`totalSp`, `doneSp`, `completionPercentage`) now reflect cumulative ticket-status-based values

**GetSprintSummaryEndpoint changes:**
- Inject `TicketRepository` via primary constructor
- After loading window sprints, load all epic tickets: `await ticketRepository.GetTicketsWithEpicAsync(ct)`
- Apply sub-team filtering to the ticket list (same pattern: filter by `Assignee?.SubTeam`)
- Pass filtered epic tickets to `ComputeTopEpics`

**No skill — logic modification to an existing computation. Inline detail required.**

**Dependencies:** Step 1 (repository query method `GetTicketsWithEpicAsync`)

### Step 5: Add frontend TypeScript types

**What:** Add all response type interfaces for the epic progress API to the shared types file.

**Files to modify:**
- `client/src/types/index.ts` — add Epic Progress section after Bug Ratio types

**Types to add (matching the backend response records from Step 2):**
- `EpicProgressSummaryMetrics`
- `EpicProgressTicketEntry`
- `EpicProgressEntry`
- `EpicProgressUnlinkedWork`
- `EpicProgressResponse`

**Follow:** create-vue-feature (step 1: define types)

**Dependencies:** None (can be done in parallel with backend steps)

### Step 6: Add epic progress API function

**What:** Add the `getEpicProgress` API function alongside existing analytics functions.

**Files to modify:**
- `client/src/api/analytics.ts` — add `getEpicProgress(subTeam?)` function, import `EpicProgressResponse` type

**Pattern to follow:** `getSprintSummary` function in the same file (single optional query param pattern — no sprintId or last params since epic progress is cumulative across all time, BR18).

**Follow:** create-vue-feature (step 2: API module)

**Dependencies:** Step 5 (types)

### Step 7: Create epicsStore

**What:** Create a new Pinia store for the Epics page. This store manages sub-team filtering and active/completed toggle state. No sprint selector (BR18).

**Files to create:**
- `client/src/stores/epicsStore.ts`

**Store state:**
- `subTeams` ref (`string[]`) — loaded on initialize
- `selectedSubTeam` ref (`string | null`, default null)
- `activeFilter` ref (`'active' | 'completed'`, default `'active'`)
- `epicProgress` ref (`EpicProgressResponse | null`)
- `expandedEpicKeys` ref (`Set<string>`) — tracks which epics are expanded in the table
- `loading` ref (boolean)
- `initializing` ref (boolean)
- `error` ref (string | null)

**Computed getters:**
- `filteredEpics` — filters `epicProgress.epics` by `activeFilter`: active = `!isCompleted`, completed = `isCompleted`
- `hasEpics` — boolean, any epics exist in the response

**Actions:**
- `initialize()` — load sub-teams and fetch epic progress in parallel
- `fetchEpicProgress()` — calls `getEpicProgress(selectedSubTeam)`, sets epicProgress
- `selectSubTeam(subTeam)` — sets selectedSubTeam, re-fetches
- `setActiveFilter(filter)` — sets activeFilter (no re-fetch, client-side filter)
- `toggleEpicExpanded(epicKey)` — adds/removes from expandedEpicKeys set
- `isEpicExpanded(epicKey)` — checks membership in expandedEpicKeys

**Follow:** pinia-patterns (setup store, initialize pattern, async action pattern)

**Dependencies:** Steps 5-6 (types and API function)

### Step 8: Build EpicsView replacing placeholder

**What:** Replace the placeholder EpicsView with the full implementation. The view is the L0 page component that wires the store to the page shell and delegates rendering to child components.

**Files to modify:**
- `client/src/views/EpicsView.vue` — full rewrite (currently a placeholder with EmptyState)
- `client/src/components/PageToolbar.vue` — add `showSprintSelector` prop (default `true`), wrap sprint selector div in `v-if`

**View structure:**
- Uses `PageToolbar` with sub-team filter only (no sprint selector — `showAggregateOptions` false, sprints prop empty/omitted so sprint dropdown is hidden)
- Three-state template: initializing -> empty -> content
- Empty state when `!store.hasEpics`: "No epic data yet - sync a sprint to see epic progress here" (matches current placeholder)
- Content section:
  - Summary cards row (3 cards — delegate to `EpicSummaryCards` component)
  - Active/Completed toggle buttons
  - Epic table (delegate to `EpicTable` component)
- URL sync: `subTeam` query parameter, `filter` query parameter for active/completed

**PageToolbar configuration:**
- `:show-sub-team-filter="true"`
- Add a `showSprintSelector` boolean prop to `PageToolbar.vue` (default `true` for backward compatibility). When `false`, hide the entire sprint selector `div`. The Epics page passes `:show-sprint-selector="false"`. This is cleaner than passing an empty sprints array (which shows a misleading "No sprints" label).
- This is a minor modification to `client/src/components/PageToolbar.vue` — add the prop and wrap the sprint selector div in `v-if="showSprintSelector"`.

**Follow:** vue-component-architecture (L0 view), create-vue-feature (page shell pattern)

**Dependencies:** Step 7 (epicsStore)

### Step 9: Create epic progress UI components

**What:** Create the visual components for the Epics page content.

**Files to create:**
- `client/src/components/epics/EpicSummaryCards.vue` — L3 props-only component. Displays 3 summary cards using BaseCard: Active Epics (count), Average Completion (percentage with progress bar), Unlinked Work (ticket count and SP total). Props: `summaryMetrics: EpicProgressSummaryMetrics`, `unlinkedWork: EpicProgressUnlinkedWork`, `activeFilter: 'active' | 'completed'`. When completed filter is active, first card shows completed count instead.
- `client/src/components/epics/EpicTable.vue` — L1 container component. Renders the epic table with one row per epic. Each row shows: epic name, progress bar (actual SP as filled segment, imputed SP as distinct hatched/striped segment), SP done/total with imputed label (e.g., "+~12 SP est."), tickets done/total, velocity, projected remaining, active sprint count. Handles expand/collapse via store's `toggleEpicExpanded`/`isEpicExpanded`. When expanded, renders `EpicTicketTable` below the row. Props: `epics: EpicProgressEntry[]`, `expandedEpicKeys: Set<string>`. Emits: `toggle-expand(epicKey)`.
  - Progress bar: actual SP completion as filled accent-colored bar, imputed SP as a visually distinct segment (use lighter/striped pattern or different opacity). When SP metrics unavailable (null), show ticket-count percentage instead.
  - Velocity: display value or "—" when null
  - Projection: display value or "Insufficient data" when null. Add "Low confidence" badge when `projectionConfidence === 'low'`
  - SP display: "— " when SP metrics are null (zero estimated tickets)
  - Imputed SP label: show "+~{imputedSp} SP est." next to SP totals when imputed SP is non-null and > 0
- `client/src/components/epics/EpicTicketTable.vue` — L3 props-only component. Renders the expanded ticket detail table. Columns: ticket key, summary, issue type, story points (or "—"), current status, assignee (or "Unassigned" — BR23). Props: `tickets: EpicProgressTicketEntry[]`. Tickets are pre-sorted by the backend (remaining first, then done).
- `client/src/components/epics/EpicActiveCompletedToggle.vue` — L4 UI component. Two toggle buttons: Active (default), Completed. Props: `activeFilter: 'active' | 'completed'`. Emits: `update:activeFilter`. Style: match the tab button pattern from DevelopersView (active gets accent border/text, inactive gets muted).

**Empty states within the table area:**
- Active view, all epics completed: "All epics are complete" message with prompt to switch to Completed view
- Completed view, no completed epics: "No completed epics yet" message

**Help tooltips:** Wire tooltip text from `docs/features/EpicProgress/help.tooltips.md` to info icons on: each summary card title, progress bar, SP columns, velocity, projected remaining, active sprint count, ticket row, active/completed toggle, sub-team filter. Use `title` attribute or a tooltip component if one exists.

**Follow:** vue-patterns, vue-component-architecture

**Dependencies:** Step 8 (EpicsView)

### Step 10: Build verification and manual testing

**What:** Verify the full build passes and test the feature end-to-end.

**Backend:**
```
dotnet build src/Services/Fokus/Fokus.API
```

**Frontend:**
```
cd client && npm run build
```

**Manual verification:**
- Start the dev server
- Navigate to Epics page — verify it replaces the placeholder
- If no epic data: verify empty state appears
- If epic data exists: verify summary cards, epic table, progress bars
- Expand an epic row — verify ticket table appears with correct columns
- Collapse — verify it hides
- Toggle Active/Completed — verify filtering works
- Select sub-team filter — verify all content recalculates
- Check velocity and projection values for correctness
- Verify "—" displays for epics with no estimated tickets
- Verify "Insufficient data" / "Low confidence" labels
- Verify Dashboard Top Epics percentages match Epics page (F8 alignment)
- Check help tooltips appear on hover

**Dependencies:** All previous steps

## Cross-Service Changes

None. Fokus is a single-service system with no cross-service dependencies in v1.

## Migration Notes

No migrations needed. This feature queries existing data only.

## Testing Strategy

**Backend:**
- GET /api/analytics/epic-progress returns epic progress data for all epics
- GET with `subTeam` parameter filters to that sub-team
- Response includes summary metrics, epic list with tickets, and unlinked work
- Epic with mix of estimated and unestimated tickets: verify imputed SP calculation
- Epic with zero estimated tickets: SP metrics are null, ticket-count progress shown
- Epic with all tickets done: isCompleted = true, appears in completed filter
- Velocity computation: verify rolling 3-sprint average, zero-progress sprints skipped
- Projection: verify (remaining SP + imputed SP) / velocity
- Projection confidence "low" when fewer than 3 data points
- Division by zero: adjusted total SP = 0 gives SP completion % = 0; velocity = 0 gives null projection
- Sub-team filter: verify all metrics scoped to filtered sub-team
- Sub-team filter: epics with zero tickets for sub-team excluded
- Unlinked work: only tickets in sprint memberships counted
- Unlinked work: sub-team filter applies
- Average completion weighted by adjusted total SP
- Empty response when no tickets with epic keys exist
- F8 alignment: Dashboard Top Epics completion % matches Epics page for same epic
- Ticket sorting: remaining first, then done

**Frontend:**
- Epics page loads and shows summary cards
- Active/Completed toggle filters epics client-side
- Expanding epic row shows ticket table
- Collapsing hides ticket table
- Sub-team filter re-fetches data
- Progress bar shows actual + imputed segments
- Velocity shows "—" when unavailable
- Projection shows "Insufficient data" when unavailable
- "Low confidence" badge when fewer than 3 data points
- Unestimated tickets show "—" in SP column
- Unassigned tickets show "Unassigned"
- Empty states render correctly for all three cases
- Help tooltips appear on info icon hover
- URL sync works for subTeam and filter params

## Open Questions

None.
