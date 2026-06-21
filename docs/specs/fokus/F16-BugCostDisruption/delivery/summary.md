# Bug Cost & Disruption Split -- Summary

## Status: COMPLETE

## What Was Built
Configurable default SP per bug (0-13, default 3) applied system-wide as a query-time fallback for unestimated bug tickets. Dashboard disruption rate split into two cards (Scope Disruption Rate + Bug Disruption Rate) with independent sparklines and deltas. All six analytics services (SprintSummary, ScopeChange, CarryOver, BugRatio, DeveloperThroughput, EpicProgress) now use a centralized `GetEffectiveSp` method for SP calculations.

## Key Outcomes
- 2 files created (EF migration + designer)
- 26 files modified (1 domain entity, 1 domain behavior, 4 settings endpoint files, 6 analytics services, 5 frontend files, 9 KB entries)
- Build passes
- Review verdict: APPROVED after 1 fix cycle
- Fix cycle addressed: ComputeTopEpics second phase using raw StoryPoints instead of effective SP (HIGH), and tooltip text alignment to spec verbatim (LOW)

## Deviations from Plan
- Step 13 (endpoint callers): No code changes needed. All analytics services already accept the full `AppSettings` object and extract `DefaultSpPerBug` internally. The plan assumed services would receive a new parameter, but the existing signature was sufficient.

## Notes
- EF migration required manual correction: `defaultValue` changed from 0 to 3, plus SQL UPDATE for existing rows. EF Core always generates 0 for int column defaults.
- The `GetEffectiveTicketSp` helper was duplicated in both `SprintSummaryService` (for ComputeTopEpics) and `EpicProgressService` (for epic-level SP). Both are private static methods with identical logic. If a third consumer appears, consider extracting to a shared location.
