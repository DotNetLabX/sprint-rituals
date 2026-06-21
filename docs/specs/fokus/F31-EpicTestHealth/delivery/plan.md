# Epic Test Health

**Feature Spec:** `docs/specs/F31-EpicTestHealth/definition/spec.md`

## Context

F14 delivers epic delivery progress (SP completion, velocity, projection) but says nothing about quality. F31 extends the existing `GET /api/analytics/epic-progress` endpoint and the Epics page UI with QA columns — coverage rate, pass rate, and bugs found per epic — derived bottom-up from each epic's feature tickets and their linked Test Executions.

This is purely additive: the existing response shape gains new optional fields; the existing UI gains conditionally-rendered QA columns. No new entities, no new endpoints, no migrations.

**Services impacted:** Fokus.API (endpoint, service, frontend), Fokus.Persistence (new repository query method).

## Scope

**In scope:**
- Extend `EpicProgressService` to compute per-epic and per-ticket QA metrics
- Extend response records with QA fields
- Extend the endpoint to load TE/TestRun data when Xray is enabled
- Add repository method to bulk-load TE links and runs for epic tickets
- Extend frontend types, store, and components (summary card, table columns, ticket detail columns, dual progress bar, RAG coloring)

**Out of scope:**
- Execution rate per epic
- Column sorting on QA columns
- Per-story drill-down into test runs
- Coverage trends across sprints
- Epic-level health score
- Deltas/sparklines

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | (none) | — | New repository method on TestExecutionRepository | |
| 2 | (none) | — | Extend EpicProgressService records + compute logic | |
| 3 | (none) | — | Extend endpoint to load QA data and pass to service | |
| 4 | (none) | — | Extend frontend types | |
| 5 | pinia-patterns | Follow | Extend epicsStore with settings dependency | |
| 6 | vue-component-architecture | Follow | Extend EpicSummaryCards, EpicTable, EpicTicketTable | |

All steps are "None" disposition on the backend because the `metric-query-patterns` skill gap identified in `docs/architecture/v1.md` still exists — no skill covers computed-on-read analytics extension patterns. Frontend steps reference applicable skills.

## Domain Model Changes

None. F31 reads existing entities: `TestExecution`, `TestExecutionLink`, `TestRun`, `Ticket`, `Developer`.

## Data Model Changes

None. No new tables, columns, or migrations.

## Implementation Steps

### Step 1: Add repository method to bulk-load epic QA data

**What:** Add a method to `TestExecutionRepository` that loads all non-cancelled TEs, their links (Tests and Blocks), and their test runs for a given set of ticket keys. This enables the service to compute coverage, pass rate, and bugs found per epic without N+1 queries.

**Files to modify:**
- `src/Services/Fokus/Fokus.Persistence/Repositories/TestExecutionRepository.cs`

**Accept:**
- Method signature: `GetTestExecutionDataForTicketsAsync(List<string> ticketKeys, CancellationToken ct)` returning a record/tuple with: (1) TE links grouped by ticket key for Tests links, (2) TE links grouped by ticket key for Blocks links, (3) test runs grouped by TE issue ID.
- Filters: only non-cancelled TEs (`Status != "Cancelled"`)
- Includes both `Tests` and `Blocks` link types (needed for bugs found via Blocks)
- Loads test runs for all relevant TEs in one query
- Pattern reference: `GetFeatureTicketsWithCoverageAsync` in same file (lines 118-239) for the TE link loading + run aggregation approach

### Step 2: Extend EpicProgressService with QA computation

**What:** Extend the response record hierarchy with QA fields, then add QA computation logic to the service. The service already receives all epic tickets grouped by epic; the QA computation attaches coverage/pass-rate/bugs to each epic entry and each ticket entry.

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/EpicProgressService.cs`

**Response record extensions:**

`EpicProgressResponse` gains:
- `bool HasQaData`
- `decimal? AverageTestCoverage`

`EpicProgressEntry` gains:
- `decimal? CoverageRate`
- `decimal? PassRate`
- `int BugsFound`
- `int FeatureTicketCount`
- `int CoveredTicketCount`
- `string? CoverageRag`
- `string? PassRateRag`

`EpicProgressTicketEntry` gains:
- `string? TestStatus` (Passed/Failed/InProgress/NoTests, null for bugs)
- `decimal? TestPassRate`
- `int? TestBugsFound`
- `TestRunSummaryDto? TestRunSummary` (new record: `{ int Passed, int Failed, int Todo, int Executing, int Aborted }`)

**Computation requirements (from spec BRs):**

Per-epic:
- BR1: Feature ticket scope = tickets where `IssueType != "Bug"` within the epic
- BR2: Coverage Rate = covered feature tickets / total feature tickets * 100. Covered = has at least one non-cancelled TE linked via `Tests`
- BR3: Pass Rate = PASS runs / (PASS + FAIL) runs across all non-cancelled TEs linked via `Tests` to the epic's feature tickets * 100. TODO/EXECUTING/ABORTED excluded from both sides.
- BR4: Bugs Found = count of unique bug ticket keys found via `Blocks` links from non-cancelled TEs that test this epic's feature tickets
- BR9: Cancelled TEs excluded
- BR12: Sub-tasks without own TE Tests links inherit parent ticket's TE links (check `ParentTicketKey`)
- BR13: Zero feature tickets → CoverageRate null, PassRate null

Per-ticket (in expanded detail):
- BR5: Test Status derived from all linked TE runs (Passed/Failed/InProgress/NoTests)
- BR6: Per-ticket pass rate = PASS / (PASS + FAIL) across ticket's non-cancelled TEs
- BR7: Per-ticket bugs found = unique bugs from Blocks links on ticket's TEs
- Bug tickets get null for all QA fields

Summary:
- BR16: Average Test Coverage = arithmetic mean of non-null coverage rates across epics. Null coverage epics excluded from mean.
- BR20: RAG via `HealthScoreCalculator.MetricRag(value, thresholds.CoverageGreen, thresholds.CoverageAmber, higherIsBetter: true)` and same for pass rate

**Method signature change:** `ComputeEpicProgress` gains an additional parameter for QA data (the result from Step 1's repo method), plus `AppSettings` already provides `QaHealthThresholds` and `XrayEnabled`.

**Accept:**
- All spec BRs (BR1-BR24) satisfied
- Existing F14 behavior unchanged when Xray disabled (hasQaData=false, all QA fields null/0)
- Sub-team filter applies to QA feature ticket scope (same filter already exists in service)

### Step 3: Extend endpoint to load QA data

**What:** Modify `GetEpicProgressEndpoint` to check `settings.XrayEnabled`, and when true, load QA data from the repository and pass it to the service.

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetEpicProgress/GetEpicProgressEndpoint.cs`

**Accept:**
- When `XrayEnabled == false`: endpoint does NOT call the repository method, passes null/empty QA data to the service. Response has `hasQaData=false`, all QA fields null.
- When `XrayEnabled == true`: endpoint loads TE data for all epic ticket keys (from `epicTickets` already loaded in step 5 of current endpoint) using the new repository method, passes to service.
- No additional DB round-trips when Xray disabled (performance: fast path identical to current F14).
- Pattern reference: `GetQaMetricsEndpoint.cs` lines 29-33 for the Xray-disabled early-return guard.

### Step 4: Extend frontend TypeScript types

**What:** Update the `EpicProgressResponse`, `EpicProgressEntry`, and `EpicProgressTicketEntry` interfaces to include the new QA fields.

**Files to modify:**
- `client/src/types/index.ts`

**New fields on `EpicProgressResponse`:**
- `hasQaData: boolean`
- `averageTestCoverage: number | null`

**New fields on `EpicProgressEntry`:**
- `coverageRate: number | null`
- `passRate: number | null`
- `bugsFound: number`
- `featureTicketCount: number`
- `coveredTicketCount: number`
- `coverageRag: string | null`
- `passRateRag: string | null`

**New fields on `EpicProgressTicketEntry`:**
- `testStatus: string | null`
- `testPassRate: number | null`
- `testBugsFound: number | null`
- `testRunSummary: { passed: number; failed: number; todo: number; executing: number; aborted: number } | null`

### Step 5: Extend epics store

**What:** The store needs access to `settingsStore` to check `xrayEnabled` for conditional rendering. Add a computed `isXrayEnabled` that reads from settingsStore, and ensure `initialize()` fetches settings if not already loaded.

**Files to modify:**
- `client/src/stores/epicsStore.ts`

**Skill:** Follow `pinia-patterns` — store composition, storeToRefs for cross-store reads.

**Feature-specific inputs:**
- Import and use `useSettingsStore` to expose `isXrayEnabled` computed
- Add `averageTestCoverage` computed that reads from `epicProgress.value?.averageTestCoverage`
- Ensure settings are loaded during `initialize()` (call `settingsStore.fetchSettings()` in the Promise.all if not already cached)
- Pattern reference: `client/src/views/DashboardView.vue` lines 31, 48, 55 for the settings + xrayEnabled gating pattern

### Step 6: Extend UI components

**What:** Add QA columns and visual elements to the Epics page components. All additions are conditionally rendered based on `hasQaData` / `xrayEnabled`.

**Files to modify:**
- `client/src/components/epics/EpicSummaryCards.vue`
- `client/src/components/epics/EpicTable.vue`
- `client/src/components/epics/EpicTicketTable.vue`
- `client/src/views/EpicsView.vue`

**Skill:** Follow `vue-component-architecture` — conditional column rendering, prop additions.

**Feature-specific inputs:**

**EpicSummaryCards.vue:**
- Add 4th card "Average Test Coverage" — conditionally rendered when `hasQaData && averageTestCoverage !== null`
- Shows `averageTestCoverage` as `XX.X%` or "—" when null
- Grid changes from `grid-cols-3` to `grid-cols-3 lg:grid-cols-4` (or responsive equivalent)
- Prop additions: `hasQaData: boolean`, `averageTestCoverage: number | null`

**EpicTable.vue:**
- Add 3 columns after "Sprints": Coverage %, Pass Rate %, Bugs Found — conditionally rendered with `v-if="hasQaData"`
- Coverage % and Pass Rate % cells use RAG coloring: `text-status-success` (green), `text-status-warning` (amber), `text-status-danger` (red) — pattern from `QualityDevTable.vue` `ragClass()` function
- Null values display "—"
- Dual progress bar: add a second thinner bar below the existing SP completion bar. Color: use `bg-emerald-500` or equivalent QA accent distinct from `bg-accent-default`. Conditionally rendered when `coverageRate !== null`.
- Prop additions: `hasQaData: boolean`
- Update expanded row `colspan` from 8 to 11 when hasQaData is true

**EpicTicketTable.vue:**
- Add 3 columns after "Assignee": Test Status, Pass Rate, Bugs Found — conditionally rendered with `v-if="hasQaData"`
- Test Status shows badge: Passed (green bg), Failed (red bg), In Progress (amber bg), No Tests (gray bg) — pattern from existing status badge in same file
- Bug tickets (issueType === 'Bug') show "—" in QA columns
- Prop additions: `hasQaData: boolean`

**EpicsView.vue:**
- Pass `hasQaData` and `averageTestCoverage` to `EpicSummaryCards`
- Pass `hasQaData` to `EpicTable` and `EpicTicketTable` (via EpicTable prop drilling or provide/inject)
- Import and use `useSettingsStore` to gate rendering — pattern: `settingsStore.settings.xrayEnabled`

**Accept:**
- BR21: Xray disabled → page identical to current F14 (no QA elements)
- BR22: Xray enabled, no data → QA columns show "—", no coverage bar, summary card shows "—"
- BR17: Summary card visible only when hasQaData and at least one epic has non-null coverage
- BR18: Dual progress bar below SP bar, distinct color, hidden when coverage null
- BR19: SP bar unchanged
- BR20: RAG coloring on Coverage % and Pass Rate % cells using thresholds from API response

## Cross-Service Changes

None. Single-service feature.

## Migration Notes

None. No schema changes.

## Testing Strategy

**Backend verification:**
- Build passes (`dotnet build`)
- Endpoint returns `hasQaData=false` with null QA fields when Xray disabled
- Endpoint returns populated QA fields when Xray enabled and TE data exists
- Coverage rate computes correctly: covered feature tickets / total feature tickets
- Pass rate excludes TODO/EXECUTING/ABORTED runs
- Bugs found counts unique bugs via Blocks links
- Sub-team filter scopes QA computations
- Bug-only epics return null coverage/pass rate
- Per-ticket test status logic: all PASS → Passed, any FAIL → Failed, TODO/EXECUTING only → InProgress, no TEs → NoTests

**Frontend verification:**
- QA columns hidden when Xray disabled (check settings)
- QA columns render with data when Xray enabled
- RAG coloring maps green/amber/red correctly
- Summary card appears/disappears based on hasQaData
- Dual progress bar renders below SP bar with distinct color
- Sub-team filter causes refetch and recalculation
- Active/Completed toggle shows QA data on both views

## KB Impact

Update `docs/kb/analytics/epic-progress.md` — add section documenting QA metric computation (coverage, pass rate, bugs found) and their relationship to the existing epic progress response.

## Open Questions

None.
