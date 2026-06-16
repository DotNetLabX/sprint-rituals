# adhoc-Pass6-AnalyticsSuffixAlign — Summary

**Status:** Complete — 2026-06-16
**Branch:** main
**Team mode:** Standard+Codex
**Outcome:** Done-check PASS · Reviewer APPROVED · Codex GO (dual-review converged, zero findings, zero fix cycles)

## What shipped

Retro-applied the `analytics-computation-service` "name by what it computes" rule to the earlier `*Calculator` analytics classes in `Fokus.Domain/Analytics/`, making the folder consistent. Pure internal rename — zero behavior change, zero wire impact (no route, DTO, gRPC contract, or SignalR string touched).

**9 classes renamed** (`*Calculator` → `*Analyzer`, via `git mv`):
`SprintSummary`, `QaMetrics`, `TestTimeline`, `CarryOver`, `CycleTime`, `BugRatio`, `ScopeChange` (the original 3c-A/3c-B sweep) + `DeveloperThroughput`, `DeveloperDetail` (added mid-run per Q1).

**2 carve-outs kept `*Calculator`** (deliberate): `LeaderboardCalculator` (raw per-developer SP/ticket aggregation) and `HealthScoreCalculator` (static scoring-helper, active coverage target — owner Q3).

After this pass, every `*Calculator` left in `Analytics/` is an intentional carve-out.

## Scope decision (Q1)

The developer's Phase-1 analysis found two 3c-C classes (`DeveloperThroughputCalculator`, `DeveloperDetailCalculator`) still carrying `*Calculator` and excluded from the original 7-class plan. The architect classified both (line-grounded, source read in full) as **mislabeled** — they compute deltas/direction/polarity/rolling-average/burnup/stall surfaces, not raw aggregation, so the rule routes them to `Analyzer`. Owner accepted the sweep; the plan was amended to 9 renames and the developer re-analyzed before implementing. See `questions.md` Q1.

## Changes

- **Domain:** 9 file+class renames in `src/Services/Fokus/Fokus.Domain/Analytics/`.
- **API:** reference sites updated — DI (`DependencyInjection.cs`), gRPC ctor (`SprintAnalyticsGrpcService.cs`), 10 endpoints, 1 static call (`GetSprintSummaryEndpoint.cs:121` → `TestTimelineAnalyzer.ComputeCrunchFlag`).
- **Comment/doc-only sites** in non-renamed files: `MetricCardBuilder.cs`, `MetricCardData.cs`, `QaMetricsResult.cs`, `TestTimelineResult.cs`.
- **Docs:** `docs/kb/bug-ratio.md`, `docs/kb/health-score.md`, ADR-005 illustrative path in `docs/architecture/decisions.md`.

## Verification

- `dotnet build SprintRituals.slnx` — **0 errors** (15 pre-existing warnings).
- Grep-guard: **0 hits** for all 9 old names in `src/`.
- Carve-outs intact; all 15 `*Result.cs` records untouched; wire-stability confirmed by both reviewers.

## Follow-ups / carry-overs (out of scope, owed)

1. **Coverage-workstream ripple** — `golden-set.md` GOLD-19/GOLD-20 and `br-coverage-golden-set-draft.md` cite the old `DeveloperThroughput*`/`DeveloperDetail*` `Calculator` paths; these go stale after the rename and must be re-pointed when the coverage workstream next runs. Left out of scope per plan (another workstream's audit/proposal docs; the research-raw mining file is immutable). **Action owed:** notify the coverage-workstream owner of `BugRatioCalculator → BugRatioAnalyzer`, `DeveloperThroughputCalculator → DeveloperThroughputAnalyzer`, `DeveloperDetailCalculator → DeveloperDetailAnalyzer`.
2. **Backlog row-6 status** — deferred (not committed by this run): `docs/backlog.md` carried an unrelated uncommitted change (RA4 / SprintContextPanel) from a parallel session at run time, so this run did not touch it to avoid clobbering parallel work. Mark Pass-6 row Done when that file is next committed.

## Commits

- `bc9f04a` — `feat(adhoc-Pass6-AnalyticsSuffixAlign): add implementation plan` (plan, amended to 9 classes)
- (implementation commit — see git log)
