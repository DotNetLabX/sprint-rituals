# Per-Developer Story Quality — Implementation

## Files Created

- `src/Services/Fokus/Fokus.API/Features/Analytics/DeveloperQualityService.cs` — Result records (`DeveloperQualityResult`, `DeveloperQualitySprintInfo`, `DeveloperQualityEntry`, `DeveloperQualitySprintBreakdown`) and computation logic: per-developer active scope, coverage, pass rate, bugs found, delta, sparkline, RAG, and below-median streak.
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetDeveloperQuality/GetDeveloperQualityQuery.cs` — Request class, validator (`GetDeveloperQualityRequestValidator`), and response DTOs (`DeveloperQualityResponse`, `DeveloperQualityEntryResponse`, `DeveloperQualitySprintBreakdownResponse`, `SparklinePointResponse`, `DeveloperQualitySprintInfoResponse`).
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetDeveloperQuality/GetDeveloperQualityEndpoint.cs` — GET `/api/analytics/developer-quality` endpoint; orchestrates sprint selection, QA-data filtering, TE bulk-load, prior sprint, sparkline window, and service call.
- `client/src/components/developers/QualityTab.vue` — L1 feature container; renders `QualityDevTable` in both modes, `QualityTrendChart` in multi-sprint mode.
- `client/src/components/developers/QualityDevTable.vue` — L3 sortable table; RAG coloring, delta indicators, inline SVG sparklines, below-median warning flag, multi-sprint averaging with zero-story exclusion.
- `client/src/components/developers/QualityTrendChart.vue` — L3 ApexCharts multi-line chart; Coverage % per developer over sprints; dark theme.

## Files Modified

- `src/Services/Fokus/Fokus.Persistence/Repositories/TestExecutionRepository.cs` — Added `GetSprintIdsWithQaDataAsync(List<int> sprintIds)`: efficient EXISTS-style query returning sprint IDs that have at least one non-cancelled TE linked via SprintMemberships.
- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` — Registered `DeveloperQualityService` as scoped.
- `client/src/types/index.ts` — Added `DeveloperQualitySprintInfo`, `DeveloperQualitySprintBreakdown`, `DeveloperQualityEntry`, `DeveloperQualityResponse` interfaces.
- `client/src/api/analytics.ts` — Added `getDeveloperQuality()` function; identical parameter shape to `getDeveloperThroughput`.
- `client/src/stores/developersStore.ts` — Extended `activeTab` union type to include `'quality'`; added `quality`, `qualityLoading`, `qualityError` state refs; added `fetchQuality()` action; updated `switchTab`, `selectSprint`, `selectLastN`, `selectSubTeam` to trigger quality fetch when active.
- `client/src/views/DevelopersView.vue` — Imported `QualityTab` and `useSettingsStore`; parallel settings fetch in `onMounted`; URL sync for `tab=quality`; Quality tab button gated on `xrayEnabled`; tab content with loading/error/empty/data states.

## Key Decisions

- **Inline SVG sparklines in `QualityDevTable`**: The spec mentioned "inline SVG polyline or small ApexCharts sparkline." Used inline SVG `<path>` (same approach as F26's `QaMetricsCard`) to avoid a per-cell ApexCharts instance overhead.
- **`belowMedianStreak` null vs. number**: Service returns `null` when streak < 2 (per BR14 — warning only appears when streak >= 2). Frontend checks `!== null` before rendering the flag.
- **SparklinePoint reuse**: Reused the existing `SparklinePoint` record already defined in `QaMetricsService.cs` rather than defining a new one — no duplication.
- **Response DTO naming**: Plan said `DeveloperQualityResponseEntry` but named as `DeveloperQualityEntryResponse` to match the project's established convention (noun + `Response` suffix, e.g., `DeveloperQualitySprintBreakdownResponse`).

## Deviations from Plan

- **Step 2 DTO name**: Plan used `DeveloperQualityResponseEntry`; implemented as `DeveloperQualityEntryResponse` to follow existing naming convention (`{Concept}Response` suffix pattern seen throughout the codebase). No functional impact.
- **Pre-existing MSB3492 build error**: `dotnet build` at solution level fails due to a file-locking issue on `AssemblyInfoInputs.cache` for building block projects (confirmed pre-existing by reproducing on clean `main`). Backend was verified by building `Fokus.API.csproj` directly — 0 errors, 3 warnings (all pre-existing: NU1903 vulnerability advisory, CS9107 on `DeveloperRepository`).

## Review Fix Cycle 1

### HIGH 1 — Below-median streak capped at sparkline window

**Fix in `GetDeveloperQualityEndpoint.cs`:**
- Added `streakWindow` (List<Sprint>) and `streakTEsBySprintId` (Dictionary<int, List<TestExecution>>) variables in the single-sprint block.
- Load all closed sprint IDs up to and including the target, filter to those with QA data (same `GetSprintIdsWithQaDataAsync` call already made for sparkline), then `TakeLast(12)` to cap the streak window.
- Load those sprints with memberships via `GetSprintsWithMembershipsAsync`.
- Load TEs for streak sprints, reusing already-loaded sparkline TEs where sprint IDs overlap.
- Extended `transitionSprintIds` to include streak window sprints so `GetStatusTransitionsForSprintTicketsAsync` covers them.
- Extended `allLoadedSprints` concat to include streak window sprints (deduplicated via `GroupBy(s => s.Id).Select(g => g.First())`).
- Added `streakWindow` and `streakTEsBySprintId` to the `ComputeDeveloperQuality` call.

**Fix in `DeveloperQualityService.cs`:**
- `ComputeBelowMedianStreak` signature updated: replaced `sortedAllSprints` + two TE dicts with `streakSprints` (pre-filtered, ascending) + single `streakTEsBySprintId` dict. The streak loop now looks up TEs from the single dict — no fallback chain needed since the streak window is pre-filtered to QA-bearing sprints.
- Internal call site in `ComputeDeveloperQuality` updated to match new signature (removed extra TE dict param).
- Streak is now accurate up to 12 sprints; a developer below median for 8 consecutive sprints correctly shows `belowMedianStreak = 8`.

### HIGH 2 — `GetSprintIdsWithQaDataAsync` missing BR8 tiebreaker

Already fixed in the initial implementation round. The repository method applies the `g.Max(x => x.SprintId) == sprintId` grouping before returning sprint IDs, matching `GetTestExecutionsForSprintAsync` semantics.

### MEDIUM — Sub-task inheritance bleeds into pass rate and bugs found

Already fixed in the initial implementation round. `ComputeDevMetrics` builds `directTeIds` from only the developer's own active-scope ticket keys (no parent TE inheritance). The sub-task coverage block marks a story as covered via parent TEs but does not add those parent TE IDs to the direct set used for pass rate and bug count.
