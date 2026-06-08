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
| 6 — LeaderboardCalculator + Result + Response + endpoint | Implemented | **HIGH Finding 1 verified preserved:** `LeaderboardCalculator.cs:141` `TotalSpDeltaPolarity` is the literal `"neutral"` (NOT a `MetricCardBuilder.Polarity(...)` call), with an explicit guard comment; the other 4 delta fields correctly use `MetricCardBuilder.Polarity(...)`. `TotalSpDeltaDirection` uses `MetricCardBuilder.Direction(...)`. |
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
| HIGH Finding 1 (`"neutral"` literal) | **PASS** | `LeaderboardCalculator.cs:141` = literal `"neutral"` + guard comment; not converted. |
| Finding 3 (no `*Response` envelope leak into domain) | **PASS** | grep `Response` in all domain `*Result.cs` → only doc-comments pointing to the wire-file location; zero `*Response` records declared in `Fokus.Domain`. |
| 3c-A isolated records unchanged + builders delegate | **PASS** | `SprintMetricCardData`/`QaMetricCardData` records intact; both builders call `MetricCardBuilder.ComputeDelta`. |
| Finding 10 (`ScopeMetricCard` in exactly 2 endpoint namespaces) | **PASS** | declared only at `GetCarryOverResponse.cs:58` + `GetScopeChangeResponse.cs:43`; no root-namespace or domain copy. |
| gRPC mixed state retired | **PASS** | gRPC constructor uses the 2 calculators; no `*Service` injection. |
| 5 services deleted, 0 live refs | **PASS** | only provenance comments remain. |

**Verdict: PASS** — 11/11 plan steps Implemented; 1 justified deviation (GetQaTrends caller fix); 0 Missing; build green (0 errors, no new warnings); all three flagged high-priority items (HIGH Finding 1, Finding 3, 3c-A isolation/delegation) independently confirmed.
