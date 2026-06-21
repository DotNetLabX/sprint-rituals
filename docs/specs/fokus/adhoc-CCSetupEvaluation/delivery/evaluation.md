# Agentic Setup Evaluation

**Date:** 2025-05-24
**Evaluators:** Critic (oh-my-claudecode:critic), Architect (oh-my-claudecode:architect)
**Scope:** Full `.claude/` setup — agents, rules, conventions, skills, pipeline protocol

---

## Score Summary

| # | Category | Critic | Architect | Avg |
|---|----------|--------|-----------|-----|
| 1 | Completeness / Separation of Concerns | 9 | 9 | 9.0 |
| 2 | Coherence / Coupling | 8 | 8 | 8.0 |
| 3 | Clarity / Information Architecture | 8 | 9 | 8.5 |
| 4 | Maintainability / Protocol Robustness | 9 | 7 | 8.0 |
| 5 | Redundancy / Extensibility | 7 | 8 | 7.5 |
| 6 | Error Recovery / Context Efficiency | 9 | 8 | 8.5 |
| 7 | Developer Experience / Decision Authority | 8 | 9 | 8.5 |
| 8 | Scalability / Feedback Loops | 8 | 8 | 8.0 |
| 9 | Guardrail Effectiveness / Cognitive Load | 9 | 7 | 8.0 |
| 10 | Learning Loop / Anti-fragility | 8 | 7 | 7.5 |
| | **Overall** | **8.3** | **8.0** | **8.15** |

---

## Top Strengths (consensus across both evaluators)

### 1. Battle-hardened anti-patterns (Critic) / Hub-and-spoke with user-decision interception (Architect)

Both evaluators highlight the system's maturity. The anti-patterns sections in `developer.md`, `reviewer.md`, and convention files are drawn from real pipeline failures, not theoretical risks. Items like "Attributing pre-existing build failures to the current feature" and "Skipping plan steps silently" encode institutional memory that prevents repeated mistakes.

The hub-and-spoke communication model through the team lead, combined with the User Decision Guardrail (`architect.md:56-63`), creates a system where scope changes and requirement reversals cannot bypass the user.

### 2. Two-phase checkpoint architecture / File-based handoffs with format contracts

The mandatory Analyze-then-Execute pattern for both architect and developer catches ambiguities before they become costly deviations. Every inter-agent handoff goes through a file with a defined format (`agents-workflow.md:112-118`). The "write first, message second" rule ensures the file is always the source of truth, not the ephemeral message.

### 3. Layered information architecture

The five-tier system (rules / conventions / agents / skills / stack conventions) with the README.md "Where Do I Put This?" decision tree makes placement unambiguous. The loading model (auto-load vs `@`-load vs on-demand) balances context availability against context budget. Both evaluators independently scored this 9/10.

### 4. Learner-driven feedback absorption

The learner agent closes the feedback loop structurally. The recurrence threshold of 2, critical exception list (data loss, security, build-breaking promote immediately), and process-vs-stack classification prevent contamination of system files while ensuring high-severity lessons propagate fast.

---

## Detailed Scores and Justifications

### Critic Perspective

#### 1. Completeness — 9/10
All necessary roles, handoffs, and edge cases are covered. Clear entry points for every workflow type. The learner closes the feedback loop. **Gap:** No formal designer/UX review agent for the Vue/Tailwind frontend.

#### 2. Coherence — 8/10
Pieces fit together well. Layered information architecture is clearly defined. Model assignments in the agent table match frontmatter. **Issue:** The "no artifacts" claim for solo in `agents-workflow.md:91` contradicts solo's artifact table which writes `implementation.md` and `lessons.md`. Minor but misleading.

#### 3. Clarity — 8/10
Agent files are remarkably clear about boundaries. Anti-patterns sections are actionable. **Drop-off:** `team-lead-operations.md` spawn modes (~200 lines) lack a quick decision tree. The architect's standalone mode instructions are easy to miss (single paragraph at line 151-152).

#### 4. Maintainability — 9/10
Separation of concerns is excellent. The learner agent prevents manual maintenance drift. Skills as authoritative pattern sources propagate changes automatically. **Concern:** 36 skills + 8 agents + 10 rules + 7 conventions with no automated lint for broken cross-references.

#### 5. Redundancy — 7/10
Intentional boundary-rule duplication is justified. **Unjustified duplication:**
- Debugging protocol: nearly verbatim between `developer.md` (103-109) and `solo.md` (73-79)
- Boy-scout section: verbatim identical between both files
- "Codebase Facts" rule: appears in 3 agent files with different wording
- Exploration protocol: near-identical in both developer and solo
- Cycle cap (3 fix cycles): stated in 5 different files

#### 6. Error Recovery — 9/10
Comprehensive and graduated. Build failure checks, agent timeout recovery (4-step), review cycle cap with escalation, phase failure options, unattended mode defaults, resume from crash via communication log. **Gap:** No handling for corrupted/truncated `implementation.md` or `communication-log.md`.

#### 7. Developer Experience — 8/10
Clear workflow with skill-first protocol and completion checklist. **Friction:** Developer loads 6 `@` files plus 10 rules plus 180-line agent file. The two-phase workflow adds a mandatory round-trip through team lead even when developer has zero questions.

#### 8. Scalability — 8/10
Hub-and-spoke scales to additional agents. Feature isolation via `docs/specs/{slug}/`. Plan splitting for complexity. **Limits:** Serial pipeline (one feature in-flight per session). Team lead as sole hub is a context bottleneck. Skill inventory scanning scales linearly.

#### 9. Guardrail Effectiveness — 9/10
Guardrails are specific and target real failure modes. The meta-layer guardrail (`guardrail-enforcement.md`) catches edge cases. **Weakness:** Honor-system enforcement — no external validation that agents actually followed their guardrails.

#### 10. Learning Loop — 8/10
Well-designed capture → classification → promotion pipeline. **Gaps:** Learner is "skip by default" — lessons may never be processed without user initiative. No measurement of whether promoted lessons actually prevented recurrence. `[TRACKED]` single-occurrence items have no automatic re-check.

### Architect Perspective

#### 1. Separation of Concerns — 9/10
Agent responsibilities are exceptionally well-partitioned with explicit "What You Never Do" sections defining negative boundaries. **Minor overlap:** Both architect (Step 1) and reviewer (Step 2) write to `review.md`, differentiated by scope but sharing a destination.

#### 2. Coupling — 8/10
Agents coupled through file-based handoffs and hub-and-spoke messaging — correct for auditable pipelines. No shared runtime state. **Concern:** Team lead as single point of failure. Communication log mitigates session-boundary failures but mid-session context loss has no recovery path.

#### 3. Information Architecture — 9/10
Five-layer hierarchy is well-designed with clear loading mechanisms. KB navigation provides context-efficient discovery shortcuts. `@` references are well-targeted per agent. **Gap:** README's "Where Do I Put This?" doesn't cover `docs/kb/` entries.

#### 4. Protocol Robustness — 7/10
Solid handling for common failures (review caps, timeouts, idempotency). **Missing:**
- No locking/merge strategy for concurrent file modification
- No handling for partial/truncated file writes on timeout
- No fallback if critic spawning fails (architect Phase 2)
- No guidance if logged branch was deleted/rebased

#### 5. Extensibility — 8/10
Adding agents, skills, and conventions follows clear patterns. **Friction:** Pipeline topology changes require coordinating edits across 4 files (`pipeline-protocol.md`, `team-lead.md`, `team-lead-operations.md`, `agents-workflow.md`).

#### 6. Context Efficiency — 8/10
Tiered loading, KB shortcuts, team lead discipline, message size contracts, architect delegation to Explore. **Concern:** Solo sessions for trivial fixes load ~150 lines of pipeline-specific rules they never use. Developer loads heavy `@` references upfront.

#### 7. Decision Authority — 9/10
Strongest aspect. Explicit authority limits per agent. Three-tier escalation chain (Developer → PO → User). User Decision Guardrail lists specific prohibited actions. Team lead is explicitly "a router, not a decision-maker."

#### 8. Feedback Loops — 8/10
Well-structured per-feature lessons, dedicated learner agent, anti-pattern absorption, known deviations tracking. **Concern:** Learner is optional — lessons accumulate without processing if user consistently declines.

#### 9. Cognitive Load — 7/10
Agent files are self-contained with clean activation boundaries. **Heavy loads:**
- Team lead: 291 lines, 3 spawn modes, 4 launch scenarios, 6 routing rules
- Architect: ~250 lines of instructions
- Developer: total context at activation (agent file + 10 rules + 6 `@` references + task files) is substantial
- Some content duplication between `team-lead.md` (88-157) and `team-lead-operations.md`

#### 10. Anti-fragility — 7/10
Resume protocol, circuit breakers, idempotency checks, timeout recovery, feedback absorption. **Degradation modes:**
- Context saturation after 2+ fix cycles degrades quality
- Cascading question escalations stall pipeline if user unavailable
- Lesson accumulation without processing
- No quality monitoring (can't detect reviewer rubber-stamping)

---

## Contradictions Found

### 1. Solo "no artifacts" vs solo artifact table (Critic)
`agents-workflow.md:91` claims solo produces "no artifacts," but `solo.md:91-99` defines an artifact table that writes `implementation.md` and `lessons.md`. **Severity: Minor** — the agent file is authoritative.

### 2. Architect critic spawning bypasses hub-and-spoke (Critic)
`architect.md:147` spawns the critic directly via `Agent()`, bypassing the team lead routing. Similarly `po.md:159-166`. This is arguably correct (critic is a quality gate, not a pipeline participant) but the exception is unstated. **Severity: Minor.**

### 3. Plan format cross-reference is misleading (Critic)
`architect.md:141` says "following the format in the coordination protocol" but the plan format is actually in the skill's `plan-template.md`, not in `pipeline-protocol.md`. The skill reference is correct but the text points to the wrong source. **Severity: Minor.**

---

## Structural Weaknesses (Architect)

### 1. Team lead single point of failure with no deputy
If the team lead loses context mid-session, the entire pipeline stalls. Communication log helps for session-boundary recovery but not mid-session context loss. No "deputy" agent can take over routing.

### 2. Circular escalation potential
Developer → Architect → Developer question loops are capped at 3 per "same area," but "same area" is not precisely defined. Plan changes by the architect could create new ambiguities in a different area, theoretically cycling indefinitely across areas. **Suggestion:** Add a global question cap (e.g., 10 total per feature).

### 3. Critic spawning deadlock risk
If a critic hangs or false-REJECTs a valid artifact, the spawning agent (architect/PO) is stuck with no timeout or override mechanism. Requires user intervention but doesn't explicitly surface this to the user.

### 4. Unattended mode fragility
Skips critic review, operates with lower-confidence PO answers, aborts on any escalation. Creates a "succeeds trivially or fails entirely" dynamic with no graceful degradation for complex features.

---

## Improvement Proposals (ranked by impact)

### HIGH Impact

| # | Proposal | Source | Effort |
|---|----------|--------|--------|
| 1 | **Extract shared agent protocols** — Move debugging protocol, boy-scout invocation, codebase-facts rule, exploration protocol from duplicated agent files into `.claude/rules/codebase-facts.md` and `.claude/conventions/developer-protocols.md`. Both developer and solo `@`-reference them. | Critic | Low |
| 2 | **Pipeline topology declarative extraction** — Single table in `pipeline-protocol.md` with Phase → Agent → Entry Condition → Exit Condition → On Failure → Next Phase. Other files reference it instead of restating the sequence. Reduces topology change coordination from 4 files to 1. | Architect | Medium |
| 3 | **Context pressure management** — After 2+ fix cycles, spawn a fresh developer scoped to remaining review findings only, not the full transcript. Prevents quality degradation under stress. | Architect | Low |
| 4 | **Make learner processing less optional** — Auto-trigger learner after every 3rd completed feature or any feature with 5+ lesson items. Add staleness metric to team-lead status check: "X lessons across Y features unprocessed." | Critic | Low |

### MEDIUM Impact

| # | Proposal | Source | Effort |
|---|----------|--------|--------|
| 5 | **Team lead cognitive load reduction** — Deduplicate content between `team-lead.md` (88-157) and `team-lead-operations.md`. Move all operational mechanics fully into operations file. Reduce team-lead.md to pure decision routing. | Architect | Medium |
| 6 | **Spawn-mode decision tree** — Add a quick-reference table at top of `team-lead-operations.md`: Signal → Mode mapping (Default → Background, Live output → Foreground, 5+ handoffs → Persistent, Unattended → Background forced). | Critic | Low |
| 7 | **Cross-reference lint** — Before promoting a lesson that renames/deletes a skill or convention, grep all agent files for references to the old name. Add this verification step to `improve-skills` and `improve-flow` skills. | Critic | Low |
| 8 | **Protocol robustness additions** — (a) Critic spawn fallback to self-review in architect Phase 2. (b) Truncated `implementation.md` detection in developer Phase 1. (c) Branch-not-found handling in team lead resume. | Architect | Low |
| 9 | **Pipeline stall detection** — Team lead checks that step count advances after each developer resume. Two consecutive resumes with no new completed steps → alert user. | Architect | Low |

### LOW Impact

| # | Proposal | Source | Effort |
|---|----------|--------|--------|
| 10 | **Conditional rule loading** — Split `.claude/rules/` into `core/` (always loaded) and `pipeline/` (loaded only for pipeline agents). Saves ~80 context lines in solo sessions. | Both | Low |
| 11 | **Global question cap** — Add a hard ceiling of ~10 total developer questions per feature regardless of area, preventing cross-area cycling. | Architect | Low |
| 12 | **Skip "all clear" round-trip** — Let developer self-transition to Phase 2 when analysis produces zero questions. Team lead is notified but doesn't need to actively resume. | Critic | Low |
| 13 | **Add `docs/kb/` to README decision tree** — "Domain knowledge → `docs/kb/`" row in the "Where Do I Put This?" section. | Architect | Trivial |

---

## User Triage

After reviewing all findings, the project owner triaged every item. The results reveal a calibration gap in the evaluation itself.

### Contradictions — Verdict

| # | Finding | Verdict |
|---|---------|---------|
| 1 | Solo "no artifacts" text vs artifact table | Real — one-line text fix needed |
| 2 | Critic spawning bypasses hub-and-spoke | Non-issue — the evaluation itself notes the critic is a quality gate, not a pipeline participant. Finding refutes itself. |
| 3 | Plan format cross-reference misleading | Already fixed in prior edits |

### Proposals — Accepted (4 of 13)

| # | Proposal | Why |
|---|----------|-----|
| 1 | Extract shared protocols (debugging, boy-scout, exploration) | Genuine duplication — these are operational protocols, not boundary rules. Saves ~40 lines per agent, prevents drift. |
| 7 | Cross-reference lint in improve-skills/improve-flow | Cheap safeguard against silent breakage on rename. |
| 13 | Add `docs/kb/` to README decision tree | One line, trivial. |
| — | Solo "no artifacts" text fix | One line. |

### Proposals — Skipped (9 of 13)

| # | Proposal | Why Skip |
|---|----------|----------|
| 2 | Pipeline topology declarative table | Over-engineering — flow is clear enough from existing files |
| 3 | Fresh developer spawn after 2+ fix cycles | Cycle caps + architect escalation already handle this |
| 4 | Auto-trigger learner every 3 features | Adds automation the user doesn't want — user controls when to run the learner |
| 5 | Team lead cognitive load reduction | Already done in prior session |
| 6 | Spawn-mode decision tree | Nice-to-have, not a real pain point |
| 8 | Protocol robustness (critic fallback, truncated files) | Edge cases that haven't caused problems |
| 9 | Pipeline stall detection | Complexity for a problem that doesn't exist yet |
| 10 | Conditional rule loading | Already in Design Decisions table: context cost < missed guardrail cost |
| 11 | Global question cap | Arbitrary number, current area-based caps work |
| 12 | Skip all-clear round-trip | Team lead involvement is the point |

---

## Meta-Evaluation: How Good Was This Evaluation?

### Calibration Problem

The evaluation was **thorough but poorly calibrated**. It read every file carefully — that part was excellent. But it could not distinguish between real problems and theoretical ones.

**Net actionable output:** One substantial improvement (#1 extract protocols), three one-line fixes, and nine proposals that were either speculative, already handled, or contradicted intentional design decisions. That is thin for two Opus agents reading 40+ files.

### Scoring Was Inflated in Its Harshness

The 7/10 scores (Protocol Robustness, Cognitive Load, Anti-fragility) were driven by speculative scenarios that haven't occurred and are already mitigated by existing mechanisms:

- "Concurrent file modification" — the pipeline is serial by design
- "Critic hangs with no fallback" — hasn't happened
- "Branch deleted during resume" — hasn't happened
- "Context saturation after 2+ fix cycles" — caps + escalation handle this
- "Learner not auto-triggered" — intentional user control, not a gap

These observations are valid in a vacuum but they penalize the system for problems that don't exist. The 7s should be 8s, pushing the realistic overall from 8.15 to **~8.5-8.7**.

### What the Evaluation Missed

- **Did not challenge its own findings.** Both evaluators flagged contradiction #2 (critic bypasses hub-and-spoke) and then immediately explained why it's not a contradiction — but still listed it.
- **Treated intentional design decisions as gaps.** Conditional rule loading (#10), learner auto-trigger (#4), and skip-all-clear (#12) are all documented in the README Design Decisions table. Flagging conscious tradeoffs as findings is noise.
- **Did not weight findings by impact.** "Extract shared protocols" (saves ~40 lines per agent, prevents drift) and "add KB to README" (one line, trivial) received equal billing under the same impact tier.
- **Structural weaknesses were all theoretical.** Team lead SPOF, circular escalation, critic deadlock, unattended fragility — none have manifested. All have existing mitigations (communication log, area caps, user intervention, serial pipeline).

### Verdict (Revised)

**Adjusted score: ~8.5-8.7/10.** The setup is more mature than the raw evaluation suggests. The evaluators provided excellent reading coverage but lacked the operational context to separate real pain from theoretical risk. The user's 5-minute triage was sharper than the agents' full analysis — which itself is evidence that the setup's design decisions are well-considered and internally consistent.

**Actionable items:** Fix solo text (#1 contradiction), extract shared protocols (#1 proposal), add cross-ref lint (#7), add KB to README (#13). Four items, all low effort.
