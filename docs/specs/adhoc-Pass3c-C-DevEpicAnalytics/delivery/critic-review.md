# Critic Review — Pass 3c-C: DevEpicAnalytics (Mode 2, Plan vs Scope, code-grounded)

**Plan reviewed:** `docs/specs/adhoc-Pass3c-C-DevEpicAnalytics/delivery/plan.md`
**Scope sources:** `docs/backlog.md` row 3c-C · ADR-005/006/010 (`docs/architecture/decisions.md`) · answered `docs/specs/adhoc-Pass3c-C-DevEpicAnalytics/delivery/questions.md` (Q1/Q2/Q3 all `Answered`)
**Method:** Read every plan step and every cited source file on disk. Verified each of the 7 calculators, all line/record references, the Q1/Q2/Q3 contracts, the carve-out table, and the shared-infra-consumption invariant against actual code.

## Verdict: APPROVE (with 1 LOW correction + 4 advisories)

The plan is faithful to all three locked decisions, the byte-identical-wire invariant, and the 3c-A/3c-B clean-break precedent. Every record-rename table, carve-out line, helper inventory, and DI line number I checked is accurate against the source. No HIGH or MEDIUM findings. One LOW factual correction (early-return counts) and four advisories where the plan over-escalates a decision it could resolve from existing code, plus one verification-gap note.

---

## Faithfulness of the three locked decisions

### Q1 = Option B — FAITHFUL
- All 5 cross-service structural records move to ONE shared wire file `Fokus.API/Features/Analytics/SharedAnalyticsWire.cs` (Step 1), namespace `Fokus.API.Features.Analytics`, consumed by both endpoints of each shared pair — **not** per-endpoint duplication. Verified the consumer graph on disk:
  - `SprintSummaryItem` (decl `DeveloperThroughputService.cs:6`) → consumed by `QaWorkloadService.cs` + `GetQaWorkloadEndpoint.MapSprintInfo`. Matches Q1 cluster 1.
  - `DeveloperProgressSprintInfo` / `DayBreakdownEntry` / `CompletedTicketEntry` (decl `DeveloperProgressService.cs:6/19/17`) → consumed by `DeveloperDetailService.cs` (`CurrentSprintDetail` at lines 34/45). Matches Q1 cluster 2.
- Single-ownership baseline verified: each of the 5 records is declared **exactly once** today (all in the two service files slated for deletion). Step 1's single-ownership rule + Step 10/11 grep checks correctly enforce that the post-migration state keeps exactly one declaration each, in `SharedAnalyticsWire.cs`. The per-endpoint `*Response.cs` files (Step 2/3/4) explicitly do **not** redeclare them. Good.
- Domain side correctly does NOT return these wire types (ADR-006/010): the rename table (plan lines 166–171) gives each a `*Data` domain counterpart in the per-calculator `*Result.cs`, mapped wire←domain at the endpoint. Correct.

### Q2 = "most meaningful suffix" — FAITHFUL
- The 7 names are assigned as binding contracts (plan lines 37–43): Throughput→**Calculator**, Progress→**Analyzer**, Detail→**Calculator**, Quality→**Scorer**, Epic→**Analyzer**, Workload→**Analyzer**, Trends→**Analyzer**. This is per-class meaningful naming — **not** blanket `*Calculator` (architect's rejected Option A) and **not** the skill's blanket `*Analyzer`. Each justification matches what the source computes (verified: Quality does coverage/pass-rate RAG scoring + below-median streak → `Scorer`; Progress does gap/pace/stall + alert-direction → `Analyzer`; etc.). Faithful to the user decision.

### Q3 = keep hand-written mappers for Quality + Workload — FAITHFUL
- Both get **no** new `*Response.cs` and **no** Mapster `IRegister` (plan line 123). Verified the existing mappers exist and will only be re-pointed:
  - DeveloperQuality: `MapResponse(DeveloperQualityResult result)` at `GetDeveloperQualityEndpoint.cs:203`, with `SparklinePointResponse` projection at 240/245. Plan Step 5 re-points the parameter type + sparkline `p` only. Correct.
  - QaWorkload: `MapMultiResponse`/`MapSingleResponse`/`MapEntry`/`MapSingleEntry`/`MapMetricCard`/`MapSprintInfo`/`MapAlert` all present at `GetQaWorkloadEndpoint.cs:208–338`. Plan Step 7 re-points parameter types and keeps `QaWorkloadResponseDto` envelope assembly in the endpoint. Correct.

---

## Behavior-unchanged / byte-identical wire — UPHELD

- All 7 wire response records keep exact names/shapes; renaming is domain-side only (`*Result`/`*Data`). Confirmed all 7 endpoints declare the **wire** type as the `Endpoint<,>` generic param (`DeveloperThroughputResponse`, `DeveloperProgressResponse`, `DeveloperDetailResponse`, `DeveloperQualityResponse`, `EpicProgressResponse`, `QaTrendsResponse`, and `QaWorkloadResponseDto`). Step 11 check 4 correctly guards this.
- 3c-A/3c-B shared infra is **consumed, not re-declared** — verified each exists exactly where the plan claims:
  - `MetricCardData` (6-field bare card) — `Fokus.Domain/Analytics/MetricCardData.cs`. Present.
  - `MetricCardBuilder` — string path (`ComputeDelta`/`Build` with `"neutral"` short-circuit) + bool path (`Direction(decimal)`/`Direction(int)`/`Polarity(decimal,bool)`/`Polarity(int,bool)`). Present and matches the plan's polarity-path map exactly.
  - Wire `MetricCard` + `SparklinePoint(string SprintName, decimal Value)` — `GetSprintSummary/GetSprintSummaryQuery.cs:56/58`. Present; stays in API layer; consumed by Quality + Workload today (verified `using Fokus.API.Features.Analytics.GetSprintSummary;` at the top of both service files). Plan correctly forbids the domain from referencing them (Step 11 check 5).
  - Domain helpers `TransitionAttributionChecker`, `ExcludedDeveloperFilter`, `CompletionChecker`, `HealthScoreCalculator` — all present in `Fokus.Domain/Analytics/`.
- **Carve-out table is exact** (verified every line):
  - `DeveloperThroughputService.cs:153` `spAssignedDeltaPolarity = "neutral";` ✓ (private helpers at 317–331 ✓)
  - `DeveloperQualityService.cs:134` `bugsDeltaPol = "neutral";` ✓ (private helpers at 441–450; note Quality has only `DeltaPolarity(decimal,bool)`, no int polarity overload — plan's "441–450" range is correct ✓)
  - `QaWorkloadService.cs:605/609/629` `tesPol/runsPol/bugsPol = "neutral"` ✓
  - The string-polarity-**argument** lines `QaWorkloadService.cs:459/466/474` (literal `"neutral"`/`"positive-up"` passed INTO `BuildMetricCard`) are correctly distinguished from the DO-NOT-TOUCH stored-field literals. Verified these flow through `BuildMetricCard`→`ComputeDelta`. The plan's distinction is sound and important.
- QaWorkload envelope: `QaWorkloadResponse` (the `{HasQaData, Mode, MultiSprint?, SingleSprint?}` record at `QaWorkloadService.cs:97`) is in fact **dead in source** — the endpoint assembles `QaWorkloadResponseDto` directly from `ComputeMultiSprint`/`ComputeSingleSprint` results and never references `QaWorkloadResponse`. The plan's instruction "stays wire-only, never mirror into domain, calculator returns the two shapes separately" is therefore **correct** (the dead record is simply deleted with the service file). See advisory A3 for the wording nit.
- QaTrends dual-injection verified: `GetQaTrendsEndpoint.cs:13–14` injects BOTH `BugRatioCalculator` and `QaTrendsService`; uses `bugRatioResult.TeamMetrics.PerSprintTrend` at line 141. Plan Step 8 correctly says "swap only QaTrendsService→QaTrendsAnalyzer; keep BugRatioCalculator." `ComputePearsonR` N≥6 gate confirmed (`QaTrendsService.cs:177 if (n < 6) return null`).
- DI swap (Step 9): the 7 registrations are at lines 45, 49, 52, 53, 54, 55, 56 — **exactly** as the plan states; `using Fokus.Domain.Analytics;` already at line 7. The remove/add table maps each old→new correctly.

---

## Findings

### LOW-1 — Early-return counts are off (cosmetic; does not change the work)
The plan over-counts the early `Send.OkAsync(new …Response(...))` constructions in two endpoints:
- **Step 2 (Throughput)**, plan line 205: "the two early empty returns (`new DeveloperThroughputResponse([], [])`, lines 23)". There is **one** such return (`GetDeveloperThroughputEndpoint.cs:23`), not two.
- **Step 3 (Progress)**, plan line 240: "the three early empty returns". There are **two** (`GetDeveloperProgressEndpoint.cs:25` and `:33`), not three.

Impact: none on correctness — the instruction is "leave them as wire constructions," which applies regardless of count. Flagged only so the developer doesn't hunt for a non-existent third/second return. (DeveloperQuality genuinely has three at lines 22/32/69; EpicProgress one at 27; those match.)

### ADVISORY A1 — Step 7 over-escalates the sparkline-card decision (it is resolvable from code now)
Step 7 (and Risk #1, plan lines 370/502) leaves open whether QaWorkload's domain card should reuse a 3c-A sparkline-bearing domain card and says a question "may be needed if the 3c-A sparkline element type differs." The code already answers this:
- Wire `SparklinePoint(string SprintName, decimal Value)` and BOTH existing domain points `SprintSparklinePoint(string SprintName, decimal Value)` (`SprintSummaryResult.cs:30`) and `QaSparklinePoint(string SprintName, decimal Value)` (`QaMetricsResult.cs:24`) are **field-identical**.
- The established 3c-A pattern (`GetSprintSummaryEndpoint.cs:179 MapMetricCard(SprintMetricCardData c)`) is: **each calculator owns its own domain sparkline point**, and the endpoint projects it to the wire `SparklinePoint`. 3c-A did NOT share one sparkline point across calculators.
- Therefore the clean-break-consistent move is unambiguous: add a **QaWorkload-local** domain sparkline-bearing card + sparkline point in `QaWorkloadResult.cs` (do **not** reuse `SprintMetricCardData`/`QaMetricCardData` from another calculator's result file — that would introduce the cross-calculator coupling 3c-A deliberately avoided). No user/architect question is warranted; element types are identical so there is zero behavior risk. Recommend the architect downgrade Risk #1 from "lowest-confidence, may need a question" to "decided: per-calculator domain card, mirror 3c-A."

### ADVISORY A2 — Step 5 has the same resolvable-now sparkline decision
Risk #2 (plan line 503) and Step 5 line 301 hedge "reuse existing 3c-A domain record vs add one." Same resolution as A1: add a DeveloperQuality-local domain sparkline point (`(string SprintName, decimal Value)`) in `DeveloperQualityResult.cs`; the hand-mapper re-points `p` to it. No question needed.

### ADVISORY A3 — Step 7 wording mis-describes the envelope's current wiring
Plan line 354/368 says the `{HasQaData,Mode,Multi?,Single?}` envelope is "the endpoint already builds it via `QaWorkloadResponseDto`." Precisely: the service-layer record `QaWorkloadResponse` (line 97) is **unused** today; the endpoint builds `QaWorkloadResponseDto` directly. The plan's *action* (don't mirror the envelope to domain; calculator returns the two shapes separately; delete `QaWorkloadResponse` with the service file) is correct — only the descriptive clause is loose. No action required beyond noting that `QaWorkloadResponse` is dead code that disappears in Step 10; the developer should not try to "move" it anywhere.

### ADVISORY A4 — `StalledTicketEntry` in the shared file is single-consumer (but user-sanctioned)
`StalledTicketEntry` has exactly one consumer (DeveloperProgress itself; verified — Detail does not use it). Putting a single-consumer record in a "shared" file is mild over-sharing against the vertical-slice grain. However, the **Q1 answer explicitly enumerates it** ("the DeveloperProgress structural records (`DeveloperProgressSprintInfo`, `DayBreakdownEntry`, `CompletedTicketEntry`, `StalledTicketEntry`)"), so the plan's inclusion (line 161) is a faithful execution of the user decision, not a plan overreach. No change required; recorded for transparency.

### NOTE N1 — Verification-sweep coverage is strong
Step 11's grep set (zero-hit old service names; single-ownership of the 5 shared records; domain-references-no-wire-type check for `MetricCard\b`/`SparklinePoint\b`/`GetSprintSummary`/`Fokus.API`; carve-out `"neutral"` integrity; BugRatioCalculator-still-injected; both-mode smoke test with a non-zero-delta `spAssignedDeltaPolarity:"neutral"` spot-check) maps cleanly onto every risk surface I independently checked. No gap. One small suggestion: also assert the domain `*Result.cs` files contain **per-calculator** sparkline points for Quality/Workload (i.e., grep that `QaWorkloadResult.cs`/`DeveloperQualityResult.cs` do NOT reference `SprintSparklinePoint`/`QaSparklinePoint`) so A1/A2 can't silently regress into cross-calculator reuse.

---

## Items confirmed accurate (no action)
- 7-calculator rename tables (Steps 2–8) — every old→new mapping matches the source records.
- `EpicQaData` stays its own name as a domain **input** record; carries `Dictionary<string,List<string>>` ×2 + `Dictionary<string,List<TestRun>>` (TestRun is a domain entity) — ADR-006-safe. Verified `EpicProgressService.cs:74–77`.
- Step 3→4 cross-step domain coupling (Detail's `CurrentSprintDetailData` references Progress's `DeveloperProgressSprintData`/`DayBreakdownData`/`CompletedTicketData`, same namespace) — verified `CurrentSprintDetail` at `DeveloperDetailService.cs:33–46` references the shared records; ordering note (land Step 3 first) is correct.
- "No-kernel" services (Progress/Detail/Epic/Trends) — confirmed none use metric-card direction/polarity helpers; Progress `Direction` is an inline string enum (`DeveloperProgressService.cs:250–258`) + sort comparator (280–282), correctly flagged keep-verbatim.
- Local `ResolveStartIndex`/`IsStartedInSprint`/`GetStageIndex` duplicates in Quality (`489`/`463`/`477`) and Trends (`355`) — correctly flagged "move verbatim, do NOT consolidate onto TransitionAttributionChecker" (behavior-parity guard).
- `BuildMetricCard` (676), `DeltaDirection`/`DeltaPolarity` (711/714), `NullableStringComparer` (722–731) in QaWorkload — present; plan's "720–731" for the comparer is off by 2 lines (starts 722), immaterial.
- DI line numbers (45/49/52/53/54/55/56) — exact.
- 3c-B IRegister precedent (`GetCarryOver/GetCarryOverResponse.cs`) cited by Steps 2/3/4/6/8 — exists and is the right pattern to mirror.
- Step-count=11 (>10) justification — sound: Step 1's shared wire is consumed by 2/3/4/7 and Steps 9–10 depend on all migrations; splitting yields a non-building intermediate. Keeping whole matches 3c-B precedent.

---

Reviewed: docs/specs/adhoc-Pass3c-C-DevEpicAnalytics/delivery/plan.md
