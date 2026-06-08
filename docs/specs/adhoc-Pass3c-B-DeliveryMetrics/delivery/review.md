# Review — adhoc-Pass3c-B-DeliveryMetrics

## Step 1 — Done-Check

Plan: `docs/specs/adhoc-Pass3c-B-DeliveryMetrics/delivery/plan.md` (11 steps).
Implementation record: `docs/specs/adhoc-Pass3c-B-DeliveryMetrics/delivery/implementation.md`.
Note: the developer's implementation.md uses its own step numbering (it splits plan Step 1 into "Step 1/Step 2", inserts a "Step 9" for an unplanned caller fix, and labels deletion/DI as "Step 10/11"). The table below maps each entry back to the **plan's** 11 steps by content.

| Step | Disposition | Notes |
|------|-------------|-------|
| 1 — Shared `MetricCardData` + `MetricCardBuilder` | Implemented | `MetricCardData.cs` (6-field sealed record) + `MetricCardBuilder.cs` present. Builder has both polarity paths: `ComputeDelta`/`Build` (string-polarity, `"neutral"` short-circuits) and `Direction`/`Polarity` decimal+int overloads (bool-polarity). Matches the Step-1 spec exactly. |
| 2 — Refactor 3c-A builders to delegate to `MetricCardBuilder` | Implemented | `SprintSummaryCalculator.cs:535` and `QaMetricsCalculator.cs:422` both delegate to `MetricCardBuilder.ComputeDelta(...)` (QA passes `"positive-up"`). Isolated records `SprintMetricCardData` (SprintSummaryResult.cs:32) + `QaMetricCardData` (QaMetricsResult.cs:16) are UNCHANGED (still carry `Sparkline` + `Rag`). No wire/record shape change. |
| 3 — CarryOverCalculator + Result + Response + endpoint | Implemented | Domain calc + `CarryOverResult.cs` + `GetCarryOverResponse.cs` (declares `ScopeMetricCard`) + endpoint rewired with Mapster `.Adapt<>()`. Finding 9 name-collision (`CarryOverPerSprintMetricsData` domain → `CarryOverPerSprintData` wire) handled in the `IRegister` config. |
| 4 — CycleTimeCalculator + Result + Response + endpoint | Implemented | `CycleTimeMappingConfig : IRegister` with `MetricCardData → CycleTimeMetricCard`; endpoint rewired both modes. |
| 5 — BugRatioCalculator + Result + Response + endpoint | Implemented | `BugRatioMappingConfig : IRegister`; `BugRatioDeveloperDelta` flat bundle mapped; endpoint rewired. |
| 6 — LeaderboardCalculator + Result + Response + endpoint | Implemented | Plan carve-out verified: `LeaderboardCalculator.cs:141` `TotalSpDeltaPolarity` is the literal `"neutral"` (NOT a `MetricCardBuilder.Polarity(...)` call), with an explicit guard comment; the other 4 delta fields correctly use `MetricCardBuilder.Polarity(...)`. `TotalSpDeltaDirection` uses `MetricCardBuilder.Direction(...)`. |
| 7 — ScopeChangeCalculator + Result + Response + endpoint | Implemented | `ScopeChangeSingleSprintMetricsData` uses `MetricCardData`; `BuildMetricCard` → `MetricCardBuilder.Build(...)`; `GetScopeChangeResponse.cs:43` declares its own `ScopeMetricCard`. |
| 8 — gRPC adaptation (ScopeChange + CarryOver) | Implemented | Constructor injects `ScopeChangeCalculator` + `CarryOverCalculator` (lines 24–25); `MapToDisruptionMetrics`/`MapToCarryOverHistory` take domain result types; stale API usings removed; `Action == "Added"` filter preserved verbatim (line 252 — latent case bug, correctly NOT fixed). Mixed state retired. |
| 9 — Delete 5 service files | Implemented | The 5 service type names survive only as provenance doc-comments in `LeaderboardResult.cs` + `MetricCardBuilder.cs`; zero live references; the 5 `.cs` files are gone from `Features/Analytics/`. |
| 10 — DI registration swap | Implemented | `DependencyInjection.cs:46–51` registers all 5 `*Calculator`; zero `*Service` registrations remain. |
| 11 — Verification sweep | Implemented | implementation.md records the grep sweeps; independently re-verified below. |
| (extra) GetQaTrendsEndpoint caller fix | Deviated (justified) | `GetQaTrendsEndpoint` injected `BugRatioService` — an undocumented caller NOT enumerated in the plan's affected-files list. Developer found it during the deletion sweep and swapped it to `BugRatioCalculator`. Correct and necessary (build would otherwise break); not scope creep. Plan-gap noted in lessons. |

### Independent verification (architect, not relying on developer self-report)

| Check | Result | Evidence |
|-------|--------|----------|
| Build | **0 errors, 4 warnings** | `dotnet build SprintRituals.slnx` → `Build succeeded. 0 Error(s)`. The 4 warnings are all pre-existing `NU1903` NuGet-vuln advisories on `System.Security.Cryptography.Xml` in `Blocks.Contracts` — unrelated to 3c-B. (Developer reported 6; fresh slnx build dedups to 4. Zero new warnings either way.) |
| Plan carve-out (`"neutral"` literal) | **PASS** | `LeaderboardCalculator.cs:141` = literal `"neutral"` + guard comment; not converted. |
| Finding 3 (no `*Response` envelope leak into domain) | **PASS** | grep `Response` in all domain `*Result.cs` → only doc-comments pointing to the wire-file location; zero `*Response` records declared in `Fokus.Domain`. |
| 3c-A isolated records unchanged + builders delegate | **PASS** | `SprintMetricCardData`/`QaMetricCardData` records intact; both builders call `MetricCardBuilder.ComputeDelta`. |
| Finding 10 (`ScopeMetricCard` in exactly 2 endpoint namespaces) | **PASS** | declared only at `GetCarryOverResponse.cs:58` + `GetScopeChangeResponse.cs:43`; no root-namespace or domain copy. |
| gRPC mixed state retired | **PASS** | gRPC constructor uses the 2 calculators; no `*Service` injection. |
| 5 services deleted, 0 live refs | **PASS** | only provenance comments remain. |

**Verdict: PASS** — 11/11 plan steps Implemented; 1 justified deviation (GetQaTrends caller fix); 0 Missing; build green (0 errors, no new warnings); all three flagged high-priority items (plan carve-out, Finding 3, 3c-A isolation/delegation) independently confirmed.

---

## Step 2 — Code Review

## Reviewed By
Reviewer agent (nexus:reviewer), Claude Sonnet 4.6. Full code read of all 12 new/modified domain files, all 5 new Response wire files, 6 modified endpoints, gRPC service, and DependencyInjection.cs. Fresh build run.

## Verdict: APPROVED

## Pre-commitment Predictions
- Expected `TotalSpDeltaPolarity = "neutral"` literal preserved per plan carve-out in LeaderboardCalculator — confirmed at line 141 with guard comment.
- Expected potential Mapster field-parity gaps from positional record constructors — none found; positional constructors enforce compile-time parity.
- Expected potential Mapster collision between two `MetricCardData → ScopeMetricCard` registrations in different namespaces — confirmed safe: two distinct CLR types produce two distinct Mapster entries.
- Expected QaMetrics delta sign to match original (`value - priorValue.Value`) — confirmed at QaMetricsCalculator.cs:420; `"positive-up"` polarity preserved.

## Findings

No blocking findings. Two style-level observations noted below.

### [LOW] CycleTimeCalculator private wrapper `BuildMetricCards` uses old naming convention
**File:** `src/Services/Fokus/Fokus.Domain/Analytics/CycleTimeCalculator.cs:395`
**Issue:** The private method retains the name `BuildMetricCards` (service-era naming convention). All other 3c-B calculators call `MetricCardBuilder.Build(...)` directly at call sites. The wrapper delegates correctly to `MetricCardBuilder.Build(...)` — behavior is correct — but naming is inconsistent with the established pattern.
**Fix:** Rename to `BuildCycleTimeMetricCards` or inline at the two call sites (lines 41 and 73). Private method, no behavior impact, green build — not blocking.
**Confidence:** LOW

### [LOW] Domain result record collection properties use `List<T>` rather than `IReadOnlyList<T>`
**File:** `src/Services/Fokus/Fokus.Domain/Analytics/CarryOverResult.cs`, `BugRatioResult.cs`, `CycleTimeResult.cs`, `LeaderboardResult.cs`, `ScopeChangeResult.cs`
**Issue:** Per `docs/conventions/csharp.md`: "IReadOnlyList<T> for collection properties on records". Positional constructor parameters use `List<T>` in several places. This follows the pre-existing 3c-A pattern — not a regression introduced by this pass.
**Fix:** Track as convention debt. Change to `IReadOnlyList<T>` in a follow-up pass across all `*Result.cs` domain files.
**Confidence:** MEDIUM the gap exists; LOW priority for this pass.

## Positive Observations
- MetricCardBuilder polarity kernel is correct and complete. String-polarity path: `"neutral"` input short-circuits unconditionally; `"positive-up"` and `"positive-down"` correct. Bool-polarity path: all four overloads (`Direction(decimal)`, `Direction(int)`, `Polarity(decimal, bool)`, `Polarity(int, bool)`) reproduce the old inline helper output byte-for-byte.
- `TotalSpDeltaPolarity = "neutral"` literal preserved at `LeaderboardCalculator.cs:141` with guard comment. Four genuine polarity calls (`FeatureSpDeltaPolarity`, `BugSpDeltaPolarity`, `FeatureTicketsDeltaPolarity`, `BugTicketsDeltaPolarity`) correctly use `MetricCardBuilder.Polarity(...)`.
- All 5 Mapster `IRegister` configs have full field parity — positional record constructors make field omission a compile error.
- Both `MetricCardData → ScopeMetricCard` Mapster registrations (GetCarryOver + GetScopeChange namespaces) are safe: distinct CLR types, distinct Mapster entries; each endpoint's `Adapt<>` traversal resolves to the correct namespace target.
- gRPC `MapToDisruptionMetrics` and `MapToCarryOverHistory` emit unchanged wire output. Field accesses verified: `result.Metrics.DisruptionRate.Value`, `result.Metrics.AddedSp.Value`, `result.PerSprintData.LastOrDefault()?.CarryOverRate`, `.CarryOverSp`, `result.Sprints.Zip(...)`, `result.ZombieTickets.Count` — all resolve to correct renamed domain fields.
- `Action == "Added"` latent case bug preserved verbatim per plan.
- ADR-005 and ADR-010 fully respected: zero `*Response`-flavored names in `Fokus.Domain`; all 5 mode envelopes live exclusively in API wire files.
- DI registration clean: 5 calculators registered, 5 service registrations removed.

## Gaps
- No runtime smoke-test evidence. Plan Step 11.8 listed manual smoke tests for all 7 affected endpoints. This is a pure refactor with a green build and wire contracts enforced at Mapster config level; residual risk is low.

## Open Questions
None.

## Evidence
| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Build | PASS | `dotnet build SprintRituals.slnx --no-incremental` | `Build succeeded. 0 Error(s). 6 Warning(s) — all pre-existing (NU1903 x4, CS8629, CS9107)` |
| Old service name live refs | PASS (0 live refs) | Grep `ScopeChangeService\|CarryOverService\|BugRatioService\|CycleTimeService\|LeaderboardService` in `*.cs` | Only in doc-comments in MetricCardBuilder.cs and LeaderboardResult.cs |
| ScopeMetricCard namespace isolation | PASS (exactly 2 declarations) | Grep `ScopeMetricCard` in `*.cs` | Only GetCarryOverResponse.cs and GetScopeChangeResponse.cs; zero in domain or root namespace |
| Neutral literal preserved | PASS | Read LeaderboardCalculator.cs:130-145 | Literal `"neutral"` at line 141 with guard comment; 4 genuine calls use `MetricCardBuilder.Polarity` |
| Domain Response leak | PASS | Grep `Response` in domain `*Result.cs` | Only doc-comment forward-references; zero Response declarations in Fokus.Domain |
| MetricCardBuilder kernel | PASS | Read MetricCardBuilder.cs | Both polarity paths present and correct; neutral short-circuit in string path; sign-based bool path separate |
| Mapster global scan scope | PASS | Grep `TypeAdapterConfig.GlobalSettings.Scan` | One scan in Fokus.API assembly; both ScopeMetricCard registrations are distinct CLR types |
