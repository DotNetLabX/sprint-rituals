# adhoc-ReflektIdentityUnification (RA2) — Summary

**Status:** COMPLETE — accepted 2026-06-14. Closed by team lead.
**Branch:** main (merged from `adhoc-stryker-smoke-test` by fast-forward at launch).
**Team mode:** Standard+Codex. **Plan review:** critic (1 round). **Code review:** reviewer ∥ Codex.

## What shipped

RA2 identity plumbing: a Reflekt retro participant's authenticated identity is now the Identity **`Person`**, not a Reflekt-local `User`.

- **Removed** Reflekt's `User` aggregate, `UserRepository`, `UserConfiguration`, `Users` DbSet, seed-`User` block, both `HasOne<User>()` FK configs, the four stubbed `_ra1/unmapped` auth endpoints, and the dead `JoinWithGoogle`/`GetByUserAndSprintAsync` members.
- **Added** a `person_id` claim to Identity's cookie (`AuthConfiguration.SignInAsync`) — the suite-wide Person anchor Reflekt reads without a gRPC round-trip.
- **Repointed** all Reflekt ownership from User id → Person id: helper `GetUserId()`→`GetPersonId()` (31 call sites) plus three hand-edited helper-bypass sites (`SprintHub.cs`, `GetSprintSummaryEndpoint.cs`, `EditCard`/`DeleteCard`). Wire DTO `JoinSprintResponse.UserId` kept stable via Mapster (`dest.UserId ← src.PersonId`).
- **Scoped** Identity's global `ActiveUserPreProcessor` off Reflekt via mechanism (A) assembly-of-declaring-endpoint (`ActiveUserGateScope`), so a retro participant (Person-or-guest, not necessarily a suite User) isn't 401'd. Forward-compatible with ADR-021's authenticated-Person-without-User case.
- **Join flow** (Step 6): single `Participant.Join(displayName, sprintId, int? personId = null)` factory; `JoinSprintEndpoint` stays `[AllowAnonymous]`, sets `PersonId` from the cookie when present. SM-approval gate left as an unbuilt seam (ADR-021).
- **Migration** `RemoveReflektUserUnifyIdentity`: drops both FKs + `Users` table + old `{SprintId,UserId}` index, renames column to `PersonId`, recreates the filtered unique index `"PersonId" IS NOT NULL`.

**Out of scope (by design):** SM waiting room (→ RW / `adhoc-RetroWaitingRoom`), guest `Person` creation, `IPersonService` client (→ RW per the ADR-016 timing amendment), `Sprint`→`RetroSession` (→ RA3), frontend.

## Pipeline outcome

| Gate | Outcome |
|------|---------|
| Developer Phase-1 analyze | Raised Q3/Q4/Q5 (re-grounded against committed RA1) |
| Architect adjudication | All 3 resolved, no escalation; plan updated + ADR-016 timing amendment |
| Critic (plan review) | **REJECT** → 1 CRITICAL (`GetSprintSummary` 3rd bypass) + 2 HIGH + 2 MEDIUM → all fixed & verified |
| Developer implementation | 7/7 steps, build green, 68/68 tests |
| Architect Step-1 done-check | **PASS** (build + tests + 4 grep-acceptances re-verified) |
| Reviewer (Step-2 code review) | **APPROVED** (highest sev LOW) |
| Codex (independent cross-check) | **NO-GO** (no CRITICAL; 1 HIGH + 1 MEDIUM) — reconciled below |

## Verdict reconciliation (reviewer APPROVE ∥ Codex NO-GO)

Resolved finding-by-finding. No CRITICAL anywhere; user accepted as-is with carry-overs.

## Carry-overs (accepted, not fixed)

- **[Codex HIGH] Migration `Down()` not operationally reversible** on a populated DB (can't remap Person ids back to dropped User ids). **Out of RA2 scope** per ratified Q3c (no backfill, dev `reflekt.db` disposable); the mapping is gone after `Up()`. Schema-reversible; not data-reversible. Known limitation.
- **[Codex MEDIUM] Authenticated repeat-join → unhandled 500.** A suite member joining the same retro twice (different display name) hits the `(SprintId,PersonId)` unique index → generic 500 instead of a graceful "already joined." Caught only by Codex. Deferred — a graceful path (check existing `Participant` by `PersonId`, or catch the unique violation) is a small future fix.
- **[Reviewer LOW] `EditCard`/`DeleteCard` else-branch throwing `GetPersonId()`** → 500 vs 403 for a stale pre-deploy cookie. Out of scope (dev cookies re-issue).
- **[Reviewer LOW] ~27 endpoints retain a local `userId` name** holding a Person id — cosmetic.

## Commits

- `aa1e9a6` — `feat(adhoc-ReflektIdentityUnification): add implementation plan` (plan + questions + critic review)
- (this close) — `feat(adhoc-ReflektIdentityUnification): implement identity unification` (source + delivery docs)

## Left in the working tree (not committed by the pipeline — owner manages)

- **ADR-016 timing amendment** in `docs/architecture/decisions.md` (the one-line owner-ratified amendment) — intermingled with the owner's other uncommitted ADR changes, so not swept into a pipeline commit.
- **Backlog RA2 → Done** flip in `docs/backlog.md` — likewise intermingled with the owner's other backlog edits.
- Other pre-existing dirty docs (`project-rules.md`, the suite proposal, two Pass4/Pass5 `lessons.md`, the learner-audit md).
