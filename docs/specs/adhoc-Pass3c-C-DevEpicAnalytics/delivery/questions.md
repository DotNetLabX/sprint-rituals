# Pass 3c-C: DevEpicAnalytics — Questions

## Q1: Cross-service shared wire types — domain placement vs. API isolation
**From:** architect
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis

**Context:** Two clusters of wire records are **shared across the 3c-C services** (defined in one service file, consumed by another), unlike 3c-B where every card type was per-feature:

1. `SprintSummaryItem` (defined in `DeveloperThroughputService.cs:6`) — consumed by **DeveloperThroughput**, **QaWorkload** (3 sites), and `GetQaWorkloadEndpoint.MapSprintInfo`.
2. `DeveloperProgressSprintInfo`, `DayBreakdownEntry`, `CompletedTicketEntry`, `StalledTicketEntry` (defined in `DeveloperProgressService.cs`) — `DeveloperProgressSprintInfo`, `DayBreakdownEntry`, `CompletedTicketEntry` are also consumed by **DeveloperDetail** (`CurrentSprintDetail`).

3c-B's `MetricCardData` precedent unified shared *card* shapes into a single Domain record while keeping wire card types per-endpoint. For these cross-service **structural** records (not metric cards), there are two clean options:

- **Option A (mirror 3c-B):** Domain calculators each return their own `*Data` records (e.g. `DeveloperThroughputResult` carries `SprintSummaryData`; `DeveloperProgressResult` carries `DeveloperProgressSprintData`/`DayBreakdownData`/etc.; `DeveloperDetailResult` reuses the **DeveloperProgress** domain records for its current-sprint sub-shape). Each endpoint owns its wire records; where two endpoints share a wire shape today, **each gets its own wire declaration in its own `*Response.cs`** (same as 3c-B's `ScopeMetricCard` living in two endpoint namespaces). Wire stays byte-identical.
- **Option B:** Promote `SprintSummaryItem` + the DeveloperProgress structural records to a single shared **wire** file under `Features/Analytics/` (a shared-types file), kept in the API layer, consumed by both endpoints — avoids duplicate wire declarations but introduces a cross-feature shared API file (mild vertical-slice tension).

**Question:** Use Option A (per-endpoint wire records, domain `*Data` per calculator, DeveloperDetail reuses DeveloperProgress domain records — fully consistent with 3c-B) or Option B (shared API-layer wire file)? I recommend **Option A** for consistency with the locked 3c-B clean-break pattern and the vertical-slice guardrail.

### Answer
**Option B (user decision — overrides architect's Option-A recommendation).** Promote the cross-service shared structural records — `SprintSummaryItem` + the DeveloperProgress structural records (`DeveloperProgressSprintInfo`, `DayBreakdownEntry`, `CompletedTicketEntry`, `StalledTicketEntry`) — into a **shared wire file under `Features/Analytics/`** in the API layer, consumed by both endpoints in each shared pair. Plan for the shared-file approach (one shared wire declaration, not duplicated per-endpoint).

---

## Q2: Calculator naming — `*Calculator` suffix vs. skill's no-`Service`-suffix rule
**From:** architect
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis

**Context:** The `analytics-computation-service` skill says domain calculators must be named *by what they compute* with **no `Service` / no generic suffix** (e.g. `WorkloadAnalyzer`, `ProgressAnalyzer`). But 3c-A and 3c-B both delivered the `*Calculator` suffix uniformly (`SprintSummaryCalculator`, `CarryOverCalculator`, `BugRatioCalculator`, …) and the existing `Fokus.Domain/Analytics/` folder is entirely `*Calculator`. To stay consistent with the established domain folder, 3c-C should name the 7 new domain classes `DeveloperThroughputCalculator`, `DeveloperProgressCalculator`, `DeveloperDetailCalculator`, `DeveloperQualityCalculator`, `EpicProgressCalculator`, `QaWorkloadCalculator`, `QaTrendsCalculator`.

**Question:** Confirm 3c-C keeps the `*Calculator` suffix (matching 3c-A/3c-B, NOT the skill's `*Analyzer`/`*Scorer` examples)? I recommend **yes** — uniformity with the existing domain folder trumps the skill's illustrative naming, and the skill gap should be logged as a lesson (skill examples diverge from the project's locked `*Calculator` convention).

### Answer
**"Keep the most meaningful suffix" (user decision — overrides architect's blanket-`*Calculator` recommendation).** Do NOT uniformly apply `*Calculator`. Name each of the 7 domain classes by the **most semantically meaningful suffix** for what it computes (`Calculator` / `Analyzer` / `Scorer` / `Projector` / etc., chosen per class). Architect assigns the per-class names in the plan as binding contracts. Log the skill-vs-project naming divergence as a lesson.

---

## Q3: Already-separated wire layers (DeveloperQuality, QaWorkload) — keep hand-written mappers or convert to Mapster?
**From:** architect
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis

**Context:** Five of the seven endpoints return the service's record directly (`Send.OkAsync(result)`), so they need NEW wire `*Response.cs` files + Mapster maps (the 3c-B pattern). But **two already have a separated wire layer**:
- **DeveloperQuality**: endpoint already maps `DeveloperQualityResult` (in the service file) → `DeveloperQualityResponse`/`*Response` types (in `GetDeveloperQualityQuery.cs`) via a hand-written `MapResponse`.
- **QaWorkload**: endpoint already maps `QaWorkload*Response`/`MetricCard` (in the service file) → `QaWorkload*Dto`/`QaWorkloadMetricCardDto` (in `GetQaWorkloadQuery.cs`) via hand-written `MapMultiResponse`/`MapSingleResponse`.

For these two, the migration only needs to **move the result records to the Domain layer** (rename to `*Data`/`*Result`) and re-point the existing mapper at the moved domain types. The existing wire DTOs and hand-written mappers already satisfy ADR-010.

**Question:** For DeveloperQuality + QaWorkload, **keep the existing hand-written mappers** (minimal change — just move result records to Domain and re-point), or **convert them to Mapster `IRegister`** for consistency with the other five? I recommend **keep the hand-written mappers** — converting working byte-stable mappers adds wire-regression risk for zero ADR benefit (ADR-010 says "map at the boundary", not "must be Mapster"; the hand mappers already are at the boundary). Flag as a LOW follow-up if uniformity is later wanted.

### Answer
**Keep the existing hand-written mappers (user decision — matches architect recommendation).** For DeveloperQuality + QaWorkload, move the result records to the Domain layer (rename to `*Data`/`*Result`) and re-point the existing hand-written `MapResponse`/`MapMultiResponse`/`MapSingleResponse` at the moved domain types. Do NOT convert to Mapster. Flag the uniformity-to-Mapster conversion as a **LOW** follow-up item.
