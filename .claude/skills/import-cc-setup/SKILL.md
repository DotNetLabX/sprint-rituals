---
name: import-cc-setup
version: 1.0.0
description: Imports a portable CC setup kit (produced by export-cc-setup) into an existing or new project. Discovers kit contents and target state, plans merge actions with collision detection, applies with user approval, and verifies cross-reference integrity. Archives displaced files — never deletes.
triggers: ["/import-cc-setup", "import cc setup", "import setup kit", "apply cc setup"]
args: "[kit-path]"
---

# Import CC Setup

Imports a portable kit (produced by `export-cc-setup`) into the current project. Handles both greenfield projects (simple copy) and established projects with existing `.claude/` setups (collision detection, archival, merge). Never deletes target files — collisions are archived to `.claude/_archived/`.

## Pipeline

```
Kit directory ──> Phase 1: Discover ──> Phase 2: Plan ──> Phase 3: Apply ──> Phase 4: Verify
                    |                     |                  |                  |
                    v                     v                  v                  v
               Kit + target          Import plan         Files written      Cross-ref check
               inventories           (approved)          + archived         + integrity
                                    [CHECKPOINT 1]                         [CHECKPOINT 2]
```

## Parameters

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `kit-path` | No | `./cc-setup-kit` | Directory containing the exported kit |

## Configuration Constants

| Constant | Default | Description |
|----------|---------|-------------|
| KIT_PATH | `./cc-setup-kit` | Source directory for the kit |
| ARCHIVE_DIR | `.claude/_archived` | Where displaced files are moved |
| KIT_MANIFEST_FILE | `.claude/.kit-manifest.md` | Tracks what was imported for re-import diffing |
| REQUIRED_KIT_FILES | `MANIFEST.md, CLAUDE.md.template, INSTALL.md` | Files that must exist for a valid kit |

## Hard Rules

1. **Never delete target files.** Displaced files go to `.claude/_archived/{YYYYMMDD}/{category}/`. The user's existing work is preserved.
2. **Never overwrite without approval.** CHECKPOINT 1 is mandatory — the user sees the full import plan before any writes happen.
3. **Kit wins on collision (after approval).** When the user approves a collision resolution, the kit version replaces the target version. The original is archived.
4. **MANIFEST.md validates the kit.** Refuse to import from a directory without MANIFEST.md — it may not be a valid export.
5. **CLAUDE.md is merged, not replaced.** Kit sections are added to the existing CLAUDE.md. Target's stack-specific content is preserved. Conflicting sections are flagged to the user before stripping.
6. **Settings.json is merged per value shape.** Scalars: kit wins on collision. Objects: recursive deep merge. Arrays of hook objects: concatenate (kit entries appended to target). Arrays of permission strings: union (deduplicate). This prevents kit hooks from silently replacing target hooks.
7. **Re-import is safe.** The `.kit-manifest.md` tracks imported files. Re-importing a newer kit only touches kit-originated files, not user additions.
8. **docs/ placeholders don't overwrite existing files.** Check at the **file** level, not directory level. If `docs/kb/glossary.md` already exists, skip that placeholder. If `docs/kb/index.md` doesn't exist (even though `docs/kb/` has other files), create it. Step 3.7 (normalize entry points) runs first — it renames existing entry points to `index.md` before placeholders are evaluated.
9. **Skills are imported as complete directories.** Same as export — the full skill tree, not just SKILL.md.
10. **Archive preserves structure.** Archived files go to `.claude/_archived/{YYYYMMDD}/{category}/{filename}` mirroring their original category. Exception: root-level files (CLAUDE.md, settings.json) go directly under the date directory since they live outside `.claude/`.
11. **Never modify kit file content to resolve references.** Agent files, rules, conventions, and skills are copied verbatim from the kit. If an `@docs/...` reference points to a file that doesn't exist in the target project, report it as a file the user must create — never rewrite the reference to match an existing file. The kit defines the expected project structure; the project conforms to the kit, not the other way around.

## Phase Index

| Phase | Doc | Gate | Duration |
|-------|-----|------|----------|
| 1 Discover | `phases/1-discover.md` | Kit validated, inventories complete | 1-2 min |
| 2 Plan | `phases/2-plan.md` | CHECKPOINT 1: import plan approved by user | 2-3 min |
| 3 Apply | `phases/3-apply.md` | All files written, archives created | 3-5 min |
| 4 Verify | `phases/4-verify.md` | CHECKPOINT 2: verification passed | 1-2 min |

Total: 7-12 min.

## Merge Strategies

| Category | Greenfield | Existing — no collision | Existing — collision |
|----------|-----------|------------------------|---------------------|
| **CLAUDE.md** | Rename template, prompt FILL markers | Merge kit sections into existing (see `playbooks/claude-md-merge.md`) | Flag conflicting sections, user decides |
| **Agents** | Copy | Copy | Archive target, copy kit |
| **Rules** | Copy | Copy alongside | Per-file decision: archive collisions, preserve non-colliding |
| **Conventions** | Copy | Copy; update index (comment out existing entries as examples) | Archive target on collision, copy kit |
| **Skills** | Copy directory | Copy directory | Archive target directory, copy kit |
| **Hooks** | Copy | Copy | Archive target, copy kit |
| **Commands** | Copy | Copy | Archive target, copy kit |
| **Settings.json** | Copy | Shape-aware merge (see Hard Rule 6) | N/A (always merged) |
| **.claude/README.md** | Copy | Regenerate post-import | Regenerate post-import |
| **docs/ placeholders** | Copy | Skip (target has content) | N/A |

## Collision Resolution

See `playbooks/collision-resolution.md` for the full protocol.

Summary: Every collision is presented at CHECKPOINT 1. The user approves the plan before any writes. Default action for collisions is "archive target, use kit" — the user can override to "keep target" or "skip" per file.

## FILL Marker Handling

The kit's `CLAUDE.md.template` contains `<!-- FILL: {description} -->` markers for sections the user must customize (Tech Stack, Frontend, Repo Structure).

- **Greenfield:** Prompt the user to fill each marker interactively.
- **Existing project:** If the target's CLAUDE.md has matching sections (by `##` header), extract that content to fill the marker automatically. Present the pre-filled result at CHECKPOINT 1 for review.

## Re-Import Protocol

When `.claude/.kit-manifest.md` exists (indicates prior import):

1. Parse it to identify kit-originated files vs user additions.
2. Only process kit-originated files — user additions are untouched.
3. Show a diff summary: what changed between old kit and new kit.
4. Same CHECKPOINT 1 approval flow.

## Error Handling

| Condition | Action |
|-----------|--------|
| Kit directory doesn't exist | Error: "Kit not found at `{path}`. Run `export-cc-setup` first." |
| MANIFEST.md missing in kit | Error: "Not a valid kit — MANIFEST.md missing. Ensure this was produced by `export-cc-setup`." |
| CLAUDE.md.template missing | Warning: proceed without CLAUDE.md merge. Note in import report. Phase 1 lists it as required but this is a soft requirement — the kit has value from agents/rules/skills alone. Warn and continue. |
| Target is read-only | Error: "Cannot write to target project. Check permissions." |
| Archive directory already exists | Use timestamped subdirectory: `_archived/{YYYYMMDD}/` (see `playbooks/collision-resolution.md` for same-day counter rule) |
| `.kit-manifest.md` is corrupted | Warning: treat as fresh import. Note that re-import tracking is lost. |
| Kit version mismatch | Warning: show version difference. Proceed — import is forward-compatible. |
| Zero collisions detected | Omit the Collisions and Archive sections from the CHECKPOINT 1 plan, but still present the plan for approval |

## Refusal Patterns

| Request | Response |
|---------|----------|
| "Import without showing the plan" | "The import plan review is mandatory — it prevents accidental overwrites of your existing setup." |
| "Delete my existing setup first" | "This skill archives, never deletes. If you want a clean slate, manually remove `.claude/` first, then re-run." |
| "Import only some files from the kit" | "Use the import plan at CHECKPOINT 1 to mark individual items as 'skip'. The plan supports per-file overrides." |
| "Merge skills instead of replacing" | "Skills are atomic directories — partial merges risk broken internal references. Archive the old version and adopt the kit version." |

## Downstream Consumers

| Consumer | What they need | Notes |
|----------|---------------|-------|
| Existing projects | Merged `.claude/` + filled CLAUDE.md | Import handles the full merge flow |
| Re-import workflows | `.kit-manifest.md` for diffing | Knows what to update vs leave alone |
| Team onboarding | One command to adopt the team's CC setup | Standard pipeline setup across all team projects |
