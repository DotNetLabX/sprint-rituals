# Scope Change & Disruption — Summary

## Status: COMPLETE

## What Was Built
Sprint-level scope change analytics for the Sprints page: a multi-sprint trend view (default, last 5 sprints) with disruption rate, scope change bars, and classification breakdowns, and a single-sprint detail view with metric cards (with deltas), scope burnup chart, chronological event table, and bug time-in-progress data. Also added a system-wide excluded-from-scope statuses setting with GET/PUT CRUD endpoints.

## Key Outcomes
- 18 files created, 9 files modified (backend: 6 created, 5 modified; frontend: 12 created, 4 modified)
- Build passes (0 errors, 3 warnings)
- Review verdict: APPROVE after 1 fix cycle (2 findings: 1 CRITICAL, 1 HIGH)

## Deviations from Plan
- None. All 10 plan steps implemented as specified. Review fixes were additive corrections only.

## Notes
- Migration fix: the EF-scaffolded migration originally had `defaultValue: ""` for the JSON column; corrected to `defaultValue: "[]"` with a safety-net UPDATE SQL for instances where the old migration was already applied.
- "All Sprints" sentinel: the frontend sends `last=0` to the backend (not undefined/null) to distinguish "all sprints" from "no parameter provided" (which defaults to last 5). The full chain (store -> API function -> validator -> endpoint) was verified end-to-end.
- Burnup chart transitions: the endpoint loads status transitions for all non-removed tickets (not just mid-sprint bugs) because the burnup completion line needs transition data for every ticket. The bug time-in-progress computation filters internally.
