# Critic Review — adhoc-Pass3c-B-DeliveryMetrics (Mode 2: plan vs scope/decisions)

> Note: Written to a file only because inline message delivery was reported failing. The critic normally returns findings by message; the architect/PO should fold these into `plan.md` (`## Plan Review`) and may delete this file afterward.

Reviewed: `docs/specs/adhoc-Pass3c-B-DeliveryMetrics/delivery/plan.md`
Authoritative sources cross-referenced: `docs/backlog.md` (3c-B row + sub-split note), `questions.md` (Q1–Q6), `docs/architecture/decisions.md` (ADR-005/006/010), the 3c-A artifacts, and the live source files under `Fokus.API/Features/Analytics/` and `Fokus.Domain/Analytics/`.

---

## 1. Verdict

**GAPS FOUND** — 1 HIGH, 3 MEDIUM, 7 LOW; no CRITICAL. The plan is fundamentally sound and faithfully applies the Q2/Q5 sharing decision, ADR-005/010, the gRPC adaptation, and the no-wire-name-in-domain rule. The gaps are correctness-edge and completeness items to fold in before developer hand-off — not a re-architecture.

---

## 2. Findings table

| # | Severity | Plan section / step | Issue |
|---|----------|---------------------|-------|
| 1 | **HIGH** | Steps 1 + 6 | `LeaderboardService.cs` line 213 hard-codes the literal `"neutral"` for `TotalSpDeltaPolarity` (it is NOT a `DeltaPolarity()` call). Step 6's blanket "replace all `DeltaPolarity` → `MetricCardBuilder.Polarity`" would convert this literal to a computed value and silently change wire output for non-flat total-SP deltas. Must carve out the literal. |
| 2 | MEDIUM | Step 1 | Plan does not separate the string-polarity path (`Build(…, "positive-down")` switch) from the bool-polarity path (`Polarity(delta, positiveUp)` branch). The `"neutral"` string input (used by ScopeChange for CommittedSpActive/Total/Removed/Net) must always yield `"neutral"` regardless of delta and must not be conflated with `positiveUp:false`. Spell out that the two paths are distinct and that `"neutral"` input short-circuits. |
| 3 | MEDIUM | Steps 3–7 | The five mode-wrapper envelopes (`CarryOverResponse`, `ScopeChangeResponse`, `BugRatioResponse`, `CycleTimeResponse`, `LeaderboardResponse`) are absent from the domain rename tables and inconsistently listed in the wire-file record lists. Plan must state they stay wire-only — the endpoint builds the `{Mode, Multi?, Single?}` envelope; the domain returns the two shapes separately. Risks both a build break (undeclared wrapper) and a domain leak if mirrored. |
| 4 | MEDIUM | Step 8 | `MapToCarryOverHistory` field-parity across renamed nested types (`CarryOverPerSprintMetricsData`, `CarryOverSprintData`, `CarryOverZombieData`) is asserted "all field names unchanged" without enumerating the actually-accessed fields (`.CarryOverRate`, `.CarryOverSp`, `.Name`, `.Count`). Enumerate to de-risk. |
| 5 | LOW | Step 7 | `ScopeChangeResponse` wrapper missing from the `GetScopeChangeResponse.cs` record list (source lines 94–97; endpoint returns it). |
| 6 | LOW | Step 5 | `BugRatioResponse` wrapper missing from the `GetBugRatioResponse.cs` record list (source lines 114–117). |
| 7 | LOW | Step 4 | `CycleTimeResponse` wrapper missing from the `GetCycleTimeResponse.cs` record list (source lines 94–97). |
| 8 | LOW | Step 6 | `LeaderboardResponse` wrapper missing from the `GetLeaderboardResponse.cs` record list (source lines 70–73). |
| 9 | LOW | Step 3 | The nested map `CarryOverPerSprintMetricsData` (domain) → `CarryOverPerSprintData` (wire) is not called out; the wire name collides with the OLD domain type name and needs an explicit note so the developer isn't confused by the reused identifier. |
| 10 | LOW | Step 9 | The `ScopeMetricCard` "zero in root namespace" grep needs tighter wording: after migration it legitimately exists in TWO endpoint sub-namespaces (`GetScopeChange` + `GetCarryOver`). Reword to "zero hits outside `GetScopeChangeResponse.cs` and `GetCarryOverResponse.cs`." |
| 11 | LOW | Step 8 | `using Fokus.API.Features.Analytics.GetCarryOver;` in the gRPC service is currently stale (resolves nothing) but becomes load-bearing the moment Step 3 moves `CarryOverMultiSprintResponse` into that sub-namespace. Since the gRPC mapping now consumes DOMAIN types (`CarryOverMultiSprintData` via `using Fokus.Domain.Analytics`), this using should be explicitly REMOVED — plan only says "verify." |

---

## 3. Decision checks

### (a) All 5 calculators present & ADR-005/010-correct — ✓ CONFIRMED
CarryOver (Step 3), CycleTime (Step 4), BugRatio (Step 5), Leaderboard (Step 6), ScopeChange (Step 7) each create a `*Calculator` + `*Result.cs` in `Fokus.Domain/Analytics/` returning `*Data` domain records, with the endpoint owning the `*Response.cs` wire file and a Mapster boundary map. Plan create-table source line ranges match the actual files exactly (ScopeChange 862, CarryOver 752, CycleTime 814, BugRatio 562, Leaderboard 328). ADR-006 honored — domain records use only primitive/domain types.

### (b) Shared `MetricCardData`/`MetricCardBuilder` (Q2/Q5) faithfully applied + 3c-A usages updated after collapse — ✓ CONFIRMED, WITH CAVEATS
- Step 1 creates the single shared `MetricCardData` (6 base fields, byte-verified identical across all 5 cards) + the shared `MetricCardBuilder` kernel; the 4 card-producing 3c-B calculators return the shared record.
- The isolation of `SprintMetricCardData`/`QaMetricCardData` is correctly justified by the divergent `Sparkline` (`List<SprintSparklinePoint>` vs `IReadOnlyList<QaSparklinePoint>`) + `Rag`, verified against live 3c-A source (`SprintSummaryResult.cs` lines 32–42, `QaMetricsResult.cs` lines 16–24).
- 3c-A usage-after-collapse is correctly BOUNDED: because the two isolated records are UNCHANGED, the 3c-A endpoints and the gRPC `MapToAnonymizedSummary` need NO signature changes — only the two private builders delegate to `MetricCardBuilder.ComputeDelta` (Step 2). This is the correct reading of "update all existing 3c-A usages after collapse": the collapse does not reach the isolated records, so there are no downstream signature updates owed beyond the two builders.
- CAVEAT = Findings 1 & 2: the builder must pin the hard-coded `"neutral"` literal and the string-vs-bool polarity paths, or a metric's polarity flips silently.

### (c) gRPC adaptation covers ScopeChange + CarryOver, wire contracts unchanged — ✓ CONFIRMED
Step 8 swaps `ScopeChangeService`+`CarryOverService` for the two calculators in the gRPC constructor, reworks `MapToDisruptionMetrics(ScopeChangeSingleSprintData)` and `MapToCarryOverHistory(CarryOverMultiSprintData)`, retires the mixed state, and preserves `DisruptionMetrics` / `CarryOverHistory` / `AnonymizedSprintSummary` byte-for-byte. The `e.Action == "Added"` latent case-mismatch bug is correctly preserved verbatim and flagged as a known-issue deviation rather than fixed. Q4 honored — CycleTime/BugRatio/Leaderboard are endpoint-only, no gRPC touch.

### (d) No wire-flavored names leak into the domain — ✓ CONFIRMED
Every rename table converts `*Response`/`*Entry`/`*Summary`/`*MetricCard` to `*Data` domain names; `ScopeMetricCard`/`BugRatioMetricCard`/`CycleTimeMetricCard` collapse to `MetricCardData` in the domain and survive only as wire records in their endpoint namespaces. The one residual concern is the five mode-wrapper envelopes (Finding 3) — if the developer mistakenly mirrors `*Response` in the domain, that WOULD leak a wire name; the plan should pre-empt by stating the envelopes are wire-only.

---

## Recommendation
Fold Finding 1 (HIGH) and Finding 3 (MEDIUM) into `plan.md` explicitly before developer hand-off — Finding 1 is a silent wire-output regression risk; Finding 3 risks a build break and a potential domain leak. Tighten Findings 2 and 4 as developer guardrails. The 7 LOW items are completeness/wording polish.
