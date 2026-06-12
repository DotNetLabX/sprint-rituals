# Golden Set Draft — BR-Coverage Loop Harness

**Status:** Draft — awaiting user confirm/reject per rule
**Date:** 2026-06-11
**Method (source laddering):** original Fokus KB/specs (`<fokus-repo>\docs\`) attest *intent*; current code (`sprint-rituals` Fokus.Domain) attests *implementation*. A rule is a golden candidate only when **both layers attest it independently** (dual attestation = high confidence). Raw mining output: `docs/audit/research-raw/golden-docs-mining.md` + `golden-code-mining.md`.

**Purpose:** held-out benchmark for the harness's precision/recall. **The harness must never see this file** — once confirmed, it moves to `docs/audit/golden-set.md` (NOT `docs/kb/`), excluded from harness inputs by rule.

**Note:** the original Fokus KB already uses BR-IDs (BR3, BR6, BR7, BR8, BR11, BR19) — the future `docs/kb/` migration should preserve those identifiers where they exist.

## Primary candidates (20) — all dual-attested, Confidence: high

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

## Alternates (4) — dual-attested, swap in if any primary is rejected

| ID | Rule | Intent | Code |
|----|------|--------|------|
| GOLD-21 | Cycle-time outlier = cycle time > median × 2 (relative, not absolute) | kb/analytics/cycle-time.md | CycleTimeCalculator.cs:390-391 |
| GOLD-22 | Cycle-time clamping: entry < sprintStart → clamp to sprintStart; open stage at sprint end → exit = sprintEnd | kb/analytics/cycle-time.md | CycleTimeCalculator.cs:279-329 |
| GOLD-23 | Daily pace = assignedSp×(capacity/100)/totalDays; behind-pace iff gap > dailyPace; grace period = first 2 calendar days (flags suppressed) | kb/analytics/daily-progress.md | DeveloperProgressAnalyzer.cs:25,65-138 |
| GOLD-24 | QA pass rate = PASS/(PASS+FAIL); TODO/EXECUTING/ABORTED excluded from numerator AND denominator; no executed runs → 0% | kb/analytics/epic-progress.md (BR3) | QaMetricsCalculator.cs:295-307 |

## Divergences found during curation — NOT golden; each needs a user decision

These are doc-vs-code or code-internal conflicts the cross-reference surfaced — precisely the P0-lane events the harness will automate. **D1–D4 are intent questions (user/PO); D5–D7 are candidate bugs (pipeline).**

| ID | Conflict | Evidence | Likely reading |
|----|----------|----------|----------------|
| DIV-1 | KB: health weights have "no enforced sum constraint — divides by actual sum". Code: API validator enforces Completion+Disruption+CarryOver == 100 | kb/domain/settings.md vs SaveHealthConfigCommand.cs:17-19 | Port-era evolution; code is stricter. Confirm code wins → KB entry must say "weights sum to 100 (validator-enforced)" |
| DIV-2 | KB composite = 3 delivery metrics only. Code adds optional 4th component: QA quality sub-score (qualityWeight default 20, gated on hasQaData) | kb/analytics/health-score.md vs SprintSummaryCalculator.cs:462-479 | Deliberate port enhancement, undocumented. Needs KB entry of its own |
| DIV-3 | KB disruptionRate is feature-only (addedSp/activeSp). Code: DisruptionRate = scopeDisruptionRate + bugDisruptionRate (bugs counted separately and added) | kb/analytics/scope-change.md vs SprintSummaryCalculator.cs:255-258 | Port evolution; intent confirmation needed — changes health score inputs |
| DIV-4 | KB closed-only guard: active sprint NEVER in trends/sparklines. Code sparkline = 4 sprints ending AT AND INCLUDING the selected sprint (which can be active) | kb/domain/sprint.md vs SprintSummaryCalculator.cs:66-69 | Possible contradiction — needs verification of call-site filtering before classifying |
| DIV-5 | Carry-over count increments regardless of excluded status while carryOverSp filters it — count and SP can disagree for excluded-status tickets | CarryOverCalculator.cs:279-290 | Candidate bug (code-internal inconsistency) |
| DIV-6 | Lower-is-better score with configured amber=0: guard short-circuits and the whole above-green region silently scores 0 | HealthScoreCalculator.cs:39-41 | Edge-case candidate bug; also a missing settings-validation bound |
| DIV-7 | QA workload alert thresholds (50%, 2 sprints) hard-coded as constants; analogous BugRatio thresholds are user-configurable AppSettings | QaWorkloadAnalyzer.cs:399-400 | Consistency gap — config or document as fixed |

## Documentation gaps flagged by mining (future harness discovery targets)

- Sync partial-failure semantics (which sprints commit/roll back mid-batch) — undocumented
- Settings validation bounds beyond DefaultSpPerBug (PlanningWindowDays max, weight 0 allowed?) — undocumented
- Overlapping sprint date ranges tiebreaker — explicitly deferred in F23, never formalized
- Xray TE re-sync with changed linked tickets — implicit full replace, no stated invariant

## What happens after confirmation

1. User marks each GOLD row: **accept / reject / edit** (target ≥20 accepted).
2. Accepted set moves to `docs/audit/golden-set.md` — outside `docs/kb/`, flagged harness-excluded.
3. DIV-1..4 decisions go to the user/PO; DIV-5..7 become backlog candidates (bug/gap slugs).
4. The golden set is frozen; harness pilot runs measure recall (found all 20?) and precision (stated them correctly?) against it.
