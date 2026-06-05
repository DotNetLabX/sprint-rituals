---
name: improve-architecture
description: Proactive discovery of architectural improvement opportunities — coupling hotspots, anemic aggregates, leaky boundaries. Uses graphify and KB data. Outputs depend on invoker (solo = implement, architect = backlog).
---

# Improve Architecture

Find and propose "deepening opportunities" — refactors that improve domain boundaries, reduce coupling, and consolidate scattered logic. Discovery-first, not catalog-driven.

## When to Use

- User asks to improve or clean up an area of the codebase
- Solo agent encounters structural issues during a fix
- Architect runs a periodic architecture health check
- After a complex feature ships and the team suspects accumulated debt

## When NOT to Use

- Mid-feature implementation — finish the feature first, then assess
- For cosmetic cleanup (naming, dead code) — use `boy-scout` skill instead
- When the user has a specific refactoring in mind — just do it, no discovery needed

## Discovery Phase

Use structured data before reading source files:

### 1. Graphify God Nodes

Read `graphify-out/GRAPH_REPORT.md`. God nodes (most-connected types) are coupling hotspots. Ask:
- Is this type a god node because it's genuinely central (aggregate root, shared value object)?
- Or is it a god node because too many things depend on it that shouldn't?

### 2. KB Business Rules

Read `docs/kb/index.md` and relevant entries. Complex business rules with many edge cases suggest the logic may be scattered across multiple handlers instead of consolidated in the domain.

### 3. Service CLAUDE.md

Read the service's CLAUDE.md for known pain points, architectural decisions, and variant axes.

### 4. Targeted Source Reading

After the above, read specific source files to confirm or deny hypotheses. Don't do an "organic walk" — go where the data points.

## What to Look For

### Domain Health

- **Anemic aggregates** — aggregate has mostly getters/setters, business logic lives in handlers or services. Fix: move logic into aggregate methods.
- **Leaked domain rules** — validation or business rules in endpoints/handlers that belong in the domain. Fix: encapsulate in aggregate or value object.
- **Missing value objects** — primitive obsession where a concept deserves its own type (e.g., a `string` that's always a Jira issue key).
- **Fat handlers** — `HandleAsync` doing load + validate + mutate + persist + side effects. Fix: decompose or move logic to domain.

### Structural Health

- **Vertical slice violations** — shared code between features that should be independent. Ask: do these features change for the same reason?
- **Circular dependencies** — feature A references feature B which references feature A. Graphify communities reveal this.
- **God services** — a service class that accumulates operations for a single entity (violates the "no entity-wrapper services" guardrail). Fix: split into operation-scoped services.
- **Scattered cross-cutting concerns** — the same check (auth, validation, null guard) copy-pasted across multiple endpoints. Fix: pipeline behavior or pre-processor.

### The Deletion Test

For each candidate, ask: **"If I deleted this module/class entirely, would complexity concentrate into one place or scatter across many?"**

- **Concentrates** → the module is load-bearing. Refactor to deepen it, don't remove it.
- **Scatters** → the module is thin glue. It may be unnecessary — its callers could absorb the logic.
- **Nothing changes** → dead code. Delete it.

## Candidate Presentation

For each finding, report:

```
### {N}. {Short title}

**What:** {What's wrong — one sentence}
**Why it matters:** {Coupling? Complexity? DDD violation? Maintenance burden?}
**Evidence:** {File paths, graphify data, or KB reference}
**Effort:** Trivial | Scoped | Complex
**Risk:** Low | Medium | High
**Suggested approach:** {How to fix it — reference a skill if applicable}
```

Rank candidates by impact (how much they improve the system) divided by effort.

## After Presentation

The invoking agent decides next steps:

- **Solo agent:** Proposes the top 1-2 candidates to the user. Implements after confirmation. Stays within solo scope guard (1-3 files). Flags anything bigger.
- **Architect agent:** Writes findings as backlog entries in `docs/backlog.md` or as new feature specs for significant work. Does not implement.
- **User directly:** Reviews candidates, picks which to pursue, decides pipeline (solo vs full team).

## DDD-Specific Vocabulary

Use these terms — not generic architecture terms:

| Instead of | Use |
|-----------|-----|
| Shallow module | Anemic aggregate |
| Seam | Bounded context boundary |
| Adapter | Anti-corruption layer |
| Module depth | Aggregate richness |
| Interface surface | Public domain API |
| Coupling | Cross-slice dependency |

## Guardrails

- **Never refactor without presenting candidates first** — no silent restructuring
- **Don't propose patterns not in the codebase** — improvements use existing patterns, applied more consistently
- **Don't redesign aggregates based on code smell alone** — aggregate boundaries come from domain analysis, not structural metrics
- **Don't merge vertical slices** — two features that share code may still need to be independent. Check if they change for the same reason.
- **Graphify is structural, not semantic** — a high connection count doesn't automatically mean "bad." Central domain concepts (Sprint, Ticket) will always be god nodes.

## What This Skill Does NOT Do

- Implement changes (unless invoked by solo with user confirmation)
- Replace the architect's domain analysis — this finds structural issues, not domain modeling problems
- Generate ADRs — we don't use ADRs. Findings go into backlog or plan steps.
- Run automated metrics (cyclomatic complexity, line counts) — we care about domain boundaries, not numbers
- Teach refactoring techniques — the developer knows Extract Method. This skill finds *what* to improve, not *how*.
