# Implementation — adhoc-Pass3c-ComputationToDomain (3c-A)

**Plan:** docs/specs/adhoc-Pass3c-ComputationToDomain/delivery/plan.md
**Status:** Complete

---

## Steps Completed

### Step 1 — Relocate 4 static helpers to Fokus.Domain/Analytics/

**Files created (domain):**
- `src/Services/Fokus/Fokus.Domain/Analytics/HealthScoreCalculator.cs` — moved from API, namespace `Fokus.Domain.Analytics`
- `src/Services/Fokus/Fokus.Domain/Analytics/TransitionAttributionChecker.cs` — moved from API, namespace `Fokus.Domain.Analytics`
- `src/Services/Fokus/Fokus.Domain/Analytics/CompletionChecker.cs` — moved from API, namespace `Fokus.Domain.Analytics`
- `src/Services/Fokus/Fokus.Domain/Analytics/ExcludedDeveloperFilter.cs` — moved from API, namespace `Fokus.Domain.Analytics`

**Deviation:** `Fokus.Domain/GlobalUsings.cs` was missing `global using Fokus.Domain;` — added it. All domain types (`Sprint`, `Developer`, `DeveloperSprintCapacity`, `StatusTransition`, etc.) are in namespace `Fokus.Domain` with no sub-namespace; without this global using the domain layer could not resolve its own types.

Build: green.

---

### Step 2 — Create TestTimelineCalculator + TestTimelineResult.cs; update GetTestTimelineEndpoint

**Files created (domain):**
- `src/Services/Fokus/Fokus.Domain/Analytics/TestTimelineResult.cs` — domain result records (`TestTimelineData`, `BurnupDayData`, `ScopeChangeDayData`, `TestingCrunchData`, `CrunchTicketData`, `PostSprintTestingData`, `PostSprintTicketData`, `UntestedAtCloseData`, `UntestedTicketData`, `DevToTestGapData`, `GapTicketData`, `TestingCrunchFlag`)
- `src/Services/Fokus/Fokus.Domain/Analytics/TestTimelineCalculator.cs` — domain calculator; `ComputeTimeline(...)` returns `TestTimelineData`; `static ComputeCrunchFlag(...)` returns `TestingCrunchFlag?`

**Files created (API wire):**
- `src/Services/Fokus/Fokus.API/Features/Sprints/GetTestTimeline/GetTestTimelineResponse.cs` — wire types (`TestTimelineResponse`, all nested entry types) + Mapster `IRegister` (`TestTimelineMap`) mapping `TestTimelineData → TestTimelineResponse`

**Files modified:**
- `src/Services/Fokus/Fokus.API/Features/Sprints/GetTestTimeline/GetTestTimelineEndpoint.cs` — replaced `TestTimelineService` with `TestTimelineCalculator`; uses `result.Adapt<TestTimelineResponse>()`

Build: green. `TestingCrunchFlag` is now a domain type.

---

### Step 3 — Create QaMetricsCalculator + QaMetricsResult.cs; update GetQaMetricsEndpoint

**Files created (domain):**
- `src/Services/Fokus/Fokus.Domain/Analytics/QaMetricsResult.cs` — `QaMetricsData`, `QaMetricCardData`, `QaSparklinePoint`, `QaBreakdownData`
- `src/Services/Fokus/Fokus.Domain/Analytics/QaMetricsCalculator.cs` — `ComputeQaMetrics(...)` returns `QaMetricsData`

**Files modified:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetQaMetrics/GetQaMetricsEndpoint.cs` — replaced `QaMetricsService` with `QaMetricsCalculator`; `MapResponse(QaMetricsData result)` maps to `QaMetricsResponse`

Note: `QaMetricsResponse` still uses `MetricCard` wire type (from `SprintSummaryService.cs` at this point in the sequence); endpoint's `MapMetricCard(QaMetricCardData)` helper bridges the two.

Build: green.

---

### Step 4 — Create SprintSummaryCalculator + SprintSummaryResult.cs; update GetSprintSummaryEndpoint

**Files created (domain):**
- `src/Services/Fokus/Fokus.Domain/Analytics/SprintSummaryResult.cs` — `SprintInfoData`, `HealthScoreData`, `SprintSparklinePoint`, `SprintMetricCardData`, `SprintMetricsData`, `EpicProgressData`, `DeveloperSummaryData`, `ZombieTicketData`, `MidSprintDisruptionData`, `SprintFlagsData`, `SprintSummaryData`. `QaBreakdownData` is NOT redefined here (defined once in `QaMetricsResult.cs`); `TestingCrunchFlag` is NOT redefined here (from `TestTimelineResult.cs`).
- `src/Services/Fokus/Fokus.Domain/Analytics/SprintSummaryCalculator.cs` — `ComputeSummary(...)` returns `SprintSummaryData`; accepts `QaBreakdownData?` (domain type) and `TestingCrunchFlag?` (domain type)

**Files modified:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetSprintSummary/GetSprintSummaryQuery.cs` — added all wire types: `SprintInfo`, `HealthScoreResult`, `QualityBreakdownResult`, `SparklinePoint`, `MetricCard`, `MetricsResult`, `EpicProgress`, `DeveloperSummary`, `ZombieTicket`, `MidSprintDisruption`, `FlagsResult`, `TestingCrunchResult`, `SprintSummaryResponse`
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetSprintSummary/GetSprintSummaryEndpoint.cs` — injected `SprintSummaryCalculator`, `QaMetricsCalculator`, `TestTimelineCalculator`; full `MapResponse(SprintSummaryData)` with `MapFlags` and `MapMetricCard` helpers

Build: green.

---

### Step 5 — Adapt SprintAnalyticsGrpcService to use SprintSummaryCalculator + QaMetricsCalculator

**Files modified:**
- `src/Services/Fokus/Fokus.API/Grpc/SprintAnalyticsGrpcService.cs` — replaced `SprintSummaryService`/`QaMetricsService` with `SprintSummaryCalculator`/`QaMetricsCalculator`; updated `FokusSprintInfo` alias to `Fokus.API.Features.Analytics.GetSprintSummary.SprintInfo`; `LoadQaDataAsync` returns `(decimal?, QaBreakdownData?, bool)`; `MapToAnonymizedSummary` takes `SprintSummaryData`; all three `ExcludedDeveloperFilter` call sites fully qualified as `Fokus.Domain.Analytics.ExcludedDeveloperFilter` to resolve CS0104 ambiguity (old API-layer file still present when this step ran)

Mixed state: `ScopeChangeService` + `CarryOverService` remain in constructor — intentional until 3c-B.

Build: green.

---

### Step 6 — Update DI registration

**Files modified:**
- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` — added `using Fokus.Domain.Analytics;`; removed `AddScoped<SprintSummaryService>()`, `AddScoped<QaMetricsService>()`, `AddScoped<TestTimelineService>()`; added `AddScoped<SprintSummaryCalculator>()`, `AddScoped<QaMetricsCalculator>()`, `AddScoped<TestTimelineCalculator>()`

Build: green.

---

### Step 7 — Delete 7 old API-layer files

**Files deleted** (via `git rm`):
1. `src/Services/Fokus/Fokus.API/Features/Analytics/HealthScoreCalculator.cs`
2. `src/Services/Fokus/Fokus.API/Features/Analytics/TransitionAttributionChecker.cs`
3. `src/Services/Fokus/Fokus.API/Features/Analytics/CompletionChecker.cs`
4. `src/Services/Fokus/Fokus.API/Features/Analytics/ExcludedDeveloperFilter.cs`
5. `src/Services/Fokus/Fokus.API/Features/Analytics/TestTimelineService.cs`
6. `src/Services/Fokus/Fokus.API/Features/Analytics/QaMetricsService.cs`
7. `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs`

**Post-deletion fixes (not in plan, necessary for green build):**

The plan's pre-delete grep check was not performed — deletion exposed two categories of breakage in 3c-B/C scope files:

1. **`MetricCard` and `SparklinePoint`** — these wire types lived in `SprintSummaryService.cs` (namespace `Fokus.API.Features.Analytics`) and were shared across `GetQaMetricsQuery.cs`, `GetQaWorkloadEndpoint.cs`, `DeveloperQualityService.cs`, `QaWorkloadService.cs`. Fixed: added `using Fokus.API.Features.Analytics.GetSprintSummary;` to those 4 files (wire types now in `GetSprintSummaryQuery.cs` in that namespace).

2. **Static helpers** (`HealthScoreCalculator`, `TransitionAttributionChecker`, `CompletionChecker`, `ExcludedDeveloperFilter`) — 22 API-layer service and endpoint files in 3c-B/C scope still call these and had no `using Fokus.Domain.Analytics;`. Fixed: added `using Fokus.Domain.Analytics;` to all 22 files (20 via Node.js batch + 2 that already had `using Fokus.API.Features.Analytics.GetSprintSummary;` via separate Edit calls).

**Files receiving `using Fokus.Domain.Analytics;`** (22 total):
`BugRatioService.cs`, `CarryOverService.cs`, `CycleTimeService.cs`, `DeveloperDetailService.cs`, `DeveloperProgressService.cs`, `DeveloperThroughputService.cs`, `EpicProgressService.cs`, `LeaderboardService.cs`, `QaTrendsService.cs`, `ScopeChangeService.cs`, `DeveloperQualityService.cs`, `QaWorkloadService.cs`, `GetBugRatioEndpoint.cs`, `GetCarryOverEndpoint.cs`, `GetCycleTimeEndpoint.cs`, `GetDeveloperDetailEndpoint.cs`, `GetDeveloperProgressEndpoint.cs`, `GetDeveloperThroughputEndpoint.cs`, `GetFailingTicketsEndpoint.cs`, `GetLeaderboardEndpoint.cs`, `GetScopeChangeEndpoint.cs`, `GetUntestedTicketsEndpoint.cs`

Build: green.

---

### Step 8 — Update CLAUDE.md

**Files modified:**
- `src/Services/Fokus/CLAUDE.md` — updated "Computed metrics" bullet to describe ADR-005/010 pattern with domain calculators in `Fokus.Domain/Analytics/`

---

### Step 9 — Verification sweep

- `dotnet build SprintRituals.slnx` — 0 errors, 0 project warnings (only pre-existing NU1903 security advisory on `Blocks.Contracts`)
- Grep for `SprintSummaryService|QaMetricsService|TestTimelineService` — 1 match: historical comment in `HealthScoreCalculator.cs` ("Extracted from SprintSummaryService..."); no live code references
- Grep for static helpers in `Fokus.API` — present in 24 files, all as call sites (correct); no class definitions remain in API layer
- `TestingCrunchFlag` — found in 5 files: `TestTimelineResult.cs`, `TestTimelineCalculator.cs`, `SprintSummaryResult.cs`, `SprintSummaryCalculator.cs`, `GetSprintSummaryEndpoint.cs` (all expected)

---

## Deviations from Plan

| Deviation | Impact | Resolution |
|-----------|--------|------------|
| `Fokus.Domain/GlobalUsings.cs` missing `global using Fokus.Domain;` | Domain files couldn't resolve their own types | Added in Step 1 |
| Pre-delete grep in Step 7 not performed | `MetricCard`/`SparklinePoint` and static helper breakage in 22 3c-B/C scope files | Added missing `using` directives post-deletion |
| gRPC `ExcludedDeveloperFilter` CS0104 ambiguity | Build error after Step 5 | Fully qualified call sites with `Fokus.Domain.Analytics.ExcludedDeveloperFilter` |

---

## Key Decisions

- `QaBreakdownData` defined once in `QaMetricsResult.cs`, used by both `QaMetricsCalculator` and `SprintSummaryCalculator` — no redefinition in `SprintSummaryResult.cs`
- `TestingCrunchFlag` is the domain type (in `TestTimelineResult.cs`); `TestingCrunchResult` is the wire type (in `GetSprintSummaryQuery.cs`) — two parallel types, endpoint maps between them in `MapFlags`
- `MetricCard` and `SparklinePoint` wire types moved to `GetSprintSummaryQuery.cs` (namespace `Fokus.API.Features.Analytics.GetSprintSummary`); 3c-B/C scope files reference them via `using` directive until those services are migrated
- gRPC mixed state: `ScopeChangeService` + `CarryOverService` remain in `SprintAnalyticsGrpcService` until 3c-B — intentional
