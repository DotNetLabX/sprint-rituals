# Kit Import Manifest

**Imported:** {YYYY-MM-DD}
**Kit version:** export-cc-setup v{version}
**Kit source:** {source project path}
**Kit generated:** {kit generation date}
**Import mode:** {Fresh import | Re-import}
**Checksum format:** first 12 hex characters of SHA-256

## Imported Files

### Agents

| File | Action | Checksum | Notes |
|------|--------|----------|-------|
| {filename} | copy | {sha256-short} | |

### Rules

| File | Action | Checksum | Notes |
|------|--------|----------|-------|
| {filename} | copy | {sha256-short} | |

### Conventions

| File | Action | Checksum | Notes |
|------|--------|----------|-------|
| {filename} | copy | {sha256-short} | |

### Skills

| Skill | Action | Files imported | Checksum (SKILL.md) | Notes |
|-------|--------|---------------|---------------------|-------|
| {skill-name} | copy | SKILL.md + {subdirs} | {sha256-short} | |

### Hooks

| File | Action | Checksum | Notes |
|------|--------|----------|-------|
| {filename} | copy | {sha256-short} | |

### Commands

| File | Action | Checksum | Notes |
|------|--------|----------|-------|
| {filename} | copy | {sha256-short} | |

### Merged Files

| File | Action | Notes |
|------|--------|-------|
| CLAUDE.md | merge | {N} sections added, {M} auto-filled, {K} user-filled |
| settings.json | merge | {N} keys added, {M} keys overridden |

### Other

| File | Action | Notes |
|------|--------|-------|
| .claude/README.md | copy / regenerated | Updated counts for merged state |
| docs/conventions/README.md | copy / skipped | |
| docs/kb/index.md | copy / skipped | |
| docs/kb/glossary.md | copy / skipped | |
| docs/architecture/README.md | copy / skipped | |

## Archived Files

| Original path | Archive path | Reason |
|---------------|-------------|--------|
| {path} | .claude/_archived/{YYYYMMDD}/{category}/{filename} | collision with kit file |

## Preserved Files (user-owned, not touched)

| File | Category | Notes |
|------|----------|-------|
| {path} | {category} | No collision — kept as-is |

## User Overrides

| File | Default action | User override | Reason |
|------|---------------|---------------|--------|
| {path} | {default} | {override} | {user's reason if given} |

(Empty if no overrides were made at CHECKPOINT 1)

## FILL Markers Resolved

| Section | Source | Content summary |
|---------|--------|----------------|
| ## Tech Stack | target CLAUDE.md / user input | {brief description} |
| ## Frontend | target CLAUDE.md / user input | {brief description} |
| ## Repo Structure | target CLAUDE.md / user input | {brief description} |
