# Phase 3: Package

Assemble the portable kit in the output directory.

## Pre-checks

1. If any files are still classified as **leaked**, refuse to proceed: "Export blocked — {N} leaked files remain. Fix them in the source project and re-run Phase 2."
2. If the output directory already exists, ask the user: "Output directory `{path}` already exists. Overwrite?" Never silently overwrite.

## Steps

### 3.1 Create directory structure

```
{output-path}/
  .claude/
    agents/
    rules/
    conventions/
    skills/
    hooks/
    commands/           <-- only if source has .claude/commands/
  docs/
    conventions/
    kb/
    architecture/
```

### 3.2 Copy portable files

For each file classified as **portable**:
- Copy to the same relative path under `{output-path}/`

For portable **skills**:
- Copy the entire skill directory: `SKILL.md` + all subdirectories (phases/, playbooks/, templates/, references/, workflows/, CHANGELOG.md)
- Use `Glob` to discover all files under each portable skill's directory
- Preserve the directory structure exactly

### 3.3 Verify zero leaked files

This is a hard gate — not a step that does work. If Phase 2's classification table contains any files with `classification: leaked` that were not overridden, refuse to proceed. This should never happen if Phase 2's checkpoint worked correctly, but verify defensively.

### 3.4 Generate template CLAUDE.md

Follow `playbooks/template-generation.md`:

1. Read the source project's `CLAUDE.md`.
2. For each `##` section, classify and transform:
   - **Keep as-is:** Guardrails, generic conventions, workflow mechanics (all scan at 0% — verified in Phase 2)
   - **Templatize:** Tech Stack, Frontend, Repo Structure -> replace body with FILL markers that include guidance and examples from a different stack
   - **Remove:** Project-specific integration sections with no template value (e.g., graphify config, specific tool references)
3. Add a header comment with generation metadata.
4. Write to `{output-path}/CLAUDE.md.template`.

### 3.5 Generate INSTALL.md

**Generate dynamically** using `templates/INSTALL.md` as a structural guide — do not copy the template verbatim. Customize:
- List the actual FILL markers present in this kit's CLAUDE.md.template
- List the actual portable skills included (by name and description)
- **Generate the "Stack-specific skills" checklist dynamically** from the excluded skills: group by purpose (feature scaffolding, persistence, error handling, domain modeling, frontend review, etc.) and list as categories to create, not hardcoded skill names
- Note any settings.json entries that need filling

Write to `{output-path}/INSTALL.md`.

### 3.6 Generate MANIFEST.md

Write the export manifest using `templates/manifest.md` as the template. Include:
- Every included file with classification and notes
- Every excluded file with reason and stack terms found
- Any leaked files that were found (with remediation status)
- Fingerprint terms used
- User overrides from CHECKPOINT 1 (if any)
- Generation date and source project path

Write to `{output-path}/MANIFEST.md`.

### 3.7 Generate placeholder files

Create minimal starter files for the recipient project:

**`docs/conventions/README.md`:**
```
# Stack Conventions

Add your project-specific coding standards and conventions here.

Required files:
- `project-rules.md` — build rules, file structure rules, project reference graph
- `coding-conventions.md` — naming, formatting, and style conventions for your stack
```

**`docs/kb/index.md`:**
```
# Knowledge Base — Topic Map

Add entries as your project's domain knowledge grows.
See `.claude/rules/kb-maintenance.md` for the maintenance protocol.
```

**`docs/kb/glossary.md`:**
```
# Glossary

| Term | Definition | Avoid |
|------|-----------|-------|
```

**`docs/architecture/README.md`:**
```
# Architecture

Create your architecture document here.
Use the `create-architecture-doc` skill to generate it from your codebase.
```

### 3.8 Update .claude/README.md

In the output copy of `.claude/README.md`:
- Update the skill count parenthetical to reflect only portable skills included
- Remove references to stack-specific skills in any lists
- Replace stack-specific mentions in examples with generic equivalents
- Update the "Getting Started" section if it references stack-specific commands

### 3.9 Clean up state directory

Move `{output-path}/_state/` to `{output-path}/.export-state/` and add it to `.gitignore` in the output. The state files are useful for debugging re-exports but should not be committed by the kit recipient.

## Output

- Complete kit in `{output-path}/`
- All files written and ready for Phase 4 verification

## Gate

Output directory fully assembled. Every classified file has been processed (copied or excluded). MANIFEST.md written. Ready for Phase 4.
