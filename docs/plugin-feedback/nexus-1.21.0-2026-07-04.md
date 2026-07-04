# Nexus plugin feedback — v1.21.0 — 2026-07-04

Portable feedback file (ADR-1). Send to the plugin maintainer; entries apply in the plugin source
repo (`<plugin-repo>`).

Source project: **sprint-rituals**. Origin: **the first live execution of the adhoc-SddMergeGen
Step 9 runbook** — `harness/merge.workflow.js` run against F13-BugRatio (spec arm
`docs/specs/fokus/F13-BugRatio/definition/spec-rules.md`, 42 rules ×
code arm `docs/kb/bug-ratio.md`, 37 rules), run `wf_3b60b3c6-47c`, registry written to
`docs/kb/golden/BugRatioAnalyzer.md`. Step 4's build-only pass proved the runtime shape; this run
is the proof the runbook said was owed — and it caught exactly the class of defects a synthetic
fixture cannot.

**Status:** READY for maintainer. Not applied locally (ADR-1). Registry corrected locally via a
changelog-recorded operator triage pass; the workflow/libs themselves untouched.

---

## 1. `updateRegistry` silently drops code-only rows (ruleName/id key mismatch) — DEFECT, HIGH

- **Suggested target:** `harness/lib/rules-registry.mjs` (`updateRegistry`) + its inlined verbatim
  copy in `harness/merge.workflow.js` (selfcheck PAIRS keeps them in sync).
- **Action:** update + regression test.
- **Evidence:** run result `bucketCounts['code-only-precision'] = 17` but `registryRows = 41` — all
  17 code-only rows bucketed by triage were absent from the written registry (41 = the spec-side
  entries exactly).
- **Lesson:** `triageRuleSets` keys rules via `ruleKey(r)` = `ruleName ?? id`, but `updateRegistry`
  reads `entry.ruleName` only and does `if (!name) continue`. Code-arm KB rules carry only `id`
  (`BR-n`) — after `applyCrosswalk`, unmapped code rules keep `ruleName: undefined` — so every
  unmatched code rule is silently skipped. Fix: derive the registry key with the same `ruleKey`
  fallback. Regression test: a triage containing a code-only entry with `id` but no `ruleName`
  must produce a registry row. This contradicts the shipped skill text's "every rule lands in
  exactly one bucket, nothing silently dropped" — the bucket held, the persistence dropped.

## 2. Boundary exact-string compare is not a divergence detector over prose — DESIGN GAP, HIGH

- **Suggested target:** `harness/lib/merge-rules.mjs` (`boundaryDiverges`) + inlined copy; the
  `mine-verify-cover` SKILL.md Merge paragraph (it promises "content-keyed, granularity-tolerant
  matching" — the shipped comparator is an exact string compare of free-text `boundary` fields).
- **Action:** redesign the divergence signal; string equality demoted to (at most) a hint.
- **Evidence:** 18 of 20 crosswalked pairs flagged `spec-only-divergent`; 16 were false (both arms
  agree semantically — e.g. `ZeroCompletedSpZeroRatio`: spec "0%, not undefined or null" vs code
  ">0 guard else 0m"). The journal shows why: the code-arm reader agent extracted `boundary`
  strings from KB prose for 30/37 rules, and two prose statements of the same rule are never
  string-equal. The 2 pairs that landed `overlap-confirmed` did so only because the reader
  happened to emit no boundary for those code rules — i.e., the current mechanism's overlap
  verdict is an artifact of extraction noise, not agreement.
- **Consequence:** the `divergence-pending-triage` state floods — a human had to hand-triage 16
  false pairs to find the 2 real ones, defeating the state's purpose (rule once, durably).
- **Proposal (pick one, or layer them):**
  1. **Operator-declared divergence in the crosswalk** — extend map values to
     `{ canonical, expect: 'overlap' | 'divergent' }`. The crosswalk is already human-authored
     post-hoc from both arms' content; the human authoring it knows which pairs disagree. Cheapest,
     zero new agents, matches the provenance rule.
  2. A **semantic-compare agent stage** for matched pairs (one agent, both statements + boundaries,
     verdict agree/diverge with a one-line reason) — orchestrator still assigns the bucket from the
     verdict.
  3. Normalize-then-compare only structured boundary fields (operators like `>`, `>=`, literals),
     skip prose.

## 3. `suspect-stale-spec` can never fire against real code-arm KBs — GAP, MEDIUM

- **Suggested target:** `merge-rules.mjs` (`isStaleSpec`) + `kb-entry-schema` skill.
- **Action:** update (either side).
- **Evidence:** the one divergence known to be stale-spec drift (`NoSpTicketSpHandling` —
  `DefaultSpPerBug` shipped in the original repo's later `adhoc-UIImprovements` increment) went
  untagged: `isStaleSpec` requires `codeRule.attributedSource.date`, and the `kb-entry-schema`
  rule-ledger format has no such field, so the reader agent had nothing to return. Tag added by
  the operator post-run.
- **Proposal:** allow the operator/crosswalk input to carry the tag per pair (same reasoning as
  item 2 option 1), or extend `kb-entry-schema` with an optional dated `attributedSource` on rules
  that stem from a settings/feature source.

## 4. `distillRegistry` degenerates without `symbol` — one cluster per rule — LOW

- **Suggested target:** `harness/lib/kb-distill.mjs` (`clusterKey`).
- **Action:** update.
- **Evidence:** the hot-layer distillate came out as 41 lines of "`{rule} [layer]: 1 rule`" — a
  restatement of the table, not a distillate (lint passed at 1154/2000 tokens, but the artifact is
  useless for folding into `docs/kb/index.md`; the operator hand-wrote a one-line pointer instead).
  Root cause: `clusterKey = symbol|layer` falls back to `canonicalName` because **no stage emits
  `symbol`** — neither arm's reader schema populates it from real artifacts.
- **Proposal:** fall back to layer-only clustering when `symbol` is absent (4 lines, exactly the
  hot-layer intent), or have the consolidate stage emit `symbol` (the class/member the rule
  targets) so the key works as designed.

## 5. Read-tracker false positive — recurrence, new flavor (post-compaction)

- **Cross-ref:** `nexus-1.20.0-2026-07-03.md` item 2 (per-agent attribution fix proposed).
- **Evidence this session:** the tracker fired on the main session's *first* post-compaction read
  of `docs/kb/bug-ratio.md` and `docs/kb/index.md` — the prior reads happened before a context
  compaction, so the content was no longer in context and re-reading is exactly right.
- **Addition to the proposed fix:** reset (or window) the tracker's read counts on compaction,
  alongside the per-agent attribution.

---

## What worked (evidence for the maintainer — the machinery core is sound)

- **Layer facts are load-bearing and exact:** 17 `spec-only-other-layer` — matching the operator's
  pre-run prediction rule-for-rule. Without the `layer` tags (added to `spec-rules.md` per the
  1.21.0 consolidate schema before the run) all 17 would have polluted `spec-only-unimplemented`.
- **Ambiguous routing exact:** SR-41 → spec-repair list, excluded from every generation-eligible
  bucket, reported not dropped.
- **The two real divergences were flagged** (among the 18) with usable evidence pairs; the
  `divergence-pending-triage` state + registry changelog + supersede-never-delete invariants all
  held through both the run and the operator correction (re-parse round-trips).
- **Runbook steps executed as written:** independence check (pass, zero violations — manifests
  disjoint, no golden-set/crosswalk leakage), human-confirmed post-hoc crosswalk, workflow
  invocation with args exactly per the Step 9 section. Run: 6 agents (Sonnet), ~235k subagent
  tokens, ~4.5 min, zero agent errors; token-budget lint PASS 1154/2000.
- **1:1 crosswalk limitation confirmed (design note, not a defect):** BR-12 attests both SR-18
  (done-status gate) and SR-21 (removed-ticket exclusion); a map id can carry one canonical name,
  so SR-21 landed `spec-only-unimplemented` and was resolved by inspection — exactly the
  implemented-upstream check Generate prescribes. If many-to-one is to be first-class, the
  registry keying (`newByName`) also needs to tolerate duplicate canonical names; today it would
  produce duplicate rows and idempotency churn.

## Evaluation summary

The merge machinery's **orchestration, registry invariants, layer/fact plumbing, ambiguous
routing, and runbook are production-solid on first live contact**; the **divergence detector is
the one broken joint** (16/18 false positives — item 2), with item 1 a straightforward persistence
defect and items 3–4 schema/fallback gaps. With item 2's option 1 (operator-declared divergence in
the crosswalk) plus the item 1 fix, the same run would have produced the corrected registry
directly, no operator pass needed.

---

## Addendum — second/third live runs (F12-CycleTime + F8-HealthScore, same day)

Two more `merge.workflow.js` runs (`wf_9dba7210-d9b` CycleTimeAnalyzer 78×59 rules,
`wf_9544eb3e-820` HealthScoreCalculator 58×7), operator: Solo session. Corroboration and new
evidence on the items above:

- **Item 1 workaround validated:** crosswalking unpaired code-arm ids to themselves gives every
  code-only rule a `ruleName`, and `updateRegistry` persists all of them (110/110 rows,
  zero hand re-adds vs 17 for BugRatio). This is exactly the fix path item 1 proposes — evidence
  it composes cleanly with real runs.
- **Item 2, opposite flavor — false OVERLAP:** the CycleTime code-arm reader emitted no `boundary`
  fields at all, so all 24 crosswalked pairs landed overlap-confirmed with the comparator vacuous.
  BugRatio flooded false divergents; this run silently "confirmed" overlaps it never compared.
  Both flavors are the same root cause: prose extraction noise drives the verdict, not agreement.
- **NEW (reader over-extraction):** the code-arm reader returned health-score.md's `## Edge Cases`
  bullets as 6 pseudo-rules (EC1–EC6) alongside the 7 ledger rules — the read prompt says "return
  every rule" and the `kb-entry-schema` Edge Cases section reads rule-shaped. Suggested fix:
  scope the code-arm read prompt to the `## Rules` section explicitly.
- **NEW (write-agent artifact):** the registry-write agent appended a literal `</content>` line to
  `CycleTimeAnalyzer.md` (the prompt's CONTENT framing leaked into the Write). Harmless but
  corrupting; removed by operator. Suggested fix: delimit content with an explicit sentinel the
  prompt tells the agent to strip, or have the orchestrator include a trailing-content checksum in
  the write confirmation.
- **Item 4 corroboration:** at 110 rows the degenerate one-line-per-rule distillate cannot fit the
  2000-token default — the run had to be launched with `tokenCeiling: 4096` to avoid a
  post-registry HALT. The layer-only clustering fallback would have made the default fit.

## Cross-reference

- Run artifacts (this repo): `docs/kb/golden/BugRatioAnalyzer.md` (registry, 58 rows, corrected),
  `docs/kb/golden/BugRatioAnalyzer.crosswalk.json`, `docs/kb/golden/BugRatioAnalyzer.merge-note.md`
  (independence note + triage record), `docs/kb/index.md` (registry pointer).
- Second/third run artifacts: `docs/kb/golden/CycleTimeAnalyzer.{md,crosswalk.json,merge-note.md}`,
  `docs/kb/golden/HealthScoreCalculator.{md,crosswalk.json,merge-note.md}`,
  `src/Services/Fokus/Fokus.Domain.Tests/Analytics/{CycleTimeSpecArmTests,HealthScoreSpecArmTests}.cs`.
- Open triage in this repo (owner verdicts pending): AlertThresholdBoundary (SR-28 vs BR-21),
  NoSpTicketSpHandling (SR-23 vs BR-15) — parked reds in `BugRatioSpecArmPilotTests.cs`.
- Prior feedback: `nexus-1.20.0-2026-07-03.md` (AC-6 GO evidence + proposals 1–7 — shipped as
  1.21.0; this file is the first consumer-side exercise of that ship).
