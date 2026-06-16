# Codex Cross-Check — adhoc-Pass6-AnalyticsSuffixAlign

**Reviewer:** Codex (independent cross-check)  
**Date:** 2026-06-16  
**Verdict:** GO

## Summary

Every claim in implementation.md was verified against the live working tree. Zero old-name `*Calculator` symbols remain in `src/`. All 9 new `*Analyzer` files are present with correct class declarations. All 22 reference sites (DI, gRPC ctor, 10 endpoints, 1 static call, 5 comment/doc sites, 3 doc files) were individually confirmed. No carve-out violated. No wire-visible identifier changed. No `*Result`/`*Data` record renamed.

---

## Findings

### Completeness

| # | Check | Result |
|---|-------|--------|
| C1 | 9 new `*Analyzer` files present in `Fokus.Domain/Analytics/` | PASS |
| C2 | 9 old `*Calculator` files absent | PASS — glob returns only `LeaderboardCalculator.cs` and `HealthScoreCalculator.cs` |
| C3 | Zero hits for all 9 old names in `src/` (C# files) | PASS — grep returns 0 matches |
| C4 | Zero hits for all 9 old names in `docs/kb/` | PASS |
| C5 | DI: all 9 `AddScoped<*Analyzer>()` present; `AddScoped<LeaderboardCalculator>()` untouched | PASS — verified `DependencyInjection.cs:48-62` |
| C6 | gRPC ctor: 4 injected types updated (SprintSummary, ScopeChange, CarryOver, QaMetrics) | PASS — verified `SprintAnalyticsGrpcService.cs:23-26` |
| C7 | Static call: `TestTimelineAnalyzer.ComputeCrunchFlag(...)` at `GetSprintSummaryEndpoint.cs:121` | PASS |
| C8 | BugRatio second injection: `BugRatioAnalyzer bugRatioCalculator` at `GetQaTrendsEndpoint.cs:14` | PASS |
| C9 | All 10 endpoint ctor-param type names updated; local var names preserved (`...Calculator` local name, type renamed) | PASS |
| C10 | `TestTimelineAnalyzer.cs:67` cross-ref comment: "reuse by SprintSummaryAnalyzer" | PASS |
| C11 | `DeveloperDetailAnalyzer.cs:125` comment "mirrors DeveloperProgressAnalyzer" preserved verbatim | PASS |
| C12 | `MetricCardBuilder.cs:6` comment: `SprintSummaryAnalyzer, QaMetricsAnalyzer` | PASS |
| C13 | `QaMetricsResult.cs:27` comment: `QaMetricsAnalyzer and SprintSummaryAnalyzer` | PASS |
| C14 | `TestTimelineResult.cs:90` comment: `SprintSummaryAnalyzer` | PASS |
| C15 | `MetricCardData.cs:5` comment: `CarryOverAnalyzer, ScopeChangeAnalyzer, BugRatioAnalyzer, CycleTimeAnalyzer` | PASS |
| C16 | `GetQaTrendsEndpoint.cs:98` comment: `BugRatioAnalyzer` | PASS |
| C17 | `GetQaTrendsEndpoint.cs:113` comment: `BugRatioAnalyzer` | PASS |
| C18 | `docs/kb/bug-ratio.md`: `BugRatioAnalyzer` at lines 4, 28, 43-45, 49; pending test file names updated to `BugRatioAnalyzerTests.cs` / `BugRatioAnalyzerPropertyTests.cs` | PASS |
| C19 | `docs/kb/health-score.md`: `SprintSummaryAnalyzer` and `QaMetricsAnalyzer`; `HealthScoreCalculator` references untouched | PASS |
| C20 | `docs/architecture/decisions.md` ADR-005: illustrative path updated to `SprintSummaryAnalyzer.cs` | PASS |

### Accidental over-reach (carve-outs and `*Result`/`*Data` records)

| # | Check | Result |
|---|-------|--------|
| O1 | `LeaderboardCalculator.cs` present and unchanged (class decl, DI registration, endpoint injection) | PASS |
| O2 | `HealthScoreCalculator.cs` present and unchanged; test file untouched | PASS |
| O3 | All 15 `*Result.cs` files retain original names — no `*AnalyzerResult` variant found | PASS |
| O4 | `DeveloperThroughputResult.cs` and `DeveloperDetailResult.cs` not renamed | PASS |
| O5 | `MetricCardBuilder.cs` not renamed (comment updated only) | PASS |
| O6 | `QaMetricsResult.cs`, `TestTimelineResult.cs` not renamed (comment updated only) | PASS |
| O7 | No `*Response` / `*Command` / `*Query` type renamed | PASS |

### Behavior / wire change (zero-change invariant)

| # | Check | Result |
|---|-------|--------|
| W1 | HTTP route strings unchanged — e.g. `/api/analytics/developer-throughput`, `/api/analytics/developer-detail/{accountId}` confirmed in endpoint attributes | PASS |
| W2 | `Blocks.Contracts` (`ISprintAnalyticsService`, proto messages) — zero old-name hits in `src/BuildingBlocks/` | PASS |
| W3 | gRPC contract not modified; only the implementing class's injected domain types changed | PASS |
| W4 | No SignalR hub method/group/claim string changed | PASS — no Calculator-named symbol appeared in any hub file; grep confirms |
| W5 | No method body logic changed — local variable names in endpoint handlers preserved (e.g. `developerThroughputCalculator`, `developerDetailCalculator`); only type names in ctor parameter lists updated | PASS |
| W6 | `DeveloperDetailAnalyzer.cs:109` calls `developerDetailCalculator.ComputeDetail(...)` — instance method on the injected var, not a renamed type call | PASS |

---

## Notes (non-blocking)

- **Coverage-workstream ripple** (carry-over, as documented in implementation.md): `docs/audit/golden-set.md` GOLD-19/GOLD-20 cite `DeveloperThroughputCalculator.cs` and `DeveloperDetailCalculator.cs` with stale paths. Left out of scope per plan Open Questions. Action owed by coverage workstream owner.
- Implementation.md line 59 says `MetricCardData.cs:5` updated "CycleTimeCalculator → Analyzer" — the live file at line 5 reads `CarryOverAnalyzer, ScopeChangeAnalyzer, BugRatioAnalyzer, CycleTimeAnalyzer`, which is correct. The notation in implementation.md was slightly informal but the actual content is accurate.
- No prior-pass delivery artifacts were edited (confirmed by absence of old names in `docs/specs/*/delivery/` — not checked exhaustively, but the live `src/` and current `docs/kb/` and `docs/architecture/` are all clean).
