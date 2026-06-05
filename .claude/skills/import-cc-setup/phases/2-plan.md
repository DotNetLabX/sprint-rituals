# Phase 2: Plan

Build the import action plan from the collision map and present it for user approval.

## Steps

### 2.1 Build action table

For each kit item, assign a default action based on its collision status:

| Status | Default action | User can override to |
|--------|---------------|---------------------|
| **new** | `copy` | `skip` |
| **collision** | `archive-and-copy` | `keep-target`, `skip` |
| **merge** | `merge` | `keep-target`, `use-kit-only` |
| **skip** | `skip` | `copy` (force overwrite) |

For re-import items:

| Re-import status | Default action |
|-----------------|---------------|
| **unchanged** | `skip` (already imported, hasn't changed) |
| **updated** | `archive-and-copy` (update to newer version) |
| **new** | `copy` |
| **removed** | `flag` (kit no longer includes this — ask user) |
| **user-modified** | `flag` (user changed a kit file — ask before overwriting) |
| **user-owned** | `preserve` (never touch) |

### 2.2 CLAUDE.md merge plan

If the target has an existing `CLAUDE.md`:

1. Read both the kit's `CLAUDE.md.template` and the target's `CLAUDE.md`
2. Parse `##`-level section headers from both
3. For each kit section:
   - **FILL marker section** (any `##` section containing `<!-- FILL:`): check if target has a matching `##` header using the alias table in `playbooks/claude-md-merge.md`. If yes, plan to auto-fill the marker with target's existing content. If no, plan to prompt the user.
   - **Pipeline section** (sections without FILL markers, e.g., Guardrails, Port Convention): check if target has a matching header. If yes and content differs, flag as conflict — present both versions at CHECKPOINT 1. If no, plan to add.
   - **Target sections not in kit:** preserve as-is (target-specific content)
4. If target has content that could conflict with kit's pipeline assumptions (e.g., contradictory guardrails, incompatible agent instructions), flag for user notification — announce what will be replaced before doing it.

See `playbooks/claude-md-merge.md` for the full merge protocol.

### 2.3 Settings.json merge plan

If both kit and target have `settings.json`:

1. Parse both as JSON
2. Classify each key by value shape (scalar, object, array of hooks, array of strings) — see Phase 3.5 for the merge rules per shape
3. For each key in kit:
   - Key doesn't exist in target -> plan to add
   - Key exists with same value -> skip
   - Key exists with different value -> plan to use kit value, note target value in report
3. For each key in target not in kit -> preserve
4. Present the merge diff at CHECKPOINT 1

### 2.4 Conventions merge plan

For `.claude/conventions/` files:

- No collision -> plan to copy kit convention
- Collision -> plan to archive target version, copy kit version

If a conventions index or map file exists in the target:
- Plan to comment out existing entries (preserved as examples)
- Add kit convention entries below the commented block

Non-colliding target conventions remain in place — they are not moved or archived.

### 2.5 Rules enumeration

Rules get special treatment — per-file decisions instead of blanket action:

1. List all target rules with one-line descriptions (read first line or frontmatter)
2. List all kit rules with one-line descriptions
3. Identify collisions (same filename)
4. For non-colliding target rules: default action is `preserve` (keep alongside kit rules)
5. For colliding rules: default action is `archive-and-copy`
6. Present the full rules list at CHECKPOINT 1 for per-file approval

### 2.6 CHECKPOINT 1

Present the import plan via `AskUserQuestion`. Display:

```
## Import Plan

**Kit:** {kit-path} (generated {date}, {N} files)
**Target:** {current project path}
**Mode:** {Fresh import | Re-import (prior: {date})}

### Actions Summary
- Copy: {N} files
- Merge: {N} files (CLAUDE.md, settings.json)
- Archive & replace: {N} files (collisions)
- Skip: {N} files (target has content / unchanged)
- Prompt: {N} items (FILL markers to complete)

### Collisions ({N})
| Target file | Kit file | Default action |
|-------------|----------|---------------|
| {path} | {path} | archive-and-copy |

### Rules — Per-File Plan
| Rule | Source | Action |
|------|--------|--------|
| {name} | target (no collision) | preserve |
| {name} | collision | archive-and-copy |

### CLAUDE.md Merge Preview
- Sections to add: {list}
- Sections auto-filled from target: {list}
- Sections requiring user input: {list}
- Conflicts (kit replaces target): {list with both-side summaries}

### Removed from Kit ({N})
{only shown on re-import — files the kit no longer includes}
| File | Previously imported | Default action |
|------|-------------------|---------------|
| {path} | {date of prior import} | archive |

### Files to Archive
{list of files that will move to .claude/_archived/}
```

**Options:**
1. **"Approve and apply"** — proceed with the plan as shown
2. **"Review collisions"** — show side-by-side content for each collision before deciding
3. **"Abort"** — cancel the import

If the user chooses "Review collisions," present each collision individually with file content summaries, collect per-file overrides, then re-present the updated plan.

## Output

- Approved import plan (action per file)
- CLAUDE.md merge plan (section-level actions)
- Settings.json merge plan (key-level actions)
- User overrides (if any)

## Gate

CHECKPOINT 1 passed: user approved the import plan. Ready for Phase 3.
