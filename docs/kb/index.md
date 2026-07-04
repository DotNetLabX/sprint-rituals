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
| [cycle-time.md](cycle-time.md) | Cycle-time analytics, per-sprint and aggregate breakdowns, CycleTimeAnalyzer | mutation-gated |

## Canonical rule registries (SDD merge — spec arm + code arm)

| File | Class | Contents |
|------|-------|----------|
| [golden/BugRatioAnalyzer.md](golden/BugRatioAnalyzer.md) | BugRatioAnalyzer | 58 canonical rules across both arms (five-bucket triage, 2 divergences pending verdict); crosswalk + run note alongside |
| [golden/CycleTimeAnalyzer.md](golden/CycleTimeAnalyzer.md) | CycleTimeAnalyzer | 110 canonical rules (29 overlap incl. 5 Generate closures, 46 other-layer, 35 code-only, 0 divergent; spec-repair resolved 2026-07-04 — SR-53/SR-66 repaired KEEP CODE, SR-9 open); crosswalk + run note alongside |
| [golden/HealthScoreCalculator.md](golden/HealthScoreCalculator.md) | HealthScoreCalculator | 58 canonical rules (2 overlap incl. SR-16 probe closure, 50 other-layer of which 29 domain-summary parked for a future SprintSummaryAnalyzer merge, 6 code-only, 0 divergent; spec-repair resolved 2026-07-04 — 5 pins KEEP CODE, SR-19 open); crosswalk + run note alongside |

## Conventions

- One file per concept. Split when an entry exceeds ~80 lines.
- File names: `kebab-case.md`, matching the concept name.
- All paths in `## Key Files` are relative to the repo root.
- Step 3 (Cover) reads this index to enumerate which rules to cover.
  Add a row here before adding the KB file — the index is the authoritative list.
- Do not add an entry until the rules are at minimum verified (confirmed against specs).
  Placeholder entries are allowed only during Step 2b extraction.
