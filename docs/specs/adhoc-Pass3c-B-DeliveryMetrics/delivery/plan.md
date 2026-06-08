# Implementation Plan — Pass 3c-B: DeliveryMetrics

**Feature Spec:** None (adhoc refactoring pass — scope from `docs/backlog.md` row 3c-B + ADR-005/010)
**Slug:** adhoc-Pass3c-B-DeliveryMetrics
**Plan:** docs/specs/adhoc-Pass3c-B-DeliveryMetrics/delivery/plan.md
**Skill:** `analytics-computation-service` + `domain-service`
**Intent:** Refactoring (restructure; behavior must not change)

---

## Context

Pass 3c-B migrates 5 delivery-metric analytics services from `Fokus.API/Features/Analytics/` to domain calculators in `Fokus.Domain/Analytics/` per ADR-005 (computation in Domain) and ADR-010 (endpoint-owns-response-DTO). Continuation of Pass 3c-A (which established the pattern and left `ScopeChangeService` + `CarryOverService` in the gRPC mixed state).

**In scope:** `CarryOverService`, `ScopeChangeService`, `CycleTimeService`, `BugRatioService`, `LeaderboardService`. Plus a **shared-type consolidation** (see "Shared types" below) that also touches the two 3c-A calculators/endpoints — explicitly in scope per the Q2/Q5 user decision.

**Locked decisions (from questions.md):**
- Q1: gRPC adaptation follows exact 3c-A pattern (ScopeChange + CarryOver). Wire contracts unchanged.
- Q2 + Q5: **Maximize sharing.** Single shared `MetricCardData` record + shared `MetricCardBuilder` (delta/polarity kernel) in `Fokus.Domain.Analytics`, consumed by all calculators. Unify everything that can be shared without loss; isolate only the genuine divergence and document it. Collapsing 3c-A's `SprintMetricCardData`/`QaMetricCardData` is **in scope** — but constrained by the shape-compatibility finding below.
- Q3: Wire records move to sibling `*Response.cs` files (not the Query file).
- Q4: gRPC adaptation is CarryOver + ScopeChange only. CycleTime, BugRatio, Leaderboard are endpoint-only.
- Q6: All 5 calculators are order-independent.

**Wire stability invariant:** All 5 HTTP response contracts (`CarryOverResponse`, `ScopeChangeResponse`, `CycleTimeResponse`, `BugRatioResponse`, `LeaderboardResponse`) AND the two 3c-A wire contracts (`SprintSummaryResponse`, `QaMetricsResponse`) must remain byte-for-byte identical. The gRPC contracts (`DisruptionMetrics`, `CarryOverHistory`, `AnonymizedSprintSummary`) must also remain unchanged.

**No migrations.** Pure code relocation + shared-type extraction — no EF schema changes, no DB changes.

---

## Shared types — the Q2/Q5 consolidation (read before any step)

### Shape-compatibility finding (architect, verified against source)

| Card type | Base 6 fields¹ | Extra fields | Used by |
|-----------|----------------|--------------|---------|
| `SprintMetricCardData` (3c-A) | ✓ | `List<SprintSparklinePoint> Sparkline`, `Rag` (init) | SprintSummary |
| `QaMetricCardData` (3c-A) | ✓ | `IReadOnlyList<QaSparklinePoint> Sparkline`, `Rag` (init) | QaMetrics |
| `ScopeMetricCard` (3c-B) | ✓ | none | ScopeChange + CarryOver (wire) |
| `BugRatioMetricCard` (3c-B) | ✓ | none | BugRatio (wire) |
| `CycleTimeMetricCard` (3c-B) | ✓ | none | CycleTime (wire) |

¹ Base 6 fields = `(string Name, decimal Value, string DisplayValue, decimal? Delta, string? DeltaDirection, string? DeltaPolarity)` — **byte-identical across all five**.

**Conclusion — what unifies and what is isolated:**

1. **`MetricCardData` (shared domain record) — UNIFIES the 3c-B domain cards.** A single `Fokus.Domain/Analytics/MetricCardData.cs` record with the 6 base fields. The 4 3c-B calculators that produce metric cards (ScopeChange, CarryOver, BugRatio, CycleTime) return `MetricCardData` instead of per-calculator card types. (Leaderboard has no metric card — it uses inline delta fields only.)

2. **`MetricCardBuilder` (shared domain static class) — UNIFIES the delta/polarity kernel for ALL calculators (3c-A + 3c-B).** `Fokus.Domain/Analytics/MetricCardBuilder.cs` exposes the byte-identical direction+polarity computation:
   ```
   public static (string? Direction, string? Polarity) ComputeDelta(decimal? delta, string polarity);
   ```
   plus a convenience `public static MetricCardData Build(string name, decimal value, string displayValue, decimal? delta, string polarity);` for the 3c-B cards. Every existing private `BuildMetricCard` / `BuildCard` (in all 5 3c-B services AND the 2 3c-A calculators) collapses to a call into this kernel.

3. **`SprintMetricCardData` and `QaMetricCardData` (3c-A) — ISOLATED (documented exception).** They CANNOT collapse into the bare `MetricCardData` because each carries a typed `Sparkline` + `Rag` that `MetricCardData` does not — and the two diverge from *each other* on the sparkline (`List<SprintSparklinePoint>` vs `IReadOnlyList<QaSparklinePoint>`). Collapsing them would lose data / change wire shape. **They stay as distinct records.** What they DO share: their card builders are refactored to delegate the direction/polarity computation to `MetricCardBuilder.ComputeDelta(...)` (no shape change, no wire change). This is the "share every helper that can be shared without loss" requirement applied to the isolated types.

   **Exact divergence documented:** `SprintMetricCardData`/`QaMetricCardData` = `MetricCardData`'s 6 fields **+ `Sparkline` (divergent element + collection type) + `Rag`**. The sparkline is the load-bearing reason for isolation.

4. **Wire-layer card types stay per-endpoint.** `ScopeMetricCard`, `BugRatioMetricCard`, `CycleTimeMetricCard` remain distinct **wire** records in their endpoint `*Response.cs` files (they are public API contracts and must not change names — wire stability). Each endpoint maps `MetricCardData → {its}MetricCard` via Mapster. CarryOver's single-sprint metrics also map `MetricCardData → ScopeMetricCard` (carry-over reused `ScopeMetricCard` as its wire type today; it keeps that wire type).

### Net shared-type deliverables

- **Create** `Fokus.Domain/Analytics/MetricCardData.cs` (shared domain record)
- **Create** `Fokus.Domain/Analytics/MetricCardBuilder.cs` (shared kernel)
- **Modify** `Fokus.Domain/Analytics/SprintSummaryCalculator.cs` — its private `BuildMetricCard` delegates to `MetricCardBuilder.ComputeDelta`
- **Modify** `Fokus.Domain/Analytics/QaMetricsCalculator.cs` — its private QA-card builder delegates to `MetricCardBuilder.ComputeDelta`
- **3c-A endpoints + gRPC:** `SprintMetricCardData`/`QaMetricCardData` are **unchanged** (isolated), so `GetSprintSummaryEndpoint.cs`, `GetQaMetricsEndpoint.cs`, and `SprintAnalyticsGrpcService.MapToAnonymizedSummary` need **no signature changes** from this consolidation. (Verified: those consumers read `SprintMetricCardData`/`SprintMetricsData` fields, which keep their shapes.) The only 3c-A source touched is the two calculators' internal builders.

---

## Ordering

No cross-calculator dependencies (Q6). The shared types (Step 1) must land first because every calculator references them. The 3c-A builder refactor (Step 2) is independent and lands early to validate the shared kernel before the 3c-B calculators consume it. Then endpoint-only migrations (CarryOver, CycleTime, BugRatio, Leaderboard), then ScopeChange, then gRPC adaptation.

Execution order: **1 → 2 → 3 → 4 → 5 → 6 → 7 → 8 → 9 → 10 → 11**

---

## Mode-wrapper envelopes are WIRE-ONLY (global rule — Finding 3)

Each of the 5 features today wraps its two computation shapes in a top-level **mode envelope**:

| Envelope (wire-only) | Source lines | Shape |
|----------------------|--------------|-------|
| `CarryOverResponse` | `CarryOverService.cs` 102–105 | `(string Mode, CarryOverMultiSprintResponse? MultiSprint, CarryOverSingleSprintResponse? SingleSprint)` |
| `ScopeChangeResponse` | `ScopeChangeService.cs` 94–97 | `(string Mode, ScopeChangeMultiSprintResponse? MultiSprint, ScopeChangeSingleSprintResponse? SingleSprint)` |
| `CycleTimeResponse` | `CycleTimeService.cs` 94–97 | `(string Mode, CycleTimeSingleSprintResponse? SingleSprint, CycleTimeMultiSprintResponse? MultiSprint)` |
| `BugRatioResponse` | `BugRatioService.cs` 114–117 | `(string Mode, BugRatioMultiSprintResponse? MultiSprint, BugRatioSingleSprintResponse? SingleSprint)` |
| `LeaderboardResponse` | `LeaderboardService.cs` 70–73 | `(string Mode, LeaderboardMultiSprintResponse? MultiSprint, LeaderboardSingleSprintResponse? SingleSprint)` |

**Rule — applies to every migration step (3–7):**
- The **domain calculator returns the two shapes separately** (the `*MultiSprintData` and `*SingleSprintData` records). The calculator does NOT know about the `Mode`/`Multi?`/`Single?` envelope — that is a transport concern.
- The **`*Response` envelope is declared ONLY in the endpoint's `*Response.cs` wire file** and is **never** added to a domain `*Result.cs` rename table. (The rename tables omit it by design; the wire-file record lists below now include it explicitly.)
- The **endpoint builds the envelope**, exactly as today: it inspects the request mode, calls the matching `Compute…` method, maps the domain result to the wire shape, and constructs `new {Feature}Response(mode, multiWireOrNull, singleWireOrNull)`. This preserves the existing handler control flow (the endpoints already branch on `req.SprintId.HasValue`).
- Do NOT mirror `*Response` into the domain — doing so would leak a wire-flavored name into `Fokus.Domain` (ADR-010 violation) and is the one residual leak risk the critic flagged.

---

## Files to Create (domain layer)

| New file | Source |
|----------|--------|
| `src/Services/Fokus/Fokus.Domain/Analytics/MetricCardData.cs` | New shared record (6 base fields) |
| `src/Services/Fokus/Fokus.Domain/Analytics/MetricCardBuilder.cs` | New shared kernel (extracted from the 7 duplicate `BuildMetricCard`/`BuildCard` helpers) |
| `src/Services/Fokus/Fokus.Domain/Analytics/CarryOverResult.cs` | Records from `CarryOverService.cs` lines 4–98 |
| `src/Services/Fokus/Fokus.Domain/Analytics/CarryOverCalculator.cs` | `CarryOverService.cs` lines 109–752 |
| `src/Services/Fokus/Fokus.Domain/Analytics/CycleTimeResult.cs` | Records from `CycleTimeService.cs` lines 4–97 |
| `src/Services/Fokus/Fokus.Domain/Analytics/CycleTimeCalculator.cs` | `CycleTimeService.cs` lines 101–814 |
| `src/Services/Fokus/Fokus.Domain/Analytics/BugRatioResult.cs` | Records from `BugRatioService.cs` lines 4–117 |
| `src/Services/Fokus/Fokus.Domain/Analytics/BugRatioCalculator.cs` | `BugRatioService.cs` lines 121–562 |
| `src/Services/Fokus/Fokus.Domain/Analytics/LeaderboardResult.cs` | Records from `LeaderboardService.cs` lines 4–73 |
| `src/Services/Fokus/Fokus.Domain/Analytics/LeaderboardCalculator.cs` | `LeaderboardService.cs` lines 77–328 |
| `src/Services/Fokus/Fokus.Domain/Analytics/ScopeChangeResult.cs` | Records from `ScopeChangeService.cs` lines 4–97 |
| `src/Services/Fokus/Fokus.Domain/Analytics/ScopeChangeCalculator.cs` | `ScopeChangeService.cs` lines 101–862 |

## Files to Create (wire layer — endpoint-owned response files)

| New file | Purpose |
|----------|---------|
| `src/Services/Fokus/Fokus.API/Features/Analytics/GetCarryOver/GetCarryOverResponse.cs` | Wire records from `CarryOverService.cs` |
| `src/Services/Fokus/Fokus.API/Features/Analytics/GetScopeChange/GetScopeChangeResponse.cs` | Wire records from `ScopeChangeService.cs` |
| `src/Services/Fokus/Fokus.API/Features/Analytics/GetCycleTime/GetCycleTimeResponse.cs` | Wire records from `CycleTimeService.cs` |
| `src/Services/Fokus/Fokus.API/Features/Analytics/GetBugRatio/GetBugRatioResponse.cs` | Wire records from `BugRatioService.cs` |
| `src/Services/Fokus/Fokus.API/Features/Analytics/GetLeaderboard/GetLeaderboardResponse.cs` | Wire records from `LeaderboardService.cs` |

## Files to Modify

| File | Change |
|------|--------|
| `src/Services/Fokus/Fokus.Domain/Analytics/SprintSummaryCalculator.cs` | Private `BuildMetricCard` delegates direction/polarity to `MetricCardBuilder.ComputeDelta`; card record + wire unchanged |
| `src/Services/Fokus/Fokus.Domain/Analytics/QaMetricsCalculator.cs` | Private QA-card builder delegates direction/polarity to `MetricCardBuilder.ComputeDelta`; card record + wire unchanged |
| `src/Services/Fokus/Fokus.API/Features/Analytics/GetCarryOver/GetCarryOverEndpoint.cs` | `CarryOverService` → `CarryOverCalculator`; map domain results to wire |
| `src/Services/Fokus/Fokus.API/Features/Analytics/GetScopeChange/GetScopeChangeEndpoint.cs` | `ScopeChangeService` → `ScopeChangeCalculator`; map domain results to wire |
| `src/Services/Fokus/Fokus.API/Features/Analytics/GetCycleTime/GetCycleTimeEndpoint.cs` | `CycleTimeService` → `CycleTimeCalculator`; map domain results to wire |
| `src/Services/Fokus/Fokus.API/Features/Analytics/GetBugRatio/GetBugRatioEndpoint.cs` | `BugRatioService` → `BugRatioCalculator`; map domain results to wire |
| `src/Services/Fokus/Fokus.API/Features/Analytics/GetLeaderboard/GetLeaderboardEndpoint.cs` | `LeaderboardService` → `LeaderboardCalculator`; map domain results to wire |
| `src/Services/Fokus/Fokus.API/Grpc/SprintAnalyticsGrpcService.cs` | `ScopeChangeService` + `CarryOverService` → calculators; rework `MapToDisruptionMetrics` / `MapToCarryOverHistory` to domain result types; retire mixed state |
| `src/Services/Fokus/Fokus.API/DependencyInjection.cs` | Remove 5 service registrations; add 5 calculator registrations |

## Files to Delete

| File | Reason |
|------|--------|
| `src/Services/Fokus/Fokus.API/Features/Analytics/CarryOverService.cs` | Replaced by calculator |
| `src/Services/Fokus/Fokus.API/Features/Analytics/ScopeChangeService.cs` | Replaced by calculator |
| `src/Services/Fokus/Fokus.API/Features/Analytics/CycleTimeService.cs` | Replaced by calculator |
| `src/Services/Fokus/Fokus.API/Features/Analytics/BugRatioService.cs` | Replaced by calculator |
| `src/Services/Fokus/Fokus.API/Features/Analytics/LeaderboardService.cs` | Replaced by calculator |

---

## Step 1 — Create shared MetricCardData + MetricCardBuilder

**Skill:** None (new shared domain primitives; no existing skill covers a cross-calculator value record + helper)
**Files:**
- Create `src/Services/Fokus/Fokus.Domain/Analytics/MetricCardData.cs`
- Create `src/Services/Fokus/Fokus.Domain/Analytics/MetricCardBuilder.cs`

**MetricCardData.cs** — namespace `Fokus.Domain.Analytics`. A sealed record with exactly the 6 base fields shared by all current card types:
`(string Name, decimal Value, string DisplayValue, decimal? Delta, string? DeltaDirection, string? DeltaPolarity)`.

**MetricCardBuilder.cs** — namespace `Fokus.Domain.Analytics`. Static class. Extract the byte-identical direction/polarity kernel that today is duplicated in 7 places (`ScopeChangeService.BuildMetricCard`, `CarryOverService.BuildMetricCard`, `BugRatioService.BuildMetricCard`, `CycleTimeService.BuildCard`, `BugRatioService.DeltaDirection`/`DeltaPolarity`, `LeaderboardService.DeltaDirection`/`DeltaPolarity`, plus the 2 3c-A builders):

The builder exposes **two distinct polarity paths** — they must stay separate (Finding 2):

- **String-polarity path** (the `BuildMetricCard`/`BuildCard` switch in ScopeChange, CarryOver, BugRatio, CycleTime, and both 3c-A builders):
  - `ComputeDelta(decimal? delta, string polarity) → (string? Direction, string? Polarity)`. Exactly as today: when `delta` is null → both null; otherwise `Direction` = `"up"` (>0) / `"down"` (<0) / `"flat"` (==0); `Polarity` from the switch — `"positive-up"`, `"positive-down"`, **and any other string (notably `"neutral"`) → `"neutral"`**. The `"neutral"` input is a real call (ScopeChange uses it for `CommittedSpActive`, `CommittedSpTotal`, `RemovedSp`, `NetScopeChange`) and MUST short-circuit to `"neutral"` regardless of the delta sign. Do NOT route `"neutral"` through the bool path.
  - `Build(string name, decimal value, string displayValue, decimal? delta, string polarity) → MetricCardData`. Rounds `value` and `delta` to 1 dp (matching the current 3c-B builders) and fills direction/polarity via `ComputeDelta`. This is the constructor the 4 3c-B card-producing calculators call.
- **Bool-polarity path** (the `DeltaDirection(...)`/`DeltaPolarity(..., bool positiveUp)` helpers in BugRatio + Leaderboard, used for the flat delta bundles, NOT cards):
  - `Direction(decimal delta) → string` = `"up"`/`"down"`/`"flat"` (int callers cast `(decimal)delta`).
  - `Polarity(decimal delta, bool positiveUp) → string`. `positiveUp:true` → up=positive / down=negative / flat=neutral; `positiveUp:false` → down=positive / up=negative / flat=neutral. This path has **no `"neutral"`-always option** — a caller that wants an unconditional `"neutral"` (e.g. `LeaderboardService.cs:213` `TotalSpDeltaPolarity`) must NOT call `Polarity(...)`; it keeps the hard-coded literal (see Finding 1 / Step 6). The bool path always returns positive/negative/neutral by sign.

Internally `Polarity(delta, positiveUp)` and the string switch share the same sign→label kernel; expose both signatures so neither call site changes its output. The bool form is NOT a substitute for the string `"neutral"` short-circuit.

**Acceptance:** both files compile; `ComputeDelta` reproduces the current output for every string-polarity input used in the codebase (`"neutral"`, `"positive-up"`, `"positive-down"`); `Direction`/`Polarity` reproduce the current `BugRatio`/`Leaderboard` helper output for `positiveUp` true and false; the `"neutral"` string input yields `"neutral"` for non-flat deltas; build green.

**Confidence:** medium — three call shapes (string-polarity card builder, `priorValue`-based QA builder, bool-polarity delta helpers) plus int-vs-decimal; the builder must cover all without changing any output, and the `"neutral"`-string short-circuit must not collapse into the bool path.

---

## Step 2 — Refactor 3c-A calculator builders to use MetricCardBuilder

**Skill:** None (internal helper delegation; no card-record or wire change)
**Files:**
- Modify `src/Services/Fokus/Fokus.Domain/Analytics/SprintSummaryCalculator.cs`
- Modify `src/Services/Fokus/Fokus.Domain/Analytics/QaMetricsCalculator.cs`

`SprintMetricCardData` and `QaMetricCardData` are the **isolated exception** (Sparkline + Rag — see "Shared types"). The records and all wire mappings are **unchanged**. Only the private builders are refactored to call the shared kernel:

- `SprintSummaryCalculator.BuildMetricCard(name, value, displayValue, delta, polarity, sparkline)`: replace the inline `direction`/`deltaPolarity` block (currently lines ~535–548) with `var (direction, deltaPolarity) = MetricCardBuilder.ComputeDelta(delta, polarity);`. Still constructs `SprintMetricCardData` with `Sparkline` (unchanged).
- `QaMetricsCalculator` QA-card builder (currently ~lines 414–442): it computes `delta = priorValue - value`-style and hardcodes `"positive-up"`. Replace its inline `direction`/`deltaPolarity` block with `var (direction, deltaPolarity) = MetricCardBuilder.ComputeDelta(delta, "positive-up");` Still constructs `QaMetricCardData` with `Sparkline` + `Rag` (unchanged).

**Do NOT** change the card record shapes, the calculator return types, the endpoints, or the gRPC mapping. This step is purely the helper-sharing requirement applied to the isolated 3c-A types.

**Acceptance:** build green; `GET /api/analytics/sprint-summary` and `GET /api/sprints/{id}/qa-metrics` JSON byte-identical to before; `AnonymizedSprintSummary` gRPC contract unaffected (the gRPC service reads `SprintMetricsData`/`SprintMetricCardData` fields, which are unchanged).

**Confidence:** medium — the QA builder's `priorValue→delta` derivation and hardcoded polarity must reduce to exactly `ComputeDelta(delta, "positive-up")`; verify the sign of `delta` matches (QA computes `value - priorValue` today — confirm the existing direction output is preserved).

---

## Step 3 — CarryOverCalculator + CarryOverResult.cs + GetCarryOverResponse.cs + endpoint

**Skill:** `analytics-computation-service`
**Files:**
- Create `src/Services/Fokus/Fokus.Domain/Analytics/CarryOverResult.cs`
- Create `src/Services/Fokus/Fokus.Domain/Analytics/CarryOverCalculator.cs`
- Create `src/Services/Fokus/Fokus.API/Features/Analytics/GetCarryOver/GetCarryOverResponse.cs`
- Modify `src/Services/Fokus/Fokus.API/Features/Analytics/GetCarryOver/GetCarryOverEndpoint.cs`

**Domain result records** (`CarryOverResult.cs`, source `CarryOverService.cs` lines 4–98): rename to domain `*Data` names. Single-sprint metrics (`CarryOverRate`, `CarryOverSp`, `CarryOverTicketCount`) become the shared `MetricCardData` (was `ScopeMetricCard`). Rename map:

| Old | New domain |
|-----|-----------|
| `CarryOverSprintInfo` | `CarryOverSprintData` |
| `CarryOverSummaryMetrics` | `CarryOverSummaryData` |
| `CarryOverStatusDistributionEntry` | `CarryOverStatusDistributionData` |
| `CarryOverPerSprintData` | `CarryOverPerSprintMetricsData` |
| `CarryOverIssueTypeEntry` | `CarryOverIssueTypeData` |
| `CarryOverZombieSummary` | `CarryOverZombieData` |
| `CarryOverMultiSprintResponse` | `CarryOverMultiSprintData` |
| `CarryOverSingleSprintMetrics` | `CarryOverSingleSprintMetricsData` (fields now `MetricCardData`) |
| `CarryOverDestinationBucket` | `CarryOverDestinationBucketData` |
| `CarryOverDestination` | `CarryOverDestinationData` |
| `CarryOverTicketEntry` | `CarryOverTicketData` |
| `ZombieTrajectorySprintEntry` | `ZombieTrajectorySprintData` |
| `ZombieTrajectoryEntry` | `ZombieTrajectoryData` |
| `CarryOverSingleSprintResponse` | `CarryOverSingleSprintData` |

**Calculator** (`CarryOverService.cs` lines 109–752): `CarryOverCalculator`. `ComputeMultiSprint` → `CarryOverMultiSprintData`; `ComputeSingleSprint` → `CarryOverSingleSprintData`. The private `BuildMetricCard` is deleted — call `MetricCardBuilder.Build(name, value, displayValue, delta, "positive-down")`. `TransitionAttributionChecker` / `ExcludedDeveloperFilter` resolve via `Fokus.Domain.Analytics`.

**Wire file** (`GetCarryOverResponse.cs`, namespace `…GetCarryOver`): move all wire records with today's property names/types — `CarryOverSprintInfo`, `CarryOverSummaryMetrics`, `CarryOverStatusDistributionEntry`, `CarryOverPerSprintData`, `CarryOverIssueTypeEntry`, `CarryOverZombieSummary`, `CarryOverMultiSprintResponse`, **`ScopeMetricCard`** (carry-over's single-sprint wire card), `CarryOverSingleSprintMetrics`, `CarryOverDestinationBucket`, `CarryOverDestination`, `CarryOverTicketEntry`, `ZombieTrajectorySprintEntry`, `ZombieTrajectoryEntry`, `CarryOverSingleSprintResponse`, and the **`CarryOverResponse`** envelope (wire-only, see the global Finding 3 rule). Register Mapster maps: `CarryOverMultiSprintData → CarryOverMultiSprintResponse`, `CarryOverSingleSprintData → CarryOverSingleSprintResponse`, `MetricCardData → ScopeMetricCard`, and the per-sprint nested map below. Pattern: 3c-A `GetSprintSummary` Mapster registration.

> **Finding 9 — name-collision note.** The domain per-sprint record is renamed `CarryOverPerSprintData` → **`CarryOverPerSprintMetricsData`** (domain), while the **wire** record keeps the old name **`CarryOverPerSprintData`** (wire-stability). So the Mapster map is `CarryOverPerSprintMetricsData (domain) → CarryOverPerSprintData (wire)` — the reused identifier `CarryOverPerSprintData` now refers to the *wire* type in the `…GetCarryOver` namespace, NOT the domain type. The developer must not confuse the two; they live in different namespaces and the map crosses between them.

**Endpoint:** inject `CarryOverCalculator`; build the `CarryOverResponse` envelope (Finding 3) and map the inner shapes before `Send.OkAsync`.

**Acceptance:** build green; `GET /api/analytics/carry-over` JSON byte-identical to before.

**Confidence:** medium — carry-over reused `ScopeMetricCard`; the `MetricCardData → ScopeMetricCard` map is the key wire-stability check.

---

## Step 4 — CycleTimeCalculator + CycleTimeResult.cs + GetCycleTimeResponse.cs + endpoint

**Skill:** `analytics-computation-service`
**Files:**
- Create `src/Services/Fokus/Fokus.Domain/Analytics/CycleTimeResult.cs`
- Create `src/Services/Fokus/Fokus.Domain/Analytics/CycleTimeCalculator.cs`
- Create `src/Services/Fokus/Fokus.API/Features/Analytics/GetCycleTime/GetCycleTimeResponse.cs`
- Modify `src/Services/Fokus/Fokus.API/Features/Analytics/GetCycleTime/GetCycleTimeEndpoint.cs`

**Domain records** (`CycleTimeService.cs` lines 4–97): rename to `*Data`. `CycleTimeMetricCard` collapses to the shared `MetricCardData` (the cycle-time metric cards carry no sparkline/rag). Rename map:

| Old | New domain |
|-----|-----------|
| `CycleTimeSprintInfo` | `CycleTimeSprintData` |
| `CycleTimeMetricCard` | `MetricCardData` (shared — not a per-calculator type) |
| `StageBreakdownEntry` | `StageBreakdownData` |
| `CycleTimeScatterPoint` | `CycleTimeScatterPointData` |
| `StageFunnelEntry` | `StageFunnelData` |
| `CycleTimeIssueTypeEntry` | `CycleTimeIssueTypeData` |
| `CycleTimeDeveloperEntry` | `CycleTimeDeveloperData` |
| `CycleTimeOutlierEntry` | `CycleTimeOutlierData` |
| `CycleTimeBoundaries` | `CycleTimeBoundariesData` |
| `CycleTimeSingleSprintResponse` | `CycleTimeSingleSprintData` |
| `CycleTimeTrendEntry` | `CycleTimeTrendData` |
| `CycleTimeSprintSummaryEntry` | `CycleTimeSprintSummaryData` |
| `CycleTimeMultiSprintResponse` | `CycleTimeMultiSprintData` |

Note: the domain `MetricCards` collections become `List<MetricCardData>?`.

**Calculator** (`CycleTimeService.cs` lines 101–814): `CycleTimeCalculator`. `ComputeSingleSprint` → `CycleTimeSingleSprintData`; `ComputeMultiSprint` → `CycleTimeMultiSprintData`. Private `TicketCycleResult`/`PercentileSet` stay private. Delete the private `BuildCard`/`BuildMetricCards` direction/polarity inline; call `MetricCardBuilder.Build(...)`. (Keep the per-card display-string formatting in the calculator — only the direction/polarity kernel is shared.)

**Wire file** (`GetCycleTimeResponse.cs`): move all wire records with today's names, including **`CycleTimeMetricCard`** (wire stays this name) and the **`CycleTimeResponse`** envelope (source lines 94–97; wire-only, Finding 3). Register Mapster maps `CycleTimeSingleSprintData → CycleTimeSingleSprintResponse`, `CycleTimeMultiSprintData → CycleTimeMultiSprintResponse`, `MetricCardData → CycleTimeMetricCard`.

**Endpoint:** inject `CycleTimeCalculator`; build the `CycleTimeResponse` envelope (Finding 3) and map the inner shapes.

**Acceptance:** build green; `GET /api/analytics/cycle-time` JSON byte-identical.

**Confidence:** medium — `BuildMetricCards` is currently an instance method; preserve behavior; the `List<MetricCardData>?` nullability must match the current `List<CycleTimeMetricCard>?`.

---

## Step 5 — BugRatioCalculator + BugRatioResult.cs + GetBugRatioResponse.cs + endpoint

**Skill:** `analytics-computation-service`
**Files:**
- Create `src/Services/Fokus/Fokus.Domain/Analytics/BugRatioResult.cs`
- Create `src/Services/Fokus/Fokus.Domain/Analytics/BugRatioCalculator.cs`
- Create `src/Services/Fokus/Fokus.API/Features/Analytics/GetBugRatio/GetBugRatioResponse.cs`
- Modify `src/Services/Fokus/Fokus.API/Features/Analytics/GetBugRatio/GetBugRatioEndpoint.cs`

**Domain records** (`BugRatioService.cs` lines 4–117): rename to `*Data`. `BugRatioMetricCard` collapses to shared `MetricCardData`. `BugRatioDeveloperDelta` keeps its pre-computed string direction/polarity fields (it is NOT a metric card — it is a flat delta bundle); rename to `BugRatioDeveloperDeltaData`, fields unchanged. Rename map:

| Old | New domain |
|-----|-----------|
| `BugRatioSprintInfo` | `BugRatioSprintData` |
| `BugRatioTeamTrendEntry` | `BugRatioTeamTrendData` |
| `BugRatioTeamMetrics` | `BugRatioTeamMetricsData` |
| `BugRatioIssueTypeEntry` | `BugRatioIssueTypeData` |
| `BugRatioAlertStatus` | `BugRatioAlertData` |
| `BugRatioDeveloperSprintBreakdown` | `BugRatioDeveloperSprintData` |
| `BugRatioDeveloperEntry` | `BugRatioDeveloperData` |
| `BugRatioMultiSprintResponse` | `BugRatioMultiSprintData` |
| `BugRatioMetricCard` | `MetricCardData` (shared) |
| `BugRatioTeamSingleMetrics` | `BugRatioTeamSingleData` (fields now `MetricCardData`) |
| `BugRatioDeveloperDelta` | `BugRatioDeveloperDeltaData` |
| `BugRatioDeveloperSingleEntry` | `BugRatioDeveloperSingleData` |
| `BugRatioSingleSprintResponse` | `BugRatioSingleSprintData` |

**Calculator** (`BugRatioService.cs` lines 121–562): `BugRatioCalculator`. Delete private `BuildMetricCard` → `MetricCardBuilder.Build(...)`. Delete private `DeltaDirection(decimal)`/`DeltaDirection(int)`/`DeltaPolarity(decimal,bool)`/`DeltaPolarity(int,bool)` → call `MetricCardBuilder.Direction(...)`/`MetricCardBuilder.Polarity(...)` (int callers cast to decimal). `EvaluateAlert` stays private; returns `BugRatioAlertData`. `TransitionAttributionChecker` resolves via `Fokus.Domain.Analytics`.

**Wire file** (`GetBugRatioResponse.cs`): move all wire records with today's names, including **`BugRatioMetricCard`**, **`BugRatioDeveloperDelta`**, and the **`BugRatioResponse`** envelope (source lines 114–117; wire-only, Finding 3). Register Mapster maps `BugRatioMultiSprintData → BugRatioMultiSprintResponse`, `BugRatioSingleSprintData → BugRatioSingleSprintResponse`, `MetricCardData → BugRatioMetricCard`, `BugRatioDeveloperDeltaData → BugRatioDeveloperDelta`.

**Endpoint:** inject `BugRatioCalculator`; build the `BugRatioResponse` envelope (Finding 3) and map the inner shapes.

**Acceptance:** build green; `GET /api/analytics/bug-ratio` JSON byte-identical.

**Confidence:** medium — `BugRatioDeveloperDelta` carries 15 pre-computed string fields (all 10 polarity/direction fields are genuine `DeltaPolarity(...)`/`DeltaDirection(...)` calls — no hard-coded literal here, unlike Leaderboard); verify the shared `Direction`/`Polarity` overloads reproduce them exactly.

---

## Step 6 — LeaderboardCalculator + LeaderboardResult.cs + GetLeaderboardResponse.cs + endpoint

**Skill:** `analytics-computation-service`
**Files:**
- Create `src/Services/Fokus/Fokus.Domain/Analytics/LeaderboardResult.cs`
- Create `src/Services/Fokus/Fokus.Domain/Analytics/LeaderboardCalculator.cs`
- Create `src/Services/Fokus/Fokus.API/Features/Analytics/GetLeaderboard/GetLeaderboardResponse.cs`
- Modify `src/Services/Fokus/Fokus.API/Features/Analytics/GetLeaderboard/GetLeaderboardEndpoint.cs`

**Domain records** (`LeaderboardService.cs` lines 4–73): rename to `*Data`. Leaderboard has **no metric card** — only `LeaderboardDeveloperDelta` (flat string-field delta bundle, like BugRatio's). Rename map:

| Old | New domain |
|-----|-----------|
| `LeaderboardSprintInfo` | `LeaderboardSprintData` |
| `LeaderboardDeveloperSprintBreakdown` | `LeaderboardDeveloperSprintData` |
| `LeaderboardDeveloperEntry` | `LeaderboardDeveloperData` |
| `LeaderboardMultiSprintResponse` | `LeaderboardMultiSprintData` |
| `LeaderboardDeveloperDelta` | `LeaderboardDeveloperDeltaData` |
| `LeaderboardDeveloperSingleEntry` | `LeaderboardDeveloperSingleData` |
| `LeaderboardSingleSprintResponse` | `LeaderboardSingleSprintData` |
| `LeaderboardResponse` | — (stays in API layer as wire envelope — see Finding 3 below) |

**Calculator** (`LeaderboardService.cs` lines 77–328): `LeaderboardCalculator`. Replace the private `DeltaDirection`/`DeltaPolarity` overloads with calls to `MetricCardBuilder.Direction(...)`/`MetricCardBuilder.Polarity(..., positiveUp)`. `GetCapacity` stays private. `TransitionAttributionChecker` resolves via `Fokus.Domain.Analytics`.

> **Finding 1 (HIGH) — carve out the hard-coded literal.** At `LeaderboardService.cs:213`, the `TotalSpDeltaPolarity` field is the **literal `"neutral"`**, NOT a `DeltaPolarity(...)` call:
> ```csharp
> Math.Round(totalSpDelta, 1), DeltaDirection(totalSpDelta), "neutral",   // ← line 213
> ```
> A blanket "replace every `DeltaPolarity` → `MetricCardBuilder.Polarity`" would NOT touch this (it is not a `DeltaPolarity` call), but the developer must NOT "tidy" it into `MetricCardBuilder.Polarity(totalSpDelta, ...)` either — doing so would compute positive/negative from the sign and silently change wire output for non-flat total-SP deltas. **Keep the literal `"neutral"` exactly as-is.** Only `FeatureSpDeltaPolarity`, `BugSpDeltaPolarity`, `FeatureTicketsDeltaPolarity`, `BugTicketsDeltaPolarity` are `DeltaPolarity(...)` calls that convert to `MetricCardBuilder.Polarity(...)`. `TotalSpDeltaDirection` IS a `DeltaDirection(...)` call → converts to `MetricCardBuilder.Direction(...)`; only the polarity is the literal.

**Wire file** (`GetLeaderboardResponse.cs`): move all wire records with today's names, including **`LeaderboardDeveloperDelta`** and the **`LeaderboardResponse`** envelope (source lines 70–73). Register Mapster maps `LeaderboardMultiSprintData → LeaderboardMultiSprintResponse`, `LeaderboardSingleSprintData → LeaderboardSingleSprintResponse`, `LeaderboardDeveloperDeltaData → LeaderboardDeveloperDelta`.

**Endpoint:** inject `LeaderboardCalculator`; build the `LeaderboardResponse` envelope (see Finding 3) and map the inner shapes; `Send.OkAsync`.

**Acceptance:** build green; `GET /api/analytics/leaderboard` JSON byte-identical — in particular `TotalSpDeltaPolarity` is `"neutral"` for every developer regardless of total-SP delta sign.

**Confidence:** medium — delta bundle has 15 string fields incl. one hard-coded `"neutral"` (line 213) that must NOT be converted; verify shared helpers reproduce the other 14 exactly.

---

## Step 7 — ScopeChangeCalculator + ScopeChangeResult.cs + GetScopeChangeResponse.cs + endpoint

**Skill:** `analytics-computation-service`
**Files:**
- Create `src/Services/Fokus/Fokus.Domain/Analytics/ScopeChangeResult.cs`
- Create `src/Services/Fokus/Fokus.Domain/Analytics/ScopeChangeCalculator.cs`
- Create `src/Services/Fokus/Fokus.API/Features/Analytics/GetScopeChange/GetScopeChangeResponse.cs`
- Modify `src/Services/Fokus/Fokus.API/Features/Analytics/GetScopeChange/GetScopeChangeEndpoint.cs`

**Domain records** (`ScopeChangeService.cs` lines 4–97): rename to `*Data`. `ScopeMetricCard` collapses to shared `MetricCardData`. Rename map:

| Old | New domain |
|-----|-----------|
| `ScopeChangeSprintInfo` | `ScopeChangeSprintData` |
| `ScopeChangeSummaryMetrics` | `ScopeChangeSummaryData` |
| `ScopeChangePerSprintData` | `ScopeChangePerSprintMetricsData` |
| `ClassificationEntry` | `ClassificationData` |
| `ScopeChangeMultiSprintResponse` | `ScopeChangeMultiSprintData` |
| `ScopeMetricCard` | `MetricCardData` (shared) |
| `ScopeChangeSingleSprintMetrics` | `ScopeChangeSingleSprintMetricsData` (fields now `MetricCardData`) |
| `BurnupDataPoint` | `BurnupDataPointData` |
| `ScopeChangeEvent` | `ScopeChangeEventData` |
| `BugTimeInProgress` | `BugTimeInProgressData` |
| `ScopeChangeSingleSprintResponse` | `ScopeChangeSingleSprintData` |

**Calculator** (`ScopeChangeService.cs` lines 101–862): `ScopeChangeCalculator`. Delete private `BuildMetricCard` → `MetricCardBuilder.Build(...)`. The existing per-card polarity string argument is passed through unchanged — including the several cards that pass `"neutral"` (`CommittedSpActive`, `CommittedSpTotal`, `RemovedSp`, `NetScopeChange`), which `MetricCardBuilder.ComputeDelta` short-circuits to `"neutral"` (Step 1 / Finding 2). Private `ScopeSprintMetrics`/`MidSprintAddition` stay private. `TransitionAttributionChecker` resolves via `Fokus.Domain.Analytics`.

**Wire file** (`GetScopeChangeResponse.cs`): move all wire records with today's names, including **`ScopeMetricCard`** (wire stays this name — this is now a *separate declaration* from carry-over's copy, each in its own endpoint namespace; the old shared root-namespace `ScopeMetricCard` disappears when `ScopeChangeService.cs` is deleted in Step 9) and the **`ScopeChangeResponse`** envelope (source lines 94–97; wire-only, Finding 3). Register Mapster maps `ScopeChangeMultiSprintData → ScopeChangeMultiSprintResponse`, `ScopeChangeSingleSprintData → ScopeChangeSingleSprintResponse`, `MetricCardData → ScopeMetricCard`.

**Endpoint:** inject `ScopeChangeCalculator`; map to wire.

**Acceptance:** build green; `GET /api/analytics/scope-change` JSON byte-identical.

**Confidence:** medium — `ScopeMetricCard` now exists in two endpoint namespaces (scope-change + carry-over); confirm no ambiguous reference in the gRPC service after Step 8.

---

## Step 8 — Adapt SprintAnalyticsGrpcService to ScopeChangeCalculator + CarryOverCalculator

**Skill:** None (constructor and call-site wiring — same as 3c-A Step 5 pattern)
**File:** Modify `src/Services/Fokus/Fokus.API/Grpc/SprintAnalyticsGrpcService.cs`

After Steps 3 and 7, the two services are gone. Retire the gRPC mixed state.

- **Constructor:** remove `ScopeChangeService` + `CarryOverService`; add `ScopeChangeCalculator` + `CarryOverCalculator`.
- **`GetDisruptionMetricsAsync`:** `scopeChangeCalculator.ComputeSingleSprint(...)` → `ScopeChangeSingleSprintData`. Update `MapToDisruptionMetrics(ScopeChangeSingleSprintData result)`. `result.Metrics` is `ScopeChangeSingleSprintMetricsData`; `result.Metrics.DisruptionRate` / `.AddedSp` are now `MetricCardData` — `.Value` access unchanged. `result.Events` is `List<ScopeChangeEventData>`; `.Action` / `.IsExcluded` unchanged. **Preserve the existing `e.Action == "Added"` filter verbatim** — note the latent case mismatch (`BuildEventTable` writes lowercase `"added"`) but DO NOT fix it in this refactoring pass; record as a known-issue deviation in `implementation.md`.
- **`GetCarryOverHistoryAsync`:** `carryOverCalculator.ComputeMultiSprint(...)` → `CarryOverMultiSprintData`. Update `MapToCarryOverHistory(CarryOverMultiSprintData result, …)`. The renamed nested domain types are accessed by these exact fields (Finding 4 — enumerate, don't assume):
  - `result.PerSprintData` → `List<CarryOverPerSprintMetricsData>`; accessed: `.LastOrDefault()?.CarryOverRate`, `.CarryOverSp`, and the `.Zip(result.Sprints, …).CarryOverRate` projection.
  - `result.Sprints` → `List<CarryOverSprintData>`; accessed: `.Name` (in the `Zip`).
  - `result.ZombieTickets` → `List<CarryOverZombieData>`; accessed: `.Count`.
  These field names are unchanged by the rename — only the *type* names change; verify each access compiles against the new record names.
- **Usings:** **REMOVE** (not "verify") `using Fokus.API.Features.Analytics;` and `using Fokus.API.Features.Analytics.GetCarryOver;` (Finding 11). The first was for the old root-namespace service/response types; the second is currently stale BUT becomes load-bearing the moment Step 3 moves `CarryOverMultiSprintResponse` into the `…GetCarryOver` sub-namespace — and the gRPC mapping now consumes **domain** types (`CarryOverMultiSprintData` via the existing `using Fokus.Domain.Analytics;`), so neither API using is needed. Removing them also prevents an accidental wire-type reference. Leave the `FokusSprintInfo` alias (`= …GetSprintSummary.SprintInfo`) untouched — it is a 3c-A artifact unrelated to 3c-B.

**Acceptance:** build green; gRPC constructor uses the 2 calculators; no `ScopeChangeService`/`CarryOverService` anywhere; `DisruptionMetrics` + `CarryOverHistory` contracts unchanged.

**Confidence:** medium — the `Action == "Added"` filter string must be preserved exactly; the domain field-name parity must be verified against the Step 3/7 rename tables.

---

## Step 9 — Delete 5 old API-layer service files

**Skill:** None

**Before deleting**, grep:
- `ScopeChangeService`, `CarryOverService`, `BugRatioService`, `CycleTimeService`, `LeaderboardService` — **zero hits** across the whole solution.
- `ScopeMetricCard` (Finding 10 — precise wording): **zero hits outside `GetScopeChangeResponse.cs` and `GetCarryOverResponse.cs`.** After migration the type legitimately exists in those TWO endpoint sub-namespaces (`…GetScopeChange` and `…GetCarryOver`); it must NOT appear anywhere else — in particular not in the old root `Fokus.API.Features.Analytics` namespace, the gRPC service, or any domain file.
- `BugRatioMetricCard` — zero hits outside `GetBugRatioResponse.cs`; `CycleTimeMetricCard` — zero hits outside `GetCycleTimeResponse.cs`.

**Delete:** `CarryOverService.cs`, `ScopeChangeService.cs`, `CycleTimeService.cs`, `BugRatioService.cs`, `LeaderboardService.cs` (all under `Fokus.API/Features/Analytics/`).

**Acceptance:** build green after deletion; no compilation errors.

---

## Step 10 — Update DI registration

**Skill:** None
**File:** Modify `src/Services/Fokus/Fokus.API/DependencyInjection.cs`

Remove the 5 `AddScoped<…Service>()` lines (`ScopeChangeService`, `CarryOverService`, `BugRatioService`, `CycleTimeService`, `LeaderboardService`). Add the 5 `AddScoped<…Calculator>()` lines in the same block. `MetricCardData`/`MetricCardBuilder` need no registration (record + static class). `using Fokus.Domain.Analytics;` already present.

**Acceptance:** 5 calculators registered; 5 services gone; build green.

---

## Step 11 — Verification sweep

**Skill:** None

1. `dotnet build SprintRituals.slnx` — clean (zero errors; no new warnings).
2. Grep `ScopeChangeService|CarryOverService|BugRatioService|CycleTimeService|LeaderboardService` — zero hits.
3. Grep each calculator name in `Fokus.Domain/Analytics/` — 5 present; in `DependencyInjection.cs` — 5 registered.
4. Grep `MetricCardData` — appears in the shared file + all 4 card-producing calculators + their result files; `MetricCardBuilder` — appears in the shared file + all 7 consumers (5 3c-B calculators + 2 3c-A calculators).
5. Grep `BuildMetricCard|BuildCard|DeltaDirection|DeltaPolarity` private helpers in the deleted services — zero (all collapsed to `MetricCardBuilder`).
6. gRPC mixed state retired: `ScopeChangeService`/`CarryOverService` zero hits in `SprintAnalyticsGrpcService.cs`.
7. Grep `ScopeMetricCard` — only in `GetScopeChangeResponse.cs` + `GetCarryOverResponse.cs` namespaces; zero in root `Fokus.API.Features.Analytics`.
8. Manual smoke test — all 7 affected endpoints (`carry-over`, `scope-change`, `cycle-time`, `bug-ratio`, `leaderboard`, **plus 3c-A `sprint-summary` + `qa-metrics`** to confirm the Step 2 builder refactor changed nothing) return HTTP 200 with byte-identical shapes.

**Acceptance:** all checks pass; no behavioral change; gRPC mixed state retired; shared metric-card primitives in place.

---

## Skill Mapping

| Step | Skill | Disposition | Note |
|------|-------|-------------|------|
| 1 — MetricCardData + MetricCardBuilder | None | None | New shared domain primitives; no skill covers a cross-calculator value record + extracted kernel. Gap candidate: a "shared-domain-primitive extraction" pattern. |
| 2 — 3c-A builder refactor | None | None | Internal helper delegation; no card-record/wire change |
| 3 — CarryOverCalculator | `analytics-computation-service` | Follow | |
| 4 — CycleTimeCalculator | `analytics-computation-service` | Follow | |
| 5 — BugRatioCalculator | `analytics-computation-service` | Follow | |
| 6 — LeaderboardCalculator | `analytics-computation-service` | Follow | |
| 7 — ScopeChangeCalculator | `analytics-computation-service` | Follow | |
| 8 — gRPC adaptation | None | None | Constructor/call-site wiring; 3c-A Step 5 pattern |
| 9 — Delete service files | None | None | File deletion with pre-deletion grep |
| 10 — DI update | None | None | Mechanical registration swap |
| 11 — Verification | None | None | Build + grep + smoke test |

---

## Plan Review

### Critic Mode-2 review — disposition (1 HIGH, 3 MEDIUM, 7 LOW; no CRITICAL)

Critic verdict: GAPS FOUND, all four decision checks PASSED (5 calculators correct; Q2/Q5 sharing faithfully applied with the 2 sparkline-bearing 3c-A records correctly isolated; gRPC correct; no domain wire-name leak). Full findings in `critic-review.md`. All 11 findings folded into the plan:

| # | Sev | Finding | Disposition — folded into |
|---|-----|---------|----------------------------|
| 1 | HIGH | `LeaderboardService.cs:213` `TotalSpDeltaPolarity` is a literal `"neutral"`, not a `DeltaPolarity()` call; blanket replace would change wire output | **Fixed** — Step 6 now carves out the literal explicitly (keep `"neutral"` verbatim; only the 4 genuine `DeltaPolarity(...)` fields convert); Step 1 bool-path note reinforces it |
| 2 | MED | String-polarity (`"neutral"` short-circuit) must stay distinct from bool-polarity (`positiveUp`) path | **Fixed** — Step 1 `MetricCardBuilder` spec now defines two explicit paths; `"neutral"` string input short-circuits and must not route through the bool path |
| 3 | MED | 5 mode-wrapper envelopes must be wire-only (endpoint builds `{Mode, Multi?, Single?}`; domain returns shapes separately) | **Fixed** — new global "Mode-wrapper envelopes are WIRE-ONLY" section + each migration step (3–7) now states the endpoint builds the envelope; envelopes added to wire-file lists, omitted from domain rename tables by design |
| 4 | MED | `MapToCarryOverHistory` accessed fields not enumerated | **Fixed** — Step 8 now enumerates the exact accessed fields (`.CarryOverRate`, `.CarryOverSp`, `.Name`, `.Count`) on the renamed types |
| 5 | LOW | `ScopeChangeResponse` missing from wire-file list | **Fixed** — added to Step 7 wire file |
| 6 | LOW | `BugRatioResponse` missing from wire-file list | **Fixed** — added to Step 5 wire file |
| 7 | LOW | `CycleTimeResponse` missing from wire-file list | **Fixed** — added to Step 4 wire file |
| 8 | LOW | `LeaderboardResponse` missing from wire-file list | **Fixed** — added to Step 6 wire file + rename table footnote |
| 9 | LOW | `CarryOverPerSprintMetricsData → CarryOverPerSprintData` wire-name collision not called out | **Fixed** — Step 3 now has an explicit name-collision note |
| 10 | LOW | Step 9 `ScopeMetricCard` grep wording imprecise | **Fixed** — reworded to "zero hits outside `GetScopeChangeResponse.cs` and `GetCarryOverResponse.cs`" |
| 11 | LOW | Stale `using …GetCarryOver;` in gRPC service: "verify" → should be "remove" | **Fixed** — Step 8 now says REMOVE both API usings, with the rationale (the stale using becomes load-bearing after Step 3) |

No finding required a re-architecture; all four critic decision checks passed. Plan approved.

### Self-check baseline (pre-critic)

Self-checked against ADR-005/010 + the Q2/Q5 maximize-sharing decision + the shape-compatibility finding:

- **ADR-005 / ADR-010 / ADR-006:** Steps 3–7 move all 3c-B computation to the domain; wire records isolated in `*Response.cs`; domain results use only primitive/domain types.
- **Q2/Q5 maximize-sharing:** Step 1 creates the shared `MetricCardData` + `MetricCardBuilder`; the 4 card-producing 3c-B calculators return the shared record; all 7 calculators (incl. 3c-A) consume the shared kernel (Step 2). The single documented isolation exception is `SprintMetricCardData`/`QaMetricCardData` — justified by the `Sparkline`+`Rag` divergence (and the two sparkline types differing from each other), which would lose data if collapsed. The isolated types still share the kernel via their refactored builders.
- **3c-A blast radius:** Bounded. Because the isolated 3c-A card *records* are unchanged, the 3c-A endpoints and the gRPC `MapToAnonymizedSummary` need no signature changes — only the two 3c-A calculator builders are touched (Step 2). Step 11.8 smoke-tests the 3c-A endpoints to confirm zero behavior change.
- **Wire stability:** every endpoint keeps its public wire card name (`ScopeMetricCard`, `BugRatioMetricCard`, `CycleTimeMetricCard`) and maps `MetricCardData → {wire}` via Mapster. The 7 HTTP contracts + 3 gRPC contracts are unchanged.

**Risks flagged for the critic:**
1. **`MetricCardBuilder` must cover every call shape** — delta-based (3c-B + SprintSummary), `priorValue`-derived hardcoded-`positive-up` (QaMetrics), and int-overload (`BugRatio`/`Leaderboard`). If the kernel diverges on any one, a metric's direction/polarity flips silently. Step 1 + Step 2 acceptance pins this.
2. **`MetricCardData → {wire}MetricCard` Mapster maps** are the wire-stability lynchpin for 4 endpoints; nested-rename maps need explicit entries (follow 3c-A `GetSprintSummary`).
3. **gRPC `Action == "Added"` latent case bug** — preserved verbatim, NOT fixed (Step 8); flagged so the critic does not treat it as a regression.
4. **Step count = 11** (>10). Per the auto-approve gate, splitting was considered: Steps 1–2 (shared types) + Steps 3–11 (migration) could split, but Steps 3–11 share the wire-stability invariant and the gRPC step depends on Steps 3+7 within the same arm; splitting would put the shared-type foundation in a separate run from its only consumers, breaking the build between runs. **Kept whole** — a split would produce a non-building intermediate state.
