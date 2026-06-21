# Developer Throughput

**Feature Spec:** `docs/features/DeveloperThroughput/spec.md`

## Context

The Developers page is currently an empty state stub. This feature fills it with per-developer sprint analytics: a throughput table showing SP assigned, SP completed, completion %, tickets done, tickets carried over -- with delta indicators for single-sprint view -- plus a multi-line trend chart showing 3-sprint rolling averages across sprints. It also introduces a new "Developer Sprint Capacity" concept (per-developer per-sprint percentage 0-100, default 100) that modulates rolling average calculations and is editable inline.

This is the second analytics feature (F9) after Sprint Summary Card (F8). It reuses the established patterns: sub-team filtering (C2), done-status-aware metric computation, and the PageToolbar sprint/sub-team selectors. It extends the toolbar with aggregate sprint options ("Last 3", "Last 5", "All").

**Service impacted:** Fokus (single service). Backend: new domain entity + migration, new analytics endpoint, two capacity CRUD endpoints, a throughput computation service. Frontend: full Developers page replacement with table + chart, capacity editing, extended toolbar.

## Scope

**In scope:**
- New `DeveloperSprintCapacity` entity (domain + persistence + migration)
- `GET /api/analytics/developer-throughput` endpoint with `sprintId`, `last`, and `subTeam` query params
- `PUT /api/developers/{accountId}/capacity` endpoint for setting capacity
- `GET /api/developers/{accountId}/capacity` endpoint for reading capacity
- `DeveloperThroughputService` -- focused operation service for throughput computation
- Per-developer metrics: SP assigned, SP completed, completion %, tickets done, tickets carried over
- Delta indicators (single-sprint mode only) with polarity-aware coloring
- 3-sprint rolling average (capacity-aware: excludes 0% capacity sprints)
- Sub-team filtering
- Frontend: throughput table with all metrics and deltas
- Frontend: multi-line trend chart (rolling average SP completed per developer across sprints)
- Frontend: inline capacity editing per developer per sprint
- Frontend: extended PageToolbar with aggregate sprint options ("Last 3", "Last 5", "All")
- Frontend: Pinia store for developer throughput state management

**Out of scope (per spec):**
- CSV/PDF export
- Developer detail drill-down page
- Comparison mode
- Configurable rolling average window (fixed at 3)
- Velocity forecasting
- Real-time updates via SignalR

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | domain-patterns | Follow | New DeveloperSprintCapacity entity, composite key (DeveloperAccountId + SprintId), no behavior | |
| 2 | persistence-patterns | Follow | EF config for DeveloperSprintCapacity, migration, DbSet | |
| 3 | persistence-patterns | Follow | New query methods on DeveloperRepository for capacity CRUD and bulk loading | |
| 4 | extract-feature-service | Follow | DeveloperThroughputService: per-developer metrics, deltas, rolling averages | |
| 5 | create-feature | Follow | FastEndpoints GET endpoint for developer throughput analytics | |
| 6 | create-feature | Follow | FastEndpoints PUT endpoint for setting developer sprint capacity | |
| 7 | create-feature | Follow | FastEndpoints GET endpoint for reading developer sprint capacity | |
| 8 | (none) | -- | Frontend TypeScript types for throughput response + capacity types | Frontend patterns gap |
| 9 | (none) | -- | Frontend API module for developer throughput and capacity endpoints | Frontend patterns gap |
| 10 | (none) | -- | Pinia store for developers page: sprint mode, sub-team filter, throughput data, capacity editing | Frontend patterns gap |
| 11 | (none) | -- | PageToolbar extension: aggregate sprint options (Last 3, Last 5, All) | Frontend patterns gap |
| 12 | (none) | -- | DevelopersView: throughput table + trend chart + capacity editing | Frontend patterns gap |

## Domain Model Changes

**New entity: `DeveloperSprintCapacity`**
- `DeveloperAccountId` (string, FK to Developer.Id) -- part of composite PK
- `SprintId` (int, FK to Sprint.Id) -- part of composite PK
- `CapacityPercent` (int, 0-100, default 100)
- Navigation properties: `Developer`, `Sprint`
- No behavior methods -- plain data container consistent with v1 thin domain pattern
- Location: `src/Services/Fokus/Fokus.Domain/Developer/DeveloperSprintCapacity.cs`

This is NOT an aggregate -- it has no identity of its own beyond the composite key. It is a standalone entity like `SprintMembership`.

## Data Model Changes

**New table: `DeveloperSprintCapacities`**
- Composite primary key: (`DeveloperAccountId`, `SprintId`)
- `DeveloperAccountId` string(128), FK to `Developers.Id`, cascade delete
- `SprintId` int, FK to `Sprints.Id`, cascade delete
- `CapacityPercent` int, not null, default 100
- Index on `SprintId` for efficient per-sprint capacity lookups

**Migration required:** `AddDeveloperSprintCapacity`

## Implementation Steps

### Step 1: Create DeveloperSprintCapacity domain entity

**What:** Add a new entity representing a developer's availability percentage for a specific sprint. This is a plain data entity with a composite key, following the same pattern as `SprintMembership`.

**Files to create:**
- `src/Services/Fokus/Fokus.Domain/Developer/DeveloperSprintCapacity.cs`

**Entity shape:**
- Composite identity: `DeveloperAccountId` (string) + `SprintId` (int) -- no separate Id property
- `CapacityPercent` (int, range 0-100)
- Navigation properties: `Developer` and `Sprint` (for EF joins)
- No base class (same as `SprintMembership` -- composite key entities don't extend `Entity<T>`)

**Skill:** Follow `domain-patterns` (entity definition, no behavior split needed for data-only entity).

**Pattern reference:** `src/Services/Fokus/Fokus.Domain/Sprint/SprintMembership.cs` (composite key entity, navigation properties, no base class).

**Dependencies:** None.

---

### Step 2: Add EF Core configuration, DbSet, and migration for DeveloperSprintCapacity

**What:** Configure the new entity in EF Core with composite primary key, foreign keys, column constraints, and an index. Add the DbSet to the DbContext. Generate and apply the migration.

**Files to create:**
- `src/Services/Fokus/Fokus.Persistence/Configurations/DeveloperSprintCapacityConfiguration.cs`

**Files to modify:**
- `src/Services/Fokus/Fokus.Persistence/FokusDbContext.cs` -- add `DbSet<DeveloperSprintCapacity> DeveloperSprintCapacities`

**Configuration details:**
- Composite key: `HasKey(c => new { c.DeveloperAccountId, c.SprintId })`
- `DeveloperAccountId`: `HasMaxLength(128)` (matches Developer.Id config)
- `CapacityPercent`: required, no max length needed (int)
- FK to Developer: `HasOne(c => c.Developer).WithMany().HasForeignKey(c => c.DeveloperAccountId).OnDelete(DeleteBehavior.Cascade)`
- FK to Sprint: `HasOne(c => c.Sprint).WithMany().HasForeignKey(c => c.SprintId).OnDelete(DeleteBehavior.Cascade)`
- Index: `HasIndex(c => c.SprintId).HasDatabaseName("IX_DeveloperSprintCapacity_SprintId")`

**Migration command:**
```
dotnet ef migrations add AddDeveloperSprintCapacity -p Services/Fokus/Fokus.Persistence -s Services/Fokus/Fokus.API
```

**Skill:** Follow `persistence-patterns` (entity configuration with composite key, DbSet registration).

**Pattern reference:** `src/Services/Fokus/Fokus.Persistence/Configurations/SprintMembershipConfiguration.cs` (composite key, two FKs, index naming).

**Dependencies:** Step 1.

---

### Step 3: Add capacity CRUD and bulk-load methods to DeveloperRepository

**What:** Add repository methods for creating/updating/reading developer sprint capacities, plus a bulk load method for the throughput computation. The throughput endpoint needs all capacity records for a set of sprints in one query (not N+1).

**New methods on `DeveloperRepository`:**

1. `GetCapacityAsync(string accountId, int sprintId)` -- returns single `DeveloperSprintCapacity` or null.

2. `GetCapacitiesForDeveloperAsync(string accountId, int? sprintId)` -- returns list of capacity entries for a developer. If `sprintId` provided, filters to that sprint. For the GET capacity endpoint.

3. `UpsertCapacityAsync(string accountId, int sprintId, int capacityPercent)` -- creates or updates a capacity record. Uses the DbContext's `DeveloperSprintCapacities` DbSet directly (same pattern as `UpsertMembershipsAsync` accessing `DbContext.SprintMemberships`).

4. `GetCapacitiesForSprintsAsync(List<int> sprintIds)` -- bulk loads all capacity records for the given sprint IDs. Returns `List<DeveloperSprintCapacity>`. Used by the throughput computation to avoid N+1 per-developer per-sprint queries.

**Performance approach:** Method 4 is the critical one -- single query with `Where(c => sprintIds.Contains(c.SprintId))`. Max ~20 sprint IDs for "All" mode, well within SQLite limits.

**Files to modify:**
- `src/Services/Fokus/Fokus.Persistence/Repositories/DeveloperRepository.cs`

**Skill:** Follow `persistence-patterns` (repository query methods).

**Pattern reference:** `SprintRepository.UpsertMembershipsAsync` (accessing a related DbSet via `DbContext`), `DeveloperRepository.GetActiveDevelopersAsync` (simple query method).

**Dependencies:** Step 2 (DbSet must exist).

---

### Step 4: Create DeveloperThroughputService

**What:** Create a focused operation service that takes sprint data, developer data, capacity data, and app settings, and computes the full developer throughput response. This is a pure computation service with no database access -- it receives all data as method parameters. Follows the same pattern as `SprintSummaryService`.

**Location:** `src/Services/Fokus/Fokus.API/Features/Analytics/DeveloperThroughputService.cs`

**Service method signature (conceptual):**
- Input: list of closed sprints with memberships (including ticket + assignee navigations), active developers list, capacity records for all relevant sprints, app settings (for done statuses), optional sub-team filter, whether this is single-sprint mode (for deltas)
- Output: the full developer throughput response object

**Computation responsibilities:**

1. **Sub-team filtering (C2 pattern).** Reuse the established pattern from `SprintSummaryService.FilterDevelopers` and `FilterMemberships`. Filter active developers by sub-team. Filter memberships by assignee sub-team.

2. **Per-developer per-sprint metrics.** For each active developer, for each sprint in the range:
   - **SP assigned:** Sum of `StoryPoints` on non-removed memberships where `Ticket.AssigneeId == developer.Id` and `StoryPoints != null`. (BR1: excludes removed tickets)
   - **SP completed:** Sum of `StoryPoints` on non-removed memberships where assignee matches, `FinalStatus` in done statuses, and `StoryPoints != null`. (BR2)
   - **Completion %:** SP completed / SP assigned * 100, or 0 if SP assigned is 0. (BR spec line 99)
   - **Tickets done:** Count of non-removed memberships where assignee matches and `FinalStatus` in done statuses. Includes tickets with null story points. (BR11)
   - **Tickets carried over:** Count of non-removed memberships where assignee matches and `FinalStatus` NOT in done statuses. Includes tickets with null story points. (BR11)
   - **Capacity %:** Look up from capacity records, default 100 if no record exists. (BR9)

3. **Rolling average (capacity-aware).** For each developer at each sprint position:
   - Look backward from the current sprint and collect up to 3 sprints where the developer's capacity > 0%.
   - Average the SP completed values from those qualifying sprints.
   - If fewer than 3 qualifying sprints are available, return null. (BR8)
   - This requires sprint data beyond the requested range to look backward -- the endpoint must load extra sprints for the rolling average window.

4. **Delta computation (single-sprint only, BR6).** When exactly one sprint is selected:
   - Identify the prior closed sprint (next-earlier start date).
   - Delta = current metric - prior sprint metric for each metric.
   - Delta direction: "up" if positive, "down" if negative, "flat" if zero.
   - Delta polarity per metric (BR7):
     - SP assigned: neutral (no color)
     - SP completed: positive-up (higher is green)
     - Completion %: positive-up
     - Tickets done: positive-up
     - Tickets carried over: positive-down (lower is green)

5. **Zero-ticket developers visible (BR4).** Active developers with no memberships in a sprint appear with all-zero metrics. Never hidden.

**DI registration:** Add `services.AddScoped<DeveloperThroughputService>();` in `src/Services/Fokus/Fokus.API/DependencyInjection.cs`.

**Response record hierarchy (defined in the service file):**

```
DeveloperThroughputResponse
  List<SprintSummaryItem> Sprints  -- ordered list of sprint summaries
  List<DeveloperThroughputEntry> Developers

SprintSummaryItem { int Id, string Name, DateTime StartDate, DateTime EndDate }

DeveloperThroughputEntry
  string DisplayName
  string? SubTeam
  string? AvatarUrl
  List<SprintBreakdown> SprintBreakdowns

SprintBreakdown
  int SprintId
  decimal SpAssigned
  decimal SpCompleted
  decimal CompletionPercent
  int TicketsDone
  int TicketsCarriedOver
  int CapacityPercent
  decimal? RollingAverageSpCompleted
  -- Delta fields (nullable, only present in single-sprint mode):
  decimal? SpAssignedDelta
  decimal? SpCompletedDelta
  decimal? CompletionPercentDelta
  int? TicketsDoneDelta
  int? TicketsCarriedOverDelta
  string? SpAssignedDeltaDirection       -- "up"/"down"/"flat"
  string? SpCompletedDeltaDirection
  string? CompletionPercentDeltaDirection
  string? TicketsDoneDeltaDirection
  string? TicketsCarriedOverDeltaDirection
  string? SpAssignedDeltaPolarity        -- always "neutral"
  string? SpCompletedDeltaPolarity       -- "positive"/"negative"/"neutral"
  string? CompletionPercentDeltaPolarity
  string? TicketsDoneDeltaPolarity
  string? TicketsCarriedOverDeltaPolarity -- inverted: down is positive
```

**Files to create:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/DeveloperThroughputService.cs` -- service class + all response/DTO records

**Files to modify:**
- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` -- add `services.AddScoped<DeveloperThroughputService>();`

**Skill:** Follow `extract-feature-service` (focused operation service pattern).

**Pattern reference:** `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs` (pure computation service, record hierarchy in same file, sub-team filtering).

**Dependencies:** None (consumes data passed by the endpoint in Step 5).

---

### Step 5: Create GetDeveloperThroughput endpoint

**What:** Create the `GET /api/analytics/developer-throughput` endpoint. It accepts optional `sprintId` (int), `last` (int), and `subTeam` (string) query parameters. It orchestrates data loading from repositories and delegates computation to `DeveloperThroughputService`.

**Files to create:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetDeveloperThroughput/GetDeveloperThroughputEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetDeveloperThroughput/GetDeveloperThroughputQuery.cs`

**Endpoint details:**
- Extends `Endpoint<GetDeveloperThroughputRequest, DeveloperThroughputResponse>`
- `[AllowAnonymous]`, `[HttpGet("/api/analytics/developer-throughput")]`, `[Tags("Analytics")]`
- Constructor dependencies: `SprintRepository`, `DeveloperRepository`, `AppSettingsRepository`, `DeveloperThroughputService`

**Handler logic:**
1. Validate mutual exclusivity: if both `sprintId` and `last` are provided, return 400.
2. Load all closed sprints (lightweight, ordered by start date ascending).
3. If no closed sprints exist, return 200 with empty results.
4. Determine target sprint IDs based on mode:
   - `sprintId` provided: validate it exists in closed sprints (400 if not). Target = that single sprint. Also identify the prior sprint for delta computation.
   - `last` provided: validate >= 1 (400 if not). Take last N from the ascending-sorted closed sprints.
   - Neither provided: all closed sprints.
5. **Rolling average window expansion:** For each developer's rolling average, we need up to 2 additional sprints before the earliest target sprint (to look back for 3 qualifying data points). Load extra sprint IDs from the full closed sprints list as needed.
6. Bulk load sprints with memberships for (target IDs + extra window IDs) via `GetSprintsWithMembershipsAsync`.
7. Load active developers via `GetActiveDevelopersAsync`.
8. Bulk load capacity records for all loaded sprint IDs via `GetCapacitiesForSprintsAsync`.
9. Load app settings via `GetAsync`.
10. Call `DeveloperThroughputService.ComputeThroughput(...)`.
11. Return 200 with the result (only target sprints in the response, extra sprints used only for rolling average lookback).

**Request type:**
```
GetDeveloperThroughputRequest { int? SprintId, int? Last, string? SubTeam }
```

**Validator:**
- `SprintId` must be > 0 when provided.
- `Last` must be >= 1 when provided.
- Custom rule: `SprintId` and `Last` must not both be provided (400).
- `SubTeam` must be non-empty when provided.

**Performance approach:** The dataset is small (SQLite, single team, max ~20 sprints). The bulk membership load is the heaviest query -- one query with `Where(s => sprintIds.Contains(s.Id))` including ticket + assignee navigations. Capacity records loaded in a single separate query. No N+1.

**Error reporting shape:**
- 400 with validation errors for bad input (both params, invalid sprint, last < 1)
- 200 with empty developers list and empty sprints list when no data exists

**Skill:** Follow `create-feature` (FastEndpoints variant, `Endpoint<TReq, TRes>` with validator).

**Pattern reference:** `src/Services/Fokus/Fokus.API/Features/Analytics/GetSprintSummary/GetSprintSummaryEndpoint.cs` (analytics endpoint, repository orchestration, service delegation).

**Dependencies:** Steps 1-4.

---

### Step 6: Create SetDeveloperCapacity endpoint

**What:** Create the `PUT /api/developers/{accountId}/capacity` endpoint for setting a developer's capacity for a specific sprint.

**Files to create:**
- `src/Services/Fokus/Fokus.API/Features/Developers/SetCapacity/SetCapacityEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Developers/SetCapacity/SetCapacityCommand.cs`

**Endpoint details:**
- Extends `Endpoint<SetCapacityRequest, SetCapacityResponse>`
- `[AllowAnonymous]`, `[HttpPut("/api/developers/{accountId}/capacity")]`, `[Tags("Developers")]`
- Constructor dependencies: `DeveloperRepository`

**Handler logic:**
1. Extract `accountId` from route.
2. Look up the developer by `accountId` -- return 404 if not found.
3. Upsert the capacity record via `UpsertCapacityAsync(accountId, request.SprintId, request.CapacityPercent)`.
4. Save changes.
5. Return 200 with the updated capacity.

**Request type:**
```
SetCapacityRequest { string AccountId (from route), int SprintId, int CapacityPercent }
```

**Validator:**
- `SprintId` is required and > 0.
- `CapacityPercent` must be in range 0-100.

**Response type:**
```
SetCapacityResponse { string DeveloperAccountId, int SprintId, int CapacityPercent }
```

**Skill:** Follow `create-feature` (FastEndpoints variant with route param binding).

**Pattern reference:** `src/Services/Fokus/Fokus.API/Features/Settings/SaveSettings/SaveSettingsEndpoint.cs` (PUT endpoint with validation).

**Dependencies:** Step 3 (repository method).

---

### Step 7: Create GetDeveloperCapacity endpoint

**What:** Create the `GET /api/developers/{accountId}/capacity` endpoint for reading a developer's capacity entries.

**Files to create:**
- `src/Services/Fokus/Fokus.API/Features/Developers/GetCapacity/GetCapacityEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Developers/GetCapacity/GetCapacityQuery.cs`

**Endpoint details:**
- Extends `Endpoint<GetCapacityRequest, List<CapacityEntry>>`
- `[AllowAnonymous]`, `[HttpGet("/api/developers/{accountId}/capacity")]`, `[Tags("Developers")]`
- Constructor dependency: `DeveloperRepository`

**Handler logic:**
1. Extract `accountId` from route.
2. Look up the developer -- return 404 if not found.
3. Call `GetCapacitiesForDeveloperAsync(accountId, request.SprintId)`.
4. Map to response entries. Sprints with no explicit capacity record are NOT returned (consumer assumes 100% default).
5. Return 200.

**Request type:**
```
GetCapacityRequest { string AccountId (from route), int? SprintId (from query) }
```

**Response type:**
```
CapacityEntry { int SprintId, int CapacityPercent }
```

**Skill:** Follow `create-feature` (FastEndpoints variant with route + query param binding).

**Pattern reference:** `src/Services/Fokus/Fokus.API/Features/Developers/GetSubTeams/GetSubTeamsEndpoint.cs` (GET endpoint in Developers area).

**Dependencies:** Step 3 (repository method).

---

### Step 8: Add frontend TypeScript types for developer throughput and capacity

**What:** Add all TypeScript interfaces matching the API response shapes for the three new endpoints (throughput analytics, set capacity, get capacity).

**Files to modify:**
- `client/src/types/index.ts` -- add new interfaces at the end

**Types to add:**

```typescript
// Developer throughput response
DeveloperThroughputResponse { sprints: SprintSummaryItem[], developers: DeveloperThroughputEntry[] }
SprintSummaryItem { id: number, name: string, startDate: string, endDate: string }
DeveloperThroughputEntry { displayName: string, subTeam: string | null, avatarUrl: string | null, sprintBreakdowns: SprintBreakdown[] }
SprintBreakdown {
  sprintId: number, spAssigned: number, spCompleted: number, completionPercent: number,
  ticketsDone: number, ticketsCarriedOver: number, capacityPercent: number,
  rollingAverageSpCompleted: number | null,
  // Delta fields (nullable)
  spAssignedDelta: number | null, spCompletedDelta: number | null, completionPercentDelta: number | null,
  ticketsDoneDelta: number | null, ticketsCarriedOverDelta: number | null,
  spAssignedDeltaDirection: string | null, spCompletedDeltaDirection: string | null,
  completionPercentDeltaDirection: string | null, ticketsDoneDeltaDirection: string | null,
  ticketsCarriedOverDeltaDirection: string | null,
  spAssignedDeltaPolarity: string | null, spCompletedDeltaPolarity: string | null,
  completionPercentDeltaPolarity: string | null, ticketsDoneDeltaPolarity: string | null,
  ticketsCarriedOverDeltaPolarity: string | null
}

// Capacity endpoints
CapacityEntry { sprintId: number, capacityPercent: number }
SetCapacityRequest { sprintId: number, capacityPercent: number }
SetCapacityResponse { developerAccountId: string, sprintId: number, capacityPercent: number }
```

**Skill:** None (frontend patterns gap).

**Pattern reference:** Existing types in `client/src/types/index.ts` (interface naming, camelCase properties matching .NET JSON serialization).

**Dependencies:** None (types are API contract documentation).

---

### Step 9: Add frontend API functions for developer throughput and capacity

**What:** Add API functions for the three new endpoints to the existing analytics API module and a new developers API module.

**Files to modify:**
- `client/src/api/analytics.ts` -- add `getDeveloperThroughput` function

**Files to create:**
- `client/src/api/developers.ts` -- new API module for developer-specific endpoints (capacity CRUD)

**Functions to add:**

In `analytics.ts`:
- `getDeveloperThroughput(sprintId?: number, last?: number, subTeam?: string): Promise<DeveloperThroughputResponse>` -- calls `GET /analytics/developer-throughput` with optional query params

In `developers.ts`:
- `setDeveloperCapacity(accountId: string, sprintId: number, capacityPercent: number): Promise<SetCapacityResponse>` -- calls `PUT /developers/{accountId}/capacity`
- `getDeveloperCapacity(accountId: string, sprintId?: number): Promise<CapacityEntry[]>` -- calls `GET /developers/{accountId}/capacity`

All functions use the existing `apiFetch` from `client/src/api/client.ts`.

**Skill:** None (frontend patterns gap).

**Pattern reference:** `client/src/api/analytics.ts` (existing analytics API module), `client/src/api/settings.ts` (PUT request pattern with JSON body).

**Dependencies:** Step 8 (types must exist for return type annotations).

---

### Step 10: Create developers Pinia store

**What:** Create a Pinia store for the Developers page that manages sprint selection mode (single sprint vs. multi-sprint), sub-team filtering, throughput data fetching, and capacity editing.

**Files to create:**
- `client/src/stores/developersStore.ts`

**Store state:**
- `closedSprints: ClosedSprintItem[]` -- list of closed sprints for the selector
- `subTeams: string[]` -- list of sub-team names for the filter
- `selectedSprintId: number | null` -- selected sprint in single-sprint mode
- `selectedLast: number | null` -- selected "Last N" value (3, 5, or null for all)
- `sprintMode: 'single' | 'multi'` -- tracks which mode the UI is in
- `selectedSubTeam: string | null` -- sub-team filter
- `throughput: DeveloperThroughputResponse | null` -- the current throughput data
- `loading: boolean`
- `initializing: boolean`
- `error: string | null`

**Store actions:**
- `initialize()` -- fetches closed sprints and sub-teams in parallel. Sets default to most recent closed sprint (single-sprint mode). Fetches throughput. Called once on Developers page mount.
- `selectSprint(sprintId: number)` -- switches to single-sprint mode, fetches throughput with `sprintId`.
- `selectLastN(n: number | null)` -- switches to multi-sprint mode. `n=3` for Last 3, `n=5` for Last 5, `n=null` for All. Fetches throughput with `last` param (or no sprint param for All).
- `selectSubTeam(subTeam: string | null)` -- updates filter, re-fetches.
- `fetchThroughput()` -- internal action that calls `getDeveloperThroughput(...)` based on current mode.
- `updateCapacity(accountId: string, sprintId: number, capacityPercent: number)` -- calls `setDeveloperCapacity(...)`, then re-fetches throughput to update rolling averages. Optimistic UI: updates the local capacity value immediately, reverts on error.

**Skill:** None (frontend patterns gap).

**Pattern reference:** `client/src/stores/dashboardStore.ts` (Pinia store with initialize, loading, error pattern).

**Dependencies:** Steps 8 and 9 (types and API functions).

---

### Step 11: Extend PageToolbar with aggregate sprint options

**What:** Add "Last 3", "Last 5", and "All" options to the sprint selector in PageToolbar. These aggregate options sit below the individual sprint options in the dropdown, separated by an optgroup or visual divider. The Developers page enables them via the existing `showAggregateOptions` prop (currently always false).

**Files to modify:**
- `client/src/components/PageToolbar.vue`

**Changes:**
- When `showAggregateOptions` is true, add a divider and three additional `<option>` elements after the individual sprint options: "Last 3 Sprints" (value: `last-3`), "Last 5 Sprints" (value: `last-5`), "All Sprints" (value: `all`).
- Update `onSprintChange` to detect these special values and emit a new event `update:sprintMode` with the appropriate payload: `{ mode: 'single', sprintId: number }` or `{ mode: 'multi', last: number | null }`.
- Add new props: `sprintMode: 'single' | 'multi'` (default 'single'), `selectedLast: number | null` (default null).
- Update the `<select>` value binding to reflect the current mode: when mode is 'multi', show the selected aggregate option as active.
- Emit: new event `update:sprintMode` alongside the existing `update:selectedSprintId`.

**Backward compatibility:** DashboardView uses `showAggregateOptions: false` (default) and only receives `update:selectedSprintId`. No changes needed to DashboardView.

**Skill:** None (frontend patterns gap).

**Pattern reference:** Current `client/src/components/PageToolbar.vue` (existing props, events, and select structure).

**Dependencies:** Step 8 (types for props).

---

### Step 12: Build DevelopersView with throughput table and trend chart

**What:** Replace the current empty-state-only DevelopersView with the full developer throughput display: a data table and a trend chart. This is the largest frontend step.

**Files to modify:**
- `client/src/views/DevelopersView.vue` -- complete rewrite

**DevelopersView structure:**

1. **Mount logic:** On mount, call `developersStore.initialize()`. Read route query params for initial state (e.g., `?sprint=123` or `?last=5`).

2. **Conditional rendering:**
   - If `initializing`: loading state.
   - If no closed sprints: existing `EmptyState` component.
   - Otherwise: throughput content.

3. **PageLayout** with title "Developers" and **PageToolbar** in the toolbar slot. Props:
   - Pass `showAggregateOptions: true` to enable multi-sprint options.
   - Wire sprint selector and sub-team filter to the store.

4. **Throughput table (single-sprint mode):**
   - Columns: Developer (avatar + name), Sub-Team, SP Assigned, SP Completed, Completion %, Tickets Done, Tickets Carried Over, Capacity %
   - Each metric column shows the value from the single sprint breakdown.
   - Delta indicators appear next to each metric value: up/down arrow icon with signed number. Colored by polarity (green for positive, red for negative, neutral gray for SP assigned).
   - Capacity column shows an inline editable number input (0-100). On change, calls `updateCapacity`.
   - Rows: one per active developer, sorted by display name.
   - Developers with zero tickets show all-zero values (BR4).

5. **Throughput table (multi-sprint mode):**
   - Columns: Developer (avatar + name), Sub-Team, then averaged values across all sprints in the range.
   - No delta indicators in multi-sprint mode (BR6).
   - Capacity column shows capacity for the most recent sprint only (informational).

6. **Trend chart (multi-sprint mode only):**
   - Appears below the table only when viewing multiple sprints.
   - Multi-line chart using `vue3-apexcharts` (line type).
   - X-axis: sprint names (ordered by start date ascending).
   - Y-axis: Rolling Average SP Completed.
   - One line per developer (each developer is a separate series).
   - Null rolling average values (fewer than 3 qualifying sprints) are gaps in the line.
   - Hover tooltip shows developer name, sprint name, and exact rolling average value.
   - Chart options: responsive, dark-theme aware (use the existing theme composable for colors).

7. **URL sync:** Update route query params on mode change. `?sprint=123` for single-sprint, `?last=3` or `?last=5` for last-N, no sprint params for All.

**Styling:**
- Reuse existing design tokens (`bg-surface-card`, `border-border-default`, `text-text-primary`, etc.)
- Delta colors: `text-status-success` for positive, `text-status-danger` for negative, `text-text-secondary` for neutral.
- Table uses `BaseCard` as container.
- Chart in a `BaseCard` with "SP Completed Trend (3-Sprint Rolling Avg)" title.

**Skill:** None (frontend patterns gap).

**Pattern reference:** `client/src/views/DashboardView.vue` (page layout, store integration, PageToolbar wiring, ApexCharts usage, URL sync pattern).

**Dependencies:** Steps 10 and 11 (store and toolbar extension).

---

## Cross-Service Changes

None. Single-service feature, read-only analytics + simple CRUD.

## Migration Notes

**Add migration:**
```
dotnet ef migrations add AddDeveloperSprintCapacity -p Services/Fokus/Fokus.Persistence -s Services/Fokus/Fokus.API
```

**Apply migration:**
```
dotnet ef database update -p Services/Fokus/Fokus.Persistence -s Services/Fokus/Fokus.API
```

No seed data needed -- capacity defaults to 100% when no record exists (handled in computation logic, not in the database).

## Testing Strategy

**Backend:**
1. **Throughput with single sprint.** Sync 2+ closed sprints. Call `GET /api/analytics/developer-throughput?sprintId={id}`. Verify per-developer metrics match expected values. Verify delta values present and correct vs prior sprint.
2. **Throughput with last N.** Call with `last=3`. Verify response includes exactly 3 sprints (or fewer if less exist). Verify no delta values. Verify per-sprint breakdowns per developer.
3. **Throughput with all sprints.** Call with no params. Verify all closed sprints included. Verify no deltas.
4. **Mutual exclusivity.** Call with both `sprintId` and `last`. Verify 400.
5. **Invalid sprint.** Call with `sprintId=99999`. Verify 400.
6. **Invalid last.** Call with `last=0`. Verify 400.
7. **Sub-team filter.** Call with `subTeam=Frontend`. Verify only matching developers appear.
8. **Zero-ticket developers.** Verify developers with no tickets in the sprint appear with all-zero metrics.
9. **Inactive developers excluded.** Verify inactive developers do not appear.
10. **Rolling average -- basic.** With 3+ sprints, verify rolling average is the mean of the 3 most recent qualifying sprints.
11. **Rolling average -- capacity 0%.** Set a developer's capacity to 0% for one sprint. Verify that sprint is excluded from the rolling average window.
12. **Rolling average -- insufficient data.** With fewer than 3 qualifying sprints, verify rolling average is null.
13. **Capacity CRUD.** PUT capacity for a developer/sprint. GET it back. Verify values match. PUT again with a different value. Verify update.
14. **Capacity 404.** PUT/GET capacity for a non-existent developer. Verify 404.
15. **Capacity validation.** PUT with `capacityPercent=150`. Verify 400. PUT with `capacityPercent=-1`. Verify 400.
16. **SP assigned excludes removed tickets.** Verify removed memberships don't count toward SP assigned.
17. **No-SP tickets.** Verify tickets with null story points count in ticket done/carried over but not in SP metrics.
18. **Delta polarity.** Verify SP assigned delta uses neutral polarity. Verify tickets carried over delta uses inverted polarity.

**Frontend:**
19. **Developers page loads.** Navigate to `/developers`. Verify sprint selector populated with individual sprints + aggregate options. Most recent sprint selected. Table displayed.
20. **Single-sprint table.** Verify all columns present with data. Verify delta indicators with correct coloring.
21. **Switch to multi-sprint.** Select "Last 3". Verify table shows averaged values, no deltas. Verify trend chart appears.
22. **Trend chart.** Verify one line per developer. Verify x-axis shows sprint names. Hover tooltip works.
23. **Capacity editing.** Change a developer's capacity. Verify immediate UI update. Verify rolling averages recalculate after re-fetch.
24. **Sub-team filter.** Select a sub-team. Verify table and chart filter to matching developers.
25. **Empty state.** With no closed sprints, verify empty state shows.
26. **URL sync.** Verify URL updates on sprint/mode change. Navigate directly to `?last=5` -- verify correct mode on load.

## Open Questions

None.
