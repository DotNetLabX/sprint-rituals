# Questions — adhoc-ReflektIdentityUnification (RA2)

**Status:** Answered (owner decisions 2026-06-14) — ready for RA2 planning
**Pass:** RA2 — Reflekt Integration Program (`docs/backlog.md`)
**Binding input:** ADR-016 + **ADR-021** + **ADR-022** + the RA2 backlog row. No spec.md (adhoc).

> **Q1/Q2 below (pre-plan, ADR-021/022) remain Answered/Resolved — unchanged.**
> **Q3–Q5 added by the developer's Phase-1 re-grounding against committed RA1 (HEAD `4d93287`).** They are technical/plan questions for the architect, raised before any code is written.

## Seam mapped (mechanical, decision-independent)

- Reflekt `Participant`: `SprintId, DisplayName, GuestToken, JoinedAt, UserId int?` — `UserId` (FK to Reflekt's own `User`) repoints to Identity `Person` → `PersonId`.
- Reflekt `User` + `UserRepository` + `UserConfiguration` + `OnGoogleTicketReceived` → **removed** (ADR-016).
- Reflekt policies are **scheme-based** (Cookie / GuestToken), no `Role` claim; `ScrumMaster` = any cookie-authenticated user.
- `ClaimsPrincipalExtensions.GetUserId()` parses `NameIdentifier` int → becomes the Identity `Person` id.
- Identity `Person`: `PersonSource` includes `Guest`, `Email?`/`UserId?` nullable — a Person can exist without a User. `IPersonService.UpsertPersonAsync`/`FindPersonByEmailAsync` exist (Fokus is already a client).

## Q1 — Auth-model reconciliation → ANSWERED (ADR-021)

**Resolution:** **Open participation, SM waiting room.** Anyone may attempt to join a retro; the SM approves entry. Participants already known to Identity (existing `Person`/`User` for that Google account) skip the room; **guests (no account) + Google users not yet in Identity** wait for SM approval. This replaces Identity's invitation gate *for Reflekt*; the global `ActiveUserPreProcessor` is scoped so it does **not** 401 Reflekt's retro endpoints.

**RA2 scope impact (plumbing only):** RA2 does **not** build the waiting room — it is a separate later feature (`adhoc-RetroWaitingRoom`, see backlog). RA2 must:
- remove Reflekt's own auth scheme registration; keep the guest scheme; rely on Identity's cookie;
- **scope `ActiveUserPreProcessor` off Reflekt's endpoints** (a retro participant is a `Person`-or-guest, not necessarily a suite `User`);
- structure the join so a future SM-approval step slots in without rework — leave the gate as an **unbuilt seam** (do not hard-wire "authenticated → instantly joined").

### Answer
Answered pre-plan by ADR-021 (owner decision 2026-06-14). Status: Resolved.

## Q2 — Guest ↔ Person → ANSWERED (ADR-022)

**Resolution:** **No `Person` for guests.** A `Person` is created only for Google-authenticated participants (Identity mints it at login; Reflekt reads it from the cookie). Guest = `Participant{GuestToken, DisplayName}`, `PersonId` null. No guest-Person creation, **no cross-session matching (no IP, no fingerprinting)**. This resolves the §3↔§10 tension in the proposal toward §10's orphan-avoidance rationale — guests are a free-tier demo edge case; a per-retro `Participant` row suffices.

**RA2 scope impact:** `Participant.PersonId` nullable, set from the cookie for **authenticated participants only**; remove Reflekt `User`/`UserRepository`/`UserConfiguration` + the Google ticket handler; `GetUserId()` → Identity Person id. No guest upsert path. A guest who later authenticates simply gains their Google `Person`; old guest rows are not retro-linked.

### Answer
Answered pre-plan by ADR-022 (owner decision 2026-06-14). Status: Resolved.

## Out of RA2 scope (recorded for the feature backlog)

- **SM waiting room** (ADR-021) — separate feature, needs spec. Open questions for the spec: per-retro approval vs approve-once-per-suite; `JoinRequest` entity vs `Participant` status; the SignalR contract; joiner + SM UI; the conscious departure from proposal §10's zero-friction guest framing.

---

## Q3 — `GetUserId()` repoint changes the identity *value space* of Team/Sprint ownership (the "structurally identical" claim is semantically false)

**From:** developer
**To:** architect
**Status:** Resolved
**Step:** Step 3 — Repoint Reflekt's identity read to Person (and Step 4/6 fallout)
**File:** `Reflekt.API/Auth/ClaimsPrincipalExtensions.cs`, `Reflekt.API/Hubs/SprintHub.cs`, `Reflekt.Domain/Teams/Team.cs`, every `User.GetUserId()` call site (~33)

**Context (grounded in committed RA1):**
Step 3 says *"Ownership comparisons now compare Person ids — structurally identical logic."* That is true syntactically but **not semantically**, and one path is left inconsistent:

1. **Ownership value space flips.** `GetUserId()` today reads `NameIdentifier` = the **Identity User id** (RA1's cookie carries the User id, not the Person id — confirmed `AuthConfiguration.SignInAsync:287-293`). Step 3 repoints it to read **`person_id`** = the **Identity Person id**. Every owner write goes through this:
   - `Team.Create(name, ownerId: User.GetUserId())` → `Team.OwnerId` (`Teams/Behaviors/Team.cs`, `CreateTeamEndpoint:20,38`)
   - `TeamRepository.CountByOwnerIdAsync(ownerId)` / `ExistsByOwnerAndNameAsync(ownerId, …)` (limit + dup checks)
   So after RA2, **new** `Team.OwnerId` rows hold a **Person id**; any **pre-existing** `Team.OwnerId` rows (and the seed Team, Q5) hold an **old Reflekt-User id**. They occupy different id spaces and won't match.

2. **`SprintHub.RequireScrumMasterAsync` is left internally inconsistent (authorization break).** `SprintHub.cs:195-219` resolves the actor as `participant?.UserId` **and**, when null, falls back to the cookie's `NameIdentifier` (`:204` — the Identity **User** id, *not* `person_id`), then compares to `team.OwnerId` (`:219`). It reads `NameIdentifier` **directly**, not via `GetUserId()`, so a mechanical Step-3 rename misses it. Post-RA2 the hub would compare `team.OwnerId` (now a **Person** id, set at creation) against the cookie's **User** id → SM timer control silently 401s for the legitimate owner.

**Question:** For RA2 plumbing, what is the canonical "owner/actor" identity Reflekt keys ownership on — **Person id** (consistent with `Participant.PersonId`) or the Identity **User id**? Specifically:
  (a) Confirm `Team.OwnerId` / `Sprint` ownership / the `{SprintId, UserId}` participant-uniqueness index should all become **Person id** (so Step 3 is genuinely uniform), AND
  (b) the `SprintHub.cs:204` cookie fallback must be repointed to read **`person_id`** (not `NameIdentifier`) so the hub compares like-for-like, AND
  (c) confirm RA2 may **ignore stale pre-existing ownership rows** (dev `reflekt.db` is disposable; Step 7 drops/rebuilds anyway) — i.e. no data backfill is in scope.

**Recommendation:** **Person id everywhere** — (a) yes, all Reflekt ownership keys become Person id; (b) yes, repoint `SprintHub.cs:204` to `person_id`; (c) yes, no backfill — treat dev DB as disposable, note the value-space change in implementation.md as an expected break for any stale row. This keeps Step 3 truly uniform and is the only internally-consistent reading of "a retro participant's identity is the Identity Person" (RA2 intent). The cost is one extra edited file beyond the plan's Step-3 list (`SprintHub.cs`), which the plan's Step-3 "update every call site" already authorizes in spirit.
**Confidence:** medium — the *direction* (Person id) is strongly implied by ADR-016 + `Participant.PersonId` and is almost certainly right; medium (not high) because the plan never names `SprintHub.cs:204`'s `NameIdentifier` fallback or the `Team.OwnerId` value-space flip, so I want the architect to ratify that ownership (not just `Participant`) moves to Person id and confirm the hub edit is in RA2 scope rather than deferred.

### Answer
**Adjudicated by architect (owner-ratified 2026-06-14). Confidence: high.** **Person id everywhere — (a) yes, (b) yes, (c) yes.**

- **(a)** All Reflekt ownership keys become Person id: `Team.OwnerId` (write side `Team.Create(name, GetPersonId())`), every `team.OwnerId != GetPersonId()` ownership check (grep-confirmed: 31 `GetUserId()` call sites, every comparison `team.OwnerId != User.GetUserId()`), `TeamRepository.CountByOwnerIdAsync`/`ExistsByOwnerAndNameAsync`/`GetTeamSummariesByOwnerAsync`, and the `Participant {SprintId, UserId}` filtered-unique index (→ `{SprintId, PersonId}`). **Why this is self-consistent:** both the owner write and the ownership comparison flow through the *same* helper (`GetUserId`→`GetPersonId`), so repointing the helper keeps all endpoint comparisons internally consistent — new `OwnerId` and the compare value both become Person id in the same edit.
- **(b)** **Three helper-bypass sites read `ClaimTypes.NameIdentifier` directly and must be repointed explicitly (corrected after critic round 1 — was stated as "two", off by one).** Two are **owner comparisons** that silently corrupt SM status if missed: `SprintHub.cs:204` (compares to `team.OwnerId` at `:219`) and **`GetSprintSummaryEndpoint.cs:25`** (computes `isScrumMaster = team.OwnerId == userId` at `:29` — the CRITICAL the critic caught: the real owner silently loses SM controls on the summary screen). The third pair are presence-checks (`EditCard:61`, `DeleteCard:61`) repointed for grep-uniformity. All repoint to the **`person_id`** claim. All are RA2 scope, enumerated in Step 3's exhaustive grep-acceptance table. (`CreateCard:59` reads `ClaimTypes.Email`, a different claim — left as-is.)
- **(c)** No backfill. Dev `reflekt.db` is disposable and Step 7's migration rebuilds the schema; any stale pre-existing `OwnerId` row holding an old Reflekt-User id is an expected, documented break (note it in implementation.md), not a defect to repair.

This is the only reading consistent with ADR-016/022 ("a retro participant's identity is the Identity Person"). Status: Resolved.

## Q4 — Step 4 mechanism: there is no clean Reflekt-only route prefix and only ONE FastEndpoints pipeline — how should `ActiveUserPreProcessor` be scoped?

**From:** developer
**To:** architect
**Status:** Resolved
**Step:** Step 4 — Scope `ActiveUserPreProcessor` off Reflekt (plan self-marks **Confidence: medium**)
**File:** `Host/Program.cs:48-54`, `Identity.API/Auth/ActiveUserPreProcessor.cs`

**Context (grounded in committed RA1) — three facts the plan's Step-4 options assume away:**

1. **No clean route discriminator.** Reflekt's routes are **not** under a Reflekt-only prefix. They are `/api/sprints/*`, `/api/teams/*`, `/api/phase-actions`, `/health`. **Fokus collides on `/api/sprints/*`** — Fokus has `GET /api/sprints`, `PUT /api/sprints/{sprintId}`, `GET /api/sprints/{sprintId}/qa-metrics`, etc. So a route-prefix `StartsWith("/api/sprints")` skip would **wrongly disable the gate for Fokus's sprint endpoints too**. Route-prefix scoping (the plan's first-named mechanism) is therefore **not viable** as written.
2. **No FastEndpoints group/tag discriminator exists.** No `: Group`/`: SubGroup`/`EndpointGroup` class anywhere in Reflekt (grep: zero hits). Endpoints carry `[Tags("Participants")]` etc. but tags are not a reliable pre-processor filter and aren't Reflekt-unique.
3. **One shared pipeline, registered globally.** `Program.cs:48-54` wires `ActiveUserPreProcessor` as a **global** pre-processor inside the single `app.UseFastEndpoints(...)` that serves **all three** modules (Identity+Fokus+Reflekt are one `AddFastEndpoints` assembly set, `Program.cs:20-28`). So the plan's alternative — *"register the gate as a non-global pre-processor on the Identity/Fokus endpoint groups only"* — has no per-module hook; there is no Reflekt-less FastEndpoints instance to attach it to, and FastEndpoints global pre-processors can't be excluded per-assembly out of the box.

**Mitigating fact (makes guests safe regardless):** Reflekt's `GuestTokenAuthenticationHandler` sets claims `participantId`/`sprintId`/`displayName` and **no `NameIdentifier`**. So a guest already hits the pre-processor's `int.TryParse(NameIdentifier)` → `return` (allow) at `ActiveUserPreProcessor.cs:18-19`. **Guests are not 401'd today.** The real Step-4 target is the **forward-compatible** case (ADR-021's authenticated Person-without-User), plus avoiding a 401 for a Reflekt request that *does* carry an Identity cookie whose User is inactive but is acting only as a retro participant.

**Question:** Which scoping mechanism should RA2 implement, given the above? Options:
  - **(A) Assembly/endpoint-definition check in the pre-processor.** Inside `ActiveUserPreProcessor`, early-return when the matched endpoint belongs to the Reflekt API assembly (e.g. inspect `context.HttpContext.GetEndpoint()` → FastEndpoints `EndpointDefinition` → declaring type's assembly == `Reflekt.API`). Robust, Reflekt-exact, no route guesswork, forward-compatible. Slightly white-box (reaches into FE internals).
  - **(B) Route-prefix skip on a Reflekt-distinct subset only.** Doesn't work cleanly — `/api/sprints` and `/api/teams` are shared/generic; rejected per fact 1.
  - **(C) Scheme-based skip (the plan's named RA2 fallback).** Skip when the principal's primary scheme is the guest scheme (or non-cookie). The plan itself flags this is **not** forward-compatible with authenticated Person-without-User participants — but for RA2 (which has *no* such participants yet — Q6/ADR-021 defers them) it is behavior-equivalent and trivial. Risk: a future feature must revisit it.
  - **(D) Re-route Reflekt under a distinct prefix** (e.g. `/api/retro/*`) so a clean route skip exists. **Out of RA2 scope** (touches every Reflekt route + frontend, RA5 territory) — flagging only to reject.

**Recommendation:** **(A) assembly/endpoint-definition check.** It is the only mechanism that is simultaneously Reflekt-exact (no Fokus collision), forward-compatible with ADR-021's Person-without-User case (so `adhoc-RetroWaitingRoom` need not revisit it), and contained to the one file the plan already targets. If the architect prefers minimal cleverness for a plumbing pass, **(C) scheme-based** is an acceptable RA2 fallback per the plan's own Risks section — but I'd record a carry-over that the waiting-room feature must harden it. I do **not** recommend (B)/(D).
**Confidence:** medium — (A) is well-supported and I'm fairly confident it's right, but it depends on a FastEndpoints internal (resolving the endpoint's declaring assembly from `IPreProcessorContext`/`HttpContext`) that I should prototype at implementation; if that handle proves awkward, the plan-sanctioned (C) is the safe fallback. Medium because the plan explicitly left this "verify at implementation" and the mechanism choice has a forward-compat consequence the architect should own.

### Answer
**Adjudicated by architect (owner-ratified 2026-06-14). Confidence: high on the ruling.** **Mechanism (A) — assembly-of-declaring-endpoint — is the RA2 mechanism. (C) scheme-based is REJECTED, not even a fallback.**

- **(A) ratified.** Inside `ActiveUserPreProcessor.PreProcessAsync`, early-return when the matched endpoint's declaring type belongs to the `Reflekt.API` assembly. Resolve via `context.HttpContext.GetEndpoint()` → FastEndpoints `EndpointDefinition` (`endpoint.GetEndpointDefinition()` / the `EndpointDefinition` feature) → declaring `EndpointType.Assembly`. This is the only mechanism that is simultaneously Reflekt-exact (no `/api/sprints/*` Fokus collision) and forward-compatible with ADR-021's authenticated-Person-without-User case.
- **(C) rejected — including as a fallback.** ADR-021 *requires* the scoped gate to pass an authenticated Person-without-User participant (the waiting-room case). A scheme-based skip (skip non-cookie principals) fails exactly that case, because such a participant carries the Identity cookie. Adopting a known-forward-incompatible mechanism for the very seam this pass is told to preserve (ADR-021 "leave compatible plumbing") is self-contradictory — it manufactures the debt the pass exists to avoid. So (C) is off the table even temporarily.
- **(B)/(D) rejected** as the developer recommended (no clean route discriminator; re-routing is RA5 scope).
- **Implementation-handle risk (the one open item):** the FastEndpoints API for reaching the declaring assembly from the pre-processor context is the part to prototype first. If that handle proves awkward, **do not fall to (C)** — raise it back to the architect (a new question), since the fallback must still satisfy the ADR-021 forward-compat invariant (e.g. an assembly check via a marker interface on Reflekt endpoints, or an `IEndpointFeature`-based discriminator), never a scheme skip.

Status: Resolved.

## Q5 — `DbInitializer` seeds a Reflekt `User` and owns the seed Team by `user.Id`; Step 1's deletion breaks it with no replacement specified

**From:** developer
**To:** architect
**Status:** Resolved
**Step:** Step 1 (remove `User`) + Step 6/7 (join + migration) — seed-data fallout
**File:** `Reflekt.Persistence/DbInitializer.cs:17-39`

**Context (grounded in committed RA1):**
`DbInitializer.SeedAsync` (run at startup via `SeedReflektDataAsync`, `Program.cs:37`) **creates a seed `User`** (`User.Create(seedEmail,…)`, lines 20-29) and uses it as the seed **Team owner**: `Team.Create("Dev Team", user.Id)` (line 39). Step 1 deletes the `User` aggregate, `Users` DbSet, and `UserRepository` — which **breaks this seeder's compile and its Team-owner source**. The plan's Step-1 grep list (`UserRepository`, `Reflekt.Domain.Users`, `GetByEmailAsync`, deleted endpoint types) would catch the `Reflekt.Domain.Users` import here, but the plan gives **no instruction for what the seed Team's `OwnerId` becomes** once Users are gone. This is the same identity-value-space question as Q3, applied to seed data.

**Question:** After removing Reflekt's `User`, how should `DbInitializer` seed Team ownership?
  (a) Drop the seed-`User` block entirely and own the seed Team by a **fixed sentinel Person id** (e.g. a documented constant, since RA2 has no Identity↔Reflekt seed handshake), OR
  (b) Keep the seed Team but with `OwnerId = 0`/a placeholder and document that a real owner is established on first authenticated `CreateTeam`, OR
  (c) something else the architect prefers (e.g. leave the seed Team un-owned-meaningfully for dev only).

Note: the seed Team only needs to exist so the dev `reflekt.db` has a board to demo (the seed Sprint/Participants/Cards hang off it). No production seeding depends on it.

**Recommendation:** **(a) drop the seed-`User` block; own the seed Team by a documented constant Person id** (e.g. `const int SeedOwnerPersonId = 1`, with a comment that it's a dev-only placeholder not guaranteed to match any real Identity Person, consistent with Q3's "no backfill / dev DB disposable"). This keeps the seeder compiling and the demo board intact with the least machinery, and matches RA2's "plumbing only, no cross-module seed handshake" scope. Defer any real seed-time Person linkage to whenever Identity/Reflekt seed coordination is actually needed (not RA2).
**Confidence:** medium — the mechanical need (remove the `User` seed, pick *some* owner value) is certain; medium on the *specific* placeholder because it's a dev-only cosmetic choice the architect may want to standardize (sentinel value vs leaving it 0), and it's coupled to Q3's ownership-value-space ruling — if Q3 says ownership stays User-id-based, this answer changes accordingly.

### Answer
**Adjudicated by architect (owner-ratified 2026-06-14). Confidence: high.** **(a) — drop the seed-`User` block; own the seed Team by a named sentinel Person-id constant with a dev-only comment.**

- Remove the seed-`User` creation block (`DbInitializer.cs:17-34`) and the `Reflekt.Domain.Users` import. Replace the Team-owner source `user.Id` with a named constant, e.g. `const int SeedOwnerPersonId = 1;`, with a comment that it is a **dev-only placeholder, not guaranteed to match any real Identity Person** (consistent with Q3(c): no backfill, dev DB disposable). The seed Team exists only so the dev `reflekt.db` has a demo board; the seed Sprint/Participants/Cards hang off it unchanged.
- This is coupled to Q3 as you noted: Q3 ruled Person id everywhere, so the seed owner is a Person id (sentinel), not a User id. Consistent.
- **Defer** any real seed-time Identity↔Reflekt Person handshake to whenever cross-module seed coordination is actually needed — out of RA2 ("plumbing only, no seed handshake").

Status: Resolved.
