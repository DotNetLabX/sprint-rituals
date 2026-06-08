# Implementation — adhoc-Pass3c-C-DevEpicAnalytics

## What Was Built

Migrated 7 analytics services from the API layer (`Fokus.API/Features/Analytics/`) to domain calculators in `Fokus.Domain/Analytics/`, per ADR-005, ADR-006, ADR-010. All 7 HTTP routes remain byte-for-byte identical post-migration.

---

## Steps Completed

### Step 1 — SharedAnalyticsWire.cs
**Created** `src/Services/Fokus/Fokus.API/Features/Analytics/SharedAnalyticsWire.cs`

5 cross-endpoint wire records promoted to single ownership (Q1 → Option B):
- `SprintSummaryItem`, `DeveloperProgressSprintInfo`, `CompletedTicketEntry`, `DayBreakdownEntry`, `StalledTicketEntry`

---

### Step 2 — DeveloperThroughput
**Created:**
- `Fokus.Domain/Analytics/DeveloperThroughputResult.cs` — `SprintSummaryItemData`, `SprintBreakdownData` (24-field flat delta bundle), `DeveloperThroughputEntryData`, `DeveloperThroughputResultData`
- `Fokus.Domain/Analytics/DeveloperThroughputCalculator.cs` — replaced private `DeltaDirection`/`DeltaPolarity` with `MetricCardBuilder.Direction`/`Polarity`; DO-NOT-TOUCH: `spAssignedDeltaPolarity = "neutral"` kept as literal
- `Fokus.API/Features/Analytics/GetDeveloperThroughput/GetDeveloperThroughputResponse.cs` — wire records + `DeveloperThroughputMappingConfig : IRegister`

**Modified:** `GetDeveloperThroughputEndpoint.cs` — inject `DeveloperThroughputCalculator`, `using Mapster;`, `.Adapt<DeveloperThroughputResponse>()`

---

### Step 3 — DeveloperProgress
**Created:**
- `Fokus.Domain/Analytics/DeveloperProgressResult.cs` — `DeveloperProgressSprintData`, `DeveloperProgressAlertData`, `CompletedTicketData`, `DayBreakdownData`, `StalledTicketData`, `DeveloperProgressEntryData`, `DeveloperProgressResultData`
- `Fokus.Domain/Analytics/DeveloperProgressAnalyzer.cs` — no MetricCardBuilder kernel; alert sort comparators and two early empty returns preserved exactly
- `Fokus.API/Features/Analytics/GetDeveloperProgress/GetDeveloperProgressResponse.cs` — wire records + `DeveloperProgressMappingConfig : IRegister`

**Modified:** `GetDeveloperProgressEndpoint.cs` — inject `DeveloperProgressAnalyzer`, `.Adapt<DeveloperProgressResponse>()`

---

### Step 4 — DeveloperDetail
**Created:**
- `Fokus.Domain/Analytics/DeveloperDetailResult.cs` — `DeveloperDetailInfoData`, `SprintTrendData`, `WorkAllocationData`, `CurrentSprintDetailData` (references `DeveloperProgressSprintData`, `DayBreakdownData`, `CompletedTicketData` from Step 3), `TicketDetailData`, `DeveloperDetailResultData`
- `Fokus.Domain/Analytics/DeveloperDetailCalculator.cs` — all private helpers (`CountBusinessDays`, `BuildTicketDetails`, `ComputeRollingAverage`, `GetTransitionCompletedMemberships`) moved verbatim
- `Fokus.API/Features/Analytics/GetDeveloperDetail/GetDeveloperDetailResponse.cs` — wire records + `DeveloperDetailMappingConfig : IRegister`

**Modified:** `GetDeveloperDetailEndpoint.cs` — inject `DeveloperDetailCalculator`, `.Adapt<DeveloperDetailResponse>()`

---

### Step 5 — DeveloperQuality
**Created:**
- `Fokus.Domain/Analytics/DeveloperQualityResult.cs` — `DeveloperQualitySprintData`, `DeveloperQualitySparklinePointData(string SprintName, decimal Value)` (local domain sparkline — NOT reused), `DeveloperQualitySprintBreakdownData`, `DeveloperQualityEntryData`, `DeveloperQualityResultData`
- `Fokus.Domain/Analytics/DeveloperQualityScorer.cs` — replaced `DeltaDirection`/`DeltaPolarity` with `MetricCardBuilder.Direction`/`Polarity`; DO-NOT-TOUCH: `bugsDeltaPol = "neutral"` kept as literal; local `ResolveStartIndex`, `IsStartedInSprint`, `GetStageIndex` helpers moved verbatim (NOT merged with `TransitionAttributionChecker`); `BuildDevSparkline` returns `List<DeveloperQualitySparklinePointData>`

**Modified:** `GetDeveloperQualityEndpoint.cs` — inject `DeveloperQualityScorer`; `MapResponse(DeveloperQualityResultData result)` re-pointed at domain types; sparkline projections `p.SprintName`/`p.Value` unchanged (type changed to `DeveloperQualitySparklinePointData`)

---

### Step 6 — EpicProgress
**Created:**
- `Fokus.Domain/Analytics/EpicProgressResult.cs` — `EpicQaData` (domain input type, not wire), `EpicProgressSummaryMetricsData`, `TestRunSummaryData`, `EpicProgressTicketEntryData`, `EpicProgressEntryData`, `EpicProgressUnlinkedWorkData`, `EpicProgressResultData`
- `Fokus.Domain/Analytics/EpicProgressAnalyzer.cs` — all logic moved verbatim, returns domain result types
- `Fokus.API/Features/Analytics/GetEpicProgress/GetEpicProgressResponse.cs` — wire records (same shapes as domain) + `EpicProgressMappingConfig : IRegister`

**Modified:** `GetEpicProgressEndpoint.cs` — inject `EpicProgressAnalyzer`, `using Fokus.Domain.Analytics;`, `using Mapster;`, `.Adapt<EpicProgressResponse>()`; `EpicQaData` now sourced from domain namespace

---

### Step 7 — QaWorkload
**Created:**
- `Fokus.Domain/Analytics/QaWorkloadResult.cs` — `QaWorkloadSparklinePointData` (local domain sparkline), `QaWorkloadMetricCardData` (carries Sparkline + Rag, named distinct from shared `MetricCardData`), multi- and single-sprint result records, `WorkloadAlertData`
- `Fokus.Domain/Analytics/QaWorkloadAnalyzer.cs` — `NullableStringComparer` moved here; `BuildTeamSparkline` returns `List<QaWorkloadSparklinePointData>`; `BuildMetricCard` returns `QaWorkloadMetricCardData`; `DeltaDirection`/`DeltaPolarity` replaced with `MetricCardBuilder.Direction`/`Polarity`; 3 DO-NOT-TOUCH carve-outs kept as `"neutral"` literals (TesOwned, RunsCompleted, BugsFound)

**Modified:** `GetQaWorkloadEndpoint.cs` — inject `QaWorkloadAnalyzer`; hand-written mapper method signatures re-pointed at domain result types (`QaWorkloadMultiSprintResultData`, `QaWorkloadSingleSprintResultData`, `SprintSummaryItemData`, `WorkloadAlertData`, `QaWorkloadEntryData`, `QaWorkloadSingleEntryData`, `QaWorkloadMetricCardData`); no Mapster IRegister (Q3: keep hand-written mappers)

---

### Step 8 — QaTrends
**Created:**
- `Fokus.Domain/Analytics/QaTrendsResult.cs` — `QaTrendsSprintInfoData`, `QualityTrendEntryData`, `TestingVolumeEntryData`, `DefectCorrelationDataPointData`, `DefectCorrelationResultData`, `QaTrendsResultData`
- `Fokus.Domain/Analytics/QaTrendsAnalyzer.cs` — local `ResolveStartIndex`, `IsStartedInSprint`, `GetStageIndex` helpers moved verbatim (NOT merged with `TransitionAttributionChecker`)
- `Fokus.API/Features/Analytics/GetQaTrends/GetQaTrendsResponse.cs` — wire records + `QaTrendsMappingConfig : IRegister`

**Modified:** `GetQaTrendsEndpoint.cs` — inject `QaTrendsAnalyzer`; `BugRatioCalculator` injection kept; `using Mapster;` added; `.Adapt<QaTrendsResponse>()`; early-return wire constructions unchanged (wire types at endpoint boundary)

---

### Step 9 — DependencyInjection.cs
**Modified:** `src/Services/Fokus/Fokus.API/DependencyInjection.cs`

7 service → calculator swaps:
- `DeveloperThroughputService` → `DeveloperThroughputCalculator`
- `DeveloperProgressService` → `DeveloperProgressAnalyzer`
- `DeveloperDetailService` → `DeveloperDetailCalculator`
- `DeveloperQualityService` → `DeveloperQualityScorer`
- `EpicProgressService` → `EpicProgressAnalyzer`
- `QaWorkloadService` → `QaWorkloadAnalyzer`
- `QaTrendsService` → `QaTrendsAnalyzer`

---

### Step 10 — Delete Old Services
**Deleted** 7 files from `Fokus.API/Features/Analytics/`:
- `DeveloperThroughputService.cs`
- `DeveloperProgressService.cs`
- `DeveloperDetailService.cs`
- `DeveloperQualityService.cs`
- `EpicProgressService.cs`
- `QaWorkloadService.cs`
- `QaTrendsService.cs`

---

### Step 11 — Build Verification
- `dotnet build Fokus.API.csproj` → **Build succeeded, 0 errors**
- Fix applied: `MetricCardData` duplicate removed from `QaWorkloadResult.cs`; renamed to `QaWorkloadMetricCardData` (carries Sparkline+Rag, consistent with pattern of `SprintMetricCardData`/`QaMetricCardData`)
- Domain files: no `using Fokus.API`, no bare `MetricCard`/`SparklinePoint` API types
- 5 DO-NOT-TOUCH carve-out `"neutral"` literals confirmed present (Throughput line 110, Quality line 90, Workload lines 504/508/528 + `BuildMetricCard` `"neutral"` switch fallthrough)
- `BugRatioCalculator` injection confirmed in `GetQaTrendsEndpoint`
- Shared wire records: single ownership in `SharedAnalyticsWire.cs`
- Cross-calculator sparkline reuse: none (each calculator uses its own `*SparklinePointData` type)

---

## Invariants Maintained

| Invariant | Status |
|-----------|--------|
| Wire-stability (7 routes byte-identical) | Held — no field renames, no struct changes |
| ADR-005 (computation in domain) | Held — all 7 calculators in `Fokus.Domain/Analytics/` |
| ADR-006 (no API types in domain inputs) | Held — `EpicQaData` moved to domain; all inputs are domain/primitive types |
| ADR-010 (endpoint owns wire contract) | Held — Mapster IRegister for 5 endpoints; hand-written mappers for QaWorkload (Q3) |
| DO-NOT-TOUCH polarity literals | Held — 5 literal `"neutral"` assignments preserved |
| Local helpers not merged with TransitionAttributionChecker | Held — DeveloperQuality + QaTrends keep own `ResolveStartIndex`/`IsStartedInSprint`/`GetStageIndex` |
| No cross-calculator sparkline reuse | Held — each of the 2 sparkline calculators (DeveloperQuality, QaWorkload) owns its own `*SparklinePointData` type |
