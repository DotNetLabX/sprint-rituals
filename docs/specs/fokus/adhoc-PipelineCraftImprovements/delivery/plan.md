# Pipeline Craft Improvements

**Feature Spec:** None (ad-hoc infrastructure — based on `docs/evaluations/cross-project-skill-mining.md`)

## Context

Cross-project skill mining of 3 well-crafted skills of the employer identified 15 convergent improvement patterns and 8 medium+ value additional techniques for Fokus's pipeline infrastructure. These are all documentation/configuration changes — no source code is modified. The goal is to harden the agent pipeline, improve cross-phase intelligence, add protective guardrails, and standardize artifact formats.

## Scope

**In scope:** All 15 convergent patterns from the mining report, plus 8 medium+ value extras. Changes target agent files, rule files, convention files, and skill metadata.

**Out of scope:**
- Source code changes
- New skills (only metadata improvements to existing skills)
- Domain-specific content (KB entries, glossary updates)
- Learner agent file modifications (improve-flow skill is updated for anti-pattern promotion, but the learner agent file itself is unchanged)

## Implementation Steps

### Step 1: Extract convention files from agents-workflow.md

**Pattern:** #13 (Convention Extraction) — prerequisite for all other agents-workflow.md changes.

Extract 3 inline format definitions from `agents-workflow.md` into standalone convention files. Replace inline blocks with one-line references. This shrinks agents-workflow.md by ~80 lines and makes formats independently referenceable by agents and skills.

**Files to create:**
- `docs/conventions/implementation-format.md` — extracted from "Implementation File Format" section (lines 218-236)
- `docs/conventions/review-format.md` — extracted from "Review Output Format" section (lines 357-392)
- `docs/conventions/questions-format.md` — extracted from "Questions File Format" section (lines 262-291)

**Files to modify:**
- `.claude/rules/agents-workflow.md` — replace each extracted block with: `See docs/conventions/{name}.md for the full format.`

**Pattern to follow:** The plan template is already extracted this way — `create-implementation-plan/references/plan-template.md` is referenced from agents-workflow.md line 175. Follow the same pattern.

**Accept:** agents-workflow.md drops by ~80 lines from current (~402 → ~320). Each convention file is self-contained with its format block, rules, and a `## Consumers` header listing which agents read/write it.

**Skill:** None

---

### Step 2: Enhance extracted convention files with cross-phase intelligence and anti-patterns

**Patterns:** #2 (Carry-Over Findings), #1 (Anti-patterns on templates), #10 (Downstream Consumers), extra (KB change report)

Add new sections to the convention files created in Step 1. These additions wouldn't fit well inline in agents-workflow.md but work naturally in standalone files.

**Files to modify:**
- `docs/conventions/implementation-format.md`:
  - Add `## Carry-Over Findings` section to the format template — structured observations the developer passes to the reviewer. Format: `**{title}** | Severity: {low/medium/high} | For: {reviewer/architect}` + one-line evidence + note.
  - Add `## KB Changes` table to the format template — `| Entry | Action (NEW/UPDATE) | What changed |`. Gives reviewer visibility into KB modifications.
  - Add `## Anti-patterns` — 3-4 items: omitting deviation reasons, writing "updated file" without explaining what changed, listing files without linking to plan steps.
  - Add `## Consumers` — who reads implementation.md and what they check.

- `docs/conventions/review-format.md`:
  - Add `## Anti-patterns` — 3-4 items: findings without file:line evidence, severity inflation (style as HIGH), approving without fresh build output.
  - Add `## Consumers` — who reads review.md and what they do with it.

- `docs/conventions/questions-format.md`:
  - Add `## Anti-patterns` — 2-3 items: asking without context, bundling unrelated questions, setting wrong `To:` field.
  - Add `## Consumers` — routing chain.

**Accept:** Each convention file has its format block + Anti-patterns + Consumers sections.

**Skill:** None

---

### Step 3: Add governance sections to agents-workflow.md

**Patterns:** #6 (Message Size Contract), #7 (Checkpoint Reports)

Add two new protocol sections to agents-workflow.md. These govern agent behavior across all phases — they belong in the coordination protocol, not in individual agent files.

**Files to modify:**
- `.claude/rules/agents-workflow.md`:
  - Add `## Message Size Contract` after the "All Agents" section:
    - Analysis outputs (Phase 1): ~500 words max
    - Handoff messages: ~300 words max
    - Content goes in files; messages are notifications with summaries
    - "Write first, message second" — extend existing questions rule to all artifacts
  - Add `## Checkpoint Report Format` after the Pipeline section:
    ```
    {Phase Name} — {Slug}
    ================================================
    {2-4 headline metrics}
    {Table or list of findings}
    Needs your attention:
      1. {flagged item}
    Action options:
      1. {default} (recommended)
      2. {alternative}
      3. Stop
    ```
    - Apply to: architect Phase 1 output, developer Phase 1 output, done check, reviewer verdict

**Files to modify (message size extension):**
- `.claude/rules/agent-model-routing.md` — add structured report constraint for Explore and general-purpose agents: "Return a structured report under 300 words. Write detailed findings to disk; the message is a summary."

**Accept:** Two new sections in agents-workflow.md. Message size constraint also in agent-model-routing.md. agents-workflow.md stays under 350 lines post-extraction + additions.

**Skill:** None

---

### Step 4: KB governance overhaul

**Patterns:** #5 (KB Lint Checks), #8 (KB Hard Rules), #10 (Downstream Consumers), extra (KB entry schema)

Consolidate all KB governance improvements into `kb-maintenance.md` and create a standardized entry schema.

**Files to modify:**
- `.claude/rules/kb-maintenance.md`:
  - Add `## Hard Rules` section (after "What NOT to Capture"):
    1. Never delete a KB entry. Mark with `## Status: Deprecated` and note why.
    2. Never remove sections without replacing them. Update, don't truncate.
    3. Always update `docs/kb/index.md` when adding or renaming an entry.
    4. Use Edit tool for KB updates, not Write. Carry forward untouched sections.
  - Add `## Lint Checks` table:
    | Check | Severity | Condition |
    | broken-index-link | error | index.md links to a file that doesn't exist |
    | orphan-entry | warning | KB file not linked from index.md |
    | stale-key-file | warning | "Key file:" path doesn't exist in codebase |
    | duplicate-term | warning | Same concept in glossary and domain entry with conflicting definitions |
    | empty-section | info | Section header with no content |
  - Add `## Consumers` table:
    | Agent | What they read | Impact of stale data |
    | Architect | Business rules, formulas | Plan steps miss edge cases |
    | Developer | Key file paths | Wrong files modified |
    | Reviewer | Business rules | Bugs missed in review |
  - Update reviewer instruction: replace "Flag stale or missing entries as HIGH severity" with reference to the lint checks table.

**Files to create:**
- `docs/conventions/kb-entry-schema.md` — standardized section names and order for KB entries: `## Rules`, `## Key Files`, `## Edge Cases`, `## Relationships`. Optional: `## Status`, `## Source`.

**Accept:** kb-maintenance.md has Hard Rules, Lint Checks, and Consumers sections. Schema file exists and is referenced from kb-maintenance.md.

**Skill:** None

---

### Step 5: Developer agent hardening

**Patterns:** #1 (Anti-patterns), #3 (Self-check gate), #11 (Idempotency), extra (Incremental implementation.md)

Harden the developer agent with protective guardrails seeded from real pipeline lessons.

**Pre-work:** Read 4-5 existing `docs/specs/*/delivery/lessons.md` files (most recent features) to extract recurring developer mistakes for anti-pattern seeding.

**Files to modify:**
- `.claude/agents/developer.md`:
  - Add `## Anti-patterns` section (after "What You Never Do"). Seed with 5-6 items from lessons analysis. Expected items (verify from actual lessons):
    - Skipping plan steps silently (reporting "done" without implementing a step)
    - Inventing patterns when a skill exists for the task
    - Writing implementation.md entries without explaining what changed ("updated file" with no detail)
    - Not running build after each step (batching verification to the end)
    - Ignoring existing codebase patterns in favor of "cleaner" approaches
    - Writing debug/TODO artifacts and forgetting to remove them
  - Convert `## Completion Checklist` from prose list into structured self-check gate table:
    | Check | Pass condition | Blocking? | On failure |
    | Build | `dotnet build` exits 0 | Yes | Fix before proceeding |
    | Plan coverage | Every plan step in implementation.md | Yes | Add missing entries |
    | Debug artifacts | No TODO/HACK/DEBUG in modified files | Yes | Remove artifacts |
    | Deviations | Every deviation documented with reason | Yes | Document or revert |
    | Carry-over | Observations for reviewer written | No | Write carry-over section |
    | Lessons | lessons.md updated | No | Write lessons |
    Gate rule: do not message "ready for Step 1" until all blocking checks pass.
  - Add idempotency check to Phase 2 opening: "If `implementation.md` already exists, read it to determine which plan steps are already completed. Resume from the first incomplete step."
  - Add incremental implementation.md instruction to Phase 2: "Update implementation.md after completing each step, not all at the end. This enables resume-from-timeout and gives the architect incremental visibility."

**Note:** Step 9 adds an additional pre-flight check to developer.md (known-deviations reference). developer.md is not finalized until Step 9.

**Accept:** Developer.md has Anti-patterns (seeded from real lessons), structured self-check table (replaces prose checklist), idempotency guard, and incremental writing instruction.

**Skill:** None

---

### Step 6: Reviewer agent hardening

**Patterns:** #1 (Anti-patterns), #2 (Carry-over consumption), #12 (Confidence scoring), extra (Reviewer stage gate)

**Pre-work:** Read 2-3 `docs/specs/*/delivery/lessons.md` files for recurring reviewer issues.

**Files to modify:**
- `.claude/agents/reviewer.md`:
  - Add `## Anti-patterns` section (after "What You Never Do"). Seed with 4-5 items from lessons:
    - Flagging style preferences as HIGH severity (style is LOW by definition)
    - Approving without fresh build output ("should work" is not evidence)
    - Checking only the latest commit instead of full diff against base
    - Rubber-stamping fix cycles (not re-checking areas adjacent to fixes)
    - Missing carry-over findings from implementation.md
  - Add carry-over consumption rule to Stage 2 checklist: "Each carry-over finding in implementation.md addressed — confirmed or refuted with evidence in review.md."
  - Add `Confidence:` qualifier to findings format: `HIGH` (hard evidence), `MEDIUM` (likely but could be intentional), `LOW` (may be wrong — moved to Open Questions by self-audit). Update Self-Audit section to reference confidence levels.
  - Add explicit gate between Stage 1 and Stage 2: "Stage 2 proceeds only after Stage 1 passes. If Stage 1 finds plan conformance issues, write review.md with REQUEST CHANGES immediately — do not proceed to code quality checks on non-conformant code."

**Accept:** Reviewer.md has Anti-patterns, carry-over rule in checklist, confidence qualifiers on findings, and explicit stage gate.

**Skill:** None

---

### Step 7: Architect agent improvements

**Patterns:** #11 (Idempotency), extra (Cite-or-flag conformance matrix), extra (Redirection tables)

**Files to modify:**
- `.claude/agents/architect.md`:
  - Add idempotency check to Phase 1: "Before analyzing, check if `plan.md` already exists. If it does, inform the team lead: 'Existing plan found for {slug}. Overwrite or resume?' Do not silently overwrite."
  - Enhance Step 1 Done Check with cite-or-flag conformance matrix. For each plan step, the architect must assign one disposition:
    | Disposition | Meaning |
    | Implemented | Matching implementation.md entry found |
    | Deviated | Different approach — reason documented |
    | Missing | No corresponding entry — fail |
    | Superseded | Plan step was updated mid-implementation |
    | N/A | Step doesn't produce code (e.g., migration command) — verify differently |
  - Convert all 5 items in "What You Never Do" to redirection format: instead of "Never X" alone, add "→ instead: Y". Examples:
    - "Write source code → instead: message developer with specific instructions"
    - "Propose patterns not in the codebase → instead: reference existing pattern or escalate to user"
    - "Skip 'where does this belong' → instead: classify per Intent Classification first"
    - "Extend instruction scope → instead: flag as separate feature for the user"
    - "Conduct Step 2 code review → instead: message reviewer via team lead"
  - Add optional `Confidence:` field to plan steps: `high` (clear pattern exists), `medium` (adaptation needed), `low` (no precedent — developer should explore extra). Document in Plan Writing Rules section.

**Accept:** Architect.md has idempotency guard, structured conformance matrix for done-check, actionable redirections on all 5 items, and plan step confidence field.

**Skill:** None

---

### Step 8: Team lead improvements

**Patterns:** #4 (Structured state), #11 (Idempotency pre-flight), extra (Pipeline completion dashboard)

**Files to modify:**
- `.claude/agents/team-lead.md`:
  - Extend communication-log.md header format with structured state fields (after existing Branch/Step/Cycle):
    ```
    **Team Mode:** {fast/standard/standard+codex}
    **Review Mode:** {self-review/critic}
    **Architect ID:** {agent-id or "not spawned"}
    **Developer ID:** {agent-id or "not spawned"}
    **Reviewer ID:** {agent-id or "not spawned"}
    **Plan Steps Completed:** [1, 2, 3]
    **Plan Steps Remaining:** [4, 5, 6]
    **Questions Resolved:** [Q1, Q2]
    ```
    Update these fields at each phase transition — not just messages.
  - Strengthen pre-flight checks with idempotency gate (Step 0, before dirty tree check):
    1. Check `summary.md` — if exists, pipeline completed. Ask: "Already completed. Re-do?"
    2. Check `communication-log.md` — if exists without summary.md, incomplete run. Trigger resume flow.
    This moves the resume logic from "when user asks" to "always at launch."
  - Add pipeline completion dashboard to Team Shutdown:
    ```
    Pipeline Complete — {Slug}
    ================================================
    Steps: {N} planned, {N} implemented, {N} deviated
    Review: {verdict} after {N} cycle(s)
    Files: {N} created, {N} modified
    Lessons: {N} items logged
    Duration: {start} → {end}
    ```
    Print this before writing summary.md.
  - Add checkpoint format enforcement to message dispatching: "If agent output at a checkpoint doesn't include action options, append them before relaying to the user."

**Accept:** Communication-log header has structured state fields (including Questions Resolved). Pre-flight has proactive idempotency. Shutdown prints a completion dashboard. Checkpoint enforcement documented.

**Skill:** None

---

### Step 9: Lessons format and known deviations catalog

**Patterns:** #14 (Known Deviations), extra (Structured improvement proposals)

**Pre-work:** Read all existing `docs/specs/*/delivery/lessons.md` files. Extract recurring patterns (items appearing 2+ times across features) for the known-deviations catalog. Extract any items that suggest systemic improvements for the proposal format.

**Files to create:**
- `docs/conventions/known-deviations.md` — running table of recurring developer mistakes:
  ```
  # Known Deviations
  
  Recurring implementation mistakes tracked across features. Developer reads during Phase 1 pre-flight. Learner populates from lessons.md analysis.
  
  | Pattern | Wrong | Correct | Frequency | First Seen |
  |---------|-------|---------|-----------|------------|
  ```
  Populate with initial entries from the lessons analysis.

**Files to modify:**
- `.claude/rules/agents-workflow.md` — enhance Lessons File Format with structured improvement proposal sub-format:
  ```
  ### Improvement Proposal (optional, for systemic issues)
  **Target:** {file path — agent file, skill, convention, or rule}
  **Change:** {what to add/modify}
  **Evidence:** {which features demonstrated this, with links}
  **Priority:** {low/medium/high}
  ```
  Add after the existing lessons format template.
- `.claude/agents/developer.md` — add to Phase 1 pre-flight: "Read `docs/conventions/known-deviations.md` if it exists. Be aware of recurring mistakes for this type of work."

**Accept:** known-deviations.md exists with initial entries. Lessons format has improvement proposal sub-format. Developer Phase 1 references the deviations file.

**Skill:** None

---

### Step 10: Skill infrastructure improvements

**Patterns:** #1 (Anti-patterns on skills), #9 (Phase I/O Contracts), #10 (Downstream Consumers on skills), #15 (Skill Changelogs)

Add structural metadata, protective guardrails, and consumer documentation to skills. Update improve-flow to recognize anti-patterns as a promotion target.

**Files to modify (add `## Required Reading` + `## Anti-patterns` blocks):**
- `.claude/skills/create-implementation-plan/SKILL.md`
- `.claude/skills/create-feature/SKILL.md`
- `.claude/skills/diagnose/SKILL.md`
- `.claude/skills/tdd/SKILL.md`

Required Reading format: list of file paths the agent must read before invoking the skill, with one-line reason.
Anti-patterns: 2-3 items per skill, seeded from lessons.md entries that mention skill-related mistakes. Example for create-implementation-plan: "Writing method-body plans instead of operation + acceptance criteria", "Omitting skill mapping for steps that have a matching skill."

**Files to modify (add `## Downstream Consumers`):**
- `.claude/skills/create-implementation-plan/SKILL.md` — consumers: developer (implements the plan), architect (reviews conformance), reviewer (verifies plan coverage)
- `.claude/skills/create-feature-spec/SKILL.md` — consumers: architect (plans from spec), critic (validates spec completeness)
- `.claude/skills/diagnose/SKILL.md` — consumers: developer (follows diagnosis), architect (reviews if escalated)

**Files to modify (add I/O contracts to sub-files, if sub-files exist):**
- Check `create-feature/workflows/*.md` — add `## Inputs` / `## Outputs` / `## Gate` headers to each workflow file.

**Files to create (CHANGELOG.md for top 5 most-evolved skills):**
- `.claude/skills/create-implementation-plan/CHANGELOG.md`
- `.claude/skills/create-feature/CHANGELOG.md`
- `.claude/skills/diagnose/CHANGELOG.md`
- `.claude/skills/create-aggregate/CHANGELOG.md`
- `.claude/skills/tdd/CHANGELOG.md`

Format:
```
# {Skill Name} — Changelog

## [Unreleased]
- Initial changelog creation

## [1.0.0] — {date of last known modification}
- Baseline version (reconstructed from git history)
```

**Files to modify (update improve-skills and improve-flow):**
- `.claude/skills/improve-skills/SKILL.md` — add instruction: "When modifying a skill, append a changelog entry to its CHANGELOG.md. Include: what changed, why, and evidence citation."
- `.claude/skills/improve-flow/SKILL.md` — add instruction: "When a lesson recurs 2+ times across features and maps to an agent file, check if that agent has an `## Anti-patterns` section. If yes, promote the recurring lesson as a new anti-pattern entry. Anti-patterns are a valid promotion target alongside rules, conventions, and agent instructions."

**Accept:** 4 skills have Required Reading + Anti-patterns blocks. 3 artifact-producing skills have Downstream Consumers. Sub-file skills have I/O contracts. 5 skills have CHANGELOG.md. improve-skills maintains changelogs. improve-flow promotes recurring lessons to anti-patterns.

**Skill:** None

## Testing Strategy

Since all changes are documentation/configuration files, testing is structural verification:

1. **File existence:** All new convention files and changelogs exist at specified paths.
2. **Reference integrity:** Every `See docs/conventions/{name}.md` reference in agents-workflow.md points to an existing file.
3. **agents-workflow.md size:** Verify line count dropped (target: under 350 lines post-extraction).
4. **No orphaned content:** Extracted content removed from agents-workflow.md, not duplicated.
5. **Anti-pattern seeding:** Anti-patterns in developer.md and reviewer.md cite real lessons, not generic placeholders.
6. **KB lint checks:** Run the 5 defined lint checks against current `docs/kb/` to verify they're actionable.

## Open Questions

None — scope confirmed by user (all 15 patterns + medium+ value extras).
