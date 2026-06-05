# Export Manifest

**Generated:** {YYYY-MM-DD}
**Source project:** {project-path}
**Kit version:** export-cc-setup v{version}
**Fingerprint terms:** {comma-separated list of stack terms detected}

## Included Files

### Agents (.claude/agents/)

| File | Classification | Notes |
|------|---------------|-------|
| {filename} | portable | |

### Rules (.claude/rules/)

| File | Classification | Notes |
|------|---------------|-------|
| {filename} | portable | |

### Conventions (.claude/conventions/)

| File | Classification | Notes |
|------|---------------|-------|
| {filename} | portable | |

### Skills (.claude/skills/)

| Skill | Classification | Files included | Notes |
|-------|---------------|----------------|-------|
| {skill-name} | portable | SKILL.md + {subdirs} | |

### Hooks (.claude/hooks/)

| File | Classification | Notes |
|------|---------------|-------|
| {filename} | portable | |

### Other

| File | Classification | Notes |
|------|---------------|-------|
| .claude/README.md | portable | Updated skill count for portable-only |
| CLAUDE.md.template | generated | Template with {N} FILL markers |
| INSTALL.md | generated | Setup instructions |

## Excluded Files

| File | Reason | Stack terms found |
|------|--------|-------------------|
| {filename} | stack-specific: {reason} | {term1, term2, ...} |

## Leaked Files (if any were found and blocked)

| File | Match % | Lines matched | Remediation status |
|------|---------|--------------|-------------------|
| {filename} | {N}% | {N} | Fixed in source / Overridden as false positive |

## User Overrides

| File | Original classification | User override | Reason |
|------|----------------------|---------------|--------|
| {filename} | {original} | {override} | {user's reason if given} |

(Empty if no overrides were made at CHECKPOINT 1)

## Verification Summary

- Stack leak scan: {PASS/FAIL}
- Structural integrity: {PASS/FAIL} ({N}/{total} checks passed)
- Leaks found: {count}
- Cross-reference errors: {count}
- Warnings: {count}
