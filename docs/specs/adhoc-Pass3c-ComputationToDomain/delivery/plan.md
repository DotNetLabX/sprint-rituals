# Implementation Plan — Pass 3c-A: ComputationToDomain (Sprint Core)

**Feature Spec:** None (adhoc refactoring pass — scope from `docs/backlog.md` row 3c + ADR-005/010)
**Slug:** adhoc-Pass3c-ComputationToDomain (sub-plan 3c-A)
**Plan:** docs/specs/adhoc-Pass3c-ComputationToDomain/delivery/plan.md
**Skill:** `analytics-computation-service` + `domain-service`
**Intent:** Refactoring (restructure; behavior must not change)

---

## Context

Pass 3c migrates all 15 analytics services from `Fokus.API/Features/Analytics/` to domain calculators in `Fokus.Domain/Analytics/` per ADR-005 (computation in Domain) and ADR-010 (endpoint-owns-response-DTO). The pass is split into three sequential pipeline runs:

- **3c-A (this plan):** Static helpers + `TestTimelineCalculator` + `QaMetricsCalculator` + `SprintSummaryCalculator` + gRPC adaptation for the 2 calculators consumed by gRPC
- **3c-B:** `CarryOverCalculator`, `ScopeChangeCalculator`, `CycleTimeCalculator`, `BugRatioCalculator`, `LeaderboardCalculator`
- **3c-C:** `DeveloperThroughputCalculator`, `DeveloperProgressCalculator`, `DeveloperDetailCalculator`, `DeveloperQualityCalculator`, `EpicProgressCalculator`, `QaWorkloadCalculator`, `QaTrendsCalculator`

3c-B and 3c-C depend on 3c-A completing first (gRPC mixed-state stabilised; `TestingCrunchFlag` domain type available).

**Locked decisions (from questions.md):**
- Q1: 3c-A/B/C as 3 separate pipeline runs — approved
- Q2: Clean break (A) — domain calculators return new domain result records; gRPC service maps from domain types; no wire-flavored names in the domain layer

**Wire stability invariant:** `SprintSummaryResponse`, `QaMetricsResponse`, `TestTimelineResponse` (wire types owned by endpoints/query files) must remain byte-for-byte identical. The gRPC contracts (`AnonymizedSprintSummary`, `DisruptionMetrics`, `CarryOverHistory`) must also remain unchanged.

**No migrations.** Pure code relocation — no EF schema changes, no DB changes.

---

## Ordering constraint

Step 2 (TestTimelineCalculator + `TestingCrunchFlag` domain type) **must land before** Step 4 (SprintSummaryCalculator), because `SprintSummaryCalculator` takes `TestingCrunchFlag?` as an input parameter and `GetSprintSummaryEndpoint` calls `TestTimelineCalculator.ComputeCrunchFlag(...)` to produce it.

Execution order: **1 → 2 → 3 → 4 → 5 → 6 → 7 → 8 → 9**

---

## Files to Create (domain layer)

| New file | Moved/rewritten from |
|----------|---------------------|
| `src/Services/Fokus/Fokus.Domain/Analytics/HealthScoreCalculator.cs` | `Fokus.API/Features/Analytics/HealthScoreCalculator.cs` |
| `src/Services/Fokus/Fokus.Domain/Analytics/TransitionAttributionChecker.cs` | `Fokus.API/Features/Analytics/TransitionAttributionChecker.cs` |
| `src/Services/Fokus/Fokus.Domain/Analytics/CompletionChecker.cs` | `Fokus.API/Features/Analytics/CompletionChecker.cs` |
| `src/Services/Fokus/Fokus.Domain/Analytics/ExcludedDeveloperFilter.cs` | `Fokus.API/Features/Analytics/ExcludedDeveloperFilter.cs` |
| `src/Services/Fokus/Fokus.Domain/Analytics/TestTimelineResult.cs` | New (domain result records from `TestTimelineService.cs` lines 1–92) |
| `src/Services/Fokus/Fokus.Domain/Analytics/TestTimelineCalculator.cs` | `Fokus.API/Features/Analytics/TestTimelineService.cs` (lines 96–688) |
| `src/Services/Fokus/Fokus.Domain/Analytics/QaMetricsResult.cs` | New (domain result records from `QaMetricsService.cs` lines 1–21) |
| `src/Services/Fokus/Fokus.Domain/Analytics/QaMetricsCalculator.cs` | `Fokus.API/Features/Analytics/QaMetricsService.cs` (lines 24–467) |
| `src/Services/Fokus/Fokus.Domain/Analytics/SprintSummaryResult.cs` | New (domain result records from `SprintSummaryService.cs` lines 1–93) |
| `src/Services/Fokus/Fokus.Domain/Analytics/SprintSummaryCalculator.cs` | `Fokus.API/Features/Analytics/SprintSummaryService.cs` (lines 97–886) |

## Files to Modify

| File | Change |
|------|--------|
| `src/Services/Fokus/Fokus.API/Features/Analytics/GetSprintSummary/GetSprintSummaryEndpoint.cs` | Replace `SprintSummaryService` + `QaMetricsService` injections with calculators; add `SprintSummaryResponse` wire-type definition; adapt result mapping |
| `src/Services/Fokus/Fokus.API/Features/Analytics/GetQaMetrics/GetQaMetricsEndpoint.cs` | Replace `QaMetricsService` with `QaMetricsCalculator`; adapt `MapResponse` to new domain result types |
| `src/Services/Fokus/Fokus.API/Features/Sprints/GetTestTimeline/GetTestTimelineEndpoint.cs` | Replace `TestTimelineService` with `TestTimelineCalculator`; add `TestTimelineResponse` + all timeline wire records to endpoint folder |
| `src/Services/Fokus/Fokus.API/Grpc/SprintAnalyticsGrpcService.cs` | Replace `SprintSummaryService` + `QaMetricsService` injections with calculators; update `LoadQaDataAsync` + `GetSprintSummaryAsync` to use new domain types; keep `ScopeChangeService` + `CarryOverService` (unchanged until 3c-B) |
| `src/Services/Fokus/Fokus.API/DependencyInjection.cs` | Remove `AddScoped<SprintSummaryService>`, `AddScoped<QaMetricsService>`, `AddScoped<TestTimelineService>`; add `AddScoped<SprintSummaryCalculator>`, `AddScoped<QaMetricsCalculator>`, `AddScoped<TestTimelineCalculator>` |
| `src/Services/Fokus/CLAUDE.md` | Update "Computed metrics" bullet to reference domain calculators per ADR-005 |

## Files to Delete

| File | Reason |
|------|--------|
| `src/Services/Fokus/Fokus.API/Features/Analytics/HealthScoreCalculator.cs` | Moved to domain |
| `src/Services/Fokus/Fokus.API/Features/Analytics/TransitionAttributionChecker.cs` | Moved to domain |
| `src/Services/Fokus/Fokus.API/Features/Analytics/CompletionChecker.cs` | Moved to domain |
| `src/Services/Fokus/Fokus.API/Features/Analytics/ExcludedDeveloperFilter.cs` | Moved to domain |
| `src/Services/Fokus/Fokus.API/Features/Analytics/TestTimelineService.cs` | Replaced by domain calculator |
| `src/Services/Fokus/Fokus.API/Features/Analytics/QaMetricsService.cs` | Replaced by domain calculator |
| `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs` | Replaced by domain calculator |

---

## Step 1 — Relocate 4 static helpers to Fokus.Domain/Analytics/

**Skill:** None (namespace change + `using` update)
**Files:** Create 4 domain files; delete 4 API-layer files (deferred to Step 7 after all callers updated)

Move the following static classes to `src/Services/Fokus/Fokus.Domain/Analytics/` with namespace changed from `Fokus.API.Features.Analytics` to `Fokus.Domain.Analytics`:

- `HealthScoreCalculator.cs` — lines 1–54 (full file; all method signatures unchanged)
- `TransitionAttributionChecker.cs` — full file; all method signatures unchanged
- `CompletionChecker.cs` — lines 1–61 (full file; all method signatures unchanged)
- `ExcludedDeveloperFilter.cs` — lines 1–51 (full file; all method signatures unchanged)

The 4 new domain files need no additional `using` directives beyond what the domain `GlobalUsings.cs` provides (`Fokus.Domain.*` types, `Blocks.Domain.*`). Verify: `HealthScoreCalculator` uses only `Math`/`decimal` (no domain types); `TransitionAttributionChecker` and `CompletionChecker` use `AppSettings` (in `Fokus.Domain.Settings`) and `StatusTransition` (in `Fokus.Domain.Ticket`) — both already in scope via global usings. `ExcludedDeveloperFilter` additionally uses `Sprint`, `Developer`, `DeveloperSprintCapacity` — also in scope.

**Do not yet add `using Fokus.Domain.Analytics;` to the API-layer callers** — that change comes as part of each service migration step (Steps 2, 3, 4). The old `Fokus.API.Features.Analytics` versions remain temporarily to keep the build green.

Accept: `dotnet build SprintRituals.slnx` green after this step (old API-layer static helpers still in place for current callers; domain versions are new additions).

**Confidence:** medium — `Fokus.Domain` global usings cover the domain types the helpers need; verify the `GlobalUsings.cs` covers `Fokus.Domain.Sprint`, `Fokus.Domain.Ticket`, `Fokus.Domain.Settings` before writing.

---

## Step 2 — Create TestTimelineCalculator + TestTimelineResult.cs; update GetTestTimelineEndpoint

**Skill:** `analytics-computation-service`
**Files:**
- Create `src/Services/Fokus/Fokus.Domain/Analytics/TestTimelineResult.cs`
- Create `src/Services/Fokus/Fokus.Domain/Analytics/TestTimelineCalculator.cs`
- Modify `src/Services/Fokus/Fokus.API/Features/Sprints/GetTestTimeline/GetTestTimelineEndpoint.cs`
- Add `src/Services/Fokus/Fokus.API/Features/Sprints/GetTestTimeline/GetTestTimelineResponse.cs` (new wire-type file)

### TestTimelineResult.cs (domain result records)

Source: `TestTimelineService.cs` lines 1–92 (all response records declared before `// --- Service ---`).

Define in namespace `Fokus.Domain.Analytics`. Rename records to domain result names (no `Response`/`Result` suffix ambiguity — use `Data` suffix where the old name was `*Result` to avoid collision with the domain file name itself, or simply retain descriptive names that do not carry wire-contract implications):

| Old name (API-layer) | New domain name |
|----------------------|-----------------|
| `TestTimelineResponse` | — (stays in API layer as wire type, see below) |
| `BurnupDayEntry` | `BurnupDayData` |
| `ScopeChangeDayEntry` | `ScopeChangeDayData` |
| `TestingCrunchResult` | `TestingCrunchData` |
| `CrunchTicketEntry` | `CrunchTicketData` |
| `PostSprintTestingResult` | `PostSprintTestingData` |
| `PostSprintTicketEntry` | `PostSprintTicketData` |
| `UntestedAtCloseResult` | `UntestedAtCloseData` |
| `UntestedTicketEntry` | `UntestedTicketData` |
| `DevToTestGapResult` | `DevToTestGapData` |
| `GapTicketEntry` | `GapTicketData` |
| `TestingCrunchFlag` | `TestingCrunchFlag` (cross-consumed — keep name; moves to domain) |

The domain calculator returns a new **`TestTimelineData`** record (parallel to `TestTimelineResponse`) holding all of the above types.

### TestTimelineCalculator.cs

Source: `TestTimelineService.cs` lines 96–688 (the `TestTimelineService` class body).

- Class name: `TestTimelineCalculator` (no `Service` suffix per ADR-005 naming)
- Namespace: `Fokus.Domain.Analytics`
- Instance method `ComputeTimeline(...)` → returns `TestTimelineData` (domain result)
- Static method `ComputeCrunchFlag(...)` → remains `static`; returns `TestingCrunchFlag?` (domain type, same logic)
- Replace all references to old API-layer record names with the new domain result names from the table above
- Replace `using Fokus.API.Features.Analytics;` (if any) with `using Fokus.Domain.Analytics;` (but since it's in the same namespace, no explicit using needed)
- `TransitionAttributionChecker` and `CompletionChecker` references resolve via `Fokus.Domain.Analytics` namespace (Step 1 moved them here)

### GetTestTimelineResponse.cs (new wire file)

Create `src/Services/Fokus/Fokus.API/Features/Sprints/GetTestTimeline/GetTestTimelineResponse.cs` in namespace `Fokus.API.Features.Sprints.GetTestTimeline`.

Move the wire types here (renaming to endpoint-owned shapes):
- `TestTimelineResponse` — identical shape to the old API-layer record (wire contract unchanged)
- `BurnupDayEntry`, `ScopeChangeDayEntry`, `TestingCrunchResult`, `CrunchTicketEntry`, `PostSprintTestingResult`, `PostSprintTicketEntry`, `UntestedAtCloseResult`, `UntestedTicketEntry`, `DevToTestGapResult`, `GapTicketEntry`

These keep the same property names/types as today (wire stability invariant).

Register a Mapster `IRegister` map in this file (or a sibling `GetTestTimelineMap.cs`) from `TestTimelineData → TestTimelineResponse`. Map all nested records. Pattern: see `GetQaMetricsEndpoint.cs` `MapResponse` private method for the manual-mapping approach; Mapster auto-config is preferred if the shapes align 1:1 after renaming.

### GetTestTimelineEndpoint.cs changes

- Replace constructor injection: `TestTimelineService testTimelineService` → `TestTimelineCalculator testTimelineCalculator`
- Replace `Endpoint<GetTestTimelineQuery, TestTimelineResponse>` — `TestTimelineResponse` is now in the endpoint namespace; no change to the type name from the outside
- In `HandleAsync`: replace `testTimelineService.ComputeTimeline(...)` → `testTimelineCalculator.ComputeTimeline(...)`; result is `TestTimelineData`; map to `TestTimelineResponse` via the Mapster map before `Send.OkAsync`
- `BuildEmptyResponse`: now constructs `TestTimelineResponse` using the wire types (unchanged shape)
- Remove the `using Fokus.API.Features.Analytics;` import (was needed for `TestTimelineService`); add `using Fokus.Domain.Analytics;` for `TestTimelineCalculator`

Accept: build green; `GET /api/sprints/{sprintId}/test-timeline` returns the same JSON shape as before (wire stability); `TestingCrunchFlag` is now a domain type in `Fokus.Domain.Analytics`.

**Confidence:** medium — the rename table is explicit; the main risk is the `BuildEmptyResponse` method which constructs nested records inline and must use the new wire type names.

---

## Step 3 — Create QaMetricsCalculator + QaMetricsResult.cs; update GetQaMetricsEndpoint

**Skill:** `analytics-computation-service`
**Files:**
- Create `src/Services/Fokus/Fokus.Domain/Analytics/QaMetricsResult.cs`
- Create `src/Services/Fokus/Fokus.Domain/Analytics/QaMetricsCalculator.cs`
- Modify `src/Services/Fokus/Fokus.API/Features/Analytics/GetQaMetrics/GetQaMetricsEndpoint.cs`

### QaMetricsResult.cs (domain result records)

Source: `QaMetricsService.cs` lines 1–21.

Namespace: `Fokus.Domain.Analytics`. Rename:

| Old name (API-layer) | New domain name |
|----------------------|-----------------|
| `QaMetricsResult` | `QaMetricsData` |
| `QualityBreakdownInfo` | `QaBreakdownData` |

`QaMetricsData` uses `MetricCard` — but the old `MetricCard` was in `SprintSummaryService.cs` (API layer). At this step, `SprintSummaryService.cs` still exists with the old `MetricCard`. The domain `QaMetricsData` cannot depend on an API-layer type.

**Resolution:** Define a domain-owned metric card type. Add to `QaMetricsResult.cs`:

```
public record QaMetricCardData(
    string Name, decimal Value, string DisplayValue,
    decimal? Delta, string? DeltaDirection, string? DeltaPolarity,
    IReadOnlyList<QaSparklinePoint> Sparkline)
{ public string? Rag { get; init; } }

public record QaSparklinePoint(string SprintName, decimal Value);
```

`QaMetricsData` fields `CoverageRate`, `ExecutionRate`, `PassRate` become `QaMetricCardData` (not `MetricCard`).

`QaBreakdownData` shape: `(decimal CoverageScore, int CoverageWeight, decimal PassRateScore, int PassRateWeight)` — same as `QualityBreakdownInfo` today.

### QaMetricsCalculator.cs

Source: `QaMetricsService.cs` lines 24–467.

- Class name: `QaMetricsCalculator`, namespace `Fokus.Domain.Analytics`
- `ComputeQaMetrics(...)` → returns `QaMetricsData`
- Replace `MetricCard` usages with `QaMetricCardData`; replace `SparklinePoint` usages with `QaSparklinePoint`
- Replace `QaMetricsResult` → `QaMetricsData`, `QualityBreakdownInfo` → `QaBreakdownData`
- `HealthScoreCalculator`, `TransitionAttributionChecker`, `CompletionChecker` references resolve via `Fokus.Domain.Analytics` (Step 1)

### GetQaMetricsEndpoint.cs changes

The existing `MapResponse(QaMetricsResult result)` private method already maps `QaMetricsResult → QaMetricsResponse`. After migration, this method maps `QaMetricsData → QaMetricsResponse`:

- `result.CoverageRate` is now `QaMetricCardData` — map to `MetricCard` wire type. Note: `QaMetricsResponse` currently uses `MetricCard?` (the old shared type). After 3c-A, `MetricCard` still exists in `SprintSummaryService.cs` (not yet deleted). The `GetQaMetricsQuery.cs` file defines `QaMetricsResponse` using `MetricCard` — this wire type must be preserved.

  **Decision:** Keep `QaMetricsResponse.CoverageRate` etc. typed as `MetricCard` (the wire type from `SprintSummaryService.cs`). The endpoint's `MapResponse` maps `QaMetricCardData → MetricCard` manually (same as the existing `QualityBreakdownInfo → QualityBreakdownResponse` mapping pattern already in this endpoint). Once `SprintSummaryService.cs` is deleted in Step 7, `MetricCard` moves to `GetSprintSummaryEndpoint`'s namespace — update the `using` in `GetQaMetricsQuery.cs` at that point.

  For 3c-A scope: `MapResponse` maps `QaMetricCardData → MetricCard` (the old wire type still in `SprintSummaryService.cs` namespace).

- Replace `QaMetricsService qaMetricsService` → `QaMetricsCalculator qaMetricsCalculator` in constructor
- Replace `qaMetricsService.ComputeQaMetrics(...)` → `qaMetricsCalculator.ComputeQaMetrics(...)` in handler; result is `QaMetricsData`

Accept: build green; `GET /api/sprints/{sprintId}/qa-metrics` returns the same JSON shape as before.

**Confidence:** medium — the `MetricCard` shared-type dependency requires manual mapping; the pattern is already present in the same endpoint.

---

## Step 4 — Create SprintSummaryCalculator + SprintSummaryResult.cs; update GetSprintSummaryEndpoint

**Skill:** `analytics-computation-service`
**Files:**
- Create `src/Services/Fokus/Fokus.Domain/Analytics/SprintSummaryResult.cs`
- Create `src/Services/Fokus/Fokus.Domain/Analytics/SprintSummaryCalculator.cs`
- Modify `src/Services/Fokus/Fokus.API/Features/Analytics/GetSprintSummary/GetSprintSummaryEndpoint.cs`
- Modify `src/Services/Fokus/Fokus.API/Features/Analytics/GetSprintSummary/GetSprintSummaryQuery.cs` (add `SprintSummaryResponse` wire record here)

**Prerequisite:** Step 2 completed (`TestingCrunchFlag` is now `Fokus.Domain.Analytics.TestingCrunchFlag`).

### SprintSummaryResult.cs (domain result records)

Source: `SprintSummaryService.cs` lines 1–93 (all records before `// --- Service ---`).

Namespace: `Fokus.Domain.Analytics`. Rename:

| Old name (API-layer) | New domain name |
|----------------------|-----------------|
| `SprintInfo` | `SprintInfoData` |
| `QualityBreakdownResult` | `QaBreakdownData` (same type defined in `QaMetricsResult.cs` — **do not redefine; both are in `Fokus.Domain.Analytics` namespace; one definition suffices**) |
| `HealthScoreResult` | `HealthScoreData` |
| `SparklinePoint` | `SprintSparklinePoint` |
| `MetricCard` | `SprintMetricCardData` |
| `MetricsResult` | `SprintMetricsData` |
| `EpicProgress` | `EpicProgressData` |
| `DeveloperSummary` | `DeveloperSummaryData` |
| `ZombieTicket` | `ZombieTicketData` |
| `MidSprintDisruption` | `MidSprintDisruptionData` |
| `FlagsResult` | `SprintFlagsData` |
| `SprintSummaryResponse` | — (stays in API layer as wire type, see below) |

`SprintMetricsData` uses `SprintMetricCardData` (not the old `MetricCard`). `HealthScoreData.QualityBreakdown` is typed as `QaBreakdownData?` (same domain type shared with `QaMetricsResult.cs`). `SprintFlagsData.TestingCrunch` is typed as `TestingCrunchFlag?` (domain type from Step 2).

The domain calculator returns a **`SprintSummaryData`** record (the domain result root). Fields parallel `SprintSummaryResponse`.

### SprintSummaryCalculator.cs

Source: `SprintSummaryService.cs` lines 97–886.

- Class name: `SprintSummaryCalculator`, namespace `Fokus.Domain.Analytics`
- `ComputeSummary(...)` → returns `SprintSummaryData`
- `TestingCrunchFlag?` parameter is now `Fokus.Domain.Analytics.TestingCrunchFlag?`
- Replace all old record name usages with domain result names from the table above
- `HealthScoreCalculator`, `TransitionAttributionChecker`, `CompletionChecker`, `ExcludedDeveloperFilter` resolve via `Fokus.Domain.Analytics` (Step 1)
- `QaBreakdownData` is defined once in `QaMetricsResult.cs` in the same namespace — no redefine needed

### GetSprintSummaryQuery.cs changes

Add `SprintSummaryResponse` wire record to `GetSprintSummaryQuery.cs` (namespace `Fokus.API.Features.Analytics.GetSprintSummary`):

```
public record SprintSummaryResponse(
    SprintInfo? Sprint,
    HealthScoreResult? HealthScore,
    MetricsResult? Metrics,
    List<EpicProgress> TopEpics,
    List<DeveloperSummary> Leaderboard,
    FlagsResult Flags);
```

Wire types `SprintInfo`, `HealthScoreResult`, `MetricCard`, `MetricsResult`, `SparklinePoint`, `EpicProgress`, `DeveloperSummary`, `ZombieTicket`, `MidSprintDisruption`, `FlagsResult` are also defined in this file (or a sibling `GetSprintSummaryResponse.cs`) — endpoint-owned, same property names/types as today.

### GetSprintSummaryEndpoint.cs changes

- Replace `SprintSummaryService sprintSummaryService` + `QaMetricsService qaMetricsService` constructor params → `SprintSummaryCalculator sprintSummaryCalculator` + `QaMetricsCalculator qaMetricsCalculator`
- `Endpoint<GetSprintSummaryQuery, SprintSummaryResponse>` — `SprintSummaryResponse` is now in the endpoint namespace
- Call `qaMetricsCalculator.ComputeQaMetrics(...)` → result is `QaMetricsData`; extract `qualitySubScore`, `qualityBreakdown` (as `QaBreakdownData`), call `testTimelineCalculator...` wait — endpoint still injects `TestTimelineCalculator` (from Step 2) for `ComputeCrunchFlag`. Add `TestTimelineCalculator testTimelineCalculator` to constructor injection here.
- `TestTimelineService.ComputeCrunchFlag(...)` call → `TestTimelineCalculator.ComputeCrunchFlag(...)` (static call syntax unchanged)
- `QualityBreakdownResult` parameter to `ComputeSummary` → `QaBreakdownData` (domain type); the mapping from `QaMetricsData.QualityBreakdown` is direct (same shape)
- `sprintSummaryService.ComputeSummary(...)` → `sprintSummaryCalculator.ComputeSummary(...)`; result is `SprintSummaryData`
- Map `SprintSummaryData → SprintSummaryResponse` before `Send.OkAsync`. Use Mapster `IRegister` map or inline mapping. The shapes are parallel so Mapster auto-config should work; register in a sibling `GetSprintSummaryMap.cs`.
- Empty response construction (line 23): now uses wire `FlagsResult([], null, [], false)` — same structure, now in endpoint namespace

Accept: build green; `GET /api/analytics/sprint-summary` returns the same JSON shape as before; `TestingCrunchFlag` flows as domain type from `TestTimelineCalculator` through the endpoint to `SprintSummaryCalculator`.

**Confidence:** medium — multiple type substitutions in one endpoint; the rename table is explicit; the `QaBreakdownData` shared-type (one definition in `SprintSummaryResult.cs`) needs verification that `QaMetricsResult.cs` doesn't redefine it.

---

## Step 5 — Adapt SprintAnalyticsGrpcService to use SprintSummaryCalculator + QaMetricsCalculator

**Skill:** None (constructor and call-site wiring)
**Files:** Modify `src/Services/Fokus/Fokus.API/Grpc/SprintAnalyticsGrpcService.cs`

The gRPC service currently injects `SprintSummaryService` and `QaMetricsService`. After Steps 3 and 4, these are deleted. Replace with the new domain calculators.

**Mixed-state note:** At end of 3c-A, the gRPC service still injects `ScopeChangeService` and `CarryOverService` (not migrated until 3c-B). This is intentional — the build must stay green with this mixed constructor.

### Constructor change

Remove: `SprintSummaryService sprintSummaryService`, `QaMetricsService qaMetricsService`
Add: `SprintSummaryCalculator sprintSummaryCalculator`, `QaMetricsCalculator qaMetricsCalculator`

### GetSprintSummaryAsync changes

- `sprintSummaryService.ComputeSummary(...)` → `sprintSummaryCalculator.ComputeSummary(...)` — returns `SprintSummaryData`
- `MapToAnonymizedSummary(result, ...)` — `result` is now `SprintSummaryData`; update the method signature and all field accesses (`result.Metrics`, `result.HealthScore`, `result.Sprint`, `result.Flags`) to use `SprintSummaryData` fields
- Wire output (`AnonymizedSprintSummary`) stays unchanged

### LoadQaDataAsync changes

- `qaMetricsService.ComputeQaMetrics(...)` → `qaMetricsCalculator.ComputeQaMetrics(...)` — returns `QaMetricsData`
- Return type tuple stays `(decimal? qualitySubScore, QualityBreakdownResult? qualityBreakdown, bool hasQaData)` — **wait**: after Step 4, `QualityBreakdownResult` is an API-layer wire type (in `GetSprintSummaryQuery.cs`). The `SprintSummaryCalculator.ComputeSummary(...)` takes `QaBreakdownData?` (domain type). Update `LoadQaDataAsync` return type to use `QaBreakdownData?` instead, and update all callers.

  The `FokusSprintInfo` alias (`using FokusSprintInfo = Fokus.API.Features.Analytics.SprintInfo;`) references the old `SprintInfo` which moves to `GetSprintSummaryQuery.cs` namespace. Update the alias target to `Fokus.API.Features.Analytics.GetSprintSummary.SprintInfo` (or remove it if the name collision is resolved by the namespace move).

- `MapToAnonymizedSummary` accesses `result.Metrics?.CompletionRate.Value` etc. — these fields are now on `SprintSummaryData` with types `SprintMetricsData`/`SprintMetricCardData`; field names are the same, update the type references in the mapping method.

Accept: build green; gRPC service compiles with `SprintSummaryCalculator` + `QaMetricsCalculator` + `ScopeChangeService` + `CarryOverService` (mixed state); all 3 gRPC methods return correct shapes.

**Confidence:** medium — the `FokusSprintInfo` alias and `QualityBreakdownResult`→`QaBreakdownData` substitution in the private helper require careful type tracking.

---

## Step 6 — Update DI registration

**Skill:** None
**File:** Modify `src/Services/Fokus/Fokus.API/DependencyInjection.cs`

Remove:
```csharp
services.AddScoped<SprintSummaryService>();
services.AddScoped<QaMetricsService>();
services.AddScoped<TestTimelineService>();
```

Add (in the same location, in the same registration block):
```csharp
services.AddScoped<SprintSummaryCalculator>();
services.AddScoped<QaMetricsCalculator>();
services.AddScoped<TestTimelineCalculator>();
```

Add `using Fokus.Domain.Analytics;` to the file's `using` directives.

Accept: DI registrations reflect the 3 new domain calculators; the 3 old service registrations are gone; build green.

---

## Step 7 — Delete 7 old API-layer files

**Skill:** None
**Files:** Delete the following (all callers updated in Steps 2–5):

1. `src/Services/Fokus/Fokus.API/Features/Analytics/HealthScoreCalculator.cs`
2. `src/Services/Fokus/Fokus.API/Features/Analytics/TransitionAttributionChecker.cs`
3. `src/Services/Fokus/Fokus.API/Features/Analytics/CompletionChecker.cs`
4. `src/Services/Fokus/Fokus.API/Features/Analytics/ExcludedDeveloperFilter.cs`
5. `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs`
6. `src/Services/Fokus/Fokus.API/Features/Analytics/QaMetricsService.cs`
7. `src/Services/Fokus/Fokus.API/Features/Analytics/TestTimelineService.cs`

**Before deleting**, grep for any remaining references to:
- `Fokus.API.Features.Analytics.SprintInfo` (may be referenced by namespace alias in gRPC service — verify updated in Step 5)
- `MetricCard` (verify that `GetQaMetricsQuery.cs` has been updated to map from `QaMetricCardData` and the `MetricCard` wire type is now in `GetSprintSummaryQuery.cs`)
- `QualityBreakdownResult` (old API-layer type — verify replaced by `QaBreakdownData` everywhere)
- `TestTimelineService`, `QaMetricsService`, `SprintSummaryService` (verify no remaining injections)

Accept: build green after all 7 files deleted; no compilation errors.

---

## Step 8 — Update CLAUDE.md

**Skill:** None
**File:** `src/Services/Fokus/CLAUDE.md`

Update the "Computed metrics" bullet under `## Key patterns`:

Before: `**Computed metrics:** Health scores are computed on-the-fly in endpoints — never stored in the DB.`

After: `**Computed metrics:** Analytics computation lives in domain calculators in `Fokus.Domain/Analytics/` (ADR-005). Calculators are pure domain logic — no `DbContext`, no HTTP dependencies. Endpoints load data, call calculators, and map domain result records to endpoint-owned response DTOs via Mapster (ADR-010). No computed metrics are stored in the DB.`

Accept: CLAUDE.md accurately describes the ADR-005/010 pattern.

---

## Step 9 — Verification sweep

**Skill:** None

1. `dotnet build SprintRituals.slnx` — must be clean (zero errors, zero warnings introduced by this pass)
2. Grep for `Fokus.API.Features.Analytics.SprintSummaryService`, `QaMetricsService`, `TestTimelineService` across the solution — zero hits expected
3. Grep for `HealthScoreCalculator`, `TransitionAttributionChecker`, `CompletionChecker`, `ExcludedDeveloperFilter` across the `Fokus.API` project — zero hits expected (all callers updated to domain namespace)
4. Grep for `TestingCrunchFlag` — should appear only in `Fokus.Domain/Analytics/TestTimelineResult.cs`, `TestTimelineCalculator.cs`, `SprintSummaryResult.cs`, `SprintSummaryCalculator.cs`, `GetSprintSummaryEndpoint.cs`, `GetTestTimelineEndpoint.cs`, `SprintAnalyticsGrpcService.cs`
5. Manual smoke test: `GET /api/analytics/sprint-summary`, `GET /api/sprints/{id}/qa-metrics`, `GET /api/sprints/{id}/test-timeline` — all return HTTP 200 with correct shapes

Accept: all verification checks pass; no behavioral change.

---

## Plan Review

Self-review completed against ADR-005/010 requirements and `analytics-computation-service` skill:

**Spec coverage:**
- ADR-005 (computation in Domain): Steps 1–4 move all 3c-A computation to `Fokus.Domain/Analytics/`. Static helpers (domain logic in API layer) covered in Step 1. 
- ADR-010 (endpoint-owns-DTO): Steps 2, 3, 4 establish endpoint-owned wire types (`TestTimelineResponse`, `QaMetricsResponse`, `SprintSummaryResponse`) separate from domain result records. Mapster maps required in Steps 2 and 4. Step 3 reuses existing manual mapping in `GetQaMetricsEndpoint`.
- ADR-006 (no DTO in domain): Domain result records (`TestTimelineData`, `QaMetricsData`, `SprintSummaryData`) use only primitive and domain types — no API-layer record types enter the domain.
- gRPC adaptation: Step 5 covers the 2 calculators consumed by `SprintAnalyticsGrpcService`; mixed state with `ScopeChangeService`/`CarryOverService` is explicitly allowed until 3c-B.
- DI: Step 6 covers registration swap.
- Cleanup: Step 7 covers deletion; Step 8 covers CLAUDE.md; Step 9 covers verification.

**Risks flagged:**
- `QaBreakdownData` shared across `SprintSummaryResult.cs` and `QaMetricsResult.cs` — plan explicitly states "define once, do not redefine."
- `MetricCard` wire type migration from `SprintSummaryService.cs` to `GetSprintSummaryQuery.cs` — Step 4 notes the dependency; Step 3 explicitly handles the transitional mapping.
- `FokusSprintInfo` alias in gRPC service — Step 5 explicitly flags and resolves.
- Step 2 must precede Step 4 (ordering constraint documented above).
