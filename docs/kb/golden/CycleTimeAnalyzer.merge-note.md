# Merge run note — CycleTimeAnalyzer (M1, 2026-07-04)

Second live execution of the SDD M1 merge (nexus 1.21.0, `merge.workflow.js` — the adhoc-SddMergeGen
Step 9 runbook), run back-to-back with HealthScoreCalculator. Companion to the registry in this folder.
Operator: Solo session (autonomous, owner-directed "same as BugRatio" run).

## Arms and independence (runbook steps 1–2)

- Spec arm: `docs/specs/fokus/F12-CycleTime/definition/spec-rules.md` (78 rules, mined 2026-07-03,
  clean-room; `layer`/`criticality` facts added 2026-07-04 per the v1.21.0 consolidate schema —
  operator pass, pre-run).
- Code arm: `docs/kb/cycle-time.md` (59 rules, mutation-gated 2026-06-21, 100% reachable kill).
- Independence: manifests disjoint (each arm is a single distinct path); neither contains a golden
  set (`docs/audit/golden-set.md` absent in this repo) or the crosswalk. Pass, zero violations.
- Crosswalk (`CycleTimeAnalyzer.crosswalk.json`, 24 pairs + 35 code-only self-maps): authored
  POST-HOC by the operator from both arms' already-mined output — never a run input to either arm.
  **Owner confirmation pending** (the BugRatio run had pre-run owner confirmation; this run was
  autonomous — flagged for the owner to ratify or amend).
- Self-map workaround: unpaired code-arm ids are crosswalked to themselves so `updateRegistry`
  (1.21.0 feedback item 1 — drops rows without `ruleName`) persists them. Worked: 110/110 rows
  written, zero hand re-adds (the BugRatio run needed 17).

## Run + operator triage

Workflow run `wf_9dba7210-d9b` (6 agents, Sonnet, ~253k subagent tokens, ~5.9 min, zero errors).
`tokenCeiling` raised to 4096 (arg): the known-degenerate one-line-per-rule distillate (feedback
item 4) inevitably exceeds the 2000 default at 110 rows and would have halted the run post-registry;
the distillate stays a git-ignored nexus-side artifact — the hot-layer fold is the hand-written
index pointer, as in the BugRatio run. Lint: PASS 2980/4096.

| Bucket | Run | After triage | Why changed |
|--------|-----|--------------|-------------|
| overlap-confirmed | 24 | 29 | +5 Generate closures (3 probes GREEN, 2 by inspection) |
| spec-only-other-layer | 46 | 46 | exact as predicted (layer facts load-bearing again) |
| spec-only-divergent | 0 | 0 | none flagged — see caveat below |
| spec-only-unimplemented | 5 | 0 | all resolved implemented (SR-65/68/73 probes, SR-74/78 inspection) |
| code-only-precision | 35 | 35 | persisted via self-map workaround; layer/criticality filled by operator |

**Zero-divergent caveat:** the boundary comparator was vacuous this run — the code-arm reader
emitted no `boundary` fields, so string-compare neither confirmed nor refuted any pair (the same
extraction-noise artifact the BugRatio feedback flagged, opposite flavor: false overlap instead of
false divergence). The 24 overlap verdicts rest on the operator's semantic inspection during
crosswalk authoring plus the mutation-gated code-arm suite. Watched pair recorded:
DominantStageHighestAccumulated (spec "highest average" vs code MaxBy-of-sums — argmax-equivalent
under any per-developer-constant denominator; spec defines none).

## Generate closed

Eligible set was 5 rules; all resolved implemented — no parked reds, no owner verdicts required:

- SR-65 calendar-days, SR-68 no-SP-included, SR-73 scope-exclusion-independence: probes GREEN on
  first run in `CycleTimeSpecArmTests.cs` (fact tags per mine-verify-cover-dotnet).
- SR-74 query-time-config, SR-78 shared-pipeline: negative/architectural constraints — absence
  verified by inspection, zero tests generated (the correct outcome).

Suite after Generate: 224 passed, 0 skipped.

## Spec-repair (owner attention)

Three ambiguous rules excluded from generation, routed to spec-repair — **proposals only, not
settled by this run**:

- **SR-53 (rework counting) — the one item worth an owner verdict.** The spec's general definition
  ("re-entry into a previously visited stage") scores its own example 1 as 2, but the spec says 1;
  only an unstated backward-transitions-only reading reconciles it. The code (BR-13, mutation-gated)
  counts any re-entry — i.e. it matches the general definition and contradicts the spec's example.
  Owner should pin the intended definition; until then the code behavior stays pinned by the
  code-arm suite.
- SR-9 (funnel bottleneck "visually obvious" — presentational non-commitment).
- SR-66 (zero-completed sprint: "metrics zero" vs "percentiles null" conflict for the Median/P85
  cards).

## Harness findings

Corroboration + new flavors appended to `docs/plugin-feedback/nexus-1.21.0-2026-07-04.md`
(vacuous-overlap comparator flavor, write-agent `</content>` artifact, distillate ceiling trip).
