# Architecture Doc Template

Use this template for every `docs/architecture/{version}.md` file. Omit sections that don't apply. Do not add sections beyond this template. If you want to add detail that a skill already covers, STOP — reference the skill and use "Not specified here" instead.

---

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
