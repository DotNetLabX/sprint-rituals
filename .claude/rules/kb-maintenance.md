# Knowledge Base Maintenance

The business knowledge base at `docs/kb/` captures domain rules, computation logic, and data flows that agents need to navigate the codebase efficiently.

## When to Update

`docs/kb/index.md` is the topic map — it lists which KB entries exist and what each one covers. After implementing a feature that modifies any concept tracked there, update the corresponding KB entry. If the feature introduces a new tracked concept, add a new entry and register it in the topic map.

## Who Updates

- **Developer:** Updates KB entries as part of implementation. Include in implementation.md under "Files Modified."
- **Reviewer:** Verifies KB entries match the implementation. Flag violations per the Lint Checks table below.

## What to Capture

- Business rules (the "why" that code doesn't explain)
- Computation formulas (exact logic, not just "calculates X")
- Edge cases (division by zero, null handling, boundary conditions)
- Key file paths (so agents know where to look)
- Relationships between concepts

## What NOT to Capture

- Full property lists (agents read the class file for that)
- Response DTO shapes (agents read the endpoint for that)
- UI layout details (agents read the UI component source for that)
- Anything derivable from reading the code itself

## Hard Rules

1. **Never delete a KB entry.** If a concept is deprecated, add `## Status: Deprecated` at the top and note why. The historical record stays.
2. **Never remove sections without replacing them.** Update and expand — don't truncate. A section with fewer lines than before is a warning sign.
3. **Always update `docs/kb/index.md`** when adding or renaming an entry. The index is the topic map — orphaned entries are invisible to agents.
4. **Use Edit tool for KB updates, not Write.** Carry forward all untouched sections exactly. Write rewrites the whole file and risks losing content.

## Lint Checks

Run these checks when reviewing KB-impacted features:

| Check | Severity | Condition |
|-------|----------|-----------|
| broken-index-link | error | `docs/kb/index.md` links to a file that doesn't exist |
| orphan-entry | warning | KB file under `docs/kb/` not linked from `docs/kb/index.md` |
| stale-key-file | warning | A "Key file:" path in a KB entry doesn't exist in the codebase |
| duplicate-term | warning | Same concept defined in `docs/kb/glossary.md` and a domain entry with conflicting definitions |
| empty-section | info | Section header present but no content beneath it |

## Consumers

| Agent | What they read | Impact of stale data |
|-------|---------------|---------------------|
| Architect | Business rules, formulas | Plan steps miss edge cases; wrong approach chosen |
| Developer | Key file paths | Wrong files modified; time lost on exploration |
| Reviewer | Business rules | Bugs missed in review; incorrect correctness judgments |

## KB Entry Schema

All KB entries follow the standard section order. See `docs/conventions/kb-entry-schema.md`.

## On-Demand Update

When the user asks to update the KB for a concept (e.g., "update KB for Xray"):

1. Read the relevant source files (entities, services, repositories for that concept)
2. Extract: business rules, computation formulas, edge cases, key file paths, relationships
3. Write or update `docs/kb/{area}/{concept}.md` following the KB Entry Schema
4. Update `docs/kb/index.md` if it's a new entry (add a link under the appropriate section)
5. Apply the "What NOT to Capture" rules — keep it to ~60 lines of what's non-obvious

Any agent can perform this (solo, developer, or the main session directly). No pipeline or plan required.
