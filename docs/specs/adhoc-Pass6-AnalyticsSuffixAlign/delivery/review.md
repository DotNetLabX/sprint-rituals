# adhoc-Pass6-AnalyticsSuffixAlign — Review

## Step 1 — Done-Check

**Checker:** architect (Step 1 done-check) · **Date:** 2026-06-16 · **Mode:** code-grounded (live tree verified, not report-trusted)

**Scope:** Step 1 only — *rename the 9 domain calculator classes and files* (`Fokus.Domain/Analytics/`). Steps 2–5 are out of this check's mandate (the implementation completed them, but they belong to the Step 2 review). `docs/backlog.md` and `docs/specs/adhoc-SprintContextPanel/` in the working tree are unrelated parallel work and were ignored.

### Verdict: **PASS**

Every Step 1 acceptance criterion is satisfied against the live tree. No deviations.

### Acceptance-criterion dispositions

| # | Step 1 Accept criterion | Disposition | Evidence |
|---|-------------------------|-------------|----------|
| 1 | 9 new files present | **PASS** | Glob `*Analyzer.cs` → all 9 renamed files present (`SprintSummary`, `QaMetrics`, `TestTimeline`, `CarryOver`, `CycleTime`, `BugRatio`, `ScopeChange`, `DeveloperThroughput`, `DeveloperDetail`) alongside the 4 pre-existing 3c-C analyzers |
| 2 | 9 old files gone (git rename) | **PASS** | Glob `*Calculator.cs` → only `LeaderboardCalculator.cs` + `HealthScoreCalculator.cs` remain; all 9 old filenames absent. Implementation used `git mv` (history preserved) |
| 3 | Each class declaration renamed | **PASS** | Grep `^public class` over `*Analyzer.cs` → all 9 declarations present at `:3` with the new names |
| 4 | Each primary constructor renamed | **PASS (vacuous)** | None of the 9 classes declares an explicit/primary constructor (verified during planning); nothing to rename — criterion not applicable, not violated |
| 5 | No old class name remains inside the 9 renamed files (incl. comments) | **PASS** | Grep all 9 old `*Calculator` symbols across `*Analyzer.cs` → **zero hits**. `TestTimelineAnalyzer.cs:67` cross-reference correctly reads "reuse by SprintSummaryAnalyzer" |
| 6 | `DeveloperDetailAnalyzer.cs:125` cross-ref comment preserved verbatim | **PASS** | Line 125 reads `// --- Current sprint computation (mirrors DeveloperProgressAnalyzer) ---` — names a non-renamed 3c-C class; correctly untouched |
| 7 | No `*Result`/`*Data` record renamed | **PASS** | All 15 `*Result.cs` retain original names, incl. the two co-located with the Q1-added classes (`DeveloperThroughputResult.cs`, `DeveloperDetailResult.cs`) |
| 8 | Carve-out classes untouched | **PASS** | `LeaderboardCalculator.cs` and `HealthScoreCalculator.cs` present and unrenamed |

### Notes (non-blocking)

- The implementation completed Steps 2–5 in the same pass (build green, 0 errors; grep-guard reports 0 hits for all 9 old names in `src/`; carve-outs intact). Those gates belong to the **Step 2 review**, not this Step 1 done-check, so they are recorded here only as context — not verified as part of this verdict.
- Carry-over correctly flagged by the developer: GOLD-19/GOLD-20 source-path citations in `docs/audit/golden-set.md` + `docs/proposals/br-coverage-golden-set-draft.md` now point at the old `DeveloperThroughput*`/`DeveloperDetail*` filenames and need re-pointing when the coverage workstream next runs. This is out-of-scope per plan Open Questions — owner/team-lead action, not a Step 1 defect.

**Step 1 is done. Cleared to proceed.**

## Step 2 — Code Review

**Reviewer:** reviewer · **Date:** 2026-06-16 · **Feature:** adhoc-Pass6-AnalyticsSuffixAlign

### Evidence

| Check | Command | Result |
|-------|---------|--------|
| Build | `dotnet build SprintRituals.slnx --no-restore` | **0 errors**, 15 pre-existing warnings (NU1903 ×12, CS8629 ×1, CS8601 ×1, CS9107 ×1) |
| Grep-guard: 9 old names in `src/` | 9 × `grep -r <OldName> src/` | **0 hits** for all 9 old names |
| Carve-out guard: `LeaderboardCalculator`, `HealthScoreCalculator` in `src/` | `grep -r "LeaderboardCalculator\|HealthScoreCalculator" src/` | Present in expected files; unchanged |
| Static call site `GetSprintSummaryEndpoint.cs:121` | `grep -n ComputeCrunchFlag GetSprintSummaryEndpoint.cs` | `TestTimelineAnalyzer.ComputeCrunchFlag(...)` — updated |
| `GetQaTrendsEndpoint.cs` all 3 occurrences (injection `:14`, comments `:98`, `:113`) | `grep -n "BugRatio" GetQaTrendsEndpoint.cs` | All 3 updated to `BugRatioAnalyzer` |
| `TestTimelineAnalyzer.cs:67` cross-ref comment | `grep -n SprintSummary TestTimelineAnalyzer.cs` | "reuse by SprintSummaryAnalyzer" — correct |
| `DeveloperDetailAnalyzer.cs:125` cross-ref preserved | `grep -n "mirrors" DeveloperDetailAnalyzer.cs` | `// --- ... (mirrors DeveloperProgressAnalyzer) ---` — unchanged |
| DI registration (`DependencyInjection.cs`) | `grep -n "Calculator\|Analyzer" DependencyInjection.cs` | All 9 `AddScoped<*Analyzer>()` present; `LeaderboardCalculator` untouched |
| gRPC ctor (`SprintAnalyticsGrpcService.cs`) | `grep -n "Calculator\|Analyzer" SprintAnalyticsGrpcService.cs` | 4 injected types updated to `*Analyzer`; local var names (e.g. `sprintSummaryCalculator`) correctly unchanged |
| Comment sites: `MetricCardBuilder.cs:6`, `QaMetricsResult.cs:27`, `TestTimelineResult.cs:90`, `MetricCardData.cs:5` | `grep -n "Calculator\|Analyzer" <file>` | All 4 non-renamed files updated; no old name remains |
| KB files: `bug-ratio.md`, `health-score.md` | `grep -n "Calculator" docs/kb/*.md` | `health-score.md` retains `HealthScoreCalculator` (carve-out — correct); all 9 renamed classes absent from both KB files |
| Pending test file names in `bug-ratio.md` | `grep -n "BugRatio" docs/kb/bug-ratio.md` | `BugRatioAnalyzerTests.cs` / `BugRatioAnalyzerPropertyTests.cs` — updated |
| ADR-005 illustrative path | `grep -n SprintSummary docs/architecture/decisions.md` | `Fokus.Domain/Analytics/SprintSummaryAnalyzer.cs` — updated |
| Wire-stability (`git diff` scope) | `git diff --name-only HEAD~1` | 31 files: 9 domain renames, 13 API reference sites, 4 comment-only domain records, 3 KB/ADR docs, 2 unrelated (`docs/backlog.md`, `.claude/settings.json`) — no route strings, `*Response`/`*Command`/`*Query` types, `Blocks.Contracts`, or SignalR symbols changed |

### Carry-Over Findings from implementation.md

| Finding | Disposition |
|---------|-------------|
| CRLF warnings from `git diff` (pre-existing Windows/Unix EOL) | **Confirmed pre-existing** — not introduced by this pass; not a finding |
| Coverage-workstream ripple: GOLD-19/GOLD-20 source-path citations for `DeveloperThroughputAnalyzer.cs` / `DeveloperDetailAnalyzer.cs` stale | **Confirmed out-of-scope per plan** — noted in plan Open Questions; owner/team-lead action when coverage workstream next runs |

### Findings

None. All 5 plan dimensions checked:

1. **Plan conformance** — all 5 steps executed as specified. Reference table exhaustively verified: 9 class renames, 13 API reference sites, 5 comment/doc sites, 2 KB files, 1 ADR path. No deviation from the plan.
2. **Correctness** — static call site updated (`TestTimelineAnalyzer.ComputeCrunchFlag`); `GetQaTrendsEndpoint.cs` dual-occurrence handled (injection + 2 comments); cross-ref comment at `:67` updated; `DeveloperDetailAnalyzer.cs:125` non-renamed cross-ref preserved verbatim; local variable names (e.g. `bugRatioCalculator`, `sprintSummaryCalculator`) correctly left unchanged throughout — these are parameter names, not type names.
3. **Security** — no surface exposed; pure rename.
4. **Performance** — no surface changed.
5. **Conventions** — naming per `analytics-computation-service` rule; file-scoped namespaces; all carve-out rules respected.
6. **Tests** — no dedicated tests existed for the 9 renamed classes (verified: only `HealthScoreCalculatorTests.cs` and `MetricCardBuilderPropertyTests.cs` in the test project, neither in scope); build is the conformance gate per plan, as stated.

### Verdict

**APPROVED**

Clean execution of a complex multi-site rename. The plan's tricky sites (dual-occurrence in `GetQaTrendsEndpoint.cs`, static call site in `GetSprintSummaryEndpoint.cs`, 5 comment-only non-renamed files, 2 pending test file names in KB) were all handled correctly. Build green; zero old names in `src/`; carve-outs intact; wire-stability confirmed.
