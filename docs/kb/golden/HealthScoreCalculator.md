# Canonical Rule Registry — HealthScoreCalculator

One row per canonical rule (SddLifecycle C1). Deprecate-never-delete: a retired/superseded row is kept, never removed. `source:` and `last_verified` are mandatory on every row.

| Rule | Layer | Criticality | Arms | Bucket | Disposition | Source | Last Verified |
|------|-------|-------------|------|--------|-------------|--------|---------------|
| composite-rag-not-configurable | settings | core | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| sprint-selector-defaults-most-recent | ui | core | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| four-metric-cards | ui | core | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| empty-state-when-no-closed-sprints | ui | edge | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| closed-sprints-ordered-descending | api | core | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| sprint-switch-recomputes-all-content | ui | core | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| url-updates-on-sprint-switch | ui | core | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| sub-team-dropdown-options | ui | core | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| sub-team-all-removes-filter | ui | core | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| no-flags-message | ui | edge | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| sprintid-defaults-most-recent | api | core | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| subteam-param-omit-or-empty-means-all | api | core | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| no-closed-sprints-returns-200-null-sprint | api | edge | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| healthscore-metrics-null-when-sprint-null | api | edge | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| sparkline-entry-shape | api | core | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| topepics-empty-when-none | api | edge | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| leaderboard-empty-when-no-developers | api | edge | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| mid-sprint-disruption-null-when-none | api | edge | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| flags-has-any-flags-field | api | core | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| invalid-sprintid-400 | api | core | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| health-score-weighted-composite | domain-summary | golden | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| completed-uses-done-statuses | domain-summary | golden | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| committed-sp-definition | domain-summary | golden | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| added-sp-definition | domain-summary | golden | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| disruption-rate-formula | domain-summary | golden | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| completion-rate-formula | domain-summary | golden | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| carryover-sp-definition | domain-summary | golden | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| carryover-rate-formula | domain-summary | golden | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| delta-prior-sprint-definition | domain-summary | core | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| delta-null-when-no-prior-sprint | domain-summary | core | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| delta-polarity-completion-pct | domain-summary | core | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| delta-polarity-disruption-carryover | domain-summary | core | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| sparkline-4-sprint-trailing-window | domain-summary | core | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| sparkline-fewer-when-fewer-exist | domain-summary | edge | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| top-epics-eligibility-and-ranking | domain-summary | core | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| top-epics-overall-progress-scope | domain-summary | core | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| leaderboard-active-developers-only | domain-summary | core | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| leaderboard-sorted-sp-desc | domain-summary | core | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| leaderboard-zero-sp-still-appear | domain-summary | edge | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| leaderboard-no-truncation | domain-summary | core | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| zombie-ticket-threshold | domain-summary | golden | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| zombie-count-scope | domain-summary | core | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| mid-sprint-disruption-grace-period | domain-summary | golden | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| zero-sp-developers-flag | domain-summary | core | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| sub-team-filter-scopes-everything | domain-summary | golden | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| dashboard-single-sprint-only | ui | core | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| tickets-no-sp-excluded-from-sp-metrics | domain-summary | golden | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| tickets-no-sp-count-for-ticket-flags | domain-summary | core | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| division-by-zero-committed | domain-summary | golden | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| division-by-zero-carryover | domain-summary | edge | spec | spec-only-other-layer | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| MetricRagFromConfigurableThresholds | domain-calc | golden | both | overlap-confirmed | supersede | operator triage pass (2026-07-04) | 2026-07-04 |
| health-score-value-ranges | domain-calc | core | both | overlap-confirmed | supersede | generate probe (2026-07-04) | 2026-07-04 |
| BR1 | domain-calc | golden | code | code-only-precision | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| BR2 | domain-calc | golden | code | code-only-precision | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| BR3 | domain-calc | golden | code | code-only-precision | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| BR4 | domain-calc | golden | code | code-only-precision | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| BR5 | domain-calc | golden | code | code-only-precision | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |
| BR7 | domain-calc | core | code | code-only-precision | add | HealthScoreCalculator merge run (2026-07-04) | 2026-07-04 |

## Changelog

- 2026-07-04: added composite-rag-not-configurable (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added sprint-selector-defaults-most-recent (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added four-metric-cards (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added empty-state-when-no-closed-sprints (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added closed-sprints-ordered-descending (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added sprint-switch-recomputes-all-content (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added url-updates-on-sprint-switch (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added sub-team-dropdown-options (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added sub-team-all-removes-filter (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added no-flags-message (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added sprintid-defaults-most-recent (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added subteam-param-omit-or-empty-means-all (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added no-closed-sprints-returns-200-null-sprint (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added healthscore-metrics-null-when-sprint-null (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added sparkline-entry-shape (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added topepics-empty-when-none (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added leaderboard-empty-when-no-developers (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added mid-sprint-disruption-null-when-none (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added flags-has-any-flags-field (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added invalid-sprintid-400 (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added health-score-weighted-composite (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added completed-uses-done-statuses (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added committed-sp-definition (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added added-sp-definition (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added disruption-rate-formula (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added completion-rate-formula (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added carryover-sp-definition (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added carryover-rate-formula (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added delta-prior-sprint-definition (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added delta-null-when-no-prior-sprint (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added delta-polarity-completion-pct (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added delta-polarity-disruption-carryover (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added sparkline-4-sprint-trailing-window (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added sparkline-fewer-when-fewer-exist (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added top-epics-eligibility-and-ranking (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added top-epics-overall-progress-scope (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added leaderboard-active-developers-only (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added leaderboard-sorted-sp-desc (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added leaderboard-zero-sp-still-appear (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added leaderboard-no-truncation (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added zombie-ticket-threshold (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added zombie-count-scope (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added mid-sprint-disruption-grace-period (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added zero-sp-developers-flag (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added sub-team-filter-scopes-everything (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added dashboard-single-sprint-only (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added tickets-no-sp-excluded-from-sp-metrics (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added tickets-no-sp-count-for-ticket-flags (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added division-by-zero-committed (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added division-by-zero-carryover (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added MetricRagFromConfigurableThresholds (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added health-score-value-ranges (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added BR1 (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added BR2 (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added BR3 (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added BR4 (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added BR5 (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: added BR7 (add) — source: HealthScoreCalculator merge run (2026-07-04)
- 2026-07-04: superseded MetricRagFromConfigurableThresholds (supersede) — source: operator triage pass (2026-07-04) — false divergence: the spec commits to per-metric RAG from configurable thresholds, independent of composite RAG, but defines no concrete mapping ("Per-metric RAG mapping undefined", spec findings); code's BR6/BR7 mapping (inclusive on the better side) is consistent with every spec commitment (both arms inspected); bucket → overlap-confirmed
- 2026-07-04: superseded health-score-value-ranges (supersede) — source: generate probe (2026-07-04) — Cover-from-spec probes SpecSR16_HigherIsBetterSubScore_AlwaysWithinZeroToHundred + SpecSR16_LowerIsBetterSubScore_AlwaysWithinZeroToHundred (FsCheck properties over the spec's committed threshold domain) GREEN on first run: implemented; composite half bounded by inspection (weighted average of [0,100] sub-scores, SprintSummaryAnalyzer); bucket → overlap-confirmed
- 2026-07-04: operator triage pass — criticality facts filled on 6 code-only rows (reader emits none): BR1–BR5 golden (the calculator's scoring anchors, mutation-gated 70/70), BR7 core
- 2026-07-04: operator review — code-arm reader over-extracted health-score.md's Edge Cases bullets as pseudo-rules EC1–EC6 (bucketed code-only-precision, 12 reported); they are documentation, not ledger rules, and are excluded from this registry by operator decision — the exclusion mechanically coincided with the known updateRegistry ruleName-drop defect (nexus-1.21.0 feedback item 1), reviewed and endorsed rather than silent
