# EpicProgress — Communication Log

## Messages

| # | From | To | Summary | Problem |
|---|------|-----|---------|---------|
| 1 | team-lead | architect | Plan EpicProgress. Spec at docs/features/EpicProgress/spec.md, save to docs/plans/EpicProgress/plan.md. Check help.tooltips.md. | None |
| 2 | architect | team-lead | Plan ready (10 steps, 0 open questions). Pre-existing plan verified against all 40 ACs. Asking: critic review or self-review? | None |
| 3 | team-lead | user | Relayed quality gate choice: critic review or self-review? | None |
| 4 | user | team-lead | Critic review. | None |
| 5 | team-lead | architect | User chose critic review. Spawn critic to cross-reference plan vs spec. | None — resumed architect via SendMessage |
| 6 | architect | critic | Cross-reference plan.md against spec.md (Mode 2). | None |
| 7 | critic | architect | ACCEPT. 3 gaps found and fixed (navigation includes on 2 repo queries, stale skill mapping row). | None |
| 8 | architect | team-lead | Critic review complete, verdict ACCEPT. Plan ready. 10 steps, auto-approve gate passes. | None |
| 9 | team-lead | developer | Begin implementation. Plan: docs/plans/EpicProgress/plan.md. 10 steps. | Spawned fresh agent instead of resuming — no prior instance existed |
| 10 | developer | team-lead | Implementation complete. All 10 steps done. Both builds pass. implementation.md written. Ready for Step 1. | None |
| 11 | team-lead | architect | Step 1 done check for EpicProgress. Plan + implementation.md. | Spawned fresh architect instead of resuming existing one — lesson learned |
| 12 | architect | team-lead | Step 1 PASSED. Note for reviewer: verify tooltip coverage depth in Step 9 components. | None |
| 13 | team-lead | reviewer | Step 2 code review. Special attention: tooltip coverage completeness. | Spawned fresh agent — first instance |
| 14 | reviewer | team-lead | REQUEST CHANGES. 2 HIGH findings: (1) projection omits Assignee navigation, (2) sub-team filter tooltip missing from PageToolbar. | None |
| 15 | team-lead | developer | Fixes needed, cycle 1/3. Two HIGH findings to fix. | Resumed via SendMessage (agent ID) |
| 16 | developer | team-lead | Fixes applied. Both findings were already resolved in codebase. Ready for re-review. Cycle 1/3. | None |
| 17 | team-lead | reviewer | Re-review cycle 1/3. Developer reports both findings already resolved. Verify in source. | Resumed via SendMessage (agent ID) |
| 18 | reviewer | team-lead | APPROVED. Both fixes verified. Builds clean. | None |
| 19 | team-lead | architect | APPROVED. Close pipeline: write summary.md and lessons.md. | Resumed via SendMessage (agent ID) |
| 20 | architect | team-lead | Pipeline COMPLETE. summary.md and lessons.md written. 5 architect lessons captured. | None |

## Problems

1. **Fresh spawns instead of resume (messages 11, 13).** Team lead spawned fresh architect and reviewer agents instead of resuming existing ones via SendMessage. Wasted context — agents had to re-read all files. Fixed mid-pipeline: messages 15, 17, 19 correctly used SendMessage to resume. Team-lead agent file updated with resume-first rule.
2. **Communication log not maintained in real-time.** Team lead only logged message #1 during the run and backfilled the rest after pipeline completion. Should be updated as messages flow.
