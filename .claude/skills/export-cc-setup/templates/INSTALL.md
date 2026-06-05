# Claude Code Setup Kit - Installation Guide

## Quick Start

1. Copy the `.claude/` directory into your project root
2. Copy `CLAUDE.md.template` to your project root as `CLAUDE.md`
3. Fill in every `<!-- FILL: ... -->` section in `CLAUDE.md`
4. Copy the `docs/` directory into your project root
5. Create your stack-specific conventions and skills (see below)

## Fill-in Checklist

After copying, complete these project-specific items:

### CLAUDE.md (required)

- [ ] `## Tech Stack` — your language, frameworks, libraries, and tools
- [ ] `## Repo Structure` — your project's folder structure with descriptions
- [ ] `## Frontend` — your frontend stack (remove section if backend-only)
- [ ] Review all `<!-- FILL: ... -->` markers and replace with your project's details

### docs/conventions/ (required)

Create files that define your project's coding standards:

- [ ] `project-rules.md` — build rules, file structure rules, project reference graph, source file definitions
- [ ] `coding-conventions.md` — naming, formatting, and style conventions for your stack

These files are loaded by agents via `@` references. Without them, agents will lack stack-specific guidance.

### docs/kb/ (recommended)

- [ ] Populate `glossary.md` with your domain terms
- [ ] Add KB entries for core business concepts (see `kb/index.md` for the template)
- [ ] Follow the schema in `.claude/rules/kb-maintenance.md`

### docs/architecture/ (recommended)

- [ ] Create your architecture document (use the `create-architecture-doc` skill to generate it from your codebase)

### .claude/settings.json

- [ ] Add your stack-specific build/test/lint commands to the permission allowlist (in `settings.local.json`, not `settings.json`)
- [ ] Add any project-specific environment variables to `settings.local.json`
- [ ] Verify hook script paths are correct for your project layout

### docs/conventions/ details

When creating `coding-conventions.md`, include these sections that portable skills read from:
- [ ] `## Boy Scout Guardrails` — the `boy-scout` skill reads this section to know which code structures are contracts and which boundaries not to cross during cleanup

### Stack-specific skills (recommended)

This kit includes only process skills. Create skills for your technology patterns.

<!-- GENERATED: Phase 3.5 replaces this section with a dynamically generated checklist
     derived from the excluded skills, grouped by purpose category:
     - Feature scaffolding (endpoint/handler/controller pattern)
     - Data access / persistence (ORM config, repositories, migrations)
     - Error handling (exception hierarchy, middleware)
     - Domain modeling (aggregates, entities, value objects — if using DDD)
     - Frontend patterns (component architecture, state management — if applicable)
     - Frontend review checklist (if applicable)
     - Integration patterns (messaging, gRPC, event handling — if applicable)
     Do not copy these example names verbatim — generate from actual excluded skills. -->

## Using the Setup

| Task | Command |
|------|---------|
| Full feature pipeline | `be team-lead` and describe the feature |
| Small fix (1-3 files) | `be solo` and describe the change |
| Write a feature spec | `be po` with the feature idea |
| Plan without spec | `be architect` with the technical task |
| Review the setup | Read `.claude/README.md` first |

## Creating Stack-Specific Skills

1. Study an included skill (e.g., `.claude/skills/tdd/SKILL.md`) for the format
2. Create `.claude/skills/{name}/SKILL.md` following the same structure
3. For complex skills, add subdirectories: `phases/`, `playbooks/`, `templates/`
4. Skills are auto-discovered by the architect during planning — no registration step needed

### Skill format reference

```yaml
---
name: your-skill-name
description: One-line description of what the skill does and when to use it.
---
```

The body should include: When to Use, Steps or Phases, Hard Rules, and What This Skill Does NOT Do.

## What's Included

See `MANIFEST.md` for the complete list of included files, excluded files, and the reasoning behind each classification decision.

## Keeping the Kit Updated

If the source project's setup evolves, re-run the `export-cc-setup` skill from the source to produce an updated kit. Compare `MANIFEST.md` between versions to see what changed.
