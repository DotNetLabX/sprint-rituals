---
name: improve-skills
description: Creates new skills and fixes existing skills based on lessons. Scaffolds .claude/skills/{name}/SKILL.md for skill gaps. Updates existing SKILL.md for skill fixes. Invoked by the learner agent after classification and user approval.
---

# Improve Skills

Creates new skills and updates existing skills based on classified lesson items. Each item has already been classified by the learner agent as either a "skill fix" (existing skill needs correction) or "skill gap" (new skill needed).

## For Skill Fixes

1. **Read the existing skill's SKILL.md** fully.
2. **Identify the section** that needs correction based on the lesson content.
3. **Apply the fix**, preserving the skill's existing structure and style.
4. **Log the fix** to `docs/skill-backlog.md` under the skill's entry with status `Fixed`.

## For Skill Gaps (New Skills)

1. **Read the gap description** from the lesson — what pattern is missing, why it was needed, reference files mentioned.

2. **Verify the gap is real:**
   - Grep `.claude/skills/` for the pattern name — confirm no existing skill covers it.
   - Check that reference files mentioned in the lesson still exist.
   - Confirm the pattern is repeatable (will be needed again, not a one-off).

3. **Study 2-3 existing skills** in `.claude/skills/` that are closest in type. Match their structure.

4. **Scaffold the skill directory:**
   ```
   .claude/skills/{skill-name}/
   └── SKILL.md
   ```
   Add `workflows/` or `references/` subdirectories only if the skill is variant-aware or needs templates.

5. **Write SKILL.md** following the native Claude Code format:

   ```yaml
   ---
   name: {skill-name}
   description: {one-line description — specific enough for auto-detection}
   ---
   ```

   Body sections (follow the pattern of existing project skills):
   - `# {Skill Name}` — one-sentence purpose
   - `## Steps` — numbered workflow steps, concrete and actionable
   - `## Arguments` — if the skill takes arguments (e.g., `/skill-name FeatureName`)
   - Additional sections as needed (folder structure, conventions, examples)

6. **Use the lessons context** to write the steps — the gap description tells you what was needed, the reference files show the actual codebase pattern.

7. **Read the reference files** mentioned in the gap. Extract the real pattern from the codebase — don't invent abstract instructions.

## Skill Backlog

Log every action to `docs/skill-backlog.md`. Create the file if it doesn't exist.

Entry format:

```markdown
### {Skill Name}
- **Status:** Created | Fixed
- **Type:** Gap | Fix
- **Source:** {Feature name} lessons
- **Description:** {What it covers or what was fixed}
- **Date:** {YYYY-MM-DD}
```

Group entries under `## Skills Created` and `## Skills Fixed` headings.

## Quality Gate

Before creating a new skill, verify all of:
- The pattern is **repeatable** — it will be needed again
- **No existing skill** covers it, even partially
- The **reference files** mentioned in the gap still exist
- The skill follows the project's **existing skill conventions** (check 2-3 neighbors)
- The steps are **concrete and actionable** — not abstract guidance
- The skill is **generic and reusable across projects** — no project-specific names, paths, or assumptions baked into the skill logic. Use placeholders (`{Name}`, `{Svc}`) and derive project-specific details from the codebase or CLAUDE.md at runtime

If the quality gate fails, log the gap to the backlog with status `Deferred` and the reason.

## Cross-Reference Lint

Before renaming or deleting a skill, grep all agent files and convention files for references to the old name. Update or remove stale references before completing the change.

## Changelog Maintenance

When modifying an existing skill (fix or significant update), append a changelog entry to its `CHANGELOG.md`:

1. If `CHANGELOG.md` doesn't exist for the skill, create it following the format in any existing skill CHANGELOG (e.g., `.claude/skills/create-implementation-plan/CHANGELOG.md`).
2. Add the entry under `## [Unreleased]` — one bullet per meaningful change.
3. Include: what changed, why it changed, and the evidence citation (feature slug or lesson source).

Entry format:
```markdown
- {What changed} ({evidence — feature slug or "lessons analysis"})
```

Do not create a version entry per change — group unreleased changes together. The learner/architect promotes to a versioned entry when the skill reaches a stable milestone.

## Post-Apply

After creating or removing skills, read `.claude/README.md` and update the skill count and any affected sections.

## What This Skill Does NOT Do

- Classify items — the learner already did that.
- Modify CLAUDE.md, agent files, or rule files — that's `improve-flow`.
- Write application code.
- Create OMC-format skills — only native Claude Code `.claude/skills/` format.
- Tag items as [APPLIED] — the learner handles tagging after skill completes.
