# F26-SprintTestCoverage — Summary

## Status: COMPLETE

## What Was Built
Sprint Test Coverage feature — QA metrics dashboard section showing coverage rate, execution rate, pass rate, and bugs found per sprint, integrated into the health score as a Quality sub-score. Includes three new API endpoints, settings UI for QA thresholds and weights, and full frontend dashboard integration with RAG coloring, sparklines, deltas, and expandable untested/failing ticket lists.

## Key Outcomes
- 20+ files created, 15+ files modified across backend and frontend
- Build passes (0 errors — both .NET and TypeScript)
- Phase 1 (backend) review: APPROVED after 1 fix cycle (HIGH: missing ThenInclude for bug count)
- Phase 2 (frontend) review: APPROVED with post-approval MEDIUM fixes (bugs-found hidden when 0, failing ticket fields, redundant param)
- Architect Step 1 done check: Phase 1 passed first try, Phase 2 passed after 1 fix round (FailingTicket field name mismatch)

## Deviations from Plan
- TransitionAttributionChecker logic inlined in Persistence layer (cross-layer constraint)
- `defaultSpPerBug` parameter added to `GetFeatureTicketsWithCoverageAsync` (necessary for GetEffectiveSp)
- hasQaData simplified to `XrayEnabled == true` (matches user decision, simpler than plan's "synced state" detection)
- `fetchQaMetrics()` called from DashboardView directly instead of inside `fetchSummary()`
- QaMetricsSection receives data props instead of `xrayEnabled` flag

## Notes
- User decided: QualityHealthWeight=0 shows observation mode (Quality line visible but excluded from composite)
- User decided: Zero TEs after sync → hasQaData=true, quality sub-score is 0% and included in composite
- Frontend not visually tested — dev server was not started. Recommend manual QA before committing.
