---
name: create-implementation-plan
description: Creates implementation plans with mandatory skill mapping per step. Ensures plan steps reference skills instead of restating patterns. Extends the agents-workflow.md plan format.
user-invocable: true
---

# Create Implementation Plan (Architect Reference)

This skill is for the **architect agent**. It produces `docs/specs/{slug}/delivery/plan.md` — the implementation plan that the developer executes. Each step either references a skill or explicitly justifies inline detail. No freeform plans.

## Purpose

Generate `docs/specs/{slug}/delivery/plan.md` with a structural guarantee that each step either references a skill or explicitly justifies inline detail. Extends the plan format in `agents-workflow.md` with mandatory skill mapping.

## When to use

After a feature spec exists (`docs/specs/{slug}/definition/spec.md` with `Status: Ready`). Replaces freeform plan writing.

## Reading protocol

Before writing, ensure you have:

1. **Feature spec** — read `docs/specs/{slug}/definition/spec.md` (or `epic.md` / `bug.md` depending on slug type)
2. **Architecture doc** — read for system shape and existing decisions
3. **Skill inventory** — scan `.claude/skills/` — read every `SKILL.md` frontmatter. If the architecture doc has a Skill Inventory section, use it; otherwise build one from scratch.
4. **Existing plans** — read `docs/specs/*/delivery/plan.md` for format consistency

## Steps

1. **Pre-fill from conversation context.** The architect has typically been discussing the feature before invoking this skill. Pull every answer you can from the existing conversation. Do not re-ask what the user already said.

2. **Run the reading protocol.** Read existing feature specs, architecture doc, skill inventory, and existing plans.

3. **Draft the implementation steps.** Each step is one focused task with full file paths.

4. **Build the Skill Mapping** — for each step, determine the disposition:
   - **Follow** — a skill covers this pattern. Provide only feature-specific inputs (entity names, file paths, property types). Do NOT restate how the pattern works.
   - **Build** — a skill covers this pattern but references infrastructure that doesn't exist locally (BuildingBlocks, base classes). The step must include building the missing infrastructure first, then following the skill.
   - **None** — no skill covers this step. Describe what to accomplish + acceptance criteria. Since the developer has no skill to invoke, also include key domain constraints (type names, case sensitivity, important values) and pattern references (point to existing code). Do NOT write method-body logic flows (sequential pseudo-code under a single method). The developer decides internal decomposition. Log to the Gaps column for future skill creation.

   **Skill verification before setting None:** For each step, list the actions it performs (throws exceptions, registers services, queries data, creates endpoints, configures persistence). Match each action against skill frontmatter descriptions. Only set None after confirming no frontmatter description matches. If a frontmatter is ambiguous, read the skill's When to Use section before deciding.

5. **Anti-pattern check.** Scan the draft for:
   - The word "adapted" or "adapt" near a skill reference — violation. Either Follow or Build, never Adapt.
   - A skill dismissed as "too simple" or "not needed for this case" — violation. Skills ensure consistency; complexity is not the criterion.
   - Implementation pattern details restated when a skill exists — over-specification. Delete and reference the skill.
   
   **Over-specification test for Follow steps:** Read the skill's SKILL.md. For each detail in the plan step, ask: "Does the skill already cover this?" If yes, delete it from the plan step. What remains should be ONLY feature-specific inputs the skill can't know:
   - Entity/type names and their properties ✓
   - Business rules and computation logic ✓
   - File paths ✓
   - Route paths, API shapes ✓
   - References to existing code for feature-specific patterns ✓
   
   Delete from Follow steps — the skill already covers these:
   - File placement rules ✗
   - DI registration instructions ✗
   - Constructor injection patterns ✗
   - Record/class structural syntax ✗
   - Boilerplate that the skill's template generates ✗
   
   **Content budget:** A Follow step should be ~5-15 lines of feature-specific inputs + a skill reference. If a Follow step exceeds 30 lines, it likely over-specifies. The developer invokes the skill via the Skill tool to get structural patterns — duplicating them in the plan causes the developer to skip skill invocation entirely.

6. **Write the plan** following `references/plan-template.md`. Output: `docs/specs/{slug}/delivery/plan.md`.

7. **Apply the auto-approve gate** from `agents-workflow.md` (always auto-approve; if >10 steps, consider splitting into sub-plans where each has >= 2 steps). Message team lead, never developer directly.

## Arguments

Pass the feature name: `create-implementation-plan Sprint`

## Required Reading

Before invoking this skill, ensure you have read:
- `docs/specs/{slug}/definition/spec.md` — feature requirements to plan against
- `docs/architecture/v1.md` — system shape and existing decisions (already loaded via @)
- `.claude/skills/` frontmatter of each skill — needed to build the Skill Mapping table
- `docs/specs/*/delivery/plan.md` (1-2 recent examples) — format consistency

## Anti-patterns

- **Writing method-body plans.** Describing sequential logic steps (1. do X, 2. do Y, 3. do Z) instead of operation + acceptance criteria. This over-specifies and leads the developer to skip skill invocation. Describe *what* to accomplish, not *how* to implement it internally.
- **Omitting skill mapping for steps that have a matching skill.** Setting disposition to None without running the skill verification test ("list all actions → match each against skill frontmatter"). A step that creates an endpoint, adds a domain event handler, or configures persistence almost always has a matching skill.
- **Restating pattern details alongside a Follow skill reference.** If the skill already covers file placement, DI wiring, or record structure — delete it from the plan step. Keep only feature-specific inputs the skill can't know. Over-specification causes the developer to skip the skill entirely.

## Downstream Consumers

| Agent | What they use | Impact if incomplete |
|-------|--------------|---------------------|
| Developer | All steps (implements against them) | Gaps in plan steps cause skipped implementation or wrong approach |
| Architect | Skill Mapping, plan structure (Step 1 done check) | Done check can't verify conformance without clear acceptance criteria |
| Reviewer | Step descriptions (Step 2 conformance check) | Reviewer can't verify plan was followed without clear per-step requirements |

## What this skill does NOT do

- Write code. Implementation is the developer's job.
- Make architectural decisions — those come from the architecture doc.
- Override the `agents-workflow.md` coordination protocol — it extends the plan format, doesn't replace the pipeline.
- Replace the architect's judgment about step ordering, scope, or complexity classification.
