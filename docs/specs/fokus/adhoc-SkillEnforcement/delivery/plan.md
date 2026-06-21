# Skill Enforcement — Architecture Doc & Implementation Plan Skills

**Feature Spec:** None

## Context

Phase 2 of the skill design proposal. Phase 1 (cleaning Reflekt-specific references from existing skills) is complete. This phase creates two new skills that structurally enforce skill awareness when writing architecture documents and implementation plans.

The root cause these skills address: the Scaffolding and JiraSync plans bypassed skills because (a) the architecture doc was written without checking the skill inventory, and (b) the plan format had no structural constraint requiring skill references. Prose rules in `architect.md` were insufficient — the architect "adapted" skills (meaning ignored them) with the architecture doc as justification.

## Scope

**In scope:**
- `create-architecture-doc` skill — generates/updates architecture docs with mandatory skill inventory
- `create-implementation-plan` skill — generates plans with mandatory skill mapping per step
- Updates to `architect.md` and `agents-workflow.md` to reference both skills

**Out of scope:**
- Rewriting the existing `docs/architecture/v1.md` to the new template (separate task)
- Rewriting existing plans (they're historical records)

## Design Decisions (from proposal discussion)

1. **Skill dispositions in plans:** Only two valid options per step:
   - **Follow** — apply the skill pattern. Adapt implementation specifics (DB engine, framework) but never skip the pattern itself.
   - **Build** — skill references infrastructure (BuildingBlocks, base classes) that doesn't exist locally. Build it first, then follow the skill.
   - **Never "Adapt"** — this was the weasel word that let the architect bypass skills in the Scaffolding plan. It is explicitly banned.

2. **Architecture doc modes:** Generate (first time) and Refresh (re-scan skills, flag stale sections).

3. **"Not specified here" field:** Each architectural decision explicitly names what it is NOT defining — the forcing function that prevents over-specification.

4. **Reviewer validation:** When the reviewer does Step 2 (code review), they also verify that any "No skill" justifications in the Skill Mapping were warranted.

5. **Project-level skills** — committed to `.claude/skills/` with the repo.

## Implementation Steps

### Step 1: Create `create-architecture-doc/SKILL.md`

**Create:** `.claude/skills/create-architecture-doc/SKILL.md`

Frontmatter:
```yaml
name: create-architecture-doc
description: Generates or updates the project architecture document with mandatory skill inventory and skill-deferred decisions. Ensures implementation details live in skills, not in the architecture doc.
```

Sections:
- **Purpose:** Generate or update `docs/architecture/{version}.md` with a structural guarantee that implementation patterns are deferred to skills.
- **When to use:** Starting a new project, adding a new service, or after significant skill additions/changes that may make inline architecture doc sections redundant.
- **Two modes:**
  - **Generate** — first-time creation. Full template. Scans `.claude/skills/` and builds the inventory from scratch.
  - **Refresh** — re-scans skills, diffs against current inventory, flags sections where a new skill now covers what was previously inline detail. Updates the inventory table. Does NOT rewrite the architecture doc from scratch.
- **Steps:**
  1. Scan `.claude/skills/` — read every `SKILL.md` frontmatter (`name` + `description`). Build a skill inventory table.
  2. If Refresh mode: read the existing architecture doc, compare its Skill Inventory against the current scan, identify stale or new entries.
  3. For each architectural decision area: check if a skill covers it. If yes, reference the skill and use the "Not specified here" field. If no skill exists, write the inline detail AND add the area to the Gaps section.
  4. Write the architecture doc following `references/template.md`.
  5. Report: list of skills referenced, list of gaps (areas needing skills).
- **What this skill does NOT do:** Make architectural decisions — those come from the architect's analysis. This skill enforces structure, not content. It does not write implementation code or plan artifacts.

Pattern: follow `create-feature-spec/SKILL.md` structure (reading protocol, steps, arguments, exclusions).

### Step 2: Create `create-architecture-doc/references/template.md`

**Create:** `.claude/skills/create-architecture-doc/references/template.md`

The template that the architecture doc output must follow. Sections:

```markdown
# {Project Name} — Architecture ({version})

**Status:** Draft | Current
**Date:** {date}
**Skill inventory built from:** `.claude/skills/` scan on {date}

---

## Skill Inventory

| Skill | Covers | Disposition |
|-------|--------|-------------|
| {skill-name} | {one-line from skill description} | Referenced / Gap |

Built by scanning `.claude/skills/` at generation time. This table is the source of truth for which patterns are skill-owned vs. inline.

---

## System Shape

High-level only: services, boundaries, communication patterns, deployment topology. What exists and why.

(No implementation patterns here — those live in skills.)

---

## Architectural Decisions

One subsection per decision area.

### {Decision Area}

**Decision:** {What was decided}
**Rationale:** {Why — constraints, trade-offs, alternatives rejected}
**Constraints:** {Non-negotiable rules this decision creates}
**Skill reference:** {skill-name} | None (see Gaps)
**Not specified here:** {What this section is NOT defining — explicitly name what the skill owns. e.g., "Entity base class shape, repository pattern, configuration inheritance — owned by `domain-patterns` and `persistence-patterns` skills."}

---

## Cross-Cutting Concerns

Same structure as Architectural Decisions, for: error handling, logging, auth, observability, etc.

### {Concern}

**Approach:** {What}
**Skill reference:** {skill-name} | None
**Not specified here:** {What the skill owns}

---

## Domain Model

Entity definitions, relationships, classification (aggregate root / entity / value object). Properties and types only — no persistence details, no code patterns.

Design notes: thin vs rich domain, event strategy, key strategy.

---

## Gaps

Areas where no skill exists yet. These are the ONLY sections in the architecture doc allowed to contain implementation detail.

| Gap | What's Missing | Suggested Skill Name |
|-----|---------------|---------------------|
| {area} | {what pattern would a skill define} | {name} |

Each gap is logged to `lessons.md` for future skill creation. As skills are created from gap entries, the architecture doc gets simpler — inline detail moves to the skill, the section gets a "Skill reference" and a "Not specified here."

---

## Deployment & Infrastructure

Environment, hosting, secrets management, CI/CD. Factual — not pattern-defining.

---

## What's Explicitly Out of Scope ({version})

Features, integrations, or capabilities not included in this version.
```

Include a note at the top of the template: "Omit sections that don't apply. Do not add sections beyond this template. If you want to add detail that a skill already covers, STOP — reference the skill and use 'Not specified here' instead."

### Step 3: Create `create-implementation-plan/SKILL.md`

**Create:** `.claude/skills/create-implementation-plan/SKILL.md`

Frontmatter:
```yaml
name: create-implementation-plan
description: Creates implementation plans with mandatory skill mapping per step. Ensures plan steps reference skills instead of restating patterns. Extends the agents-workflow.md plan format.
```

Sections:
- **Purpose:** Generate `docs/plans/{Feature}/plan.md` with a structural guarantee that each step either references a skill or explicitly justifies inline detail.
- **When to use:** After a feature spec exists (`docs/features/{Feature}.md` with `Status: Ready`). Replaces freeform plan writing.
- **Reading protocol (before writing):**
  1. Read the feature spec (`docs/features/{Feature}.md`)
  2. Read the architecture doc (for system shape and existing decisions)
  3. Scan `.claude/skills/` — read every `SKILL.md` frontmatter. If the architecture doc has a Skill Inventory section, use it; otherwise build one from scratch.
  4. Read existing plans (`docs/plans/*/plan.md`) for format consistency
- **Steps:**
  1. Pre-fill from conversation context (same as `create-feature-spec`).
  2. Run the reading protocol.
  3. Draft the implementation steps.
  4. **Build the Skill Mapping** — for each step, determine the disposition:
     - **Follow** — a skill covers this pattern. Provide only feature-specific inputs (entity names, file paths, property types). Do NOT restate how the pattern works.
     - **Build** — a skill covers this pattern but references infrastructure that doesn't exist locally (BuildingBlocks, base classes). The step must include building the missing infrastructure first, then following the skill.
     - **None** — no skill covers this step. Provide full inline detail. Log to the Gaps column for future skill creation.
  5. **Anti-pattern check:** Scan the draft for:
     - The word "adapted" or "adapt" near a skill reference → violation. Either Follow or Build, never Adapt.
     - A skill dismissed as "too simple" or "not needed for this case" → violation. Skills ensure consistency; complexity is not the criterion.
     - Implementation pattern details restated when a skill exists → over-specification. Delete and reference the skill.
  6. Write the plan following `references/plan-template.md`. Output: `docs/plans/{Feature}/plan.md`.
  7. Apply the auto-approve gate from `agents-workflow.md` (≤12 steps, no open questions → auto-approve and message developer).
- **What this skill does NOT do:** Write code. Make architectural decisions (those come from the architecture doc). Override the agents-workflow.md coordination protocol (it extends the plan format, doesn't replace the pipeline).

### Step 4: Create `create-implementation-plan/references/plan-template.md`

**Create:** `.claude/skills/create-implementation-plan/references/plan-template.md`

The plan template. Extends the existing format from `agents-workflow.md` with the Skill Mapping section. The template should include:

```markdown
# {Feature Name}

## Context
What problem this solves. Which service(s) impacted and why.

## Scope
In scope. Explicitly out of scope.

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | {skill-name} | Follow | {only what's unique: entity names, paths, types} | |
| 2 | {skill-name} | Build | {what to build locally + feature inputs} | |
| 3 | (none) | — | (full inline detail in step) | Log to lessons.md |

**Disposition rules:**
- **Follow** — apply the skill pattern. Adapt implementation specifics (e.g., SQLite instead of SQL Server) but never skip the pattern.
- **Build** — skill references infrastructure that doesn't exist locally. Build it first, then follow the skill.
- **None** — no skill covers this step. Full inline detail required. Log the gap.
- **Never "Adapt"** — if you're about to write "adapted for this project's needs," you're skipping the skill. Either Follow (the pattern applies, specifics may differ) or Build (the infrastructure is missing).

**Anti-patterns (if any of these appear, revise before proceeding):**
- A skill dismissed as "simple" or "not needed" → Skills ensure consistency, not just complexity.
- Pattern details restated alongside a skill reference → Over-specification. Delete the pattern detail, keep only feature-specific inputs.
- "No shared BuildingBlocks to reference" used to skip a skill → The pattern is the value, not the package. Build locally or Follow.

## Domain Model Changes
New/modified aggregates, entities, value objects, domain events.

## Data Model Changes
New tables, columns, relationships, migrations needed.

## Implementation Steps

Numbered steps — each step is one focused task.
For each step:
- What to do (not how to code it — unless Skill Mapping says "None")
- Which files to create or modify (full paths)
- Skill reference (from the mapping table) — if Follow or Build, say "Follow {skill-name}" or "Build {what} then follow {skill-name}"
- Feature-specific inputs only (entity names, property types, route paths)
- Dependencies on previous steps

## Cross-Service Changes (if applicable)
gRPC contract changes, integration events, consumers.

## Migration Notes
EF Core migration commands. Seed data if needed.

## Testing Strategy
Key scenarios to test.

## Open Questions
Unresolved decisions needing input.
```

Add a note: "This template extends the plan format in `agents-workflow.md`. The Skill Mapping section and anti-pattern rules are additions — all other sections follow the existing format."

### Step 5: Update `architect.md`

**Modify:** `.claude/agents/architect.md`

Changes:
1. In the **Plan Workflow** section (around line 90), add before step 2:
   - "Use the `create-implementation-plan` skill when writing plans. The skill's reading protocol, skill mapping, and anti-pattern check replace the freeform approach."

2. In the **Plan Writing Rules** section (around lines 100-105), simplify:
   - Remove the rules that are now owned by the skill ("Reference relevant skills", "Missing skill → lessons entry", "When a skill exists, provide only feature-specific inputs — never inline what the skill defines"). Replace with: "Follow the `create-implementation-plan` skill. The skill's template enforces skill references and prevents over-specification."

3. Add a new section **Architecture Doc Workflow** (before or after Plan Workflow):
   ```
   ## Architecture Doc Workflow

   Use the `create-architecture-doc` skill when writing or updating architecture documentation. The skill scans the skill inventory and structures the document to defer implementation patterns to skills.

   Two modes:
   - **Generate** — first-time creation for a new project or service.
   - **Refresh** — re-scan skills after skill additions/changes. Flags sections where inline detail now has a matching skill.
   ```

4. In the **What You Know** section (around line 76), add:
   - `.claude/skills/create-architecture-doc/` — architecture doc skill (scan + template)
   - `.claude/skills/create-implementation-plan/` — plan skill (mapping + template)

### Step 6: Update `agents-workflow.md`

**Modify:** `.claude/rules/agents-workflow.md`

Changes:
1. In the **Implementation Plan Format** section, add a note at the top:
   ```
   Plans created via the `create-implementation-plan` skill include an additional **Skill Mapping** section before Implementation Steps. See the skill's `references/plan-template.md` for the full template.
   ```

2. In the **Skill Authority** section (at the bottom), add:
   ```
   The `create-architecture-doc` and `create-implementation-plan` skills enforce this principle via mandatory skill inventory checks. Architecture docs defer implementation patterns to skills. Plans map each step to a skill or justify inline detail.
   ```

3. In the **Review Checklist — Step 2** section, add a new check:
   ```
   - Skill mapping verified: any "None" disposition in the Skill Mapping was warranted (no existing skill actually covers the step)
   ```

### Step 7: Verify

After all files are created/updated:
1. Confirm both new skill directories exist with correct file structure:
   - `.claude/skills/create-architecture-doc/SKILL.md`
   - `.claude/skills/create-architecture-doc/references/template.md`
   - `.claude/skills/create-implementation-plan/SKILL.md`
   - `.claude/skills/create-implementation-plan/references/plan-template.md`
2. Confirm `architect.md` references both skills
3. Confirm `agents-workflow.md` has the Skill Mapping note and reviewer check
4. Read each new file and confirm internal consistency (no references to nonexistent sections, no orphaned placeholders)

## Testing Strategy

- Read the `create-architecture-doc` template and confirm every section has a Skill Reference or Not Specified Here field
- Read the `create-implementation-plan` template and confirm the Skill Mapping table is mandatory (not optional)
- Read `architect.md` and confirm the freeform plan writing rules are replaced by skill references
- Read `agents-workflow.md` and confirm the reviewer checklist includes skill mapping verification

## Open Questions

None.
