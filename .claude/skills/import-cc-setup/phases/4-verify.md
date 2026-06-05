# Phase 4: Verify

Validate the imported setup is internally consistent and all cross-references resolve.

## Steps

### 4.1 Cross-reference checks

| Check | Method | Severity |
|-------|--------|----------|
| Agent -> skill references | Grep agent files for skill names and "use the `{name}` skill" patterns. Verify each referenced skill directory exists in `.claude/skills/` | error |
| Agent -> internal references | Grep agent files for `@.claude/...` path references. Verify target files exist within `.claude/` | error |
| Agent -> project references | Grep agent files for `@docs/...` path references. Report as items the user must create in their project | info |
| Agent -> rule references | Grep agent files for `.claude/rules/` paths. Verify they exist | error |
| Convention cross-references | Grep convention files for references to other conventions or rules. Verify targets exist within `.claude/` | error |
| Convention -> project references | Grep convention files for `docs/` path references. Report as items the user must create | info |
| Settings.json hook references | If settings.json exists, verify every hook script path it references exists in `.claude/hooks/` | error |
| README accuracy | Parse skill/agent counts in `.claude/README.md` and compare to actual files on disk | warning |
| No empty files | Check all imported files have content (> 0 bytes) | error |
| Kit manifest completeness | Verify every imported file appears in `.kit-manifest.md` | warning |
| CLAUDE.md FILL markers | Verify all `<!-- FILL:` markers were resolved (none remaining in CLAUDE.md) | warning |
| Archive integrity | Verify every file listed as archived exists at its archive path | warning |

### 4.2 Dangling reference scan

Grep all files under `.claude/` for path references (patterns: `@path`, `./path`, `.claude/path`, `docs/path`). For each reference found, verify the target file exists in the project.

Classify each dangling reference:
- **Internal (`.claude/...`):** The target should exist — it's part of the kit. Report as error.
- **Project (`docs/...`, `@docs/...`):** The target is a project file the user creates. Report as info with a "Files to create" checklist.

**Never resolve a dangling reference by modifying the file that contains it.** The reference is correct — the target file is what's missing. The user creates the target, not the other way around.

Dangling references indicate:
- A kit file references something that was excluded from the kit (error — investigate)
- A target file references something that was archived during import (error — restore from archive)
- A kit file references a project file the user hasn't created yet (info — expected)

### 4.3 CHECKPOINT 2

Present the verification report:

```
## Import Verification Report

**Date:** {YYYY-MM-DD}
**Kit:** {kit-path} (version {version})
**Target:** {project path}
**Mode:** {Fresh import | Re-import}

### Actions Completed
- Files copied: {N}
- Files merged: {N}
- Files archived: {N}
- Files skipped: {N}
- Files preserved (user-owned): {N}
- FILL markers resolved: {N}/{total}

### Cross-Reference Checks
- Checks passed: {N}/{total}
- Errors:
  {list each error with file path and broken reference}
- Warnings:
  {list each warning}

### Archived Files
| Original | Archive location |
|----------|-----------------|
| {path} | {archive-path} |

### Verdict: {PASS | FAIL}
{one-line summary}
```

**If PASS (zero errors):**
Report success: "Setup imported from `{kit-path}`. {N} files imported, {M} archived, {K} skipped. Kit manifest saved to `.claude/.kit-manifest.md`."

If any FILL markers remain unresolved, remind: "Fill these CLAUDE.md sections before using the pipeline: {list}."

If warnings exist, list them — they don't block but should be addressed.

**If FAIL — cross-reference errors:**
List each error with the specific broken reference. These must be fixed:
- Missing skill reference -> the skill wasn't in the kit or was skipped. Options: import the skill manually, or create a stub skill for the user's stack.
- Missing internal `.claude/` reference -> the referenced file should have been in the kit. Re-copy from the kit or investigate why it's missing.
- Apply fixes and re-run Phase 4 verification checks only (no full re-import).

**Info items — project references:**
List `@docs/...` references from agent files as a checklist of files the user needs to create in their project. These are NOT errors — they are the kit's expected project structure. Present as:
```
Files to create in your project:
- [ ] docs/architecture/index.md — referenced by architect, developer, solo, reviewer agents
- [ ] docs/conventions/project-rules.md — referenced by architect, developer, solo, reviewer agents
- [ ] docs/conventions/coding-conventions.md — referenced by developer, solo, reviewer agents
...
```

**Hard rule: Never modify agent file content to resolve references.** Agent files define the kit's conventions. If a referenced file doesn't exist, the user creates the file — the reference is never rewritten. This applies to `@` paths, skill names, convention paths, and any other references within agent files.

**If FAIL — other errors:**
List errors with remediation suggestions. Offer to fix automatically where possible (e.g., empty files can be re-copied from the kit).

## Output

- Verification report (displayed to user)
- Verified imported setup (if PASS)
- Corrective action loop (if FAIL, max 2 iterations before escalating to user)

## Gate

CHECKPOINT 2 passed: either clean pass or user accepted remaining warnings. Setup is imported and ready for use.
