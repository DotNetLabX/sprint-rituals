# Test Execution Timeline — Implementation

## Files Created

- `src/Services/Fokus/Fokus.API/Features/Analytics/TestTimelineService.cs` — all response records (TestTimelineResponse and 10 sub-types + TestingCrunchFlag) and TestTimelineService with ComputeTimeline (instance) and ComputeCrunchFlag (public static). Private helpers for each sub-computation. Internal TerminalRunEntry record.
- `src/Services/Fokus/Fokus.API/Features/Sprints/GetTestTimeline/GetTestTimelineQuery.cs` — GetTestTimelineRequest (SprintId route param, SubTeam query param) and GetTestTimelineRequestValidator.
- `src/Services/Fokus/Fokus.API/Features/Sprints/GetTestTimeline/GetTestTimelineEndpoint.cs` — GET /api/sprints/{sprintId}/test-timeline. Loads sprint, TEs, memberships, transitions, prior sprint. Returns hasQaData=false when Xray disabled or zero runs.
- `client/src/components/sprints/TestExecutionBurnupChart.vue` — ApexCharts line chart with 3 cumulative series, phase background annotations, scope change vertical markers, post-sprint separator, custom dark tooltip.
- `client/src/components/sprints/TestingCrunchSection.vue` — conditional render when isCrunchFlagged, warning header with percentage/counts, expandable ticket table.
- `client/src/components/sprints/PostSprintTestingSection.vue` — conditional render when hasPostSprintTesting, warning header, expandable ticket table sorted by postSprintRunCount desc.
- `client/src/components/sprints/UntestedAtCloseSection.vue` — conditional render when hasUntestedAtClose, warning header with count, expandable ticket table with devDoneDate formatted.
- `client/src/components/sprints/DevToTestGapSection.vue` — always renders, median gap metric with delta arrow (down=green/good, up=red/bad), expandable per-ticket table sorted by gapDays desc.
- `docs/kb/analytics/test-timeline.md` — business rules, computation formulas, key file paths, relationship to F26.

## Files Modified

- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` — registered TestTimelineService as scoped.
- `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs` — added TestingCrunchFlag? TestingCrunch to FlagsResult, extended ComputeSummary/ComputeFlags signatures, updated hasFlags computation.
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetSprintSummary/GetSprintSummaryEndpoint.cs` — calls TestTimelineService.ComputeCrunchFlag (static) in Xray block, passes testingCrunch to ComputeSummary. Removed testTimelineService from constructor (static call, no DI needed).
- `client/src/types/index.ts` — added TestingCrunchFlag interface, testingCrunch field to FlagsResult, all 10 F30 TypeScript interfaces (BurnupDayEntry through TestTimelineResponse).
- `client/src/api/analytics.ts` — added getTestTimeline function calling /sprints/{sprintId}/test-timeline.
- `client/src/stores/sprintsStore.ts` — added testTimeline ref, fetched in parallel in single-sprint mode, cleared in multi mode, added to return object.
- `client/src/views/SprintsView.vue` — imported 5 new components, added sprintEndDayNumber computed, added Test Execution Timeline section (single-sprint only, gated on isXrayEnabled with sync prompt when !hasQaData).
- `client/src/components/dashboard/SprintFlags.vue` — added Testing Crunch flag block after Zero-SP Developers, shows percentage/counts, InfoTooltip wired from help.tooltips.md.
- `docs/kb/index.md` — added Test Timeline entry under Analytics section.

## Key Decisions

- **ComputeCrunchFlag as public static:** Dashboard endpoint needs crunch flag without loading full timeline data. Static method on TestTimelineService avoids creating a second DI dependency chain. Mirrors the qualitySubScore optional-parameter pattern already in ComputeSummary.
- **Endpoint namespace isolation:** GetTestTimelineEndpoint is in Fokus.API.Features.Sprints.GetTestTimeline but response types are in Fokus.API.Features.Analytics. GlobalUsings does not include Analytics namespace, so explicit `using Fokus.API.Features.Analytics;` added at top of endpoint file.
- **SprintMembership.AddedAt non-nullable:** AddedAt is DateTime (not DateTime?). Removed .HasValue/.Value guards found initially — confirmed by reading SprintMembership entity.
- **sprintEndDayNumber computed in SprintsView:** Derived from sprintStartDate/sprintEndDate in the testTimeline response rather than adding a field to the response. Keeps the backend lean.

## Deviations from Plan

- **testTimelineService removed from GetSprintSummaryEndpoint constructor:** Plan said "Add TestTimelineService as a dependency" but ComputeCrunchFlag is static — no instance needed. Removed the injected parameter to eliminate CS9113 compiler warning. Static call is cleaner and correct.

## Review Fixes (Cycle 1)

**HIGH-1 — Prior sprint transitions bug (fixed):**
- `GetTestTimelineEndpoint.cs` — loads transitions for both sprint IDs in one call (`GetStatusTransitionsForSprintTicketsAsync([sprint.Id, priorSprint.Id])`), splits by ticket membership into separate `statusTransitions` and `priorStatusTransitions` lists.
- `TestTimelineService.cs` — `ComputeTimeline` signature extended with `List<StatusTransition>? priorStatusTransitions`. Builds a separate `priorTransitionsByTicket` dictionary. `ComputeDevToTestGap` receives this distinct dictionary. Guard extended: delta only computed when `priorTransitionsByTicket is not null`.

**HIGH-2 — Missing "Xray enabled, not synced" empty state (fixed):**
- `TestTimelineResponse` record — added `IsXrayEnabled` bool field. Set `false` when Xray disabled, `true` otherwise.
- `BuildEmptyResponse` — accepts `isXrayEnabled` parameter.
- `SprintsView.vue` — section gates on `isXrayEnabled`. Shows "Sync sprint to load QA data." prompt when `isXrayEnabled && !hasQaData`. Hidden when `!isXrayEnabled`.
- `types/index.ts` — added `isXrayEnabled: boolean` to `TestTimelineResponse`.

**LOW — sealed records (fixed):** All 11 public response records marked `sealed`.

**LOW — IReadOnlyList<T> (fixed):** Collection properties on all response records changed from `List<T>` to `IReadOnlyList<T>`.
