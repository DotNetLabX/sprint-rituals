# Canonical Rule Registry — BugRatioAnalyzer

One row per canonical rule (SddLifecycle C1). Deprecate-never-delete: a retired/superseded row is kept, never removed. `source:` and `last_verified` are mandatory on every row.

| Rule | Layer | Criticality | Arms | Bucket | Disposition | Source | Last Verified |
|------|-------|-------------|------|--------|-------------|--------|---------------|
| PerDevSprintRatioFormula | domain-calc | golden | both | overlap-confirmed | add | BugRatioAnalyzer merge run (2026-07-04) | 2026-07-04 |
| PerSprintBreakdownProvided | api | core | both | overlap-confirmed | add | BugRatioAnalyzer merge run (2026-07-04) | 2026-07-04 |
| bug-ratio-alert-threshold-setting | settings | core | spec | spec-only-other-layer | add | BugRatioAnalyzer merge run (2026-07-04) | 2026-07-04 |
| bug-ratio-consecutive-sprint-count-setting | settings | core | spec | spec-only-other-layer | add | BugRatioAnalyzer merge run (2026-07-04) | 2026-07-04 |
| settings-reuse-existing-endpoints | api | core | spec | spec-only-other-layer | add | BugRatioAnalyzer merge run (2026-07-04) | 2026-07-04 |
| view-mode-selection-by-sprint-range | ui | core | spec | spec-only-other-layer | add | BugRatioAnalyzer merge run (2026-07-04) | 2026-07-04 |
| query-param-mutual-exclusivity | api | core | spec | spec-only-other-layer | add | BugRatioAnalyzer merge run (2026-07-04) | 2026-07-04 |
| default-last-5-closed-sprints | api | core | spec | spec-only-other-layer | add | BugRatioAnalyzer merge run (2026-07-04) | 2026-07-04 |
| multi-sprint-response-top-level | api | core | spec | spec-only-other-layer | add | BugRatioAnalyzer merge run (2026-07-04) | 2026-07-04 |
| multi-sprint-per-developer-entry | api | core | spec | spec-only-other-layer | add | BugRatioAnalyzer merge run (2026-07-04) | 2026-07-04 |
| single-sprint-response-top-level | api | core | spec | spec-only-other-layer | add | BugRatioAnalyzer merge run (2026-07-04) | 2026-07-04 |
| single-sprint-per-developer-entry | api | core | spec | spec-only-other-layer | add | BugRatioAnalyzer merge run (2026-07-04) | 2026-07-04 |
| alert-status-fields-in-both-modes | api | core | spec | spec-only-other-layer | add | BugRatioAnalyzer merge run (2026-07-04) | 2026-07-04 |
| error-invalid-sprint-id | api | core | spec | spec-only-other-layer | add | BugRatioAnalyzer merge run (2026-07-04) | 2026-07-04 |
| error-last-below-one | api | core | spec | spec-only-other-layer | add | BugRatioAnalyzer merge run (2026-07-04) | 2026-07-04 |
| empty-results-when-no-closed-sprints | api | edge | spec | spec-only-other-layer | add | BugRatioAnalyzer merge run (2026-07-04) | 2026-07-04 |
| only-active-developers | api | core | spec | spec-only-other-layer | add | BugRatioAnalyzer merge run (2026-07-04) | 2026-07-04 |
| subteam-all-clears-filter | ui | core | spec | spec-only-other-layer | add | BugRatioAnalyzer merge run (2026-07-04) | 2026-07-04 |
| tab-structure-and-persisted-filters | ui | core | spec | spec-only-other-layer | add | BugRatioAnalyzer merge run (2026-07-04) | 2026-07-04 |
| SubTeamBlankMeansAllDevelopers | api | core | both | overlap-confirmed | supersede | BugRatioAnalyzer merge run (2026-07-04) | 2026-07-04 |
| DevDeltaCurrentMinusPrior | domain-calc | core | both | overlap-confirmed | supersede | BugRatioAnalyzer merge run (2026-07-04) | 2026-07-04 |
| BugClassificationExactCaseSensitive | domain-calc | golden | both | overlap-confirmed | supersede | BugRatioAnalyzer merge run (2026-07-04) | 2026-07-04 |
| CompletedMembershipGate | domain-calc | golden | both | overlap-confirmed | supersede | BugRatioAnalyzer merge run (2026-07-04) | 2026-07-04 |
| TeamRatioOfTotals | domain-calc | golden | both | overlap-confirmed | supersede | BugRatioAnalyzer merge run (2026-07-04) | 2026-07-04 |
| ExcludedStatusesExcluded | domain-calc | core | both | overlap-confirmed | supersede | BugRatioAnalyzer merge run (2026-07-04) | 2026-07-04 |
| NoSpTicketSpHandling | domain-calc | golden | both | overlap-confirmed | supersede | owner verdict (2026-07-04) | 2026-07-04 |
| ZeroCompletedSpZeroRatio | domain-calc | golden | both | overlap-confirmed | supersede | BugRatioAnalyzer merge run (2026-07-04) | 2026-07-04 |
| PerDevMultiSprintRatioOfTotals | domain-calc | golden | both | overlap-confirmed | supersede | BugRatioAnalyzer merge run (2026-07-04) | 2026-07-04 |
| AlertUsesFullSprintHistory | domain-calc | core | both | overlap-confirmed | supersede | BugRatioAnalyzer merge run (2026-07-04) | 2026-07-04 |
| AlertThresholdBoundary | domain-calc | golden | both | overlap-confirmed | supersede | owner verdict (2026-07-04) | 2026-07-04 |
| AlertActivationCount | domain-calc | golden | both | overlap-confirmed | supersede | BugRatioAnalyzer merge run (2026-07-04) | 2026-07-04 |
| ZeroSpSprintEndsStreak | domain-calc | edge | both | overlap-confirmed | supersede | BugRatioAnalyzer merge run (2026-07-04) | 2026-07-04 |
| DeltaSingleSprintOnly | domain-calc | core | both | overlap-confirmed | supersede | BugRatioAnalyzer merge run (2026-07-04) | 2026-07-04 |
| DeltaPolarityPerMetric | domain-calc | core | both | overlap-confirmed | supersede | BugRatioAnalyzer merge run (2026-07-04) | 2026-07-04 |
| SubTeamFilterOnDevelopers | domain-calc | core | both | overlap-confirmed | supersede | BugRatioAnalyzer merge run (2026-07-04) | 2026-07-04 |
| SprintOrderingByStartDate | domain-calc | core | both | overlap-confirmed | supersede | BugRatioAnalyzer merge run (2026-07-04) | 2026-07-04 |
| IssueTypeBreakdownCompletedOnly | domain-calc | core | both | overlap-confirmed | supersede | BugRatioAnalyzer merge run (2026-07-04) | 2026-07-04 |
| removed-tickets-excluded | domain-calc | core | both | overlap-confirmed | supersede | BugRatioAnalyzer merge run (2026-07-04) | 2026-07-04 |
| zero-ticket-developers-visible | domain-calc | core | both | overlap-confirmed | supersede | generate probe (2026-07-04) | 2026-07-04 |
| no-rolling-average | domain-calc | edge | spec | overlap-confirmed | supersede | inspection (2026-07-04) | 2026-07-04 |
| bug-priority-ignored | domain-calc | edge | both | overlap-confirmed | supersede | generate probe (2026-07-04) | 2026-07-04 |
| BR-4 | domain-calc | core | code | code-only-precision | add | operator triage pass (2026-07-04) | 2026-07-04 |
| BR-6 | domain-calc | core | code | code-only-precision | add | operator triage pass (2026-07-04) | 2026-07-04 |
| BR-8 | domain-calc | core | code | code-only-precision | add | operator triage pass (2026-07-04) | 2026-07-04 |
| BR-10 | domain-calc | core | code | code-only-precision | add | operator triage pass (2026-07-04) | 2026-07-04 |
| BR-11 | domain-calc | core | code | code-only-precision | add | operator triage pass (2026-07-04) | 2026-07-04 |
| BR-14 | domain-calc | core | code | code-only-precision | add | operator triage pass (2026-07-04) | 2026-07-04 |
| BR-18 | domain-calc | core | code | code-only-precision | add | operator triage pass (2026-07-04) | 2026-07-04 |
| BR-19 | domain-calc | core | code | code-only-precision | add | operator triage pass (2026-07-04) | 2026-07-04 |
| BR-24 | domain-calc | core | code | code-only-precision | add | operator triage pass (2026-07-04) | 2026-07-04 |
| BR-25 | domain-calc | core | code | code-only-precision | add | operator triage pass (2026-07-04) | 2026-07-04 |
| BR-26 | domain-calc | edge | code | code-only-precision | add | operator triage pass (2026-07-04) | 2026-07-04 |
| BR-27 | domain-calc | core | code | code-only-precision | add | operator triage pass (2026-07-04) | 2026-07-04 |
| BR-28 | domain-calc | core | code | code-only-precision | add | operator triage pass (2026-07-04) | 2026-07-04 |
| BR-29 | domain-calc | core | code | code-only-precision | add | operator triage pass (2026-07-04) | 2026-07-04 |
| BR-32 | domain-calc | core | code | code-only-precision | add | operator triage pass (2026-07-04) | 2026-07-04 |
| BR-33 | domain-calc | core | code | code-only-precision | add | operator triage pass (2026-07-04) | 2026-07-04 |
| BR-35 | domain-calc | core | code | code-only-precision | add | operator triage pass (2026-07-04) | 2026-07-04 |

## Divergence detail

### NoSpTicketSpHandling
- state: resolved-spec-repaired (owner verdict 2026-07-04 — keep code; stale spec confirmed, DefaultSpPerBug exception written into spec.md rule 6 and SR-23)
- spec citation: (repaired) "Exception (amended 2026-07-04): when the Default SP per bug setting is configured (> 0), a completed **bug** ticket with no story points receives that default in SP calculations; at the setting's default of 0, the exclusion above applies unchanged."
- code attestation: BR-15 — SprintMembership.GetEffectiveSp(defaultSpPerBug): a no-SP Bug membership receives settings.BugRatio.DefaultSpPerBug (null effective SP coalesced to 0m); DefaultSpPerBug shipped in the later adhoc-UIImprovements increment (original Fokus), post-dating the F13 spec
- tags: suspect-stale-spec-confirmed

### AlertThresholdBoundary
- state: resolved-spec-repaired (owner verdict 2026-07-04 — keep code; spec.md rule 10 + SR-1/SR-28 repaired to ">=" inclusive, matching the original Fokus behavior)
- spec citation: (repaired) "count backward how many consecutive sprints the developer's bug ratio is at or above the threshold (>=, inclusive — a sprint exactly at the threshold counts toward the streak)."
- code attestation: BR-21 — BugRatioAnalyzer.cs:272 'ratio >= threshold' (inclusive), deliberately pinned by mutation-gated test Alert_Fires_When_RatioExactlyAtThreshold_ForConsecutiveCount; original Fokus KB (<fokus-repo> docs/kb/analytics/bug-ratio.md) documents the same '>= threshold'


## Changelog

- 2026-07-04: added PerDevSprintRatioFormula (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: added PerSprintBreakdownProvided (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: added bug-ratio-alert-threshold-setting (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: added bug-ratio-consecutive-sprint-count-setting (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: added settings-reuse-existing-endpoints (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: added view-mode-selection-by-sprint-range (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: added query-param-mutual-exclusivity (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: added default-last-5-closed-sprints (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: added multi-sprint-response-top-level (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: added multi-sprint-per-developer-entry (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: added single-sprint-response-top-level (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: added single-sprint-per-developer-entry (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: added alert-status-fields-in-both-modes (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: added error-invalid-sprint-id (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: added error-last-below-one (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: added empty-results-when-no-closed-sprints (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: added only-active-developers (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: added subteam-all-clears-filter (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: added tab-structure-and-persisted-filters (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: added SubTeamBlankMeansAllDevelopers (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: added DevDeltaCurrentMinusPrior (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: added BugClassificationExactCaseSensitive (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: added CompletedMembershipGate (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: added TeamRatioOfTotals (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: added ExcludedStatusesExcluded (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: added NoSpTicketSpHandling (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: added ZeroCompletedSpZeroRatio (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: added PerDevMultiSprintRatioOfTotals (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: added AlertUsesFullSprintHistory (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: added AlertThresholdBoundary (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: added AlertActivationCount (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: added ZeroSpSprintEndsStreak (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: added DeltaSingleSprintOnly (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: added DeltaPolarityPerMetric (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: added SubTeamFilterOnDevelopers (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: added SprintOrderingByStartDate (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: added IssueTypeBreakdownCompletedOnly (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: added removed-tickets-excluded (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: added zero-ticket-developers-visible (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: added no-rolling-average (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: added bug-priority-ignored (add) — source: BugRatioAnalyzer merge run (2026-07-04)
- 2026-07-04: superseded SubTeamBlankMeansAllDevelopers (supersede) — source: operator triage pass (2026-07-04) — false divergence: boundary prose differs textually, semantics agree (both arms inspected)
- 2026-07-04: superseded DevDeltaCurrentMinusPrior (supersede) — source: operator triage pass (2026-07-04) — false divergence: boundary prose differs textually, semantics agree (both arms inspected)
- 2026-07-04: superseded BugClassificationExactCaseSensitive (supersede) — source: operator triage pass (2026-07-04) — false divergence: boundary prose differs textually, semantics agree (both arms inspected)
- 2026-07-04: superseded CompletedMembershipGate (supersede) — source: operator triage pass (2026-07-04) — false divergence: boundary prose differs textually, semantics agree (both arms inspected)
- 2026-07-04: superseded TeamRatioOfTotals (supersede) — source: operator triage pass (2026-07-04) — false divergence: boundary prose differs textually, semantics agree (both arms inspected)
- 2026-07-04: superseded ExcludedStatusesExcluded (supersede) — source: operator triage pass (2026-07-04) — false divergence: boundary prose differs textually, semantics agree (both arms inspected)
- 2026-07-04: superseded ZeroCompletedSpZeroRatio (supersede) — source: operator triage pass (2026-07-04) — false divergence: boundary prose differs textually, semantics agree (both arms inspected)
- 2026-07-04: superseded PerDevMultiSprintRatioOfTotals (supersede) — source: operator triage pass (2026-07-04) — false divergence: boundary prose differs textually, semantics agree (both arms inspected)
- 2026-07-04: superseded AlertUsesFullSprintHistory (supersede) — source: operator triage pass (2026-07-04) — false divergence: boundary prose differs textually, semantics agree (both arms inspected)
- 2026-07-04: superseded AlertActivationCount (supersede) — source: operator triage pass (2026-07-04) — false divergence: boundary prose differs textually, semantics agree (both arms inspected)
- 2026-07-04: superseded ZeroSpSprintEndsStreak (supersede) — source: operator triage pass (2026-07-04) — false divergence: boundary prose differs textually, semantics agree (both arms inspected)
- 2026-07-04: superseded DeltaSingleSprintOnly (supersede) — source: operator triage pass (2026-07-04) — false divergence: boundary prose differs textually, semantics agree (both arms inspected)
- 2026-07-04: superseded DeltaPolarityPerMetric (supersede) — source: operator triage pass (2026-07-04) — false divergence: boundary prose differs textually, semantics agree (both arms inspected)
- 2026-07-04: superseded SubTeamFilterOnDevelopers (supersede) — source: operator triage pass (2026-07-04) — false divergence: boundary prose differs textually, semantics agree (both arms inspected)
- 2026-07-04: superseded SprintOrderingByStartDate (supersede) — source: operator triage pass (2026-07-04) — false divergence: boundary prose differs textually, semantics agree (both arms inspected)
- 2026-07-04: superseded IssueTypeBreakdownCompletedOnly (supersede) — source: operator triage pass (2026-07-04) — false divergence: boundary prose differs textually, semantics agree (both arms inspected)
- 2026-07-04: superseded AlertThresholdBoundary (supersede) — source: operator triage pass (2026-07-04) — evidence pair enriched (code attestation detail + original-Fokus lineage); state stays divergence-pending-triage
- 2026-07-04: superseded NoSpTicketSpHandling (supersede) — source: operator triage pass (2026-07-04) — tagged suspect-stale-spec (code behavior attributed to a source the mined spec predates); state stays divergence-pending-triage
- 2026-07-04: superseded removed-tickets-excluded (supersede) — source: operator triage pass (2026-07-04) — implemented by BR-12 clause (a) (RemovedAt null gate); the 1:1 crosswalk could not pair BR-12 twice (already paired with SR-18/CompletedMembershipGate)
- 2026-07-04: added BR-4 (add) — source: operator triage pass (2026-07-04) — re-added code-only row dropped by the merge run (updateRegistry keys on ruleName; code-arm rules carry only id); statement lives in docs/kb/bug-ratio.md
- 2026-07-04: added BR-6 (add) — source: operator triage pass (2026-07-04) — re-added code-only row dropped by the merge run (updateRegistry keys on ruleName; code-arm rules carry only id); statement lives in docs/kb/bug-ratio.md
- 2026-07-04: added BR-8 (add) — source: operator triage pass (2026-07-04) — re-added code-only row dropped by the merge run (updateRegistry keys on ruleName; code-arm rules carry only id); statement lives in docs/kb/bug-ratio.md
- 2026-07-04: added BR-10 (add) — source: operator triage pass (2026-07-04) — re-added code-only row dropped by the merge run (updateRegistry keys on ruleName; code-arm rules carry only id); statement lives in docs/kb/bug-ratio.md
- 2026-07-04: added BR-11 (add) — source: operator triage pass (2026-07-04) — re-added code-only row dropped by the merge run (updateRegistry keys on ruleName; code-arm rules carry only id); statement lives in docs/kb/bug-ratio.md
- 2026-07-04: added BR-14 (add) — source: operator triage pass (2026-07-04) — re-added code-only row dropped by the merge run (updateRegistry keys on ruleName; code-arm rules carry only id); statement lives in docs/kb/bug-ratio.md
- 2026-07-04: added BR-18 (add) — source: operator triage pass (2026-07-04) — re-added code-only row dropped by the merge run (updateRegistry keys on ruleName; code-arm rules carry only id); statement lives in docs/kb/bug-ratio.md
- 2026-07-04: added BR-19 (add) — source: operator triage pass (2026-07-04) — re-added code-only row dropped by the merge run (updateRegistry keys on ruleName; code-arm rules carry only id); statement lives in docs/kb/bug-ratio.md
- 2026-07-04: added BR-24 (add) — source: operator triage pass (2026-07-04) — re-added code-only row dropped by the merge run (updateRegistry keys on ruleName; code-arm rules carry only id); statement lives in docs/kb/bug-ratio.md
- 2026-07-04: added BR-25 (add) — source: operator triage pass (2026-07-04) — re-added code-only row dropped by the merge run (updateRegistry keys on ruleName; code-arm rules carry only id); statement lives in docs/kb/bug-ratio.md
- 2026-07-04: added BR-26 (add) — source: operator triage pass (2026-07-04) — re-added code-only row dropped by the merge run (updateRegistry keys on ruleName; code-arm rules carry only id); statement lives in docs/kb/bug-ratio.md
- 2026-07-04: added BR-27 (add) — source: operator triage pass (2026-07-04) — re-added code-only row dropped by the merge run (updateRegistry keys on ruleName; code-arm rules carry only id); statement lives in docs/kb/bug-ratio.md
- 2026-07-04: added BR-28 (add) — source: operator triage pass (2026-07-04) — re-added code-only row dropped by the merge run (updateRegistry keys on ruleName; code-arm rules carry only id); statement lives in docs/kb/bug-ratio.md
- 2026-07-04: added BR-29 (add) — source: operator triage pass (2026-07-04) — re-added code-only row dropped by the merge run (updateRegistry keys on ruleName; code-arm rules carry only id); statement lives in docs/kb/bug-ratio.md
- 2026-07-04: added BR-32 (add) — source: operator triage pass (2026-07-04) — re-added code-only row dropped by the merge run (updateRegistry keys on ruleName; code-arm rules carry only id); statement lives in docs/kb/bug-ratio.md
- 2026-07-04: added BR-33 (add) — source: operator triage pass (2026-07-04) — re-added code-only row dropped by the merge run (updateRegistry keys on ruleName; code-arm rules carry only id); statement lives in docs/kb/bug-ratio.md
- 2026-07-04: added BR-35 (add) — source: operator triage pass (2026-07-04) — re-added code-only row dropped by the merge run (updateRegistry keys on ruleName; code-arm rules carry only id); statement lives in docs/kb/bug-ratio.md
- 2026-07-04: superseded AlertThresholdBoundary (supersede) — source: owner verdict (2026-07-04) — KEEP CODE, spec repaired: ">=" inclusive written into spec.md rule 10 / SR-1 / SR-28; divergence resolved, bucket → overlap-confirmed
- 2026-07-04: superseded NoSpTicketSpHandling (supersede) — source: owner verdict (2026-07-04) — KEEP CODE, spec repaired: DefaultSpPerBug exception written into spec.md rule 6 / SR-23; stale spec confirmed; divergence resolved, bucket → overlap-confirmed
- 2026-07-04: superseded zero-ticket-developers-visible (supersede) — source: generate probe (2026-07-04) — Cover-from-spec probe SpecSR32_ActiveDeveloperWithZeroCompletedTickets GREEN on first run: implemented; bucket → overlap-confirmed
- 2026-07-04: superseded bug-priority-ignored (supersede) — source: generate probe (2026-07-04) — probe SpecSR42_BugPriority_HasNoEffectOnRatio GREEN: implemented (the analyzer never reads Priority); bucket → overlap-confirmed
- 2026-07-04: superseded no-rolling-average (supersede) — source: inspection (2026-07-04) — negative constraint: conformance is the ABSENCE of a rolling-average path, verified by inspection; nothing to generate (a probe would assert a missing field, not behavior)
