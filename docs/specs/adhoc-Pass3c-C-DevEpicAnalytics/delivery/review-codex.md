# Cross-check Review -- adhoc-Pass3c-C-DevEpicAnalytics

Reviewer: Codex (gpt-5.4) via codex-rescue subagent
Commit: 62cb009 (branch: main)
Date: 2026-06-08

---

## Verdict

**GO** -- all 6 structural checkpoints pass; one WARN on byte-identical JSON not being runtime-verified (inherent to static review scope, not a blocker).

---

## Findings

| Severity | File | Lines | Issue | Status |
|----------|------|-------|-------|--------|
| WARN | (runtime) | -- | Wire byte-parity confirmed structurally but not via live HTTP capture; DTO shapes and projections match source-to-source | WARN |
| INFO | Fokus.API/DependencyInjection.cs | 24-27 | Mapster auto-scan still enabled | PASS |
| INFO | GetDeveloperThroughputResponse.cs | 47-55 | Mapster config covers all nested domain-to-wire pairs | PASS |
| INFO | GetDeveloperProgressResponse.cs | 47-58 | Mapster config covers all nested domain-to-wire pairs | PASS |
| INFO | GetDeveloperDetailResponse.cs | 72-84 | Mapster config covers all nested domain-to-wire pairs | PASS |
| INFO | GetEpicProgressResponse.cs | 76-85 | Mapster config covers all nested domain-to-wire pairs | PASS |
| INFO | GetQaTrendsResponse.cs | 43-52 | Mapster config covers all nested domain-to-wire pairs | PASS |
| INFO | GetDeveloperQualityEndpoint.cs | 205-254 | Hand-written mapper projects every field from domain result into unchanged DTO | PASS |
| INFO | GetQaWorkloadEndpoint.cs | 208-338 | Hand-written mapper projects every envelope/entry/alert/sprint/card field | PASS |
| INFO | DeveloperThroughputCalculator.cs | 104-114 | spAssignedDeltaPolarity is literal "neutral", not routed through MetricCardBuilder | PASS |
| INFO | DeveloperQualityScorer.cs | 80-90 | bugsDeltaPol is literal "neutral", not routed through MetricCardBuilder | PASS |
| INFO | QaWorkloadAnalyzer.cs | 502-528 | tesPol/runsPol/bugsPol are literal "neutral", not routed through MetricCardBuilder | PASS |
| INFO | QaWorkloadAnalyzer.cs | 575-605 | Team-card "neutral" stored as final literal in local builder | PASS |
| INFO | Fokus.Domain/Analytics/ (all files) | -- | No using Fokus.API imports; no MetricCard/SparklinePoint/GetSprintSummary API wire types referenced | PASS |
| INFO | GetQaTrendsEndpoint.cs | 8-16, 98-156 | BugRatioCalculator still injected alongside QaTrendsAnalyzer; used before QaTrendsAnalyzer call | PASS |
| INFO | Fokus.API/DependencyInjection.cs | 44-49 | BugRatioCalculator DI registration present | PASS |

---

## Checkpoint Detail

### 1. Wire response shapes (byte-parity)

Evidence: All 7 HTTP routes still use the same DTO/response types. The 5 Mapster endpoint configs explicitly cover all nested domain-to-wire type pairs. The 2 hand-written mappers project every field into unchanged DTO shapes. No response field additions, removals, or renames detected in diff.

Caveat: This is a static source review. Live HTTP byte-capture was not performed. The structural evidence is sufficient for a GO.

Verdict: WARN (not a blocker -- inherent to static review scope)

---

### 2. Mapster maps + hand-written mappers (DeveloperQuality, QaWorkload)

Evidence:
- Mapster auto-scan: Fokus.API/DependencyInjection.cs:24-27 -- confirmed still enabled.
- 5 Mapster endpoint configs confirmed at:
  - GetDeveloperThroughputResponse.cs:47-55
  - GetDeveloperProgressResponse.cs:47-58
  - GetDeveloperDetailResponse.cs:72-84
  - GetEpicProgressResponse.cs:76-85
  - GetQaTrendsResponse.cs:43-52
- DeveloperQuality hand mapper: GetDeveloperQualityEndpoint.cs:205-254 -- all response fields projected from moved domain result into GetDeveloperQualityQuery.cs:35-88 DTO shapes (unchanged).
- QaWorkload hand mapper: GetQaWorkloadEndpoint.cs:208-338 -- all envelope, entry, alert, sprint, and card fields projected into GetQaWorkloadQuery.cs:37-177 DTO shapes (unchanged).

Verdict: PASS

---

### 3. Neutral-polarity carve-outs intact, not routed through MetricCardBuilder

Evidence:
- DeveloperThroughputCalculator.cs:104-114: spAssignedDeltaPolarity = "neutral" literal -- matches pre-refactor DeveloperThroughputService.cs:147-157 carve-out at :153.
- DeveloperQualityScorer.cs:80-90: bugsDeltaPol = "neutral" literal -- matches DeveloperQualityService.cs:125-134 carve-out at :134.
- QaWorkloadAnalyzer.cs:502-528: tesPol, runsPol, bugsPol all literal "neutral" -- matches QaWorkloadService.cs:604-629 carve-outs at :605, :609, :629.
- QaWorkloadAnalyzer.cs:575-605: team-card "neutral" stored as final literal in local builder -- matches QaWorkloadService.cs:676-707.

All 5 carve-outs confirmed intact and bypass MetricCardBuilder.

Verdict: PASS

---

### 4. No behavior drift in moved calculator logic

Evidence (per calculator):
- DeveloperThroughputCalculator (DeveloperThroughputCalculator.cs:104-147): changes limited to result-type renames + delta-helper swap to MetricCardBuilder.Direction/Polarity() -- semantics match deleted helpers at DeveloperThroughputService.cs:317-331 and MetricCardBuilder.cs:69-95.
- DeveloperProgressAnalyzer (DeveloperProgressAnalyzer.cs:15-331): pure type-rename move, no computation edits -- matches DeveloperProgressService.cs:63-380.
- DeveloperDetailCalculator (DeveloperDetailCalculator.cs:18-442): pure type-rename move, same cross-sprint/current-sprint/ticket-state logic -- matches DeveloperDetailService.cs:81-514.
- DeveloperQualityScorer (DeveloperQualityScorer.cs:395-439): local ResolveStartIndex/IsStartedInSprint/GetStageIndex helpers preserved -- matches DeveloperQualityService.cs:463-499; delta-helper swap matches MetricCardBuilder.cs:69-95.
- EpicProgressAnalyzer (EpicProgressAnalyzer.cs:15-527): type-rename move -- matches EpicProgressService.cs:85-605.
- QaTrendsAnalyzer (QaTrendsAnalyzer.cs:185-333): local start-stage helpers preserved -- matches QaTrendsService.cs:223-371; only result-type renames beyond that.
- QaWorkloadAnalyzer (QaWorkloadAnalyzer.cs:575-605): team-card delta logic preserved -- matches QaWorkloadService.cs:676-707; flat per-person deltas call MetricCardBuilder.Direction/Polarity() matching deleted helpers at QaWorkloadService.cs:711-717.

Verdict: PASS

---

### 5. Domain files reference no Fokus.API wire types

Evidence:
- grep "^using Fokus.API" src/Services/Fokus/Fokus.Domain/Analytics: no matches.
- grep "MetricCard|SparklinePoint" src/Services/Fokus/Fokus.Domain/Analytics: no matches.
- grep "GetSprintSummary" src/Services/Fokus/Fokus.Domain/Analytics: no matches.
- Some domain result files contain comments referencing API file locations (e.g. DeveloperThroughputResult.cs:3-4) -- comments only, no code-level dependency.

Verdict: PASS

---

### 6. BugRatioCalculator still injected in GetQaTrends

Evidence:
- GetQaTrendsEndpoint.cs:8-16: BugRatioCalculator injected alongside QaTrendsAnalyzer in constructor.
- GetQaTrendsEndpoint.cs:98-156: injected calculator computes bugRatioResult and bugRatioBySprintId before QaTrendsAnalyzer call -- not inlined.
- Fokus.API/DependencyInjection.cs:44-49: BugRatioCalculator DI registration present.

Verdict: PASS

---

## Summary

The refactor at commit 62cb009 correctly implements the ADR-005/006/010 boundary shift. All 7 analytics computations now live in domain calculators with domain-only result types; API endpoints retain wire ownership; the 2 hand-written mappers and 5 Mapster configs project every field unchanged; all 5 "neutral" carve-outs are intact and bypass MetricCardBuilder; BugRatioCalculator remains injected (not inlined) in GetQaTrends; and no domain file imports Fokus.API wire types. The sole WARN -- byte-identical JSON not runtime-captured -- is inherent to static source review and does not affect the GO verdict.
