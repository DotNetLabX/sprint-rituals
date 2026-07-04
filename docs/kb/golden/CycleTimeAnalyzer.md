# Canonical Rule Registry — CycleTimeAnalyzer

One row per canonical rule (SddLifecycle C1). Deprecate-never-delete: a retired/superseded row is kept, never removed. `source:` and `last_verified` are mandatory on every row.

| Rule | Layer | Criticality | Arms | Bucket | Disposition | Source | Last Verified |
|------|-------|-------------|------|--------|-------------|--------|---------------|
| StartStageDefaultSecondWorkflowStage | domain-calc | core | both | overlap-confirmed | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| EndStageDefaultFirstDoneStatus | domain-calc | core | both | overlap-confirmed | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| DeveloperBreakdownAlphabeticalOrder | domain-calc | core | both | overlap-confirmed | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| OutlierTableSortedDescending | domain-calc | core | both | overlap-confirmed | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| SubTeamFilterScopesComputation | domain-calc | core | both | overlap-confirmed | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| ReworkZeroWithoutReEntry | domain-calc | core | both | overlap-confirmed | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| CompletedMembershipGate | domain-calc | golden | both | overlap-confirmed | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| BoundaryStagesInclusive | domain-calc | golden | both | overlap-confirmed | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| PerStageAccumulationAcrossVisits | domain-calc | golden | both | overlap-confirmed | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| NeverEnteredStartStageExcluded | domain-calc | golden | both | overlap-confirmed | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| OutlierStrictlyAboveTwiceMedian | domain-calc | golden | both | overlap-confirmed | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| PercentilesFromCompletedSet | domain-calc | core | both | overlap-confirmed | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| FunnelWithinBoundaryStages | domain-calc | core | both | overlap-confirmed | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| DominantStageHighestAccumulated | domain-calc | core | both | overlap-confirmed | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| DeltaNullWithoutPrior | domain-calc | core | both | overlap-confirmed | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| DeltaPolarityPerMetric | domain-calc | core | both | overlap-confirmed | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| MultiSprintPerSprintAveraging | domain-calc | golden | both | overlap-confirmed | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| MultiSprintDeltaLastVsPrev | domain-calc | core | both | overlap-confirmed | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| SprintOrderingByStartDate | domain-calc | core | both | overlap-confirmed | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| SprintStartClamping | domain-calc | core | both | overlap-confirmed | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| SprintEndClamping | domain-calc | core | both | overlap-confirmed | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| CompletionDateEarliestDoneInWindow | domain-calc | core | both | overlap-confirmed | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| PreSprintDoneExcluded | domain-calc | edge | both | overlap-confirmed | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| UnrecognizedStagesGroupedOther | domain-calc | edge | both | overlap-confirmed | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| cycle-start-stage-must-reference-configured-stage | api | core | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| cycle-end-stage-must-reference-stage-or-done-status | api | core | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| sprint-selector-defaults-most-recent-closed | ui | core | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| scatter-plot-reference-lines-p50-p85 | ui | core | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| scatter-plot-hover-content | ui | core | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| rework-badge-display | ui | core | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| p85-trend-one-point-per-sprint | ui | core | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| sprint-summary-row-navigation | ui | core | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| subteam-all-removes-filter | ui | core | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| sprint-range-selection-view-switch | ui | core | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| url-reflects-sprint-selection | ui | core | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| cycle-starts-dropdown-population | ui | core | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| cycle-ends-dropdown-population | ui | core | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| boundaries-saved-via-dedicated-endpoint | api | core | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| boundaries-recalculate-on-next-visit | ui | core | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| percentile-toggle-options | ui | core | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| percentile-toggle-default-p85 | ui | core | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| percentile-toggle-updates-reference-line-and-card | ui | core | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| p50-line-always-visible | ui | core | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| cycle-time-endpoints-no-auth | api | core | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| get-boundaries-response-content | api | core | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| get-boundaries-always-200 | api | core | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| get-boundaries-defaults-when-unconfigured | api | core | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| get-boundaries-empty-when-no-stages | api | edge | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| put-boundaries-400-invalid-stage | api | core | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| put-boundaries-400-start-not-preceding-end | api | core | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| sprintid-last-mutually-exclusive-400 | api | core | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| last-below-one-400 | api | core | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| subteam-param-optional-empty-means-all | api | core | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| no-params-defaults-all-closed-sprints | api | core | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| frontend-default-sends-sprintid | ui | core | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| sprintid-returns-single-last-returns-multi | api | core | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| sprintid-unmatched-400 | api | core | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| sprint-null-when-no-closed-sprints | api | edge | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| null-sprint-is-200-not-error | api | edge | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| metriccards-null-when-sprint-null | api | edge | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| scatterplot-empty-when-no-completed-tickets | api | edge | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| stagefunnel-empty-when-no-completed-tickets | api | edge | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| issuetypebreakdown-empty-when-no-completed-tickets | api | edge | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| developerbreakdown-empty-when-no-completed-tickets | api | edge | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| outliers-empty-when-none-exceed-threshold | api | edge | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| stage-funnel-entry-includes-percentage | api | core | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| delta-polarity-color-green | ui | core | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| empty-state-when-no-workflow-stages | ui | core | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| percentile-toggle-single-sprint-only | ui | core | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| multi-sprint-trend-always-p85 | ui | core | spec | spec-only-other-layer | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| calendar-days-measurement | domain-calc | core | both | overlap-confirmed | supersede | generate probe (2026-07-04) | 2026-07-04 |
| no-story-points-included | domain-calc | core | both | overlap-confirmed | supersede | generate probe (2026-07-04) | 2026-07-04 |
| excluded-from-scope-no-effect | domain-calc | edge | both | overlap-confirmed | supersede | generate probe (2026-07-04) | 2026-07-04 |
| current-stage-config-at-query-time | domain-calc | core | spec | overlap-confirmed | supersede | inspection (2026-07-04) | 2026-07-04 |
| shared-pipeline-all-issue-types | domain-calc | core | spec | overlap-confirmed | supersede | inspection (2026-07-04) | 2026-07-04 |
| BR-1 | domain-calc | core | code | code-only-precision | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| BR-4 | domain-calc | core | code | code-only-precision | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| BR-6 | domain-calc | core | code | code-only-precision | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| BR-7 | domain-calc | core | code | code-only-precision | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| BR-10 | domain-calc | core | code | code-only-precision | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| BR-11 | domain-calc | core | code | code-only-precision | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| BR-16 | domain-calc | core | code | code-only-precision | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| BR-19 | domain-calc | core | code | code-only-precision | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| BR-20 | domain-calc | core | code | code-only-precision | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| BR-21 | domain-calc | edge | code | code-only-precision | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| BR-22 | domain-calc | edge | code | code-only-precision | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| BR-24 | domain-calc | edge | code | code-only-precision | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| BR-25 | domain-calc | core | code | code-only-precision | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| BR-27 | domain-calc | core | code | code-only-precision | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| BR-29 | domain-calc | edge | code | code-only-precision | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| BR-32 | domain-calc | core | code | code-only-precision | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| BR-33 | domain-calc | core | code | code-only-precision | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| BR-34 | domain-calc | edge | code | code-only-precision | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| BR-37 | domain-calc | core | code | code-only-precision | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| BR-38 | domain-calc | edge | code | code-only-precision | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| BR-41 | domain-calc | core | code | code-only-precision | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| BR-42 | domain-calc | core | code | code-only-precision | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| BR-43 | domain-calc | core | code | code-only-precision | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| BR-44 | domain-calc | core | code | code-only-precision | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| BR-45 | domain-calc | core | code | code-only-precision | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| BR-46 | domain-calc | core | code | code-only-precision | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| BR-47 | domain-calc | core | code | code-only-precision | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| BR-48 | domain-calc | core | code | code-only-precision | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| BR-49 | domain-calc | core | code | code-only-precision | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| BR-51 | domain-calc | edge | code | code-only-precision | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| BR-54 | domain-calc | core | code | code-only-precision | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| BR-56 | domain-calc | core | code | code-only-precision | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| BR-57 | domain-calc | core | code | code-only-precision | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| BR-58 | domain-calc | core | code | code-only-precision | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |
| BR-59 | domain-calc | core | code | code-only-precision | add | CycleTimeAnalyzer merge run (2026-07-04) | 2026-07-04 |

## Changelog

- 2026-07-04: added StartStageDefaultSecondWorkflowStage (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added EndStageDefaultFirstDoneStatus (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added DeveloperBreakdownAlphabeticalOrder (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added OutlierTableSortedDescending (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added SubTeamFilterScopesComputation (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added ReworkZeroWithoutReEntry (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added CompletedMembershipGate (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added BoundaryStagesInclusive (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added PerStageAccumulationAcrossVisits (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added NeverEnteredStartStageExcluded (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added OutlierStrictlyAboveTwiceMedian (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added PercentilesFromCompletedSet (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added FunnelWithinBoundaryStages (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added DominantStageHighestAccumulated (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added DeltaNullWithoutPrior (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added DeltaPolarityPerMetric (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added MultiSprintPerSprintAveraging (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added MultiSprintDeltaLastVsPrev (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added SprintOrderingByStartDate (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added SprintStartClamping (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added SprintEndClamping (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added CompletionDateEarliestDoneInWindow (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added PreSprintDoneExcluded (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added UnrecognizedStagesGroupedOther (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added cycle-start-stage-must-reference-configured-stage (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added cycle-end-stage-must-reference-stage-or-done-status (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added sprint-selector-defaults-most-recent-closed (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added scatter-plot-reference-lines-p50-p85 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added scatter-plot-hover-content (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added rework-badge-display (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added p85-trend-one-point-per-sprint (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added sprint-summary-row-navigation (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added subteam-all-removes-filter (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added sprint-range-selection-view-switch (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added url-reflects-sprint-selection (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added cycle-starts-dropdown-population (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added cycle-ends-dropdown-population (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added boundaries-saved-via-dedicated-endpoint (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added boundaries-recalculate-on-next-visit (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added percentile-toggle-options (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added percentile-toggle-default-p85 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added percentile-toggle-updates-reference-line-and-card (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added p50-line-always-visible (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added cycle-time-endpoints-no-auth (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added get-boundaries-response-content (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added get-boundaries-always-200 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added get-boundaries-defaults-when-unconfigured (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added get-boundaries-empty-when-no-stages (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added put-boundaries-400-invalid-stage (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added put-boundaries-400-start-not-preceding-end (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added sprintid-last-mutually-exclusive-400 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added last-below-one-400 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added subteam-param-optional-empty-means-all (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added no-params-defaults-all-closed-sprints (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added frontend-default-sends-sprintid (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added sprintid-returns-single-last-returns-multi (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added sprintid-unmatched-400 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added sprint-null-when-no-closed-sprints (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added null-sprint-is-200-not-error (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added metriccards-null-when-sprint-null (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added scatterplot-empty-when-no-completed-tickets (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added stagefunnel-empty-when-no-completed-tickets (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added issuetypebreakdown-empty-when-no-completed-tickets (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added developerbreakdown-empty-when-no-completed-tickets (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added outliers-empty-when-none-exceed-threshold (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added stage-funnel-entry-includes-percentage (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added delta-polarity-color-green (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added empty-state-when-no-workflow-stages (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added percentile-toggle-single-sprint-only (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added multi-sprint-trend-always-p85 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added calendar-days-measurement (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added no-story-points-included (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added excluded-from-scope-no-effect (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added current-stage-config-at-query-time (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added shared-pipeline-all-issue-types (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added BR-1 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added BR-4 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added BR-6 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added BR-7 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added BR-10 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added BR-11 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added BR-16 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added BR-19 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added BR-20 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added BR-21 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added BR-22 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added BR-24 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added BR-25 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added BR-27 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added BR-29 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added BR-32 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added BR-33 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added BR-34 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added BR-37 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added BR-38 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added BR-41 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added BR-42 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added BR-43 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added BR-44 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added BR-45 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added BR-46 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added BR-47 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added BR-48 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added BR-49 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added BR-51 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added BR-54 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added BR-56 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added BR-57 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added BR-58 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: added BR-59 (add) — source: CycleTimeAnalyzer merge run (2026-07-04)
- 2026-07-04: operator triage pass — the 24 overlap-confirmed verdicts rest on operator semantic inspection during post-hoc crosswalk authoring plus the mutation-gated code-arm suite: the mechanical boundary comparator was vacuous this run (code-arm reader emitted no boundary fields), so string-equality neither confirmed nor refuted any pair
- 2026-07-04: operator note — DominantStageHighestAccumulated: spec says "highest average time", code is MaxBy over summed StageDurations; argmax is identical under any per-developer-constant denominator and the spec defines no denominator (spec findings: dominant-stage tie-break undefined) — judged overlap, watched pair
- 2026-07-04: operator triage pass — layer/criticality facts filled on 35 code-only rows (reader emits none for code-arm KB rules): all domain-calc; edge for the degenerate-input guards (BR-21/22/24/29/34/38/51), core otherwise
- 2026-07-04: superseded calendar-days-measurement (supersede) — source: generate probe (2026-07-04) — Cover-from-spec probe SpecSR65_CycleTime_CountsCalendarDays_IncludingWeekends GREEN on first run (Fri→Mon = 3.00 days): implemented; bucket → overlap-confirmed
- 2026-07-04: superseded no-story-points-included (supersede) — source: generate probe (2026-07-04) — probe SpecSR68_TicketWithoutStoryPoints_IsIncludedInAllCalculations GREEN: implemented (the analyzer never reads StoryPoints); bucket → overlap-confirmed
- 2026-07-04: superseded excluded-from-scope-no-effect (supersede) — source: generate probe (2026-07-04) — probe SpecSR73_ExcludedFromScopeStatuses_HaveNoEffectOnCycleTime GREEN: implemented (the analyzer never reads ExcludedFromScopeStatuses); bucket → overlap-confirmed
- 2026-07-04: superseded current-stage-config-at-query-time (supersede) — source: inspection (2026-07-04) — negative/architectural constraint: boundaries resolve from the AppSettings instance passed at compute time, no historical-config path exists; absence verified by inspection, zero tests generated
- 2026-07-04: superseded shared-pipeline-all-issue-types (supersede) — source: inspection (2026-07-04) — negative constraint: one orderedStages resolved once per compute, IssueType never influences boundaries; absence verified by inspection, zero tests generated
- 2026-07-04: owner-delegated verdict — SR-53 rework counting: KEEP CODE, spec examples repaired to 2 and 3 (per-entry counting); original Fokus attests "increments reworkCount per entry" (<fokus-repo> docs/kb/analytics/cycle-time.md) with identical CycleTimeService logic; behavior stays pinned by the mutation-gated code-arm suite (BR-13). Rule verified in spec-rules.md; registry row materializes on the next merge re-run
- 2026-07-04: owner-delegated verdict — SR-66 zero-completed sprint: KEEP CODE, spec repaired — Median/P85 metric cards show 0, not null (BR-29). Rule verified in spec-rules.md; row on next re-run
- 2026-07-04: spec-repair disposition — SR-9 (funnel bottleneck "visually obvious") left open by decision: presentational non-commitment, no factual pin available
- 2026-07-04: crosswalk ratified by owner delegation (session message 2026-07-04); spec-rules stamp recomputed sha256:25d4de4cf184 after the two spec repairs (delta re-check: repair edits only)

