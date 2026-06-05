---
name: boy-scout
description: Small adjacent improvements while touching a file — dead code, naming, duplication, complexity. Always produces a report. Loaded by developer and solo agents after modifying files.
---

# Boy Scout

"Leave the code better than you found it." Apply small, scoped improvements to files you're already modifying. Always report what you improved and why.

## When to Use

- After completing changes to a file during implementation
- When the user asks for targeted cleanup ("boy scout this area")
- After applying reviewer fixes (clean up adjacent issues in the same file)

## When NOT to Use

- You haven't modified the file in this session — don't go looking for work
- The file is outside your current task scope
- You're in the middle of debugging (finish the fix first, then boy scout)

## Quick-Scan Checklist

Look for these in the files you just modified. Check in order — stop when improvements are proportional to your task:

1. **Dead code** — unused variables, unreachable branches, commented-out code, unused imports/usings
2. **Misleading names** — variable/method names that don't match what they do. Apply naming rules from `docs/conventions/coding-conventions.md`.
3. **Obvious duplication** — but apply the semantic test first (see below)
4. **Magic numbers/strings** — unnamed literals that obscure meaning
5. **Unnecessary complexity** — nested ternaries, double negations, overly clever LINQ chains that a simpler loop would clarify
6. **Stale comments** — comments that describe what the code used to do, not what it does now

## The Semantic Test (critical)

Before extracting "duplicate" code, ask: **do these blocks share meaning or just structure?**

- **Same meaning** (same business concept, same reason to change) → extract. Example: two endpoints both computing effective SP the same way.
- **Same structure, different meaning** (coincidentally similar, different reasons to change) → leave it. Example: two validators that happen to check null the same way but for different domain rules.

Premature abstraction from structural similarity is worse than duplication. When in doubt, leave it duplicated.

## Scope Constraints

- **Same file or adjacent files you already modified** — never touch files you didn't open
- **Proportional to your task** — a bug fix gets 1-2 small cleanups, a feature step gets a few more
- **No new abstractions** — don't create base classes, shared helpers, or utility methods
- **No file moves** — don't reorganize folder structure as a boy scout improvement
- **Time-box: under 5 minutes** — if a cleanup takes longer, it's a separate task

## Project-Specific Guardrails

Read `docs/conventions/coding-conventions.md` § Boy Scout Guardrails for project-specific architectural boundaries (e.g., which code structures are contracts and must not be renamed, which boundaries must not be crossed). Skip this section if the project has no boy-scout guardrails defined.

## Report Format

After making improvements, report inline (not a separate file):

```
Boy Scout — {filename}:
- Removed unused `_logger` field (dead code)
- Renamed `x` → `sprintMembership` (misleading name)
- Replaced magic number `3` → `DefaultSpPerBug` constant (magic number)
```

For trivial improvements (1-2 items), a single line suffices:
```
Boy Scout — {filename}: removed unused using, renamed `tmp` → `activeSprints`
```

If nothing worth improving: say nothing. Don't report "no improvements found."

## What This Skill Does NOT Do

- Architectural improvements — use `improve-architecture` skill for that
- Cross-file refactoring — this is single-file or adjacent-file only
- Test cleanup — not in scope (tests have their own conventions)
- Convention enforcement — `docs/conventions/coding-conventions.md` defines conventions; this skill applies them opportunistically, not exhaustively
