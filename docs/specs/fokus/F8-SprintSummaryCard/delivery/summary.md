# Sprint Summary Card — Summary

## Status: COMPLETE

## What Was Built
The Dashboard's first analytics feature: a single-sprint summary view with composite health score (weighted from completion, disruption, and carry-over sub-scores with RAG thresholds), four metric cards with delta-to-prior-sprint and 4-sprint trailing sparklines, top epics with cross-sprint progress, developer leaderboard, and sprint flags (zombie tickets, mid-sprint disruption, zero-SP developers). Three new read-only endpoints were added (sprint summary, closed sprints list, sub-teams list) along with a pure computation service, and the frontend was fully built out with a Pinia store, reusable dashboard components, and URL-synced sprint selection.

## Key Outcomes
- 12 files created, 7 files modified
- Backend build: PASS (0 errors)
- Frontend build: PASS (0 errors)
- Review verdict: APPROVE (1 cycle — no CRITICAL or HIGH findings; 2 MEDIUM, 2 LOW)
- All 11 plan steps implemented with full conformance

## Deviations from Plan
- Step 3 validator: plan said "validate SubTeam is non-empty when provided"; implemented as `NotEmpty().When(x => x.SubTeam is not null)` which treats empty string as null/omitted. Aligns with plan intent.
- Step 5: `GetSubTeamsQuery.cs` created as a comment-only placeholder file (no request/response type needed beyond `List<string>`); kept consistent with the plan's two-file-per-endpoint pattern.

## Notes
- **Two MEDIUM findings from review (not blocking, but worth addressing before committing):**
  1. `BuildSparkline` in `SprintSummaryService.cs` has an unused `doneStatuses` parameter (the lambdas capture it via closure from the outer scope). The parameter and `allWindowSprints` can be removed from the method signature and its four call sites.
  2. `GetSprintSummaryEndpoint.cs:63` uses `windowSprints.First(...)` which throws `InvalidOperationException` if the selected sprint is absent from the bulk load (race condition — unlikely in single-user SQLite, but an unhandled 500). Replace with `FirstOrDefault` and a null guard returning 404.
- **Spec display format deviation:** The spec example showed SP Completed as "24 / 30 SP" (completed / committed fraction), but the implementation displays only the completed number (e.g., "24"). The plan did not specify the exact format string. Consider whether the fraction format would be more useful for users.
- **No automated test coverage exists.** The computation logic (health score interpolation, sparkline windowing, delta nullability) is tested only manually. This creates regression risk as F9-F14 inherit these patterns.
- **Error state not surfaced in UI.** The Pinia store tracks an `error` field but `DashboardView.vue` never renders it — network failures result in a blank page with no feedback.
