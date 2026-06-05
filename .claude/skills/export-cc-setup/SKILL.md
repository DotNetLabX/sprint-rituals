---
name: export-cc-setup
version: 1.0.0
description: Exports the stack-independent portion of the .claude/ setup as a portable kit for other projects. Scans, classifies (portable / stack-specific / leaked), validates zero stack leaks, and packages agents, rules, conventions, and portable skills with a template CLAUDE.md. Blocks export on leaks with per-line remediation proposals. Gated by two interactive checkpoints.
triggers: ["/export-cc-setup", "export cc setup", "export setup kit", "package cc setup"]
args: "[output-path]"
---

# Export CC Setup

Extracts a stack-independent kit from the current project's `.claude/` setup. Scans every file against a stack fingerprint derived from `CLAUDE.md`, classifies files as portable / stack-specific / leaked. Blocks export if any pipeline files have stack leaks — proposes per-line remediations to fix at the source. When clean, assembles a ready-to-drop-in package for any project regardless of tech stack.

## Pipeline

```
CLAUDE.md ──> Phase 1: Discover ──> Phase 2: Classify ──> Phase 3: Package ──> Phase 4: Verify
               |                      |                     |                    |
               v                      v                     v                    v
          Stack fingerprint      Classification         Output directory     Leak-free report
          + file inventory       table (approved)       + manifest           + structural check
                                 [CHECKPOINT 1]                              [CHECKPOINT 2]
```

## Parameters

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `output-path` | No | `./cc-setup-kit` | Directory to write the exported kit |

## Configuration Constants

| Constant | Default | Description |
|----------|---------|-------------|
| OUTPUT_PATH | `./cc-setup-kit` | Target directory for the exported kit |
| FINGERPRINT_SOURCES | `CLAUDE.md: ## Tech Stack, ## Frontend` | Sections to extract stack terms from |
| LEAK_THRESHOLD | 20% | Max % of lines with stack terms before a file is classified as stack-specific (excluded). Files between 0% and this threshold are "leaked" — export is blocked until fixed in source. |
| FILL_MARKER | `<!-- FILL: {description} -->` | Placeholder in template CLAUDE.md for sections the user must complete |

## Hard Rules

1. **Never modify source files.** The current project's `.claude/` is read-only during export. All writes go to the output directory.
2. **Never export secrets or credentials.** Skip `settings.local.json` entirely. For `settings.json`, redact environment variable values and project-specific permission entries. Include hook definitions (they wire up the hook scripts).
3. **Fingerprint is derived, not hardcoded.** Stack terms come from the current project's CLAUDE.md, not from a built-in list. The skill works for any tech stack.
4. **User approves classification before packaging.** CHECKPOINT 1 is mandatory — the user sees the full classification table and can override any decision.
5. **Leaked files block export.** If any file has 1%-LEAK_THRESHOLD stack references, the export stops with a remediation report. Fix the source project first, then re-export. No auto-cleaning — the source must be clean.
6. **Skills are exported as complete directories.** Copy the entire `{skill}/` tree (SKILL.md + phases/ + playbooks/ + templates/ + references/), not just the SKILL.md.
7. **Output is self-contained.** The kit must work when copied into a project with zero files from the source project. No dangling references.
8. **Idempotent.** Running the skill twice with the same input overwrites the previous output cleanly. No merge — full replace.
9. **Template CLAUDE.md preserves structure.** The template keeps every section header from the source; stack-specific content is replaced with FILL markers, not removed.
10. **Cross-references are validated.** Agent files that reference skills, conventions, or rules must only reference items included in the kit.

## Phase Index

| Phase | Doc | Gate | Duration |
|-------|-----|------|----------|
| 1 Discover | `phases/1-discover.md` | Fingerprint extracted, inventory complete | 2-3 min |
| 2 Classify | `phases/2-classify.md` | CHECKPOINT 1: classification approved by user | 3-5 min |
| 3 Package | `phases/3-package.md` | Output directory assembled | 5-10 min |
| 4 Verify | `phases/4-verify.md` | CHECKPOINT 2: verification passed | 2-3 min |

Total: 12-21 min.

## Output Structure

```
{output-path}/
  .claude/
    README.md                        <-- updated skill count, portable-only
    settings.json                    <-- included only if it scans at 0%
    agents/                          <-- all agents (must be 0% to export)
    rules/                           <-- all rules (fully portable)
    conventions/                     <-- all conventions (fully portable)
    skills/                          <-- portable skills only, complete directories
    hooks/                           <-- portable hooks
    commands/                        <-- custom slash commands (if any exist)
  CLAUDE.md.template                 <-- template with FILL markers
  docs/
    conventions/
      README.md                      <-- explains what to put here
    kb/
      index.md                       <-- empty topic map template
      glossary.md                    <-- empty glossary template
    architecture/
      README.md                      <-- explains what to put here
  INSTALL.md                         <-- setup instructions for recipient project
  MANIFEST.md                        <-- what's included, what was excluded, why
```

## Classification Rules

### Category 1: Portable (include as-is)

File's primary purpose is workflow, process, or coordination AND zero fingerprint term matches after scanning.

**Portable by default (verified by fingerprint scan):**
- `.claude/rules/*` — behavioral constraints, no code patterns
- `.claude/conventions/*` — artifact formats, coordination protocol
- `.claude/hooks/*` — infrastructure tooling
- `.claude/commands/*` — custom slash commands (if directory exists)
- `.claude/settings.json` — if hook definitions and permissions scan clean

These categories are expected to scan clean, but Phase 2 verifies every file. Any file with fingerprint matches is reclassified as leaked or stack-specific regardless of category.

### Category 2: Stack-Specific (exclude)

File's primary purpose is code scaffolding or technology-specific patterns.

**Stack-specific signals:**
- Skill whose `description:` frontmatter names specific technologies (frameworks, ORMs, UI libraries)
- Match % > LEAK_THRESHOLD across file content
- File lives under `docs/conventions/` (stack coding standards)

### Category 3: Leaked (block — remediate in source)

File is primarily portable but contains stack references that should not be there (0 < match % <= LEAK_THRESHOLD). **Export is blocked until all leaked files reach 0%.**

The skill analyzes each match and proposes a remediation — where to move the stack-specific content in the source project so the pipeline file stays clean. See `playbooks/remediation-rules.md` for the remediation patterns.

**Common leak sources:**
- Build/test commands hardcoded in agent files → move to `docs/conventions/project-rules.md`
- Framework names in behavioral rules → generalize the prose, move specifics to convention files
- Stack-specific skill references in agent examples → generalize or remove
- Stack-specific permissions in `settings.json` → move to `settings.local.json`

**Design principle:** The source project's pipeline layer should be 0% stack-coupled at all times — not just at export time. Cleaning during export masks drift. Fixing at the source keeps both the source project and all future exports clean.

## Stack Fingerprint

The fingerprint is derived from the current project — never hardcoded. See `playbooks/stack-detection.md` for the full extraction protocol.

Summary: Read `CLAUDE.md` sections `## Tech Stack` and `## Frontend`. Extract every technology name, framework, library, and tool mentioned. Build case-insensitive regex patterns. Add common aliases (e.g., "EF Core" also matches "Entity Framework", "DbContext").

## Template CLAUDE.md

See `playbooks/template-generation.md` for the full protocol.

Summary: Copy the source CLAUDE.md. For each section:
- **Keep as-is:** Behavioral guardrails, generic conventions, workflow mechanics
- **Templatize:** `## Tech Stack`, `## Frontend`, `## Repo Structure`, `## Architecture` — replace content with FILL markers that describe what to put there and include examples from a different stack
- **Remove:** Project-specific integration sections (e.g., graphify, project-specific tool configs) that have no template value

## Downstream Consumers

| Consumer | What they need | Notes |
|----------|---------------|-------|
| New project setup | Complete kit in output-path | Copy `.claude/` + fill in CLAUDE.md.template |
| Cross-project sync | MANIFEST.md for diffing | Compare manifests between kit versions to see what changed |
| Skill creators | Portable skill examples | Use included skills as format reference for new stack-specific skills |

## Error Handling

| Condition | Action |
|-----------|--------|
| CLAUDE.md has no `## Tech Stack` | Warn: fingerprint will be empty, all files classified as portable. Proceed only after user confirms. |
| Output directory already exists | Ask user: overwrite or abort. Never silently overwrite. |
| Agent file references skill excluded from the kit | Flag in Phase 4 verification. If the reference is in the source, it's a leak — remediation needed. |
| Zero portable skills found | Warn but proceed — the kit has value from agents/rules/conventions alone |
| Skill directory has broken internal references | Copy as-is, note in MANIFEST.md. Skill-internal integrity is not this skill's responsibility. |
| Binary files in skill directories (images, diagrams) | Copy as-is without fingerprint scanning. Note in MANIFEST.md as unscanned. |
| `.claude/commands/` directory exists | Inventory and classify like other files. Custom commands are typically portable. |
| `.claude/settings.json` missing | Skip — not all projects have one. Note in MANIFEST.md. |

## Refusal Patterns

| Request | Response |
|---------|----------|
| "Export stack-specific skills too" | "That defeats the purpose. Stack-specific skills belong in each project. Create matching skills for your new stack instead." |
| "Skip the classification checkpoint" | "Classification review is mandatory — it catches false positives that would leak stack terms into other projects." |
| "Modify the source project's files" | "This skill is read-only on the source. All writes go to the output directory." |
| "Export to the same .claude/ directory" | "Output path must differ from source to prevent overwriting your working setup." |
