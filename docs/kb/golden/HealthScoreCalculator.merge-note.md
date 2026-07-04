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
  id forms defensively doubled): authored POST-HOC by the operator. **Owner confirmation pending**
  (same flag as the CycleTimeAnalyzer run).

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

## Spec-repair (owner attention)

Six ambiguous rules excluded from generation, routed to spec-repair — **proposals only, not
settled by this run**. The code resolves each gap one specific way (mutation-gated), so these are
cheap spec pins, not behavior decisions:

- SR-1: composite bands don't partition decimal scores — (74, 75) unassigned; code: amber (BR5).
- SR-26/SR-27: exactly-amber sub-score unassigned in both directions; code: 50 (amber band inclusive).
- SR-40: SP Completed/Committed delta polarity — spec contradicts itself (higher-is-better vs
  neutral); code side lives in SprintSummaryAnalyzer, outside this merge.
- SR-17: "flat" delta direction condition undefined (response-shape rule).
- SR-19: display formats given only as illustrative examples.

## Harness findings

Corroboration + new flavors appended to `docs/plugin-feedback/nexus-1.21.0-2026-07-04.md`
(Edge-Cases over-extraction by the code-arm reader is the new item).
