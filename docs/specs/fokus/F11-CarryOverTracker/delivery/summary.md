# Carry-Over Tracker — Summary

## Status: COMPLETE

## What Was Built
Carry-over analytics (F11) for the Sprints page — a new GET endpoint and 9 Vue components that show which tickets didn't finish, where they're stuck, how long they've been stuck (zombie detection), and whether prior carry-over tickets eventually got completed. Supports multi-sprint trend view and single-sprint detail view with sub-team filtering.

## Key Outcomes
- 12 files created, 5 files modified
- Backend build passed, frontend TypeScript check clean
- APPROVED after 1 fix cycle (BR3 excluded-from-scope in issue type breakdown, BR20 flat table fallback)

## Deviations from Plan
- None

## Notes
- No new migrations or domain model changes — purely query-side analytics over existing data
- The `fetchScopeChange` store action was renamed to `fetchAllData` (fetches scope change + carry-over in parallel); callers use unchanged store actions (`selectSprint`, `selectLastN`, `selectSubTeam`)
