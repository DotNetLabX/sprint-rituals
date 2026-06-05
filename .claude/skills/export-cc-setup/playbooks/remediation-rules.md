# Remediation Rules Playbook

When Phase 2 detects stack leaks in pipeline files, this playbook tells the agent how to analyze each match and propose a fix **in the source project**. No content is auto-cleaned — the user applies the fixes and re-exports.

## Design Principle

The source project's pipeline layer (`.claude/agents/`, `.claude/rules/`, `.claude/conventions/`, `.claude/hooks/`, `.claude/README.md`) should be 0% stack-coupled at all times. Stack-specific details belong in convention files (`docs/conventions/`) and skill files (`.claude/skills/`). Pipeline files reference these by path, not by inlining the content.

This is the **indirection pattern**: pipeline files say "verify the build passes (see docs/conventions/project-rules.md)" instead of "run `dotnet build && dotnet test`."

## Remediation Target Map

For each type of leaked content, this is where it should move:

| Leak type | Target file | Target section |
|-----------|-------------|----------------|
| Build/test commands | `docs/conventions/project-rules.md` | `## Build Verification` |
| Framework-specific coding rules | `docs/conventions/coding-conventions.md` | Appropriate `##` section |
| Framework-specific anti-patterns | `docs/conventions/coding-conventions.md` | `## Anti-patterns` or similar |
| Boy scout guardrails | `docs/conventions/coding-conventions.md` | `## Boy Scout Guardrails` |
| Stack-specific file extensions | `docs/conventions/project-rules.md` | `## Source Files` |
| Stack-specific permission entries | `.claude/settings.local.json` | (move from `settings.json`) |
| Stack-specific env vars | `.claude/settings.local.json` | (move from `settings.json`) |
| Stack-specific skill references | Remove or generalize | (the skill is excluded; the reference shouldn't exist) |
| Framework name in behavioral prose | Generalize in-place | (drop the qualifier, keep the concept) |

## Remediation Patterns

### Build/test commands in agent files

**Before (leaked):**
```
Run `dotnet build` from solution root. All projects must compile with zero warnings.
Then run `dotnet test` to verify no regressions.
```

**After (remediated):**
```
Verify the build passes with zero warnings, then run the test suite.
See docs/conventions/project-rules.md § Build Verification for the commands.
```

**What moves to project-rules.md:**
```markdown
## Build Verification
Run `dotnet build` from solution root. All projects must compile with zero warnings.
Then run `dotnet test` to verify no regressions.
```

### Framework names in behavioral prose

**Before (leaked):**
```
FastEndpoints endpoint classes own feature slices directly.
```

**After (remediated):**
```
Endpoint classes own feature slices directly.
```

No content moves — the framework qualifier is just dropped. The concept (endpoints owning feature slices) is stack-independent.

### Stack-specific skill references

**Before (leaked):**
```
Use the `create-feature` skill to scaffold a FastEndpoints endpoint.
```

**After (remediated — option A, generalize):**
```
Use the feature scaffolding skill to create a new endpoint.
```

**After (remediated — option B, remove):**
Remove the line entirely if the surrounding text works without it. Skill references in agent files are informational, not load-bearing — the architect's plan maps steps to skills.

### Stack-specific permissions in settings.json

**Before (leaked):**
```json
{
  "permissions": {
    "allow": ["Read *", "Bash(dotnet build)", "Bash(npm run dev)"]
  }
}
```

**After (remediated):**
In `settings.json` (tracked, portable):
```json
{
  "permissions": {
    "allow": ["Read *"]
  }
}
```

In `settings.local.json` (gitignored, project-specific):
```json
{
  "permissions": {
    "allow": ["Bash(dotnet build)", "Bash(npm run dev)"]
  }
}
```

### Stack-specific file extensions

**Before (leaked):**
```
Source files are `.cs` files under `src/`. Never modify `.csproj` files without updating the reference graph.
```

**After (remediated):**
```
See docs/conventions/project-rules.md for source file definitions and project reference rules.
```

**What moves to project-rules.md:**
```markdown
## Source Files
Source files are `.cs` files under `src/`. Never modify `.csproj` files without updating the reference graph.
```

## Remediation Report Format

Phase 2 generates this report for each leaked file:

```
LEAKED: {file-path} ({match_%}% — {N} lines)

  Line {N}: "{matched line text}"
    Type: {leak type from target map}
    Move to: {target file} § {section}
    Source becomes: "{generic replacement text}"

  Line {N}: "{matched line text}"
    Type: framework name in prose
    Fix: drop "{term}", keep the rest
    Source becomes: "{line without the framework name}"
```

## When No Convention File Exists

If the remediation target file doesn't exist yet in the source project (e.g., `docs/conventions/project-rules.md`), the remediation report should note: "Create `{file}` with the content below" and provide the initial content. This is a one-time setup cost that permanently solves the indirection.

## When the User Disagrees

If the user says a match is a false positive (e.g., "Vue" used as a proper noun unrelated to the framework), they override the classification to portable at CHECKPOINT 1. The remediation report is a proposal, not a mandate.
