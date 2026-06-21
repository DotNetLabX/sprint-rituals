# Cross-Sprint QA Trends

**Feature Spec:** `docs/specs/F27-CrossSprintQaTrends/definition/spec.md`

## Context

F26 shows per-sprint QA health on the Dashboard (coverage rate, execution rate, pass rate) with sparklines. Scrum Masters need the longer view -- "are we getting better at quality over time?" -- and the causal link between test coverage and escaped defects. F27 provides a dedicated QA sidebar page with full cross-sprint trend charts and a defect correlation overlay that compares test coverage against bug ratio with an N+1 sprint lag.

**Services impacted:** Fokus (single service). Backend: one new analytics endpoint + one new computation service. Frontend: new QA page (route, store, view, chart components), conditional sidebar entry.

## Scope

**In scope:**
- Backend: `GET /api/analytics/qa-trends?last&subTeam` endpoint and `QaTrendsService` computation service
- Backend: Pearson r correlation computation
- Frontend: QA page with three chart sections (Quality Trends, Testing Volume, Defect Correlation)
- Frontend: Pinia store (`qaTrendsStore`) with fixed-range sprint selector (Last 3/5/10/All)
- Frontend: Conditional "QA" sidebar entry gated on `xrayEnabled`
- Frontend: Sub-team filter on the page toolbar
- Empty states per spec Flow 5
- Help tooltips from `docs/specs/F27-CrossSprintQaTrends/definition/help.tooltips.md`

**Out of scope:**
- Same-sprint correlation toggle
- Scatter plot view
- Configurable lag period (N+2, etc.)
- Per-developer trend breakdown (F28)
- QA Workload on this page (F29)
- Summary cards (Dashboard already shows F26 current-sprint cards)
- Trend-line regression / forecast
- Export/download

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | (none) | -- | QaTrendsService: multi-sprint QA metric aggregation, bug ratio reuse, Pearson r | Log: no skill for metric computation patterns |
| 2 | create-feature | Follow | GetQaTrends endpoint (GET /api/analytics/qa-trends), FastEndpoints | |
| 3 | vue-patterns, pinia-patterns | Follow | qaTrendsStore: fixed-range sprint selector, API integration | |
| 4 | vue-patterns | Follow | QualityTrendsChart: multi-line ApexChart (coverage, pass, execution rates) | |
| 5 | vue-patterns | Follow | TestingVolumeChart: grouped bar ApexChart (TE count, bugs found) | |
| 6 | vue-patterns | Follow | DefectCorrelationSection: dual-panel chart + Pearson r badge | |
| 7 | vue-patterns | Follow | QaTrendsView: page assembly, sidebar entry, route registration | |

## Domain Model Changes

None. F27 reads from existing entities (TestExecution, TestExecutionLink, TestRun, Ticket, Sprint, SprintMembership, AppSettings). No new entities, value objects, or settings extensions.

## Data Model Changes

None. No new tables, columns, or migrations.

## Implementation Steps

### Step 1: Create QaTrendsService

Create the backend computation service that aggregates QA metrics across multiple sprints and computes the defect correlation with Pearson r.

**No matching skill** -- metric computation patterns are a gap.

**Files:**
- Create: `src/Services/Fokus/Fokus.API/Features/Analytics/QaTrendsService.cs`
- Modify: `src/Services/Fokus/Fokus.API/DependencyInjection.cs` -- register `QaTrendsService` as scoped

**Records to define** (in the same file, above the service class -- following BugRatioService/QaMetricsService pattern):

- `QaTrendsSprintInfo(int Id, string Name, DateTime StartDate, DateTime EndDate)`
- `QualityTrendEntry(int SprintId, string SprintName, decimal CoverageRate, decimal PassRate, decimal ExecutionRate, string CoverageRag, string PassRateRag, string ExecutionRag)` -- RAG coloring per data point using F26 QA health thresholds (BR28)
- `TestingVolumeEntry(int SprintId, string SprintName, int TeCount, int BugsFound)`
- `DefectCorrelationDataPoint(int SprintId, string SprintName, decimal CoverageRate, decimal? NextSprintBugRatio, string? NextSprintName)`
- `DefectCorrelationResult(List<DefectCorrelationDataPoint> DataPoints, decimal? PearsonR, int DataPointCount)`
- `QaTrendsResponse(bool HasQaData, List<QaTrendsSprintInfo> Sprints, List<QualityTrendEntry> QualityTrends, List<TestingVolumeEntry> TestingVolume, DefectCorrelationResult? DefectCorrelation)`

**Service shape:**

Class `QaTrendsService` with constructor-injected dependencies: none (pure computation, receives pre-loaded data).

Public method:
```
QaTrendsResponse ComputeTrends(
    List<Sprint> qaSprints,
    Dictionary<int, List<TestExecution>> tesBySprintId,
    Dictionary<int, List<SprintMembership>> membershipsBySprintId,
    Dictionary<int, decimal> bugRatioBySprintId,
    List<StatusTransition> statusTransitions,
    AppSettings settings,
    string? subTeam)
```

**Computation rules:**

1. **Quality trend metrics per sprint** -- For each sprint, compute coverage rate, execution rate, and pass rate using the same formulas as `QaMetricsService` (BR1-BR4). Reuse the active scope determination pattern from `QaMetricsService.GetActiveFeatureTicketKeys` and the coverage/execution/pass rate private methods. The developer should extract or call these -- the specific decomposition is the developer's decision.

2. **Testing volume per sprint** -- TE Count: count of unique non-cancelled TEs attributed to the sprint (BR7). Bugs Found: count of unique bug tickets linked via Blocks from non-cancelled TEs (BR8, same as F26 BR4).

3. **RAG coloring per data point** -- Use `HealthScoreCalculator.MetricRag()` with `settings.QaHealthThresholds` for each metric value. Coverage and Execution: green >= thresholds.CoverageGreen, amber >= thresholds.CoverageAmber, red < thresholds.CoverageAmber. Pass Rate: green >= thresholds.PassRateGreen, amber >= thresholds.PassRateAmber, red < thresholds.PassRateAmber. (BR28)

4. **Defect correlation** -- For each sprint N in the sorted list, pair sprint N's coverage rate with sprint N+1's bug ratio from `bugRatioBySprintId`. The most recent sprint has `nextSprintBugRatio = null` (BR9). Exclude sprints where either side is missing from the Pearson r computation (BR11).

5. **Pearson r** -- Standard formula: `r = (n * sum(xy) - sum(x)*sum(y)) / sqrt((n*sum(x^2) - sum(x)^2) * (n*sum(y^2) - sum(y)^2))`. Where x = coverage rates, y = next-sprint bug ratios. Null when fewer than 6 complete data points (BR12, BR22). Round to 2 decimal places (BR13). Handle edge case: if denominator is zero (all values identical), return null.

6. **Sub-team filter** -- When subTeam is specified, filter memberships to tickets assigned to developers in that sub-team. Same pattern as `QaMetricsService.FilterMemberships`. This scopes coverage rate, execution rate, pass rate, and TE count. Bug ratio comes pre-filtered from the caller (BugRatioService already supports sub-team).

7. **hasQaData** -- false when fewer than 2 sprints have QA data (BR21). true otherwise.

8. **Division by zero** -- All rates default to 0% when denominator is 0 (BR17-BR20).

**Accept:** Service returns correct QaTrendsResponse for a set of sprints. Pearson r is null when < 6 data points. RAG colors match F26 thresholds. Sub-team filtering scopes all metrics.

**Pattern reference:** `src/Services/Fokus/Fokus.API/Features/Analytics/QaMetricsService.cs` (per-sprint metric formulas), `src/Services/Fokus/Fokus.API/Features/Analytics/BugRatioService.cs` (multi-sprint aggregation pattern), `src/Services/Fokus/Fokus.API/Features/Analytics/HealthScoreCalculator.cs` (RAG computation).

**Dependencies:** None

---

### Step 2: Create GetQaTrends Endpoint

Create the `GET /api/analytics/qa-trends` endpoint that loads data and delegates to `QaTrendsService`.

**Follow** `create-feature` (FastEndpoints variant).

**Files:**
- Create: `src/Services/Fokus/Fokus.API/Features/Analytics/GetQaTrends/GetQaTrendsEndpoint.cs`
- Create: `src/Services/Fokus/Fokus.API/Features/Analytics/GetQaTrends/GetQaTrendsQuery.cs`

**Endpoint details:**
- Route: `GET /api/analytics/qa-trends`
- Tags: "Analytics"
- Request: `GetQaTrendsRequest` with `Last` (int?, from query), `SubTeam` (string?, from query)
- Response: `QaTrendsResponse` (from Step 1)

**Handler logic:**

1. Load AppSettings. If `XrayEnabled == false`, return 200 with `HasQaData = false` and empty arrays.
2. Load all closed sprints (lightweight) via `SprintRepository.GetClosedSprintsAsync`. If fewer than 2, return `HasQaData = false`.
3. Normalize `last`: if null, use all sprints. If provided and < 2, treat as 2 (BR16 minimum). "All" is expressed by omitting `last` (spec says so).
4. Use `TestExecutionRepository.GetSprintIdsWithQaDataAsync` to identify which closed sprints have QA data. Filter to only those. If fewer than 2 QA-synced sprints, return `HasQaData = false`.
5. Select the last N QA-synced sprints (sorted chronologically). These are the target sprints.
6. Load target sprints with memberships via `SprintRepository.GetSprintsWithMembershipsAsync`.
7. Load TEs for each target sprint via `TestExecutionRepository.GetTestExecutionsForSprintAsync` -- build `Dictionary<int, List<TestExecution>>`.
8. Load status transitions for target sprint tickets via `TicketRepository.GetStatusTransitionsForSprintTicketsAsync`.
9. **Bug ratio per sprint** -- Load active developers via `DeveloperRepository.GetActiveDevelopersAsync` and all developers via `DeveloperRepository.GetAllAsync`. Reuse `BugRatioService.ComputeMultiSprint` to get per-sprint bug ratios. The method requires: target sprints, all closed sprints (with memberships), active developers, settings, status transitions, subTeam. The response contains `TeamMetrics.PerSprintTrend` with `BugRatioPercent` per sprint. Build `Dictionary<int, decimal>` mapping sprintId to bugRatioPercent. This ensures bug ratio numbers are identical to the Developers page (spec BR10). Note: the correlation also needs bug ratio for sprint N+1 (which may be beyond the selected range). Include one additional sprint beyond the selected range in the target sprints passed to `ComputeMultiSprint` for this purpose.
10. Normalize sub-team.
11. Call `QaTrendsService.ComputeTrends(...)`.
12. Return 200 with the result.

**Performance note:** This endpoint loads TEs per sprint in a loop. For typical usage (10-20 sprints), this is acceptable. If "All" with 50+ sprints causes latency, this is a future optimization (batched TE loading) -- not in scope for F27.

**Pattern reference:** `src/Services/Fokus/Fokus.API/Features/Analytics/GetBugRatio/GetBugRatioEndpoint.cs` (multi-sprint data loading, BugRatioService injection).

**Dependencies:** Step 1

---

### Step 3: Create qaTrendsStore and API Module

Create the Pinia store and API function for the QA Trends page.

**Follow** `pinia-patterns` for store. **Follow** `vue-patterns` for types.

**Files:**
- Modify: `client/src/types/index.ts` -- add QaTrends response types
- Modify: `client/src/api/analytics.ts` -- add `getQaTrends` function
- Create: `client/src/stores/qaTrendsStore.ts`

**Types to add** (in `types/index.ts`):

```
QaTrendsSprintInfo { id: number, name: string, startDate: string, endDate: string }
QualityTrendEntry { sprintId: number, sprintName: string, coverageRate: number, passRate: number, executionRate: number, coverageRag: string, passRateRag: string, executionRag: string }
TestingVolumeEntry { sprintId: number, sprintName: string, teCount: number, bugsFound: number }
DefectCorrelationDataPoint { sprintId: number, sprintName: string, coverageRate: number, nextSprintBugRatio: number | null, nextSprintName: string | null }
DefectCorrelationResult { dataPoints: DefectCorrelationDataPoint[], pearsonR: number | null, dataPointCount: number }
QaTrendsResponse { hasQaData: boolean, sprints: QaTrendsSprintInfo[], qualityTrends: QualityTrendEntry[], testingVolume: TestingVolumeEntry[], defectCorrelation: DefectCorrelationResult | null }
```

**API function** (in `analytics.ts`):

`getQaTrends(last?: number, subTeam?: string): Promise<QaTrendsResponse>` -- `GET /api/analytics/qa-trends`. Convention: `last=0` means "all" (frontend sends 0 when user picks "All", omit the param on the backend side). Follow the existing pattern in `getBugRatio` / `getScopeChange`.

**Store shape** (`qaTrendsStore.ts`):

Setup store pattern (matching `sprintsStore.ts`):
- State: `closedSprints`, `subTeams`, `selectedLast` (ref, default 10), `selectedSubTeam` (ref, default null), `qaTrends` (ref<QaTrendsResponse | null>), `loading`, `initializing`, `error`
- Actions: `initialize()` (load closed sprints + sub-teams, then fetch trends), `selectLastN(n: number | null)`, `selectSubTeam(subTeam: string | null)`, `fetchTrends()` (calls `getQaTrends`)
- No `selectedSprintId` or `sprintMode` -- F27 is always multi-sprint

The sprint selector values map to: Last 3 = 3, Last 5 = 5, Last 10 = 10, All = 0 (sends `last=0` which the API treats as "all").

**Pattern reference:** `client/src/stores/sprintsStore.ts` (store structure, initialize pattern, fetchAllData pattern).

**Dependencies:** Step 2

---

### Step 4: Create Quality Trends Chart Component

Create the multi-line chart for coverage rate, pass rate, and execution rate across sprints.

**Follow** `vue-patterns`.

**Files:**
- Create: `client/src/components/qa/QualityTrendsChart.vue`

**Props:**
- `qualityTrends: QualityTrendEntry[]`

**Chart configuration:**
- Type: `line` (ApexCharts)
- Three series: Coverage Rate (blue -- `#3b82f6`), Pass Rate (green -- `#22c55e`), Execution Rate (gray -- `#6b7280`)
- X-axis: sprint names in chronological order
- Y-axis: 0-100%, formatter `${val}%`
- Smooth curves (`stroke.curve: 'smooth'`)
- Tooltip on hover shows all three values for the hovered sprint
- RAG-colored data point markers: use `discrete` markers config in ApexCharts. For each data point, set the marker fill color based on the RAG value (green/amber/red from the `coverageRag`/`passRateRag`/`executionRag` fields). The line color stays as the series color; only the markers change.
- Dark theme styling (matching existing chart components)
- Tooltip from `help.tooltips.md`: "Shows coverage rate, pass rate, and execution rate across sprints..."

**Pattern reference:** `client/src/components/developers/QualityTrendChart.vue` (multi-line ApexChart with dark theme).

**Dependencies:** Step 3

---

### Step 5: Create Testing Volume Chart Component

Create the grouped bar chart for TE count and bugs found per sprint.

**Follow** `vue-patterns`.

**Files:**
- Create: `client/src/components/qa/TestingVolumeChart.vue`

**Props:**
- `testingVolume: TestingVolumeEntry[]`

**Chart configuration:**
- Type: `bar` (ApexCharts, grouped)
- Two series: TE Count, Bugs Found -- each sprint has two side-by-side bars
- X-axis: sprint names aligned with Quality Trends chart (same ordering)
- Y-axis: count (auto-scaled)
- Tooltip on hover shows TE count and bugs found for the sprint
- Dark theme styling
- Tooltip from `help.tooltips.md`: "Total test executions and bugs found per sprint..."

**Pattern reference:** `client/src/components/developers/BugRatioStackedChart.vue` (grouped/stacked bar chart with ApexCharts).

**Dependencies:** Step 3

---

### Step 6: Create Defect Correlation Section

Create the dual-panel correlation visualization with Pearson r badge.

**Follow** `vue-patterns`.

**Files:**
- Create: `client/src/components/qa/DefectCorrelationSection.vue`

**Props:**
- `defectCorrelation: DefectCorrelationResult | null`
- `hasQaData: boolean`

**Structure:**

1. **Two stacked line charts** with aligned sprint X-axis:
   - Top panel: Coverage Rate line (blue) across sprints. Y-axis: 0-100%.
   - Bottom panel: Bug Ratio line (red -- `#ef4444`) across sprints. The X-axis labels are the *next* sprint names (`nextSprintName` field) to visualize the N+1 offset. Data points use `nextSprintBugRatio` values, excluding null entries (the last sprint has no next-sprint data).

2. **Visual connector** -- A subtle annotation or visual indicator connecting coverage in sprint N to bug ratio in sprint N+1. Implementation options: dashed grid alignment, annotation arrows, or a shared vertical highlight on hover. The developer decides the specific approach.

3. **Pearson r badge** (when `defectCorrelation.pearsonR !== null`):
   - Pill-shaped badge in the section header
   - Text: `r = {value}` (e.g., "r = -0.72")
   - Background color: green when r < -0.3, amber when -0.3 <= r <= 0.3, red when r > 0.3 (BR27)
   - Tooltip: "Correlation between test coverage and next-sprint bug ratio. Negative values indicate higher coverage predicts fewer bugs."

4. **Empty states:**
   - `defectCorrelation === null` and `hasQaData === true`: show "No bug ratio data available for correlation"
   - `pearsonR === null` and `dataPointCount > 0` and `dataPointCount < 6`: show "Need 6+ sprints with both QA and bug data for correlation analysis"
   - `dataPointCount === 0`: show "No bug ratio data available for correlation"

**Tooltip from `help.tooltips.md`:** "Compares test coverage in one sprint against the bug ratio in the next sprint..."

**Pattern reference:** `client/src/components/developers/QualityTrendChart.vue` (line chart), `client/src/components/dashboard/HealthScoreBadge.vue` (badge with tooltip and RAG color).

**Dependencies:** Step 3

---

### Step 7: Create QA Trends View, Route, and Sidebar Entry

Assemble the page, register the route, and add the conditional sidebar entry.

**Follow** `vue-patterns`.

**Files:**
- Create: `client/src/views/QaTrendsView.vue`
- Modify: `client/src/router.ts` -- add `/qa` route
- Modify: `client/src/components/AppSidebar.vue` -- add conditional "QA" nav item

**QaTrendsView.vue:**

- Uses `PageLayout` and `PageToolbar` (same pattern as `SprintsView.vue`)
- Toolbar contains: sprint range selector (BaseSelect with options: Last 3, Last 5, Last 10, All) and sub-team filter (BaseSelect)
- Initializes `qaTrendsStore` on mount
- Content sections (each wrapped in `BaseCard`):
  1. Quality Trends chart (`QualityTrendsChart`)
  2. Testing Volume chart (`TestingVolumeChart`)
  3. Defect Correlation section (`DefectCorrelationSection`)
- Empty states:
  - `hasQaData === false`: show EmptyState with "Not enough data to show trends. Sync at least 2 sprints with QA data."
  - Loading state: standard loading indicator
- Wire help tooltips from `help.tooltips.md` to section headers

**Router** (`router.ts`):

Add route before the `/settings` route:
```
{ path: '/qa', name: 'qa', component: () => import('./views/QaTrendsView.vue') }
```

**Sidebar** (`AppSidebar.vue`):

The current sidebar uses a static `navItems` array. The "QA" entry must be conditional on `xrayEnabled`. Implementation:
- Import `useSettingsStore` and access `settings.xrayEnabled`
- Convert `navItems` to a computed property that conditionally includes the QA entry when `settingsStore.settings.xrayEnabled === true`
- QA entry: `{ path: '/qa', label: 'QA', exact: false, icon: '<svg>...</svg>' }` -- placed between "Epics" and "Cycle Time" in the nav order
- Icon: a clipboard-check or test-tube SVG icon (Heroicons style, matching existing icons)

**Accept:** QA page renders with three chart sections. Sprint range selector defaults to "Last 10". Sub-team filter scopes all data. Empty states display correctly. Sidebar shows "QA" only when Xray is enabled. Route is accessible at `/qa`.

**Pattern reference:** `client/src/views/SprintsView.vue` (page layout with toolbar, store integration), `client/src/components/AppSidebar.vue` (nav item structure).

**Dependencies:** Steps 3, 4, 5, 6

---

## Cross-Service Changes

None. Single-service application.

## Migration Notes

None. No database changes.

## Testing Strategy

### Backend -- QaTrendsService

- 10 sprints with varying coverage/execution/pass rates -- correct per-sprint values
- Sprint with zero feature tickets -- rates are 0%, not NaN
- Sprint with zero covered tickets -- execution rate 0%
- Sprint with zero terminal runs -- pass rate 0%
- Cancelled TEs excluded from all metrics (BR4)
- Sub-team filter scopes all metrics correctly
- TE Count counts unique non-cancelled TEs per sprint
- Bugs Found counts unique bug tickets linked via Blocks per sprint

### Backend -- Defect Correlation

- 8 sprints: Pearson r computed and non-null, correct sign (negative when coverage inversely correlates with bugs)
- 4 sprints: Pearson r is null (below 6-point threshold)
- Most recent sprint: nextSprintBugRatio is null
- Sprints with no bug ratio data: excluded from correlation
- All values identical (zero variance): Pearson r is null (division by zero guard)
- Correct N+1 pairing: sprint N coverage paired with sprint N+1 bug ratio

### Backend -- Endpoint

- `last` omitted: returns all QA-synced sprints
- `last=10`: returns last 10 QA-synced sprints
- `last=1`: treated as 2 (minimum)
- Xray disabled: returns `hasQaData: false` with empty arrays
- Fewer than 2 QA-synced sprints: returns `hasQaData: false`
- Sub-team parameter scopes all results including bug ratio in correlation

### Frontend

- Sprint range selector: Last 3/5/10/All options update all charts
- Sub-team filter scopes all data
- Quality Trends chart shows three lines with RAG-colored markers
- Testing Volume chart shows grouped bars aligned with quality trends X-axis
- Defect Correlation shows dual panels with offset visualization
- Pearson r badge: appears/disappears based on data point count
- Pearson r badge color: green for r < -0.3, amber for -0.3 to 0.3, red for r > 0.3
- Empty state: Xray disabled -- sidebar entry hidden
- Empty state: < 2 QA sprints -- "Not enough data" message
- Empty state: no bug ratio data -- correlation section shows fallback
- Sidebar "QA" entry visible only when Xray enabled
- Help tooltips match `help.tooltips.md` content

## KB Impact

Update `docs/kb/frontend-map.md`:
- Add QaTrendsView row to the View -> Store -> API Mapping table: QaTrendsView | qaTrendsStore | qa-trends | Multi-sprint only, no single-sprint mode

## Open Questions

None.
