# Claude Code Setup Audit

**Date:** 2026-05-26
**Auditors:** Critic (consistency, gaps, contradictions) + Architect (structure, scalability, design)
**Scope:** Full `.claude/` setup, `CLAUDE.md`, `docs/conventions/`, pipeline protocol, skill inventory

### Fixes Applied (2026-05-26)

- **M1 (RESOLVED):** Migrated all legacy artifacts (`docs/features/`, `docs/plans/`, `docs/issues/`) to `docs/specs/` convention. Updated all backlog links. Removed empty legacy directories.
- **M2 (RESOLVED):** `coding-conventions.md` already aggregates `@ef-core.md`. Added `@testing.md` to the aggregator. Removed redundant individual `@` refs from `developer.md` and `reviewer.md`.
- **M3 (RESOLVED):** Solo already references `@coding-conventions.md` which now includes all conventions (csharp, vue, ef-core, testing) via the aggregator.
- **M4 (RESOLVED):** Critic updated to follow `docs/product/index.md` instead of hardcoded `v1.md`.

---

## Overall Assessment

**Verdict: ACCEPT-WITH-RESERVATIONS**

This is an exceptionally well-engineered agentic setup. Agent boundaries are crisp, handoff protocols are detailed, failure modes are documented, and the layered information architecture (rules auto-loaded, conventions via `@`, skills on-demand) is sound. The system has shipped 35+ features through this pipeline — proof that it works.

The findings below are genuine issues but mostly fall in "drift and inconsistency" rather than "structurally broken." The three-tier loading model, two-phase analyze/execute pattern, and hub-and-spoke messaging are strong design choices that should be preserved.

---

## Findings by Severity

### MAJOR

#### M1. Legacy artifact paths — agents will fail to find v1 specs and plans

| Attribute | Detail |
|-----------|--------|
| **Where** | `docs/backlog.md`, `agents-workflow.md:7-24` |
| **Issue** | The backlog links v1 features to `docs/features/{Name}/spec.md` and `docs/plans/{Name}/plan.md`. The standard convention (used by all agents) is `docs/specs/{slug}/definition/spec.md` and `docs/specs/{slug}/delivery/plan.md`. Bug/gap specs live at `docs/issues/` — a third path. |
| **Impact** | An agent checking `docs/specs/F8-SprintSummaryCard/definition/spec.md` won't find it — the actual file is at `docs/features/SprintSummaryCard/spec.md`. The team-lead's status check will report "no spec" for every v1 feature. |
| **Mitigant** | All v1 features are Done, so the pipeline won't run on them again. Blast radius is limited to bugs/gaps at legacy paths. |
| **Fix** | Add a "Legacy Path Resolution" section to `agents-workflow.md` explaining that v1 features use `docs/features/` and `docs/plans/` while v2+ use `docs/specs/`. Or migrate all legacy artifacts. |

#### M2. Reviewer missing `@docs/conventions/ef-core.md`

| Attribute | Detail |
|-----------|--------|
| **Where** | `.claude/agents/reviewer.md` |
| **Issue** | The reviewer checks for "Data loading depth" (line 59) but does NOT load `@docs/conventions/ef-core.md`, which documents the exact detection rules (`Missing ThenInclude causes silent null/zero`). The developer loads it. |
| **Impact** | The reviewer is told to check for a bug class whose rules are in a file it never loads. Checks will be inconsistent. |
| **Fix** | Add `@docs/conventions/ef-core.md` to reviewer.md's `@` references. |

#### M3. Solo agent missing `@docs/conventions/testing.md` and `@docs/conventions/ef-core.md`

| Attribute | Detail |
|-----------|--------|
| **Where** | `.claude/agents/solo.md` |
| **Issue** | Solo loads architecture, project-rules, and coding-conventions — but not testing or ef-core. The developer loads all four. |
| **Impact** | Solo implementing backend changes touching EF Core may produce wrong migration defaults, skip `ThenInclude` depth checks, or miss testing patterns. |
| **Fix** | Add `@docs/conventions/testing.md` and `@docs/conventions/ef-core.md` to solo.md. |

#### M4. Critic hardcoded to v1.md for product spec cross-reference

| Attribute | Detail |
|-----------|--------|
| **Where** | `.claude/agents/critic.md` (line ~28) |
| **Issue** | The critic's Mode 1 review references "source spec (docs/product/v1.md)." The product now spans v1.md and v2.md (features F25-F31 are v2). For v2 features, the critic cross-references against the wrong spec. |
| **Impact** | Critic review of v2 features may miss spec violations or flag false positives based on stale v1 content. |
| **Fix** | Change to reference `docs/product/index.md` and follow the link to the relevant version, or accept both v1.md and v2.md as valid source specs. |

---

### MODERATE

#### D1. `agents-workflow.md` is the heavyweight outlier in the rules layer

| Attribute | Detail |
|-----------|--------|
| **Where** | `.claude/rules/agents-workflow.md` (155 lines — nearly half the total rules budget of ~313 lines) |
| **Issue** | Contains the full "Pipeline Modes" section (entry points, team configurations, key principles) which overlaps with `pipeline-protocol.md:16-53` and `team-lead.md:86-158`. The slug convention alone appears in three places: `agents-workflow.md:7-24`, `po.md:63-71`, and `create-feature-spec/SKILL.md:69-76`. |
| **Impact** | Drift risk between copies. Every session (including solo, ad-hoc) pays context cost for content only the team-lead needs. |
| **Fix** | Move Pipeline Modes to `pipeline-protocol.md` (already `@`-loaded by pipeline agents). Canonicalize slug conventions in one location. Reduce `agents-workflow.md` to ~80 lines: slug convention, agent roster, cycle caps, artifact format index. |

#### D2. `kb-maintenance.md` is borderline for the rules layer

| Attribute | Detail |
|-----------|--------|
| **Where** | `.claude/rules/kb-maintenance.md` (70 lines — second largest rule file) |
| **Issue** | Primary consumers are architect, developer, and reviewer (pipeline agents). The lint checks table is only relevant during review. Every solo session and ad-hoc question pays 70 lines of context for pipeline-only content. |
| **Impact** | Unnecessary context cost for non-pipeline sessions. |
| **Fix** | Move to `.claude/conventions/` and add `@` directives to developer.md and reviewer.md. Solo sessions that never touch the KB stop paying the tax. |

#### D3. Auto-loaded context budget is heavy and will not scale

| Attribute | Detail |
|-----------|--------|
| **Where** | All rule files + CLAUDE.md |
| **Issue** | Every session loads: CLAUDE.md (102 lines) + all rules (~313 lines) = ~415 lines minimum. Each pipeline agent then adds its own file plus `@` directives. Heaviest agents start with ~1100-1170 lines of instruction context before any feature-specific files. |
| **Impact** | Works now but leaves limited headroom. Adding more rules/conventions will eventually degrade agent performance. |
| **Fix** | Add a budget comment to `agents-workflow.md` tracking total auto-loaded lines. Set a ceiling (e.g., 500 lines). Before adding a new rule, check the budget. Implement D1 and D2 to reclaim ~150 lines immediately. |

#### D4. Frontend skills are thin relative to the stack surface

| Attribute | Detail |
|-----------|--------|
| **Where** | `.claude/skills/` |
| **Issue** | Backend has 13 scaffolding skills covering every creation pattern. Frontend has 1 scaffolding skill (`create-vue-feature`), 3 pattern-reference skills, and 1 review skill. Missing: `create-pinia-store`, `create-vue-composable`, `create-vue-route`, `create-vue-component` (following the level hierarchy). |
| **Impact** | Frontend plan steps get `Skill: None` disposition, meaning the developer gets less structural guidance for Vue work than C# work. |
| **Fix** | Add frontend scaffolding skills incrementally as needed. Priority: `create-pinia-store` and `create-vue-composable`. |

#### D5. `coding-conventions.md` is suspiciously thin (4 lines)

| Attribute | Detail |
|-----------|--------|
| **Where** | `docs/conventions/coding-conventions.md` |
| **Issue** | Loaded by developer, reviewer, and solo via `@`. At 4 lines, it either contains almost nothing or is a redirect. Coding conventions may be spread across `project-rules.md`, `csharp.md`, `ef-core.md`, and `vue.md` without a clear entry point. |
| **Impact** | Agents load a near-empty file, wasting an `@` directive slot. If conventions exist elsewhere, the entry point is misleading. |
| **Fix** | Either expand with actual conventions or make it an explicit index/redirect pointing to the specific convention files. |

#### D6. Repo structure duplicated in 3 files with subtle differences

| Attribute | Detail |
|-----------|--------|
| **Where** | `CLAUDE.md:18-31`, `project-rules.md:32-44`, `docs/architecture/v1.md` |
| **Issue** | CLAUDE.md has BuildingBlocks annotations (`app-agnostic`, `app-specific`) that project-rules.md lacks. The README acknowledges this as intentional ("Each audience needs it inline") but drift has already occurred. |
| **Impact** | Agents get inconsistent structure information depending on which file they read. |
| **Fix** | Accept the tradeoff but add sync reminders (comments in each file pointing to the others). Or consolidate into a single include. |

---

### MINOR

#### N1. README skill count is stale

| Attribute | Detail |
|-----------|--------|
| **Where** | `.claude/README.md` line 34 |
| **Issue** | Says "36 skills" but actual count is 37. |
| **Fix** | Update count or remove it (it will drift again). |

#### N2. CLAUDE.md graphify wiki reference is dead code

| Attribute | Detail |
|-----------|--------|
| **Where** | `CLAUDE.md` line 9 |
| **Issue** | `If graphify-out/wiki/index.md exists, navigate it instead of reading raw files` — the file doesn't exist. The conditional makes it harmless but agents may waste a file-existence check. |
| **Fix** | Remove the conditional line or generate the wiki. |

#### N3. Lessons mandatory vs conditional mismatch

| Attribute | Detail |
|-----------|--------|
| **Where** | `lessons-format.md` line 50 vs agent files |
| **Issue** | Format file says "Every agent must write lessons before finishing." Agent files say "Update if anything was learned" — conditional, not mandatory. |
| **Fix** | Align the format file with agent behavior (conditional is reasonable). |

#### N4. PO agent Grep restriction is misleadingly worded

| Attribute | Detail |
|-----------|--------|
| **Where** | `.claude/agents/po.md` line 183 |
| **Issue** | Says "Grep (file names only, no source code content)" but Grep inherently returns content snippets. An LLM might refuse to use Grep for pattern matching, or might use it and have forbidden content in context. |
| **Fix** | Reword to: "via Glob (file names/paths) and Grep (file-level existence checks). Do not Read source file contents." |

#### N5. Artifact Formats table gives raw template path instead of skill reference

| Attribute | Detail |
|-----------|--------|
| **Where** | `agents-workflow.md` Artifact Formats table |
| **Issue** | Lists `Plan | .claude/skills/create-implementation-plan/references/plan-template.md`. An agent reading this might read the template directly instead of invoking the skill. |
| **Fix** | Change to `Plan | .claude/skills/create-implementation-plan/ (invoke via Skill tool)`. |

#### N6. Commit message format not in project-rules.md

| Attribute | Detail |
|-----------|--------|
| **Where** | `team-lead-operations.md` line 129 |
| **Issue** | `feat({slug}): {description}` convention only defined in team-lead-operations. Solo agent has no commit guidance. |
| **Fix** | Add commit message format to `project-rules.md`. Reference from solo.md's completion checklist. |

#### N7. Solo agent missing skill failure fallback

| Attribute | Detail |
|-----------|--------|
| **Where** | `.claude/agents/solo.md` |
| **Issue** | Developer has explicit fallback for Skill tool failures (retry once, then read SKILL.md directly). Solo references skills but has no fallback protocol. |
| **Fix** | Add the same fallback protocol from developer.md to solo.md. |

#### N8. `service-registration` skill has placeholder description

| Attribute | Detail |
|-----------|--------|
| **Where** | Skills list |
| **Issue** | Description is just `service-registration: service-registration` — appears to be a placeholder. |
| **Fix** | Write a proper one-line description. |

---

## What's Missing

### Missing Protocols

| Gap | Description | Priority |
|-----|-------------|----------|
| **Merge conflict handling** | Pipeline assumes clean main throughout. No procedure for developer hitting a conflict mid-implementation. Team-lead's resume flow checks branch match but not merge state. | Medium |
| **Backlog archival** | Backlog is 320 lines and growing. Every completed feature (35+) remains with full descriptions. No guidance on when to archive, trim, or split. | Low |
| **Parallel feature coordination** | Pipeline is explicitly serial. No mechanism for interleaving features, WIP switching, or parallel team-lead instances. Acknowledged tradeoff. | Low (future) |
| **Concurrent developer sessions** | What happens if two sessions modify the same files? No locking or conflict detection. | Low (future) |

### Missing Convention File Content

| Gap | Description | Priority |
|-----|-------------|----------|
| **Consumer tables** on `summary-format.md` and `lessons-format.md` | Other format files have them. These two don't. The consumer table pattern is one of the best documentation practices in this system. | Low |
| **Learner `@` references** | Learner reads "current state of all target files" on-demand but doesn't pre-load any via `@`. Works but less efficient than other agents. | Low |

---

## Structural Analysis

### What's Working Well

| Aspect | Assessment |
|--------|-----------|
| **Three-tier loading model** (rules/conventions/skills) | Sound. The README's decision tree at `.claude/README.md:37-46` maps each type of instruction to its correct layer. |
| **Two-phase analyze/execute pattern** | Strongest design choice. Mandatory checkpoint between analysis and execution catches ambiguities before they compound. |
| **Hub-and-spoke messaging** | Well-implemented with good escape valves. PO escalation chain minimizes user interruptions. |
| **Anti-patterns sections** in agent files | Specific, actionable, and learned from real pipeline runs. The learner feedback loop working as designed. |
| **Skill authority model** | Clear three-tier: skill exists + infra exists = follow; skill exists + infra missing = build; no skill = architect inlines. Enforced by `create-implementation-plan`. |
| **KB navigation rule** | Correctly directs agents to read ~60 lines of KB instead of 300+ lines of source. Real context savings. |
| **"What You Know" sections** in agent files | Useful navigation aids that prevent unnecessary file reads. |
| **Checkpoint/resume system** | Handles timeouts, stalls, and cross-session resume via communication log. Production-grade. |

### Design Tradeoffs (Acknowledged)

| Dimension | Current Choice | Pro | Con |
|-----------|---------------|-----|-----|
| Auto-load all rules | ~313 lines always in context | No agent ever misses a guardrail | Solo sessions pay ~200 lines of pipeline-only context |
| Hub-and-spoke messaging | Single team-lead routes all | Auditability, user-decision interception | Single point of failure, no parallel features |
| Serial pipeline | One feature at a time | No coordination overhead | Throughput capped at 1 feature pipeline |
| Sonnet for reviewer | Cheaper, faster reviews | Good enough for pattern-matching work | May miss subtle logic bugs (self-audit compensates) |
| Two-phase analyze/execute | Mandatory checkpoint | Catches ambiguities early | Doubles agent invocations (latency + cost) |
| Anti-patterns in agent files | Per-agent learned behaviors | Agents avoid past mistakes | Increases agent file size over time |

### Scalability Concerns

| Trigger | What Breaks | When |
|---------|-------------|------|
| Adding 5+ services | Nothing in conventions. Port table and project-reference graph scale by adding rows. Skills are variant-aware. | Not a concern |
| 50+ specs in backlog | Critic's Phase 4.5 breadth scan becomes expensive (reads sibling specs for cross-feature inconsistencies). Backlog file becomes unwieldy. | Medium-term |
| 3+ parallel developers | Hub-and-spoke serializes all communication. No WIP switching or feature locking. | Future, if team grows |
| 10+ more rule files | Auto-loaded context exceeds useful budget. Agent performance degrades. | Soon, without budget tracking |

---

## Prioritized Action Plan

### Tier 1 — Fix Now (structural problems affecting pipeline correctness)

1. **Add `@docs/conventions/ef-core.md` to reviewer.md** [M2] — 1-line change
2. **Add `@docs/conventions/testing.md` and `@docs/conventions/ef-core.md` to solo.md** [M3] — 2-line change
3. **Update critic to use `docs/product/index.md` instead of hardcoded v1.md** [M4] — small edit
4. **Add legacy path resolution to `agents-workflow.md`** [M1] — 5-10 lines

### Tier 2 — Fix Soon (drift prevention, consistency)

5. **Canonicalize slug conventions** [D1] — single source in `agents-workflow.md`, reference from `po.md` and `create-feature-spec/SKILL.md`
6. **Move Pipeline Modes from `agents-workflow.md` to `pipeline-protocol.md`** [D1] — reclaims ~40 lines from auto-loaded rules
7. **Add commit message format to `project-rules.md`** [N6]
8. **Fix PO Grep restriction wording** [N4]
9. **Add skill failure fallback to solo.md** [N7]
10. **Align lessons mandatory vs conditional** [N3]

### Tier 3 — Evolve Later (growth opportunities)

11. **Move `kb-maintenance.md` to conventions** [D2] — reclaims ~70 lines from auto-loaded rules
12. **Add context budget tracking** [D3] — comment + ceiling in `agents-workflow.md`
13. **Add frontend scaffolding skills** [D4] — `create-pinia-store`, `create-vue-composable` first
14. **Expand or redirect `coding-conventions.md`** [D5]
15. **Add consumer tables to remaining format files** [missing]
16. **Add critic breadth scan scoping** for 50+ spec growth
17. **Add merge conflict handling protocol** [missing]
18. **Add backlog archival convention** [missing]

---

## Appendix: Agent Context Budget

| Agent | Own file | @ directives | Est. total (with rules + CLAUDE.md) |
|-------|----------|-------------|--------------------------------------|
| Developer | 179 | architecture (248) + project-rules (80) + coding-conventions (4) + testing (93) + ef-core (29) + pipeline-protocol (124) | ~1,172 |
| Team-lead | 290 | backlog (320) + pipeline-protocol (124) | ~1,149 |
| Reviewer | 180 | architecture (248) + project-rules (80) + coding-conventions (4) + testing (93) + pipeline-protocol (124) | ~1,144 |
| Architect | 252 | architecture (248) + project-rules (80) + pipeline-protocol (124) | ~1,119 |
| Solo | 151 | architecture (248) + project-rules (80) + coding-conventions (4) | ~898 |
| PO | 248 | product/index + pipeline-protocol (124) | ~889 |

*Base: CLAUDE.md (102) + all rules (~313) = ~415 lines loaded in every session.*

---

## Ambiguity Risks

| Risk | Where | Issue |
|------|-------|-------|
| CSS file boundary | `project-rules.md` line 13 | Lists `client/src/**/*.css` as source files restricted agents cannot modify. Tailwind config files may be wrongly classified. |
| "Update before /compact" | developer.md, reviewer.md, solo.md | Implies agents should detect `/compact` or `/clear` before they happen — impossible. Spirit is "write lessons at natural stopping points." |
| Critic reading code vs reviewing code | `critic.md` | Phase 2.5 reads source for feasibility; "What Critic Never Does" says don't review code. Distinction is clear to humans but may confuse an LLM. |

---

## Phase 2: Decision Gate & Logic Trace (2026-05-26)

Phase 1 found surface-level consistency issues. Phase 2 traces every decision point across the agent pipeline to find **missing interview gates** — places where agents decide autonomously when they should ask the user.

### The Dead Rule: `research-before-asking.md`

The single biggest systemic issue. This rule lives in `.claude/rules/` (auto-loaded for ALL agents) and says: when an agent hits ambiguity, present questions AND offer to research first. **Only the PO agent references it.** Every other agent that asks questions ignores it.

| Agent | Asks user questions? | References research offering? |
|-------|---------------------|-------------------------------|
| PO | Yes (discussion flow) | **Yes** — po.md lines 78-82 |
| Architect | Yes (Phase 1 questions, standalone mode) | **No** |
| Solo | Yes (approach confirmation, trade-offs) | **No** |
| Team-lead | Yes (launch questions) | **No** |
| Developer | Asks architect, not user directly | N/A |

**Impact:** The user created this rule intentionally but it's effectively dead for 4 of 5 question-asking agents. This is the root cause of "agents don't offer research before asking."

### Missing Decision Gates

#### G1. Architect classifies intent without confirmation [MAJOR]

**File:** `architect.md` lines 29-37
**Problem:** The architect classifies work as Trivial/Scoped/Complex/Refactoring unilaterally. "Trivial" skips planning entirely — the architect tells the developer to proceed with no plan. This is a scope decision disguised as a classification.
**Fix:** In standalone mode, state classification and rationale before proceeding. In pipeline mode, include classification in Phase 1 output. If classifying as Trivial (skip planning), confirm with team lead — skipping the plan is a scope decision.

#### G2. Architect auto-approve bypasses team-lead routing [MAJOR]

**File:** `architect.md` line 148, `team-lead.md` line 216
**Problem:** The architect messages "For developer: Plan approved. Begin implementation." — pre-deciding the next action. The team-lead's own rule says "Never auto-proceed past a checkpoint" (line 285), but auto-approve IS auto-proceeding. These two instructions contradict.
**Fix:** Change architect to message "For team lead: Plan approved for {FeatureName}. Ready for developer." — don't instruct the developer directly. Let the team-lead manage spawning.

#### G3. Architect escalation updates plan without routing through team-lead [MAJOR]

**File:** `architect.md` lines 212-219
**Problem:** When reviewer escalates, the architect decides "plan wrong or code wrong" and updates the plan directly. The team-lead rule says "Scope change or plan step removal → STOP. Confirm with user first." But the architect's escalation flow bypasses the team-lead entirely.
**Fix:** If the answer requires a plan change, message team-lead: "Plan update needed. Current: X. Proposed: Y." Let team-lead triage before updating.

#### G4. Architect answers developer questions and updates plan silently [MAJOR]

**File:** `architect.md` lines 200-209
**Problem:** When answering developer questions, step 3 says "Update the plan if any answer changes it" — then messages the developer to continue. The plan change happens without routing through the team-lead. Plan changes are scope changes, but the team-lead never sees them.
**Fix:** If the answer would change the plan, route through team-lead first. If it doesn't change the plan, answer directly.

#### G5. Team-lead skips team mode choice when Codex unavailable [MODERATE]

**File:** `team-lead.md` line 106
**Problem:** When Codex is not available: "Default to Standard. Inform: 'Standard mode, background spawn.'" — this is an inform, not a question. The user might want Fast mode. The Codex-unavailable path eliminates the user's team-mode choice.
**Fix:** Always present the team-mode choice regardless of Codex availability.

#### G6. Commit auto-commits onto potentially dirty working tree [MODERATE]

**File:** `team-lead-operations.md` line 136
**Problem:** "Auto-commit at each checkpoint." Combined with pre-flight check saying "If uncommitted changes exist, inform and continue" — the team-lead will auto-commit ON TOP of unrelated uncommitted changes.
**Fix:** If uncommitted changes exist AND they are not part of this feature, warn and ask before proceeding.

#### G7. Spec status set to "Ready" without user confirmation [MAJOR]

**File:** `po.md` lines 162-166
**Problem:** When critic accepts: PO sets Status to Ready and updates backlog BEFORE reporting to user (step 6). The user never confirms the spec is ready. "Ready" triggers the architect pipeline — once set, the pipeline can start without further user input.
**Fix:** Present critic verdict first, then ask: "Spec looks clean — mark as Ready?" Set status only after user confirms.

#### G8. Architect Phase 1 spec gate fails for ad-hoc work [MODERATE]

**File:** `architect.md` line 129, `team-lead.md` line 136
**Problem:** The team-lead says "No spec gate for ad-hoc work." But the architect always runs the spec gate in Phase 1. When spawned for ad-hoc work, the architect hits "No spec found" and has no instruction for what to do.
**Fix:** Add: "For ad-hoc work (slugs starting with `adhoc-`), skip the spec gate. Use the team-lead's problem description as the requirements source."

### PO Flow Gaps

#### P1. Jira ticket flow skips research offering [MAJOR]

**File:** `po.md` lines 41-48
**Problem:** Step 5 says "enter the Discuss → Challenge flow to fill them" — but reads as self-contained. The PO will present gaps and start asking questions without the research offering because the Jira flow doesn't explicitly reference "How You Discuss Step 1."
**Fix:** Change step 5 to: "Present the gaps to the user and enter the full discussion flow (see 'How You Discuss' below — start at Step 1: internal research, then research offering, then questions)."

#### P2. Jira epic flow writes epic.md before any discussion [MAJOR]

**File:** `po.md` lines 52-61
**Problem:** The epic flow goes Fetch → Write epic.md → then discuss stories. It bypasses the entire Listen → Research → Discuss → Challenge sequence. The PO writes the epic spec from Confluence content without discussing gaps with the user first.
**Fix:** Add a gap analysis + research offering step between fetching Confluence content and writing epic.md.

#### P3. `create-feature-spec` SKILL.md bypasses research offering [MAJOR]

**File:** `create-feature-spec/SKILL.md` lines 48-49
**Problem:** The skill's Step 3 ("Ask clarifying questions in one batch") has zero mentions of research offering. When the PO enters this skill with unresolved ambiguities, questions get asked without the offering.
**Fix:** Add: "If these are first-contact questions, end with the research offering per research-before-asking.md."

#### P4. Cross-check path has no defined steps [MAJOR]

**File:** `po.md` lines 143-148
**Problem:** The critic path has a full "Managing the Critic" section with 6 steps. The cross-check path is described in one sentence. What does the PO do after cross-checking? Set Ready? Report findings? Ask user? None defined.
**Fix:** Add a "Managing the Cross-check" section with: verify fields, if gaps → fix + re-check, if clean → present to user and ask to mark Ready.

#### P5. Critic findings fixed without user visibility [MODERATE]

**File:** `po.md` lines 162-163
**Problem:** When critic rejects, the PO fixes gaps and re-runs — user never sees the critic's findings. Product decisions in critic findings get resolved by the PO without user input.
**Fix:** If any finding is CRITICAL or involves a product scope decision, present it to the user before fixing. PO owns MEDIUM/LOW fixes autonomously.

### Handoff Mismatches

#### H1. Plan splitting has no team-lead handler [MODERATE]

**File:** `architect.md` lines 143-144
**Problem:** Architect messages team-lead with a plan split proposal. Team-lead has no handler for this message type — never mentions plan splitting.
**Fix:** Add to team-lead: "If architect proposes a plan split, inform user of sub-plan breakdown and proceed. Plan splits are structural, not scope changes."

#### H2. PO escalation path has no matching team-lead handler [MODERATE]

**File:** `po.md` line 217
**Problem:** PO escalates with "For user: N questions need your input — PO couldn't answer from spec." Team-lead has a handler for "route to PO first" but no handler for "PO couldn't answer, escalate to user."
**Fix:** Add to team-lead dispatching rules: "PO escalation → relay PO's full Q&A to user verbatim. Collect answers. Route back to architect."

#### H3. Team-lead and architect can produce conflicting classifications [GAP]

**Problem:** Team-lead classifies ad-hoc work (Fast/Standard). Architect classifies intent (Trivial/Scoped/Complex). These are independent — team-lead says Standard, architect says Trivial and skips planning. No reconciliation mechanism exists.

### Properly Gated (Working Correctly)

These decision points are correctly implemented with user confirmation:

| Gate | File | Status |
|------|------|--------|
| Team-lead asks user for review mode (attended) | team-lead.md:187 | OK |
| Team-lead asks user for team mode (Codex available) | team-lead.md:99-105 | OK |
| Architect idempotency check on existing plan | architect.md:128 | OK |
| Solo confirms approach before implementing | solo.md:48-54 | OK |
| PO mandatory gate before writing spec | po.md:141-149 | OK |
| Team-lead scope change triage | team-lead.md:34 | OK |
| Architect User Decision Guardrail | architect.md:56-63 | OK |
| PO slug confirmation | po.md:65-72 | OK |
| Solo scope guard | solo.md:82-87 | OK |
| Team-lead lessons processing | team-lead.md:235 | OK |

### Phase 2 Prioritized Fixes

**Tier 1 — Fix now (logic bugs that will cause problems):**

1. G2 + G3 + G4: Architect plan changes must route through team-lead
2. G7: Spec "Ready" needs user confirmation
3. P1 + P2 + P3: Jira flows and create-feature-spec need research offering integration
4. P4: Cross-check path needs defined steps
5. G1: Architect Trivial classification needs confirmation gate

**Tier 2 — Fix soon (rough edges):**

6. G5: Team-mode choice regardless of Codex availability
7. G8: Architect ad-hoc spec gate bypass
8. H1 + H2: Team-lead handlers for plan splits and PO escalation
9. P5: Critical critic findings shown to user
10. G6: Commit protocol warns about unrelated uncommitted changes

**Tier 3 — Decide (systemic question):**

11. B1: `research-before-asking.md` — either narrow to PO-only (move out of rules/) or enforce across all agents. Current state (universal rule, single implementor) is the worst option.

---

*Phase 1: Critic + Architect audit agents (surface consistency)*
*Phase 2: Handoff + PO flow trace agents (decision gate logic), 2026-05-26*
