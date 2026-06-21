# Cycle Time — Implementation

## Files Created

- `src/Services/Fokus/Fokus.Domain/Settings/AppSettings.cs` — Added `CycleTimeStartStage` and `CycleTimeEndStage` nullable string properties and included them in `CreateDefault()`
- `src/Services/Fokus/Fokus.Persistence/Migrations/*_AddCycleTimeBoundaries.cs` — EF Core migration adding two nullable TEXT columns to AppSettings table
- `src/Services/Fokus/Fokus.API/Features/Settings/GetCycleTimeBoundaries/GetCycleTimeBoundariesEndpoint.cs` — GET /api/settings/cycle-time-boundaries; resolves defaults per BR13 (start = 2nd workflow stage, end = 1st done status); returns resolved values + available stages list
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveCycleTimeBoundaries/SaveCycleTimeBoundariesEndpoint.cs` — PUT /api/settings/cycle-time-boundaries with validator; validates both stages exist in ordered list; validates start precedes end; returns saved boundaries
- `src/Services/Fokus/Fokus.API/Features/Analytics/CycleTimeService.cs` — Full focused operation service with all response records and computation logic: boundary resolution, per-ticket cycle time (sprint clamping, rework counting, exclusion rules BR1-5/BR18/BR19), percentile calculation, metric cards with delta/polarity, scatter plot, stage funnel (including "Other" for BR21), issue type breakdown, developer breakdown with dominant stage, outlier table; ComputeMultiSprint with per-sprint averaging (BR12), trend entries, sprint summaries, averaged stage funnel
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetCycleTime/GetCycleTimeQuery.cs` — Request class and validator matching scope-change convention (SprintId, Last, SubTeam with mutual exclusion rule)
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetCycleTime/GetCycleTimeEndpoint.cs` — GET /api/analytics/cycle-time; single/multi mode dispatch; bulk loads transitions for both target and prior sprint in single-sprint mode; last=0 means all sprints
- `client/src/components/cycle-time/CycleTimeMetricCards.vue` — Four metric cards with delta/polarity coloring; tooltip text on card name labels
- `client/src/components/cycle-time/CycleTimeScatterPlot.vue` — ApexCharts scatter plot; series grouped by issue type for color; P50 dashed + Pselected solid annotations; custom hover tooltip with stage breakdown and rework count
- `client/src/components/cycle-time/PercentileToggle.vue` — Button group P50/P75/P85/P90, default P85, emits update:modelValue
- `client/src/components/cycle-time/StageFunnel.vue` — Horizontal stacked bar; bottleneck highlighted red; legend with stage name + avg days; tooltip on container
- `client/src/components/cycle-time/CycleTimeIssueTypeTable.vue` — Table: issue type, tickets, median, P85
- `client/src/components/cycle-time/CycleTimeDeveloperTable.vue` — Table: developer (with avatar), tickets, median, P85, dominant stage; tooltip on dominant stage header
- `client/src/components/cycle-time/CycleTimeOutlierTable.vue` — Table: ticket key, summary, type, cycle time, inline stage breakdown, rework badge with tooltip
- `client/src/components/cycle-time/CycleTimeTrendChart.vue` — ApexCharts line chart; P85 solid + median dashed; tooltip
- `client/src/components/cycle-time/CycleTimeSprintSummaryTable.vue` — Clickable rows emit sprint-click; outlier count badge
- `client/src/stores/cycleTimeStore.ts` — Pinia store: single default mode with most recent sprint; initialize loads sprints+subTeams+boundaries in parallel; fetchData maps single/multi correctly (last=0 for all); hasWorkflowStages computed from boundaries
- `client/src/views/CycleTimeView.vue` — Three-state view (initializing/empty/content); two empty states (no sprints, no workflow stages); URL sync watch; single-sprint and multi-sprint component layouts; PercentileToggle single-sprint only (BR22)

## Files Modified

- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` — Added `services.AddScoped<CycleTimeService>()`
- `client/src/types/index.ts` — Added all cycle time TypeScript interfaces (16 types)
- `client/src/api/analytics.ts` — Added `getCycleTime()` function; updated import
- `client/src/api/settings.ts` — Added `getCycleTimeBoundaries()` and `saveCycleTimeBoundaries()` functions; updated import
- `client/src/router.ts` — Added `/cycle-time` route before `/settings`
- `client/src/components/AppSidebar.vue` — Added "Cycle Time" nav item with clock icon between Epics and Settings
- `client/src/views/SettingsView.vue` — Added boundary state refs, saveBoundaries(), getCycleTimeBoundaries fetch in allSettled block, Cycle Time Boundaries section with two dropdowns and save button; tooltip wiring on both labels

## Key Decisions

- `CycleTimeService.ComputeSingleSprint` receives status transitions for both target and prior sprint (loaded together in endpoint), allowing prior sprint delta computation without an extra DB call.
- Stage time accumulation walks transitions using an index-based loop rather than a dictionary of "last entry time" to correctly handle rework (re-entries). Each transition's ToStatus is measured from that transition until the next.
- Sprint-start clamping is applied at enter time: if `transition.Timestamp < sprintStart`, enter is clamped to `sprintStart`. Sprint-end clamping is applied at exit time: the next transition or sprint end, whichever is earlier.
- `BuildDeveloperBreakdown` reads assignee from `sprint.Memberships` navigation (already loaded by `GetSprintsWithMembershipsAsync`), not a separate developer load.
- `avgThroughput` and `avgOutliers` from `List<int>.Average()` return `double`; cast to `decimal` before passing to `BuildCard`.
- Tooltips are wired via native HTML `title` attributes on the relevant label/heading elements, following the help-tooltips rule.

## Deviations from Plan

- Plan says default for multi-sprint mode when neither param is given is "all closed sprints (same as last=0)". Implemented exactly: `var last = req.Last ?? 0`, so omitting both params defaults to `last=0` = all sprints.
- `GetCycleTimeBoundaries` returns `startStage = ""` when no workflow stages are configured at all (not an error, frontend checks `workflowStageCount`). This matches BR13 / spec behavior.
- The `AppSettingsRepository.SaveAsync` was not modified (plan confirmed `SetValues` handles nullable strings correctly — verified by reading the existing code).

## Fixes — Review Cycle 1

### [HIGH] BR13 empty state never triggers when done statuses exist but workflow stages are empty

**Files modified:**
- `src/Services/Fokus/Fokus.API/Features/Settings/GetCycleTimeBoundaries/GetCycleTimeBoundariesEndpoint.cs` — Added `int WorkflowStageCount` to `GetCycleTimeBoundariesResponse` record; populated from `settings.WorkflowStages.Count` in the handler
- `client/src/types/index.ts` — Added `workflowStageCount: number` to `CycleTimeBoundariesResponse` interface
- `client/src/stores/cycleTimeStore.ts` — Changed `hasWorkflowStages` computed from `boundaries.value.availableStages.length > 0` to `boundaries.value.workflowStageCount > 0`; `availableStages` includes done statuses (which always have defaults), so checking it masked the empty-workflow-stages condition

### [MEDIUM] BR21 "Other" funnel bucket unreachable

**File modified:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/CycleTimeService.cs` — In `ComputeTicketCycleTime`, when `stageIdx == -1` (stage name not present in `orderedStages` — the renamed/removed stage scenario), duration is now accumulated into `stageDurations["__Other__"]` using the same clamping logic as boundary stages. The `"__Other__"` sentinel key falls into the `else` branch of `BuildStageFunnel`'s loop (not found in `configuredWorkflowStages` or `orderedStages`), which adds it to `otherSum` and emits the "Other" funnel entry when non-zero. No change to `BuildStageFunnel` was required.
