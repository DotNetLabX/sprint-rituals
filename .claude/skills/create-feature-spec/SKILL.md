---
name: create-feature-spec
description: Creates a feature specification in docs/specs/{slug}/definition/ — the product definition that precedes implementation planning. Used by the PO agent (primary) or architect. Run this before writing a plan.
user-invocable: true
---

# Create Feature Spec (Architect Reference)

This skill is for the **PO agent** (primary owner) or **architect agent**. It produces `docs/specs/{slug}/definition/spec.md` — the product definition that the implementation plan is built against. The plan in `docs/specs/{slug}/delivery/plan.md` references this spec but does not replace it.

## Audience & voice

This document is read by the **PO, architect, and the human stakeholder** — not the Developer. The Developer reads the plan, never this file.

Write in **domain / product language**. Describe what the user does and what the system guarantees — not how the code implements it. A reader with no codebase access should understand every section.

### Do not include

These belong in the implementation plan, not the feature spec:

- **Class or method names** — `CreateTeamCommandValidator`, `ExistsByCreatorEmailAsync`, `GuestTokenAuthenticationHandler`
- **Framework internals** — `HandleResponse()`, `SaveTokens = false`, `AuthenticateResult.NoResult()`, `CookieSecurePolicy.SameAsRequest`
- **Persistence details** — collation strategies, cascade behavior, FK column names, migration names, index types
- **Constructor / instantiation patterns** — private constructors, `required init`, factory method signatures
- **Interface or base class names** — `IDomainEvent`, `IEventHandler<T>`, `AggregateRoot`
- **Config key paths** — `Auth:AllowedEmails`, `Auth:Google:ClientId` (say "email allowlist configured per-environment" instead)

**Rule of thumb:** if it would change during a refactor but the product behavior stays the same, it's plan material, not spec material.

## When to use

Before writing an implementation plan. Every feature needs a spec in `docs/specs/{slug}/definition/spec.md` before `docs/specs/{slug}/delivery/plan.md` is created.

## Reading protocol

Before writing, ensure you have:

1. **Business spec** — read on-demand (project-specific path listed in the Architect agent's "What you know" section)
2. **Architecture reference** — already loaded via the Architect agent's `@` directives
3. **Existing feature specs** — read `docs/specs/*/definition/spec.md` (and `docs/specs/*/*/definition/spec.md` for nested issues) for format consistency, overlaps, dependencies
4. **Domain source files** — read current aggregates/entities to understand what already exists. Use this to inform your understanding, but do not carry class names or implementation patterns into the spec.

## Steps

1. **Pre-fill from conversation context.** The Architect has typically been discussing the feature before invoking this skill. Pull every answer you can from the existing conversation. Do not re-ask what the user already said.

2. **Walk through the reading protocol.** Read existing feature specs and domain source files. Identify which sections of the business spec this feature traces to.

3. **Ask clarifying questions in one batch.** For anything ambiguous or unspecified in the business spec for this feature — ask once, all together. Do not write the file yet.

4. **Write the spec** — follow `workflows/Template.md`. Output path: `docs/specs/{slug}/definition/spec.md` (or `epic.md` for epics, `bug.md` for bugs).

5. **Voice check.** Before telling the user, scan the draft for:
   - Code-formatted identifiers in backticks — for each one, ask: domain term or code artifact? Code artifacts move to the plan.
   - Type-suffix patterns: `Handler`, `Validator`, `Repository`, `Service`, `Command`, `Query`.
   - Code-formatted method invocations (backticks containing `()`).
   - Framework flag literals: `= true`, `= false`, `Policy.X`.
   - Config section paths (e.g. `Auth:AllowedEmails`, `Foo:Bar`).

   Domain concepts that happen to share names with classes (e.g. "ScrumMaster policy" as a concept) can stay — the rule targets code references, not vocabulary overlap.

6. **Offer final choices.** Ask the user two things together:
   - **Verification method:** cross-check (quick, same-context) or critic review (thorough, independent)?
   - **Help content:** "Do you want help content files for this feature?" If yes, produce two sibling files in the same folder, both mirroring the spec's section headings:
     - `docs/specs/{slug}/definition/help.tooltips.md` — one section per UI element, each containing only the tooltip text (under 150 chars).
     - `docs/specs/{slug}/definition/help.page.md` — one section per UI element, each containing a guide page paragraph explaining interpretation and recommended actions.
     Write in user-facing language — this content will appear in the app as tooltips and guide page entries.

## Slug Resolution

The `{slug}` is determined by the PO before invoking this skill, following the naming convention in `agents-workflow.md` § Slug and Path Resolution:
- Internal feature: `F{N}-{Name}` (e.g., `F5-SprintSummaryCard`)
- Jira issue: `{KEY}-{2-3-words}` (e.g., `ABC-5226-split-config`)
- Jira epic: `{KEY}-{2-3-words}` (e.g., `ABC-5234-shelf-compliance-kpis`) — writes `epic.md`, not `spec.md`
- Bug: `BUG-{N}-{name}` (e.g., `BUG-1-bug-count-zero`) — writes `bug.md`, not `spec.md`
- Gap: `GAP-{N}-{name}` (e.g., `GAP-3-sub-team-management-ui`)

For an issue nested under an epic, the path becomes `docs/specs/{epic-slug}/{issue-slug}/definition/spec.md`.

## Arguments

Pass the feature name: `create-feature-spec Sprint`

## Downstream Consumers

| Agent | What they use | Impact if spec is incomplete |
|-------|--------------|------------------------------|
| Architect | All sections (plans against the spec) | Plan steps miss requirements; gaps discovered only during implementation |
| Critic (Mode 1) | All sections (validates spec vs v1.md) | Critic can't find gaps in an incomplete spec; architectural misalignments reach the plan stage |
| Developer | Never reads spec directly — reads the plan | Indirect: spec gaps become plan gaps, which become implementation gaps |

## What this skill does NOT do

- Does not write implementation plans — that is the Architect's plan workflow.
- Does not create code, project files, or any implementation artifact.
- Does not make architecture decisions — those live in the architecture reference doc.
- Does not duplicate the business spec — it references sections and adds specificity (exact API shapes, acceptance criteria, resolved ambiguities).
