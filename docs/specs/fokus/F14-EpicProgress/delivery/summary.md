# Epic Progress (F14) — Summary

## Status: COMPLETE

## What Was Built
The Epics page — a cumulative epic progress analytics feature that shows total scope (including backlog tickets), dual completion tracking (ticket count and story points), imputed SP for unestimated tickets, epic-level velocity (rolling 3-sprint average), projected sprints to completion, and per-ticket drill-down. It also aligned F8's Top Epics completion calculation with the cumulative ticket-status-based approach so numbers match across pages.

## Key Outcomes
- 12 files created, 5 files modified
- Backend: EpicProgressService, GetEpicProgress endpoint, repository query methods, F8 ComputeTopEpics alignment
- Frontend: epicsStore, EpicsView (replaced placeholder), EpicSummaryCards, EpicTable, EpicTicketTable, EpicActiveCompletedToggle, TypeScript types, API function
- Shared component: PageToolbar gained showSprintSelector prop
- Backend build: PASS (0 errors)
- Frontend build: PASS (0 errors, 0 type errors)
- Review verdict: APPROVE after 2 cycles (2 HIGH findings in cycle 1, both fixed in cycle 2)

## Deviations from Plan
- GetTicketsWithoutEpicInSprintsAsync used an EF Core Select projection instead of .Include(t => t.Assignee) — this omitted AssigneeId and Assignee, silently breaking sub-team filtering on unlinked work. Fixed in cycle 2 per reviewer finding.
- Sub-team filter tooltip on PageToolbar was missed in the initial implementation pass. Added in cycle 2.

## Notes
- No EF Core migration required — feature queries existing data only.
- The reviewer flagged an open question about whether active sprint count (BR17) should be scoped by sub-team filter. The current implementation scopes it (consistent with BR13 "all computations restrict"), which is the defensible reading. If users expect unscoped sprint count, this would be a future spec clarification.
