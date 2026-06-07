# Review — adhoc-Pass3c-ComputationToDomain (3c-A)

## Step 1 — Done-Check

Performed by architect against committed code (commit 4e58c18). Pre-commitment predictions: (1) gRPC `FokusSprintInfo` alias + `QaBreakdownData` substitution in `LoadQaDataAsync`; (2) shared `MetricCard` transitional mapping in `GetQaMetricsEndpoint`; (3) wire/gRPC byte-stability drift during rename. All three verified clean.

| Step | Disposition | Notes |
|------|-------------|-------|
| 1 — Relocate 4 static helpers to Fokus.Domain/Analytics/ | Implemented | All 4 (`HealthScoreCalculator`, `TransitionAttributionChecker`, `CompletionChecker`, `ExcludedDeveloperFilter`) present in `Fokus.Domain/Analytics/`, namespace `Fokus.Domain.Analytics`. Deviation (documented): `Fokus.Domain/GlobalUsings.cs` was missing `global using Fokus.Domain;` — added so domain files resolve their own types. Valid; no behavioral impact. |
| 2 — TestTimelineCalculator + TestTimelineResult.cs + GetTestTimelineEndpoint | Implemented | `TestTimelineCalculator.ComputeTimeline` returns `TestTimelineData`; `static ComputeCrunchFlag` returns `TestingCrunchFlag?`. Wire types in `GetTestTimelineResponse.cs` are byte-for-byte identical to originals; Mapster `TestTimelineMap : IRegister` registers all 11 nested maps. Endpoint uses `result.Adapt<TestTimelineResponse>()` (config scanned at DI startup, line 25 of DependencyInjection.cs). `BuildEmptyResponse` rebuilt with wire types — shape unchanged. |
| 3 — QaMetricsCalculator + QaMetricsResult.cs + GetQaMetricsEndpoint | Implemented | `ComputeQaMetrics` returns `QaMetricsData`. Domain-owned `QaMetricCardData`/`QaSparklinePoint` avoid API-layer `MetricCard` coupling. `MapResponse(QaMetricsData)` bridges `QaMetricCardData → MetricCard` wire type via `MapMetricCard` helper. `QaMetricsResponse`/`QualityBreakdownResponse` wire shapes byte-for-byte identical. |
| 4 — SprintSummaryCalculator + SprintSummaryResult.cs + GetSprintSummaryEndpoint | Implemented | `ComputeSummary` returns `SprintSummaryData`, accepts domain `QaBreakdownData?` + `TestingCrunchFlag?`. Wire types added to `GetSprintSummaryQuery.cs` — all records byte-for-byte identical to originals (`SprintSummaryResponse`, `MetricCard`, `FlagsResult` with `TestingCrunchResult?` carrying same 3 fields/names/types as old `TestingCrunchFlag?`). TestingCrunch wiring intact: endpoint computes via `TestTimelineCalculator.ComputeCrunchFlag` (line 122) and passes to `ComputeSummary` (line 133). |
| 5 — Adapt SprintAnalyticsGrpcService | Implemented | `SprintSummaryCalculator` + `QaMetricsCalculator` injected; `ScopeChangeService` + `CarryOverService` retained (mixed state, intentional until 3c-B). `FokusSprintInfo` alias updated to `...GetSprintSummary.SprintInfo`. `LoadQaDataAsync` returns `QaBreakdownData?` and reads domain `qaResult.QualityBreakdown` directly. `MapToAnonymizedSummary(SprintSummaryData)` — output `AnonymizedSprintSummary` contract unchanged. `ExcludedDeveloperFilter` fully qualified to resolve CS0104. |
| 6 — Update DI registration | Implemented | 3 old service registrations removed; 3 calculator registrations added (lines 42-44); `using Fokus.Domain.Analytics;` added. Grep confirms no `AddScoped` of old services survives. |
| 7 — Delete 7 old API-layer files | Implemented | All 7 deleted (grep: no `class SprintSummaryService\|QaMetricsService\|TestTimelineService` definitions remain). Deviation (documented): plan's pre-delete grep not run first; deletion exposed `MetricCard`/`SparklinePoint` + static-helper references in 22 3c-B/C-scope files, fixed by adding `using` directives. Valid — these are transitional dependencies that 3c-B/C will migrate; no behavior changed, build green. |
| 8 — Update CLAUDE.md | Implemented | `src/Services/Fokus/CLAUDE.md` "Computed metrics" bullet now describes ADR-005/010 domain-calculator pattern. |
| 9 — Verification sweep | Implemented | `dotnet build SprintRituals.slnx` re-run by architect: Build succeeded, 0 errors. Grep checks confirm clean migration. |

### Build evidence

`dotnet build SprintRituals.slnx` — **Build succeeded, 0 Errors, 7 Warnings.**

Warnings (none introduced as defects by this pass except one benign code-quality item):
- NU1903 ×4 — pre-existing security advisory on `Blocks.Contracts` (`System.Security.Cryptography.Xml`); unrelated to 3c-A.
- CS8629 (Jira.RestApi), CS9107 (DeveloperRepository) — pre-existing, unrelated.
- **CS9113** — `GetSprintSummaryEndpoint.cs:15` parameter `testTimelineCalculator` is unread. Root cause: `ComputeCrunchFlag` is a **static** method, so the injected instance is never consumed (the static call works without it). The crunch-flag computation itself is correct and wired through. Cosmetic only — no behavioral impact. Noted for the reviewer to flag as LOW (remove the unused constructor parameter, call the static method directly).

### Risk-area verification (per done-check note — 4 accepted MEDIUM gaps)

1. **gRPC mixed-state:** PASS. Constructor carries 2 new calculators + 2 unmigrated services. `FokusSprintInfo` alias, `QaBreakdownData` return type, fully-qualified `ExcludedDeveloperFilter` all correct. `MapToDisruptionMetrics`/`MapToCarryOverHistory` still bind to `ScopeChangeSingleSprintResponse`/`CarryOverMultiSprintResponse` (3c-B scope, untouched).
2. **Shared MetricCard wire type across steps:** PASS. `MetricCard`/`SparklinePoint` consolidated in `GetSprintSummaryQuery.cs`; both `GetQaMetricsEndpoint` (via `using` + `MapMetricCard`) and 3c-B/C-scope consumers reference them via `using`. No duplicate definitions (build proves no CS0101).
3. **Wire/gRPC byte-stability:** PASS. All three HTTP response contracts (`SprintSummaryResponse`, `QaMetricsResponse`, `TestTimelineResponse`) and the gRPC `AnonymizedSprintSummary` contract are unchanged in field names, types, nullability, and order.

**Verdict: PASS**

---

## Step 2 — Code Review

## Reviewed By
Reviewer agent (nexus:reviewer), reading committed diff 4e58c18 against plan and conventions, with fresh build run.

## Verdict: APPROVED

## Pre-commitment Predictions

Four MEDIUM-level gaps were flagged by the critic but accepted before commit. Independent re-derivation:

1. **gRPC mixed-state correctness** — Constructor carries migrated calculators plus unmigrated `ScopeChangeService`/`CarryOverService`. Expected: compilable, all three gRPC methods function, no stale type references. Confirmed clean.
2. **Shared MetricCard wire type** — `MetricCard`/`SparklinePoint` moved to `GetSprintSummaryQuery.cs`; 3c-B/C consumers reference via `using`. Expected: single definition, no CS0101. Confirmed clean.
3. **Wire/gRPC byte-stability** — All HTTP response records and `AnonymizedSprintSummary` unchanged in field names, types, nullability, order. Confirmed clean.
4. **`using`-directive fallout in 22 files** — 7 deleted API-layer files expose callers in 3c-B/C scope; all 22 files received the needed `using` directives. Build green confirms.

All four re-derived predictions match the committed code.

## Findings

### [LOW] Unused injected constructor parameter — CS9113 build warning
**File:** `src/Services/Fokus/Fokus.API/Features/Analytics/GetSprintSummary/GetSprintSummaryEndpoint.cs:15`
**Issue:** `TestTimelineCalculator testTimelineCalculator` is declared as a primary constructor parameter and registered via `AddScoped<TestTimelineCalculator>()` in DI, but `ComputeCrunchFlag` is a static method called as `TestTimelineCalculator.ComputeCrunchFlag(...)` (line 122), so the injected instance is never used. The computation is correct and the static dispatch works; the instance is dead weight and produces CS9113.
**Fix:** Remove the `testTimelineCalculator` parameter from `GetSprintSummaryEndpoint`'s constructor. Note: `GetTestTimelineEndpoint` injects `TestTimelineCalculator` as an instance for `ComputeTimeline`, so the `AddScoped` registration must stay — only the `GetSprintSummaryEndpoint` constructor parameter is removed.

### [LOW] Domain result records use `List<T>` instead of `IReadOnlyList<T>` on positional properties
**File:** `src/Services/Fokus/Fokus.Domain/Analytics/SprintSummaryResult.cs:39,75,77,87,88`
**Issue:** Coding conventions (`csharp.md` — Records section): "`IReadOnlyList<T>` for collection properties on records." `SprintMetricCardData.Sparkline`, `SprintFlagsData.ZombieTickets`, `SprintFlagsData.ZeroSpDevelopers`, `SprintSummaryData.TopEpics`, `SprintSummaryData.Leaderboard` are positional `List<T>` properties. `TestTimelineResult.cs` correctly uses `IReadOnlyList<T>` throughout; `QaMetricsResult.cs` correctly uses `IReadOnlyList<T>` for its Sparkline. Inconsistency within the same commit.
**Fix:** Change to `IReadOnlyList<T>` on all five properties. Callers (`SprintSummaryCalculator`) build these via LINQ `.ToList()` which satisfies `IReadOnlyList<T>`; `MapResponse` in the endpoint similarly constructs with `.ToList()`.

### [LOW] Wire record positional properties use `List<T>` instead of `IReadOnlyList<T>`
**File:** `src/Services/Fokus/Fokus.API/Features/Analytics/GetSprintSummary/GetSprintSummaryQuery.cs:65,101,103,118,119`
**Issue:** Same convention applies to wire-layer records. `MetricCard.Sparkline`, `FlagsResult.ZombieTickets`, `FlagsResult.ZeroSpDevelopers`, `SprintSummaryResponse.TopEpics`, `SprintSummaryResponse.Leaderboard` are `List<T>`. The `TestTimelineResponse` and `GetQaMetricsQuery.cs` wire records use `IReadOnlyList<T>` correctly — inconsistency.
**Fix:** Change to `IReadOnlyList<T>`. Note: if the project intentionally keeps wire DTOs as `List<T>` for JSON serialisation flexibility, this finding can be closed without action and noted in `known-deviations.md`.

### [LOW] Domain result records in `QaMetricsResult.cs` and `SprintSummaryResult.cs` are not `sealed`
**File:** `src/Services/Fokus/Fokus.Domain/Analytics/QaMetricsResult.cs:6,13,18,23,29` and `SprintSummaryResult.cs:5,14,28,34,43,52,61,66,71,77,83`
**Issue:** Coding conventions: "Use `sealed record` for DTOs, value objects, and domain events." `QaMetricsData`, `QaMetricCardData`, `QaSparklinePoint`, `QaBreakdownData`, and all records in `SprintSummaryResult.cs` are declared as `record` without `sealed`. `TestTimelineResult.cs` records are correctly `sealed record` throughout — inconsistency within the same commit.
**Fix:** Add `sealed` modifier to all domain result records in `QaMetricsResult.cs` and `SprintSummaryResult.cs`.

## Positive Observations

- Behavioral parity confirmed: computation logic is a clean relocation — no algorithmic changes found by tracing all calculator methods.
- `QaBreakdownData` defined once in `QaMetricsResult.cs`, referenced across `SprintSummaryResult.cs` and `SprintSummaryCalculator.cs` without redefinition.
- `TestingCrunchFlag` (domain) and `TestingCrunchResult` (wire) are correctly named parallel types in their respective layers. Endpoint maps between them in `MapFlags`.
- gRPC `FokusSprintInfo` alias correctly updated to `Fokus.API.Features.Analytics.GetSprintSummary.SprintInfo`; CS0104 resolved cleanly via full qualification on the three `ExcludedDeveloperFilter` call sites.
- `LoadQaDataAsync` return type correctly changed from `QualityBreakdownResult?` (wire) to `QaBreakdownData?` (domain); the now-redundant manual conversion block removed.
- Mapster `TestTimelineMap : IRegister` registers all 11 nested maps; wire contract byte-for-byte identical.
- `SprintSummaryCalculator.ComputeSummary` parameter list correctly accepts `QaBreakdownData?` and `TestingCrunchFlag?` as domain types — no wire contamination.
- Build: 0 errors. 7 warnings — all pre-existing except CS9113 (flagged LOW above).

## Gaps

- The 22 files that received `using Fokus.Domain.Analytics;` remain as 3c-B/C migration targets. The `using` addition is a minimal change; no 3c-B/C logic was touched.
- No unit tests cover the new calculators. This is a known project-level gap documented in the plan; behavioral correctness is evidenced by the clean relocation and passing build rather than test coverage.

## Open Questions

- **Wire record `List<T>` policy**: if the project intentionally relaxes the `IReadOnlyList<T>` convention for wire DTOs serialised to JSON (where `List<T>` is common practice), the LOW finding on `GetSprintSummaryQuery.cs` wire types can be closed without action and recorded in `known-deviations.md`.

## Evidence

| Check | Result | Command | Output summary |
|-------|--------|---------|----------------|
| Fresh build | PASS | `dotnet build SprintRituals.slnx --no-incremental` | Build succeeded, 0 errors, 7 warnings (4×NU1903, CS8629, CS9107 pre-existing; CS9113 flagged LOW) |
| Old service registrations removed | PASS | DI diff review | 0 `AddScoped` of old services in `DependencyInjection.cs` |
| Domain calculators registered | PASS | DI diff review | All 3 new `AddScoped` entries present |
| 7 API-layer files deleted | PASS | `git show` diff stat | 7 files removed; build green confirms no orphan references |
| `QaBreakdownData` defined once | PASS | Code inspection | Single definition in `QaMetricsResult.cs`; `SprintSummaryResult.cs` carries a comment confirming the non-redefinition |
| Wire contract byte-stability | PASS | Manual diff of wire records vs originals | All field names, types, nullability, order unchanged across all 3 HTTP responses and gRPC contract |
| gRPC mixed state compiles | PASS | Build result + code inspection | `SprintAnalyticsGrpcService` constructor has 2 calculators + 2 legacy services; all 3 gRPC methods resolve |
