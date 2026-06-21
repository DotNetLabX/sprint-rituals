# FeatureOnlyMetrics — Summary

## Status: COMPLETE

## What Was Built
- Metrics (SP Completed, Completion %, Throughput, Burnup) now report feature-only values, excluding bug tickets. A "(+X bug SP)" annotation shows bug SP completed alongside the feature-only card.

## Key Outcomes
- 8 files modified (4 backend services, 3 frontend components, 1 types file)
- 4 KB entries updated
- Build passes (0 errors)
- Review verdict: APPROVED (1 fix cycle, 0 CRITICAL/HIGH findings)

## Deviations from Plan
- None

## Notes
- MEDIUM: `SprintMetrics.SpCompleted` field is now dead — consider removing in a follow-up cleanup.
- LOW: `BugSpCompleted` is not rounded to 1 decimal before serialization — cosmetic, consider fixing.
