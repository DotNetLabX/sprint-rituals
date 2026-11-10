# Nexus plugin feedback — v1.20.0 — 2026-07-03

Portable feedback file (ADR-1). Send to the plugin maintainer; entries apply in the plugin source
repo (`<plugin-repo>/plugins`).

Source project: **sprint-rituals**. Origin: solo-run `mine-from-spec` pilot, 2026-07-03 — the spec
arm of `mine-verify-cover` executed standalone against the three specs whose classes already carry
mutation-gated code-arm KB entries (M1 targets). This file is both **pilot evidence for the AC-6
verdict** (item 1) and one hook defect (item 2).

**Status:** READY for maintainer. Not applied locally (ADR-1).

---

## AC-6 pilot evidence: `mine-from-spec` spec arm — GO, with three conditions

- **Suggested target:** `nexus/skills/mine-verify-cover/SKILL.md` (the AC-6-gated deferrals: M1/M3
  merge machinery, Cover-from-spec) and `docs/specs/adhoc-SddLifecycle/definition/tech-spec.md`
  in the nexus repo (wherever AC-6 is adjudicated).
- **Action:** adjudicate AC-6; on GO, build the merge machinery with the three pilot-surfaced
  conditions below designed in.
- **Evidence:** three complete standalone spec-arm runs (Mine → Consolidate → Skeptic → artifact),
  orchestrated per the skill's execution topology (parallel clean-room `general-purpose` miners,
  fresh consolidate+skeptic, main session as orchestrator/writer):

  | Spec | Miner yields | Consolidated | Verified / ambiguous | Spec findings |
  |------|-------------|--------------|----------------------|---------------|
  | F13-BugRatio | 27 / 33 / 38 | 42 | 41 / 1 | 14 |
  | F12-CycleTime | 79 / 47 / 71 | 78 | 75 / 3 | 15 |
  | F8-SprintSummaryCard | 44 / 59 / 45 | 58 | 52 / 6 | 15 |

  Artifacts: `docs/specs/fokus/{F13-BugRatio,F12-CycleTime,F8-SprintSummaryCard}/definition/spec-rules.md`
  (stamp grammar honored, LF-normalized sha256, per-file provenance).
- **Quality signals (why GO):**
  - 3-miner consensus is strong where it matters: 70–85% of consolidated rules at 3/3 agreement;
    1/3 rules were almost all response-shape/UI commitments, not missed business rules.
  - The skeptic stage caught **two genuine spec self-contradictions** (F12 rework definition vs its
    own worked example; F8 BR-12 delta polarity "higher is better" vs "neutral" in the same rule)
    and repeatedly stripped miner-invented boundary precision ("at or below resets" where the spec
    says only "exceeds"/"below"). The adversarial Verify design does exactly what it exists to do.
  - Spec findings are a real second product: 44 findings across 3 specs, several of which the
    code-arm KB already resolves (e.g. health-score at-amber → 50, pinned by mutation-gated tests)
    — precisely the triage input the M1 merge needs.
- **Lesson — three conditions for the generate/merge half, all surfaced by the pilot:**
  1. **Layer-tag the rules.** Spec rules span domain calculation, API contract, and UI behavior in
     one flat list (F12: 78 rules, of which maybe 30 target `CycleTimeAnalyzer`). Cover-from-spec
     picks tests against "the plan's target surface" — that selection needs a per-rule layer tag
     (domain-calc | api | ui) at consolidate time, or the generator will aim UI rules at a domain
     class.
  2. **`ambiguous` must block generation for that rule.** An ambiguous verdict means the spec does
     not commit to one behavior — generating a red test from it would encode the miner's guess.
     Ambiguous rules route to spec repair (they are findings), not to Cover.
  3. **Plan for granularity mismatch in the merge.** Spec-rule count ≠ code-arm BR count and the
     mapping is not 1:1 (42 spec rules vs 27 code BRs for BugRatio; different atomicity choices).
     The registry/triage design should match on rule *content* (symbol + condition), not counts or
     names, and expect many-to-one both ways.

## Read-tracker false-positives on clean-room parallel miners

- **Suggested target:** the nexus read-tracker hook (the "Read each file once per round" reminder;
  agents-workflow Read Discipline enforcement script).
- **Action:** update — count reads **per agent**, not per round across all agents; or exempt reads
  performed inside spawned subagents.
- **Evidence:** fired on every mine-from-spec run this session (x2/x3 on F13, x2/x3/x4 on F12 and
  F8 spec.md) — 8+ warnings in one session, all false positives. Recurrence within one session;
  first session running this mode.
- **Lesson:** clean-room multi-miner stages *require* N independent agents to each read the same
  file in their own fresh context — that is the method's isolation guarantee, not a read-discipline
  violation. The tracker currently aggregates reads across concurrent subagents into the
  orchestrator's "round", so any parallel fan-out over a shared input trips it. Per-agent
  attribution fixes it without weakening the rule for the single-context case it was written for.

---

## Addendum (same day): F13 delta + Cover-from-spec mini-pilot — the deferred half works, and the conditions held

A manual triage-and-generate pass over F13-BugRatio, run to preview what the deferred M1 merge /
Cover-from-spec machinery will do. Strengthens the GO and empirically validates all three conditions.

**Delta (42 spec rules vs 37 code BRs):** ~18 overlap (independently confirmed by both arms),
~14 spec-only API-layer, ~6 spec-only UI/settings-layer, **4 spec-only/divergent domain-layer**,
~10 code-only precision rules (rounding, casing, defaults) the spec never pinned. Condition 1
(layer tagging) is confirmed load-bearing: only 4 of 24 spec-only rules target the analyzer.

**Mini-pilot** (4 tests asserting SPEC expectations, run against the shipped analyzer —
`Fokus.Domain.Tests/Analytics/BugRatioSpecArmPilotTests.cs`):

| Rule | Result | Verdict |
|------|--------|---------|
| SR-28 at-threshold streak | **RED** (streak counted 1, expected 0) | Real divergence: spec "exceeds" (>) vs code `>=` (BugRatioAnalyzer.cs:272) — and the code-arm suite *deliberately pins* the inclusive boundary. Two attested opposite truths at one boundary; triage must pick. |
| SR-23 no-SP bug → SP metrics | **RED** (TotalBugSp 5, expected 0) | Intentional drift: `GetEffectiveSp(DefaultSpPerBug)` came from a later feature; the F13 spec is stale. Validates the base-spec-only manifest caveat. |
| SR-23 no-SP → ticket metrics | GREEN | Both arms agree. |
| SR-42 priority ignored | GREEN | Both arms agree. |
| SR-31 active developers | resolved by inspection | Implemented upstream (`DeveloperRepository`, `.Where(d => d.IsActive)`) — a layer question, not a gap. |
| SR-30 threshold=0 edge | **not generated** | Spec-ambiguous — condition 2 applied: ambiguous rules must not generate tests. |

Red tests are KEPT, parked with `[Fact(Skip = "SPEC-CODE DIVERGENCE ... pending triage")]` plus the
observed values, so the suite stays green (218 passed / 2 skipped) while the divergences stay on the
record. **Design note for the machinery:** a spec-arm red against mutation-attested code needs a
distinct disposition state (`divergence-pending-triage`) — it is neither the code arm's "candidate
bug" (code is attested-intended) nor a discard (the spec commitment is real).

---

## Proposals for the merge / Cover-from-spec machinery (consolidated)

Distilled from the three runs + the F13 delta + the mini-pilot; numbered for reference from the
nexus-repo planning pass:

1. **`layer` field on every consolidated rule** (`domain-calc | api | ui | settings`), assigned at
   the consolidate+skeptic stage and emitted in `spec-rules.md`. Empirically load-bearing: only 4 of
   F13's 24 spec-only rules target the domain class; ungated generation would aim API/UI rules at
   the wrong surface. Cheap where it belongs — the skeptic already reads every rule.
2. **`ambiguous` verdict blocks test generation for that rule.** An ambiguous rule encodes a guess;
   it routes to spec repair (it is a finding), never to Cover. Applied manually to SR-30's
   threshold-0 edge — it filtered exactly the right rule.
3. **Merge matches on rule content (symbol + condition), never on names or counts.** 42 spec rules
   vs 37 code BRs for the same feature, different atomicity, many-to-one both ways. SR/BR numbering
   and agreement counts are per-arm artifacts; the join key is the behavior statement.
4. **A third disposition state: `divergence-pending-triage`.** A spec-arm red against
   mutation-attested code is neither `candidate bug` (the code behavior is deliberately pinned by a
   killing test) nor a discard (the spec commitment is real). Hit twice in one pilot (SR-28, SR-23);
   the merge should emit this state with the evidence pair (spec citation + code attestation) so a
   human rules once, durably.
5. **Triage output = the five delta buckets** observed on F13: `overlap-confirmed`,
   `spec-only-other-layer`, `spec-only-divergent` (→ state 4), `spec-only-unimplemented` (→ GAP
   backlog item), `code-only-precision` (→ spec/KB enrichment; includes cleanups like the dead
   `startIndex`). Every rule lands in exactly one bucket; nothing silently dropped.
6. **Stale-manifest signal.** SR-23's divergence traces to a later feature's amendment
   (`DefaultSpPerBug`) — a base-spec-only manifest documents v1 commitments, not current intent.
   Tag a divergence `suspect-stale-spec` when the code-arm KB attributes the behavior to a
   settings/feature source the mined spec predates, and route it to spec repair rather than
   code-bug triage.
7. **Upstream-layer resolution before red-test authoring.** SR-31 ("only active developers") is
   implemented in the repository, not the analyzer — one inspection settled it without a misleading
   wrong-layer red. Cover-from-spec should run an implemented-upstream check for rules whose layer
   ≠ target surface before generating anything.

## Evaluation summary

The spec arm is production-worthy at Mine→Verify depth: miner consensus is high where it matters,
the skeptic reliably converts miner-invented precision and spec self-contradictions into
`ambiguous` verdicts and findings, and the stamp/provenance grammar held across three runs. The
deferred generate/merge half is validated in miniature: a hand-run delta plus 4 generated tests
found two real spec-code divergences (one boundary disagreement, one stale-spec drift), correctly
withheld the one ambiguous rule, and kept the suite green by parking reds as skipped divergence
records. AC-6 recommendation: **GO**, with proposals 1–7 folded into the machinery's spec.

---

## Cross-reference

- Run artifacts (this repo): the three `spec-rules.md` files listed above; code-arm counterparts
  `docs/kb/{bug-ratio,cycle-time,health-score}.md` (all `mutation-gated`); pilot probes
  `src/Services/Fokus/Fokus.Domain.Tests/Analytics/BugRatioSpecArmPilotTests.cs` (2 green,
  2 parked divergences).
- The two spec self-contradictions are also actionable **in this consuming repo** as one-line spec
  fixes (F12 rework example; F8 BR-12 polarity) — tracked locally, not plugin-bound.
- Prior open feedback: `nexus-1.18.6-2026-06-29.md` item 2 (`group-scalars-into-value-object`
  skill) still `[DEFERRED]` upstream; unrelated to this file.
