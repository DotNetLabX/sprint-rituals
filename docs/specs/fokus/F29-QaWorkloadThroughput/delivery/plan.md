# QA Workload & Throughput

**Feature Spec:** `docs/specs/F29-QaWorkloadThroughput/definition/spec.md`

## Context

F25 introduced Xray integration and the TestExecution / TestRun / TestExecutionLink domain entities. F26 surfaces sprint-level QA metrics on the Dashboard. F28 surfaces per-developer story quality (coverage of a developer's stories). F29 completes the QA picture from the opposite direction: per-person QA **execution** workload — who ran the tests, how many, and how distributed is the testing load across the team. This is a new tab on the Developers page, following the same endpoint + service + tab component pattern established by Throughput, Bug Ratio, Leaderboard, and Quality tabs.

**Services impacted:** Fokus (single service). No new entities, no migrations, no settings changes — F29 reads from entities introduced by F25 and thresholds from F26.

## Scope

**In scope:**
- New GET /api/analytics/qa-workload endpoint with multi/single sprint modes
- QaWorkloadService computation service (attribution, metric formulas, workload balance alert)
- Frontend: QA Workload tab on Developers page (metric cards, distribution chart, trend chart, per-person table)
- Frontend: types, API module, store extensions, tab component
- Help tooltips from `docs/specs/F29-QaWorkloadThroughput/definition/help.tooltips.md`
- Empty states (Xray disabled, no QA data, no persons)

**Out of scope:**
- Per-developer story quality (F28)
- Test execution timeline (F30)
- Cross-sprint QA trends (F27)
- Avg execution time per person
- Configurable workload balance threshold
- Per-test-run detail view

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | (none) | — | QaWorkloadService: attribution logic, metric formulas, workload balance alert, multi/single sprint modes | Log: no skill for metric computation patterns |
| 2 | create-feature | Follow | GetQaWorkload endpoint (GET /api/analytics/qa-workload), FastEndpoints | |
| 3 | (none) | — | Frontend types for QaWorkloadResponse, store extensions, API function | Log: no skill for frontend type/store patterns |
| 4 | vue-patterns | Follow | QaWorkloadTab component with charts and table | |
| 5 | vue-patterns | Follow | DevelopersView tab integration, store wiring, URL sync | |

## Domain Model Changes

None. F29 reads from existing entities: TestExecution, TestRun, TestExecutionLink, Developer, Sprint, SprintMembership. No new entities, value objects, or domain events.

## Data Model Changes

None. No new tables, columns, or migrations required.

## Implementation Steps

### Step 1: Create QaWorkloadService

Create the computation service that calculates all QA workload metrics. Pure computation — receives pre-loaded data, returns result records.

**No matching skill** — metric computation patterns are a gap. Full inline detail required.

**Files:**
- Create: `src/Services/Fokus/Fokus.API/Features/Analytics/QaWorkloadService.cs`
- Modify: `src/Services/Fokus/Fokus.API/DependencyInjection.cs` — register `QaWorkloadService` as scoped

**Records to define** (in the same file, above the service class — following BugRatioService / DeveloperQualityService pattern):

**SprintSummaryItem** — reuse existing record from `DeveloperThroughputService.cs` (already `public record SprintSummaryItem(int Id, string Name, DateTime StartDate, DateTime EndDate)` in the Analytics namespace).

**Multi-sprint records:**

- `QaWorkloadTeamMetrics` — totalTes (int), totalRunsCompleted (int), passCount (int), failCount (int), teamPassRate (decimal)
- `QaWorkloadSprintBreakdown` — sprintId (int), sprintName (string), tesOwned (int), runsCompleted (int), passCount (int), failCount (int), passRate (decimal), storiesCovered (int), bugsFound (int)
- `WorkloadAlert` — isActive (bool), consecutiveSprintCount (int), thresholdPercent (int, always 50)
- `QaWorkloadEntry` — accountId (string?), displayName (string), subTeam (string?), avatarUrl (string?), tesOwned (int), runsCompleted (int), passCount (int), failCount (int), passRate (decimal), storiesCovered (int), bugsFound (int), sprintBreakdowns (List\<QaWorkloadSprintBreakdown\>), workloadAlert (WorkloadAlert)
- `QaWorkloadMultiSprintResponse` — sprints (List\<SprintSummaryItem\>), teamMetrics (QaWorkloadTeamMetrics), developers (List\<QaWorkloadEntry\>)

**Single-sprint records:**

- `QaWorkloadSingleTeamMetrics` — totalTes (MetricCard), totalRunsCompleted (MetricCard), teamPassRate (MetricCard). Reuse `MetricCard` from `SprintSummaryService.cs`.
- `QaWorkloadSingleDeltaPair` — delta (decimal?), direction (string?)
- `QaWorkloadSingleEntry` — same fields as QaWorkloadEntry, plus delta pairs for each numeric column: tesOwnedDelta/tesOwnedDirection, runsCompletedDelta/runsCompletedDirection, passCountDelta/passCountDirection, failCountDelta/failCountDirection, passRateDelta/passRateDirection, storiesCoveredDelta/storiesCoveredDirection, bugsFoundDelta/bugsFoundDirection, workloadAlert (WorkloadAlert)
- `QaWorkloadSingleSprintResponse` — sprint (SprintSummaryItem), teamMetrics (QaWorkloadSingleTeamMetrics), developers (List\<QaWorkloadSingleEntry\>)

**Response wrapper:**

- `QaWorkloadResponse` — hasQaData (bool), mode (string: "multi" or "single"), multiSprint (QaWorkloadMultiSprintResponse?), singleSprint (QaWorkloadSingleSprintResponse?)

**Service shape:**

Class `QaWorkloadService` with two public methods:

```
QaWorkloadMultiSprintResponse ComputeMultiSprint(
    List<Sprint> targetSprints,
    Dictionary<int, List<TestExecution>> tesBySprintId,
    List<Developer> allDevelopers,
    List<Sprint> allClosedSprints,
    Dictionary<int, List<TestExecution>> allClosedTesBySprintId,
    AppSettings settings,
    string? subTeam)

QaWorkloadSingleSprintResponse ComputeSingleSprint(
    Sprint targetSprint,
    List<TestExecution> targetTEs,
    Sprint? priorSprint,
    List<TestExecution>? priorTEs,
    List<Sprint> sparklineWindow,
    Dictionary<int, List<TestExecution>> sparklineTEsBySprintId,
    List<Sprint> allClosedSprints,
    Dictionary<int, List<TestExecution>> allClosedTesBySprintId,
    List<Developer> allDevelopers,
    AppSettings settings,
    string? subTeam)
```

**Attribution logic (BR 1-3):**

For each TestRun with terminal status (Pass or Fail), attribute to a person:
1. If `TestRun.ExecutedById` is non-null, attribute to that person.
2. Else if parent `TestExecution.AssigneeId` is non-null, attribute to that person.
3. Else attribute to "Unassigned" (accountId = null).

Build a helper method `GetAttributedAccountId(TestRun run, TestExecution te) -> string?` that returns the accountId or null for unassigned.

**Person inclusion rule (BR 4):** A person appears in the table if they have at least one attributed terminal run across the viewed sprint range. This is different from other developer tabs which filter by `activeDevelopers` — QA workload uses attribution-based inclusion. The `allDevelopers` parameter is used to look up DisplayName, SubTeam, AvatarUrl for attributed accountIds.

**Unassigned row (BR 3):** When runs have both ExecutedById and AssigneeId null, create a synthetic entry with accountId = null, displayName = "Unassigned", subTeam = null, avatarUrl = null.

**Metric formulas per person (BR 5-11):**

- **TEs Owned (BR 5):** Count of non-cancelled TestExecutions where `AssigneeId == person's accountId`, linked to tickets in the sprint(s) via TestExecutionLink (Tests type). For the Unassigned row, count TEs where AssigneeId is null.
- **Runs Completed (BR 6):** Count of terminal TestRuns (Pass or Fail) attributed to this person.
- **Pass Count (BR 7):** Count of PASS TestRuns attributed to this person.
- **Fail Count (BR 8):** Count of FAIL TestRuns attributed to this person.
- **Pass Rate (BR 9):** passCount / (passCount + failCount) * 100. Zero denominator = 0%.
- **Stories Covered (BR 10):** Count of unique TicketKeys linked via Tests from TEs where this person has at least one attributed terminal run. Uses the execution attribution model (who ran the test).
- **Bugs Found (BR 11):** Count of unique bug TicketKeys linked via Blocks from TEs where AssigneeId = this person. Bug discovery credit goes to TE owner. For Unassigned row, counts bugs from TEs where AssigneeId is null.

**Aggregation rules (BR 12-15):**
- Exclude cancelled TEs (`IsCancelled == true`) from all computations.
- Exclude non-terminal runs (Todo, Executing, Aborted) from Runs Completed, Pass, Fail, Pass Rate.
- Sprint scoping: reuse `TestExecutionRepository.GetTestExecutionsForSprintAsync` which already handles BR8 (max-sprint-id tiebreaker) and excludes cancelled TEs.
- Multi-sprint aggregation: top-level per-person metrics are sums; Pass Rate = total pass / total terminal runs.

**Sub-team filter (BR 21):** When `subTeam` is specified, filter persons (QA executors) by `Developer.SubTeam == subTeam`. All metrics recalculate within the sub-team's population. Helper: `FilterBySubTeam(Dictionary<string?, PersonMetrics> personMap, List<Developer> allDevelopers, string? subTeam)` — removes persons whose Developer.SubTeam does not match. The Unassigned row has null SubTeam, so it is excluded when any sub-team filter is active.

**Developer exclusion NOT applied (BR 20):** Do NOT use `ExcludedDeveloperFilter`. QA workload inclusion is governed solely by attributed terminal runs. Inactive developers with QA data still appear.

**Team-level metrics (BR 21-24):**
- Total TEs: count of unique non-cancelled TEs in the sprint(s), deduplicated by TE ID.
- Total Runs Completed: sum of all terminal runs across all persons.
- Team Pass Rate: total PASS runs / total terminal runs * 100.

**Workload balance alert (BR 16-19):**

Computed from **full closed sprint history** (not just viewed range):
- For each closed sprint: compute each person's attributed terminal runs. A person "dominates" when their runs > 50% of total runs in that sprint.
- Walk backward from most recent: count consecutive sprints where person dominates.
- Alert fires when consecutiveSprintCount >= 2.
- Return `WorkloadAlert(isActive, consecutiveSprintCount, thresholdPercent: 50)`.

Method: `EvaluateWorkloadAlert(string? accountId, List<Sprint> allClosedSprints, Dictionary<int, List<TestExecution>> allClosedTesBySprintId) -> WorkloadAlert`

**Single-sprint deltas:**
- For each numeric column, compute delta = current - prior. Direction = up/down/flat.
- Delta is null when no prior sprint has QA data.
- Delta polarities per spec: TEs Owned = neutral, Runs Completed = neutral, Pass = higher-is-better, Fail = lower-is-better, Pass Rate = higher-is-better, Stories Covered = higher-is-better, Bugs Found = neutral.

**Single-sprint team MetricCard:**
- Total TEs: neutral polarity. Sparkline up to 4 trailing sprints.
- Total Runs Completed: neutral polarity. Sparkline up to 4 trailing sprints.
- Team Pass Rate: higher-is-better. RAG-colored using `HealthScoreCalculator.MetricRag(value, settings.QaHealthThresholds.PassRateGreen, settings.QaHealthThresholds.PassRateAmber, higherIsBetter: true)`. Sparkline up to 4 trailing sprints.

Use `SprintSummaryService.BuildMetricCard` pattern (value, delta, direction, sparkline) for team metric cards.

**Division by zero (BR 25-26):** Zero persons -> empty developers array, team metrics show 0. Zero terminal runs for a person -> Pass Rate 0%.

**Pattern reference:** `src/Services/Fokus/Fokus.API/Features/Analytics/BugRatioService.cs` (multi/single split, alert evaluation, delta helpers), `src/Services/Fokus/Fokus.API/Features/Analytics/DeveloperQualityService.cs` (TE-based computation, sparkline building).

**Dependencies:** None

---

### Step 2: Create GetQaWorkload Endpoint

Create the GET /api/analytics/qa-workload endpoint that returns QA workload data for multi-sprint and single-sprint modes.

**Follow** `create-feature` (FastEndpoints variant).

**Files:**
- Create: `src/Services/Fokus/Fokus.API/Features/Analytics/GetQaWorkload/GetQaWorkloadEndpoint.cs`
- Create: `src/Services/Fokus/Fokus.API/Features/Analytics/GetQaWorkload/GetQaWorkloadQuery.cs`

**GetQaWorkloadQuery.cs contents:**

- `GetQaWorkloadRequest` — SprintId (int?, query), Last (int?, query), SubTeam (string?, query). Same shape as GetBugRatioRequest.
- `GetQaWorkloadRequestValidator` — same rules as GetBugRatioRequestValidator (SprintId > 0, Last >= 1, mutually exclusive, SubTeam not empty when provided).
- `QaWorkloadResponseDto` — the serializable response class (property-based, not positional record, matching the spec's response shape). Contains: hasQaData (bool), mode (string), multiSprint (nullable), singleSprint (nullable). Nested DTO classes for each sub-shape.

**GetQaWorkloadEndpoint.cs:**

- Route: `GET /api/analytics/qa-workload`
- Tags: "Analytics"
- Constructor dependencies: `SprintRepository`, `AppSettingsRepository`, `DeveloperRepository`, `TestExecutionRepository`, `QaWorkloadService`
- Request: `GetQaWorkloadRequest`
- Response: `QaWorkloadResponseDto`

**Handler logic** (follows GetBugRatioEndpoint / GetDeveloperQualityEndpoint pattern):

1. Load settings. If `!settings.XrayEnabled`, return 200 with `hasQaData = false`, empty developers.
2. Load all closed sprints (lightweight, ascending).
3. If no closed sprints, return 200 with `hasQaData = false`.
4. Determine mode:
   - `sprintId` provided -> single-sprint mode. Validate sprint exists (404 if not found).
   - `last` provided -> multi-sprint mode, take last N.
   - Neither -> multi-sprint mode, default `last = 5` (spec default).
5. Load all developers (not just active — BR 20 excludes the delivery exclusion filter).
6. Normalize sub-team.

**Multi-sprint path:**
7. Filter target sprint IDs to only those with QA data (via `testExecutionRepository.GetSprintIdsWithQaDataAsync`). If zero remain, return 200 with `hasQaData = false`.
8. Load target sprints with memberships.
9. Load TEs for each target sprint (via `GetTestExecutionsForSprintAsync`).
10. For workload alert: load ALL closed sprints with memberships, load TEs for all closed sprints.
11. Call `qaWorkloadService.ComputeMultiSprint(...)`.
12. Map result to response DTO and return 200.

**Single-sprint path:**
7. Load target sprint with memberships. Load TEs for target sprint. If zero TEs, check `GetSprintIdsWithQaDataAsync` — if sprint has no QA data, return 200 with `hasQaData = false`.
8. Determine prior sprint (previous closed sprint). Load prior sprint TEs if it has QA data.
9. Sparkline window: up to 4 trailing sprints with QA data. Load their TEs.
10. For workload alert: load ALL closed sprints, load TEs for all closed sprints.
11. Call `qaWorkloadService.ComputeSingleSprint(...)`.
12. Map result to response DTO and return 200.

**Performance note:** The workload alert requires iterating all closed sprints (same as BugRatioService's alert evaluation). This is bounded by the number of closed sprints (typically < 50).

**Pattern reference:** `src/Services/Fokus/Fokus.API/Features/Analytics/GetBugRatio/GetBugRatioEndpoint.cs` (mode branching, all-closed-sprints loading for alerts), `src/Services/Fokus/Fokus.API/Features/Analytics/GetDeveloperQuality/GetDeveloperQualityEndpoint.cs` (TE loading, sparkline/prior sprint window, QA data check, response DTO mapping).

**Dependencies:** Step 1

---

### Step 3: Frontend Types, API Module, and Store Extensions

Add TypeScript types for the QA workload response, the API function, and extend the developers store.

**No matching skill** for frontend type/store patterns. Full inline detail required.

**Files:**
- Modify: `client/src/types/index.ts`
- Modify: `client/src/api/analytics.ts`
- Modify: `client/src/stores/developersStore.ts`

**Types to add** (in `client/src/types/index.ts`):

```typescript
// --- QA Workload (F29) ---

export interface WorkloadAlert {
  isActive: boolean
  consecutiveSprintCount: number
  thresholdPercent: number
}

export interface QaWorkloadSprintBreakdown {
  sprintId: number
  sprintName: string
  tesOwned: number
  runsCompleted: number
  passCount: number
  failCount: number
  passRate: number
  storiesCovered: number
  bugsFound: number
}

export interface QaWorkloadEntry {
  accountId: string | null
  displayName: string
  subTeam: string | null
  avatarUrl: string | null
  tesOwned: number
  runsCompleted: number
  passCount: number
  failCount: number
  passRate: number
  storiesCovered: number
  bugsFound: number
  sprintBreakdowns: QaWorkloadSprintBreakdown[]
  workloadAlert: WorkloadAlert
}

export interface QaWorkloadTeamMetrics {
  totalTes: number
  totalRunsCompleted: number
  passCount: number
  failCount: number
  teamPassRate: number
}

export interface QaWorkloadMultiSprintResponse {
  sprints: SprintSummaryItem[]
  teamMetrics: QaWorkloadTeamMetrics
  developers: QaWorkloadEntry[]
}

export interface QaWorkloadSingleTeamMetrics {
  totalTes: MetricCard
  totalRunsCompleted: MetricCard
  teamPassRate: MetricCard
}

export interface QaWorkloadSingleEntry {
  accountId: string | null
  displayName: string
  subTeam: string | null
  avatarUrl: string | null
  tesOwned: number
  runsCompleted: number
  passCount: number
  failCount: number
  passRate: number
  storiesCovered: number
  bugsFound: number
  tesOwnedDelta: number | null
  tesOwnedDirection: string | null
  runsCompletedDelta: number | null
  runsCompletedDirection: string | null
  passCountDelta: number | null
  passCountDirection: string | null
  failCountDelta: number | null
  failCountDirection: string | null
  passRateDelta: number | null
  passRateDirection: string | null
  storiesCoveredDelta: number | null
  storiesCoveredDirection: string | null
  bugsFoundDelta: number | null
  bugsFoundDirection: string | null
  workloadAlert: WorkloadAlert
}

export interface QaWorkloadSingleSprintResponse {
  sprint: SprintSummaryItem
  teamMetrics: QaWorkloadSingleTeamMetrics
  developers: QaWorkloadSingleEntry[]
}

export interface QaWorkloadResponse {
  hasQaData: boolean
  mode: 'multi' | 'single'
  multiSprint: QaWorkloadMultiSprintResponse | null
  singleSprint: QaWorkloadSingleSprintResponse | null
}
```

**API function** (in `client/src/api/analytics.ts`):

Add `getQaWorkload(sprintId?: number, last?: number, subTeam?: string): Promise<QaWorkloadResponse>` — GET /api/analytics/qa-workload with query params. Same pattern as `getBugRatio`.

Add import of `QaWorkloadResponse` from types.

**Store extensions** (in `client/src/stores/developersStore.ts`):

- Import `QaWorkloadResponse` type and `getQaWorkload` API function.
- Add state: `qaWorkload` (ref\<QaWorkloadResponse | null\>), `qaWorkloadLoading` (ref\<boolean\>), `qaWorkloadError` (ref\<string | null\>).
- Extend `activeTab` type union: `'throughput' | 'bugRatio' | 'leaderboard' | 'quality' | 'qaWorkload'`.
- Add `fetchQaWorkload()` action — same pattern as `fetchBugRatio()` / `fetchQuality()`: builds query from sprintMode, selectedSprintId, selectedLast, selectedSubTeam. Calls `getQaWorkload(...)`.
- Extend `switchTab()`: add `'qaWorkload'` case that calls `fetchQaWorkload()`.
- Extend `selectSprint()`, `selectLastN()`, `selectSubTeam()`: add `if (activeTab.value === 'qaWorkload') { await fetchQaWorkload() }`.
- Add to return object: `qaWorkload`, `qaWorkloadLoading`, `qaWorkloadError`, `fetchQaWorkload`.

**Pattern reference:** Existing `quality` / `fetchQuality` in `client/src/stores/developersStore.ts`.

**Dependencies:** Steps 1, 2

---

### Step 4: Create QaWorkloadTab Component

Create the QA Workload tab component with team metric cards, workload distribution chart, throughput trend chart, and per-person table.

**Follow** `vue-patterns`.

**Files:**
- Create: `client/src/components/developers/QaWorkloadTab.vue`

**Props:** `data` (QaWorkloadResponse), `sprintMode` ('single' | 'multi')

**Component structure:**

**1. Team Metric Cards (spec BR 27-32):**
- Multi-sprint mode: Three plain-value cards in a row — Total TEs (neutral), Total Runs Completed (neutral), Team Pass Rate (RAG-colored using F26 thresholds).
  - Read values from `data.multiSprint.teamMetrics`.
  - Team Pass Rate RAG: reuse the existing `ragClass` helper pattern from QualityTab or HealthScoreBadge — green >= 90%, amber >= 70%, red < 70% (F26 defaults). Since the backend doesn't send RAG on the multi-sprint team metrics (plain aggregates), compute it client-side using the same threshold constants.
- Single-sprint mode: Three MetricCard components (reuse existing `MetricCard.vue` from dashboard).
  - Read values from `data.singleSprint.teamMetrics` (MetricCard shape with delta, direction, sparkline).
  - Team Pass Rate gets RAG from `data.singleSprint.teamMetrics.teamPassRate.rag`.
- Tooltip text from `help.tooltips.md` on each card.

**2. Workload Distribution Chart (spec BR 33-39):**
- Stacked horizontal bar chart (ApexCharts `type: 'bar'`, `plotOptions.bar.horizontal: true`).
- One bar per person, sorted by total terminal runs descending.
- Two stacked segments: PASS (green / `#22c55e`) and FAIL (red / `#ef4444`).
- X-axis: run count. Y-axis: person name.
- Unassigned bar pinned at bottom.
- Wrapped in BaseCard with title "Workload Distribution" and tooltip from `help.tooltips.md`.
- Data source: multi-sprint -> `data.multiSprint.developers`, single-sprint -> `data.singleSprint.developers`. Both have passCount and failCount.

**3. Throughput Trend Chart (spec BR 40-44):**
- Multi-sprint only (hidden in single-sprint mode).
- Line chart (ApexCharts `type: 'line'`, `stroke.curve: 'smooth'`, height 300).
- X-axis: sprint names from `data.multiSprint.sprints`. Y-axis: "Runs Completed".
- Each person = one series using `sprintBreakdowns.runsCompleted`.
- Unassigned series: gray color, dashed line.
- Tooltip theme: dark. Wrapped in BaseCard with title "Execution Throughput Trend" and tooltip from `help.tooltips.md`.
- Same chart pattern as Throughput tab's `chartOptions` / `chartSeries`.

**4. Per-Person Table (spec BR 45-57):**
- Columns: Person | Sub-Team | TEs Owned | Runs Completed | Pass | Fail | Pass Rate | Stories Covered | Bugs Found
- Person column: Avatar + display name (same rendering pattern as Throughput table). Workload alert badge (warning icon) when `workloadAlert.isActive`. Badge tooltip: "Handles >50% of test executions for N consecutive sprints."
- Pass Rate column: RAG-colored using F26 pass rate thresholds.
- All numeric columns sortable (click header to toggle asc/desc) — same sort pattern as Throughput table's `toggleThroughputSort` / `throughputSortIcon`.
- Single-sprint mode: inline delta indicators (arrow + signed number) on all numeric columns. Delta polarities from spec (BR 51-57).
- Unassigned row pinned at bottom (no avatar, italic display name).
- Tooltip text from `help.tooltips.md` on each column header.

**Empty states (spec BR 58-60):**
- `data.hasQaData === false` -> "Sync sprint to load QA data" prompt (EmptyState component, same pattern as Quality tab empty state).
- `data.hasQaData === true` but developers array empty -> "No test execution data for the selected sprint(s)" message. Team metrics show 0. Charts hidden.

**Pattern reference:** `client/src/components/developers/QualityTab.vue` (tab component structure, QA data empty state), `client/src/views/DevelopersView.vue` (chart options, sort pattern, avatar rendering, delta display).

**Dependencies:** Step 3

---

### Step 5: Integrate QA Workload Tab into Developers Page

Wire the QaWorkloadTab component into DevelopersView, add the tab button (Xray-gated), handle URL sync, and ensure data loads on tab switch and sprint/sub-team changes.

**Follow** `vue-patterns`.

**Files:**
- Modify: `client/src/views/DevelopersView.vue`

**Changes:**

1. **Import QaWorkloadTab:** Add `import QaWorkloadTab from '../components/developers/QaWorkloadTab.vue'`.

2. **Tab bar extension:** Add a "QA Workload" tab button after the "Quality" button, also gated with `v-if="settingsStore.settings.xrayEnabled"`. Tab value: `'qaWorkload'`.

3. **URL sync:**
   - In `onMounted`: extend the tabParam check: `else if (tabParam === 'qa-workload') { store.activeTab = 'qaWorkload' }`. Add initial data load: `if (store.activeTab === 'qaWorkload' && store.qaWorkload === null) { await store.fetchQaWorkload() }`.
   - In the `watch` for URL sync: add `else if (tab === 'qaWorkload') { query.tab = 'qa-workload' }`.
   - Extend `onTabSwitch` function type: `'throughput' | 'bugRatio' | 'leaderboard' | 'quality' | 'qaWorkload'`.

4. **Tab content:** Add a `<template v-else-if="store.activeTab === 'qaWorkload'">` block after the Quality tab block:
   - Loading state: `<div v-if="store.qaWorkloadLoading" class="text-xs text-text-muted">Updating...</div>`
   - Error state: `<div v-if="store.qaWorkloadError" class="text-sm text-status-danger">{{ store.qaWorkloadError }}</div>`
   - Empty QA data: `<EmptyState v-else-if="store.qaWorkload && !store.qaWorkload.hasQaData" title="No QA data available" description="Sync sprint to load QA data.">` (same icon as Quality tab).
   - Data present: `<QaWorkloadTab v-else-if="store.qaWorkload && store.qaWorkload.hasQaData" :data="store.qaWorkload" :sprint-mode="store.sprintMode" />`

**Pattern reference:** Existing Quality tab integration in `client/src/views/DevelopersView.vue` lines 357-597.

**Dependencies:** Steps 3, 4

---

## Cross-Service Changes

None. Single-service application.

## Migration Notes

None. No new tables or columns. No migration needed.

## Testing Strategy

### Attribution
- TestRun with ExecutedById -> attributed to that person
- TestRun with null ExecutedById, TE has AssigneeId -> attributed to AssigneeId
- TestRun with both null -> attributed to "Unassigned" row
- Person with only execution attribution (no owned TEs) -> appears with TEs Owned = 0

### Metric Formulas
- Person with mixed pass/fail runs -> correct pass rate
- Person with zero terminal runs -> pass rate 0%
- Stories Covered counts unique tickets via execution attribution
- Bugs Found counts unique bug tickets via TE ownership (AssigneeId)
- Cancelled TEs excluded from all computations
- Non-terminal runs (Todo, Executing, Aborted) excluded from run counts

### Multi-Sprint Aggregation
- Top-level metrics are sums, pass rate is weighted average
- Sprint breakdowns provide per-sprint detail
- Team metrics are across all persons in viewed range

### Single-Sprint Mode
- Delta vs prior sprint on all numeric columns
- MetricCard with sparkline for team metrics
- Delta null when no prior sprint has QA data
- Team Pass Rate RAG-colored

### Workload Balance Alert
- Person with >50% of runs in 2+ consecutive sprints -> alert active
- Alert computed from full closed sprint history, not just viewed range
- Person with >50% in 1 sprint only -> alert not active
- Streak broken by sprint where person is <= 50% or has zero runs

### Sub-team Filter
- Filters by QA executor's sub-team (not ticket assignee's sub-team)
- All metrics recalculate within filtered population
- Unassigned row excluded when sub-team filter active

### Developer Exclusion
- Delivery exclusion filter (0% capacity + 0 tickets) NOT applied
- Inactive developers with QA data still appear
- Person with zero delivery capacity but test runs -> appears in table

### Empty States
- Xray disabled -> QA Workload tab hidden, hasQaData = false
- Xray enabled, no QA data for viewed sprints -> "Sync sprint to load QA data"
- Xray enabled, QA data exists, zero persons -> empty table, charts hidden, team metrics 0

### Charts
- Distribution chart shows stacked pass/fail per person, sorted by total runs desc
- Unassigned bar pinned at bottom
- Trend chart shows only in multi-sprint mode, one series per person
- Single-sprint mode hides trend chart

### Tab Integration
- Tab appears after Quality when Xray is enabled
- Tab hidden when Xray disabled
- URL updates to ?tab=qa-workload
- Sprint selector and sub-team filter affect QA workload data
- Default view is last=5 sprints

## Open Questions

None.
