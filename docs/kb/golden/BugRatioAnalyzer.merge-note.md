# Merge run note — BugRatioAnalyzer (M1, 2026-07-04)

First live execution of the SDD M1 merge (nexus 1.21.0, `merge.workflow.js` — the adhoc-SddMergeGen
Step 9 runbook, previously written-only). Companion to the registry in this folder.

## Arms and independence (runbook steps 1–2)

- Spec arm: `docs/specs/fokus/F13-BugRatio/definition/spec-rules.md` (42 rules, mined 2026-07-03,
  clean-room; `layer`/`criticality` facts added 2026-07-04 per the v1.21.0 consolidate schema —
  operator pass, post-hoc).
- Code arm: `docs/kb/bug-ratio.md` (37 rules, mutation-gated 2026-06-22).
- `checkIndependence` (equivalent reimplementation of `harness/lib/independence-check.mjs`, pure):
  **pass, zero violations** — manifests disjoint; neither contains the golden set
  (`docs/audit/golden-set.md`, absent in this repo) or the crosswalk.
- Crosswalk (`BugRatioAnalyzer.crosswalk.json`, 20 pairs, 1:1): authored POST-HOC by the operator
  from both arms' already-mined output, **human-confirmed by the owner before the run** — never a
  run input to either arm.

## Run + operator triage

Workflow run `wf_3b60b3c6-47c` (6 agents, Sonnet, ~235k tokens, ~4.5 min, zero errors) produced the
initial registry; an operator triage pass the same day corrected two harness defects (recorded in
the registry changelog, rows superseded never deleted):

| Bucket | Run | After triage | Why changed |
|--------|-----|--------------|-------------|
| overlap-confirmed | 2 | 19 | 16 false divergents flipped (boundary prose string-compare); + removed-tickets-excluded (BR-12 clause (a), 1:1 crosswalk limit) |
| spec-only-divergent | 18 | 2 | only AlertThresholdBoundary + NoSpTicketSpHandling are real |
| spec-only-other-layer | 17 | 17 | exact as predicted (layer facts load-bearing) |
| spec-only-unimplemented | 4 | 3 | removed-tickets-excluded resolved implemented |
| code-only-precision | 17 (bucketed, 0 written) | 17 | rows dropped by `updateRegistry` ruleName/id key defect, re-added by hand |

Spec-repair list (ambiguous, excluded from generation): SR-41 (F9 discrepancy acknowledgment).

## Triage RESOLVED (owner verdicts, 2026-07-04) + Generate closed

Both divergences adjudicated **KEEP CODE, repair spec** — the original Fokus attests the same
behaviors (`>=` in its KB; `DefaultSpPerBug` from its later `adhoc-UIImprovements` increment):

- **AlertThresholdBoundary**: spec.md rule 10 + SR-1/SR-28 repaired to ">= inclusive";
  spec-rules.md stamp recomputed (`sha256:0ae232302d55`, delta re-check recorded).
- **NoSpTicketSpHandling**: DefaultSpPerBug exception written into spec.md rule 6 / SR-23.

Obsolete parked reds removed from `BugRatioSpecArmPilotTests.cs` (record lives in this registry's
changelog); the two behaviors stay pinned by the mutation-gated code-arm suite.

**Generate (Cover-from-spec) closed:** eligible set was 3 rules. SR-32 probe authored → GREEN
(implemented); SR-42 probe GREEN (implemented); no-rolling-average is a negative constraint —
absence verified by inspection, zero tests generated (the correct outcome). All probes carry
`[Trait]` fact tags (layer/criticality/runtime-cost). Suite: 219 passed, 0 skipped.

**Final buckets:** overlap-confirmed 24, spec-only-other-layer 17, code-only-precision 17,
spec-only-divergent 0 (2 resolved), spec-only-unimplemented 0, spec-repair 1 (SR-41, ambiguous).

## Harness findings

Routed to the maintainer via `docs/plugin-feedback/nexus-1.21.0-2026-07-04.md` (4 defects/gaps,
positives, and proposals).
