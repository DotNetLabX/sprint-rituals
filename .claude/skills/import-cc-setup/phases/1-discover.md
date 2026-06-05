# Phase 1: Discover

Validate the kit and inventory both the kit contents and the target project's existing setup.

## Steps

### 1.1 Validate kit

Verify the kit directory exists and contains required files:
- `MANIFEST.md` — **hard required** (proves this is a valid export)
- `.claude/` directory — **hard required** (the actual setup content)
- `CLAUDE.md.template` — **soft required** (warn if missing, proceed without CLAUDE.md merge)
- `INSTALL.md` — **soft required** (reference only, not executed — warn if missing)

If any hard-required file is missing, stop: "Invalid kit at `{path}`. Missing: {list}. Ensure this was produced by `export-cc-setup`."

If soft-required files are missing, warn: "Kit at `{path}` is missing: {list}. Proceeding without CLAUDE.md merge / install reference." Continue to Phase 1.2.

### 1.2 Parse kit MANIFEST

Read `{kit-path}/MANIFEST.md`. Extract:
- **Kit metadata:** generation date, source project, kit version, fingerprint terms
- **Included files:** file paths, classifications, notes (from each subsection table)
- **Excluded files:** what was left out and why (for user reference at CHECKPOINT 1)
- **User overrides:** any classification overrides from the export (for transparency)

Build the kit inventory:
```yaml
kit:
  metadata:
    generated: {date}
    source: {project}
    version: {version}
    fingerprint: [{terms}]
  files:
    - path: {relative path}
      category: agent | rule | convention | skill | hook | command | settings | readme | template | docs
      classification: portable | generated | stack-specific | leaked
      notes: {from MANIFEST}
  skills:
    - name: {skill-name}
      files: [{list of files in skill directory}]
```

Only `portable` and `generated` files should appear in the kit's included files. If any row in the MANIFEST carries `stack-specific` or `leaked` classification under "Included Files," warn: "Kit contains files classified as {classification} — this may indicate a malformed export. Verify the kit was produced by a clean `export-cc-setup` run."

### 1.3 Inventory kit files on disk

Walk the kit directory to verify MANIFEST accuracy. Use Glob to discover:
- `.claude/agents/*.md`
- `.claude/rules/*.md`
- `.claude/conventions/*.md`
- `.claude/skills/*/` (each skill directory, all files within)
- `.claude/hooks/*`
- `.claude/commands/*` (if directory exists)
- `.claude/settings.json`
- `.claude/README.md`
- `CLAUDE.md.template`
- `docs/**/*`

Flag any files present on disk but missing from MANIFEST (or vice versa). Disk is the source of truth — MANIFEST discrepancies are warnings, not blockers.

### 1.4 Inventory target project

Scan the current project for existing `.claude/` setup:

| Location | What to find |
|----------|-------------|
| `.claude/agents/*.md` | Existing agent files |
| `.claude/rules/*.md` | Existing rule files |
| `.claude/conventions/*.md` | Existing convention files |
| `.claude/skills/*/SKILL.md` | Existing skill directories |
| `.claude/hooks/*` | Existing hook scripts |
| `.claude/commands/*` | Existing custom commands |
| `.claude/settings.json` | Existing settings (read top-level keys) |
| `.claude/README.md` | Existing README |
| `CLAUDE.md` | Existing project instructions (read `##` section headers) |
| `docs/conventions/` | Existing stack conventions (list files) |
| `docs/kb/` | Existing knowledge base (list files) |
| `docs/architecture/` | Existing architecture docs (list files) |

Also check:
- `.claude/_archived/` — prior archives exist? (indicates previous import)
- `.claude/.kit-manifest.md` — prior import manifest? (enables re-import diff)

If the target has no `.claude/` directory at all, classify as **greenfield** — the entire import is a simple copy with FILL marker prompts.

### 1.5 Detect collisions

Compare kit inventory against target inventory. For each kit file, classify:

| Status | Meaning |
|--------|---------|
| **new** | Kit file has no counterpart in target — simple copy |
| **collision** | Target has a file at the same path — needs resolution |
| **merge** | File requires content-level merge (CLAUDE.md, settings.json, conventions index) |
| **skip** | Target already has richer content (docs/ placeholders) |

For skills, compare by directory name (not individual file paths within the skill).

For rules, list each target rule individually — rules get per-file decisions at CHECKPOINT 1, not blanket treatment.

### 1.6 Check for re-import

If `.claude/.kit-manifest.md` exists:
1. Parse it to identify files from the prior import (paths, actions, checksums)
2. For each kit-originated file still in the target, compare its current content checksum against the stored checksum. If they differ, the user modified it since import -> classify as `user-modified`
3. Diff current kit MANIFEST against prior kit-manifest
4. Classify each kit file additionally:

| Re-import status | Meaning |
|-----------------|---------|
| **unchanged** | Same as prior import — skip |
| **updated** | Changed since prior import — archive old, copy new |
| **new** | Not in prior import — treat as fresh |
| **removed** | Was in prior import, not in current kit — flag for user |
| **user-modified** | Kit-originated file was modified by user since import — flag for user decision |

Files the user added since the prior import (in target but not in any kit-manifest) are tagged **user-owned** — never touched.

## Output

- Kit inventory (parsed from MANIFEST + verified on disk)
- Target inventory
- Collision map with status per file
- Re-import diff (if applicable)
- Greenfield flag (if target has no `.claude/`)

## Gate

Kit validated. Both inventories complete. Collision map built. Ready for Phase 2 planning.
