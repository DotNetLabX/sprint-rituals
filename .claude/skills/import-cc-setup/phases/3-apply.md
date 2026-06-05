# Phase 3: Apply

Execute the approved import plan. Every write follows the approved action — no deviations.

## Pre-checks

1. Re-validate that the kit directory still exists and hasn't changed since Phase 1.
2. If `.claude/_archived/` already exists (from a prior import), the timestamped subdirectory ensures no overwrite. See `playbooks/collision-resolution.md` for the `{YYYYMMDD}` naming and same-day counter rule.

## Steps

### 3.1 Create archive directory

If any files will be archived, create the archive structure:
```
.claude/_archived/{YYYYMMDD}/
  agents/
  rules/
  conventions/
  skills/
  hooks/
  commands/
```

Only create subdirectories that will actually receive files.

### 3.2 Archive collision files

For each file with action `archive-and-copy`:
1. Copy the target file to `.claude/_archived/{YYYYMMDD}/{category}/{filename}`
2. For skills, copy the entire skill directory to `.claude/_archived/{YYYYMMDD}/skills/{skill-name}/`

### 3.3 Copy new and replacement files

For each file with action `copy` or `archive-and-copy`:
1. Create parent directories if needed
2. Copy the kit file to the target path
3. For skills, copy the entire skill directory preserving internal structure

### 3.3b Handle re-import statuses

On re-import, CHECKPOINT 1 resolves `flag` actions into concrete actions. Execute per the user's decision:

| Resolved action | What to do |
|----------------|------------|
| `flag` -> `overwrite` | Archive the current file, copy the kit version (same as `archive-and-copy`) |
| `flag` -> `keep` | Do nothing — leave the file as-is |
| `preserve` (user-owned) | Do nothing — never touch user-added files |
| `removed` -> `archive` | Move the file to `.claude/_archived/{YYYYMMDD}/{category}/` (default — per Hard Rule 1, never delete) |
| `removed` -> `keep` | Do nothing — user wants to keep the old kit file even though the kit no longer includes it |
| `skip` (unchanged) | Do nothing — file already matches the kit version |

### 3.4 Merge CLAUDE.md

Follow `playbooks/claude-md-merge.md`:

**If target has no CLAUDE.md (greenfield):**
1. Copy `CLAUDE.md.template` to `CLAUDE.md`
2. Remove the generation metadata header comment
3. For each FILL marker, prompt the user to provide content via `AskUserQuestion`
4. Replace each marker with the user's input

**If target has existing CLAUDE.md:**
1. Archive the target's CLAUDE.md to `.claude/_archived/{YYYYMMDD}/CLAUDE.md`
2. Start with the target's CLAUDE.md as the base document
3. For each kit section:
   - **FILL marker sections where target has matching content:** replace the FILL marker with the target's existing section content (auto-filled)
   - **FILL marker sections with no target match:** prompt the user via `AskUserQuestion`, showing the marker's description and examples as guidance
   - **Pipeline sections missing from target:** add to the CLAUDE.md in a logical position
   - **Pipeline sections that conflict:** replace with kit version — the user was notified at CHECKPOINT 1, and the original is preserved in the archive
4. Target sections not in the kit are preserved in place
5. Strip the kit's generation metadata header comment

### 3.5 Merge settings.json

**If target has no settings.json:**
- Copy kit's settings.json directly

**If target has existing settings.json:**
1. Archive target's settings.json to `.claude/_archived/{YYYYMMDD}/settings.json`
2. Read both as JSON
3. Merge per value shape:

| Value shape | Merge rule | Example |
|-------------|-----------|---------|
| Scalar (string, number, bool) | Kit wins | `"model": "opus"` — kit value replaces target |
| Object (nested keys) | Recursive deep merge | `"permissions": {...}` — recurse into sub-keys |
| Array of hook objects | Concatenate (kit appended to target) | `"PreToolUse": [...]` — target hooks preserved, kit hooks added after |
| Array of permission strings | Union (deduplicate) | `"allow": [...]` — combine both lists, remove duplicates |

4. Preserve target keys not in kit
5. Write merged result to `.claude/settings.json`

**Why not simple "kit wins":** settings.json contains arrays (hooks, permissions) where both target and kit entries are valid and must coexist. Replacing the entire array would destroy the target's custom hooks and stack-specific build permissions.

### 3.6 Merge conventions

Copy kit convention files to `.claude/conventions/`.

If the target has a conventions index (a file in `.claude/conventions/` whose primary purpose is listing or linking to other convention files — identified by name containing "index", "map", or "README", or by content that is predominantly a list of links/references to other `.md` files):
1. Read existing index content
2. Comment out existing entries as reference examples:
   ```markdown
   <!-- Existing conventions (preserved as reference):
   {original index content}
   -->
   ```
3. Add kit convention entries below the commented block

Non-colliding target conventions remain in place untouched.

### 3.7 Normalize docs/ entry points

The kit convention is that every docs/ category uses `index.md` as its entry point. Agent `@` references rely on this (`@docs/architecture/index.md`, `@docs/product/index.md`, `@docs/kb/index.md`).

**Normalize these categories:**

| Category | Expected entry point |
|----------|---------------------|
| `docs/architecture/` | `index.md` |
| `docs/product/` | `index.md` |
| `docs/kb/` | `index.md` |
| `docs/conventions/` | N/A — agents reference specific files, not an index |

For each category:

| Target state | Action |
|-------------|--------|
| `index.md` already exists | Skip — already conforms |
| Directory has a single `.md` file (not `index.md`) | Rename it to `index.md` |
| Directory has multiple `.md` files but no `index.md` | Create `index.md` that lists/links to the existing files |
| Directory missing or empty | Copy the kit placeholder |

**This step normalizes the target project to match the kit's entry point convention — agent references are never rewritten.**

### 3.8 Handle docs/ placeholders

For each remaining kit docs/ placeholder (`docs/conventions/README.md`, `docs/kb/glossary.md`, `docs/architecture/README.md`):

Check at the **file** level, not directory level:

| Target state | Action |
|-------------|--------|
| **That specific file** exists | Skip — target has real content for this file |
| Directory exists, **this file** missing | Copy the placeholder file (even if directory has other files) |
| Directory missing | Create directory and copy placeholder |

The check is per-file: `docs/kb/glossary.md` missing but `docs/kb/index.md` exists → copy `glossary.md`. Never skip a placeholder just because the parent directory has unrelated content.

### 3.9 Regenerate .claude/README.md

After all files are written, the README must reflect the merged state:

1. Use Glob to count actual files: `.claude/agents/*.md`, `.claude/rules/*.md`, `.claude/conventions/*.md`, `.claude/skills/*/SKILL.md`, `.claude/hooks/*`, `.claude/commands/*`
2. Read the current `.claude/README.md` (kit's copy or target's existing)
3. Find the skill count parenthetical (e.g., "(37 skills)") and update to the actual count
4. If the README has agent/role tables, verify they list all agents currently in `.claude/agents/`
5. If the README references specific skills by name, verify those skills exist in the merged `.claude/skills/`

### 3.10 Write kit manifest

Write `.claude/.kit-manifest.md` using `templates/kit-manifest.md` as the format. Record:
- Import date and kit metadata
- Every file imported (path + action taken)
- Every file archived (original path + archive path)
- User overrides from CHECKPOINT 1
- Files preserved (user-owned, not touched)

This file enables re-import diffing in future runs.

### 3.11 Clean up

Do NOT copy these kit artifacts to the target:
- `MANIFEST.md` — export artifact (consumed during discovery, not needed in target)
- `INSTALL.md` — setup guide (automated by this skill)
- `CLAUDE.md.template` — consumed during CLAUDE.md merge
- `.export-state/` — export debug state

**Note:** `settings.local.json` is never included in kits and is never touched by import. The target's `settings.local.json` (if any) remains untouched — it contains project-specific secrets and local overrides that are outside the kit's scope.

## Output

- All files written per approved plan
- Archives created for displaced files
- Kit manifest written for re-import tracking

## Gate

All approved actions executed. No files written outside the plan. Archive complete. Ready for Phase 4.
