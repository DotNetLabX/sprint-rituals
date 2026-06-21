# NormalizedCapacityIndicator — Summary

## Status: COMPLETE

## What Was Built
Normalized SP indicators that show a capacity-adjusted story point value (~X) next to raw SP completed for developers working at less than 100% capacity. Displayed across three surfaces: Developer Throughput tab, Leaderboard tab, and Dashboard leaderboard widget.

## Key Outcomes
- 4 backend files modified (LeaderboardService, SprintSummaryService, GetLeaderboardEndpoint, GetSprintSummaryEndpoint)
- 5 frontend files modified (types, DevelopersView, LeaderboardTable, DashboardView, InfoTooltip wired)
- 1 KB entry updated (docs/kb/analytics/leaderboard.md)
- Build passes (0 errors), TypeScript passes (0 errors)
- Review verdict: APPROVED after 1 fix cycle (KB entry update)

## Deviations from Plan
- None

## Notes
- Multi-sprint normalization uses per-sprint normalize-then-average (BR6), not average-then-normalize
- Tooltips wired from help.tooltips.md to all (~X) indicator spans and column headers
