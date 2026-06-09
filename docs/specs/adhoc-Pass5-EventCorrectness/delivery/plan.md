# Implementation Plan — Pass 5: EventCorrectness

**Feature Spec:** None (adhoc refactoring/correctness pass — scope from `docs/backlog.md` Pass 5 row + `docs/proposals/refactor-triage.md` findings B1, B2, B6, B7; binding constraint `docs/architecture/decisions.md` ADR register)

> **Dangling-reference note (critic F1):** the backlog and ADR-003 cite `.claude/rules/engineering-discipline.md` as the binding engineering-discipline rule, but **that file does not exist in this repo** (`.claude/` has only `settings*.json`, `.current-agent`, `.personas.json`, `.pipeline-state` — no `rules/` dir; the rule text ships in the Nexus plugin cache, not the project tree). This plan therefore binds to the **ADR register** (`docs/architecture/decisions.md`, present and authoritative) as the enforcement source, not the dangling path. **Flag for the ADR-register owner:** ADR-003's "Enforced by `.claude/rules/engineering-discipline.md`" pointer is also dangling — reconcile it (vendor the rule into the repo or repoint the citation). Not fixed here — fixing the ADR register is out of this pass's scope.
**Slug:** adhoc-Pass5-EventCorrectness
**Plan:** docs/specs/adhoc-Pass5-EventCorrectness/delivery/plan.md
**Intent:** Refactoring / correctness (small, surgical — no feature behavior change except the intended B6 DB-default removal)

---

## Context

Pass 5 closes the four domain-event-dispatch-correctness findings from the refactor-triage. All five scope questions were resolved by the user (see "Locked decisions" below); the resolutions make this a **deliberately small** pass — two of the four findings (B1, B2) are doc-only / annotation, and the two code changes (B6, B7) are surgical.

**Verified domain-event surface (whole solution, source-only — established in Phase 1):**
- Domain events declared: `PersonCreated`, `UserCreated` (Identity only). Fokus aggregates declare **zero** domain events.
- Raise sites: `Person.RaiseCreated()` only, called once in the bootstrap path of `AuthConfiguration.cs:106`.
- `UserCreated`: never raised; `User` is `Entity<int>`, not an `AggregateRoot`.
- `IEventHandler<>` implementations: **zero** for either event.
- `SprintSynced` (MassTransit integration event) and `SprintHub.SprintSynced` (SignalR method name) are **not** the domain-event/interceptor path — out of scope.

**Dispatch mechanism (confirmed correct):** `DispatchDomainEventsInterceptor` (`Blocks.EntityFrameworkCore/Interceptors`) fires on `SavedChangesAsync` (post-save), reads `ChangeTracker.Entries<IAggregateRoot>()` with pending `DomainEvents`, clears them, and publishes via `IDomainEventPublisher` → FastEndpoints `IEvent` bus. This is why the bootstrap path's `RaiseCreated()` + second `SaveChangesAsync()` **does** dispatch `PersonCreated` today.

## Scope

**In scope (4 findings):**
- **B1** — Document that `PersonCreated` dispatch is correct (triage verdict stale); no code change. (Doc-only.)
- **B2** — Annotate `UserCreated` as an intentional deferred-infra placeholder; do not delete, do not wire. (Single doc-comment in `UserCreated.cs`.)
- **B6** — Remove `HasDefaultValue("Manager")` from `UserConfiguration` + EF migration. (Code + migration.)
- **B7** — Fix `RepositoryBase` upsert so the value copy includes private backing fields; add a guardrail/regression note. (Shared `Blocks.EntityFrameworkCore` code.)

**Explicitly out of scope:**
- **No `User` → aggregate promotion** (Q5 held). B2 is a placeholder, so the promotion is not triggered.
- **No domain-event handlers** — wiring consumers for `PersonCreated`/`UserCreated` is deferred-infra work, not this pass.
- **No change to the bootstrap two-save pattern** (Q1: accepted as-is).
- **No `SprintSynced` / SignalR / MassTransit changes** — not the domain-event path.
- **No Fokus changes** — Fokus aggregates raise no domain events.

---

## Locked decisions (from questions.md — all user-decided)

- **Q1 → Retract B1 + note.** `PersonCreated` already dispatches correctly; the triage line is stale. No code change. The two-save bootstrap workaround in `AuthConfiguration.cs` is **accepted as-is**.
- **Q2 → Keep `UserCreated` as a documented placeholder.** Intentional not-yet-wired for the deferred Identity infra increment. Annotate intent in `UserCreated.cs`. Do **not** delete, do **not** raise it, no behavior change.
- **Q3 (B6) → Remove** the DB `HasDefaultValue("Manager")` so the factory default (`User.CreateFromPerson(..., role = "Manager")`) is the single source of truth. Include the EF migration.
- **Q4 (B7) → Fix now.** Make `RepositoryBase` upsert copy private backing fields correctly. Add a guardrail/regression note (no automated suite).
- **Q5 → Scope held.** Event-dispatch correctness only; no `User`→aggregate promotion.

---

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | (none — doc-comment only) | None | Annotate `UserCreated.cs` with deferred-placeholder intent; no logic | — |
| 2 | `persistence-patterns` | Follow | Remove `HasDefaultValue("Manager")` from `UserConfiguration.cs:22`; factory default is the single source | — |
| 3 | `persistence-patterns` | Follow | EF migration for the dropped column default on `Users.Role`; Identity migration command | — |
| 4 | (none — skill gap) | None | Fix `RepositoryBase.UpsertAsync` value copy to include private backing fields; constraint + acceptance below | Log to lessons.md → `persistence-patterns` |

**Skill-verification notes:**
- **Step 1** performs no action that any skill frontmatter covers (it adds an XML doc comment stating intent). `None` is warranted.
- **Steps 2–3** configure persistence + add a migration → `persistence-patterns` covers entity configuration and migrations. `Follow`.
- **Step 4** modifies the shared `RepositoryBase.UpsertAsync`. `persistence-patterns` documents `UpsertAsync` exists and even notes `CurrentValues.SetValues` does not deep-copy **owned/complex** types — but it does **not** cover the **private backing-field** copy concern named by B7. No skill frontmatter matches this specific fix, and the reference project (`<reference-repo>`) has **no `RepositoryBase`** to derive a pattern from (verified). `None` is warranted; the fix + a skill-improvement note feed back into `persistence-patterns`.

**Anti-pattern check:** no "adapt" near a skill ref; no skill dismissed as "too simple"; Follow steps carry only feature-specific inputs (file:line + the one value to change / the migration command), not restated EF boilerplate.

---

## Domain Model Changes

None. No new/modified aggregates, value objects, or domain events. `UserCreated` and `PersonCreated` records are untouched in shape (Step 1 adds only a doc comment to `UserCreated`).

## Data Model Changes

- **B6:** `Users.Role` column loses its DB-level default value (`"Manager"`). The column stays `NOT NULL`, `maxLength 32`. **No data migration** — existing rows already have a concrete `Role` value; dropping the default only affects future raw inserts that bypass the factory (which the codebase never does). One EF migration captures the model-snapshot change.

---

## Implementation Steps

### Step 1 — Annotate `UserCreated` as a deferred-infra placeholder (B2)
**File (modify):** `src/Services/Identity/Identity.Domain/Users/Events/UserCreated.cs`

Add an XML doc comment (`/// <summary>…</summary>`) above the `UserCreated` record stating that it is an **intentional, not-yet-raised placeholder** for the deferred Identity infra increment — declared ahead of its consumer, deliberately not wired, and must not be deleted as dead code. Do **not** add a `RaiseCreated`, do **not** raise it anywhere, do **not** change the record's shape or its `: IDomainEvent` declaration.

- **Skill:** None (doc comment only — no pattern applies).
- **Accept:** `UserCreated.cs` carries a doc comment naming it a deferred placeholder; `grep` for `UserCreated` across `src/` still shows exactly the declaration and zero raise sites (unchanged from Phase 1); build green.
- **Confidence:** high.

### Step 2 — Remove the DB-level default on `User.Role` (B6)
**File (modify):** `src/Services/Identity/Identity.Persistence/Configurations/UserConfiguration.cs:22`

Remove `.HasDefaultValue("Manager")` from the `Role` property configuration. Keep `.IsRequired().HasMaxLength(32)`. The factory default in `User.CreateFromPerson(int personId, string role = "Manager")` becomes the single source of truth for the default role.

- **Skill:** Follow `persistence-patterns` (entity configuration).
- **Dependency:** none.
- **Accept:** `UserConfiguration.cs` line for `Role` reads `builder.Property(u => u.Role).IsRequired().HasMaxLength(32);` with no `HasDefaultValue`. `grep "HasDefaultValue" src/Services/Identity` returns only the unrelated `IsActive` default on line 23 (DO-NOT-TOUCH — that default was not in scope; only `Role`'s default is removed).
- **Out-of-scope note (critic F4):** `IsActive`'s DB default (`HasDefaultValue(true)` at `UserConfiguration.cs:23`) has the **identical** DB-vs-factory duplication smell (the factory also sets `IsActive = true` at `User.cs:11`), but Q3 scoped B6 to `Role` only. The `IsActive` duplication is **knowingly left for a future pass** — not an oversight. Do not expand scope to it.
- **Confidence:** high.

### Step 3 — Add the EF migration for the dropped `Role` default (B6)
**Files (create):** new migration under `src/Services/Identity/Identity.Persistence/Migrations/` + updated `IdentityDbContextModelSnapshot.cs`.

Generate the migration capturing the removal of the `Users.Role` default value, using the Identity migration command from `docs/conventions/project-rules.md`:
```
dotnet ef migrations add RemoveUserRoleDbDefault -p src/Services/Identity/Identity.Persistence -s src/Services/Identity/Identity.API
```
(Run from the `src/` root or adjust project paths accordingly; the `-s` startup project is `Identity.API`.)

- **Skill:** Follow `persistence-patterns` (migrations).
- **Dependency:** Step 2 (migration reflects the config change).
- **Accept:** a new `*_RemoveUserRoleDbDefault.cs` migration exists; its `Up` alters the `Users.Role` column to drop `defaultValue: "Manager"` (and `Down` restores it); `IdentityDbContextModelSnapshot.cs` no longer shows the `"Manager"` default on `Role`; `dotnet build` green. **Do not hand-edit** the migration — let `dotnet ef` scaffold it, then verify the `Up`/`Down` and snapshot diff are exactly the default drop and nothing else.
- **Confidence:** high.

### Step 4 — Fix `RepositoryBase.UpsertAsync` to copy private backing fields (B7)
**File (modify):** `src/BuildingBlocks/Blocks.EntityFrameworkCore/Repositories/RepositoryBase.cs:22-29`

**Problem (latent + currently dead code):** the existing upsert does `DbContext.Entry(existing).CurrentValues.SetValues(entity)`, passing the **detached** incoming `entity` object directly. When `SetValues` is handed a plain object it reads destination-shaped values reflectively and can miss properties mapped to **private backing fields** (fields configured with `PropertyAccessMode.Field`), because the read path on a detached POCO does not go through EF's property machinery.

**Critical context (critic F2/F3 — verified on disk):** `RepositoryBase.UpsertAsync` has **zero production callers**. All four Fokus repositories — `SprintRepository:46`, `TicketRepository:39`, `DeveloperRepository:28`, `TestExecutionRepository:29` — declare `public new async Task UpsertAsync(...)` that **hides** the base method and does its own explicit per-column scalar copy (e.g. `SprintRepository:55-63`, `TicketRepository:49-58`). The sync services call `UpsertAsync` on the **concrete repo types** (`SprintIssueSyncService:59/121/125/155/160`, `XrayIssueSyncService:239/324`), so the `new` override resolves — never the base. Therefore:
- This fix touches **dormant shared infrastructure**. It carries **no runtime regression risk to the sync path** (the sync path never reaches the base method).
- The "harden the shared base" value is **purely forward-looking**: nothing calls the base today, and any future aggregate wanting the hardened copy would *also* have to stop hiding it with a `new` override to benefit. No current aggregate uses private backing fields either, so the bug is latent on top of being unreached.
- The user (Q4) chose "fix now" knowing it is latent/low — this plan honors that, but states the value honestly: a correctness improvement to dead-today infra, not a fix to a live hazard.

**What to accomplish (developer decides decomposition — do NOT write a method body here):**
- Make the value copy use EF's own property machinery for **both** sides so it respects configured access modes (including backing fields). The EF-idiomatic approach: attach/track the incoming `entity` (or read its values through `DbContext.Entry(entity).CurrentValues`) and copy from that `PropertyValues` source onto `existing`'s `CurrentValues`, rather than passing the raw detached object. Equivalent correct approaches are acceptable as long as the acceptance criteria hold.
- Preserve the existing semantics exactly: insert when `FindByIdAsync` returns null; otherwise update the tracked `existing` in place. Do not change the method signature, the `Add` branch, or any other `RepositoryBase` member.
- **Carve-out (DO-NOT-TOUCH):** this fix is the **scalar/backing-field** copy only. The `persistence-patterns` skill already notes that `CurrentValues.SetValues` does **not** deep-copy **owned/complex types** — that is a *separate*, documented concern with its own explicit-assignment pattern (`SaveXraySettings`, etc.). Do **not** attempt to also solve owned/complex-type copying in this step; it is out of scope and no current aggregate has owned types on an upsert path.

- **Skill:** None (skill gap — `persistence-patterns` documents `UpsertAsync` and the owned-type caveat but not the backing-field copy fix; reference project has no `RepositoryBase`). Log to lessons.md as a `persistence-patterns` improvement. **When logging the lesson (critic F6), capture the real codebase nuance:** the base `UpsertAsync` is bypassed by `public new` overrides in all four Fokus repos that do explicit per-column copy — so the skill improvement should document *both* the base backing-field mechanics *and* the live "override-with-explicit-copy for non-trivial entities" pattern.
- **Dependency:** none (independent of Steps 1–3).
- **Accept:**
  1. `RepositoryBase.UpsertAsync` no longer passes the raw detached `entity` to `SetValues`; the copy goes through EF `PropertyValues` (or an equivalent access-mode-respecting mechanism).
  2. Insert-vs-update branch behavior unchanged (null → `AddAsync`; non-null → update in place).
  3. `dotnet build SprintRituals.slnx` green (filter `error CS` per build-verification rules).
  4. **Verification is compile-green + manual reasoning — NOT a sync re-run.** Because `RepositoryBase.UpsertAsync` has no production caller (all four Fokus repos hide it with `new`; see Problem above), **no end-to-end run can exercise this change** — a sprint/ticket re-sync resolves the `new` override and never touches the edited base method. Do **not** claim a re-sync validates Step 4. The honest verification is: (a) build green, (b) read the edited method and confirm it copies via EF `PropertyValues` (access-mode-respecting) while preserving the insert-vs-update branch. If the developer wants positive runtime evidence, construct a throwaway EF in-memory probe that calls the base `UpsertAsync` directly on an entity type that does *not* override it; this is optional given there is no such production type today. State whichever verification was done in `implementation.md`.
- **Confidence:** medium — the fix has no skill or reference precedent; the developer should explore EF's `PropertyValues`/`SetValues(PropertyValues)` overloads and confirm the access-mode behavior before settling on the approach. If the chosen approach changes any observable behavior of the base `UpsertAsync` beyond the backing-field copy, raise a question rather than proceeding.

### Step 5 — Add the B7 guardrail/regression note (documentation)
**File (create or modify):** `docs/conventions/ef-core.md` (the conventions index references it) — add a short guardrail entry; if a more appropriate convention file exists for repository/persistence guardrails, use that instead.

Document the guardrail surfaced by B7. The note must name the **actual live pattern the codebase uses today** (critic F3), not a path no aggregate takes:

- **Live pattern (the rule the code actually relies on):** repositories with non-trivial upsert/copy semantics **override `UpsertAsync` with `public new`** and do an explicit per-column assignment (the `SprintRepository`/`TicketRepository`/`DeveloperRepository`/`TestExecutionRepository` pattern). The base `RepositoryBase.UpsertAsync` is the **fallback for simple flat entities** and currently has no production caller.
- **Forward-looking guardrail (what the B7 fix enables):** *if* a future repository drops its `new` override and relies on the base `UpsertAsync`, and *if* its aggregate adopts private backing fields, the aggregate must configure the matching `PropertyAccessMode` so the base copy (now EF-`PropertyValues`-based after Step 4) picks them up. Until both conditions hold, the hardening is dormant.
- **Verification reality:** the base method has no end-to-end test path (no caller); verification is compile-green + code reading, not a sync re-run (there is no automated suite). Do not document a re-sync as validating the base change.

This is the "guardrail/regression note" the user asked for under Q4.

- **Skill:** None (documentation).
- **Dependency:** Step 4 (documents the fixed behavior + the constraint that keeps it correct).
- **Accept:** the guardrail note exists in a conventions file and names (a) the live `new`-override explicit-copy pattern as the current rule, (b) the forward-looking backing-field/`PropertyAccessMode` condition, and (c) that the base method has no current caller so verification is compile + reasoning. (Documentation artifact — not a source file.)
- **Confidence:** high.

---

## Cross-Service Changes

None. No gRPC contract changes, no integration events, no new consumers. (B1 confirms the existing `PersonCreated` dispatch is correct; no handler is added.)

## Migration Notes

- One migration only — Step 3: `RemoveUserRoleDbDefault` on `Identity.Persistence` (startup `Identity.API`). No seed data. No Fokus migration. The migration is a pure column-default drop; existing rows are unaffected.

## Testing Strategy

Manual (no automated suite — accepted for this app):
1. **B6:** Create a User via the normal factory path (Google sign-in bootstrap → first user `Admin`; subsequent factory calls default to `Manager`); confirm `Role` is set by the factory, not the DB. Confirm the app starts and the migration applies cleanly to `identity.db`.
2. **B7 verification = compile-green + code reading, NOT a sync re-run.** `RepositoryBase.UpsertAsync` has no production caller (all four Fokus repos hide it with `new`), so no end-to-end run exercises the change — see Step 4 Accept #4. A sprint/ticket re-sync exercises the **`new` overrides**, not the edited base, so it cannot validate Step 4. *Optionally* run a re-sync of the same Fokus sprint/tickets twice as a **general sync sanity check unrelated to B7** (confirms the override path still updates existing rows + fresh sync inserts) — but do not record it as B7 verification.
3. **Regression (B1/B2):** Confirm Google sign-in still works end-to-end (bootstrap + regular login); `PersonCreated` still dispatches on the bootstrap second save (no handler, so no observable effect — verify no exception). `UserCreated` remains unraised.

## KB Impact

None — no `docs/kb/` index exists in this repo, and these changes are infra/correctness with no business-rule/formula/edge-case content to capture. (The B7 guardrail lands in `docs/conventions/`, captured as numbered Step 5 — not the KB.)

## Open Questions

None — Q1–Q5 all resolved by the user (see Locked decisions).

---

## Plan Review

**Critic (Mode 2, code-grounded) verdict:** REQUEST CHANGES — full findings in `critic-review.md`. Coverage matrix passed (all five locked decisions covered, no scope creep, B6 migration correct). One High + three Medium/Low framing/accuracy defects, all on the B7 steps. **User decision on F2 (High):** KEEP B7's fix (Steps 4–5); correct the framing/verification only — do not drop the steps.

| Finding | Sev | Disposition | Where fixed |
|---------|-----|-------------|-------------|
| F1 — dangling `engineering-discipline.md` citation | Med | Fixed | Front-matter: binding constraint repointed to the ADR register; dangling path + ADR-003 pointer flagged for the ADR-register owner (not fixed here — out of pass scope). |
| F2 — re-sync "regression check" tests a path the change can't reach (base `UpsertAsync` has zero callers; all 4 Fokus repos hide it with `new`) | **High** | Fixed | Step 4 Problem rewritten with verified zero-caller reality; Accept #4 changed to compile-green + code-reading (re-sync explicitly rejected as B7 verification); Testing #2 relabeled as a general sync sanity check unrelated to B7. **User: keep the fix, correct framing.** Independently verified on disk (`SprintRepository:46`, `TicketRepository:39`, `DeveloperRepository:28`, `TestExecutionRepository:29` all `public new`; sync services call the concrete types). |
| F3 — "hardens the shared base" overstates current benefit | Med | Fixed | Step 4 now states the hardening is purely forward-looking (base unreached); Step 5 guardrail rewritten to name the live pattern (explicit-copy `new` overrides) + the forward-looking backing-field condition. |
| F4 — parallel `IsActive` DB-vs-factory duplication | Low | Fixed | Step 2: one-line note that `IsActive`'s identical duplication is knowingly out of Q3 scope (future pass), not an oversight. |
| F5 — migration step correctness | Low | No action (confirmed correct) | Step 3 unchanged — critic confirmed `Up`/`Down`/snapshot acceptance matches the on-disk InitialCreate migration. |
| F6 — enrich Step 4 lesson with `new`-override nuance | Low | Fixed | Step 4 skill-gap routing now instructs the developer to capture both the backing-field mechanics and the live `new`-override pattern in the `persistence-patterns` lesson. |

**Post-fix self-finalize:** all High/Medium findings resolved or correctly deferred per user decision; no CRITICAL or HIGH remains open. Plan **APPROVED**. 5 steps (no split). No open questions.
