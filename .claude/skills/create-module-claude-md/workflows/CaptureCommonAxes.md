# Capture Common Axes

Regardless of archetype, every module needs these axes. Walk through the list using values pre-filled from conversation context first, then ask the user about anything still unanswered in a single batch.

## Axes

| Axis | Values | Notes |
|------|--------|-------|
| Module name | PascalCase | Drives folder name `src/Modules/{Name}/` |
| Purpose | 1-2 sentences | Goes in CLAUDE.md header |
| Archetype | `Component` / `Domain` | **Decisive branch.** Determines which workflow runs next and the entire CLAUDE.md shape |

## How to choose archetype

Ask yourself (or the user) these questions:

1. **Does the module have its own domain model (aggregates, domain events, state transitions)?** If yes → **Domain**. If no → **Component**.
2. **Is the module an infra adapter replaceable by swapping implementations (e.g., AzureBlob vs MinIO)?** If yes → **Component**. If no → probably **Domain**.
3. **Does the module produce side effects by handling domain events from the host service (e.g., tracking timeline entries when the host publishes events)?** If yes → **Domain**.
4. **Is the module pure contract + one or more runtime implementations with no persistence of its own?** If yes → **Component**.

If the answer is ambiguous (some of both), default to **Domain** — Component archetype is strictly for stateless infra adapters.

## Pre-fill from conversation

Before asking the user anything, scan the existing conversation for:
- Module name mentioned in the user's request or existing plan files
- Verbs like "tracks", "stores", "records" → Domain hint
- Verbs like "sends", "uploads", "reads from" → Component hint
- Existing references to `IFileStorage`, `IEmailSender`, or similar contract interfaces → Component
- Existing references to aggregates, entities, or domain events → Domain

## Ask-in-one-batch rule

Once you have pre-filled values, mark each axis as either `[confirmed from context]` or `[unanswered]`. Ask the user about **all** unanswered axes in a single message. Do not ask sequentially.

Example:

> I captured these from our conversation:
> - Name: `Audit`
> - Archetype: `Domain` (you mentioned tracking who changed what)
>
> I still need:
> 1. Purpose — one sentence on what the module tracks
>
> Confirm or correct.

## After this step

You have `{Name}`, `{Purpose}`, `{Archetype}`. Next:
- `Component` → `CaptureComponentAxes.md`
- `Domain` → `CaptureDomainAxes.md`
