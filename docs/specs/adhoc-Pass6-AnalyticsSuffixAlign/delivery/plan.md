# Pass 6 — Analytics Suffix Alignment

**Feature Spec:** None (ad-hoc refactoring pass — binding inputs are the `analytics-computation-service` naming rule, the 3c-C precedent, and the owner decision in `docs/backlog.md` row 6)

## Context

After Pass 3c-C shipped semantically-suffixed analytics calculators (`*Analyzer`/`*Scorer`/`*Calculator` by what each computes), the earlier 3c-A and 3c-B calculators remain uniformly suffixed `*Calculator`. `Fokus.Domain/Analytics/` is therefore **mixed-suffix**: half semantic, half blanket. The learner consolidation (2026-06-10) logged this inconsistency; the owner backlogged the cleanup as `adhoc-Pass6-AnalyticsSuffixAlign` (backlog row 6).

This pass retro-applies the `analytics-computation-service` naming rule — *name by what the class computes* (`Scorer` = produces a score; `Analyzer` = trend/direction/polarity/alerts/burnup over periods; `Calculator` = raw aggregation/derivation/assembly) — to the misnamed calculators, making the whole folder consistent.

**Arithmetic.** Eleven classes in `Fokus.Domain/Analytics/` carried the `*Calculator` suffix at plan time. Nine are renamed (7 from the original 3c-A/3c-B sweep + 2 added per Q1: `DeveloperThroughputCalculator`, `DeveloperDetailCalculator`, which 3c-C shipped without a semantic suffix). Two are kept as documented carve-outs: `LeaderboardCalculator` (raw per-developer SP/ticket aggregation) and `HealthScoreCalculator` (static scoring-helper, active coverage target — owner Q3). After this pass `Fokus.Domain/Analytics/` holds **zero** misnamed calculators: every `*Calculator` left is a deliberate carve-out.

**Internal-only.** These are domain-layer C# class names. No HTTP route, response DTO, gRPC contract, or SignalR string is affected. This is a pure rename with zero wire impact and zero behavior change.

Impacted: `Fokus.Domain` (class declarations) and `Fokus.API` (DI, gRPC service injection, endpoint injection) only. No frontend impact (client grep: zero hits).

## Scope

**In scope:** rename the 9 misnamed calculator classes + their `.cs` files; update every C# reference site; update the 2 current KB entries and the ADR-005 illustrative path that name the renamed classes.

**Out of scope (DO NOT TOUCH):**
- `LeaderboardCalculator` — stays `Calculator` (raw per-developer SP/ticket aggregation; correctly categorized).
- `HealthScoreCalculator` — static scoring-helper; left as-is per owner decision (Q3). It is the active coverage/Stryker target; disturbing it is out of scope.
- The 3c-C classes (`DeveloperProgressAnalyzer`, `DeveloperQualityScorer`, etc.) — already correctly suffixed.
- All `*Result` / `*Data` domain records and the `*Response` wire envelopes — never part of a calculator rename.
- The other static helpers (`MetricCardBuilder`, `TransitionAttributionChecker`, `CompletionChecker`, `ExcludedDeveloperFilter`) — already non-`Calculator`-suffixed.
- Prior-pass delivery artifacts under `docs/specs/*/delivery/` — immutable point-in-time records.
- The shared plugin skill (`analytics-computation-service` SKILL.md, in `<plugins-folder>`) — skill edits route through the learner, never from this repo. The skill teaches `SprintSummaryCalculator` as its `Calculator` exemplar; our project's call to make it an `Analyzer` is a deliberate local decision, not a skill defect.
- The coverage-workstream artifacts (`docs/audit/golden-set.md`, `docs/proposals/br-coverage-golden-set-draft.md`, `docs/audit/research-raw/golden-code-mining.md`) and any *pending* `BugRatio*Tests.cs` scaffolding — see **Open Questions / Coordination**.

## Skill Mapping

| Step | Skill | Disposition | TDD | Feature-Specific Inputs | Gap? |
|------|-------|-------------|-----|------------------------|------|
| 1 | (none) | — | no | 9 class+file renames per the rename table; naming authority = `analytics-computation-service` "name by what it computes" | |
| 2 | (none) | — | no | reference-site table (DI, gRPC ctor, 10 endpoints, 1 static call, 1 doc comment) | |
| 3 | (none) | — | no | build command + old-name/carve-out grep guards | |
| 4 | (none) | — | no | wire-stability diff invariants | |
| 5 | (none) | — | no | 2 KB files + ADR-005 illustrative path, with before/after | |

**This is a legitimate all-`None` plan.** No *pattern* skill applies — `analytics-computation-service` *creates* calculators; it does not perform renames. It is cited as the **naming authority** (the rule the new suffixes satisfy), not as a `Follow` step. The done-check applies the all-`None` exemption: an empty `skill-invocations.log` for this run is expected and is **not** a Fail.

**TDD: no on every step** — a pure rename changes no behavior and adds no testable surface. The 9 target classes have **no** dedicated test files (verified: the test project holds only `HealthScoreCalculatorTests.cs` and `MetricCardBuilderPropertyTests.cs`, neither in scope). The build is the conformance gate.

## Domain Model Changes

Class renames only. No new/modified aggregates, entities, value objects, or domain events. No method signatures, parameters, return types, or bodies change. Constructors rename to match their class.

### Rename table (old → new) — byte-checkable

| # | Old class & file | New class & file | Category |
|---|------------------|------------------|----------|
| 1 | `SprintSummaryCalculator` / `SprintSummaryCalculator.cs` | `SprintSummaryAnalyzer` / `SprintSummaryAnalyzer.cs` | Analyzer |
| 2 | `QaMetricsCalculator` / `QaMetricsCalculator.cs` | `QaMetricsAnalyzer` / `QaMetricsAnalyzer.cs` | Analyzer |
| 3 | `TestTimelineCalculator` / `TestTimelineCalculator.cs` | `TestTimelineAnalyzer` / `TestTimelineAnalyzer.cs` | Analyzer |
| 4 | `CarryOverCalculator` / `CarryOverCalculator.cs` | `CarryOverAnalyzer` / `CarryOverAnalyzer.cs` | Analyzer |
| 5 | `CycleTimeCalculator` / `CycleTimeCalculator.cs` | `CycleTimeAnalyzer` / `CycleTimeAnalyzer.cs` | Analyzer |
| 6 | `BugRatioCalculator` / `BugRatioCalculator.cs` | `BugRatioAnalyzer` / `BugRatioAnalyzer.cs` | Analyzer |
| 7 | `ScopeChangeCalculator` / `ScopeChangeCalculator.cs` | `ScopeChangeAnalyzer` / `ScopeChangeAnalyzer.cs` | Analyzer |
| 8 | `DeveloperThroughputCalculator` / `DeveloperThroughputCalculator.cs` | `DeveloperThroughputAnalyzer` / `DeveloperThroughputAnalyzer.cs` | Analyzer |
| 9 | `DeveloperDetailCalculator` / `DeveloperDetailCalculator.cs` | `DeveloperDetailAnalyzer` / `DeveloperDetailAnalyzer.cs` | Analyzer |

All nine domain files live in `src/Services/Fokus/Fokus.Domain/Analytics/`.

Rows 8–9 are the two classes the architect classified in Q1 as missed entries (not carve-outs): both compute trend/delta/direction/polarity/rolling-average/burnup/stall surfaces, not raw aggregation. See `questions.md` Q1 for the line-grounded rationale.

**Not renamed (carve-out, same text root):** `LeaderboardCalculator`, `HealthScoreCalculator`. A blanket replace of `Calculator` is forbidden — rename only the nine exact symbols above.

## Data Model Changes

None. No tables, columns, relationships, or migrations.

## Implementation Steps

### Step 1 — Rename the 9 domain calculator classes and files

Rename each of the nine `.cs` files and the class declaration + primary constructor inside it, per the rename table. Also update **every** old-name mention inside each renamed file — whether a self-reference (the file's own class) or a cross-reference to another renamed class — including comments, XML-doc `<see cref>`, `nameof(...)`, and log category strings. (Known cross-reference: `TestTimelineCalculator.cs:67` mentions `SprintSummaryCalculator` in a doc comment — it must become `SprintSummaryAnalyzer`.) Comment occurrences in **non-renamed** files are handled in Step 2.

Self-reference notes for rows 8–9 (verified at amend time): `DeveloperThroughputCalculator.cs` and `DeveloperDetailCalculator.cs` each name themselves only in the class declaration (line 3) — neither declares an explicit constructor, has a `nameof`, or self-mentions in a comment. The `// mirrors DeveloperProgressAnalyzer` comment at `DeveloperDetailCalculator.cs:125` names a **different** class (a 3c-C `*Analyzer` that is NOT renamed) and stays verbatim — do not touch it.

- Files: the 9 under `src/Services/Fokus/Fokus.Domain/Analytics/` (rename table).
- **DO NOT** rename `LeaderboardCalculator`, `HealthScoreCalculator`, any `*Result`/`*Data` record, or any `*Response` envelope — even if co-located in or near the renamed file. (Specifically the co-located `DeveloperThroughputResult.cs` and `DeveloperDetailResult.cs` hold the `*Data` result records and are NOT renamed.)
- Skill: None (mechanical rename; naming authority = `analytics-computation-service`).
- `Satisfies:` backlog row 6 (owner suffix-align decision) + the `analytics-computation-service` "name by what it computes" rule + Q1 (owner-accepted sweep of the two 3c-C misnamed calculators).

**Accept:** 9 new files present, 9 old files gone (git rename); each class and its constructor renamed; no `*Result`/`*Data` record renamed (incl. `DeveloperThroughputResult`/`DeveloperDetailResult`); carve-out classes untouched; **no old class name remains anywhere inside the 9 renamed files** (including comments — e.g. `TestTimelineCalculator.cs:67`); the `:125` cross-reference comment in `DeveloperDetailCalculator.cs` preserved verbatim.

### Step 2 — Update every C# reference site

Update all references to the renamed types. The complete set (verified by solution-wide grep at plan time):

| Reference site | Renamed types | Kind |
|----------------|---------------|------|
| `Fokus.API/DependencyInjection.cs` (lines ~48-62) | all 9 (SprintSummary, QaMetrics, TestTimeline, CarryOver, CycleTime, BugRatio, ScopeChange, **DeveloperThroughput @ :51, DeveloperDetail @ :62**) | `AddScoped<T>()` registration |
| `Fokus.API/Grpc/SprintAnalyticsGrpcService.cs` (ctor + fields) | SprintSummary, QaMetrics, CarryOver, ScopeChange | constructor injection (4 in one ctor) |
| `Fokus.API/Features/Analytics/GetSprintSummary/GetSprintSummaryEndpoint.cs` | SprintSummary (inj), QaMetrics (inj), **TestTimeline (static call at :121 — `TestTimelineCalculator.ComputeCrunchFlag(...)` → `TestTimelineAnalyzer.ComputeCrunchFlag(...)`)** | injection + static call |
| `Fokus.API/Features/Analytics/GetQaMetrics/GetQaMetricsEndpoint.cs` | QaMetrics | constructor injection |
| `Fokus.API/Features/Sprints/GetTestTimeline/GetTestTimelineEndpoint.cs` | TestTimeline | constructor injection (note: under `Features/Sprints/`, not `Analytics/`) |
| `Fokus.API/Features/Analytics/GetCarryOver/GetCarryOverEndpoint.cs` | CarryOver | constructor injection |
| `Fokus.API/Features/Analytics/GetCycleTime/GetCycleTimeEndpoint.cs` | CycleTime | constructor injection |
| `Fokus.API/Features/Analytics/GetBugRatio/GetBugRatioEndpoint.cs` | BugRatio | constructor injection |
| `Fokus.API/Features/Analytics/GetQaTrends/GetQaTrendsEndpoint.cs` | **BugRatio (second injection site)** | constructor injection |
| `Fokus.API/Features/Analytics/GetScopeChange/GetScopeChangeEndpoint.cs` | ScopeChange | constructor injection |
| `Fokus.API/Features/Analytics/GetDeveloperThroughput/GetDeveloperThroughputEndpoint.cs` (ctor param @ :13; type only — local var name unchanged) | DeveloperThroughput | constructor injection |
| `Fokus.API/Features/Analytics/GetDeveloperDetail/GetDeveloperDetailEndpoint.cs` (ctor param @ :15; type only — local var name unchanged) | DeveloperDetail | constructor injection |
| `Fokus.Domain/Analytics/MetricCardData.cs` (line ~5) | CarryOver, ScopeChange, BugRatio, CycleTime (doc comment) | XML/doc comment |

- `GetLeaderboardEndpoint.cs` is **not** touched (Leaderboard stays `Calculator`).
- **No test file** references any of the 9 (verified) — the test project is not modified by this step.
- Skill: None.

**Comment / doc-comment occurrences in non-renamed files.** These contain an old name in **prose only** — update the text; do **not** rename the file or any record (the `*Result`/`*Data`/`*Builder` carve-out still holds):

| File:line | Old → new |
|-----------|-----------|
| `Fokus.Domain/Analytics/MetricCardBuilder.cs:6` | `SprintSummaryCalculator`, `QaMetricsCalculator` → `*Analyzer` (Builder NOT renamed — comment only) |
| `Fokus.Domain/Analytics/QaMetricsResult.cs:27` | `QaMetricsCalculator`, `SprintSummaryCalculator` → `*Analyzer` (record NOT renamed — comment only) |
| `Fokus.Domain/Analytics/TestTimelineResult.cs:90` | `SprintSummaryCalculator` → `SprintSummaryAnalyzer` (record NOT renamed — comment only) |
| `Fokus.API/Features/Analytics/GetQaTrends/GetQaTrendsEndpoint.cs:98` | `BugRatioCalculator` → `BugRatioAnalyzer` (code comment) |
| `Fokus.API/Features/Analytics/GetQaTrends/GetQaTrendsEndpoint.cs:113` | `BugRatioCalculator` → `BugRatioAnalyzer` (code comment) |

`GetQaTrendsEndpoint.cs` appears in **both** tables — the injection at :14 (main table) **and** the comments at :98/:113 — don't tick the file off after the injection edit.

**Accept:** the solution compiles; every reference resolves to the new name; the three easy-to-miss code sites (static call at `GetSprintSummaryEndpoint.cs:121`, BugRatio's second injection in `GetQaTrendsEndpoint`, the `MetricCardData.cs` doc comment) **and** all 5 comment/doc sites in the sub-table are updated.

### Step 3 — Build + grep-guard verification (hard gate)

1. `dotnet build SprintRituals.slnx` → 0 errors (warnings: pre-existing NU1903 only).
2. Grep `src/` for each of the 9 **old** names (`SprintSummaryCalculator`, `QaMetricsCalculator`, `TestTimelineCalculator`, `CarryOverCalculator`, `CycleTimeCalculator`, `BugRatioCalculator`, `ScopeChangeCalculator`, `DeveloperThroughputCalculator`, `DeveloperDetailCalculator`) → **zero hits**.
3. Grep `src/` for the carve-out names (`LeaderboardCalculator`, `HealthScoreCalculator`) → **still present, unchanged** (proves no over-rename). Note both old `Developer*Calculator` names share no substring with the carve-outs, so no false collision.
- Skill: None.

**Accept:** build green; zero hits for the 9 old names in `src/`; carve-out names intact.

### Step 4 — Wire-stability check (what must NOT change)

Review `git diff` and confirm the change is confined to domain class names + their C# reference sites + the three docs in Step 5. Specifically, **zero** changes to:
- any HTTP route string / endpoint `Configure()` route,
- any `*Response` / `*Command` / `*Query` type name or serialized property name,
- any gRPC contract (`Blocks.Contracts` interfaces, proto message names) — the gRPC service's *injected* domain types change, the *contract* does not,
- any SignalR hub method, group, or claim string.
- Skill: None.

**Accept:** `git diff` shows no wire-visible identifier changes; the diff is rename-only.

### Step 5 — Update current KB entries + ADR-005 illustrative path

Update only these **current/canonical** docs (prior-pass artifacts and the shared skill stay untouched — see Scope):

- `docs/kb/bug-ratio.md`: `BugRatioCalculator` → `BugRatioAnalyzer` at lines 4, 28, 43, 44, 45, 49. Also update the *pending* test-file names at lines 29-30: `BugRatioCalculatorTests.cs` → `BugRatioAnalyzerTests.cs`, `BugRatioCalculatorPropertyTests.cs` → `BugRatioAnalyzerPropertyTests.cs` (these files don't exist yet; the KB names the future target).
- `docs/kb/health-score.md`: `SprintSummaryCalculator` → `SprintSummaryAnalyzer` and `QaMetricsCalculator` → `QaMetricsAnalyzer` at lines 4, 36, 37, 42. **Leave every `HealthScoreCalculator` reference unchanged** (not renamed).
- `docs/architecture/decisions.md` ADR-005: change the illustrative path only —
  before: `(e.g. \`Fokus.Domain/Analytics/SprintSummaryCalculator.cs\`)`
  after:  `(e.g. \`Fokus.Domain/Analytics/SprintSummaryAnalyzer.cs\`)`
  The decision text is otherwise unchanged. This is an accuracy fix to an example path, not an amendment to the decision.
- Skill: None.

**No KB edit owed for the two Q1-added classes.** A `docs/` grep for `DeveloperThroughputCalculator` / `DeveloperDetailCalculator` (at amend time) returns hits only in (a) prior-pass delivery artifacts under `docs/specs/*/` (immutable — never edited), and (b) the coverage-workstream files (`docs/audit/golden-set.md`, `docs/proposals/br-coverage-golden-set-draft.md`, `docs/audit/research-raw/golden-code-mining.md`) — all three already out-of-scope (see Scope + Open Questions). **No `docs/kb/` entry names either class**, so neither appears in the two KB files edited here and Step 5 adds no new KB edit for them. The coverage-workstream ripple for these two is folded into the Open Questions note below.

**Accept:** the three docs updated; the old class names of the renamed classes that *were* present no longer appear in `docs/kb/bug-ratio.md` or `docs/kb/health-score.md` (these two KB files reference only `BugRatio`/`SprintSummary`/`QaMetrics` — never the Developer pair); ADR-005 path corrected; no prior-pass artifact edited.

## Cross-Service Changes

None to any **contract**. `SprintAnalyticsGrpcService` injects four renamed domain calculators (SprintSummary, QaMetrics, CarryOver, ScopeChange) — those injection references update in Step 2, but the gRPC contract (`ISprintAnalyticsService` and its proto messages in `Blocks.Contracts`) is unchanged. No integration events affected.

## Migration Notes

None. No schema or data changes.

## Testing Strategy

- Primary gate: `dotnet build SprintRituals.slnx` green (Step 3).
- No new automated tests — pure rename, no behavior change, and the program runs manual testing per pass (no automated suite by decision). The 9 classes have no existing tests to update (the test project holds only `HealthScoreCalculatorTests.cs` and `MetricCardBuilderPropertyTests.cs`, neither in scope; no `DeveloperThroughput*`/`DeveloperDetail*` test file exists).
- Manual smoke (optional, owner's call): hit a couple of the analytics endpoints (e.g. `GetSprintSummary`, `GetBugRatio`) and confirm identical responses — the wire contract is unchanged, so this is belt-and-suspenders.

## KB Impact

Two KB entries name the renamed classes and are updated in **Step 5** (a numbered step, not a trailing note): `docs/kb/bug-ratio.md`, `docs/kb/health-score.md`. The ADR-005 illustrative path is corrected in the same step.

## Open Questions / Coordination

**Coverage-workstream ripple (flagged, not a blocker).** `BugRatioCalculator` (and the Sprint/Qa calculators) — **plus the two Q1-added classes `DeveloperThroughputCalculator` and `DeveloperDetailCalculator`** — are referenced by an active coverage/mutation-testing workstream:
- `docs/audit/golden-set.md` (GOLD-19 cites `DeveloperThroughputCalculator.cs:252-268`; GOLD-20 cites `DeveloperDetailCalculator.cs:58-59`), `docs/proposals/br-coverage-golden-set-draft.md` (same two rows), `docs/audit/research-raw/golden-code-mining.md` (immutable mining record naming both files + line ranges).
- *pending* test scaffolding named `BugRatioCalculatorTests.cs` / `BugRatioCalculatorPropertyTests.cs` (not yet created — referenced as "pending Step 3 / Cover" in `docs/kb/bug-ratio.md`).

**Recommended handling (Confidence: medium):** keep those artifacts **out of scope** for this naming pass — editing another workstream's audit/proposal docs from a rename pass is scope creep, and the research-raw/golden-code-mining file is an immutable record. Because the BugRatio tests are still *pending*, doing Pass 6 **now** is actually the low-friction sequencing: the Cover workstream will create them as `BugRatioAnalyzerTests.cs` from the start. **Two consequences of the Q1 sweep for this ripple:** (1) GOLD-19/GOLD-20's source-path citations (`DeveloperThroughput*`/`DeveloperDetail*`) will go stale the moment Step 1 renames the files — they must be re-pointed to `DeveloperThroughputAnalyzer.cs` / `DeveloperDetailAnalyzer.cs` (and the line ranges re-verified) when the coverage workstream next runs; (2) the cited line ranges may also shift slightly, since renaming a self-reference can move no lines but is worth a re-check. Action owed: the team lead / learner notifies the coverage-workstream owner that `BugRatioCalculator` → `BugRatioAnalyzer`, `DeveloperThroughputCalculator` → `DeveloperThroughputAnalyzer`, and `DeveloperDetailCalculator` → `DeveloperDetailAnalyzer`, and that `golden-set.md` + the coverage proposal should be re-pointed when that workstream next runs. If the owner would rather absorb those edits into this pass, say so and I will add a sixth step.

## Plan Review

**Mode 2 (plan review), code-grounded critic — 2026-06-16.** The critic re-grepped the live `src/` tree and cross-referenced every old-name hit against the Step 2 table.

**Verdict: REQUEST-CHANGES → resolved.** Two HIGH findings, one root cause: Step 2's reference table under-counted the old-name occurrences that Step 3's zero-hit grep-guard enforces, leaving the plan internally contradictory (work list < acceptance gate). Six comment/doc sites were missed: `MetricCardBuilder.cs:6`, `QaMetricsResult.cs:27`, `TestTimelineResult.cs:90`, `GetQaTrendsEndpoint.cs:98`, `GetQaTrendsEndpoint.cs:113` (non-renamed files), and `TestTimelineCalculator.cs:67` (cross-reference inside a renamed file). All are comments — build stays green; no CRITICAL.

**Resolution applied:** Step 1's clause broadened to cover every old-name mention inside the 7 renamed files (incl. the `:67` cross-reference); Step 2 gained a comment/doc sub-table for the 5 sites in non-renamed files, with the `*Result`/`*Builder` "comment-only, record-not-renamed" carve-out restated. Both gates (Step 3 grep-guard) are now satisfiable.

**Passed on first review (no change needed):** carve-out correctness (`Leaderboard`/`HealthScoreCalculator` exist and are untouched; no `*Result`/`*Data`/`*Response` in the rename table); wire-stability ("zero wire impact" confirmed — none of the 7 names appear in `Blocks.Contracts`, proto, or SignalR); KB line-number accuracy; refactor-rule conformance (rename table, hard grep-guard step, DO-NOT-TOUCH carve-outs, wire invariants); test-file independence; out-of-scope-doc handling.

### Amendment self-review — Q1 sweep of the two `Developer*` calculators (self mode, 2026-06-16)

**Scope of amendment:** add `DeveloperThroughputCalculator` → `DeveloperThroughputAnalyzer` and `DeveloperDetailCalculator` → `DeveloperDetailAnalyzer` (owner-accepted Q1), with full reference-site sweep, grep-guard, doc-ripple, and arithmetic reconciliation. Reviewed against the established 7-row rename pattern.

**Verdict: PASS.** The amendment conforms to the established pattern and the gates are satisfiable. Evidence below.

| Check | Finding | Evidence |
|-------|---------|----------|
| Rename table parity | Rows 8–9 follow the exact `Old class & file → New class & file → Category=Analyzer` shape of rows 1–7 | rename table |
| Reference-site completeness | Solution-wide grep for the two **exact** class names returns exactly 6 sites: 2 class decls (renamed in Step 1) + 2 DI registrations + 2 endpoint ctor params. All four reference sites (DI `:51`/`:62`, endpoints `:13`/`:15`) are in the Step 2 table | grep over `src/`; both endpoint files read in full |
| No static call / no second injection / no comment occurrence of the full class name | Unlike `TestTimeline` (static call) and `BugRatio` (2nd injection + comments), neither `Developer*` class has a static call site, a second injection, or a comment mentioning the full `...Calculator` symbol. The endpoint bodies call instance methods on the injected local (`developerThroughputCalculator.ComputeThroughput`, `developerDetailCalculator.ComputeDetail`) — local var names, not types, so they need no edit | endpoint bodies (`:102`, `:109`) read |
| `*Result`/`*Data` carve-out holds | `DeveloperThroughputResult.cs` / `DeveloperDetailResult.cs` (and every `*Data`/`*Response`/`*MappingConfig`/`Get*Endpoint`/`Get*Query` prefix-sharing sibling) are NOT in the rename table — confirmed not the calculator symbol | broad-stem grep distinguishes class symbol from siblings |
| Intra-file self-refs | Each renamed file self-names only at class decl `:3` (no explicit ctor, no `nameof`, no self-comment). `DeveloperDetailCalculator.cs:125` "mirrors DeveloperProgressAnalyzer" names a NON-renamed class — preserved verbatim | both files read in full |
| Grep-guard satisfiable | Step 3 zero-hit list extended to 9; the two new full names occur only at the 4 reference sites + 2 decls all edited by Steps 1–2; no stray comment occurrence of the full `...Calculator` string exists | grep confirms 6 total occurrences of each exact name |
| Wire-stability | Renaming the domain calculator does NOT touch `Get*Endpoint`/`Get*Query`/`Get*Response` (route/feature-named), the `*Response`/`*Entry` wire records, the Mapster `*MappingConfig`, or any `Blocks.Contracts`/proto/SignalR symbol. Route strings (`/api/analytics/developer-throughput`, `/api/analytics/developer-detail/{accountId}`) are untouched | endpoint attributes read |
| Doc ripple | No `docs/kb/` entry names either class → no new Step 5 KB edit owed. The only current-doc hits are the coverage-workstream files (already out-of-scope); Open Questions note widened to name both classes + the stale GOLD-19/GOLD-20 source-path citations | `docs/` grep |
| Arithmetic | 11 `*Calculator` classes at plan time → 9 renamed (7 original + 2 Q1) + 2 carve-outs (`Leaderboard`, `HealthScore`); all count references reconciled (Context, Scope, Skill Mapping, Steps 1/2/3, TDD, Testing Strategy) | full-doc `\b7\b`/`eight`/`seven` grep, residual hits are the historical Plan-Review record (intentionally not rewritten) and the literal "7 from the original sweep" |

**One residual, intentional:** the prior Plan Review subsection (Mode 2, lines ~190–196) still says "7" — that is a point-in-time record of the earlier review of the 7-row plan and is left unaltered; this amendment self-review is the current record.
