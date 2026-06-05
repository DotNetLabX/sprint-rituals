---
name: create-architecture-doc
description: Generates or updates the project architecture document with mandatory skill inventory and skill-deferred decisions. Ensures implementation details live in skills, not in the architecture doc.
user-invocable: true
---

# Create Architecture Doc (Architect Reference)

This skill is for the **architect agent**. It produces `docs/architecture/{version}.md` — the technical architecture document that implementation plans are built against. The architecture doc defines system shape and decisions; implementation patterns live in skills.

## Purpose

Generate or update `docs/architecture/{version}.md` with a structural guarantee that implementation patterns are deferred to skills. Every architectural decision explicitly names what it is NOT defining — the forcing function that prevents over-specification.

## When to use

- Starting a new project or adding a new service (Generate mode).
- After significant skill additions or changes that may make inline architecture doc sections redundant (Refresh mode).

## Two modes

- **Generate** — first-time creation. Full template. Scans `.claude/skills/` and builds the inventory from scratch.
- **Refresh** — re-scans skills, diffs against current inventory, flags sections where a new skill now covers what was previously inline detail. Updates the inventory table. Does NOT rewrite the architecture doc from scratch.

## Reading protocol

Before writing, ensure you have:

1. **Business spec** — read on-demand (project-specific path listed in the Architect agent's "What you know" section)
2. **Architecture reference** — if Refresh mode, read the existing `docs/architecture/{version}.md`
3. **Skill inventory** — scan `.claude/skills/` — read every `SKILL.md` frontmatter (`name` + `description`). Build a skill inventory table.
4. **Existing architecture docs** — read `docs/architecture/*.md` for format consistency and version history

## Steps

1. **Scan `.claude/skills/`** — read every `SKILL.md` frontmatter (`name` + `description`). Build a skill inventory table with columns: Skill, Covers, Disposition (Referenced / Gap).

2. **If Refresh mode:** read the existing architecture doc, compare its Skill Inventory against the current scan, identify stale entries (skills removed or renamed) and new entries (skills added since last scan).

3. **For each architectural decision area:** check if a skill covers it.
   - If yes: reference the skill and fill the "Not specified here" field naming what the skill owns. Do not restate the skill's pattern.
   - If no skill exists: write the inline detail AND add the area to the Gaps section.

4. **Write the architecture doc** following `references/template.md`. Output path: `docs/architecture/{version}.md`.

5. **Report:** list of skills referenced, list of gaps (areas needing skills that should be logged to `lessons.md`).

## Arguments

Pass the version identifier: `create-architecture-doc v2`

For Refresh mode, pass the existing version: `create-architecture-doc v1 --refresh`

## What this skill does NOT do

- Make architectural decisions — those come from the architect's analysis. This skill enforces structure, not content.
- Write implementation code or plan artifacts.
- Replace the architect agent's judgment about system shape, technology choices, or domain modeling.
- Rewrite the architecture doc from scratch when in Refresh mode — it only updates the inventory and flags stale sections.
