# Scope Change & Disruption

**Feature Spec:** `docs/features/ScopeChangeDisruption/spec.md`

## Context

The Sprints page (`/sprints`) is currently an empty shell. This feature fills it with sprint-level scope change analytics: a multi-sprint trend view (default landing) showing disruption rates, scope change bars, and classification breakdowns across recent sprints, and a single-sprint detail view showing metric cards with deltas, a scope burnup chart, a chronological event table, and bug time-in-progress data.

This is the third analytics feature (F10) after Sprint Summary Card (F8) and Developer Throughput (F9). It reuses the established patterns: sub-team filtering (C2), done-status-aware metric computation, delta computation (C1), and the PageToolbar sprint/sub-team selectors with aggregate options. It also introduces a new system-wide setting (excluded-from-scope statuses) that affects all analytics features.

**Service impacted:** Fokus (single service). Backend: domain model extension (new property on AppSettings), migration, new analytics computation service, one analytics endpoint, two settings CRUD endpoints. Frontend: full Sprints page replacement with two view modes, charts, tables, and settings integration.

## Scope

**In scope:**
- New `ExcludedFromScopeStatuses` property on `AppSettings` entity + migration
- `GET /api/analytics/scope-change` endpoint with `sprintId`, `last`, and `subTeam` query params
- `GET /api/settings/excluded-statuses` endpoint returning the exclusion list
- `PUT /api/settings/excluded-statuses` endpoint replacing the exclusion list
- `ScopeChangeService` -- focused operation service for scope change computation
- Multi-sprint mode: summary metric cards (avg disruption rate, avg net scope change, total bugs added), stacked/grouped bar chart, disruption rate trend, classification breakdown table
- Single-sprint mode: metric cards with deltas (committed SP active/total, added SP, removed SP, net scope change, disruption rate, bug count), scope burnup chart, classification breakdown, event table, bug time-in-progress
- Automatic classification: planning overflow, unplanned bug, priority escalation, scope injection
- Sub-team filtering across all computations
- Excluded-from-scope statuses: case-insensitive matching, affects metric calculations, visible in event table as dimmed

**Out of scope (per spec):**
- Net-zero swap annotations
- Configurable grace period (fixed at 2 days)
- Manual disruption tagging
- Disruption alerts/notifications
- Sprint comparison mode
- Export (PDF/PNG)
- Real-time updates (SignalR)
- Carry-over sections (F11)
- Churn rate composite metric
- Per-developer disruption attribution

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | persistence-patterns | Follow | New `List<string>` property on AppSettings, JSON converter, EF config, migration | |
| 2 | create-feature | Follow | FastEndpoints GET endpoint for excluded statuses, `EndpointWithoutRequest<List<string>>` | |
| 3 | create-feature | Follow | FastEndpoints PUT endpoint for excluded statuses, `Endpoint<TReq, TRes>` with validator | |
| 4 | persistence-patterns | Follow | New query method on TicketRepository for bulk-loading status transitions by ticket IDs | |
| 5 | extract-feature-service | Follow | ScopeChangeService: scope metrics, classification, burnup, events, bug time-in-progress | |
| 6 | create-feature | Follow | FastEndpoints GET endpoint for scope-change analytics, `Endpoint<TReq, TRes>` with validator | |
| 7 | (none) | -- | Frontend TypeScript types for scope change response + excluded statuses | Frontend patterns gap |
| 8 | (none) | -- | Frontend API functions for scope change + excluded statuses endpoints | Frontend patterns gap |
| 9 | (none) | -- | Pinia store for Sprints page: sprint mode, sub-team filter, scope change data | Frontend patterns gap |
| 10 | (none) | -- | SprintsView: multi-sprint trend + single-sprint detail, charts, tables | Frontend patterns gap |

## Domain Model Changes

**Modified entity: `AppSettings`**
- Add `ExcludedFromScopeStatuses` property: `List<string>`, default empty list `[]`
- This parallels existing `DoneStatuses` and `WorkflowStages` properties
- Location: `src/Services/Fokus/Fokus.Domain/Settings/AppSettings.cs`

No new entities, aggregates, or domain events.

## Data Model Changes

**Modified table: `AppSettings`**
- New column: `ExcludedFromScopeStatuses` (TEXT, stores JSON array, default `'[]'`)
- Same JSON converter pattern as `DoneStatuses` and `WorkflowStages`

**Migration required:** `AddExcludedFromScopeStatuses`

## Implementation Steps

### Step 1: Add ExcludedFromScopeStatuses to AppSettings and create migration

**What:** Add a new `List<string>` property to the `AppSettings` entity for excluded-from-scope statuses. Configure it in EF Core with the same JSON converter pattern used by `DoneStatuses` and `WorkflowStages`. Generate the migration.

**Files to modify:**
- `src/Services/Fokus/Fokus.Domain/Settings/AppSettings.cs` -- add property `ExcludedFromScopeStatuses` with default `[]`, update `CreateDefault()` to include it
- `src/Services/Fokus/Fokus.Persistence/Configurations/AppSettingsConfiguration.cs` -- add JSON converter for the new property (same pattern as `DoneStatuses`)

**Property:**
```
public List<string> ExcludedFromScopeStatuses { get; set; } = [];
```

**Migration command:**
```
dotnet ef migrations add AddExcludedFromScopeStatuses -p Services/Fokus/Fokus.Persistence -s Services/Fokus/Fokus.API
```

**Skill:** Follow `persistence-patterns` (JSON column conversion on existing entity).

**Pattern reference:** `src/Services/Fokus/Fokus.Persistence/Configurations/AppSettingsConfiguration.cs` lines 13-15 (`DoneStatuses` converter).

**Dependencies:** None.

---

### Step 2: Create GetExcludedStatuses endpoint

**What:** Create `GET /api/settings/excluded-statuses` endpoint that returns the current excluded-from-scope statuses list.

**Files to create:**
- `src/Services/Fokus/Fokus.API/Features/Settings/GetExcludedStatuses/GetExcludedStatusesEndpoint.cs` -- endpoint + response type

**Endpoint details:**
- Extends `EndpointWithoutRequest<List<string>>`
- `[AllowAnonymous]`, `[HttpGet("/api/settings/excluded-statuses")]`, `[Tags("Settings")]`
- Constructor dependency: `AppSettingsRepository`
- Handler: call `GetAsync()`, return `settings.ExcludedFromScopeStatuses` (or empty list if null)

**Skill:** Follow `create-feature` (FastEndpoints variant, `EndpointWithoutRequest`).

**Pattern reference:** `src/Services/Fokus/Fokus.API/Features/Settings/GetSettings/GetSettingsEndpoint.cs`.

**Dependencies:** Step 1.

---

### Step 3: Create SaveExcludedStatuses endpoint

**What:** Create `PUT /api/settings/excluded-statuses` endpoint that replaces the excluded-from-scope statuses list.

**Files to create:**
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveExcludedStatuses/SaveExcludedStatusesEndpoint.cs` -- endpoint + request/response + validator

**Endpoint details:**
- Extends `Endpoint<SaveExcludedStatusesRequest, List<string>>`
- `[AllowAnonymous]`, `[HttpPut("/api/settings/excluded-statuses")]`, `[Tags("Settings")]`
- Constructor dependency: `AppSettingsRepository`

**Handler logic:**
1. Load current settings via `GetAsync()`.
2. Replace `ExcludedFromScopeStatuses` with the request's statuses list.
3. Save via `SaveAsync()`.
4. Return 200 with the updated list.

**Request type:**
```
SaveExcludedStatusesRequest { List<string> Statuses }
```

**Validator:**
- Each status string must not be empty or whitespace: `RuleForEach(x => x.Statuses).NotEmpty().WithMessage("Status must not be empty or blank.")`

**Note:** The `AppSettingsRepository.SaveAsync` method uses `SetValues` which only copies scalar properties. Since `ExcludedFromScopeStatuses` is a `List<string>` (reference type, persisted via JSON converter), it needs explicit assignment like `DoneStatuses`. Verify the existing `SaveAsync` method handles it -- if not, add `existing.ExcludedFromScopeStatuses = settings.ExcludedFromScopeStatuses;` alongside the existing explicit list assignments.

**Skill:** Follow `create-feature` (FastEndpoints variant, `Endpoint<TReq, TRes>` with validator).

**Pattern reference:** `src/Services/Fokus/Fokus.API/Features/Settings/SaveSettings/SaveSettingsEndpoint.cs` (PUT endpoint saving AppSettings).

**Dependencies:** Step 1.

---

### Step 4: Add bulk status transition query method to TicketRepository

**What:** Add a repository method to bulk-load status transitions for a set of ticket IDs. This is needed for the bug time-in-progress computation in single-sprint mode -- we need to know how much time each mid-sprint bug spent in active work statuses.

**New method on `TicketRepository`:**

`GetStatusTransitionsForTicketsAsync(List<string> ticketIds)` -- returns `List<StatusTransition>` for the given ticket IDs. Single query: `Where(st => ticketIds.Contains(st.TicketId))`.

**Files to modify:**
- `src/Services/Fokus/Fokus.Persistence/Repositories/TicketRepository.cs`

**Performance approach:** Single query with `Contains`. The ticket ID list is bounded (only mid-sprint bugs for one sprint -- typically <20). Well within SQLite limits.

**Skill:** Follow `persistence-patterns` (repository query method).

**Pattern reference:** `src/Services/Fokus/Fokus.Persistence/Repositories/TicketRepository.cs` existing methods, e.g., `GetExistingKeysAsync`.

**Dependencies:** None.

---

### Step 5: Create ScopeChangeService

**What:** Create a focused operation service that takes sprint data, app settings, and status transitions, and computes the full scope change response for both multi-sprint and single-sprint modes. This is a pure computation service with no database access -- it receives all data as method parameters. Follows the same pattern as `SprintSummaryService` and `DeveloperThroughputService`.

**Location:** `src/Services/Fokus/Fokus.API/Features/Analytics/ScopeChangeService.cs`

**DI registration:** Add `services.AddScoped<ScopeChangeService>();` in `src/Services/Fokus/Fokus.API/DependencyInjection.cs` alongside existing analytics services.

**Service methods (conceptual):**

**`ComputeMultiSprint(...)`**
- Input: list of closed sprints with memberships (including Ticket + Assignee navigations), app settings (for done statuses, excluded statuses, workflow stages), optional sub-team filter
- Output: `ScopeChangeMultiSprintResponse`

**`ComputeSingleSprint(...)`**
- Input: selected sprint with memberships (including Ticket + Assignee navigations), prior sprint for deltas (nullable), status transitions for mid-sprint bug tickets, app settings, optional sub-team filter
- Output: `ScopeChangeSingleSprintResponse`

**Computation responsibilities:**

1. **Sub-team filtering (C2 pattern).** Reuse the established pattern: filter memberships to those where `Ticket.Assignee.SubTeam == subTeam`. Same logic as `SprintSummaryService.FilterMemberships`.

2. **Excluded-from-scope filtering.** For each membership, check if `FinalStatus` matches any entry in `ExcludedFromScopeStatuses` (case-insensitive comparison using `StringComparer.OrdinalIgnoreCase`). Excluded memberships are removed from SP metric calculations but kept in the event table with an `isExcluded` flag.

3. **Core scope metrics per sprint.** From the filtered, non-excluded memberships:
   - **Committed SP (active):** Sum of `StoryPoints` where `WasCommitted == true` AND `RemovedAt == null` AND `StoryPoints != null` AND `FinalStatus` NOT in excluded statuses.
   - **Committed SP (total):** Same but without the excluded-status filter -- for the "Active vs Total" display.
   - **Added SP:** Sum of `StoryPoints` where `WasCommitted == false` AND `RemovedAt == null` AND `StoryPoints != null` AND `FinalStatus` NOT in excluded statuses.
   - **Removed SP:** Sum of `StoryPoints` where `RemovedAt != null` AND `StoryPoints != null`. (Removed items counted regardless of exclusion -- they were explicitly taken out.)
   - **Completed SP:** Sum of `StoryPoints` where `FinalStatus` in done statuses AND `RemovedAt == null` AND `StoryPoints != null` AND `FinalStatus` NOT in excluded statuses.
   - **Net scope change:** Added SP - Removed SP.
   - **Disruption rate:** Active Committed SP > 0 ? Added SP / Active Committed SP * 100 : 0.
   - **Bug count:** Count of memberships where `WasCommitted == false` AND `RemovedAt == null` AND `Ticket.IssueType == "Bug"` (regardless of story points or exclusion -- ticket count metric).

4. **Classification.** For each mid-sprint addition (membership where `WasCommitted == false` AND `RemovedAt == null`), assign exactly one category using this priority order:
   - **Planning overflow:** `AddedAt <= sprint.StartDate.AddDays(2)` (added within first 2 days).
   - **Unplanned bug:** `Ticket.IssueType == "Bug"` AND added after day 2.
   - **Priority escalation:** `Ticket.CreatedDate < sprint.StartDate` AND added after day 2. The ticket existed before the sprint -- pulled in due to changed priorities.
   - **Scope injection:** Remaining mid-sprint additions after day 2.

   Aggregate by category: ticket count, SP total (sum where `StoryPoints != null`), percentage of total mid-sprint additions (by count).

5. **Delta computation (single-sprint only, C1 pattern).** For each metric card, compute delta vs prior sprint:
   - Disruption rate: polarity `positive-down` (lower is better).
   - Added SP: polarity `positive-down` (lower is better).
   - Removed SP: polarity `neutral`.
   - Net scope change: polarity `neutral`.
   - Completed SP: polarity `positive-up` (higher is better).
   - Bug count: polarity `positive-down` (lower is better).
   - Committed SP (active): polarity `neutral`.

   When no prior sprint exists, delta is null.

6. **Burnup chart data (single-sprint only).** For each day of the sprint (from `StartDate` to `EndDate`):
   - Day number (1-based).
   - Calendar date.
   - **Total scope SP:** Start at active committed SP. For each day, add the SP of items added on that day (`AddedAt` falls on that day, not removed, not excluded), subtract the SP of items removed on that day (`RemovedAt` falls on that day). Cumulative.
   - **Completed SP:** Needs status transitions. For each membership not removed, find the earliest transition to a done status within the sprint date range. On the day that transition occurred, add that ticket's SP to completed. Cumulative.
   - **Phase label:** Days 1-2 = "planning", day 3+ = "execution".

7. **Event table (single-sprint only).** Chronological list of all add/remove events:
   - For each membership where `WasCommitted == false` (mid-sprint addition): create an "added" event at `AddedAt`.
   - For each membership where `RemovedAt != null` (removal): create a "removed" event at `RemovedAt`.
   - Each event carries: date (calendar date + sprint day number), `Ticket.Id` (key), `Ticket.Summary`, `StoryPoints` (nullable), `Ticket.IssueType`, action ("added"/"removed"), classification category (for added items; null for removed), `isExcluded` (true if `FinalStatus` matches an excluded status).
   - Sort by date ascending.

8. **Bug time-in-progress (single-sprint only).** For each bug added mid-sprint (`WasCommitted == false`, `Ticket.IssueType == "Bug"`, `RemovedAt == null`):
   - Use the provided status transitions for that ticket.
   - Identify which statuses are "active work" statuses -- statuses that fall between the workflow start and end boundaries from `AppSettings.WorkflowStages`. If `WorkflowStages` is empty, use the convention that "In Progress" is the only active status.
   - Sum the time spent in active statuses: for each transition INTO an active status, measure until the next transition OUT (or until sprint end if still active). Express in days (decimal).
   - Return: ticket key, summary, time in active statuses (days), current status.
   - When no mid-sprint bugs exist, this section is empty (frontend hides it).

**Response record hierarchy (defined in the service file):**

**Multi-sprint response:**
```
ScopeChangeMultiSprintResponse
  List<ScopeChangeSprintInfo> Sprints
  ScopeChangeSummaryMetrics SummaryMetrics
  List<ScopeChangePerSprintData> PerSprintData
  List<ClassificationEntry> ClassificationBreakdown

ScopeChangeSprintInfo { int Id, string Name, DateTime StartDate, DateTime EndDate }

ScopeChangeSummaryMetrics {
  decimal AverageDisruptionRate,
  decimal? AverageDisruptionRateDelta,  // delta vs previous window (null)
  string? AverageDisruptionRateDeltaDirection,
  decimal AverageNetScopeChange,
  int TotalBugsAdded
}

ScopeChangePerSprintData {
  int SprintId,
  decimal CommittedSpActive,
  decimal CommittedSpTotal,
  decimal AddedSp,
  decimal RemovedSp,
  decimal NetScopeChange,
  decimal CompletedSp,
  decimal DisruptionRate,
  int BugCount
}

ClassificationEntry {
  string Category,     // "Planning Overflow", "Unplanned Bug", "Scope Injection", "Priority Escalation"
  int TicketCount,
  decimal? SpTotal,    // null when all items in category have no SP
  decimal Percentage   // of total mid-sprint additions by count
}
```

**Single-sprint response:**
```
ScopeChangeSingleSprintResponse
  ScopeChangeSprintInfo Sprint
  ScopeChangeSingleSprintMetrics Metrics
  List<BurnupDataPoint> BurnupData
  List<ClassificationEntry> ClassificationBreakdown
  List<ScopeChangeEvent> Events
  List<BugTimeInProgress> BugTimeInProgress

ScopeChangeSingleSprintMetrics {
  ScopeMetricCard CommittedSpActive,
  ScopeMetricCard CommittedSpTotal,   // no delta -- display only
  ScopeMetricCard AddedSp,
  ScopeMetricCard RemovedSp,
  ScopeMetricCard NetScopeChange,
  ScopeMetricCard DisruptionRate,
  ScopeMetricCard BugCount
}

ScopeMetricCard {
  string Name,
  decimal Value,
  string DisplayValue,
  decimal? Delta,
  string? DeltaDirection,    // "up"/"down"/"flat"
  string? DeltaPolarity      // "positive"/"negative"/"neutral"
}

BurnupDataPoint {
  int DayNumber,
  DateTime Date,
  decimal TotalScopeSp,
  decimal CompletedSp,
  string Phase               // "planning" or "execution"
}

ScopeChangeEvent {
  DateTime Date,
  int SprintDayNumber,
  string TicketKey,
  string TicketSummary,
  decimal? StoryPoints,
  string IssueType,
  string Action,             // "added" or "removed"
  string? Category,          // classification -- null for removed items
  bool IsExcluded
}

BugTimeInProgress {
  string TicketKey,
  string TicketSummary,
  decimal TimeInActiveDays,
  string CurrentStatus
}
```

**Files to create:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/ScopeChangeService.cs` -- service class + all response/DTO records

**Files to modify:**
- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` -- add `services.AddScoped<ScopeChangeService>();`

**Skill:** Follow `extract-feature-service` (focused operation service pattern).

**Pattern reference:** `src/Services/Fokus/Fokus.API/Features/Analytics/DeveloperThroughputService.cs` (pure computation service, record hierarchy in same file, sub-team filtering, separate methods for modes).

**Dependencies:** None (consumes data passed by the endpoint in Step 6).

---

### Step 6: Create GetScopeChange endpoint

**What:** Create the `GET /api/analytics/scope-change` endpoint. It accepts optional `sprintId` (int), `last` (int), and `subTeam` (string) query parameters. It orchestrates data loading from repositories and delegates computation to `ScopeChangeService`.

**Files to create:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetScopeChange/GetScopeChangeEndpoint.cs` -- endpoint class
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetScopeChange/GetScopeChangeQuery.cs` -- request + validator

**Endpoint details:**
- Extends `Endpoint<GetScopeChangeRequest>`
- `[AllowAnonymous]`, `[HttpGet("/api/analytics/scope-change")]`, `[Tags("Analytics")]`
- Constructor dependencies: `SprintRepository`, `TicketRepository`, `AppSettingsRepository`, `ScopeChangeService`

**Why `Endpoint<TReq>` without `TRes`:** The response type differs by mode (multi vs single). Use `SendOkAsync(object)` to send the appropriate response type, same as how the endpoint can send different shaped responses. Alternatively, use a wrapper response. Recommendation: use a discriminated response wrapper.

**Response wrapper:**
```
ScopeChangeResponse {
  string Mode,                                       // "multi" or "single"
  ScopeChangeMultiSprintResponse? MultiSprint,
  ScopeChangeSingleSprintResponse? SingleSprint
}
```

The endpoint returns `Endpoint<GetScopeChangeRequest, ScopeChangeResponse>`.

**Handler logic:**
1. Load all closed sprints (lightweight, no memberships) via `GetClosedSprintsAsync()`.
2. If no closed sprints exist, return 200 with empty response (mode = "multi", null data).
3. Determine mode and target sprints (same pattern as `GetDeveloperThroughputEndpoint`):
   - `sprintId` provided: single-sprint mode. Validate it exists in closed sprints (400 if not).
   - `last` provided: multi-sprint mode with last N.
   - Neither provided: default to `last=5`.
4. Normalize sub-team (empty string -> null).
5. Load app settings via `GetAsync()`.

**Multi-sprint mode:**
6a. Determine target sprint IDs from closed sprints list (take last N, ordered ascending).
7a. Bulk load sprints with memberships via `GetSprintsWithMembershipsAsync(targetIds)`.
8a. Call `ScopeChangeService.ComputeMultiSprint(loadedSprints, settings, subTeam)`.
9a. Return wrapped response with mode = "multi".

**Single-sprint mode:**
6b. Identify the prior closed sprint (for delta computation) -- next-earlier start date.
7b. Load the target sprint and prior sprint with memberships via `GetSprintsWithMembershipsAsync([targetId, priorId])`.
8b. Identify mid-sprint bug ticket IDs from the target sprint's memberships (where `WasCommitted == false`, `RemovedAt == null`, `Ticket.IssueType == "Bug"`).
9b. If bug ticket IDs are non-empty, bulk load their status transitions via `GetStatusTransitionsForTicketsAsync(bugTicketIds)`.
10b. Call `ScopeChangeService.ComputeSingleSprint(targetSprint, priorSprint, transitions, settings, subTeam)`.
11b. Return wrapped response with mode = "single".

**Request type:**
```
GetScopeChangeRequest { int? SprintId, int? Last, string? SubTeam }
```

**Validator** (same pattern as `GetDeveloperThroughputRequestValidator`):
- `SprintId` must be > 0 when provided.
- `Last` must be >= 1 when provided.
- Custom rule: `SprintId` and `Last` must not both be provided (400).
- `SubTeam` must be non-empty when provided.

**Performance approach:** The dataset is small (SQLite, single team). Multi-sprint mode: one query for sprints with memberships (max ~20 sprints). Single-sprint mode: one query for 1-2 sprints with memberships + one query for bug status transitions (typically <20 bugs). No caching needed.

**Error reporting shape:**
- 400 with validation errors for bad input (both params, invalid sprint, last < 1).
- 200 with mode="multi", null MultiSprint data when no closed sprints exist.

**Skill:** Follow `create-feature` (FastEndpoints variant, `Endpoint<TReq, TRes>` with validator).

**Pattern reference:** `src/Services/Fokus/Fokus.API/Features/Analytics/GetDeveloperThroughput/GetDeveloperThroughputEndpoint.cs` (analytics endpoint, mode-based logic, repository orchestration, service delegation).

**Dependencies:** Steps 1, 4, 5.

---

### Step 7: Add frontend TypeScript types for scope change and excluded statuses

**What:** Add all TypeScript interfaces matching the API response shapes for the new endpoints.

**Files to modify:**
- `client/src/types/index.ts` -- add new interfaces at the end

**Types to add:**

```typescript
// Scope change response wrapper
ScopeChangeResponse { mode: 'multi' | 'single', multiSprint: ScopeChangeMultiSprintResponse | null, singleSprint: ScopeChangeSingleSprintResponse | null }

// Multi-sprint response
ScopeChangeMultiSprintResponse { sprints: ScopeChangeSprintInfo[], summaryMetrics: ScopeChangeSummaryMetrics, perSprintData: ScopeChangePerSprintData[], classificationBreakdown: ClassificationEntry[] }
ScopeChangeSprintInfo { id: number, name: string, startDate: string, endDate: string }
ScopeChangeSummaryMetrics { averageDisruptionRate: number, averageDisruptionRateDelta: number | null, averageDisruptionRateDeltaDirection: string | null, averageNetScopeChange: number, totalBugsAdded: number }
ScopeChangePerSprintData { sprintId: number, committedSpActive: number, committedSpTotal: number, addedSp: number, removedSp: number, netScopeChange: number, completedSp: number, disruptionRate: number, bugCount: number }
ClassificationEntry { category: string, ticketCount: number, spTotal: number | null, percentage: number }

// Single-sprint response
ScopeChangeSingleSprintResponse { sprint: ScopeChangeSprintInfo, metrics: ScopeChangeSingleSprintMetrics, burnupData: BurnupDataPoint[], classificationBreakdown: ClassificationEntry[], events: ScopeChangeEvent[], bugTimeInProgress: BugTimeInProgress[] }
ScopeChangeSingleSprintMetrics { committedSpActive: ScopeMetricCard, committedSpTotal: ScopeMetricCard, addedSp: ScopeMetricCard, removedSp: ScopeMetricCard, netScopeChange: ScopeMetricCard, disruptionRate: ScopeMetricCard, bugCount: ScopeMetricCard }
ScopeMetricCard { name: string, value: number, displayValue: string, delta: number | null, deltaDirection: string | null, deltaPolarity: string | null }
BurnupDataPoint { dayNumber: number, date: string, totalScopeSp: number, completedSp: number, phase: string }
ScopeChangeEvent { date: string, sprintDayNumber: number, ticketKey: string, ticketSummary: string, storyPoints: number | null, issueType: string, action: string, category: string | null, isExcluded: boolean }
BugTimeInProgress { ticketKey: string, ticketSummary: string, timeInActiveDays: number, currentStatus: string }
```

**Skill:** None (frontend patterns gap).

**Pattern reference:** Existing types in `client/src/types/index.ts`.

**Dependencies:** None (types are API contract documentation).

---

### Step 8: Add frontend API functions for scope change and excluded statuses

**What:** Add API functions for the new endpoints.

**Files to modify:**
- `client/src/api/analytics.ts` -- add `getScopeChange` function
- `client/src/api/settings.ts` -- add `getExcludedStatuses` and `saveExcludedStatuses` functions

**Functions to add:**

In `analytics.ts`:
- `getScopeChange(sprintId?: number, last?: number, subTeam?: string): Promise<ScopeChangeResponse>` -- calls `GET /analytics/scope-change` with optional query params. Same URL-building pattern as `getDeveloperThroughput`.

In `settings.ts`:
- `getExcludedStatuses(): Promise<string[]>` -- calls `GET /settings/excluded-statuses`.
- `saveExcludedStatuses(statuses: string[]): Promise<string[]>` -- calls `PUT /settings/excluded-statuses` with `{ statuses }` body.

All functions use the existing `apiFetch` from `client/src/api/client.ts`.

**Skill:** None (frontend patterns gap).

**Pattern reference:** `client/src/api/analytics.ts` (GET with query params), `client/src/api/settings.ts` (PUT with JSON body).

**Dependencies:** Step 7 (types for return type annotations).

---

### Step 9: Create sprints Pinia store

**What:** Create a Pinia store for the Sprints page that manages sprint selection mode (single vs multi, defaulting to `last=5`), sub-team filtering, scope change data fetching, and view mode tracking.

**Files to create:**
- `client/src/stores/sprintsStore.ts`

**Store state:**
- `closedSprints: ClosedSprintItem[]` -- list for the sprint selector
- `subTeams: string[]` -- list for the sub-team filter
- `selectedSprintId: number | null` -- selected sprint in single-sprint mode
- `selectedLast: number | null` -- selected "Last N" value (3, 5, or null for all)
- `sprintMode: 'single' | 'multi'` -- current mode (default: 'multi')
- `selectedSubTeam: string | null` -- sub-team filter
- `scopeChange: ScopeChangeResponse | null` -- current data
- `loading: boolean`
- `initializing: boolean`
- `error: string | null`

**Store actions:**
- `initialize()` -- fetches closed sprints and sub-teams in parallel. Sets default to multi-sprint mode with `last=5`. Fetches scope change data. Called once on Sprints page mount.
- `selectSprint(sprintId: number)` -- switches to single-sprint mode, fetches scope change with `sprintId`.
- `selectLastN(n: number | null)` -- switches to multi-sprint mode. Fetches scope change with `last` param.
- `selectSubTeam(subTeam: string | null)` -- updates filter, re-fetches.
- `fetchScopeChange()` -- internal action that calls `getScopeChange(...)` based on current mode.

**Default behavior difference from DevelopersView:** The Sprints page defaults to multi-sprint mode (`last=5`), not single-sprint mode. This matches the spec's "User navigates to Sprints -- the sprint selector defaults to Last 5."

**Skill:** None (frontend patterns gap).

**Pattern reference:** `client/src/stores/developersStore.ts` (same structure: initialize, mode switching, sub-team filter, loading/error states).

**Dependencies:** Steps 7 and 8 (types and API functions).

---

### Step 10: Build SprintsView with multi-sprint trend and single-sprint detail

**What:** Replace the current empty-state SprintsView with the full scope change display. This is the largest frontend step, with two distinct view modes.

**Files to modify:**
- `client/src/views/SprintsView.vue` -- complete rewrite

**Files to create (component extraction for readability and reuse):**
- `client/src/components/sprints/ScopeMetricCards.vue` -- renders summary metric cards (multi) or detail metric cards with deltas (single)
- `client/src/components/sprints/ClassificationTable.vue` -- renders classification breakdown table (used in both views)
- `client/src/components/sprints/ScopeChangeChart.vue` -- stacked/grouped bar chart + disruption rate trend (multi-sprint)
- `client/src/components/sprints/BurnupChart.vue` -- scope burnup chart with phase shading (single-sprint)
- `client/src/components/sprints/EventTable.vue` -- chronological event table (single-sprint)
- `client/src/components/sprints/BugTimeTable.vue` -- bug time-in-progress table (single-sprint)

**SprintsView structure:**

1. **Mount logic:** On mount, call `sprintsStore.initialize()`. Read route query params for initial state (`?sprint=123` for single, `?last=5` or `?last=all` for multi). Default: multi with last=5.

2. **Conditional rendering:**
   - If `initializing`: loading state.
   - If no closed sprints: existing `EmptyState` component.
   - If `scopeChange.mode === 'multi'`: multi-sprint trend view.
   - If `scopeChange.mode === 'single'`: single-sprint detail view.

3. **PageLayout** with title "Sprints" and **PageToolbar** in the toolbar slot. Props:
   - Pass `showAggregateOptions: true` (reuse the toolbar extension from F9).
   - Wire sprint selector and sub-team filter to the store.
   - Sprint click in chart navigates to single-sprint mode.

4. **Multi-sprint trend view:**
   - **ScopeMetricCards** showing: Average Disruption Rate, Average Net Scope Change, Total Bugs Added. Displayed in a row of 3 `BaseCard` components.
   - **ScopeChangeChart** using `vue3-apexcharts`:
     - Chart type: grouped bar chart.
     - Series: Committed SP (baseline, gray), Added SP (orange), Removed SP (red), Completed SP (green) per sprint.
     - X-axis: sprint names.
     - A separate line series (or secondary y-axis) for disruption rate trend.
     - Bar click triggers navigation to single-sprint detail for that sprint.
   - **ClassificationTable** showing totals across all selected sprints.

5. **Single-sprint detail view:**
   - **ScopeMetricCards** showing 7 metric cards with deltas: Committed SP (active + total display "Active: X | Total: Y"), Added SP, Removed SP, Net Scope Change, Disruption Rate, Bug Count. Each with delta arrow and polarity coloring.
   - **BurnupChart** using `vue3-apexcharts`:
     - Chart type: area/line chart.
     - Series: Total Scope SP (line), Completed SP (area fill).
     - X-axis: sprint days (day number + calendar date).
     - Background shading: planning phase (days 1-2) with a subtle annotation zone, execution phase (day 3+).
   - **ClassificationTable** for this sprint.
   - **EventTable** listing all add/remove events. Columns: Date, Ticket Key, Summary, SP, Issue Type, Action, Category. Excluded items shown with dimmed opacity + "excluded" badge.
   - **BugTimeTable** (conditional -- hidden when empty): Table showing bug key, summary, time in active days, current status.

6. **URL sync:** Update route query params on mode change. `?sprint=123` for single-sprint, `?last=5` for last-N, `?last=all` for all. Same pattern as `DevelopersView.vue`.

**Styling:**
- Reuse existing design tokens (`bg-surface-card`, `border-border-default`, `text-text-primary`, etc.)
- Delta colors: `text-status-success` for positive, `text-status-danger` for negative, `text-text-secondary` for neutral.
- Excluded event rows: `opacity-50` + a small "excluded" badge.
- Classification category names: use plain text, no color coding in v1.
- All sections use `BaseCard` as container.

**Skill:** None (frontend patterns gap).

**Pattern reference:** `client/src/views/DevelopersView.vue` (page layout, store integration, PageToolbar wiring, ApexCharts usage, URL sync, mode switching).

**Dependencies:** Step 9 (store must exist).

---

## Cross-Service Changes

None. Single-service feature, read-only analytics + settings CRUD.

## Migration Notes

**Add migration:**
```
dotnet ef migrations add AddExcludedFromScopeStatuses -p Services/Fokus/Fokus.Persistence -s Services/Fokus/Fokus.API
```

**Apply migration:**
```
dotnet ef database update -p Services/Fokus/Fokus.Persistence -s Services/Fokus/Fokus.API
```

No seed data needed -- `ExcludedFromScopeStatuses` defaults to empty list.

## Testing Strategy

**Backend:**
1. **Excluded statuses CRUD.** `GET /api/settings/excluded-statuses` returns empty list initially. `PUT` with `["To Do", "Blocked"]` returns updated list. `GET` returns the saved list. `PUT` with empty status string returns 400.
2. **Scope change -- multi-sprint default.** Call `GET /api/analytics/scope-change` with no params. Verify mode="multi", last 5 closed sprints returned, per-sprint data present.
3. **Scope change -- multi-sprint with last.** Call with `last=3`. Verify exactly 3 sprints in response.
4. **Scope change -- single sprint.** Call with `sprintId={validId}`. Verify mode="single", single-sprint response with metrics, burnup data, events, classification.
5. **Mutual exclusivity.** Call with both `sprintId` and `last`. Verify 400.
6. **Invalid sprint.** Call with `sprintId=99999`. Verify 400.
7. **Delta computation.** With 2+ closed sprints, verify delta values in single-sprint mode. With only 1 closed sprint, verify deltas are null.
8. **Classification.** Create sprint data with: item added day 1 (expect planning overflow), bug added day 5 (expect unplanned bug), old ticket added day 5 (expect priority escalation), new ticket added day 5 (expect scope injection). Verify categories match.
9. **Excluded-from-scope effect.** Configure excluded statuses. Verify committed SP active excludes matching items. Verify committed SP total still includes them. Verify event table shows excluded items with `isExcluded=true`.
10. **Sub-team filter.** Call with `subTeam=Frontend`. Verify all metrics scoped to that sub-team's developers.
11. **Burnup chart data.** Verify daily data points span the sprint duration. Verify cumulative scope line steps up on addition days and down on removal days.
12. **Bug time-in-progress.** For a bug with status transitions through active statuses, verify time computation in days. When no bugs exist, verify empty array.
13. **Division by zero.** Sprint with 0 committed SP. Verify disruption rate = 0.
14. **Bug count includes unestimated bugs.** Add a bug with no story points mid-sprint. Verify it counts in bug count but not in SP metrics.

**Frontend:**
15. **Sprints page loads.** Navigate to `/sprints`. Verify multi-sprint trend view with last 5 sprints. Summary cards, bar chart, classification table all rendered.
16. **Switch to single sprint.** Click a sprint bar or select a specific sprint. Verify URL updates to `?sprint=123`. Verify metric cards, burnup chart, event table, classification.
17. **Switch to multi-sprint.** Select "Last 3" or "Last 5" from toolbar. Verify URL updates. Verify trend view renders.
18. **Sub-team filter.** Select a sub-team. Verify all content filters.
19. **Empty state.** With no closed sprints, verify empty state message.
20. **Excluded items in event table.** With excluded statuses configured, verify excluded events are dimmed.
21. **Bug time-in-progress hidden.** When no mid-sprint bugs exist, verify section is not rendered.
22. **Burnup chart phases.** Verify planning phase (days 1-2) and execution phase (day 3+) are visually distinct.

## Open Questions

None.
