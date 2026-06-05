# KB Entry Schema

Standard section order for all `docs/kb/` entries. Use these headings exactly — agents and lint checks rely on consistent naming.

## Required Sections

```markdown
# {Concept Name}

## Rules
Business rules that govern this concept — the "why" that code doesn't explain.
One rule per bullet. Each rule should be falsifiable (you could write a test for it).

## Key Files
- `path/to/file` — what it contains relevant to this concept

## Edge Cases
- {condition}: {what happens and why}

## Relationships
- {ConceptA} → {ConceptB}: {nature of relationship}
```

## Optional Sections

```markdown
## Status
Deprecated | Active (omit section if Active — only needed when deprecated)
Reason: {why deprecated, what replaced it}

## Source
Which spec section, domain rule, or external system defines this concept.
```

## Rules

- Section order must match the schema above. Agents scan by heading name.
- `## Rules` captures business invariants. `## Edge Cases` captures boundary conditions. Don't mix them.
- `## Key Files` paths must be relative to the repo root (e.g., `src/Services/Fokus/Fokus.Domain/Aggregates/Sprint.cs`).
- Keep entries to ~60 lines total. If an entry grows beyond 80 lines, split into sub-entries.
- When deprecating: add `## Status: Deprecated` at the top, keep all other sections intact.

## Consumers

| Agent | Uses | Impact |
|-------|------|--------|
| Architect | Rules, Relationships | Plans without KB context miss invariants |
| Developer | Key Files, Edge Cases | Faster file discovery, correct boundary handling |
| Reviewer | Rules, Edge Cases | Verifies implementation enforces documented rules |
