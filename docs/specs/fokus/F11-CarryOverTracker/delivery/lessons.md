# Carry-Over Tracker — Lessons

## Developer Lessons

- When a service needs to compute per-item sprint count from a full history, returning a `Dictionary<string, int>` from a single LINQ pass (GetZombieTicketKeys) is cleaner than recomputing per ticket. Avoids O(n²) when iterating carry-over tickets.
- Duplicating a private helper (BuildMetricCard) between two services in the same namespace is preferable to coupling the services, as the plan explicitly recommended. The 15-line helper is stable and unlikely to diverge.
- Vue stacked bar chart (ApexCharts): when stage names vary per sprint, collect the union of stage names across all perSprintData entries first, then map each sprint's data to 0 when a stage is absent. Otherwise missing data points break the stacked series.

## Reviewer Lessons

- When reviewing BR3 (excluded-status exclusions), check every metric computation path individually — SP exclusion and count/percentage exclusion are separate code paths and one may be missed while the other is implemented. In CarryOverService, SP was excluded correctly but the percentage denominator in BuildStatusDistribution still counted excluded tickets.
- `BuildCarryOverDestination` takes `excludedStatuses` indirectly via `priorMemberships` being pre-filtered, but SP summation within the destination method needs the excluded list explicitly to filter SP correctly. Always trace whether list-level filtering and value-level filtering are both applied.
- The feature spec said `last < 1 → 400`; the plan overrode this to `last >= 0` with `0 = all`. When spec and plan disagree, the plan is the contract for implementation review — flag the deviation as open question for architect, not as a code bug.

## Architect Lessons

- Step 1 plan item 6 (issue type breakdown) said "Exclude tickets with excluded-from-scope statuses from issue type counts per BR3" — the developer initially interpreted this as excluding from SP only, not from counts/percentages. The instruction was technically correct but the word "counts" was buried in a dense paragraph with 6 other computation items. For complex computation services, break exclusion rules into their own clearly labeled sub-section rather than embedding them inline with grouping logic.
- The critic flagged that the plan referenced "matches F8" for the ScopeChangeService pattern, but F8 predates excluded-from-scope (which was added in F10). The computation instructions were correct since they referenced the actual code, but the feature citation was stale. When citing pattern references, cite the file path (stable) rather than a feature number (can become misleading as features layer on top of each other).
- Step 1 done check was straightforward for this feature because implementation.md was thorough and matched plan structure 1:1. The "Post-Review Fixes" section in implementation.md was a clean pattern for documenting reviewer-driven changes without muddying the original implementation record.

## Skill Gaps

- **Missing skill:** Frontend analytics component patterns (charts, metric cards, tables). Steps 3–6 had no matching skill. Suggested name: `vue-analytics-components`. Coverage: ApexCharts integration pattern, BaseCard usage, delta coloring, table grouping with subtotals. Reference files: `ScopeMetricCards.vue`, `ScopeChangeChart.vue`, `ClassificationTable.vue`.
