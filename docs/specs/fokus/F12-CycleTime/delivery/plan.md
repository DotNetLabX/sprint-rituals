# Cycle Time

**Feature Spec:** `docs/features/CycleTime/spec.md`

## Context

Scrum Masters need evidence-based answers to "how long does work actually take?" Jira shows individual ticket timelines but cannot aggregate cycle time across sprints, break it down by workflow stage, or surface bottlenecks. This feature computes cycle time from existing status transition data, surfaces percentile metrics (P50/P85), identifies outlier tickets, and shows a stage funnel revealing the team's bottleneck. It owns a dedicated "Cycle Time" page with single-sprint drill-down and multi-sprint trend views.

**Services impacted:** Fokus (single service). Backend adds two new settings endpoints and one analytics endpoint. Frontend adds a new page with store, API module, types, and components.

## Scope

**In scope:**
- `GET /api/analytics/cycle-time` endpoint (single-sprint and multi-sprint modes)
- `GET /api/settings/cycle-time-boundaries` endpoint
- `PUT /api/settings/cycle-time-boundaries` endpoint
- `CycleTimeService` focused operation service for all cycle time computation
- Two new `AppSettings` fields: `CycleTimeStartStage`, `CycleTimeEndStage`
- EF Core migration for the new fields
- Frontend: Cycle Time page with scatter plot, stage funnel, metric cards, breakdowns, outlier table, trend line, sprint summary table
- Sidebar navigation entry
- Route with URL state (`/cycle-time?sprint=X` or `/cycle-time?last=N`)
- Sub-team filter and percentile toggle (single-sprint only)
- Settings UI: cycle time boundary dropdowns below the Workflow Stages section
- Tooltip wiring from `docs/features/CycleTime/help.tooltips.md`

**Explicitly out of scope:**
- Business day calculations (calendar days only)
- PR-level cycle time
- Real-time SignalR updates
- Export (PDF/CSV)
- Cycle time contribution to health score
- Flow efficiency
- WIP-over-time chart
- Scatter plot click-through to Jira
- Multi-sprint per-developer and per-issue-type breakdowns
- Per-developer trend sparklines

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | persistence-patterns | Follow | Two new `string?` columns on `AppSettings`, JSON conversion in EF config | |
| 2 | create-feature | Follow | FastEndpoints GET endpoint for cycle-time-boundaries | |
| 3 | create-feature | Follow | FastEndpoints PUT endpoint for cycle-time-boundaries | |
| 4 | extract-feature-service | Follow | `CycleTimeService` in `Features/Analytics/`, result records for single/multi responses | |
| 5 | create-feature | Follow | FastEndpoints GET endpoint for cycle-time analytics | |
| 6 | create-vue-feature | Follow | Types, API module, store, route, sidebar entry | |
| 7 | vue-component-architecture | Follow | L3 shared (metric cards, stage funnel) + single-sprint (scatter plot, breakdowns, outlier table) | |
| 8 | vue-component-architecture | Follow | L3 trend line chart, sprint summary table (multi-sprint components) | |
| 9 | pinia-patterns | Follow | `cycleTimeStore` with initialize, sprint mode, sub-team, percentile toggle | |
| 10 | create-vue-feature | Follow | `CycleTimeView.vue` L0 page, three-state template, URL sync | |
| 11 | (none) | -- | Settings UI: boundary dropdowns in SettingsView.vue | Inline: extends existing view |
| 12 | (none) | -- | Tooltip wiring from help.tooltips.md | Inline: follows help-tooltips rule |

## Domain Model Changes

**No new aggregates, entities, or value objects.**

**AppSettings extensions (two new fields):**
- `CycleTimeStartStage` (`string?`, default `null`) -- when null, defaults to second workflow stage at query time
- `CycleTimeEndStage` (`string?`, default `null`) -- when null, defaults to first done status at query time

These are nullable strings (not lists), following the same pattern as existing scalar fields on `AppSettings`.

## Data Model Changes

**Modified table:** `AppSettings`
- Add column `CycleTimeStartStage` (`TEXT`, nullable)
- Add column `CycleTimeEndStage` (`TEXT`, nullable)

**No new tables.** All cycle time metrics are computed on the fly from existing `StatusTransition`, `SprintMembership`, `Ticket`, `Developer`, and `Sprint` data.

## Implementation Steps

### Step 1: Add cycle time boundary fields to AppSettings and persist

**What:** Add two nullable string properties to `AppSettings`, update the EF configuration, add a migration, and update `AppSettingsRepository.SaveAsync` to handle the new fields.

**Follow:** `persistence-patterns`

**Files to modify:**
- `src/Services/Fokus/Fokus.Domain/Settings/AppSettings.cs` -- add `CycleTimeStartStage` and `CycleTimeEndStage` string? properties with default `null`, add them to `CreateDefault()`
- `src/Services/Fokus/Fokus.Persistence/Configurations/AppSettingsConfiguration.cs` -- no config needed for nullable strings (EF convention handles them)

**Note:** `AppSettingsRepository.SaveAsync` does NOT need modification. The `SetValues()` call already copies all scalar properties including nullable strings. The explicit lines after `SetValues` are only for navigation properties and JSON-converted `List<string>` properties that `SetValues` cannot handle. `CycleTimeStartStage` and `CycleTimeEndStage` are simple nullable strings -- `SetValues` handles them correctly.

**Migration command:**
```
dotnet ef migrations add AddCycleTimeBoundaries -p Services/Fokus/Fokus.Persistence -s Services/Fokus/Fokus.API
```

**Dependencies:** None.

---

### Step 2: Create GET /api/settings/cycle-time-boundaries endpoint

**What:** Create a GET endpoint that returns the current cycle time boundaries plus the full list of available stages (workflow stages + done statuses) for dropdown population.

**Follow:** `create-feature`

**Files to create:**
- `src/Services/Fokus/Fokus.API/Features/Settings/GetCycleTimeBoundaries/GetCycleTimeBoundariesEndpoint.cs`

**Route:** `GET /api/settings/cycle-time-boundaries`
**Tags:** `Settings`
**Auth:** `AllowAnonymous`
**Type:** `EndpointWithoutRequest<GetCycleTimeBoundariesResponse>`

**Response record** (define in same file):
```
GetCycleTimeBoundariesResponse(
    string StartStage,        // resolved: configured or default (2nd workflow stage)
    string EndStage,          // resolved: configured or default (1st done status)
    List<string> AvailableStages  // workflow stages + done statuses
)
```

**Logic:** Load `AppSettings`. Resolve defaults per BR13: start = second workflow stage (index 1), end = first done status. Return empty available stages when no workflow stages configured.

**Pattern reference:** `GetExcludedStatuses/GetExcludedStatusesEndpoint.cs` (simple settings read).

**Dependencies:** Step 1.

---

### Step 3: Create PUT /api/settings/cycle-time-boundaries endpoint

**What:** Create a PUT endpoint that saves cycle time start and end stages to AppSettings. Validates that both stages exist in the configured workflow stages or done statuses, and that start precedes end in the configured order.

**Follow:** `create-feature`

**Files to create:**
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveCycleTimeBoundaries/SaveCycleTimeBoundariesEndpoint.cs`

**Route:** `PUT /api/settings/cycle-time-boundaries`
**Tags:** `Settings`
**Auth:** `AllowAnonymous`
**Type:** `Endpoint<SaveCycleTimeBoundariesRequest, SaveCycleTimeBoundariesResponse>`

**Request record:**
```
SaveCycleTimeBoundariesRequest {
    string StartStage
    string EndStage
}
```

**Validator (sibling class in same file):**
- `StartStage` required, not empty
- `EndStage` required, not empty

**Handler validation (beyond FluentValidation):**
- Load settings, build ordered stage list (workflow stages + done statuses)
- 400 if `StartStage` not found in the ordered list
- 400 if `EndStage` not found in the ordered list
- 400 if `StartStage` index >= `EndStage` index (start must precede end)

**On success:** Set `settings.CycleTimeStartStage` and `settings.CycleTimeEndStage`, call `repository.SaveAsync`, return the saved boundaries.

**Response record:**
```
SaveCycleTimeBoundariesResponse(string StartStage, string EndStage)
```

**Pattern reference:** `SaveExcludedStatuses/SaveExcludedStatusesEndpoint.cs` (dedicated settings save avoiding full-replacement conflicts).

**Dependencies:** Step 1.

---

### Step 4: Create CycleTimeService focused operation service

**What:** Create the `CycleTimeService` that contains all cycle time computation logic. This is the core of the feature. It computes single-sprint and multi-sprint responses from loaded domain data.

**Follow:** `extract-feature-service`

**Files to create:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/CycleTimeService.cs` -- service class + all response record types

**DI registration:**
- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` -- add `services.AddScoped<CycleTimeService>()`

**Response records (define above the service class, in same file):**

Single-sprint response:
```
CycleTimeSprintInfo(int Id, string Name, DateTime StartDate, DateTime EndDate)

CycleTimeMetricCard(
    string Name, decimal Value, string DisplayValue,
    decimal? Delta, string? DeltaDirection, string? DeltaPolarity)

CycleTimeScatterPoint(
    string TicketKey, string TicketSummary, string IssueType,
    decimal CycleTimeDays, DateTime CompletionDate,
    List<StageBreakdownEntry> StageBreakdown, int ReworkCount)

StageBreakdownEntry(string StageName, decimal DurationDays)

StageFunnelEntry(string StageName, decimal AverageDurationDays, decimal Percentage)

CycleTimeIssueTypeEntry(
    string IssueType, int TicketCount, decimal MedianCycleTime, decimal P85CycleTime)

CycleTimeDeveloperEntry(
    string DisplayName, string? AvatarUrl, string? SubTeam,
    int TicketsCompleted, decimal MedianCycleTime, decimal P85CycleTime,
    string DominantStage)

CycleTimeOutlierEntry(
    string TicketKey, string TicketSummary, string IssueType,
    decimal CycleTimeDays, List<StageBreakdownEntry> StageBreakdown, int ReworkCount)

CycleTimeBoundaries(string StartStage, string EndStage)

CycleTimeSingleSprintResponse(
    CycleTimeSprintInfo? Sprint,
    List<CycleTimeMetricCard>? MetricCards,  // 4 cards: Median, P85, Throughput, Outliers
    List<CycleTimeScatterPoint> ScatterPlot,
    List<StageFunnelEntry> StageFunnel,
    List<CycleTimeIssueTypeEntry> IssueTypeBreakdown,
    List<CycleTimeDeveloperEntry> DeveloperBreakdown,
    List<CycleTimeOutlierEntry> Outliers,
    CycleTimeBoundaries Boundaries)
```

Multi-sprint response:
```
CycleTimeTrendEntry(
    int SprintId, string SprintName,
    decimal P85CycleTime, decimal MedianCycleTime, int TicketsCompleted)

CycleTimeSprintSummaryEntry(
    int SprintId, string SprintName, DateTime StartDate,
    int TicketsCompleted, decimal MedianCycleTime, decimal P85CycleTime,
    int OutlierCount)

CycleTimeMultiSprintResponse(
    List<CycleTimeSprintInfo> Sprints,
    List<CycleTimeMetricCard>? MetricCards,  // averaged across sprints
    List<CycleTimeTrendEntry> Trend,
    List<StageFunnelEntry> StageFunnel,
    List<CycleTimeSprintSummaryEntry> SprintSummaries,
    CycleTimeBoundaries Boundaries)
```

Wrapper:
```
CycleTimeResponse(
    string Mode,  // "single" or "multi"
    CycleTimeSingleSprintResponse? SingleSprint,
    CycleTimeMultiSprintResponse? MultiSprint)
```

**Service methods:**

`ComputeSingleSprint(Sprint targetSprint, Sprint? priorSprint, List<StatusTransition> statusTransitions, AppSettings settings, string? subTeam)` -> `CycleTimeSingleSprintResponse`

`ComputeMultiSprint(List<Sprint> sprints, List<StatusTransition> statusTransitions, AppSettings settings, string? subTeam)` -> `CycleTimeMultiSprintResponse`

**Core computation logic (business rules, all implemented as private methods):**

1. **ResolveBoundaries** -- resolve start/end stages from settings (BR13). Build ordered stage list: workflow stages + done statuses. Return start index and end index.

2. **ComputeTicketCycleTime** -- for each completed ticket (BR1: final status in done statuses), walk its status transitions:
   - Build a stage timeline: for each transition where `ToStatus` matches a stage within boundaries, record enter time. Next transition out = exit time.
   - **Sprint-start clamping (BR18):** If a ticket is in a measured stage at sprint start (its last transition into that stage predates sprint start), begin accumulation from sprint start date.
   - **Sprint-end clamping (BR18):** If a ticket is still in a measured stage at sprint end, end accumulation at sprint end date.
   - **Rework counting (BR4):** Track visited stages. When a ticket re-enters a previously visited measured stage, increment rework count.
   - **Exclusion (BR5):** Skip tickets that never entered the start stage.
   - **Exclusion (BR19):** Skip tickets whose done-status transitions all predate the sprint window.
   - Total cycle time = sum of all stage durations within boundaries (BR3).
   - Completion date = earliest done-status transition within sprint window (BR19).

3. **ComputePercentiles** -- from the list of cycle times, compute P50, P75, P85, P90 using linear interpolation (BR7).

4. **BuildMetricCards** -- four cards:
   - Median Cycle Time: `P50` value, display `"{X} days"`, delta vs prior sprint, polarity positive-down
   - P85 Cycle Time: `P85` value, display `"{X} days"`, delta vs prior sprint, polarity positive-down
   - Throughput: ticket count, display `"{N} tickets"`, delta vs prior sprint, polarity positive-up
   - Outliers: count of tickets > 2x median (BR6), display `"{N} outliers"`, delta vs prior sprint, polarity positive-down

5. **BuildStageFunnel** -- for each stage within boundaries (BR8), compute average duration across all completed tickets. Calculate percentage of total. Only stages within boundaries appear.

6. **BuildIssueTypeBreakdown** -- group completed tickets by issue type, compute median and P85 per group.

7. **BuildDeveloperBreakdown** -- group completed tickets by assignee. Per developer: count, median, P85, dominant stage (BR9: stage with highest average time for that developer's tickets). Sort alphabetically.

8. **BuildOutlierTable** -- filter tickets where cycle time > 2x sprint median (BR6). Include stage breakdown and rework count. Sort by cycle time descending.

9. **ComputeMultiSprintMetrics** -- for multi-sprint mode:
   - Iterate over each sprint independently. For each sprint: filter its memberships by sub-team, filter its status transitions to that sprint's ticket set, call `ComputeTicketCycleTime` to get per-ticket cycle times, then call `ComputePercentiles` to get that sprint's P50/P85/throughput/outlier count. Store these per-sprint results in a list.
   - Metric cards: average of per-sprint P50 values, average of per-sprint P85 values, average of per-sprint throughput counts, average of per-sprint outlier counts (BR12: per-sprint averaging, not pooled). Delta = most recent sprint values vs the sprint before it.
   - Stage funnel: for each stage within boundaries, average its duration across all sprints. Compute percentage of total from the averaged values.
   - Trend entries: one per sprint with that sprint's P85, median, and ticket count.
   - Sprint summaries: one per sprint with that sprint's ticket count, median, P85, and outlier count.

**Sub-team filtering (BR10):** Follows the established `FilterMemberships` pattern from `SprintSummaryService`. Filter memberships by `m.Ticket?.Assignee?.SubTeam == subTeam`.

**Unrecognized stages (BR21):** Status names from transitions that don't match any current workflow stage are grouped under an "Other" segment in the funnel.

**Division by zero (BR15):** When no completed tickets exist, all metrics are zero, percentiles null, collections empty.

**Pattern reference:** `ScopeChangeService.cs` (same focused operation service pattern, single/multi split, metric card builder, sub-team filtering).

**Dependencies:** Step 1.

---

### Step 5: Create GET /api/analytics/cycle-time endpoint

**What:** Create the analytics endpoint that loads data and delegates to `CycleTimeService`.

**Follow:** `create-feature`

**Files to create:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetCycleTime/GetCycleTimeEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetCycleTime/GetCycleTimeQuery.cs`

**Route:** `GET /api/analytics/cycle-time`
**Tags:** `Analytics`
**Auth:** `AllowAnonymous`
**Type:** `Endpoint<GetCycleTimeRequest, CycleTimeResponse>`

**Request (in GetCycleTimeQuery.cs):**
```
GetCycleTimeRequest {
    int? SprintId
    int? Last
    string? SubTeam
}
```

**Validator (in GetCycleTimeQuery.cs):**
- `SprintId` > 0 when provided
- `Last` >= 0 when provided (0 means "all sprints", matching the existing scope-change convention)
- `SprintId` and `Last` mutually exclusive (400 if both present)
- `SubTeam` not empty when provided

**Endpoint logic (in GetCycleTimeEndpoint.cs):**

Constructor injection: `SprintRepository`, `TicketRepository`, `AppSettingsRepository`, `CycleTimeService`.

1. Load all closed sprints (lightweight, `GetClosedSprintsAsync`)
2. If no closed sprints: return `CycleTimeResponse("multi", null, null)`
3. Normalize sub-team
4. Load app settings
5. Determine mode:

**Single-sprint mode** (when `SprintId` provided):
- Find the sprint in closed sprints. 400 if not found.
- Identify prior sprint (next-earlier by start date) for delta calculation.
- Bulk load target + prior with memberships via `GetSprintsWithMembershipsAsync`.
- Load status transitions for all non-removed tickets in the target sprint via `GetStatusTransitionsForTicketsAsync`.
- Call `cycleTimeService.ComputeSingleSprint(...)`.
- Return `CycleTimeResponse("single", singleResult, null)`.

**Multi-sprint mode** (when `Last` provided or neither param):
- When neither param provided: default to all closed sprints (same as `last=0`).
- When `Last` is 0: all closed sprints.
- When `Last` > 0: take last N sprints ascending.
- Bulk load sprints with memberships.
- Load status transitions for all non-removed tickets across all loaded sprints.
- Call `cycleTimeService.ComputeMultiSprint(...)`.
- Return `CycleTimeResponse("multi", null, multiResult)`.

**Convention alignment:** This follows the same `last=0` means "all" convention as `GetScopeChangeEndpoint`. The frontend sends `last=0` for "All Sprints", matching the existing `sprintsStore.ts` pattern where `selectedLast === null` maps to `last: 0` in the API call.

**Pattern reference:** `GetScopeChange/GetScopeChangeEndpoint.cs` (same single/multi pattern, same data loading sequence).

**Performance approach:** Bulk load all status transitions for all relevant tickets in one query, not per-ticket. The `GetStatusTransitionsForTicketsAsync` method already supports a list of ticket IDs.

**Dependencies:** Step 4.

---

### Step 6: Add TypeScript types and API module for cycle time

**What:** Define all TypeScript interfaces for the cycle time API responses and create the API functions.

**Follow:** `create-vue-feature` (steps 1-2)

**Files to modify:**
- `client/src/types/index.ts` -- add all cycle time interfaces at the end

**Types to add:**

```typescript
// Cycle Time types
CycleTimeSprintInfo { id, name, startDate, endDate }
CycleTimeMetricCard { name, value, displayValue, delta, deltaDirection, deltaPolarity }
CycleTimeScatterPoint { ticketKey, ticketSummary, issueType, cycleTimeDays, completionDate, stageBreakdown: StageBreakdownEntry[], reworkCount }
StageBreakdownEntry { stageName, durationDays }
StageFunnelEntry { stageName, averageDurationDays, percentage }
CycleTimeIssueTypeEntry { issueType, ticketCount, medianCycleTime, p85CycleTime }
CycleTimeDeveloperEntry { displayName, avatarUrl, subTeam, ticketsCompleted, medianCycleTime, p85CycleTime, dominantStage }
CycleTimeOutlierEntry { ticketKey, ticketSummary, issueType, cycleTimeDays, stageBreakdown: StageBreakdownEntry[], reworkCount }
CycleTimeBoundaries { startStage, endStage }
CycleTimeSingleSprintResponse { sprint, metricCards, scatterPlot, stageFunnel, issueTypeBreakdown, developerBreakdown, outliers, boundaries }
CycleTimeTrendEntry { sprintId, sprintName, p85CycleTime, medianCycleTime, ticketsCompleted }
CycleTimeSprintSummaryEntry { sprintId, sprintName, startDate, ticketsCompleted, medianCycleTime, p85CycleTime, outlierCount }
CycleTimeMultiSprintResponse { sprints, metricCards, trend, stageFunnel, sprintSummaries, boundaries }
CycleTimeResponse { mode: 'single' | 'multi', singleSprint, multiSprint }
CycleTimeBoundariesResponse { startStage, endStage, availableStages: string[] }
SaveCycleTimeBoundariesResponse { startStage, endStage }
```

**Files to modify:**
- `client/src/api/analytics.ts` -- add `getCycleTime(sprintId?, last?, subTeam?)` function. For the `last` parameter: when provided, pass it as-is (0 = all sprints, matching scope-change convention). When undefined, omit from query params.

**Files to modify:**
- `client/src/api/settings.ts` -- add `getCycleTimeBoundaries(): Promise<CycleTimeBoundariesResponse>` and `saveCycleTimeBoundaries(startStage: string, endStage: string): Promise<SaveCycleTimeBoundariesResponse>` functions

**Pattern reference:** Existing types and API functions in the same files.

**Dependencies:** Steps 2, 3, 5 (API must exist for types to be accurate).

---

### Step 7: Create shared and single-sprint cycle time components

**What:** Create the Vue components used in the single-sprint drill-down view. `CycleTimeMetricCards` and `StageFunnel` are shared between both modes (they accept the same props shape from both single and multi responses). The remaining components are single-sprint only.

**Follow:** `vue-component-architecture`

**Files to create (all in `client/src/components/cycle-time/`):**

- `CycleTimeMetricCards.vue` (L3, props-only) -- four metric cards following the `MetricCard.vue` pattern from dashboard. Each card: name, display value, delta with polarity coloring. Wire tooltips from help.tooltips.md for each card.

- `CycleTimeScatterPlot.vue` (L3, props-only) -- scatter plot with ApexCharts. X-axis: completion date. Y-axis: cycle time days. Dots colored by issue type. Horizontal reference lines at P50 (dashed) and configurable percentile (solid, default P85). Hover tooltip: ticket key, summary, cycle time, stage breakdown. Rework badge on dots with reworkCount > 0. Wire tooltips from help.tooltips.md.

- `PercentileToggle.vue` (L4, atomic) -- toggle control with options P50, P75, P85, P90. Default P85. Emits selected percentile. Wire tooltip from help.tooltips.md.

- `StageFunnel.vue` (L3, props-only) -- horizontal stacked bar showing average time per stage. Widest segment visually highlighted as bottleneck. Each segment labeled with stage name and days. Wire tooltip from help.tooltips.md.

- `CycleTimeIssueTypeTable.vue` (L3, props-only) -- table: issue type, ticket count, median, P85. Wire tooltip from help.tooltips.md.

- `CycleTimeDeveloperTable.vue` (L3, props-only) -- table: developer name, tickets completed, median, P85, dominant stage. Sorted alphabetically. Wire tooltips from help.tooltips.md for the table and the dominant stage column.

- `CycleTimeOutlierTable.vue` (L3, props-only) -- table: ticket key, summary, issue type, cycle time, stage breakdown (inline), rework count. Sorted by cycle time descending. Wire tooltip from help.tooltips.md.

**Pattern reference:** `client/src/components/sprints/ScopeMetricCards.vue` for metric cards, `client/src/components/sprints/ClassificationTable.vue` for table structure.

**Dependencies:** Step 6 (types must exist).

---

### Step 8: Create multi-sprint cycle time components

**What:** Create the Vue components for the multi-sprint trend view: P85 trend line chart and per-sprint summary table.

**Follow:** `vue-component-architecture`

**Files to create (in `client/src/components/cycle-time/`):**

- `CycleTimeTrendChart.vue` (L3, props-only) -- line chart with ApexCharts. One point per sprint showing P85 cycle time. X-axis: sprint name. Y-axis: days. Wire tooltip from help.tooltips.md.

- `CycleTimeSprintSummaryTable.vue` (L3, props-only) -- table: sprint name, tickets completed, median, P85, outlier count. Rows are clickable (emit sprint-click event with sprintId). Wire tooltip from help.tooltips.md.

**Pattern reference:** `client/src/components/sprints/CarryOverRateChart.vue` for trend chart, `client/src/components/sprints/ZombieSummaryTable.vue` for clickable table rows.

**Dependencies:** Step 6 (types must exist).

---

### Step 9: Create cycleTimeStore

**What:** Create the Pinia store for the Cycle Time page. Manages sprint selection (single/multi mode), sub-team filter, percentile toggle state, and data fetching.

**Follow:** `pinia-patterns`

**Files to create:**
- `client/src/stores/cycleTimeStore.ts`

**Store name:** `'cycleTime'`
**Export:** `useCycleTimeStore`

**State:**
- `closedSprints: ClosedSprintItem[]`
- `subTeams: string[]`
- `boundaries: CycleTimeBoundariesResponse | null` (fetched on init, needed for empty state detection)
- `selectedSprintId: number | null`
- `selectedLast: number | null` (default `null` -- meaning single-sprint mode with most recent)
- `sprintMode: 'single' | 'multi'` (default `'single'`)
- `selectedSubTeam: string | null`
- `selectedPercentile: number` (default `85` -- single-sprint only, values: 50, 75, 85, 90)
- `cycleTime: CycleTimeResponse | null`
- `loading: boolean`
- `initializing: boolean`
- `error: string | null`

**Computed:**
- `hasWorkflowStages: boolean` -- `boundaries.value !== null && boundaries.value.availableStages.length > 0`. Used by the view for the empty state check (spec BR13: no workflow stages configured).

**Actions:**
- `initialize()` -- load closed sprints + sub-teams + cycle-time boundaries in parallel (three calls via `Promise.all`), set default sprint (most recent closed), then fetch data
- `selectSprint(sprintId)` -- set single mode, fetch data
- `selectLastN(n: number | null)` -- set multi mode (null = all), fetch data
- `selectSubTeam(subTeam: string | null)` -- update filter, re-fetch
- `selectPercentile(p: number)` -- update local state (no re-fetch, purely UI)
- `fetchData()` -- call `getCycleTime(sprintId?, last?, subTeam?)`

**API call mapping in `fetchData()`:**
```
sprintMode === 'single' → getCycleTime(selectedSprintId, undefined, subTeam)
sprintMode === 'multi'  → getCycleTime(undefined, selectedLast === null ? 0 : selectedLast, subTeam)
```
This matches the existing `sprintsStore.ts` pattern where `selectedLast === null` maps to `last: 0` in the API call (meaning "all sprints"). The cycle-time endpoint follows the same `last=0` convention as scope-change.

**Pattern reference:** `client/src/stores/sprintsStore.ts` (same single/multi pattern, same initialize sequence, same `last=0` mapping).

**Key difference from sprintsStore:** Default mode is `'single'` with most recent sprint (not multi with last 5). The spec says "the sprint selector defaults to the most recent closed sprint."

**Dependencies:** Step 6.

---

### Step 10: Create CycleTimeView and wire route + navigation

**What:** Create the page view component, add the route, and add the sidebar navigation entry.

**Follow:** `create-vue-feature` (steps 5-7)

**Files to create:**
- `client/src/views/CycleTimeView.vue` (L0, < 100 lines)

**View structure:**
- Three-state template: initializing -> empty (no closed sprints OR no workflow stages configured) -> content
- Empty state for no workflow stages: check `store.hasWorkflowStages` (from boundaries fetch in Step 9), direct user to Settings (spec BR13)
- PageToolbar with sprint selector (showAggregateOptions: true), sub-team filter
- PercentileToggle visible only in single-sprint mode (BR22)
- URL sync via `watch` on `[store.sprintMode, store.selectedSprintId, store.selectedLast]` (same pattern as SprintsView)
- onMounted: seed state from URL query params, then `store.initialize()`

**Single-sprint mode components** (rendered when `store.cycleTime?.mode === 'single' && store.cycleTime.singleSprint`):
1. `CycleTimeMetricCards` -- `:metric-cards="store.cycleTime.singleSprint.metricCards"` `:selected-percentile="store.selectedPercentile"`
2. `PercentileToggle` -- `v-model="store.selectedPercentile"`
3. `CycleTimeScatterPlot` -- `:data-points="store.cycleTime.singleSprint.scatterPlot"` `:selected-percentile="store.selectedPercentile"`
4. `StageFunnel` -- `:stages="store.cycleTime.singleSprint.stageFunnel"`
5. `CycleTimeIssueTypeTable` -- `:entries="store.cycleTime.singleSprint.issueTypeBreakdown"`
6. `CycleTimeDeveloperTable` -- `:entries="store.cycleTime.singleSprint.developerBreakdown"`
7. `CycleTimeOutlierTable` -- `:entries="store.cycleTime.singleSprint.outliers"`

**Multi-sprint mode components** (rendered when `store.cycleTime?.mode === 'multi' && store.cycleTime.multiSprint`):
1. `CycleTimeMetricCards` -- `:metric-cards="store.cycleTime.multiSprint.metricCards"` (same component, reused)
2. `CycleTimeTrendChart` -- `:trend="store.cycleTime.multiSprint.trend"`
3. `StageFunnel` -- `:stages="store.cycleTime.multiSprint.stageFunnel"` (same component, reused)
4. `CycleTimeSprintSummaryTable` -- `:summaries="store.cycleTime.multiSprint.sprintSummaries"` `@sprint-click="onSprintBarClick"`

Note: `CycleTimeMetricCards` and `StageFunnel` are shared between both modes. `PercentileToggle` is single-sprint only (BR22). `CycleTimeScatterPlot`, breakdown tables, and outlier table are single-sprint only. `CycleTimeTrendChart` and `CycleTimeSprintSummaryTable` are multi-sprint only.

**Files to modify:**
- `client/src/router.ts` -- add route `{ path: '/cycle-time', name: 'cycle-time', component: () => import('./views/CycleTimeView.vue') }`
- `client/src/components/AppSidebar.vue` -- add "Cycle Time" nav item between "Epics" and "Settings" with a clock/timer icon

**Pattern reference:** `client/src/views/SprintsView.vue` (same structure: URL sync, mode switching, toolbar wiring).

**Dependencies:** Steps 7, 8, 9.

---

### Step 11: Add cycle time boundary settings UI to SettingsView

**What:** Add the "Cycle Time Boundaries" section to the existing Settings page, below the Workflow Stages section. Two dropdowns: "Cycle starts at" and "Cycle ends at". Independent save following the excluded-statuses pattern.

**Files to modify:**
- `client/src/views/SettingsView.vue`

**UI additions (new `<section>` after Workflow Stages):**
- Section title: "Cycle Time Boundaries"
- Description text: "Configure which workflow stages mark the start and end of cycle time measurement."
- Two `<select>` dropdowns populated from `availableStages` (workflow stages + done statuses, fetched from `GET /api/settings/cycle-time-boundaries`)
- Default display when no boundaries saved: show resolved defaults (2nd workflow stage, 1st done status)
- "Save Boundaries" button that calls `PUT /api/settings/cycle-time-boundaries`
- Success/error feedback (same pattern as the main save)
- Wire tooltips from help.tooltips.md: "Cycle Starts At (Settings)" and "Cycle Ends At (Settings)"

**State additions to SettingsView.vue `<script setup>`:**
- `boundaryStartStage` ref
- `boundaryEndStage` ref
- `boundaryAvailableStages` ref
- `boundariesLoading` ref
- `boundariesSaving` ref
- `boundariesSaved` ref
- `boundariesError` ref
- Fetch boundaries on mount (add to `Promise.allSettled` block)
- `saveBoundaries()` function

**Pattern reference:** The excluded-statuses section is not in SettingsView (it uses dedicated endpoints). Follow the pattern of independent save with local state, similar to how the main save works but scoped to boundaries only.

**Dependencies:** Steps 2, 3 (backend endpoints must exist).

---

### Step 12: Build and verify

**What:** Run both backend and frontend builds to verify everything compiles.

**Files:** No new files.

**Commands:**
```bash
dotnet build src/Services/Fokus/Fokus.API
cd client && npm run build
```

**Dependencies:** All previous steps.

## Migration Notes

**Migration command (run after Step 1):**
```
dotnet ef migrations add AddCycleTimeBoundaries -p Services/Fokus/Fokus.Persistence -s Services/Fokus/Fokus.API
```

No seed data needed. The two new columns are nullable with `null` as the default. The application resolves defaults at query time (BR13).

## Testing Strategy

**Backend:**
- Verify boundaries default resolution: when no boundaries set, second workflow stage and first done status are returned
- Verify PUT validation: 400 when start stage does not precede end stage
- Verify PUT validation: 400 when stage name not in configured stages
- Verify single-sprint with known data: correct P50, P85, throughput, outlier count
- Verify sprint-start/end clamping: ticket in measured stage at sprint boundaries uses clamped times
- Verify rework counting: ticket entering a stage 3 times has rework count 2
- Verify tickets never entering start stage are excluded
- Verify tickets already done before sprint window are excluded
- Verify sub-team filtering scopes all calculations
- Verify multi-sprint averaging: per-sprint averages, not pooled
- Verify 400 when both sprintId and last provided
- Verify 200 with null data when no closed sprints exist
- Verify unrecognized stage names appear as "Other" in funnel

**Frontend:**
- Page loads with most recent closed sprint selected
- Sprint selector switches between single and multi modes
- Metric cards display with correct polarity (lower cycle time = green)
- Scatter plot renders dots colored by issue type
- Percentile toggle changes reference line and metric card
- Sub-team filter recalculates all content
- Multi-sprint trend chart renders one point per sprint
- Sprint summary table rows navigate to single-sprint view
- Settings boundary dropdowns populate from workflow stages + done statuses
- Empty state shown when no workflow stages configured
- URL reflects sprint selection and is linkable

## Open Questions

None.
