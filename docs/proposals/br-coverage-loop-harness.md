# Proposal — BR-Grounded Test-Coverage Loop Harness

**Status:** Idea captured + research-validated — not scheduled
**Date:** 2026-06-11 (v2 — revised after deep-dive research)
**Research:** [br-coverage-loop-harness-research.md](br-coverage-loop-harness-research.md) (207 sources, 5 threads; raw agent output in `docs/audit/research-raw/`)
**Origin:** User idea (verbatim intent): a loop-harness for continuously improving tests & test coverage — grounded in *real* business rules, not hallucinations. Analyze code → reverse-engineer → extract all business rules → cover them with tests → while covering, discover edge cases / rules not initially considered → everything evaluated with confidence.

## Problem

1. Coverage-driven test generation pins *incidental* behavior. Research confirms worse: coverage-**gated** LLM generation is actively harmful — CoverAgent-style tools rejected 470 bug-revealing tests and 59.6% of their final suites *validated* bugs instead of detecting them (arXiv:2412.14137). Without gates, ~36% of LLM code descriptions contain at least one hallucination (arXiv:2404.00971).
2. `docs/kb/` is referenced by CLAUDE.md as the business-rules knowledge base, but it is **empty**. The rules live only in code.
3. No systematic edge-case discovery — they surface ad hoc during review cycles.

## Core Idea

**Mine → Verify → Cover → Discover → repeat until dry.** Every artifact carries evidence + confidence. Nothing enters the ledger without a quote-grounded citation; nothing counts as covered without a mutation kill.

```
   Roslyn evidence index (deterministic, pre-LLM)
        │
        ▼                                              ┌──────────────┐
   A. MINE          B. VERIFY            C. COVER      │  D. DISCOVER │
   LLM on grounded  collaborative   ──►  rule tests ──►│  boundary    │──┐
   slices, quote-   refutation +         (PBT where    │  analysis →  │  │
   first extraction confidence gate      possible) +   │  candidate   │  │
        ▲                │               mutation kill │  BRs         │  │
        │                └─► contradiction / low-conf  └──────────────┘  │
        │                    → HUMAN QUEUE (P0/P1/P2 lanes)              │
        └────────────────────────────────────────────────────────────────┘
   Stopping: EXTERNAL signals only — dry counter + hard cap + mutation-score ratchet
```

### Phase A — Mine (two-stage, Roslyn-grounded)
Single-pass LLM extraction caps at ~71.6% accuracy with fabrication as the dominant failure mode; every production BRE tool (IBM ADDI, AWS Transform, EvolveWare, Swimm) runs deterministic static analysis first, LLM second (research §2). Therefore:
1. **Stage 1 — Roslyn evidence index** (deterministic): `CSharpSyntaxWalker` + `SemanticModel` identifies aggregates/VOs by base type, extracts guard clauses, `throw` expressions, FluentValidation chains (`RuleFor()`, `Must()`), builds a file:line index with resolved symbols. 70–80% of code is infrastructure noise — this stage filters it out before any LLM call.
2. **Stage 2 — LLM mining on grounded slices**: the LLM receives annotated slices (not raw files), extracts only rules **directly stated** in the slice (explicit-only policy), **quote-first**: the verbatim code quote is extracted *before* the rule statement is written.
- Each candidate BR record: `{ rule_id, statement, evidence: [file/line/snippet/kind], source_layer: domain|application, bounded_context, aggregate, confidence, status }`.
- **Evidence-or-reject:** no Roslyn-confirmed code location → rejected at intake.
- Mine both domain guard clauses AND FluentValidation validators — different rule categories, both binding.

### Phase B — Verify (the anti-hallucination gates, research-corrected)
1. **Quote-entailment check** — verify the extracted quote actually entails the stated rule (citation alone is insufficient: up to 57% of LLM citations are post-rationalized — Wallat et al.). Quote-first ordering + entailment verification is the Anthropic-Citations-validated pattern (10% → 0% source hallucinations at Endex).
2. **Collaborative refutation** — NOT zero-sum debate (competitive framing *degrades* accuracy up to −15% F1 via debate hacking). Refuter agents use "find missing points" framing and must quote counter-evidence by exact string match (ColMAD pattern, +2–5% F1). **Proposer and refuter use architecturally distinct base models** — same-model pairs share correlated blind spots (~26% of code hallucinations are the Library/Project class both would miss).
3. **Confidence = measured, not verbalized.** Verbalized high/medium/low is the weakest signal (ECE ≈ 0.10 systematic overconfidence). Confidence comes from **semantic consistency across 3–5 independent extraction samples** + post-hoc signals; verbalized bands only as a secondary channel; never CoT-derived confidence. Routing:
   - consistent across samples + refutation survived + no conflicts → auto-proceed to Cover
   - inconsistent / interpretation needed → human review queue (P1, batched by similarity)
   - contradicts KB or another rule → **P0 immediate lane, never auto-resolved, never batched** — escalation package carries both rule statements + both source spans + conflict type
4. **KB cross-check** — verified rules promoted to `docs/kb/` (kb-entry-schema skill + Karpathy wiki pattern, see State & Artifacts).
5. Expected escalation volume: 16–25% of candidates is the production norm — the human queue must be volume-sized from day one or it becomes the bottleneck.

### Phase C — Cover (pin verified rules — mutation-gated, never coverage-gated)
- **The only valid oracle is the explicit rule statement** — never snapshotted observed behavior (except the time-limited bootstrap below).
- **Mutation kill is the acceptance gate** (Meta ACH pattern — the strongest structural anti-hallucination gate; 32% mutant kill rate vs 5.3% for coverage-guided; 73% engineer acceptance in production): Stryker.NET finds surviving mutants → LLM (full class-under-test context, zero-shot, mutant description in prompt) writes targeted killer tests → Stryker confirms the kill → accept. **Comment-stripping equivalence pre-filter** before the gate (lifts P/R from 0.79/0.47 to 0.95/0.96; without it ~25% of mutant cycles are wasted).
- **Coverage is never an acceptance criterion anywhere in the loop** — informational only. Never discard a test because it fails on current code (it may be revealing a bug); route it to the human queue as a candidate bug instead.
- **Property-based tests are the preferred rule-encoding form** where the rule shape permits (round-trip, idempotency, invariants-after-commands): FsCheck 3, one custom `Arbitrary<T>` + shrinker per value object — the generator is the executable definition of "valid domain input". Each property validated by killing its target mutants.
- **Traceability anchors:** `[Trait("RuleId", "RULE-NNN")]` + `[Fact(DisplayName = "RULE-NNN: …")]`; CI gate asserts every verified KB rule has ≥1 trait-matched test.
- **Characterization-vs-specification rule:** when code behavior and apparent intent diverge, the harness never decides. Pin current behavior + escalate as candidate bug. A green test proves rule-implementation *consistency*, not business *correctness* — extracted rules inherit implementation bugs, so the human gate on rule intent is permanent.
- **Bootstrap (Pass 0, time-limited):** Verify snapshot tests as a non-regression scaffold before rule-encoding tests exist — with an explicit expiry/replacement plan (snapshots cement bugs if not retired).

**Adoption #3 — 5-gate battery (*method — working copy → candidate for a nexus(-dotnet) skill once the pilot proves it*).** An employer-internal harness's coverage flavor defines and mechanically enforces a 5-gate honesty battery (`measure.py:170-181`). Adopt these engineered, validated gate forms:

| Gate | What it checks | Harness source |
|------|---------------|------------|
| `suite_green` | All tests pass (both double-runs) | `measure.py:171` |
| `no_flaky` | Identical pass/fail counts across the two runs | `measure.py:172-174` |
| `mutation_floor` | Sampled kill-rate ≥ floor (per experiment); full-scope at certify | `measure.py:183-201` |
| `no_new_skips` | Skip count must not increase vs SOTA baseline | `measure.py:176` |
| `char_pin` | Prod-source touch: test-count-didn't-shrink proxy (**caveat:** this is a proxy — `baseline suite_green AND collected >= base_collected` at `measure.py:179-181`; NOT a manifest pin-check; a real characterization-pin must be reimplemented for the BR flavor) | `measure.py:178-181` |

The `dotnet-xunit.yaml` command and glob shapes are directly reusable:
- Coverage: `dotnet test --nologo --collect:"XPlat Code Coverage"` → `**/TestResults/**/coverage.cobertura.xml`
- Mutation: `dotnet stryker --mutate {targets} --reporter json` → `**/StrykerOutput/**/mutation-report.json`

See nexus boundary: the evaluation of an employer-internal harness (a private document, not in this repo), § Boundary & sequencing.

### Phase D — Discover (the loop's value)
- Boundary analysis per covered rule: empty collections, off-by-one, null/missing domain values, timezone, rounding in metric formulas, partial-failure paths.
- Each discovery is a *candidate BR* → feeds back into Phase B at the Roslyn-grounding step. No discovery skips verification.

### Termination — external signals only
**No LLM self-assessment anywhere in stopping or correction** — intrinsic self-correction degrades performance (Huang et al., ICLR 2024); "the LLM says nothing new found" is the coherence trap. Stopping is multi-signal:
- Dry counter: N consecutive iterations (3–5) with zero net-new verified rules
- Hard budget cap (~20 iterations; deployed loops converge within ~5)
- Plateau detection on mutation score
- **Ratchet:** suite mutation score never regresses — a regression means the harness is broken; halt.

## Anti-Hallucination Gates — research verdicts

| # | Gate | Research verdict | Correction applied |
|---|------|------------------|--------------------|
| 1 | Evidence-or-reject (file:line) | Validated but insufficient — 57% of citations post-rationalized | Quote-FIRST ordering + entailment check + Roslyn grounding |
| 2 | Adversarial refutation | Validated only if collaborative — competitive debate −15% F1 | Collaborative framing, exact-quote counter-evidence, distinct base models |
| 3 | Confidence gating (verbalized H/M/L) | **Weakest gate as originally designed** — ECE ≈ 0.10 overconfidence | Semantic consistency over 3–5 samples + post-hoc signals; verbalized = secondary |
| 4 | Mutation proof | **Strongest gate** — structural, not statistical (Meta ACH, production scale) | Keep; add comment-stripping equivalence pre-filter |
| 5 | Contradictions → human | Correctly designed — 16–25% escalation is the production norm | Add queue engineering: P0/P1/P2 lanes, context-complete packages |
| + | (new) Reward-hacking defenses | Uninstructed reward hacking is empirically real | Agent gets **no write access** to Stryker config, ledger schema, or eval infra; holdout bug set; eval-edit monitoring. **Adoption #2** (*method — working copy → candidate for a nexus(-dotnet) skill once the pilot proves it*): the harness's `prd.md §3` enumerates 7 named attacks (scope exclusion, assertion-free tests, flaky tests, silent behavior change, skipping tests, denominator shrink, no-mutation-tool stack); `cookbook.md §TRAPS` adds 13 rows (coverage-as-target, %-gate-gaming, aggregate blindness, change-detector/over-mocked tests, 100%-kill chasing, size-confounded confidence, and more). Use as the red-team checklist for this loop's defenses. See nexus boundary: the evaluation of an employer-internal harness (a private document, not in this repo), § Boundary & sequencing. |
| + | (new) No LLM self-judgment | Self-correction without external feedback degrades output | All correction grounded in `dotnet test` / Stryker output (CRITIC pattern) |

## State & Artifacts

- **BR ledger = `docs/kb/`** following the Karpathy three-layer wiki pattern: `rule_id` primary key; five non-negotiable frontmatter fields (`rule_id`, `evidence[]` as structured YAML, `confidence`, `status: active|superseded|disputed|stale`, `last_verified`); plus `bounded_context`, `aggregate`, `kind: invariant|policy|architectural`, typed `relates_to` edges. Entries ≤ ~2k tokens. **`index.md` is load-bearing** — first artifact written, last updated, every run. Supersession, not deletion.
- **Loop state:** stateless iterations against the durable ledger, versioned by git SHA; per-rule episodic records of failed verification attempts (prevents re-exploring dead ends); no conversation carry-over between iterations.
- **Rule-rot CI hook:** per-PR `git diff` → affected files → diff-scoped re-extraction → stale-flag broken citations → block merge if a *verified* rule goes stale. Full re-extraction only on domain-model refactors.
- **Rule ID convention:** flat `RULE-NNN`, classification via `kind:`. Same ID flows through KB frontmatter → `[Trait("RuleId",…)]` → CI coverage gate (and optionally ArchUnitNET `.Because()/.As()` for architectural rules).
- **Gherkin is OUT** for the domain layer (Adzic's own 10-year retrospective: feature files become buried automation scripts; double maintenance surface; no non-developer audience here). Plain `[Fact]` + `[Trait]`.

## Language-Agnostic Architecture (user requirement, 2026-06-11)

The harness must adapt to other languages later. Split: **language-neutral core** (loop controller, all five gates, confidence math, KB schema, rule IDs, ledger, stopping signals, HITL queue) + **Language Adapter** providing exactly three slots:

| Adapter slot | .NET (first adapter) | Future examples |
|---|---|---|
| Evidence indexer | Roslyn (`CSharpSyntaxWalker` + `SemanticModel`) | ts-morph / TS compiler API; JavaParser / Spoon |
| Mutation tool | dotnet-stryker | StrykerJS (same family); PIT (JVM) |
| Test stack + PBT | xunit.v3 + FsCheck | vitest + fast-check; JUnit + jqwik |

Nothing in `docs/kb/`, the rule-ID convention, traceability gates, or the loop state format may carry .NET assumptions. The Stryker family (.NET/JS/Scala) shares config concepts and baseline/threshold semantics — preferred when choosing future mutation tools.

## Recommended .NET Test Stack (research §3 — first Language Adapter)

| Layer | Tool | Note |
|-------|------|------|
| Framework | xunit.v3 | Stable, per-test isolation, best LLM training data; pin versions (3 majors in 6 months) |
| Runner | Microsoft.Testing.Platform | Microsoft's forward direction |
| Property-based | FsCheck.Xunit v3 | One `Arbitrary<T>` per value object |
| Snapshot (bootstrap only) | Verify.XUnit | Time-limited Pass-0 scaffold |
| Mutation gate | dotnet-stryker 4.14+ | `--with-baseline` for PR deltas; `break-at 70` → 75 → 80; **smoke-test .NET 10 support first** (implied, not documented) |
| Coverage (informational) | Coverlet.Collector | Never an acceptance criterion |
| Assertions | FluentAssertions v7 | — |

TUnit excluded (preview APIs, Coverlet-incompatible, thin LLM training data); re-evaluate at 1.0. Stryker scope = `Fokus.Domain` + `Identity.Domain` only; Persistence/API/Host excluded from mutation targets.

## Recommended Pilot

**Fokus.Domain analytics calculators** (delta/direction/polarity, sparkline builders, rolling averages, health metrics): densest pure-function BR area, no I/O, formulas with real edge cases — ideal first target with cheap mutation runs.

**Mandatory pre-start verification:** Stryker.NET smoke test against a .NET 10 `.Domain` project before committing to the stack.

**Pass-1 smoke test — re-scoped (adoption #1, *instance/test-run — stays project*).** The harness's coverage
flavor validated .NET 10 live: coverlet cobertura + Stryker.NET 4.14 on a **fixture classlib**
(`dotnet-xunit.yaml:1`, `prd.md §4`). Its `dotnet-xunit.yaml` ships working command shapes (`dotnet
test --nologo --collect:"XPlat Code Coverage"`, `dotnet stryker --mutate {targets} --reporter json`)
and report globs. This narrows but does not retire the hard risk. Re-scope the smoke test to the four
residual deltas:

| Delta to verify on Fokus.Domain itself | Why it can still fail |
|---|---|
| xunit.**v3** under Stryker 4.14+ | The harness fixture likely used xunit v2; 3 majors in 6 months |
| **Microsoft.Testing.Platform** runner | Stryker and coverlet-collector have historically lagged MTP; the harness's `test_cmd` is plain `dotnet test` |
| Real solution shape | Multi-project refs, central package management, analyzers — fixture was a single classlib |
| `--with-baseline` PR-delta mode | Not exercised by the harness's validation |

Smoke-test status remains **go/no-go**; scope is now the four deltas above, not the full stack.

**Pass-1 result — GO (2026-06-13, `adhoc-StrykerSmokeTest`).** Ran on `Fokus.Domain` with a minimal
xUnit v3 fixture (`HealthScoreCalculator`, 17 boundary tests) inside the real `SprintRituals.slnx` + CPM.
Stryker.NET 4.14.2 **killed 32 mutants** on the tested class → the mutation kill-loop works. Per-delta:

| Delta | Verdict | Note |
|---|---|---|
| xunit.v3 under Stryker 4.14+ | ✅ — **only via Stryker's MTP runner** | The default **VSTest** runner is broken for xUnit v3 → 0 kills ([stryker-net #3117](https://github.com/stryker-mutator/stryker-net/issues/3117), open). Must run `--test-runner mtp`. |
| Microsoft.Testing.Platform runner | ✅ (preview) | Shipped in Stryker 4.13; required for xUnit v3. Preview — per-test coverage incomplete, so use `coverage-analysis: off` (or `all`), not `perTest`. |
| Real solution + CPM + analyzers | ✅ | Test project builds in the real solution under central package management. |
| `--with-baseline` PR-delta | ✅ (mechanically) | Baseline created + reused; works. |

**Net config that works:** a `stryker-config.json` with `"test-runner": "mtp"` + `"coverage-analysis": "off"`.
**Do NOT** add a repo-root `global.json` with `{ "test": { "runner": "Microsoft.Testing.Platform" } }` —
it crashes the .NET 10.0.301 SDK MTP handshake (`KeyNotFoundException: key '8'`); Stryker's flag works without it.
**Residual risk:** the MTP runner is preview ([#3424](https://github.com/stryker-mutator/stryker-net/issues/3424));
xUnit v2 + VSTest is the rock-solid fallback if it destabilizes.

## Decisions (user, 2026-06-11)

1. **Test stack confirmed** (table above) — with the binding constraint that the architecture stays language-flexible (see Language-Agnostic Architecture). The stack is the first Language Adapter, not the harness.
2. **Refuter model: Codex** (user-confirmed) — via existing OMC tooling (`/ask codex` / codex agents). Gemini stays available as a later A/B option.
3. **Escalation queue is async and non-blocking** — an escalated rule is parked (`status: disputed`), only that rule and its dependents freeze; the loop continues on all other rules. Human reviews in batches at their own cadence; the user is never a blocker for loop progress.
4. **Golden set = ~20 hand-confirmed business rules** used as a held-out benchmark for the harness's own precision/recall (the loop never sees them). Process: architect drafts candidates; user confirms/rejects (~30 min, once).
   **Source-laddering methodology (user direction):** golden rules are attested across the original project lineage in confidence-increasing order — original architecture/spec → feature-specs → feature-plans → current code (reverse-engineering). A rule attested at multiple independent layers gets higher confidence; spec-attested but code-absent = porting gap (candidate bug); code-only = incidental candidate, lower confidence. Original sources (read-only, recorded in CLAUDE.md): the original Fokus project (architecture, product, feature specs, plans, populated `docs/kb/`), the original Reflekt project (same lineage, port pending), the reference codebase (best-practices reference). This laddering also generalizes to the harness itself: spec-lineage artifacts are intent sources independent of code, mitigating the "extracted rules inherit code bugs" risk for ported codebases.

5. **Harness ownership resolved — team template harness; Pass-4 substrate candidate** (2026-06-13).
   `HANDS-ON.md:1-4` confirms the attendee is the owner; `CLAUDE.md:27` names an employer-internal harness
   as the team's template harness to co-evolve. §6.5 risk in the evaluation is retired. The harness remains
   the leading Pass-4 substrate candidate; decision deferred to the Pass-3 checkpoint against the
   criteria in the evaluation §6. Three-layer boundary: method → nexus(-dotnet) plugin skills (promotion
   candidate, gated on this pilot); instance data → this project (`sprint-rituals`); loop orchestration
   → the harness flavor. See the evaluation of an employer-internal harness (a private document, not in this repo), § Boundary & sequencing.

## Open Questions (remaining)

1. ~~Mutation proof mechanics~~ → answered: Stryker.NET + ACH workflow. **.NET 10 smoke test passed (GO, 2026-06-13)** — xUnit v3 via Stryker's MTP runner (VSTest broken); see Pass-1 result above.
2. Run cadence — on-demand per module vs recurring (e.g., after each pipeline close) + the per-PR rule-rot hook.

## Roadmap (agreed direction, 2026-06-11)

| # | Pass | Content | Output |
|---|------|---------|--------|
| 0 | Freeze | Confirmed golden set → `docs/audit/golden-set.md` (harness-excluded); DIV-1..4 user rulings applied; DIV-5..7 → backlog BUG/GAP rows | Frozen benchmark |
| 1 | Foundations (adhoc) | First test project (xunit.v3 + MTP + FsCheck + FluentAssertions), **Stryker.NET .NET 10 smoke test (go/no-go — re-scoped to four residual deltas, see below)**, KB scaffold (`docs/kb/index.md` + schema) | Test convention + validated tooling + KB skeleton |
| 2 | Pilot — one MANUAL loop iteration (adhoc) | Mine → Verify (Codex refutation + consistency sampling) → Cover ~10 rules mutation-gated → Discover; measured against golden set | Calibration: recall/precision, escalation rate, cost/rule, Stryker runtime |
| 3 | Evaluate (checkpoint) | Tune thresholds, batch sizes, `break-at`; go/no-go for automation | Tuned gate parameters |
| 4 | Automate | Workflow script (loop-until-dry, budget cap, mutation ratchet) + per-PR rule-rot CI hook | Push-button harness |
| 5 | Scale | Rest of Fokus.Domain → Identity.Domain → validators → Reflekt at port time | Growing KB + rule-pinned suite |

Design rules baked into the ordering: pilot before automation (calibrate, then script); step 1 carries the only hard external risk (Stryker/.NET 10 — pause point if smoke test fails); **hold-out is per-measurement, not forever** — after step 3 the golden rules are promoted into `docs/kb/` and tested like all rules; measurement honesty is preserved because miner input is production source only (never KB, never test projects).

## Next Step

Backlog rows (team lead to add when scheduling): *Pass 1 — Harness Foundations* and *Pass 2 — Harness Pilot (Fokus.Domain analytics)*, both `adhoc-`, with this proposal + research doc + frozen golden set as binding inputs. DIV-5..7 as independent BUG/GAP rows.
