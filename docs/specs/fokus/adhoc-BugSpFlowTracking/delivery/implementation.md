# Bug SP Flow Tracking — Implementation

## Files Modified
- `src/Services/Fokus/Fokus.API/Features/Analytics/ScopeChangeService.cs` — Added `bugCompletedToday` computation in `BuildBurnupData` that subtracts completed bug SP (first done transition) from cumulative bug SP each day. Changes Bug SP from scope membership tracking to remaining/open bug flow tracking.
- `docs/features/BurnupBugOverlay/spec.md` — Marked revision (2026-05-10) as Implemented.

## Key Decisions
- Reused the existing `doneTransitionByTicket` lookup with a `IssueType == "Bug"` filter rather than building a separate bug-specific lookup. Keeps the code consistent with how `completedToday` works.

## Deviations from Plan
- None. No plan existed — conversation was the plan per solo workflow.
