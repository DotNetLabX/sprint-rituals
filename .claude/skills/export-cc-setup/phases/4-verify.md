# Phase 4: Verify

Validate the exported kit is truly stack-independent and structurally sound.

## Steps

### 4.1 Stack leak scan

Re-scan every file in `{output-path}/` against the full fingerprint pattern list from Phase 1.

For each match found, classify:
- **Leak:** A stack term that should not be present. Example: "FastEndpoints" still appearing in an agent file. This should have been caught in Phase 2 — if found here, it's a pipeline bug.
- **False positive:** A generic word that matches a fingerprint term. Example: "Go" used as a verb, "Vue" in a French word. Determine by reading the surrounding context.
- **Marker content:** Term appears inside a `<!-- FILL: ... -->` marker. These are expected — skip them.

Record leaks and false positives separately.

### 4.2 Structural integrity checks

| Check | Method | Severity |
|-------|--------|----------|
| Agent -> skill references | Grep agent files for skill names mentioned in Follow/Build dispositions or "use the `{name}` skill" patterns. Verify each referenced skill directory exists in `{output-path}/.claude/skills/` | error |
| Agent -> convention references | Grep agent files for `@` path references (e.g., `@docs/conventions/...`, `@.claude/conventions/...`). Verify target files exist in output | error |
| Agent -> rule references | Grep agent files for `.claude/rules/` paths. Verify they exist in output | error |
| Convention cross-references | Grep convention files for references to other conventions or rule files. Verify targets exist | error |
| Settings.json hook references | If settings.json is included, verify every hook script path it references exists in `{output-path}/.claude/hooks/` | error |
| README skill count | Parse the skill count in `.claude/README.md` and compare to actual skill directories in output | warning |
| README agent count | Parse the agent count and compare to actual agent files | warning |
| INSTALL.md completeness | Verify every `<!-- FILL: ... -->` marker in CLAUDE.md.template is mentioned in INSTALL.md | warning |
| No empty files | Check all output files have content (> 0 bytes) | error |
| Template has FILL markers | Verify CLAUDE.md.template contains at least one FILL marker for Tech Stack | error |
| MANIFEST completeness | Verify every file from Phase 2 inventory appears in MANIFEST.md (either included or excluded) | warning |
| Skill directory completeness | For each included skill, verify the output has the same file set as the source | warning |

### 4.3 Build verification report

Format:

```
## Export Verification Report

**Date:** {YYYY-MM-DD}
**Source:** {project path}
**Output:** {output path}
**Fingerprint:** {N} terms ({comma-separated list})

### Stack Leak Scan
- Files scanned: {N}
- Leaks found: {N}
  {list each: file:line — matched term — context}
- False positives: {N}
  {list each: file:line — matched term — why false positive}

### Structural Integrity
- Checks passed: {N}/{total}
- Errors:
  {list each error}
- Warnings:
  {list each warning}

### Verdict: {PASS | FAIL}
{one-line summary}
```

### 4.4 CHECKPOINT 2

Present the verification report to the user.

**If PASS (zero leaks, zero errors):**
Report the success. Summarize: "Kit exported to `{output-path}/`. {N} files included, {M} excluded. Copy `.claude/` to your new project and rename `CLAUDE.md.template` to `CLAUDE.md`."

**If FAIL — leaks found:**
Present each leak. Offer via `AskUserQuestion`:
1. **"Fix in source and re-run"** — go back to Phase 2 with the source project fixed
2. **"Override as false positives"** — user asserts the matches are harmless; note in MANIFEST.md
3. **"Abort"** — delete the output directory

**If FAIL — structural errors:**
List each error with the specific broken reference. These must be fixed:
- Missing skill reference -> either include the skill (re-classify as portable) or fix the reference in the source project
- Missing convention/rule reference -> include the file or remove the reference
- Apply fixes and re-run Phase 4 verification checks only (no full re-package)

## Output

- Verification report (displayed to user)
- Verified kit (if PASS)
- Corrective action loop (if FAIL, max 2 iterations before escalating to user)

## Gate

CHECKPOINT 2 passed: either clean pass or user accepted exceptions. Kit is ready for use.
