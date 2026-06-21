# Bug Ratio (F13) — Summary

## Status: COMPLETE

## What Was Built
Bug Ratio tab on the Developers page, showing per-developer and team-level bug ratio metrics (Bug SP vs Non-Bug SP) with trend charts, alert badges for developers exceeding a configurable threshold for consecutive sprints, and issue type breakdowns. Two new AppSettings properties (alert threshold, consecutive sprint count) with full settings round-trip.

## Key Outcomes
- 10 files created, 12 files modified
- Backend and frontend builds pass
- Reviewer approved after 1 fix cycle (4 issues: 2 HIGH, 1 MEDIUM, 1 LOW — all fixed)

## Deviations from Plan
- Stacked bar chart uses team-level aggregation (Bug SP total vs Non-Bug SP total per sprint) instead of per-developer breakdown. Rationale: more readable at team scale; per-developer data available in the table.

## Notes
- EF migration was generated with defaultValue: 0 for both new int columns — manually corrected to 50 and 2 to match domain defaults. Without this fix, existing AppSettings rows would have threshold=0 (alerting all developers) and consecutiveCount=0 (impossible to satisfy).
- F9 (Developer Throughput) does not apply excluded-from-scope statuses. The Throughput tab and Bug Ratio tab on the same Developers page may show different total ticket counts when exclusions are configured. This is a known gap tracked as a separate backlog item.
