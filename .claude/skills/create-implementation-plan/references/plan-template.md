# Plan Template

Use this template for every `docs/specs/{slug}/delivery/plan.md` file. This template extends the plan format in `agents-workflow.md`. The Skill Mapping section and anti-pattern rules are additions — all other sections follow the existing format.

---

```markdown
# {Feature Name}

**Feature Spec:** `docs/specs/{slug}/definition/spec.md` | None

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
- **None** — no skill covers this step. Describe what to accomplish + acceptance criteria. Do NOT write method-body logic flows. The developer decides internal decomposition. Log the gap.
- **Never "Adapt"** — if you're about to write "adapted for this project's needs," you're skipping the skill. Either Follow (the pattern applies, specifics may differ) or Build (the infrastructure is missing).

**Anti-patterns (if any of these appear, revise before proceeding):**
- A skill dismissed as "simple" or "not needed" — Skills ensure consistency, not just complexity.
- Pattern details restated alongside a skill reference — Over-specification. Delete the pattern detail, keep only feature-specific inputs.
- "No shared BuildingBlocks to reference" used to skip a skill — The pattern is the value, not the package. Build locally or Follow.

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

**Always include a final numbered step for KB updates** if the KB Impact section identifies entries to update. Trailing sections are routinely skipped — only numbered steps are verified by the done check.

**Follow step content budget:** When disposition is Follow, include ONLY what the skill can't know — feature-specific inputs. The developer invokes the skill via the Skill tool to get structural patterns. Over-specifying Follow steps causes the developer to skip skill invocation (all info is already inline). Target: 5-15 lines. A Follow step exceeding 30 lines is over-specified — split feature-specific inputs from structural patterns and delete the structural patterns.

## Cross-Service Changes (if applicable)
gRPC contract changes, integration events, consumers.

## Migration Notes
Migration commands per persistence convention. Seed data if needed.

## Testing Strategy
Key scenarios to test.

## KB Impact
Which `docs/kb/` entries need updating or creating after this feature? (None if purely frontend/infra)

**Important:** If any KB entries need updating, the architect MUST include a numbered implementation step for KB updates (e.g., "Step N: Update KB entries"). Trailing sections after the numbered steps are routinely skipped by developers. Making it a numbered step ensures the done check catches it.

## Open Questions
Unresolved decisions needing input.
```
