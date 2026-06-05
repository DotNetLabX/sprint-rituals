# Collision Resolution Protocol

How to handle files that exist in both the kit and the target project.

## Principle

The kit represents the team's standardized pipeline setup. On collision, the kit version is preferred — but the user always approves first. Displaced files are archived, never deleted.

## Archive Structure

```
.claude/_archived/{YYYYMMDD}/
  agents/          <- displaced agent files
  rules/           <- displaced rule files
  conventions/     <- displaced convention files
  skills/          <- displaced skill directories (complete)
  hooks/           <- displaced hook files
  commands/        <- displaced command files
  CLAUDE.md        <- target's original CLAUDE.md (before merge)
  settings.json    <- target's original settings.json (before merge)
```

The `{YYYYMMDD}` timestamp ensures multiple imports don't overwrite each other's archives. If the same date directory exists (multiple imports in one day), append a counter: `{YYYYMMDD}-2`.

## Collision Types

### File collision (same filename)
Agent, rule, convention, hook, or command file exists at the same relative path in both kit and target.

**Default action:** archive target, copy kit
**User overrides:** `keep-target` (skip kit file), `skip` (neither changes)

### Skill collision (same skill directory name)
Skill directory with the same name exists in both kit and target.

**Default action:** archive entire target skill directory, copy entire kit skill directory
**User overrides:** same as file collision
**Note:** skills are atomic — no partial merge. The entire directory is replaced.

### Content merge (CLAUDE.md, settings.json)
Files that are always merged at the content level, not file level.

**Default action:** merge per respective protocol (see `playbooks/claude-md-merge.md` for CLAUDE.md, Phase 3 Step 3.5 for settings.json)
**User overrides:** `keep-target` (don't merge kit content), `use-kit-only` (replace entirely with kit version)
**Archive:** target's original is always archived before merge, regardless of override

## Category-Specific Rules

### Rules — per-file decisions

Rules get individual treatment, not blanket actions:

1. List all target rules with a one-line description (first meaningful line or frontmatter description)
2. List all kit rules the same way
3. Present both lists at CHECKPOINT 1
4. For non-colliding target rules: **preserve** (keep alongside kit rules). These are project-specific constraints that remain valid.
5. For colliding rules (same filename): default is `archive-and-copy`. User can override per rule.

### Agents — announce replacements

Agents are critical — replacing one changes pipeline behavior:

1. For each collision, show both the target and kit agent's description/first paragraph
2. Announce: "These agents will be replaced. Their originals will be archived."
3. User confirms or overrides per agent

### Skills — no partial merge

Skills are atomic directories. On collision:
1. Show both skill descriptions (from SKILL.md frontmatter)
2. The entire target skill directory is archived
3. The entire kit skill directory replaces it
4. No file-level cherry-picking within a skill

### Conventions — index preservation

For convention file collisions, standard archive-and-copy applies.

For the conventions index (if one exists):
- Don't archive the index — merge it
- Comment out existing entries as examples
- Add kit entries below
- This preserves the target's convention references as visible documentation

## Presentation at CHECKPOINT 1

For each collision, show:

```
**{category}: {filename}**
Target: {one-line description or first line}
Kit:    {one-line description or first line}
Action: archive target, use kit [override: keep-target | skip]
```

Group by category (agents, rules, skills, etc.). If more than 10 total collisions, show the summary table first with an option to "Review details" per category.

## Re-Import Collisions

On re-import (`.kit-manifest.md` exists), collisions are classified differently:

| Scenario | Meaning | Default action |
|----------|---------|---------------|
| Kit-vs-kit | Kit file updated since last import | archive old, copy new |
| Kit-vs-user-modified | User edited a kit-originated file | **flag** — user's changes would be lost |
| New-vs-target | New kit file collides with existing target file | same as fresh import |

**Kit-vs-user-modified** is the most important case: the user customized something the kit provided. Always flag these with full context — never silently overwrite user modifications.
