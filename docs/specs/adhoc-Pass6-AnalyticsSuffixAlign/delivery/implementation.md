# Implementation — adhoc-Pass6-AnalyticsSuffixAlign

**Status:** Complete  
**Build:** Green (0 errors, 15 pre-existing warnings — NU1903 × 12, CS8629 × 1, CS8601 × 1, CS9107 × 1)  
**Grep-guard:** 0 hits for all 9 old names in `src/`; carve-outs intact

## Steps

### Step 1 — Rename the 9 domain calculator classes and files

**Files changed:** 9 domain files in `src/Services/Fokus/Fokus.Domain/Analytics/`

Renamed via `git mv` (preserves git history as renames):

| Old | New |
|-----|-----|
| `SprintSummaryCalculator.cs` | `SprintSummaryAnalyzer.cs` |
| `QaMetricsCalculator.cs` | `QaMetricsAnalyzer.cs` |
| `TestTimelineCalculator.cs` | `TestTimelineAnalyzer.cs` |
| `CarryOverCalculator.cs` | `CarryOverAnalyzer.cs` |
| `CycleTimeCalculator.cs` | `CycleTimeAnalyzer.cs` |
| `BugRatioCalculator.cs` | `BugRatioAnalyzer.cs` |
| `ScopeChangeCalculator.cs` | `ScopeChangeAnalyzer.cs` |
| `DeveloperThroughputCalculator.cs` | `DeveloperThroughputAnalyzer.cs` |
| `DeveloperDetailCalculator.cs` | `DeveloperDetailAnalyzer.cs` |

Class declaration at `:3` updated in each file to match. `TestTimelineAnalyzer.cs:67` cross-reference comment updated: "reuse by SprintSummaryCalculator" → "reuse by SprintSummaryAnalyzer". `DeveloperDetailAnalyzer.cs:125` comment "mirrors DeveloperProgressAnalyzer" preserved verbatim (non-renamed class — correct per plan).

No `*Result`/`*Data` records renamed. `LeaderboardCalculator.cs` and `HealthScoreCalculator.cs` untouched (carve-outs).

None of the 9 files have explicit constructors or `nameof(...)` self-references — only the class declaration line required editing per file (except `TestTimelineAnalyzer.cs` which needed the additional `:67` comment).

### Step 2 — Update every C# reference site

**Files changed:** 13 files in `Fokus.API`

**DI registration** (`DependencyInjection.cs:48-62`): all 9 `AddScoped<T>()` calls updated. `LeaderboardCalculator` DI registration left untouched.

**gRPC ctor** (`SprintAnalyticsGrpcService.cs:23-26`): 4 injected type names updated (SprintSummary, ScopeChange, CarryOver, QaMetrics). Parameter local variable names preserved unchanged.

**Endpoints — injection (type-only, local var names unchanged):**
- `GetSprintSummaryEndpoint.cs:12,14` — SprintSummary, QaMetrics
- `GetQaMetricsEndpoint.cs:13` — QaMetrics
- `GetTestTimelineEndpoint.cs:13` — TestTimeline
- `GetCarryOverEndpoint.cs:13` — CarryOver
- `GetCycleTimeEndpoint.cs:13` — CycleTime
- `GetBugRatioEndpoint.cs:13` — BugRatio
- `GetQaTrendsEndpoint.cs:14` — BugRatio (second injection site)
- `GetScopeChangeEndpoint.cs:13` — ScopeChange
- `GetDeveloperThroughputEndpoint.cs:13` — DeveloperThroughput
- `GetDeveloperDetailEndpoint.cs:15` — DeveloperDetail

**Static call** (`GetSprintSummaryEndpoint.cs:121`): `TestTimelineCalculator.ComputeCrunchFlag(...)` → `TestTimelineAnalyzer.ComputeCrunchFlag(...)`.

**Comment/doc sub-table (non-renamed files — text only):**
- `MetricCardBuilder.cs:6` — SprintSummaryCalculator, QaMetricsCalculator → Analyzer
- `QaMetricsResult.cs:27` — QaMetricsCalculator, SprintSummaryCalculator → Analyzer
- `TestTimelineResult.cs:90` — SprintSummaryCalculator → SprintSummaryAnalyzer
- `MetricCardData.cs:5` — CarryOver, ScopeChange, BugRatio, CycleTimeCalculator → Analyzer
- `GetQaTrendsEndpoint.cs:98` — BugRatioCalculator → BugRatioAnalyzer (code comment)
- `GetQaTrendsEndpoint.cs:113` — BugRatioCalculator → BugRatioAnalyzer (code comment)

`GetLeaderboardEndpoint.cs` not touched (Leaderboard stays Calculator). No test file modified (none exist for the 9 renamed classes).

### Step 3 — Build + grep-guard verification

`dotnet build SprintRituals.slnx` → **0 errors** (15 warnings, all pre-existing).

Grep `src/` for each of 9 old names → **0 hits**.

Grep `src/` for carve-out names → **still present** (LeaderboardCalculator: DI + endpoint + class file; HealthScoreCalculator: class file + test file + callers in SprintSummaryAnalyzer and QaMetricsAnalyzer).

### Step 4 — Wire-stability check

`git diff --stat` reviewed: 27 files changed. Every changed file is one of:
- A domain class file rename (9 files)
- An API reference site update (13 files in Fokus.API)
- A comment/doc-only update in a non-renamed domain record (4 files)
- A doc/KB update (1 file — `docs/backlog.md` was pre-changed by team-lead; the other 3 are in Step 5)

No changes to:
- HTTP route strings (all endpoint `[HttpGet(...)]` attributes unchanged)
- `*Response` / `*Command` / `*Query` type names
- `Blocks.Contracts` gRPC interfaces or proto message names (`ISprintAnalyticsService` is unchanged; only the implementing class's injected domain types changed)
- SignalR hub methods/groups/claims

Wire-stability confirmed.

### Step 5 — Update current KB entries + ADR-005 illustrative path

**`docs/kb/bug-ratio.md`**: Updated `BugRatioCalculator` → `BugRatioAnalyzer` at lines 4, 28, 43, 44, 45, 49. Updated pending test file names at lines 29-30: `BugRatioCalculatorTests.cs` → `BugRatioAnalyzerTests.cs`, `BugRatioCalculatorPropertyTests.cs` → `BugRatioAnalyzerPropertyTests.cs`.

**`docs/kb/health-score.md`**: Updated `SprintSummaryCalculator` → `SprintSummaryAnalyzer` and `QaMetricsCalculator` → `QaMetricsAnalyzer` at lines 4, 36, 37, 42. All `HealthScoreCalculator` references left unchanged (carve-out not renamed).

**`docs/architecture/decisions.md` ADR-005**: Illustrative path updated from `Fokus.Domain/Analytics/SprintSummaryCalculator.cs` to `Fokus.Domain/Analytics/SprintSummaryAnalyzer.cs`. Decision text otherwise unchanged.

No prior-pass delivery artifact edited.

## Skills Used

| Step | Skill | Invocation |
|------|-------|------------|
| 1 | None | Plan-sanctioned all-None |
| 2 | None | Plan-sanctioned all-None |
| 3 | None | Plan-sanctioned all-None |
| 4 | None | Plan-sanctioned all-None |
| 5 | None | Plan-sanctioned all-None |

All-None plan: `analytics-computation-service` cited as naming authority, not a Follow step. Empty `skill-invocations.log` is expected and correct (plan §Skill Mapping).

## Deviations

None. All changes follow the plan exactly. Local variable names in endpoint ctor parameters preserved (types renamed, names unchanged — matching existing code style for injected dependencies throughout the codebase).

## Carry-Over

- The CRLF warnings from `git diff` are pre-existing Windows/Unix EOL differences in the working tree — not introduced by this pass. No action needed.
- Coverage-workstream ripple: `GOLD-19` (cites `DeveloperThroughputCalculator.cs:252-268`) and `GOLD-20` (cites `DeveloperDetailCalculator.cs:58-59`) in `docs/audit/golden-set.md` and `docs/proposals/br-coverage-golden-set-draft.md` will need re-pointing to `DeveloperThroughputAnalyzer.cs` / `DeveloperDetailAnalyzer.cs` when the coverage workstream next runs. Out of scope for this pass per plan Open Questions.
