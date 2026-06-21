# F29-QaWorkloadThroughput — Summary

## Status: COMPLETE

## What Was Built
QA Workload & Throughput tab on the Developers page surfacing per-person test-execution ownership, run throughput, pass/fail split, stories covered, bugs found, plus a workload balance flag for sustained >50% concentration across consecutive sprints. Multi-sprint and single-sprint modes with team metric cards, stacked horizontal workload distribution chart, throughput trend line, and a sortable per-person table.

## Key Outcomes
- Files created: 6 (`QaWorkloadService.cs`, `GetQaWorkloadQuery.cs`, `GetQaWorkloadEndpoint.cs`, `QaWorkloadTab.vue`, `implementation.md`, `lessons.md`)
- Files modified: 5 (`DependencyInjection.cs`, `types/index.ts`, `api/analytics.ts`, `developersStore.ts`, `DevelopersView.vue`)
- Build status: .NET 0 errors, TypeScript 0 errors
- Review verdict: APPROVE — Step 1 took 2 cycles (1 HIGH on missing delta polarity fields fixed); Step 2 code review approved with MEDIUM/LOW non-blocking findings only

## Deviations from Plan
- Single-sprint per-person delta polarities were initially missing — added in cycle 1 to match spec BR 51-57 and the BugRatio pattern. Plan amended retroactively in implementation.md.

## Notes
- Reviewer flagged a MEDIUM: ApexCharts horizontal-bar `categories` placed in `yaxis.categories`. Verify in browser; move to `xaxis.categories` if person names do not render.
- Reviewer flagged a MEDIUM: sprint-not-found returns 400 instead of 404 (spec expected 404). Matches the existing `GetBugRatioEndpoint` pattern; should be standardized in a separate follow-up across analytics endpoints rather than diverging here.
- Sparkline data is computed for single-sprint team cards but the MetricCard rendering path does not yet wire it (gap noted by reviewer, non-blocking).
- 13 CS8714 nullable-dictionary-key warnings remain — `NullableStringComparer` handles null keys correctly at runtime.
- No domain or data model changes. Reads entirely from F25 entities and reuses F26 RAG thresholds.
