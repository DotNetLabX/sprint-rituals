# Per-Developer Story Quality

**Feature Spec:** `docs/specs/F28-PerDeveloperStoryQuality/definition/spec.md`

## Context

Fokus v2 has sprint-level QA metrics (F26) but no per-developer breakdown. Scrum Masters need to see whose stories are well-tested and whose are not — surfacing coverage gaps for retro conversations. F28 extends the Developers page with a "Quality" tab showing per-developer test coverage %, pass rate %, untested count, bugs found, delta indicators, sparklines, RAG coloring, and a below-median warning flag.

Service impacted: Fokus (single service). Layers: API (new endpoint + service), Frontend (new tab + components + store extensions + types + API module).

## Scope

**In scope:**
- Backend: `GET /api/analytics/developer-quality` endpoint with `DeveloperQualityService`
- Frontend: Quality tab on Developers page (table, chart, empty states, sorting, RAG, deltas, sparklines, warning flag)
- Tab visibility gated on `xrayEnabled` setting
- Sub-team filtering, multi-sprint averaging, single-sprint deltas/sparklines/warning

**Out of scope (per spec):**
- QA workload per person (F29)
- Execution rate per developer
- Expandable per-story detail rows
- Configurable warning threshold
- Pass Rate trend chart

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | create-feature | Follow | FastEndpoints GET endpoint, `GetDeveloperQuality` feature name, route `/api/analytics/developer-quality` | |
| 2 | extract-feature-service | Follow | `DeveloperQualityService` in `Features/Analytics/`, result records for per-developer quality | |
| 3 | error-handling | Follow | 400 for mutual exclusion (sprintId+last), invalid sprintId, last<1 | |
| 4 | (none) | — | Warning flag streak computation — novel algorithm, no existing skill | Log |
| 5 | create-vue-feature | Follow | Types, API function, store extension (not new store), component hierarchy | |
| 6 | pinia-patterns | Follow | Extend `developersStore` with quality state/actions, lazy fetch on tab switch | |
| 7 | vue-component-architecture | Follow | L1 `QualityTab.vue`, L3 `QualityDevTable.vue`, L3 `QualityTrendChart.vue` | |
| 8 | vue-patterns | Follow | Script setup, computed, watch, defineProps, ApexCharts integration | |
| 9 | tailwind-theme | Follow | RAG coloring using existing theme tokens (`text-status-success`, `text-status-danger`, `text-status-warning`) | |

## Domain Model Changes

None. F28 reads from existing entities: TestExecution, TestExecutionLink, TestRun, Ticket, Developer, SprintMembership, AppSettings.

## Data Model Changes

None. No new tables, columns, or migrations. All metrics computed on-the-fly from existing data.

## Implementation Steps

### Step 1: Backend response records and service

**What:** Create `DeveloperQualityService.cs` in `Features/Analytics/` with result records and computation logic.

**Files to create:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/DeveloperQualityService.cs`

**Skill:** Follow `extract-feature-service` for service structure. Service name: `DeveloperQualityService`. Result records defined in the same file above the service class.

**Result records:**

Top-level result:
- `DeveloperQualityResult(bool HasQaData, List<DeveloperQualitySprintInfo> Sprints, List<DeveloperQualityEntry> Developers)`

Sprint info (reuse shape from `SprintSummaryItem` already in `DeveloperThroughputService.cs`):
- `DeveloperQualitySprintInfo(int Id, string Name, DateTime StartDate, DateTime EndDate)`

Per-developer entry:
- `DeveloperQualityEntry(string AccountId, string DisplayName, string? SubTeam, string? AvatarUrl, List<DeveloperQualitySprintBreakdown> SprintBreakdowns, int? BelowMedianStreak)`

Per-sprint breakdown:
- `DeveloperQualitySprintBreakdown(int SprintId, int Stories, int Covered, decimal CoveragePercent, decimal PassRatePercent, int Untested, int BugsFound, string? CoverageRag, string? PassRateRag, decimal? CoveragePercentDelta, string? CoveragePercentDeltaDirection, string? CoveragePercentDeltaPolarity, decimal? PassRatePercentDelta, string? PassRatePercentDeltaDirection, string? PassRatePercentDeltaPolarity, int? BugsFoundDelta, string? BugsFoundDeltaDirection, string? BugsFoundDeltaPolarity, List<SparklinePoint>? CoverageSparkline, List<SparklinePoint>? PassRateSparkline)`

**Service method signature:**
```
DeveloperQualityResult ComputeDeveloperQuality(
    List<Sprint> allLoadedSprints,
    List<int> targetSprintIds,
    List<Developer> activeDevelopers,
    Dictionary<int, List<TestExecution>> tesBySprintId,
    List<StatusTransition> statusTransitions,
    AppSettings settings,
    string? subTeam,
    bool isSingleSprint,
    Sprint? priorSprint,
    List<TestExecution>? priorSprintTEs,
    List<Sprint> sparklineWindow,
    Dictionary<int, List<TestExecution>> sparklineTEsBySprintId)
```

**Computation logic (per-developer, per-sprint) — references:**

The service reuses existing helper patterns from `QaMetricsService.cs` but scoped per developer:

1. **Active scope per developer (BR1):** For each developer, filter sprint memberships to `m.Ticket?.AssigneeId == developer.Id`, then apply the same active-scope logic as `QaMetricsService.GetActiveFeatureTicketKeys()`: not removed, not bug, not excluded status, has effective SP, IsStartedInSprint. Reference: `QaMetricsService.cs` lines 169-192.

2. **Coverage per developer (BR2-BR3):** From this developer's active-scope ticket keys, find those with at least one non-cancelled TE via Tests link. Sub-task inheritance (BR10): if a sub-task (attributed to its own assignee) has no own TE links, inherit from parent's TEs. Reference: `QaMetricsService.ComputeCoverageRate()` lines 249-283.

3. **Pass Rate per developer (BR4):** PASS / (PASS + FAIL) across all non-cancelled TEs linked to this developer's stories. Scoped to TEs from `BuildTestsTesByTicket()` for this developer's ticket keys. Reference: `QaMetricsService.ComputePassRate()` lines 316-329, but scoped to developer's TEs only.

4. **Bugs Found per developer (BR6):** Unique bug tickets linked via Blocks links from non-cancelled TEs that test this developer's stories. A bug linked from multiple TEs on the same developer counts once. Reference: `QaMetricsService.ComputeBugsFound()` lines 334-348, scoped to developer's TEs.

5. **Division by zero (BR11-13):** Zero stories -> 0% coverage, 0% pass rate. Zero covered -> 0% pass rate. Zero PASS+FAIL runs -> 0% pass rate.

6. **Delta computation (BR20, single-sprint only):** Current minus prior sprint value. Direction: up/down/flat. Polarity: Coverage % and Pass Rate % are higher-is-better (positive=green, negative=red). Bugs Found is neutral polarity. Delta is null when prior sprint has no QA data. Reference: `DeveloperThroughputService` delta helpers, lines 316-330.

7. **Sparkline (BR21, single-sprint only):** Up to 4 trailing sprints with QA data, ending at selected sprint. Compute per-developer coverage % and pass rate % for each sparkline sprint. Use `SparklinePoint(sprintName, value)` — same record type used by `QaMetricsService`. Reference: `QaMetricsService.BuildQaSparkline()` pattern.

8. **RAG coloring (BR18):** Use `HealthScoreCalculator.MetricRag()` with `settings.QaHealthThresholds.CoverageGreen/CoverageAmber` for coverage cells, `PassRateGreen/PassRateAmber` for pass rate cells, both `higherIsBetter: true`.

9. **Warning flag / belowMedianStreak (BR14-17, single-sprint only):**
   - Compute team median coverage % from all active developers with >= 1 story in the sprint. Exclude zero-story developers.
   - If sub-team filter active, recalculate median from only that sub-team's developers.
   - Walk backward from selected sprint through consecutive closed sprints with QA data: for each sprint, check if this developer's coverage % is strictly below that sprint's team median. A sprint where the developer has zero stories breaks the streak. A sprint with no QA data breaks the streak.
   - `BelowMedianStreak` = streak length. Null when streak < 2.

**Performance approach:** Bulk-load all TEs for the target sprints + sparkline window in the endpoint (see Step 2). The service receives pre-loaded data and iterates in memory — no N+1 queries.

**Dependencies:** None (first step).

---

### Step 2: Backend endpoint and validator

**What:** Create the `GetDeveloperQuality` endpoint and request validator.

**Files to create:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetDeveloperQuality/GetDeveloperQualityEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetDeveloperQuality/GetDeveloperQualityQuery.cs`

**Skill:** Follow `create-feature` (FastEndpoints variant). Follow `error-handling` for 400 responses.

**Request class:** `GetDeveloperQualityRequest` with `int? SprintId`, `int? Last`, `string? SubTeam`. Same shape as `GetDeveloperThroughputRequest`.

**Validator class:** `GetDeveloperQualityRequestValidator` — same rules as `GetDeveloperThroughputRequestValidator`:
- SprintId > 0 when provided
- Last >= 1 when provided
- SprintId and Last cannot both be provided (400)
- SubTeam not empty when provided

**Response class:** `DeveloperQualityResponse` with:
- `bool HasQaData`
- `List<DeveloperQualitySprintInfo> Sprints`
- `List<DeveloperQualityResponseEntry> Developers`

The response entry mirrors the result record from Step 1 but as a response DTO class (mutable properties for serialization). Define in the query file.

**Endpoint:** `GetDeveloperQualityEndpoint` — `Endpoint<GetDeveloperQualityRequest, DeveloperQualityResponse>`.

Route: `[HttpGet("/api/analytics/developer-quality")]`, Tags: `["Analytics"]`.

**HandleAsync orchestration** — follow the pattern in `GetDeveloperThroughputEndpoint.cs`:

1. Load settings. If `!settings.XrayEnabled`, return `{ HasQaData = false, Sprints = [], Developers = [] }`.
2. Load all closed sprints (lightweight).
3. Determine target sprint IDs based on mode (sprintId / last / all).
4. **QA-data filtering for multi-sprint mode:** In multi-sprint mode (when `last` is provided or neither param given), filter to only sprints that have QA data. A sprint has QA data when `TestExecutionRepository.GetTestExecutionsForSprintAsync(sprintId)` returns a non-empty list. To avoid N queries, add a new repository method `GetSprintIdsWithQaDataAsync(List<int> sprintIds)` that checks for existence of non-cancelled TEs linked to each sprint (see Step 3). In single-sprint mode, always include the requested sprint regardless.
5. Validate: if `sprintId` provided and sprint not found among closed sprints, return 400.
6. Load sprints with memberships for target + sparkline window.
7. Load active developers, filter by sub-team.
8. Load status transitions for all sprint tickets.
9. Bulk-load TEs for all target sprints and sparkline sprints via `GetTestExecutionsForSprintAsync()` into `Dictionary<int, List<TestExecution>>`.
10. Determine prior sprint (single-sprint mode only): the closed sprint immediately before the target. Load its TEs.
11. Build sparkline window (single-sprint only): up to 4 trailing sprints with QA data ending at selected.
12. Call `DeveloperQualityService.ComputeDeveloperQuality()`.
13. Map result to response, send 200.

**DI registration:** Add `services.AddScoped<DeveloperQualityService>();` to `DependencyInjection.cs` (line after `QaMetricsService` registration).

**Pattern reference:** `GetDeveloperThroughputEndpoint.cs` for orchestration flow, `GetQaMetricsEndpoint.cs` for TE loading and sparkline window construction.

**Dependencies:** Step 1.

---

### Step 3: Repository method for QA-data sprint detection

**What:** Add `GetSprintIdsWithQaDataAsync` to `TestExecutionRepository` to efficiently check which sprints have QA data.

**Files to modify:**
- `src/Services/Fokus/Fokus.Persistence/Repositories/TestExecutionRepository.cs`

**Skill:** None — repository extension, simple query.

**Method signature:**
```csharp
public async Task<HashSet<int>> GetSprintIdsWithQaDataAsync(List<int> sprintIds, CancellationToken ct = default)
```

**Logic:** Query `TestExecutionLinks` joined with `SprintMemberships` and `TestExecutions` (non-cancelled), grouped by sprint ID, returning sprint IDs that have at least one non-cancelled TE. This avoids loading full TE objects just to check existence.

**Pattern reference:** `GetTestExecutionsForSprintAsync()` in the same file for the join pattern between TestExecutionLinks and SprintMemberships.

**Dependencies:** None (can be done in parallel with Step 1).

---

### Step 4: Frontend TypeScript types

**What:** Add TypeScript interfaces for the developer quality API response.

**Files to modify:**
- `client/src/types/index.ts`

**Skill:** Follow `create-vue-feature` Step 1 (define types).

**Types to add (append after the QA Metrics types section):**

```typescript
// Developer Quality types
export interface DeveloperQualitySprintInfo {
  id: number
  name: string
  startDate: string
  endDate: string
}

export interface DeveloperQualitySprintBreakdown {
  sprintId: number
  stories: number
  covered: number
  coveragePercent: number
  passRatePercent: number
  untested: number
  bugsFound: number
  coverageRag: string | null
  passRateRag: string | null
  coveragePercentDelta: number | null
  coveragePercentDeltaDirection: string | null
  coveragePercentDeltaPolarity: string | null
  passRatePercentDelta: number | null
  passRatePercentDeltaDirection: string | null
  passRatePercentDeltaPolarity: string | null
  bugsFoundDelta: number | null
  bugsFoundDeltaDirection: string | null
  bugsFoundDeltaPolarity: string | null
  coverageSparkline: SparklinePoint[] | null
  passRateSparkline: SparklinePoint[] | null
}

export interface DeveloperQualityEntry {
  accountId: string
  displayName: string
  subTeam: string | null
  avatarUrl: string | null
  sprintBreakdowns: DeveloperQualitySprintBreakdown[]
  belowMedianStreak: number | null
}

export interface DeveloperQualityResponse {
  hasQaData: boolean
  sprints: DeveloperQualitySprintInfo[]
  developers: DeveloperQualityEntry[]
}
```

**Dependencies:** None (can be done in parallel with backend steps).

---

### Step 5: Frontend API function

**What:** Add the `getDeveloperQuality` API function.

**Files to modify:**
- `client/src/api/analytics.ts`

**Skill:** Follow `create-vue-feature` Step 2 (API module).

**Function to add:**
```typescript
export function getDeveloperQuality(sprintId?: number, last?: number, subTeam?: string): Promise<DeveloperQualityResponse> {
  const params = new URLSearchParams()
  if (sprintId !== undefined) params.set('sprintId', String(sprintId))
  if (last !== undefined) params.set('last', String(last))
  if (subTeam) params.set('subTeam', subTeam)
  const query = params.toString()
  return apiFetch<DeveloperQualityResponse>(`/analytics/developer-quality${query ? `?${query}` : ''}`)
}
```

**Pattern reference:** `getDeveloperThroughput()` in the same file — identical parameter shape.

**Dependencies:** Step 4 (types must exist for the import).

---

### Step 6: Extend developers store with quality state and actions

**What:** Add quality tab state, loading flags, fetch action, and tab switching logic to the existing `developersStore`.

**Files to modify:**
- `client/src/stores/developersStore.ts`

**Skill:** Follow `pinia-patterns` for async action pattern. Follow the existing tab pattern in `developersStore.ts` (see `bugRatio` / `leaderboard` state + fetch + switchTab).

**Changes:**

1. Import `DeveloperQualityResponse` type and `getDeveloperQuality` API function.
2. Import `useSettingsStore` for `xrayEnabled` check.
3. Add state refs:
   - `quality: ref<DeveloperQualityResponse | null>(null)`
   - `qualityLoading: ref(false)`
   - `qualityError: ref<string | null>(null)`
4. Extend the `activeTab` type to include `'quality'`: change from `'throughput' | 'bugRatio' | 'leaderboard'` to `'throughput' | 'bugRatio' | 'leaderboard' | 'quality'`.
5. Add `fetchQuality()` async action — follows the exact pattern of `fetchBugRatio()`: check sprint mode, call API with appropriate params, set loading/error state.
6. Update `switchTab()` to call `fetchQuality()` when tab is `'quality'`.
7. Update `selectSprint()`, `selectLastN()`, `selectSubTeam()` to also call `fetchQuality()` when activeTab is `'quality'`.
8. Expose new state and actions in the return object.

**Pattern reference:** `fetchBugRatio()` and `fetchLeaderboard()` in the same store — identical pattern.

**Dependencies:** Steps 4, 5.

---

### Step 7: Quality tab components

**What:** Create the Quality tab component hierarchy: `QualityTab.vue` (L1 feature container), `QualityDevTable.vue` (L3 table), `QualityTrendChart.vue` (L3 chart).

**Files to create:**
- `client/src/components/developers/QualityTab.vue`
- `client/src/components/developers/QualityDevTable.vue`
- `client/src/components/developers/QualityTrendChart.vue`

**Skill:** Follow `vue-component-architecture` for levels. Follow `vue-patterns` for script setup. Follow `tailwind-theme` for tokens.

**QualityTab.vue (L1, < 200 lines):**
- Props: `data: DeveloperQualityResponse`, `sprintMode: 'single' | 'multi'`
- Determines single vs multi view
- Single-sprint: renders `QualityDevTable` (with deltas, sparklines, warning flags)
- Multi-sprint: renders `QualityDevTable` (averaged, no deltas/sparklines/flags) + `QualityTrendChart`
- Pattern reference: `BugRatioTab.vue`

**QualityDevTable.vue (L3, < 200 lines):**
- Props: `developers`, `sprints`, `sprintMode`, `isSingleSprint`
- Table columns: Developer, Sub-Team, Stories, Covered, Coverage %, Pass Rate %, Untested, Bugs Found
- **RAG coloring on Coverage % and Pass Rate % cells:** Apply `text-status-success` (green), `text-status-warning` (amber), `text-status-danger` (red) based on the `coverageRag` and `passRateRag` fields from the response.
- **Delta indicators (single-sprint only):** On Coverage %, Pass Rate %, and Bugs Found. Coverage % and Pass Rate % use higher-is-better polarity colors. Bugs Found uses neutral polarity (no color, gray text). Stories, Covered, and Untested do NOT show deltas.
- **Sparklines (single-sprint only):** Inline mini charts on Coverage % and Pass Rate % cells. Use a minimal SVG polyline or small ApexCharts sparkline (same pattern as F26 if it exists, otherwise inline SVG points from the sparkline data).
- **Warning flag (single-sprint only):** Amber triangle icon on rows where `belowMedianStreak >= 2`. Tooltip: "Coverage below team median for N consecutive sprints". Pattern reference: `BugRatioDevTable.vue` alert badge.
- **Sorting:** Sortable column headers. Default sort: Coverage % ascending (lowest first). Same sort toggle pattern as `BugRatioDevTable.vue`.
- **Multi-sprint averaging:** When `sprintMode === 'multi'`, compute averages from `sprintBreakdowns` array. Exclude sprints where `stories === 0` from the developer's average (spec BR22). No deltas, sparklines, or warning flags.
- **Zero-story developers:** Appear with 0 values, not hidden.
- **Tooltips:** Wire tooltip text from `help.tooltips.md` to column headers: Stories, Covered, Coverage %, Pass Rate %, Untested, Bugs Found.
- Pattern reference: `BugRatioDevTable.vue` for table structure, sorting, delta rendering, alert badge

**QualityTrendChart.vue (L3, < 100 lines):**
- Props: `developers: DeveloperQualityEntry[]`, `sprints: DeveloperQualitySprintInfo[]`
- Multi-line ApexChart: X-axis = sprint names, Y-axis = Coverage %, one line per developer
- Each developer = separate series. Data from `sprintBreakdowns[].coveragePercent`
- Hover tooltip: exact coverage % for developer + sprint
- Chart options: dark theme, transparent background, no toolbar, smooth curves
- Pattern reference: `chartSeries` + `chartOptions` computed properties in `DevelopersView.vue` (lines 176-207)

**Dependencies:** Steps 4, 6.

---

### Step 8: Wire Quality tab into DevelopersView

**What:** Add the Quality tab button to the tab bar, render QualityTab component, gate on xrayEnabled, and handle empty states.

**Files to modify:**
- `client/src/views/DevelopersView.vue`

**Skill:** Follow `vue-patterns`. Follow `vue-component-architecture` (L0 page stays < 100 lines — delegating to QualityTab component).

**Changes:**

1. Import `QualityTab` component and `useSettingsStore`.
2. In `onMounted`: also call `settingsStore.fetchSettings()` (parallel with `store.initialize()`). If `activeTab === 'quality'` and quality data not loaded, call `store.fetchQuality()`.
3. Extend URL sync: handle `tab=quality` in both reading (`onMounted`) and writing (`watch`).
4. Extend `onTabSwitch` parameter type to include `'quality'`.
5. **Tab bar:** Add 4th tab button "Quality" AFTER "Leaderboard". Conditionally render only when `settingsStore.settings.xrayEnabled` is true. Pattern: same as existing tab buttons.
6. **Tab content:** Add `<template v-else-if="store.activeTab === 'quality'">` section:
   - Loading state: `store.qualityLoading` -> "Updating..."
   - Error state: `store.qualityError` -> red error text
   - Empty state when `store.quality?.hasQaData === false`: "Sync sprint to load QA data" prompt using `EmptyState` component.
   - Data display: `<QualityTab :data="store.quality" :sprint-mode="store.sprintMode" />` when quality data is loaded and hasQaData is true.

**Pattern reference:** BugRatio tab rendering in `DevelopersView.vue` lines 540-544. DashboardView `xrayEnabled` check pattern.

**Dependencies:** Steps 6, 7.

---

### Step 9: Build verification and cleanup

**What:** Verify the full solution builds and the frontend compiles without errors.

**Commands:**
- `dotnet build` (from solution root)
- `cd client && npm run build` (Vite build)

**Checks:**
- No TypeScript errors
- No C# build errors
- No unused imports
- No debug artifacts (Console.WriteLine, TODO, HACK)

**Dependencies:** All previous steps.

## Cross-Service Changes

None. Single service, no gRPC, no integration events.

## Migration Notes

None. No schema changes. No seed data.

## Testing Strategy

**Backend scenarios:**
1. Single sprint, developer with covered + uncovered stories -> correct coverage %, pass rate %, untested count
2. Developer with zero stories -> all metrics at 0, excluded from median computation
3. Sub-task without own TEs inherits parent coverage (BR10), attributed to sub-task assignee
4. TE linked to multiple developers' stories -> counts for each developer independently (BR9)
5. Cancelled TEs excluded from all computations (BR8)
6. Pass rate: only PASS and FAIL runs count, TODO/EXECUTING/ABORTED excluded (BR4)
7. Bugs found: unique bug count per developer, deduped across TEs (BR6)
8. Below-median streak: developer below median for 3 consecutive sprints -> belowMedianStreak = 3
9. Below-median streak: sprint with zero stories breaks the streak (BR15)
10. Below-median streak: sprint with no QA data breaks the streak (BR15)
11. Sub-team filter recalculates median using only filtered sub-team (BR16)
12. Multi-sprint mode: only sprints with QA data included in response
13. Multi-sprint mode: no deltas, sparklines, or warning flags in response
14. Delta is null when prior sprint has no QA data (BR20)
15. Both sprintId and last provided -> 400

**Frontend scenarios:**
1. Quality tab hidden when xrayEnabled is false
2. Quality tab shows empty state when hasQaData is false
3. Single-sprint table shows deltas, sparklines, RAG colors, warning icons
4. Multi-sprint table shows averages (excluding zero-story sprints), no deltas/sparklines/flags
5. Multi-sprint chart renders one line per developer
6. Sorting by Coverage % ascending is default
7. Sub-team filter re-fetches and updates table + chart

## Open Questions

None.
