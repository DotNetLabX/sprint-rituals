# Knowledge Base Index

Verified business-rule records for the Fokus domain. Each entry is a single KB file
under `docs/kb/` with sections defined in `docs/kb/_schema/entry-template.md`.

## Status legend

| Status | Meaning |
|--------|---------|
| placeholder | Structure in place; rules not yet verified against source |
| verified | Rules confirmed against original Fokus specs/plans |
| mutation-gated | Stryker has killed at least one mutant for each rule |

## Entries

| File | Concept | Status |
|------|---------|--------|
| [health-score.md](health-score.md) | HealthScore interpolation and RAG classification | mutation-gated |
| [bug-ratio.md](bug-ratio.md) | Bug-ratio metrics, multi-sprint aggregation, alert streak | mutation-gated |
| [jira-story-points-write-back.md](jira-story-points-write-back.md) | Jira issue-edit write path, customfield_10016, 204 guard, write-first sequencing | verified |
| [poker-sprint-tickets-freshness.md](poker-sprint-tickets-freshness.md) | GetSprintTickets live pass-through contract, no-cache rule, Free-mode guard | verified |

## Conventions

- One file per concept. Split when an entry exceeds ~80 lines.
- File names: `kebab-case.md`, matching the concept name.
- All paths in `## Key Files` are relative to the repo root.
- Step 3 (Cover) reads this index to enumerate which rules to cover.
  Add a row here before adding the KB file — the index is the authoritative list.
- Do not add an entry until the rules are at minimum verified (confirmed against specs).
  Placeholder entries are allowed only during Step 2b extraction.
