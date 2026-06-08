# adhoc-Pass3c-B-DeliveryMetrics — Questions

## Q1: gRPC mixed-state retirement strategy
**From:** architect
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis (pre-plan)
**File:** `src/Services/Fokus/Fokus.API/Grpc/SprintAnalyticsGrpcService.cs`

**Context:** The gRPC service currently holds a mixed-state constructor: `SprintSummaryCalculator` + `QaMetricsCalculator` (migrated in 3c-A) alongside `ScopeChangeService` + `CarryOverService` (still API-layer services, to be migrated here in 3c-B). The gRPC mapping methods `MapToDisruptionMetrics` and `MapToCarryOverHistory` both map *from the current API-layer response types* of those services — specifically `ScopeChangeSingleSprintResponse` (with `ScopeMetricCard`, `ScopeChangeEvent`, etc.) and `CarryOverMultiSprintResponse` (with `CarryOverPerSprintData`, `CarryOverZombieSummary`, etc.) — because neither the services nor the response records have moved to the domain yet.

After 3c-B, `ScopeChangeService` → `ScopeChangeCalculator` returning `ScopeChangeData`, and `CarryOverService` → `CarryOverCalculator` returning `CarryOverMultiSprintData` + `CarryOverSingleSprintData`. The gRPC mapping methods will need to map from the new domain result types instead.

The 3c-A pattern was: gRPC service adapts its call sites to use the new calculators AND the mapping methods are updated to take the domain result types. The gRPC *wire contracts* (`DisruptionMetrics`, `CarryOverHistory`) remain unchanged.

**Question:** Can I confirm the 3c-A pattern applies here exactly — update gRPC to inject `ScopeChangeCalculator` + `CarryOverCalculator`, and rework `MapToDisruptionMetrics` / `MapToCarryOverHistory` to map from the new domain result types — with the wire contracts (`DisruptionMetrics`, `CarryOverHistory`) unchanged? Or is there anything about the gRPC surface that needs a different approach for 3c-B?

### Answer
Yes, confirmed — same 3c-A gRPC pattern. Wire contracts unchanged.

---

## Q2: ScopeMetricCard / metric card type consolidation strategy
**From:** architect
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis (pre-plan)
**File:** `src/Services/Fokus/Fokus.API/Features/Analytics/ScopeChangeService.cs`, `CarryOverService.cs`, `BugRatioService.cs`, `CycleTimeService.cs`

**Context:** All 5 services define their own metric card type:
- `ScopeChangeService.cs`: `ScopeMetricCard` (Name, Value, DisplayValue, Delta, DeltaDirection, DeltaPolarity) — **shared** by `CarryOverService.cs` (the carry-over service imports `ScopeMetricCard` directly).
- `BugRatioService.cs`: `BugRatioMetricCard` (identical shape to `ScopeMetricCard`)
- `CycleTimeService.cs`: `CycleTimeMetricCard` (identical shape to `ScopeMetricCard`)
- The 3c-A domain already has: `SprintMetricCardData` (on `SprintSummaryResult.cs`) and `QaMetricCardData` (on `QaMetricsResult.cs`) — both in `Fokus.Domain.Analytics`

Each service also has an identical private `BuildMetricCard` / `BuildCard` helper that computes direction + polarity.

**Two options:**

**A — Per-calculator domain types (parallel to 3c-A):** Each calculator defines its own `*MetricCardData` record (e.g. `ScopeMetricCardData`, `CarryOverMetricCardData`, `BugRatioMetricCardData`, `CycleTimeMetricCardData`). Wire types (`ScopeMetricCard`, `BugRatioMetricCard`, `CycleTimeMetricCard`) move to their endpoint query files and stay wire-shaped. Identical `BuildMetricCard` logic is repeated per calculator.

**B — Single shared domain metric card:** Define one `MetricCardData(string Name, decimal Value, string DisplayValue, decimal? Delta, string? DeltaDirection, string? DeltaPolarity)` in a shared domain file (e.g. `Fokus.Domain/Analytics/SharedAnalyticsTypes.cs` or `Fokus.Domain/Analytics/MetricCardData.cs`). All 5 calculators (3c-A + 3c-B) reference it. The builder logic is extracted once. Wire types in endpoints keep their current names (they are already distinct: `ScopeMetricCard`, `BugRatioMetricCard`, etc.). This avoids proliferating ~5 nearly-identical domain records.

Option B requires touching the already-merged 3c-A domain records (`SprintMetricCardData`, `QaMetricCardData`) to unify them — an out-of-scope modification to 3c-A deliverables.

**Question:** Which strategy for metric card types — A (per-calculator, fully isolated) or B (single shared domain type, touching 3c-A domain files)? The ADR-010 "clean break" decision does not specify granularity of shared types. Please decide.

### Answer
**REVISED (final, authoritative):** Option B — **maximize sharing**. Use a single shared `MetricCardData` record + a shared `MetricCardBuilder` (delta/polarity logic) in `Fokus.Domain.Analytics`, referenced by all 3c-B calculators. The "touching already-merged 3c-A files is out of scope" objection is **OVERRULED** — unifying 3c-A's `SprintMetricCardData`/`QaMetricCardData` into the shared type IS in scope and desired, **provided shapes are compatible**. Verify shape-compatibility, then unify every card/helper that can be shared without loss; isolate (duplicate) ONLY a specific type/helper that genuinely cannot be unified, and document the exact shape divergence in the plan where you do. Default = share; isolation is the documented exception. Account for updating all existing 3c-A usages (endpoints + gRPC mapping) when collapsing the 3c-A records.

**Architect shape-compatibility finding (drives the plan):**
- The 6 base fields (`Name, Value, DisplayValue, Delta, DeltaDirection, DeltaPolarity`) are byte-identical across all 5 cards → **shared `MetricCardData`**.
- `SprintMetricCardData` additionally carries `List<SprintSparklinePoint> Sparkline` + `Rag`; `QaMetricCardData` additionally carries `IReadOnlyList<QaSparklinePoint> Sparkline` + `Rag`. The 3c-B cards carry **neither**. The sparkline element types AND collection interfaces also diverge between the two 3c-A cards (`List<SprintSparklinePoint>` vs `IReadOnlyList<QaSparklinePoint>`).
- **Decision:** The 3c-A cards **cannot collapse into the bare shared `MetricCardData`** without losing `Sparkline`/`Rag` (and they diverge from each other on the sparkline type). They are the **documented isolation exception**. However, the **delta-direction + delta-polarity computation kernel is byte-identical everywhere** → extracted to shared `MetricCardBuilder`, consumed by ALL calculators (3c-A and 3c-B). The 3c-A card *builders* are refactored to call `MetricCardBuilder` for the direction/polarity kernel (no card-shape change, no wire change), satisfying "share every helper that can be shared without loss."

---

## Q3: BugRatioMetricCard — wire type placement after service deletion
**From:** architect
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis (pre-plan)
**File:** `src/Services/Fokus/Fokus.API/Features/Analytics/BugRatioService.cs`, `GetBugRatio/GetBugRatioEndpoint.cs`

**Context:** `BugRatioService.cs` currently defines `BugRatioMetricCard` (lines 62–68) and it is the root file where `BugRatioTeamSingleMetrics` uses `BugRatioMetricCard`. The `GetBugRatioEndpoint.cs` references `BugRatioResponse`, `BugRatioSingleSprintResponse`, `BugRatioTeamSingleMetrics`, etc. — all currently declared in `BugRatioService.cs` (alongside the service class).

After migration: `BugRatioService.cs` is deleted, and `BugRatioMetricCard` (if kept as a wire type) must move somewhere. The natural target is `GetBugRatioQuery.cs` (the sibling query/response file for the endpoint). This is the same pattern used in 3c-A where `SprintSummaryResponse` wire types moved to `GetSprintSummaryQuery.cs`.

**Question:** Confirm that `BugRatioMetricCard` and all other wire records from `BugRatioService.cs` (the full record hierarchy: `BugRatioSprintInfo`, `BugRatioTeamTrendEntry`, `BugRatioTeamMetrics`, `BugRatioIssueTypeEntry`, `BugRatioAlertStatus`, etc.) should move to `GetBugRatioQuery.cs` (or a sibling `GetBugRatioResponse.cs`). Same question applies to `LeaderboardService.cs` records → `GetLeaderboardQuery.cs`. Is a sibling `*Response.cs` file preferred (to avoid bloating the query file), or is adding to the existing `*Query.cs` file acceptable?

### Answer
Sibling `*Response.cs` file — do not bloat the Query file.

---

## Q4: CycleTimeMetricCard is NOT used by gRPC — confirm no gRPC touch needed
**From:** architect
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis (pre-plan)
**File:** `src/Services/Fokus/Fokus.API/Grpc/SprintAnalyticsGrpcService.cs`

**Context:** The gRPC service currently uses `ScopeChangeService` (for `GetDisruptionMetricsAsync`) and `CarryOverService` (for `GetCarryOverHistoryAsync`). It does NOT use `CycleTimeService`, `BugRatioService`, or `LeaderboardService`. After 3c-B, only `ScopeChangeCalculator` and `CarryOverCalculator` need gRPC adaptation. The other 3 (CycleTime, BugRatio, Leaderboard) touch only their own endpoint files.

**Question:** Confirm that gRPC adaptation in 3c-B is limited to `ScopeChangeCalculator` + `CarryOverCalculator` only. CycleTime, BugRatio, and Leaderboard migrations are endpoint-only changes (no gRPC touch).

### Answer
Confirmed — CycleTime, BugRatio, Leaderboard are endpoint-only.

---

## Q5: DeltaDirection / DeltaPolarity helper duplication
**From:** architect
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis (pre-plan)
**File:** `BugRatioService.cs`, `LeaderboardService.cs`, `ScopeChangeService.cs`, `CarryOverService.cs`, `CycleTimeService.cs`

**Context:** `BugRatioService` and `LeaderboardService` both define identical private helpers: `DeltaDirection(decimal)`, `DeltaDirection(int)`, `DeltaPolarity(decimal, bool)`, `DeltaPolarity(int, bool)`. `ScopeChangeService` and `CarryOverService` both define an identical private `BuildMetricCard(...)` helper. `CycleTimeService` defines `BuildCard(...)` with the same direction/polarity logic.

After migration to domain calculators, these helpers will exist inside each calculator. They are domain logic (polarity is business semantics — "positive-down" means lower is better) and could be extracted to a shared `DeltaHelper` or `MetricCardBuilder` in `Fokus.Domain.Analytics`.

**Question:** Should the `DeltaDirection` / `DeltaPolarity` helper logic be:
- **A** — Duplicated privately inside each domain calculator (simplest, fully isolated, no new shared types)
- **B** — Extracted to a shared `MetricCardBuilder` static class in `Fokus.Domain/Analytics/MetricCardBuilder.cs`

Note: this is independent of Q2 (metric card record type consolidation). Even with per-calculator record types (Q2-A), the helper logic could be shared (Q5-B).

### Answer
**REVISED (final, authoritative):** Option B — **shared `MetricCardBuilder`** static class in `Fokus.Domain/Analytics/MetricCardBuilder.cs`. The delta-direction + delta-polarity kernel is byte-identical across all calculators (3c-A and 3c-B) and is extracted once. All 5 3c-B calculators consume it; the 2 3c-A card builders are refactored to call it for the direction/polarity computation (the only divergence between the two 3c-A builders is the input — `delta` directly vs `priorValue` and an inline-hardcoded `"positive-up"` — both reduce to the same `(delta, polarity) → (direction, deltaPolarity)` kernel). This satisfies "maximize sharing; isolate only what genuinely cannot unify."

---

## Q6: Execution ordering — are any 3c-B calculators interdependent?
**From:** architect
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis (pre-plan)

**Context:** In 3c-A, `TestTimelineCalculator` had to precede `SprintSummaryCalculator` because `SprintSummaryCalculator` takes `TestingCrunchFlag?` as an input. In 3c-B, checking dependencies:

- `CarryOverCalculator`: uses `TransitionAttributionChecker`, `ExcludedDeveloperFilter` (both already in domain after 3c-A). No dependency on other 3c-B calculators.
- `ScopeChangeCalculator`: uses `TransitionAttributionChecker` (domain). No dependency on other 3c-B calculators.
- `CycleTimeCalculator`: uses `TransitionAttributionChecker` (domain). No dependency on other 3c-B calculators.
- `BugRatioCalculator`: uses `TransitionAttributionChecker` (domain). `EvaluateAlert` is internal. No dependency on other 3c-B calculators.
- `LeaderboardCalculator`: uses `TransitionAttributionChecker` (domain). No dependency on other 3c-B calculators.

None of the 5 calculators depend on each other's output types. **The gRPC adaptation step (ScopeChange + CarryOver) must come after both `ScopeChangeCalculator` and `CarryOverCalculator` are defined**, but that is within the same pass.

**Question:** Is my dependency analysis correct — the 5 calculators are independent of each other and can be ordered in any sequence? Or is there a dependency I am missing? (Confirming before I fix the execution order in the plan.)

### Answer
Confirmed — all 5 are order-independent.

---
