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
