# adhoc-Pass5-EventCorrectness — Questions

Scope source: `docs/backlog.md` (Pass 5 row) + `docs/proposals/refactor-triage.md` findings B1, B2, B6, B7. No `spec.md` (adhoc pass) — scope derived from ADR register + triage. Three of the four findings carry an unresolved `needs-decision`/`confirm` verdict in the triage; per the operating model and the User Decision Guardrail the architect cannot resolve them unilaterally.

**Verified domain-event surface (whole solution, source-only):**
- Events declared: `PersonCreated`, `UserCreated` (Identity only). Fokus aggregates declare **zero** domain events.
- Raise sites: `Person.RaiseCreated()` only — called once, in the bootstrap path of `AuthConfiguration.cs:106`.
- `UserCreated`: never raised. `User` has no `RaiseCreated`.
- Handlers: **zero** `IEventHandler<>` implementations exist for either event.
- `SprintSynced` is a MassTransit *integration* event + a SignalR method name — not the domain-event/interceptor path. Out of scope for "domain-event dispatch."

---

## Q1: B1 — `PersonCreated` dispatch is already firing; the triage verdict is stale. Retract, or redefine the target?
**From:** architect
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis
**File:** `Identity.API/Auth/AuthConfiguration.cs:99-114`, `Identity.Domain/Persons/Behaviors/Person.cs:30`

**Context:** The triage (B1) cited `Person.cs:30` and stated "`RaiseCreated()` called *after* `SaveChangesAsync`, so `PersonCreated` never dispatches." That is **no longer true** in the current code. The bootstrap path now does:
```
person.CreateFromGoogle(...)           // does NOT raise (Id == 0)
personRepository.SaveChangesAsync()    // save #1 — assigns real PK
person.RaiseCreated()                  // adds PersonCreated(Id) with the real Id
personRepository.SaveChangesAsync()    // save #2 — interceptor dispatches the event here
```
The `DispatchDomainEventsInterceptor` fires on `SavedChangesAsync` (post-save), so `PersonCreated` **does** dispatch on save #2. B1 as written is already fixed.

Two real smells remain: (a) the **two-save workaround** is awkward — the bootstrap path performs **4** `SaveChangesAsync` calls for one logical "create person + user" operation, and the raise-then-save-again step exists only to flush the event; (b) `PersonCreated` has **zero handlers**, so it dispatches into the void regardless.

**Question:** How should Pass 5 treat B1?
- **(a) Retract B1** as already-fixed; leave the two-save pattern as-is (no code change). Recommended only if the bootstrap-path awkwardness is acceptable.
- **(b) Keep the event firing but remove the workaround** — e.g. raise `PersonCreated` inside the create flow and let a single post-PK save dispatch it (collapse to fewer saves). This is a small refactor of `AuthConfiguration` bootstrap, not a domain-event-correctness fix.
- **(c) Treat "event with zero handlers" as the real defect** — out of Pass 5's stated scope (handlers are deferred infra work); flag for a later pass.

My recommendation: **(b)** if you want the bootstrap path cleaned up now, otherwise **(a)** + note. This depends on your intent for the event surface (see Q2).

### Answer
**(a) Retract + note.** `PersonCreated` already dispatches correctly (triage stale). No code change. The two-save bootstrap workaround is accepted as-is. → Plan: documented in Context + Locked decisions; no code step.

---

## Q2: B2 — `UserCreated`: wire it, or delete it?
**From:** architect
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis
**File:** `Identity.Domain/Users/Events/UserCreated.cs`, `Identity.Domain/Users/Behaviors/User.cs`

**Context:** `UserCreated(int UserId) : IDomainEvent` is declared but **never raised** and has **no handler**. The triage flagged it `needs-decision`: "intentional not-yet-wired placeholder for the in-flight Identity work, or dead code." `User` is an `Entity<int>`, **not** an `AggregateRoot`, so it cannot currently carry domain events (only `AggregateRoot` has `AddDomainEvent`). Wiring `UserCreated` would require either promoting `User` to an aggregate or raising the event from the `Person` aggregate.

**Question:** What is the intended disposition of `UserCreated`?
- **(a) Delete it** as dead code (simplest; consistent with "no handlers, no raise site").
- **(b) Wire it** — and if so, from where? (User is not an aggregate; this pulls in an aggregate-boundary decision.)
- **(c) Keep as a placeholder** for the deferred Identity infra increment (document intent, no code change).

This is the central B2 needs-decision; I cannot pick it for you.

### Answer
**(c) Keep as a documented placeholder.** Intentional not-yet-wired for the deferred Identity infra increment. Annotate intent; do NOT delete, do NOT wire, no behavior change. → Plan Step 1.

---

## Q3: B6 — DB `HasDefaultValue("Manager")` on `User.Role`: keep as safety net, or remove?
**From:** architect
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis
**File:** `Identity.Persistence/Configurations/UserConfiguration.cs:22`

**Context:** `builder.Property(u => u.Role)...HasDefaultValue("Manager")` duplicates the factory default in `User.CreateFromPerson(int personId, string role = "Manager")`. Triage flagged `needs-decision`: keep the DB-level default as a safety net, or remove it to force factory use. Note: the bootstrap path always passes `"Admin"` explicitly, and the factory default covers normal creation — the DB default is only reachable on a raw insert that bypasses the factory.

**Question:** Keep `HasDefaultValue("Manager")`, or remove it (forcing the factory to be the single source of the default)?

### Answer
**Remove** the DB default; factory (`User.CreateFromPerson`) is the single source of truth. Include the EF migration. → Plan Steps 2–3.

---

## Q4: B7 — `RepositoryBase.SetValues` shallow-copy latent bug: fix now or defer?
**From:** architect
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis
**File:** `Blocks.EntityFrameworkCore/Repositories/RepositoryBase.cs:22-29`

**Context:** `UpsertAsync` uses `DbContext.Entry(existing).CurrentValues.SetValues(entity)`, which copies scalar EF-mapped properties but **misses private backing fields** if an aggregate adopts them. Triage verdict: `real (latent)`, **low** — "Low priority, note for when domain thickens." No current aggregate uses private backing fields, and Identity's `PersonRepository`/`UserRepository` don't use `RepositoryBase.UpsertAsync` at all (Fokus's `SprintRepository`/`TicketRepository` do, but their aggregates are flat-scalar by the B3 by-design decision).

**Question:** Is B7 in scope for Pass 5?
- **(a) Defer** — it's latent and low; no aggregate triggers it today. Document in the architecture/KB and revisit when an aggregate adopts backing fields. (My recommendation — fixing a latent bug with no current trigger and no reproduction risks a speculative change to shared infrastructure.)
- **(b) Fix now** proactively (e.g. property-copy via reflection over backing fields, or document the constraint as a guardrail that aggregates must keep EF-mapped public setters).

### Answer
**(b) Fix now.** Make `RepositoryBase` upsert/`SetValues` copy private backing fields correctly. Add a guardrail/regression note (no automated suite). → Plan Steps 4–5.

---

## Q5: ADR-007 vs the current anemic Identity model — does Pass 5 touch encapsulation, or is that out of scope?
**From:** architect
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis
**File:** `Identity.Domain/Persons/Person.cs`, `Identity.Domain/Users/User.cs`

**Context:** ADR-007 says rich aggregates with VOs apply to **all** aggregates **including Identity `Person`/`User`** (it explicitly "resolves B4-Identity"). Pass 3b's backlog row claims "+ Identity Person/User encapsulation (ADR-007)" was **Done**. But B2's resolution (wiring `UserCreated`) may hinge on whether `User` becomes an aggregate — which is an ADR-007 encapsulation question, not a pure event-dispatch question. I want to confirm the boundary: **Pass 5 is event-dispatch correctness only (B1/B2/B6/B7); any `User`→aggregate promotion or further encapsulation is out of scope** and belongs to the already-Done Pass 3b or a new pass. If Q2 answer (b) requires promoting `User` to an aggregate, that crosses this boundary and should be its own decision.

**Question:** Confirm Pass 5 scope excludes aggregate-boundary/encapsulation changes (i.e. if wiring `UserCreated` needs `User` to become an aggregate, that is a separate pass — not folded into Pass 5). Or do you want the `User`→aggregate promotion pulled into Pass 5?

### Answer
**Scope held** — Pass 5 is event-dispatch correctness only; no `User`→aggregate promotion (B2 = placeholder, so not triggered). → Plan Scope: out of scope.
