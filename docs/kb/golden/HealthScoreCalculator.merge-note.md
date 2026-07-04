# Merge run note — HealthScoreCalculator (M1, 2026-07-04)

Third live execution of the SDD M1 merge (nexus 1.21.0, `merge.workflow.js` — the adhoc-SddMergeGen
Step 9 runbook), run back-to-back with CycleTimeAnalyzer. Companion to the registry in this folder.
Operator: Solo session (autonomous, owner-directed "same as BugRatio" run).

## Class-vs-feature scope note

F8-SprintSummaryCard's only code-arm KB is `docs/kb/health-score.md`, which covers
`HealthScoreCalculator` alone — the merge target class. The F8 spec's other domain rules live in
`SprintSummaryAnalyzer`, which has no code-arm KB yet. Those 29 rules carry the `layer:
domain-summary` fact (added in the pre-run operator pass) and park as `spec-only-other-layer` —
they await their own merge once SprintSummaryAnalyzer is mined (candidate next target). Without
this convention they would have flooded the generation-eligible bucket with out-of-class rules.

## Arms and independence (runbook steps 1–2)

- Spec arm: `docs/specs/fokus/F8-SprintSummaryCard/definition/spec-rules.md` (58 rules, mined
  2026-07-03, clean-room; `layer`/`criticality` facts added 2026-07-04 — operator pass, pre-run).
- Code arm: `docs/kb/health-score.md` (7 rules BR1–BR7, mutation-gated 2026-06-13, 70/70 killed).
- Independence: manifests disjoint; no golden set in this repo; crosswalk never a run input.
  Pass, zero violations.
- Crosswalk (`HealthScoreCalculator.crosswalk.json`, 1 pair + 6 code-only self-maps, dash/no-dash
  id forms defensively doubled): authored POST-HOC by the operator. **Ratified by owner
  delegation, 2026-07-04** (same session message as the CycleTimeAnalyzer run).

## Run + operator triage

Workflow run `wf_9544eb3e-820` (6 agents, Sonnet, ~240k subagent tokens, ~6.1 min, zero errors).
Lint: PASS 1719/2000 (default ceiling).

| Bucket | Run | After triage | Why changed |
|--------|-----|--------------|-------------|
| overlap-confirmed | 0 | 2 | +1 false divergence flipped, +1 Generate closure |
| spec-only-other-layer | 50 | 50 | exact as predicted (21 ui/api/settings + 29 domain-summary) |
| spec-only-divergent | 1 | 0 | MetricRagFromConfigurableThresholds — false: spec defines no concrete RAG mapping ("Per-metric RAG mapping undefined", spec findings); code's BR6/BR7 mapping is consistent with every spec commitment |
| spec-only-unimplemented | 1 | 0 | health-score-value-ranges (SR-16) — FsCheck property probes GREEN |
| code-only-precision | 12 | 6 | reader over-extracted health-score.md's Edge Cases bullets as pseudo-rules EC1–EC6; excluded on operator review (documentation, not ledger rules) — exclusion mechanically coincided with the updateRegistry ruleName-drop defect, reviewed and endorsed rather than silent |

## Generate closed

Eligible set was 1 rule (SR-16, sub-scores bounded to 0–100): two FsCheck property probes in
`HealthScoreSpecArmTests.cs` GREEN on first run over the spec's committed threshold domain
(non-negative, correctly ordered); the composite half is bounded by inspection (weighted average
of [0,100] sub-scores in SprintSummaryAnalyzer). The negative-threshold pathology documented in
the KB's Edge Cases is outside the spec's domain and deliberately not generated against.

Suite after Generate: 224 passed, 0 skipped.

## Spec-repair — RESOLVED (owner-delegated verdicts, 2026-07-04)

Six ambiguous rules were routed to spec-repair; the owner delegated the verdicts
("I rely on your best judgment"). Five settled KEEP CODE on attested evidence, one left open:

- **SR-1: amber band pinned [40, 75)** — "40-74" left decimal scores in (74, 75) unassigned;
  BR5 (mutation-gated) is the half-open band. spec.md rule 3 + settings + AC repaired.
- **SR-26/SR-27: exactly-amber pinned to 50 in both directions** — matches BR1–BR4. Rule 2 repaired.
- **SR-40: SP Completed/Committed polarity pinned neutral** — SprintSummaryAnalyzer builds the
  card with the unconditional "neutral" polarity; rule 12's self-contradiction removed.
- **SR-17: flat pinned = delta exactly 0** — MetricCardBuilder.ComputeDelta; response-shape line pinned.
- **SR-19 left open by decision** — display formats stay illustrative; no format contract pinned.

spec-rules stamp recomputed (sha256:3a93fb5c7035, delta re-check: repair edits only).

## Harness findings

Corroboration + new flavors appended to `docs/plugin-feedback/nexus-1.21.0-2026-07-04.md`
(Edge-Cases over-extraction by the code-arm reader is the new item).
