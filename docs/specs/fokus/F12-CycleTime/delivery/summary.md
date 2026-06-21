# Cycle Time — Summary

## Status: COMPLETE

## What Was Built
Cycle Time analytics feature measuring how long completed tickets spend in each workflow stage. Includes a dedicated page with single-sprint drill-down (scatter plot, stage funnel, percentile metrics, breakdowns, outlier table) and multi-sprint trend view (P85 trend line, sprint summary table). Configurable cycle time boundaries in Settings. Three new API endpoints (GET/PUT boundaries, GET analytics) and a focused operation service implementing 22 business rules including sprint-start/end clamping, rework counting, per-sprint averaging, and unrecognized stage handling.

## Key Outcomes
- 15 files created (4 backend, 9 Vue components, 1 Pinia store, 1 view)
- 7 files modified (DI, types, 2 API modules, router, sidebar, settings view)
- 1 EF Core migration (2 nullable columns on AppSettings)
- Backend and frontend builds pass (0 errors)
- Review verdict: APPROVE after 2 cycles (1 HIGH + 1 MEDIUM fixed)

## Deviations from Plan
- `last` parameter uses `last=0` for "all sprints" (matching scope-change convention) instead of spec's `400 when last < 1`. Documented plan deviation, approved by architect.
- `GetCycleTimeBoundariesResponse` gained a `WorkflowStageCount` field (reviewer fix cycle 2) to correctly detect empty workflow stages for the BR13 empty state. Minor response shape addition, not a plan deviation.
- Unrecognized stage accumulation uses `__Other__` sentinel key internally (reviewer fix cycle 2) to implement BR21. Internal implementation detail, API contract unchanged.

## Notes
- The spec's `last < 1 = 400` error condition should be updated to reflect the `last=0 = all sprints` convention now shared by scope-change and cycle-time endpoints.
- Cycle time includes time in the end stage (per BR2 "inclusive"). If product intent changes, this is a one-line boundary adjustment in CycleTimeService.
- Tooltips are wired via native HTML `title` attributes following the help-tooltips rule. All entries from `docs/features/CycleTime/help.tooltips.md` are covered.
