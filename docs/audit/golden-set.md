# Golden Set — FROZEN (held-out harness benchmark)

**Status:** Frozen — confirmed by user 2026-06-11 (the 20 primary candidates, alternates not needed)
**⚠ HARNESS-EXCLUDED (sequestered tier):** This file must NEVER be provided as input to the BR-coverage
loop harness (miner, verifier, or test-writer agents). It is the answer key for measuring harness
recall/precision. Promotion of these rules into `docs/kb/` happens only AFTER the first measured pilot
run (see proposal roadmap, step 3).

Two-layer enforcement (adoption #4 — *schema = method — working copy → candidate for a nexus
golden-set-schema skill once the pilot proves it*; see nexus boundary:
the evaluation of an employer-internal harness (a private document, not in this repo), § Boundary & sequencing):

1. **Placement:** this file must live outside any directory visible to harness agents (`docs/audit/` is
   not an editable or optimize-tier surface — never move it to `src/`, `docs/kb/`, or any path the
   harness is given write or read access to).
2. **Tripwire (pre-run/CI assertion):** before each harness run and in CI, a cheap mechanical check
   must assert that the path of this file (`docs/audit/golden-set.md`) appears in NO harness input
   context (agent prompts, campaign config, editable-surface declarations, or `surfaces()` output).
   Implementation: a CI grep / pre-run script that exits non-zero if `golden-set.md` appears in any
   harness-generated artifact under `tasks/` or in the campaign config. This is a tripwire for
   accidental leakage — it catches the most common failure mode. It does NOT prevent a determined
   agent with shell access from reading the file directly (the harness's own audit-hook enforcement is
   same-process and can be bypassed). Placement + tripwire together; not placement alone.

**Source:** curated via source-laddering — original Fokus docs (`<fokus-repo>/docs/`) attest intent; sprint-rituals code attests implementation. Both attestations verified per rule. Draft + methodology: `docs/proposals/br-coverage-golden-set-draft.md`.

## The 20 rules

| ID | Rule | Intent attestation (fokus docs) | Code attestation (sprint-rituals) |
|----|------|--------------------------------|-----------------------------------|
| GOLD-01 | Higher-is-better sub-score: ≥green→100; [amber,green)→linear 50–99; <amber→linear 0–49, clamped 0 | kb/analytics/health-score.md | HealthScoreCalculator.cs:13-24 |
| GOLD-02 | Lower-is-better sub-score: ≤green→100; (green,amber]→linear 99–50; >amber→linear 49–0, zero at 2×amber | kb/analytics/health-score.md | HealthScoreCalculator.cs:29-42 |
| GOLD-03 | Composite RAG: ≥75→green, ≥40→amber, else red (fixed thresholds) | kb/analytics/health-score.md | HealthScoreCalculator.cs:44-45 |
| GOLD-04 | Effective SP fallback: StoryPoints if >0; else defaultSpPerBug if Bug and default>0; else null. default=0 disables fallback | kb/cross-cutting.md (GetEffectiveSp) | Sprint/Behaviors/SprintMembership.cs:11-19 |
| GOLD-05 | Division by zero in all analytics → 0 (never null, never error) | kb/cross-cutting.md | SprintSummaryCalculator.cs:254-258 (+ all calculators) |
| GOLD-06 | Completion rate = completedSp/activeSp×100; CAN exceed 100% (prior-sprint carry-over completes → completedSp without activeSp) | kb/analytics/health-score.md | SprintSummaryCalculator.cs:254 + TransitionAttributionChecker |
| GOLD-07 | Transition-based attribution: started/completed in the sprint containing the FIRST qualifying transition to start/end stage within [sprintStart, sprintEnd]; no double-counting | kb/cross-cutting.md (TransitionBasedSprintScope) | TransitionAttributionChecker.cs:88-141 |
| GOLD-08 | Intentional fallback divergence (BR19): scope attribution start = CycleTimeStartStage ?? orderedStages[0]; cycle-time measurement start = ?? orderedStages[1] | kb/cross-cutting.md (spec BR19) | TransitionAttributionChecker.cs:27-47 vs CycleTimeCalculator.cs:199-203 |
| GOLD-09 | Planning cutoff = sprint.StartDate + planningWindowDays (default 2); additions during the window are NOT disruption | kb/cross-cutting.md | SyncConfig.cs:6 + TransitionAttributionChecker.cs:150 |
| GOLD-10 | "Added" (disruption) requires BOTH AddedAt > planningCutoff AND isStarted (cycle-entered) | kb/analytics/scope-change.md | TransitionAttributionChecker.cs:150-151 |
| GOLD-11 | committedSpTotal = non-bug ∧ (WasCommitted ∨ AddedAt≤cutoff) ∧ (RemovedAt null ∨ >cutoff); NO excluded-status filter | kb/analytics/scope-change.md | ScopeChangeCalculator.cs:261-266 |
| GOLD-12 | Disruption classification priority: 1) Unplanned Bug (IssueType=="Bug"), 2) Priority Escalation (CreatedDate < sprint.StartDate), 3) Scope Injection | kb/analytics/scope-change.md | ScopeChangeCalculator.cs:321-331 |
| GOLD-13 | Carry-over = isStarted ∧ !isCompleted (feature-only); carryOverRate = carryOverSp/activeSp×100 | kb/analytics/carry-over.md | TransitionAttributionChecker.cs:182 + SprintSummaryCalculator.cs:258 |
| GOLD-14 | Carry-over destinations: Completed / Carried Again (started ∧ !completed) / Removed (RemovedAt≠null) / Dropped (no qualifying start or absent) | kb/analytics/carry-over.md | CarryOverCalculator.cs:503-555 |
| GOLD-15 | Zombie ticket = 3+ distinct sprint memberships across ALL synced sprints (not the current view range) | kb/analytics/carry-over.md | CarryOverCalculator.cs:428-435 |
| GOLD-16 | Bug classification = IssueType == "Bug", exact case-sensitive match; everything else is a feature | kb/domain/ticket.md | BugRatioCalculator.cs:334 (+ uniform across calculators) |
| GOLD-17 | Multi-sprint bug ratio = ratio of totals (ΣbugSp/ΣcompletedSp), NOT average of per-sprint ratios | kb/analytics/bug-ratio.md | BugRatioCalculator.cs:48-49 |
| GOLD-18 | Bug-ratio alert: ratio ≥ threshold for N consecutive sprints (recent-first); a 0-completed-SP sprint breaks the streak (BR11) | kb/analytics/bug-ratio.md | BugRatioCalculator.cs:255-282 |
| GOLD-19 | Dev throughput rolling avg: 3-sprint window backward from current inclusive; skip 0%-capacity sprints; <3 qualifying → null | kb/analytics/throughput.md | DeveloperThroughputCalculator.cs:252-268 |
| GOLD-20 | Developer excluded from analytics iff effective capacity == 0 AND 0 completed; 0%-capacity with a completed ticket stays IN | kb/domain/developer.md | DeveloperDetailCalculator.cs:58-59 |

## Measurement protocol (pilot step 2→3)

- **Recall** = golden rules independently rediscovered by the miner / 20. A discovery counts if the harness's extracted rule matches the golden rule's substance (same formula/threshold/edge case), regardless of wording.
- **Precision (on the golden subset)** = of the rediscovered rules, how many are stated correctly (formula, boundaries, edge cases all right).
- Miner input = production source only. Never this file, never `docs/kb/`, never test projects.

## Open items NOT in this set (tracked in the draft doc)

- DIV-1..4: doc-vs-code intent conflicts — await user ruling (needed before pilot writes KB entries for those areas)
- DIV-5..7: candidate bugs / consistency gaps — backlog rows
