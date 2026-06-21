# F26-SprintTestCoverage — Communication Log

**Branch:** main
**Step:** done
**Cycle:** 0/3

## Messages

| # | From | To | Summary | Problem |
|---|------|-----|---------|---------|
| 1 | team-lead | architect | Plan F26-SprintTestCoverage. Spec at docs/specs/F26-SprintTestCoverage/definition/spec.md | None |
| 2 | architect | team-lead | Pre-planning questions: None. All spec requirements clear and implementable. | None |
| 3 | architect | team-lead | Plan written (14 steps, 0 open questions). Requesting critic review. | None |
| 4 | team-lead | critic | Cross-reference plan vs spec (Mode 2) | None |
| 5 | critic | team-lead | Verdict: REVISE. 3 HIGH, 4 MEDIUM, 2 LOW findings. Key: mixed nullability model, missing SettingsView call site, weight=0 semantics ambiguity, zero TEs spec ambiguity. | None |
| 6 | team-lead | user | Relayed critic findings. Asked product decisions on weight=0 semantics and zero TEs. | None |
| 7 | user | team-lead | Weight=0: show observation mode. Zero TEs: include as 0% (hasQaData=true). | None |
| 8 | team-lead | architect | Forward critic findings + user decisions for plan revision. | None |
| 9 | architect | team-lead | Plan revised. All 9 findings addressed. Summary: (1) SaveHealthConfig uses non-nullable fields, (2) full saveHealthConfig chain listed in Step 14, (3) three-way QualityWeight branching per user decisions, (4) MetricCard Rag as init property, (5) frontend MetricCard type+component updated, (6) active scope filter criteria explicit in Step 5, (7) route convention note added, (8) migration deferral note in Step 2, (9) DashboardView useSettingsStore explicit. | None |
| 10 | team-lead | user | Plan revised, ready for approval (14 steps). | None |
| 11 | user | team-lead | Approved. Split into two phases: backend (Steps 1-12), then frontend (Steps 13-14). | None |
| 12 | team-lead | developer | Implement Phase 1: Steps 1-12 (backend). Plan at docs/specs/F26-SprintTestCoverage/delivery/plan.md. | None |
| 13 | developer | team-lead | For architect: implementation.md written for F26-SprintTestCoverage Phase 1 (Steps 1-12), ready for Step 1. | None |
| 14 | team-lead | architect | Step 1 done check for Phase 1 (Steps 1-12). Plan + implementation.md. | None |
| 15 | architect | team-lead | Step 1 PASSED for F26-SprintTestCoverage Phase 1. All 12 steps verified. Lessons written. Ready for reviewer. | None |
| 16 | team-lead | reviewer | Step 2 code review for F26-SprintTestCoverage Phase 1. Plan at docs/specs/F26-SprintTestCoverage/delivery/plan.md. | None |
| 17 | reviewer | team-lead | REQUEST CHANGES: F26-SprintTestCoverage Phase 1. 1 HIGH (Bugs Found always 0 — missing ThenInclude), 2 MEDIUM, 2 LOW. Review at docs/specs/F26-SprintTestCoverage/delivery/review.md. Cycle 1/3. | None |
| 18 | team-lead | developer | Fixes needed for Phase 1, see review.md. Cycle 1/3. | None |
| 19 | developer | team-lead | Fixes applied: ThenInclude added, migration default fixed to 20, dead ContainsKey guard removed, redundant IsCancelled filter removed, Cancelled string comment added. Ready for re-review. | None |
| 20 | team-lead | reviewer | Re-review Phase 1 fixes. Cycle 1/3. | None |
| 21 | reviewer | team-lead | APPROVED: F26-SprintTestCoverage Phase 1. All 5 fixes verified. Build passes (0 errors). | None |
| 22 | team-lead | developer | Implement Phase 2: Steps 13-14 (frontend). Plan at docs/specs/F26-SprintTestCoverage/delivery/plan.md. | None |
| 23 | developer | team-lead | For architect: implementation.md updated for Phase 2 (Steps 13-14), ready for Step 1. Build passes (TS + .NET). | None |
| 24 | team-lead | architect | Step 1 done check for Phase 2 (Steps 13-14). Plan + implementation.md. | None |
| 25 | architect | team-lead | Step 1 FAILED for F26-SprintTestCoverage Phase 2. 1 HIGH (FailingTicket field name mismatch), 1 MEDIUM (HealthScoreBadge tooltip), 2 LOW. See review.md. | FailingTicket fields will show undefined at runtime |
| 26 | team-lead | developer | Step 1 failed for Phase 2, see review.md. Fix 1 HIGH, 1 MEDIUM, 2 LOW. | None |
| 27 | developer | team-lead | Phase 2 fixes applied: FailingTicket fields corrected, HealthScoreBadge tooltip updated, hasQaData added, bugsFoundDelta removed. Build passes. | None |
| 28 | team-lead | architect | Step 1 re-check for Phase 2 fixes. | None |
| 29 | architect | team-lead | Step 1 PASSED for F26-SprintTestCoverage Phase 2. All 4 fixes verified. Ready for reviewer. | None |
| 30 | team-lead | reviewer | Step 2 code review for F26-SprintTestCoverage Phase 2 (Steps 13-14). Plan at docs/specs/F26-SprintTestCoverage/delivery/plan.md. | None |
| 31 | reviewer | team-lead | APPROVED Phase 2. 3 MEDIUM, 1 LOW. Bugs-found not hidden when 0, undocumented deviations, redundant query param. | None |
| 32 | team-lead | developer | Fix MEDIUMs before closing: bugs-found hidden when 0, failing tickets render assigneeName/storyPoints, remove redundant sprintId param, document deviations. | None |
| 33 | developer | team-lead | MEDIUM fixes applied. Build passes. Ready to close. | None |
| 34 | team-lead | — | Pipeline complete. Summary written. | None |

## Problems

- Phase 1 Cycle 1: HIGH — `ComputeBugsFound` always returns 0 because `GetTestExecutionsForSprintAsync` did not load `TestExecutionLink.Ticket` nav property via ThenInclude. Fixed in cycle 2.
- Phase 2: HIGH — `FailingTicket` TypeScript interface uses `failCount`/`totalCount` but backend serializes `failedRunCount`/`totalRunCount`. Also missing `assigneeName` and `storyPoints` fields.
