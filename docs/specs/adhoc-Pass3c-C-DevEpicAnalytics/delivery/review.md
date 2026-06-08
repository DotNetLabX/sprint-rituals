# Review — adhoc-Pass3c-C-DevEpicAnalytics

## Step 1 — Done-Check

Done-check performed by the architect against the source on disk (not a code review — that is Step 2). Independent full-`.slnx` build reported GREEN (0 errors, 4 pre-existing NU1903 warnings).

### Step dispositions

| Step | Disposition | Notes |
|------|-------------|-------|
| 1 — SharedAnalyticsWire.cs (Q1 → Option B) | Implemented | `SharedAnalyticsWire.cs` present; all 5 shared records (`SprintSummaryItem`, `DeveloperProgressSprintInfo`, `CompletedTicketEntry`, `DayBreakdownEntry`, `StalledTicketEntry`) defined **exactly once**, all in this file, byte-identical field shapes. Single-ownership verified (INV2). |
| 2 — DeveloperThroughputCalculator | Implemented | `DeveloperThroughputResult.cs` + `DeveloperThroughputCalculator.cs` + `GetDeveloperThroughputResponse.cs` (IRegister) present; endpoint injects calculator, `.Adapt<DeveloperThroughputResponse>()`. Carve-out `spAssignedDeltaPolarity = "neutral"` preserved as literal (INV4). |
| 3 — DeveloperProgressAnalyzer | Implemented | result + analyzer + response (IRegister) present; no MetricCardBuilder kernel (domain `Direction` enum kept verbatim); **two** early empty returns preserved. |
| 4 — DeveloperDetailCalculator | Implemented | result + calculator + response (IRegister) present; `CurrentSprintDetailData` references the Step-3 domain records (`DeveloperProgressSprintData`/`DayBreakdownData`/`CompletedTicketData`), same namespace; helpers moved verbatim. |
| 5 — DeveloperQualityScorer | Implemented | result + scorer present; **Q3 hand-mapper kept** — endpoint uses `MapResponse(DeveloperQualityResultData)`, no `.Adapt`, no IRegister (INV8). Local domain `DeveloperQualitySparklinePointData` declared, no cross-calculator reuse (INV5). Carve-out `bugsDeltaPol = "neutral"` literal preserved (INV4). Local `ResolveStartIndex`/`IsStartedInSprint`/`GetStageIndex` moved verbatim. |
| 6 — EpicProgressAnalyzer | Implemented | result + analyzer + response (IRegister) present; `EpicQaData` moved to domain as input record (keeps name); endpoint `.Adapt<EpicProgressResponse>()`. |
| 7 — QaWorkloadAnalyzer | Implemented (with benign domain-name deviation) | result + analyzer present; **Q3 hand-mappers kept** — `MapMultiResponse`/`MapSingleResponse`, no `.Adapt`, no IRegister (INV8). Local `QaWorkloadSparklinePointData` + `QaWorkloadMetricCardData` (sparkline+Rag) declared, no cross-calculator reuse (INV5). `NullableStringComparer` relocated to domain. 3 carve-out literals (`tesPol`/`runsPol`/`bugsPol = "neutral"`) preserved (INV4). Dead `QaWorkloadResponse` envelope deleted with the service file (per A3). |
| 8 — QaTrendsAnalyzer | Implemented | result + analyzer + response (IRegister) present; **`BugRatioCalculator` injection preserved** in `GetQaTrendsEndpoint` (line 14, used 98/113 — INV6); local `ResolveStartIndex`/`IsStartedInSprint`/`GetStageIndex` moved verbatim; endpoint `.Adapt<QaTrendsResponse>()`. |
| 9 — DI registration swap | Implemented | All 7 new calculators registered with the exact Q2 binding names; **zero** old `*Service` registrations remain (INV7). |
| 10 — Delete 7 old service files + grep | Implemented | All 7 `*Service.cs` deleted (none remain on disk); single-ownership grep (INV2) confirms relocated records have no stray declarations. |
| 11 — Verification sweep | Implemented | Independent full-`.slnx` build GREEN; all invariant greps below pass. |

### Critical-invariant verification (grep against source — not self-report)

| # | Invariant | Result |
|---|-----------|--------|
| INV1 | 7 routes byte-identical — wire type unchanged at `Endpoint<,>` generic param | **PASS** — all 7 endpoints declare their original wire response type (`DeveloperThroughputResponse`, `DeveloperProgressResponse`, `DeveloperDetailResponse`, `DeveloperQualityResponse`, `EpicProgressResponse`, `QaWorkloadResponseDto`, `QaTrendsResponse`) |
| INV2 | Q1 Option-B single ownership — shared records defined once, in `SharedAnalyticsWire.cs`, no per-endpoint redeclaration | **PASS** — 5 records, 1 definition each, all in `SharedAnalyticsWire.cs` |
| INV3 | Domain files reference no `Fokus.API` / wire `MetricCard` / wire `SparklinePoint` (ADR-006) | **PASS** — zero hits across `Fokus.Domain/Analytics/` |
| INV4 | 5 DO-NOT-TOUCH `"neutral"` carve-out literals preserved (not routed through `MetricCardBuilder.Polarity`) | **PASS** — Throughput:110, Quality:90, Workload:504/508/528 are literal `= "neutral"` |
| INV5 | Per-calculator local sparkline types; no cross-calculator reuse (Step 11 check 9 / N1) | **PASS** — `DeveloperQualitySparklinePointData` + `QaWorkloadSparklinePointData`/`QaWorkloadMetricCardData` local; zero refs to `SprintSparklinePoint`/`QaSparklinePoint`/`SprintMetricCardData`/`QaMetricCardData` |
| INV6 | `BugRatioCalculator` still injected in `GetQaTrendsEndpoint` | **PASS** — injected line 14, used 98/113 |
| INV7 | DI — 7 new calculators registered, 0 old services | **PASS** — 7 calculators with Q2 names; zero `*Service` registrations |
| INV8 | Q3 — hand-mappers kept for DeveloperQuality + QaWorkload (no `.Adapt`, no IRegister) | **PASS** — Quality `MapResponse`, Workload `MapMultiResponse`/`MapSingleResponse`; zero IRegister in both folders |
| INV9 | 7 calculator classes exist with the Q2 binding names | **PASS** |
| INV10 | 5 Mapster IRegister configs present (Throughput/Progress/Detail/Epic/QaTrends) | **PASS** |

### Notes / deviations (all benign, none Missing)

- **Domain-record name deviations (Deviated, valid reason).** The QaWorkload domain shapes were named `QaWorkloadMultiSprintResultData`/`QaWorkloadSingleSprintResultData` (plan table said `*Data`); Epic domain records used `EpicProgressSummaryMetricsData`/`EpicProgressUnlinkedWorkData` (plan said `EpicProgressSummaryData`/`EpicProgressUnlinkedData`). These are **internal domain-record names**, not public/wire surfaces — per the plan's own rule ("named identifiers are binding contracts for public surfaces only; internal decomposition is the developer's decision"). Wire stability (the binding contract) is intact (INV1). Not a conformance gap.
- These naming deltas are flagged for the **reviewer (Step 2)** only as a consistency observation — they do not affect the done-check verdict.

**Verdict: PASS**

---

## Step 2 — Code Review

## Reviewed By
Reviewer agent (nexus:reviewer), reading source files directly. Build evidence from implementation.md Step 11.

## Verdict: APPROVED

## Pre-commitment Predictions
- Expected: 5 Mapster IRegister configs with field-for-field coverage of all nested types. Found: All 5 present and complete.
- Expected: 2 hand-written mappers (DeveloperQuality, QaWorkload) re-pointed at domain types, wire DTOs untouched. Found: Confirmed.
- Expected: 5 DO-NOT-TOUCH literal "neutral" assignments preserved verbatim. Found: All 5 confirmed.
- Expected: Private BuildMetricCard in QaWorkload deleted, replaced by MetricCardBuilder.ComputeDelta calls. Found: Private BuildMetricCard retained with an inlined switch — behaviorally identical but a structural deviation.

## Findings

### [LOW] QaWorkloadAnalyzer retains private BuildMetricCard with inlined polarity switch instead of MetricCardBuilder.ComputeDelta
**File:** `src/Services/Fokus/Fokus.Domain/Analytics/QaWorkloadAnalyzer.cs:573-606`
**Issue:** Plan Step 7 states: "Delete the private `BuildMetricCard` (676-707) → build the domain sparkline card via `MetricCardBuilder.ComputeDelta(delta, polarity)`." The implementation retains the private helper with its own inlined switch expression (`polarity switch { "positive-up" => ..., _ => "neutral" }`). Semantically identical to `MetricCardBuilder.ComputeDelta` — wire output byte-identical. Code-structure deviation only; no behavioral impact.
**Fix:** Replace inlined switch with `var (direction, deltaPolarity) = MetricCardBuilder.ComputeDelta(delta, polarity);` to eliminate duplication. Can be a follow-up if desired.

## Positive Observations
- Wire-stability held across all 7 routes. Mapster IRegister configs for the 5 auto-mapped endpoints are field-for-field identical. The 2 hand-written mappers project every field with no omissions.
- All 5 DO-NOT-TOUCH carve-out literals preserved as string literals, not routed through MetricCardBuilder.Polarity.
- ADR-006 clean. No domain file under `Fokus.Domain/Analytics/` references `Fokus.API`, the wire MetricCard type, or the wire SparklinePoint type. Grep hits are comments and private method names only.
- Per-calculator local sparkline types correctly isolated. `DeveloperQualitySparklinePointData` in DeveloperQualityResult.cs; `QaWorkloadSparklinePointData`/`QaWorkloadMetricCardData` in QaWorkloadResult.cs only. Zero references to 3c-A types.
- Domain-record name deviations (architect-flagged) are internal only. Mapster config type-pairs and wire-layer names are correct; no wire surface affected.
- Local helpers moved verbatim. `ResolveStartIndex`/`IsStartedInSprint`/`GetStageIndex` are private copies in both DeveloperQualityScorer and QaTrendsAnalyzer, explicitly not merged with TransitionAttributionChecker.
- BugRatioCalculator injection preserved in GetQaTrendsEndpoint. Both calculators injected; bugRatioBySprintId wiring at lines 142-143 is intact.
- DI registration complete. All 7 calculators registered with Q2 binding names; zero old Service registrations remain.
- Option-B single-ownership enforced. 5 shared wire records declared exactly once in SharedAnalyticsWire.cs; per-endpoint files consume without re-declaring.
- QaWorkload BuildSingleTeamMetrics "neutral" polarity strings are legitimate string-polarity path inputs (arguments to BuildMetricCard's polarity param, not DO-NOT-TOUCH field assignments). The three carve-out literals (lines 504/508/528) are the separate per-developer delta field assignments — distinct from the card-builder calls.

## Gaps
- Smoke test (Step 11 check 10) not run by the reviewer. Live round-trip comparison was not performed. Structural analysis gives strong confidence but empirical byte-identical verification is not reviewer-sourced.

## Open Questions
- None.

## Evidence
| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Build | pass | `dotnet build SprintRituals.slnx` (implementation.md Step 11) | Build succeeded, 0 errors |
| ADR-006: no API types in domain | pass | grep `Fokus\.API\|MetricCard\b\|SparklinePoint\b` in `Fokus.Domain/Analytics/*.cs` | Hits are comments and private method names only; zero type references |
| Shared wire single-ownership | pass | grep `record SprintSummaryItem`, `record DeveloperProgressSprintInfo`, etc. | 5 records, 1 declaration each, all in SharedAnalyticsWire.cs |
| DO-NOT-TOUCH literals | pass | grep `"neutral"` in Throughput/Quality/Workload calculators | Lines 110/90/504/508/528 are literal `= "neutral"` assignments |
| DI: 7 calculators registered, 0 old services | pass | Read DependencyInjection.cs | 7 calculators with Q2 names; no old Service registrations |
| Cross-calculator sparkline reuse | pass | grep 3c-A type names in DeveloperQualityResult/QaWorkloadResult | zero hits |
| BugRatioCalculator in QaTrends endpoint | pass | Read GetQaTrendsEndpoint.cs | Injected line 14; used lines 137-143 |
| 5 Mapster IRegister configs present | pass | Read each Response.cs | DeveloperThroughputMappingConfig, DeveloperProgressMappingConfig, DeveloperDetailMappingConfig, EpicProgressMappingConfig, QaTrendsMappingConfig |
| Q3: no IRegister in Quality/Workload folders | pass | grep `IRegister` in GetDeveloperQuality/ and GetQaWorkload/ | zero hits |
