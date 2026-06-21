# ActiveSprintAnalytics — Summary

## Status: COMPLETE

## What Was Built
Widened the analytics pipeline to include the active sprint alongside closed sprints. The sprint selector endpoint now returns all sprints (active + closed) with a `State` field. All 15 analytics endpoints were updated to use the renamed repository methods, maintain closed-only baselines/averages, and correctly handle active sprint queries. Frontend renamed from `ClosedSprintItem` to `SprintItem` across types, API, stores, components, and views.

## Key Outcomes
- 37 files modified (backend endpoints, repository, frontend types/stores/views/components, KB)
- Build: zero C# errors, zero TypeScript errors
- Review: APPROVED after 1 fix cycle (1 CRITICAL, 1 HIGH, 1 MEDIUM, 1 LOW — all resolved)

## Deviations from Plan
- GetScopeChangeEndpoint error message was incorrectly excluded from the plan's update list — architect corrected the plan during developer Q&A

## Notes
- The active sprint uses closed-only data for averages and baselines to avoid skewing metrics with incomplete sprint data
- KB entry `docs/kb/domain/sprint.md` updated with Analytics Query Scope section documenting the pattern
