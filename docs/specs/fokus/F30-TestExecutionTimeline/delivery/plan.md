# Test Execution Timeline

**Feature Spec:** `docs/specs/F30-TestExecutionTimeline/definition/spec.md`

## Context

F25 introduced Xray integration with TestExecution, TestRun, and TestExecutionLink entities. F26 surfaces sprint-level QA metrics (coverage, pass rate). F30 answers a different question: **when** did testing happen relative to the sprint lifecycle? It adds a test execution burnup chart, testing crunch flag, post-sprint testing indicator, completed-but-untested list, and dev-done-to-tested gap metric — all on the Sprints page single-sprint detail view. It also extends the Dashboard flags with a testing crunch flag.

**Services impacted:** Fokus (single service). No new entities, no migrations, no settings changes. F30 reads from entities introduced by F25 (TestRun, TestExecution, TestExecutionLink) and leverages SprintMembership (F10 scope change) and StatusTransition (F12 cycle time).

## Scope

**In scope:**
- New `GET /api/sprints/{sprintId}/test-timeline` endpoint with `?subTeam=X` support
- `TestTimelineService` computation service (burnup, scope overlay, crunch, post-sprint, untested, dev-to-test gap)
- Extension of `GET /api/analytics/sprint-summary` — add `testingCrunch` to FlagsResult
- Frontend: "Test Execution Timeline" section on SprintsView (single-sprint only)
- Frontend: testing crunch flag on Dashboard SprintFlags component
- Frontend: types, API module function, store extension
- Help tooltips from `docs/specs/F30-TestExecutionTimeline/definition/help.tooltips.md`
- Empty states (Xray disabled, not synced, zero runs)

**Out of scope:**
- Multi-sprint test execution trends (F27)
- Per-developer testing timing breakdown (F29)
- Configurable testing crunch threshold
- Configurable crunch percentage threshold
- Testing crunch as a health sub-score
- Test execution burndown
- Ideal/expected execution line
- Per-test-run detail drill-down
- Gantt-style per-TE timeline

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | (none) | — | TestTimelineService: burnup computation, scope overlay, crunch flag, post-sprint, untested, dev-to-test gap | Log: no skill for metric computation patterns |
| 2 | create-feature | Follow | GetTestTimeline endpoint (GET /api/sprints/{sprintId}/test-timeline), FastEndpoints | |
| 3 | (none) | — | Extend SprintSummaryService FlagsResult with testingCrunch field | Log: no skill for extending existing response models |
| 4 | (none) | — | Frontend types for TestTimelineResponse, API function, sprintsStore extension | Log: no skill for frontend type/store patterns |
| 5 | vue-patterns | Follow | TestExecutionTimeline section container + burnup chart component | |
| 6 | vue-patterns | Follow | Crunch, post-sprint, untested, gap detail components | |
| 7 | vue-patterns | Follow | SprintFlags testing crunch flag integration | |

## Domain Model Changes

None. F30 reads from existing entities: TestExecution, TestRun, TestExecutionLink, Sprint, SprintMembership, StatusTransition, Developer, AppSettings. No new entities, value objects, or domain events.

## Data Model Changes

None. No new tables, columns, or migrations required.

## Implementation Steps

### Step 1: Create TestTimelineService

Create the computation service that calculates all test execution timeline metrics. Pure computation — receives pre-loaded data, returns result records.

**No matching skill** — metric computation patterns are a gap.

**Files:**
- Create: `src/Services/Fokus/Fokus.API/Features/Analytics/TestTimelineService.cs`
- Modify: `src/Services/Fokus/Fokus.API/DependencyInjection.cs` — register `TestTimelineService` as scoped

**Records to define** (in the same file, above the service class — following QaMetricsService / QaWorkloadService pattern):

**Response records:**

- `TestTimelineResponse` — hasQaData (bool), sprintStartDate (DateTime), sprintEndDate (DateTime), planningWindowDays (int), burnupData (List\<BurnupDayEntry\>), scopeChangeOverlay (List\<ScopeChangeDayEntry\>), testingCrunch (TestingCrunchResult), postSprintTesting (PostSprintTestingResult), untestedAtClose (UntestedAtCloseResult), devToTestGap (DevToTestGapResult)

- `BurnupDayEntry` — dayNumber (int), calendarDate (DateTime), isWithinSprint (bool), cumulativePass (int), cumulativeFail (int), cumulativeTotal (int), dailyPass (int), dailyFail (int)

- `ScopeChangeDayEntry` — dayNumber (int), calendarDate (DateTime), addedSp (decimal), removedSp (decimal), netSp (decimal)

- `TestingCrunchResult` — isCrunchFlagged (bool), crunchPercentage (decimal?), crunchRunCount (int), totalRunCount (int), crunchTickets (List\<CrunchTicketEntry\>)

- `CrunchTicketEntry` — ticketKey (string), summary (string), assigneeName (string?), storyPoints (decimal?), lateRunCount (int)

- `PostSprintTestingResult` — hasPostSprintTesting (bool), postSprintPercentage (decimal?), postSprintRunCount (int), totalRunCount (int), postSprintTickets (List\<PostSprintTicketEntry\>)

- `PostSprintTicketEntry` — ticketKey (string), summary (string), assigneeName (string?), storyPoints (decimal?), postSprintRunCount (int)

- `UntestedAtCloseResult` — hasUntestedAtClose (bool), untestedAtCloseCount (int), untestedAtCloseTickets (List\<UntestedTicketEntry\>)

- `UntestedTicketEntry` — ticketKey (string), summary (string), assigneeName (string?), storyPoints (decimal?), devDoneDate (DateTime)

- `DevToTestGapResult` — medianGapDays (decimal?), medianGapDelta (decimal?), medianGapDirection (string?), gapTickets (List\<GapTicketEntry\>)

- `GapTicketEntry` — ticketKey (string), summary (string), assigneeName (string?), devDoneDate (DateTime), firstTestDate (DateTime), gapDays (decimal)

- `TestingCrunchFlag` — crunchPercentage (decimal), crunchRunCount (int), totalRunCount (int). Used for the Dashboard flags extension.

**Service method signature:**

```
public TestTimelineResponse ComputeTimeline(
    List<TestExecution> sprintTEs,
    List<SprintMembership> memberships,
    List<StatusTransition> statusTransitions,
    Sprint sprint,
    Sprint? priorSprint,
    List<TestExecution>? priorSprintTEs,
    List<SprintMembership>? priorMemberships,
    AppSettings settings,
    string? subTeam)
```

**Business rules to implement (all referenced from spec):**

1. **Test run effective date:** FinishedAt ?? StartedAt. Exclude runs where both are null.
2. **Terminal-status filter:** Only PASS or FAIL runs plotted/counted.
3. **Cancelled TE exclusion:** TEs with Status == "Cancelled" excluded (already handled at repository level, but verify no cancelled TEs leak through).
4. **Sprint day assignment:** Day 1 = sprint StartDate. dayNumber = (effectiveDate.Date - sprint.StartDate.Date).Days + 1. Days after EndDate are post-sprint (isWithinSprint = false).
5. **Burnup cumulation:** Accumulate day-over-day. Green=PASS, Red=FAIL, Gray=Total. Only increases.
6. **Scope change overlay:** From SprintMembership AddedAt/RemovedAt, aggregate per-day SP added/removed/net. Only days with events included.
7. **Testing crunch:** Denominator = terminal runs within sprint boundary (StartDate through EndDate inclusive). Numerator = terminal runs in last 2 calendar days before EndDate (inclusive). Flag fires when percentage > 50. Zero denominator = null percentage, flag does not fire. Post-sprint runs excluded from both.
8. **Post-sprint testing:** Runs with effective date > EndDate. Percentage denominator = all terminal runs (within-sprint + post-sprint). Hidden when zero post-sprint runs.
9. **Completed but untested:** Ticket reached CycleTimeEndStage before EndDate (using StatusTransition data) AND has zero terminal-status test runs with effective date <= EndDate. Sort by dev-done date ascending.
10. **Dev-to-test gap:** Dev-done = earliest transition to CycleTimeEndStage. Tested = earliest FinishedAt among terminal-status runs linked to that ticket (via TestExecutionLinks with linkType=Tests). Gap = (firstTestDate - devDoneDate).TotalDays. Median across eligible tickets. Negative gaps valid. Delta vs prior sprint's median (lower is better: green down, red up).
11. **Sub-team filter:** When subTeam is provided, filter memberships to tickets whose Assignee has matching SubTeam. All computations scope to filtered tickets only.

**Accept:** All business rules from spec implemented. Pure computation, no DB access. Unit-testable.

**Pattern reference:** Follow `src/Services/Fokus/Fokus.API/Features/Analytics/QaMetricsService.cs` for structure — records at top, service class below, private helper methods for each sub-computation.

### Step 2: Create GetTestTimeline Endpoint

**Follow `create-feature`** (FastEndpoints variant, auto-discovered).

**Feature-specific inputs:**
- Route: `GET /api/sprints/{sprintId}/test-timeline`
- Request: `GetTestTimelineRequest` with `int SprintId` (route param) and `string? SubTeam` (query param)
- Response: `TestTimelineResponse` (from Step 1)
- Validator: SprintId > 0; SubTeam not empty when provided (same pattern as `GetSprintSummaryRequest`)
- Tags: `"Sprints"`

**Files:**
- Create: `src/Services/Fokus/Fokus.API/Features/Sprints/GetTestTimeline/GetTestTimelineEndpoint.cs`
- Create: `src/Services/Fokus/Fokus.API/Features/Sprints/GetTestTimeline/GetTestTimelineQuery.cs` (request + validator)

**Handler logic** (what to load and call — developer decides decomposition):
1. Load sprint by ID (404 if not found)
2. Check `settings.XrayEnabled` — if false, return 200 with `hasQaData=false` and empty/null fields
3. Load TEs for sprint via `TestExecutionRepository.GetTestExecutionsForSprintAsync`
4. If zero TEs and zero test runs → return 200 with `hasQaData=false`
5. Load sprint memberships, status transitions, app settings
6. Resolve prior sprint (previous closed sprint by StartDate) and its TEs/memberships for delta computation
7. Call `TestTimelineService.ComputeTimeline(...)` and return result

**Accept:** Returns 200 with full timeline data, 200 with hasQaData=false when Xray disabled or no data, 404 for unknown sprint.

### Step 3: Extend Dashboard Flags with Testing Crunch

Add `testingCrunch` field to the Dashboard sprint-summary response.

**Files:**
- Modify: `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs`
  - Add `TestingCrunchFlag? TestingCrunch` property to `FlagsResult` record
  - Update `HasAnyFlags` computation to include testingCrunch
  - In `ComputeFlags`, when Xray is enabled and QA data exists, compute the crunch flag using the same logic as TestTimelineService (>50% of terminal runs in last 2 days). Reuse `TestTimelineService` or extract a shared static helper to avoid duplication.
  - Add `TestTimelineService` as a dependency of `SprintSummaryService` if reusing (or keep the crunch logic in a static helper on `TestTimelineService`).

**Accept:** `GET /api/analytics/sprint-summary` response `flags.testingCrunch` is non-null when Xray enabled, QA data exists, and crunch flag fires. Null otherwise. Does not break existing flags structure (other flags unchanged).

### Step 4: Frontend Types + API Function + Store Extension

**No matching skill** — frontend type/store patterns are a gap.

**Files:**
- Modify: `client/src/types/index.ts` — add interfaces matching `TestTimelineResponse` and all sub-types
- Modify: `client/src/api/analytics.ts` — add `getTestTimeline(sprintId: number, subTeam?: string): Promise<TestTimelineResponse>` function
- Modify: `client/src/stores/sprintsStore.ts` — add `testTimeline` ref, fetch in `fetchAllData` (only when `sprintMode === 'single'`), clear on mode switch to multi
- Modify: `client/src/types/index.ts` — extend `FlagsResult` interface with optional `testingCrunch` field: `{ crunchPercentage: number, crunchRunCount: number, totalRunCount: number } | null`

**TypeScript interfaces to add:**

```typescript
export interface BurnupDayEntry {
  dayNumber: number
  calendarDate: string
  isWithinSprint: boolean
  cumulativePass: number
  cumulativeFail: number
  cumulativeTotal: number
  dailyPass: number
  dailyFail: number
}

export interface ScopeChangeDayEntry {
  dayNumber: number
  calendarDate: string
  addedSp: number
  removedSp: number
  netSp: number
}

export interface CrunchTicketEntry {
  ticketKey: string
  summary: string
  assigneeName: string | null
  storyPoints: number | null
  lateRunCount: number
}

export interface TestingCrunchResult {
  isCrunchFlagged: boolean
  crunchPercentage: number | null
  crunchRunCount: number
  totalRunCount: number
  crunchTickets: CrunchTicketEntry[]
}

export interface PostSprintTicketEntry {
  ticketKey: string
  summary: string
  assigneeName: string | null
  storyPoints: number | null
  postSprintRunCount: number
}

export interface PostSprintTestingResult {
  hasPostSprintTesting: boolean
  postSprintPercentage: number | null
  postSprintRunCount: number
  totalRunCount: number
  postSprintTickets: PostSprintTicketEntry[]
}

export interface UntestedTicketEntry {
  ticketKey: string
  summary: string
  assigneeName: string | null
  storyPoints: number | null
  devDoneDate: string
}

export interface UntestedAtCloseResult {
  hasUntestedAtClose: boolean
  untestedAtCloseCount: number
  untestedAtCloseTickets: UntestedTicketEntry[]
}

export interface GapTicketEntry {
  ticketKey: string
  summary: string
  assigneeName: string | null
  devDoneDate: string
  firstTestDate: string
  gapDays: number
}

export interface DevToTestGapResult {
  medianGapDays: number | null
  medianGapDelta: number | null
  medianGapDirection: string | null
  gapTickets: GapTicketEntry[]
}

export interface TestTimelineResponse {
  hasQaData: boolean
  sprintStartDate: string
  sprintEndDate: string
  planningWindowDays: number
  burnupData: BurnupDayEntry[]
  scopeChangeOverlay: ScopeChangeDayEntry[]
  testingCrunch: TestingCrunchResult
  postSprintTesting: PostSprintTestingResult
  untestedAtClose: UntestedAtCloseResult
  devToTestGap: DevToTestGapResult
}
```

**Store extension pattern:** Follow `sprintsStore.ts` existing pattern. Add `testTimeline` as `ref<TestTimelineResponse | null>(null)`. In `fetchAllData`, when `sprintMode === 'single'` and `selectedSprintId` is set, call `getTestTimeline(sprintId, subTeam)` in parallel with existing scope-change and carry-over calls.

**Accept:** Types compile. API function works. Store fetches timeline data when in single-sprint mode.

### Step 5: Test Execution Burnup Chart Component

**Follow `vue-patterns`.**

**Files:**
- Create: `client/src/components/sprints/TestExecutionBurnupChart.vue`

**Feature-specific inputs:**
- Props: `burnupData: BurnupDayEntry[]`, `scopeChangeOverlay: ScopeChangeDayEntry[]`, `planningWindowDays: number`, `sprintEndDayNumber: number`
- Chart library: ApexCharts (already used by `BurnupChart.vue`)
- Chart type: Line chart (cumulative lines) with annotations
- Series: 3 lines — green (cumulativePass), red (cumulativeFail), gray (cumulativeTotal)
- X-axis: dayNumber labels matching BurnupChart pattern (`Day N (Mon)`)
- Background phase annotations: planning zone (days 1 through planningWindowDays), execution zone (planningWindowDays+1 through sprintEndDayNumber-2), testing crunch zone (last 2 days within sprint)
- Post-sprint visual separator: vertical annotation at sprintEndDayNumber boundary (dashed line)
- Scope change markers: vertical xaxis annotations on days that have scope change events, labeled with net SP ("+3 SP" or "-2 SP")
- Wrap in `BaseCard` with title "Test Execution Burnup" and InfoTooltip (text from help.tooltips.md: "Cumulative test runs completed per sprint day. Green = PASS, Red = FAIL, Gray = Total. Shows when testing happened.")

**Pattern reference:** `client/src/components/sprints/BurnupChart.vue` for ApexCharts configuration, annotations, and phase shading.

**Accept:** Chart renders 3 cumulative lines, phase shading visible, scope change markers appear, post-sprint days visually separated. Empty when no burnup data.

### Step 6: Testing Crunch, Post-Sprint, Untested, and Gap Detail Components

**Follow `vue-patterns`.**

**Files:**
- Create: `client/src/components/sprints/TestingCrunchSection.vue`
- Create: `client/src/components/sprints/PostSprintTestingSection.vue`
- Create: `client/src/components/sprints/UntestedAtCloseSection.vue`
- Create: `client/src/components/sprints/DevToTestGapSection.vue`

**All four sections share a common pattern:** expandable section with header metric and expandable ticket list. Follow the pattern of `ZombieTrajectorySection.vue` (collapsible detail with ticket table).

**TestingCrunchSection:**
- Props: `crunch: TestingCrunchResult`
- Only renders when `crunch.isCrunchFlagged`
- Header: "Testing crunch: X% of test runs in the last 2 days (N of M runs)"
- Expandable ticket list: ticketKey, summary, assigneeName, storyPoints, lateRunCount. Sorted by lateRunCount desc (already sorted from API).
- InfoTooltip: "Fires when >50% of test runs completed in the last 2 sprint days. Shows percentage and affected ticket count."

**PostSprintTestingSection:**
- Props: `postSprint: PostSprintTestingResult`
- Only renders when `postSprint.hasPostSprintTesting`
- Header: "Post-sprint testing: X% of test runs (N of M runs completed after sprint close)"
- Expandable ticket list: ticketKey, summary, assigneeName, storyPoints, postSprintRunCount. Sorted by postSprintRunCount desc.
- InfoTooltip: "Percentage of test runs that completed after the sprint closed. These tests missed the sprint boundary."

**UntestedAtCloseSection:**
- Props: `untested: UntestedAtCloseResult`
- Only renders when `untested.hasUntestedAtClose`
- Header: "Completed but untested by sprint close: N tickets"
- Expandable ticket list: ticketKey, summary, assigneeName, storyPoints, devDoneDate (formatted). Sorted by devDoneDate asc.
- InfoTooltip: "Tickets that finished development before sprint end but had no test results by close. A testing gap signal."

**DevToTestGapSection:**
- Props: `gap: DevToTestGapResult`
- Always renders (shows "No data" empty state when medianGapDays is null)
- Metric card: median gap value with delta arrow (green down = better, red up = worse). Follow MetricCard/delta pattern from DashboardView.
- Per-ticket table: ticketKey, summary, assigneeName, devDoneDate, firstTestDate, gapDays (one decimal). Sorted by gapDays desc.
- InfoTooltip on metric: "Median calendar days between dev completion and first test result. Lower is better — less QA queue time."
- InfoTooltip on delta: "Change vs prior sprint. Green down arrow = gap shrinking (faster testing). Red up arrow = gap growing."

**Accept:** Each section conditionally renders. Ticket lists expand/collapse. Gap section shows metric card with delta. Tooltips wired from help.tooltips.md.

### Step 7: Integrate into SprintsView + Dashboard Flags

**Follow `vue-patterns`.**

**Files:**
- Modify: `client/src/views/SprintsView.vue` — add "Test Execution Timeline" section in single-sprint template block
- Modify: `client/src/components/dashboard/SprintFlags.vue` — add testing crunch flag display

**SprintsView integration:**
- Add import for all new components (TestExecutionBurnupChart, TestingCrunchSection, PostSprintTestingSection, UntestedAtCloseSection, DevToTestGapSection)
- In the single-sprint detail template (after the existing BugTimeTable), add a conditional section:
  - Gate: `store.testTimeline?.hasQaData` (whole section hidden when false/null)
  - Section separator (matching "Carry-Over Analysis" pattern): border-t, heading "Test Execution Timeline"
  - Empty state: when `hasQaData` but Xray not synced → show "Sync sprint to load QA data" message (matching F26 pattern)
  - Components ordered: BurnupChart → TestingCrunchSection → PostSprintTestingSection → UntestedAtCloseSection → DevToTestGapSection
  - Pass props from `store.testTimeline`
  - Compute `sprintEndDayNumber` from response dates for the burnup chart

**SprintFlags integration:**
- Add a new flag block after "Zero-SP Developers" (or before, based on visual priority — after mid-sprint disruption is appropriate):
  - Condition: `flags.testingCrunch` (non-null)
  - Warning icon + "Testing Crunch" label
  - Content: "X% of test runs completed in the last 2 days (N of M runs)"
  - InfoTooltip: "Fires when >50% of test runs completed in the last 2 sprint days. Shows percentage and affected ticket count."

**Accept:** Timeline section appears on single-sprint view when Xray enabled and QA data exists. Dashboard shows crunch flag when applicable. Empty states render correctly. Sub-team filter re-fetches and scopes all F30 data.

## Cross-Service Changes

None. Fokus is a single-service system.

## Migration Notes

None. No schema changes.

## Testing Strategy

**Backend:**
- TestTimelineService unit tests: verify burnup accumulation, crunch flag threshold (exactly 50% vs >50%), post-sprint boundary, untested identification, gap computation with negative values, zero-denominator edge cases
- Endpoint integration: 404 for unknown sprint, hasQaData=false when Xray disabled, sub-team filtering

**Frontend:**
- Verify burnup chart renders with mock data (3 lines, phase shading, scope markers)
- Verify conditional rendering: sections appear/hide based on data flags
- Verify testing crunch flag appears on Dashboard when applicable
- Verify empty states for all three scenarios (Xray disabled, not synced, zero runs)

## KB Impact

Create `docs/kb/analytics/test-timeline.md` — document burnup computation, crunch threshold (2-day fixed), effective date resolution (FinishedAt ?? StartedAt), sprint day assignment formula, gap computation, and the relationship to F26 (which is coverage-focused) vs F30 (which is timing-focused).

## Open Questions

None — all decisions resolved in the spec. Assumptions noted:
- The prior sprint for delta computation is the most recent closed sprint before the selected sprint (by StartDate), consistent with the pattern used in SprintSummaryService.
- The endpoint lives under `Features/Sprints/` (not `Features/Analytics/`) because it is sprint-scoped with a path parameter, matching the Sprints resource pattern rather than the analytics query pattern.
