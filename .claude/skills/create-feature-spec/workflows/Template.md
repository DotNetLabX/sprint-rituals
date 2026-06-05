# Feature Spec Template

Use this template for every `docs/specs/{slug}/definition/spec.md` file (or `epic.md` for epics, `bug.md` for bugs). Omit sections marked "if applicable" when they don't apply. Do not add sections beyond this template.

---

```markdown
# {Feature Name}

**Traces to:** `docs/product/v1.md` §{section numbers}
**Source:** Scratch | Jira {TICKET-KEY}
**Dependencies:** {features or aggregates that must exist first, or "None"}
**Status:** Draft | Ready | Done
**Plan:** `docs/specs/{slug}/delivery/plan.md` | None

---

> **Revision (YYYY-MM-DD) — Pending:** One-paragraph summary: what changed, why, what's already implemented, and where the implementation delta is. Include enough detail that the architect can plan only the delta — not re-plan the entire feature. Status is `Pending` (not yet implemented) or `Implemented` (done). Stack multiple revisions in reverse chronological order (newest first). The architect plans only `Pending` revisions; mark `Implemented` when the pipeline completes. *Only add when revising a spec that already reached `Status: Ready`. Omit during initial creation.*

---

## Purpose

Why this feature exists in v1. One paragraph max. Reference the v1 spec for broader context — don't repeat it.

## Entities

Domain concepts this feature introduces or extends. Only what this feature owns — don't repeat entities owned by other features.

For each entity, specify:
- Fields with types and constraints (required, max length, unique)
- Relationships in domain language ("A Team belongs to one creator", not FK column names)
- Creation rules ("only created through a factory that enforces X" — without naming the method)

Do not include: persistence details (cascade behavior, collation, index types, migration names), constructor visibility, base class names, or interface names. Those belong in the plan.

Reference `docs/product/v1.md` §4 for the canonical entity definitions.

## User Flows

Numbered step-by-step interactions. One flow per distinct user journey.

```
Flow 1: {Name}
1. User does X
2. System responds with Y
3. ...
```

## API Surface

| Method | Route | Auth | Request body | Response | Status codes |
|--------|-------|------|-------------|----------|-------------|
| | | | | | |

For each endpoint, specify error responses and their conditions.

## Real-time Events

*Omit section if this feature produces no real-time events.*

| Event | Payload | Trigger |
|-------|---------|---------|
| | | |

## Business Rules

Numbered list. Each rule is enforceable and testable. State rules in domain language — "email comparison is case-insensitive", not "uses `StringComparer.OrdinalIgnoreCase`". Describe what the system guarantees, not how the code enforces it.

1. {Rule}
2. {Rule}

## Acceptance Criteria

Checklist format. Each item is an observable outcome — something a tester can verify from the outside without reading source code. Name endpoints and HTTP status codes, not classes or methods.

- [ ] {Criterion}
- [ ] {Criterion}

## Out of Scope

What this feature explicitly does NOT include. Each item has a reason.

- **{Thing}** — {why it's deferred}

## Open Questions

*Omit section if all questions are resolved.*

Questions that must be answered before the implementation plan can be written.

- [ ] {Question}
```
