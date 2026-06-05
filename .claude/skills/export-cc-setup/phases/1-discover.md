# Phase 1: Discover

Extract the stack fingerprint from the current project and build the complete file inventory.

## Steps

### 1.1 Read CLAUDE.md stack sections

Read the project root `CLAUDE.md`. Extract the full text from:
- `## Tech Stack` section (primary)
- `## Frontend` section (if present)
- Any other section that explicitly lists technologies

If neither section exists, warn the user: "No Tech Stack or Frontend section found in CLAUDE.md. The fingerprint will be empty and all files will be classified as portable." Proceed only after user confirms via `AskUserQuestion`.

### 1.2 Extract fingerprint terms

From the stack section text, extract every named technology:
- **Languages:** .NET, C#, Python, Go, TypeScript, JavaScript, Rust, Java, etc.
- **Frameworks:** ASP.NET Core, FastEndpoints, Django, Flask, Gin, Vue, React, Angular, etc.
- **Libraries:** EF Core, Mapster, MassTransit, Pinia, Tailwind, FluentValidation, etc.
- **Infrastructure:** gRPC, SignalR, RabbitMQ, Redis, SQLite, PostgreSQL, Docker, etc.
- **File formats:** csproj, slnx, package.json, go.mod, Cargo.toml, etc.

For each term, expand with known aliases. See `playbooks/stack-detection.md` for the alias table and expansion rules.

### 1.3 Build regex patterns

Convert each term + aliases into a case-insensitive regex pattern:
- Escape regex special characters (`.`, `+`, `#`)
- Multi-word terms: allow flexible whitespace (`EF\s+Core`)
- Short ambiguous terms (e.g., "Go", "Vue"): require word boundaries + nearby tech context
- Compound terms: match as a single unit

### 1.4 Inventory files

Use `Glob` to list all files under:
- `.claude/agents/` — agent definitions
- `.claude/rules/` — behavioral rules
- `.claude/conventions/` — artifact formats
- `.claude/skills/*/SKILL.md` — skill entry points (Phase 2 reads content)
- `.claude/hooks/` — infrastructure hooks
- `.claude/commands/` — custom slash commands (if directory exists)
- `.claude/settings.json` — project-level settings (hook definitions, permissions)
- `.claude/README.md` — system overview
- `docs/conventions/` — stack-specific coding standards (for exclusion check)

For each file, record:
- **Path** (relative to project root)
- **Category:** agent | rule | convention | skill | hook | command | settings | readme | docs-convention
- **Size** (line count)
- **Binary:** true/false (images, diagrams — skip fingerprint scanning for these, copy as-is)

Note: `.claude/settings.local.json` is always excluded (project-specific secrets and permissions). Do not inventory it.

For skills specifically, also read the `description:` field from SKILL.md frontmatter — this is the primary signal for skill classification in Phase 2.

### 1.5 Persist state

Write state to `{output-path}/_state/phase-1.yaml` (created if needed). This enables resumability if context is lost between phases.

```yaml
skill: export-cc-setup
version: 1.0.0
phase: 1
status: complete
timestamp: {ISO-8601}

fingerprint_terms:
  - name: "{term}"
    aliases: ["{alias1}", "{alias2}"]
    pattern: "{regex}"
    category: "{language|framework|library|infrastructure|format}"

file_inventory:
  - path: "{relative-path}"
    category: "{agent|rule|convention|skill|hook|command|settings|readme|docs-convention}"
    size: {lines}
    binary: {true|false}

skill_descriptions:
  "{skill-name}": "{description text}"
```

On resume: if `phase-1.yaml` exists and is `status: complete`, skip Phase 1 and load state directly.

## Output

- Stack fingerprint (terms + compiled patterns)
- Complete file inventory with category hints
- Skill description map
- State persisted to disk

## Gate

Fingerprint extracted (may be empty with user approval). Inventory complete. State persisted. Ready for Phase 2.
