# Scope Change & Disruption — Implementation

## Files Created

- `src/Services/Fokus/Fokus.API/Features/Settings/GetExcludedStatuses/GetExcludedStatusesEndpoint.cs` — GET /api/settings/excluded-statuses; extends EndpointWithoutRequest<List<string>>, returns ExcludedFromScopeStatuses from AppSettings
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveExcludedStatuses/SaveExcludedStatusesEndpoint.cs` — PUT /api/settings/excluded-statuses; includes request type, validator (NotEmpty on each status), and handler that loads/replaces/saves the list
- `src/Services/Fokus/Fokus.API/Features/Analytics/ScopeChangeService.cs` — focused computation service + all response/DTO records; implements ComputeMultiSprint and ComputeSingleSprint, sub-team filtering (C2), excluded-status filtering (case-insensitive), classification priority chain, delta computation (C1), burnup chart data, event table, bug time-in-progress
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetScopeChange/GetScopeChangeQuery.cs` — GetScopeChangeRequest and validator (SprintId > 0, Last >= 1, mutual exclusivity, SubTeam not empty when provided)
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetScopeChange/GetScopeChangeEndpoint.cs` — GET /api/analytics/scope-change; orchestrates SprintRepository, TicketRepository, AppSettingsRepository, ScopeChangeService; returns ScopeChangeResponse wrapper with mode field
- `src/Services/Fokus/Fokus.Persistence/Migrations/<timestamp>_AddExcludedFromScopeStatuses.cs` — EF Core migration adding ExcludedFromScopeStatuses column (TEXT, JSON array)
- `client/src/stores/sprintsStore.ts` — Pinia store for Sprints page; defaults to multi-sprint mode with last=5 (unlike developersStore which defaults to single); initialize, selectSprint, selectLastN, selectSubTeam, fetchScopeChange actions
- `client/src/components/sprints/ScopeMetricCards.vue` — renders summary cards (multi) or detail metric cards with delta arrows (single)
- `client/src/components/sprints/ClassificationTable.vue` — classification breakdown table (used in both multi and single views)
- `client/src/components/sprints/ScopeChangeChart.vue` — grouped bar chart (Committed/Added/Removed/Completed SP per sprint) + line chart (disruption rate trend); bar click emits sprint-click event
- `client/src/components/sprints/BurnupChart.vue` — area/line chart; planning-phase annotation at end of day 2
- `client/src/components/sprints/EventTable.vue` — chronological event table; excluded items rendered at opacity-50 with "excluded" badge
- `client/src/components/sprints/BugTimeTable.vue` — bug time-in-progress table; hidden via v-if when bugs is empty

## Files Modified

- `src/Services/Fokus/Fokus.Domain/Settings/AppSettings.cs` — added ExcludedFromScopeStatuses List<string> property (default []) and included it in CreateDefault()
- `src/Services/Fokus/Fokus.Persistence/Configurations/AppSettingsConfiguration.cs` — added JSON converter for ExcludedFromScopeStatuses using same pattern as DoneStatuses and WorkflowStages
- `src/Services/Fokus/Fokus.Persistence/Repositories/AppSettingsRepository.cs` — added explicit ExcludedFromScopeStatuses assignment in SaveAsync (same pattern as DoneStatuses/WorkflowStages — required because SetValues does not copy reference-type list properties)
- `src/Services/Fokus/Fokus.Persistence/Repositories/TicketRepository.cs` — added GetStatusTransitionsForTicketsAsync(List<string> ticketIds) method for bulk-loading status transitions by ticket ID
- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` — registered ScopeChangeService as scoped alongside existing analytics services
- `client/src/types/index.ts` — added all TypeScript interfaces: ScopeChangeSprintInfo, ScopeChangeSummaryMetrics, ScopeChangePerSprintData, ClassificationEntry, ScopeChangeMultiSprintResponse, ScopeMetricCard, ScopeChangeSingleSprintMetrics, BurnupDataPoint, ScopeChangeEvent, BugTimeInProgress, ScopeChangeSingleSprintResponse, ScopeChangeResponse
- `client/src/api/analytics.ts` — added getScopeChange function with optional sprintId/last/subTeam query params
- `client/src/api/settings.ts` — added getExcludedStatuses and saveExcludedStatuses functions
- `client/src/views/SprintsView.vue` — complete rewrite; URL sync from/to route query, store wiring, PageToolbar with showAggregateOptions, conditional multi/single view rendering, sprint bar click navigation

## Key Decisions

- AppSettingsRepository.SaveAsync: plan noted the risk that SetValues won't copy reference-type lists. Confirmed existing pattern in the codebase — DoneStatuses and WorkflowStages are both explicitly reassigned. Added the same explicit assignment for ExcludedFromScopeStatuses.
- ScopeChangeResponse wrapper: used a discriminated wrapper record (mode + nullable MultiSprint + nullable SingleSprint) as specified. Endpoint type is Endpoint<TReq, ScopeChangeResponse>.
- Status transitions loaded for all non-removed tickets (not just mid-sprint bugs). The burnup BuildBurnupData method builds a doneTransitionByTicket lookup from these transitions to place each ticket's completion on the correct sprint day, as the plan requires. ComputeBugTimeInProgress filters internally to mid-sprint bugs only. A single query via GetStatusTransitionsForTicketsAsync covers both uses.
- GetActiveStatuses heuristic: when WorkflowStages is non-empty and has more than 2 stages, middle stages (between first and last) are treated as active. This matches the workflow model where first = backlog, last = done. When empty, defaults to "In Progress".
- sprintsStore defaults to multi/last=5: distinct from developersStore which defaults to single. Matches spec "Sprints page defaults to Last 5."

## Review Fix Round 1

### Files Modified (fixes)

- `src/Services/Fokus/Fokus.Persistence/Migrations/20260509080338_AddExcludedFromScopeStatuses.cs` — CRITICAL fix: changed `defaultValue: ""` to `defaultValue: "[]"` so existing rows get valid JSON. Added safety-net `Sql("UPDATE AppSettings SET ExcludedFromScopeStatuses = '[]' WHERE ExcludedFromScopeStatuses = '';")` for instances where the migration was previously applied with the wrong default.
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetScopeChange/GetScopeChangeQuery.cs` — HIGH fix (part 1): relaxed validator from `GreaterThanOrEqualTo(1)` to `GreaterThanOrEqualTo(0)` so `last=0` passes validation as the "all sprints" sentinel.
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetScopeChange/GetScopeChangeEndpoint.cs` — HIGH fix (part 2): changed multi-sprint branch to treat `last == 0` as "all sprints" (`last == 0 ? ascending : ascending.TakeLast(last).ToList()`).
- `client/src/stores/sprintsStore.ts` — HIGH fix (part 3): when `selectedLast` is `null` (user selected "All Sprints"), send `last=0` to the backend instead of `undefined`. Eliminates the `null ?? undefined` → no-param → server-default-5 bug.

## Deviations from Plan

- None. All plan steps implemented as specified. Review fixes are additive corrections only.
