# Implementation — adhoc-Pass3c-B-DeliveryMetrics

## Summary

All 11 plan steps implemented. Build: 0 errors, 6 pre-existing warnings.

5 delivery-metric analytics computations relocated from API-layer services to domain calculators in `Fokus.Domain/Analytics/`, following the ADR-005/010 pattern established in Pass 3c-A. Wire contracts are byte-for-byte identical.

---

## Steps

### Step 1 — MetricCardData shared kernel (done prior to context compaction)

**Created:** `Fokus.Domain/Analytics/MetricCardData.cs`
- 6-field sealed record: `Name, Value, DisplayValue, Delta, DeltaDirection, DeltaPolarity`

### Step 2 — MetricCardBuilder shared kernel (done prior to context compaction)

**Created:** `Fokus.Domain/Analytics/MetricCardBuilder.cs`
- Two polarity paths: string-polarity (with "neutral" short-circuit) and bool-polarity (`positiveUp` flag)
- Methods: `ComputeDelta`, `Build`, `Direction(decimal)`, `Direction(int)`, `Polarity(decimal, bool)`, `Polarity(int, bool)`

**Modified:** `SprintSummaryCalculator.cs`, `QaMetricsCalculator.cs`
- Replaced inline direction/deltaPolarity blocks with `MetricCardBuilder.ComputeDelta(...)` calls

### Step 3 — CarryOverCalculator + GetCarryOverResponse + endpoint (done prior to context compaction)

**Created:** `Fokus.Domain/Analytics/CarryOverResult.cs`, `Fokus.Domain/Analytics/CarryOverCalculator.cs`
**Created:** `Fokus.API/Features/Analytics/GetCarryOver/GetCarryOverResponse.cs`
- Wire records with original names; `ScopeMetricCard` declared here
- `CarryOverMappingConfig : IRegister` with full Mapster config
- Finding 9 preserved: domain `CarryOverPerSprintMetricsData` → wire `CarryOverPerSprintData`

**Modified:** `Fokus.API/Features/Analytics/GetCarryOver/GetCarryOverEndpoint.cs`
- Swapped `CarryOverService` → `CarryOverCalculator`; added `using Mapster;`; `.Adapt<>()` for both modes

### Step 4 — CycleTimeCalculator + GetCycleTimeResponse + endpoint (done prior to context compaction)

**Created:** `Fokus.Domain/Analytics/CycleTimeResult.cs`, `Fokus.Domain/Analytics/CycleTimeCalculator.cs`
**Created:** `Fokus.API/Features/Analytics/GetCycleTime/GetCycleTimeResponse.cs`
- `CycleTimeMappingConfig : IRegister` with `MetricCardData → CycleTimeMetricCard` map

**Modified:** `Fokus.API/Features/Analytics/GetCycleTime/GetCycleTimeEndpoint.cs`
- Swapped `CycleTimeService` → `CycleTimeCalculator`; `.Adapt<>()` for both modes

### Step 5 — BugRatioCalculator + GetBugRatioResponse + endpoint (done prior to context compaction)

**Created:** `Fokus.Domain/Analytics/BugRatioResult.cs`, `Fokus.Domain/Analytics/BugRatioCalculator.cs`
**Created:** `Fokus.API/Features/Analytics/GetBugRatio/GetBugRatioResponse.cs`
- `BugRatioMappingConfig : IRegister`; `BugRatioDeveloperDelta` flat delta bundle

**Modified:** `Fokus.API/Features/Analytics/GetBugRatio/GetBugRatioEndpoint.cs`
- Swapped `BugRatioService` → `BugRatioCalculator`; `.Adapt<>()` for both modes

### Step 6 — LeaderboardCalculator + GetLeaderboardResponse + endpoint

**Created:** `Fokus.Domain/Analytics/LeaderboardResult.cs` (done prior to compaction)
**Created:** `Fokus.Domain/Analytics/LeaderboardCalculator.cs`
- Full computation migrated from `LeaderboardService`
- CRITICAL Finding 1 preserved: `TotalSpDeltaPolarity = "neutral"` (literal, not `MetricCardBuilder.Polarity(...)`) on line with comment

**Created:** `Fokus.API/Features/Analytics/GetLeaderboard/GetLeaderboardResponse.cs`
- Wire records with original names; `LeaderboardMappingConfig : IRegister`
- Flat delta bundle: `LeaderboardDeveloperDeltaData → LeaderboardDeveloperDelta`

**Modified:** `Fokus.API/Features/Analytics/GetLeaderboard/GetLeaderboardEndpoint.cs`
- Swapped `LeaderboardService` → `LeaderboardCalculator`; added `using Mapster;`; `.Adapt<>()` for both modes

### Step 7 — ScopeChangeCalculator + GetScopeChangeResponse + endpoint

**Created:** `Fokus.Domain/Analytics/ScopeChangeResult.cs`
- Domain records with `*Data` suffix; `ScopeChangeSingleSprintMetricsData` uses `MetricCardData` fields

**Created:** `Fokus.Domain/Analytics/ScopeChangeCalculator.cs`
- Full computation migrated from `ScopeChangeService`
- `MetricCardBuilder.Build(...)` replaces private `BuildMetricCard`
- Internal `ScopeSprintMetrics` record stays private within calculator

**Created:** `Fokus.API/Features/Analytics/GetScopeChange/GetScopeChangeResponse.cs`
- Wire records with original names; `ScopeMetricCard` declared here (separate from `GetCarryOver` namespace)
- `ScopeChangeMappingConfig : IRegister`

**Modified:** `Fokus.API/Features/Analytics/GetScopeChange/GetScopeChangeEndpoint.cs`
- Swapped `ScopeChangeService` → `ScopeChangeCalculator`; added `using Mapster;`; `.Adapt<>()` for both modes

### Step 8 — SprintAnalyticsGrpcService

**Modified:** `Fokus.API/Grpc/SprintAnalyticsGrpcService.cs`
- Removed stale usings: `Fokus.API.Features.Analytics`, `Fokus.API.Features.Analytics.GetCarryOver`
- Swapped constructor params: `ScopeChangeService` → `ScopeChangeCalculator`, `CarryOverService` → `CarryOverCalculator`
- `MapToDisruptionMetrics` signature changed: `ScopeChangeSingleSprintResponse` → `ScopeChangeSingleSprintData`
  - `metrics.DisruptionRate.Value` and `metrics.AddedSp.Value` access `MetricCardData.Value` — wire-identical
  - `Action == "Added"` filter preserved verbatim (latent case bug — domain stores lowercase `"added"`)
- `MapToCarryOverHistory` signature changed: `CarryOverMultiSprintResponse` → `CarryOverMultiSprintData`
  - `result.PerSprintData` is `List<CarryOverPerSprintMetricsData>`; `data.CarryOverRate` preserved
  - `result.Sprints` is `List<CarryOverSprintData>`; `info.Name` preserved
  - `result.ZombieTickets.Count` preserved

### Step 9 — GetQaTrendsEndpoint secondary fix

`GetQaTrendsEndpoint` injected `BugRatioService` (not in the main 5-endpoint list but would break on service deletion).

**Modified:** `Fokus.API/Features/Analytics/GetQaTrends/GetQaTrendsEndpoint.cs`
- Added `using Fokus.Domain.Analytics;`
- Swapped `BugRatioService` → `BugRatioCalculator`; updated comments

### Step 10 — Delete service files

Deleted (zero references remaining at time of deletion):
- `Fokus.API/Features/Analytics/ScopeChangeService.cs`
- `Fokus.API/Features/Analytics/CarryOverService.cs`
- `Fokus.API/Features/Analytics/BugRatioService.cs`
- `Fokus.API/Features/Analytics/CycleTimeService.cs`
- `Fokus.API/Features/Analytics/LeaderboardService.cs`

### Step 11 — DependencyInjection.cs

**Modified:** `Fokus.API/DependencyInjection.cs`
- Replaced 5 `AddScoped<*Service>()` with 5 `AddScoped<*Calculator>()`

---

## Build Verification

```
dotnet build SprintRituals.slnx --no-incremental
Build succeeded.
0 Error(s)
6 Warning(s) — all pre-existing (NuGet vuln, nullable, constructor capture)
```

---

## Verification Sweep

- Service names (`ScopeChangeService`, `CarryOverService`, `BugRatioService`, `CycleTimeService`, `LeaderboardService`): only found in doc comments in `MetricCardBuilder.cs` and `LeaderboardResult.cs` — zero live code references
- Calculator names: 5 class declarations + 5 DI registrations + 6 injection sites (5 endpoints + 1 gRPC)
- `MetricCardData` / `MetricCardBuilder`: present across 17 files as expected
- `BuildMetricCard` / `BuildCard`: only in `SprintSummaryCalculator.cs` (pre-existing, out of scope) and `QaWorkloadService.cs` (out of scope); zero hits in migrated calculators
- `ScopeMetricCard`: exactly 2 declarations — `GetCarryOver` and `GetScopeChange` namespaces

---

## Finding Handling

| Finding | Disposition |
|---------|-------------|
| F1 (HIGH) — `TotalSpDeltaPolarity = "neutral"` literal in LeaderboardCalculator | Preserved verbatim with comment |
| F9 — `CarryOverPerSprintMetricsData` (domain) → `CarryOverPerSprintData` (wire) name collision | Mapster map explicit, comment on wire type |
| F10 — `ScopeMetricCard` in two namespaces | Both declared separately in `GetCarryOver` and `GetScopeChange` |
| gRPC `Action == "Added"` case bug | Preserved verbatim with comment (latent bug, do not fix) |
| `GetQaTrendsEndpoint` BugRatioService injection | Found during deletion sweep; updated to BugRatioCalculator |
