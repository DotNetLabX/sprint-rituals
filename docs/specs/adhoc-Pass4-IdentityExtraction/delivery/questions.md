# Identity Extraction (Pass 4) — Questions

All blockers below must be answered before the spec is marked Ready and the architect plans. Context: Pass 4 finishes the half-done Fokus→Identity cut and delivers three deferred seams — invitation redemption-at-login, company-domain auto-seed, and the cross-module Person service. Locked: L1 (Invitation + its 4 features stay in Fokus), L2 (full cut — all three deferred items in scope). Binding: `docs/architecture/decisions.md`, no cross-module project references.

Evidence pointers (current stubs):
- `Identity.API/Auth/AuthConfiguration.cs:88-95` — redemption stub redirects any token to `/invite/error?reason=not-implemented`.
- `AuthConfiguration.cs:116` — company-domain auto-seed is a comment only.
- `Identity.Domain/Users/Events/UserCreated.cs` — unraised placeholder; notes User must become an aggregate root before cross-module person work.

---

## Q1: Redemption entry point
**From:** PO
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis (pre-plan)
**File:** `Identity.API/Auth/AuthConfiguration.cs`

**Context:** An invitation link carries a single-use token. A validation endpoint (`GET /api/invitations/{token}/validate`) already exists. The sign-in callback already reads an `invite_token` from sign-in state but dead-ends on it.

**Question:** Is the intended flow: invited user opens the invite link → app validates the token → user clicks "Sign in with Google" with the token carried through sign-in state → the callback redeems? Or is there a different entry point?
**Recommended default:** Yes — link → validate → Google sign-in carrying the token → callback redeems.

### Answer
Confirmed (default). Flow: invitation link → validate token → "Sign in with Google" carries the token through sign-in state → the callback redeems.

---

## Q2: Email-match enforcement
**From:** PO
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis (pre-plan)
**File:** `Identity.API/Auth/AuthConfiguration.cs`

**Context:** A user could sign in with a Google account whose email differs from the invitation's email.

**Question:** Must the signing-in Google email exactly match the invitation email (reject on mismatch), or accept and bind the user to the signed-in email regardless?
**Recommended default:** Enforce exact match (case-insensitive); reject on mismatch with a specific reason.

### Answer
Confirmed (default). Enforce exact email match, case-insensitive; reject a mismatch with a specific reason (no sign-in).

---

## Q3: Role source on redemption
**From:** PO
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis (pre-plan)
**File:** `Identity.Domain/Users/Behaviors/User.cs`, `Fokus.Domain/Auth/Invitation.cs`

**Context:** Ordinary self-registration defaults a new user to "Manager"; bootstrap forces the first user to "Admin". An invitation carries its own role (Admin or Manager).

**Question:** On redemption, does the new user's role come from the invitation (overriding the ordinary default), with bootstrap still forcing the first user to administrator?
**Recommended default:** Yes — role comes from the invitation; bootstrap rule unchanged.

### Answer
Confirmed (default). Redeemed user's role comes from the invitation; bootstrap still forces the first user to Admin.

---

## Q4: Person reuse vs. create on redemption
**From:** PO
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis (pre-plan)
**File:** `Identity.Domain/Persons/Behaviors/Person.cs`

**Context:** A Person may already exist for the invited email (e.g. previously synced from Jira, `Source=Jira`, no linked User). Persons carry a source and an optional User link.

**Question:** If a Person already exists for the invited email, link a new User to that existing Person (and leave its source as-is), or always create a fresh Person on redemption?
**Recommended default:** Reuse the existing Person and link a User to it; create a new Person only when none exists.

### Answer
Confirmed (default). Reuse an existing Person for that email (link a new User, leave its source as-is); create a Person only when none exists.

---

## Q5: Redemption failure UX (and already-registered user)
**From:** PO
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis (pre-plan)
**File:** `Identity.API/Auth/AuthConfiguration.cs`

**Context:** Redemption can fail (expired / used / revoked / not-found / email-mismatch). The existing stub redirects to `/invite/error?reason=...`; the regular-login path uses `/login?error=...`. An already-registered active user might also click an invite link.

**Question:** On failure, use dedicated invite-error destinations carrying the specific reason, or fall back to the generic login-error destination? And when an already-registered active user clicks an invite link — ignore the token and log them in normally, or surface "already a member"?
**Recommended default:** Dedicated `/invite/error?reason=...` for redemption failures (specific reason); for an already-registered active user, ignore the token and log in normally.

### Answer
Confirmed (default). Redemption failures redirect to a dedicated `/invite/error?reason=...` carrying the specific reason; an already-registered active user who clicks an invite link has the token ignored and is logged in normally.

---

## Q6: Company-domain auto-seed trigger
**From:** PO
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis (pre-plan)
**File:** `Identity.API/Auth/AuthConfiguration.cs:116`, `Fokus.Domain/Settings/AppSettings.cs`

**Context:** `CompanyDomain` (Fokus AppSettings) is currently always null until set by hand, and only enforced in invitation creation.

**Question:** Seed the company domain on bootstrap only (first administrator's first sign-in), or also reconcile later (seed if still unset on a subsequent admin login)?
**Recommended default:** Bootstrap only — seed once, at first-administrator provisioning.

### Answer
Confirmed (default). Seed on bootstrap only — the first administrator's first sign-in. No later reconciliation.

---

## Q7: Company-domain auto-seed source & public-domain exclusion
**From:** PO
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis (pre-plan)
**File:** `Identity.API/Auth/AuthConfiguration.cs`, `Fokus.Domain/Settings/AppSettings.cs`

**Context:** A company-domain gate is only meaningful for a real company domain; public providers (gmail.com, outlook.com, …) would make the gate trivially permissive or accidentally lock everyone out.

**Question:** Derive the seed value from the bootstrap administrator's email domain (the part after `@`)? And do we exclude public providers — skip seeding for those (leave it null/manual), or seed anyway?
**Recommended default:** Derive from the admin email domain; skip seeding when it is a known public provider (leave unset for manual configuration).

### Answer
Confirmed (default). Derive the seed from the bootstrap admin's email domain; skip seeding for known public providers (gmail.com, outlook.com, …) — leave unset for manual configuration in that case.

---

## Q8: Cross-module write mechanism for the seed
**From:** PO
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis (pre-plan)
**File:** `Identity.API/Auth/AuthConfiguration.cs`, `Fokus.Domain/Settings`

**Context:** `CompanyDomain` lives in `fokus.db`; Identity must not reference Fokus directly (ADR-001). MassTransit in-process integration events already exist (`SprintSynced`); gRPC code-first contracts also exist (`Blocks.Contracts`).

**Question:** Seed the company domain via an integration event (Identity announces "first admin registered with domain X" → Fokus consumes and seeds its settings), or via the cross-module service contract (a call into Fokus)?
**Recommended default:** Integration event (fire-and-forget, matches the existing `SprintSynced` pattern; no synchronous coupling for a one-time seed).

### Answer
Confirmed (default). Seed via an integration event — Identity announces the first admin's domain; Fokus consumes and seeds its company-domain setting — riding the existing in-process transport.

---

## Q9: Cross-module Person service — direction & purpose
**From:** PO
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis (pre-plan)
**File:** `Blocks.Contracts/Grpc`, `Identity.Domain/Persons`

**Context:** `Identity/CLAUDE.md` lists a deferred `PersonGrpcService` as "cross-module Person upsert." Existing gRPC contracts run Fokus(server)→Host(client). Fokus syncs developers from Jira; unifying suite-wide Person identity is the stated motivation.

**Question:** What is its direction and purpose — Fokus→Identity (Fokus upserts a Person when it syncs a Jira developer, so identity is unified in Identity), Identity→Fokus, or a read/lookup (one module queries the other for a Person by email/id to enrich its data)?
**Recommended default:** Fokus→Identity upsert — when Fokus syncs a Jira developer, it upserts a Person into Identity (source = Jira), unifying suite-wide person identity in the Identity module.

### Answer
Confirmed (default). Direction is Fokus→Identity upsert — Fokus upserts a Person (source = Jira) into Identity when it syncs a Jira developer; Identity is the single home of suite-wide Person identity.

---

## Q10: Cross-module Person service — scope this pass
**From:** PO
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis (pre-plan)
**File:** `Blocks.Contracts/Grpc`

**Context:** The contract could be minimal (only what redemption + auto-seed need) or the full Person-exchange surface.

**Question:** Limit the cross-module Person contract to what redemption + auto-seed actually require this pass, or deliver the full Person-exchange contract regardless of immediate consumers?
**Recommended default:** Deliver the full upsert/lookup contract chosen in Q9 (L2 is a "full cut"), but wire only the consumers the in-scope flows need; leave unused operations defined but unconsumed.

### Answer
Confirmed (default). Define the full upsert/lookup contract, but wire only the consumers the in-scope flows need; unused operations are defined but left unconsumed.

---

## Q11: Promote User to drive cross-module side effects
**From:** PO
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis (pre-plan)
**File:** `Identity.Domain/Users/User.cs`, `Identity.Domain/Users/Events/UserCreated.cs`

**Context:** `UserCreated` exists as an unraised placeholder; its doc-comment states cross-module person work requires promoting `User` from an entity to an aggregate root so the event can dispatch. Pass 5 already touched event correctness but left this deferred.

**Question:** Promote the User concept so user-creation can raise its creation event and drive cross-module side effects (company-domain seed and/or Person sync) in this pass, or keep it deferred?
**Recommended default:** Promote it this pass — L2 is the full cut, and the seed/sync side effects are exactly what the placeholder was reserved for.

### Answer
Confirmed (default). Promote User to an aggregate root this pass so its creation event is raised on provisioning and drives the company-domain seed + cross-module Person sync via the bus.

---

## Q12: Frontend scope
**From:** PO
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis (pre-plan)
**File:** `client/`

**Context:** Pass 4 is structural/infra. Redemption and errors are currently server redirects to `/invite/...` and `/login?error=...` paths.

**Question:** Does this pass include client changes (an `/invite/{token}` landing page, invite-error pages), or is it API/redirect-only with the frontend deferred?
**Recommended default:** API/redirect-only this pass; client landing/error pages deferred to a follow-up unless you want them now.

### Answer
Confirmed (default). API/redirect-only this pass; client invite landing/error pages are deferred to a follow-up.

---

## Q13: Cross-module mechanism for synchronous invitation redemption (Identity → Fokus)
**From:** architect
**To:** PO → user (scope change)
**Status:** Answered
**Step:** Phase 1 analysis (pre-plan)
**File:** `Identity.API/Auth/AuthConfiguration.cs:88-95`, `Fokus.Domain/Auth/Behaviors/Invitation.cs`, `Fokus.API/Features/Auth/ValidateInvitation/`, `Blocks.Contracts/Grpc`

**Context:** This is the load-bearing gap that blocks planning. The spec names exactly two sanctioned cross-module mechanisms and binds each to one direction/purpose:
- **Integration event** (Q8/BR 10) — Identity → Fokus, company-domain seed only. Fire-and-forget; it cannot return a value to the login flow.
- **gRPC `PersonGrpcService`** (Q9/Q10/BR 11) — Fokus → Identity, Person upsert. Wrong direction (Identity is the server here) and wrong domain (Person, not Invitation).

But **redemption is synchronous and Identity-initiated**: inside `OnTicketReceived`, mid-login, Identity must (a) read the Invitation by token from `fokus.db`, (b) confirm it is usable (Pending/unexpired/not-used/not-revoked), (c) obtain its **email and role** to gate the email match and set the user's role, and (d) **mark it Accepted** so it cannot be redeemed twice (AC: "the same link cannot be redeemed a second time"). None of the two named mechanisms can do this:
- An integration event cannot return the email/role synchronously to gate the login, and cannot block sign-in on an invalid token.
- The named gRPC contract is `PersonGrpcService`, Fokus→Identity — the opposite direction.

Codebase facts (verified):
- `Fokus.Domain/Auth/Behaviors/Invitation.cs` has `Accept(DateTime now)` but **no endpoint or cross-module path consumes it today** — it is unreachable.
- The only token-scoped Fokus surface is the **anonymous REST** endpoint `GET /api/invitations/{token}/validate` (returns Valid + email + reason, but **not** the role, and does **not** mark Accepted).
- `docs/proposals/suite-integration-architecture.md` §"No cross-database joins" (line 154): *"All cross-module data access goes through gRPC or integration events"* — direct in-process HTTP calls and shared-DI across the boundary are **not** sanctioned by the architecture.
- There is **no `IInvitationService` gRPC contract** anywhere, and Identity.API has **no project reference** to Fokus (correct per ADR-001) and must keep it that way.

**Question:** What sanctioned mechanism does Identity use to synchronously validate-and-accept a Fokus invitation by token during login? The architecture (§154) restricts this to gRPC or integration events; only gRPC is synchronous, so this implies a **second gRPC contract** the spec never named:

- **Option A (recommended — consistent with the architecture and Q9's pattern):** Add a new gRPC code-first contract `IInvitationService` in `Blocks.Contracts`, **Fokus = server, Identity = client** (the reverse direction of `PersonGrpcService`). It exposes a redeem operation — given a token + signing-in email, Fokus validates, enforces the email match, calls `Invitation.Accept(...)`, and returns the outcome (success + role, or a specific failure reason) in one round trip. Consequence: this pass stands up **two** new gRPC contracts going **opposite directions** — Identity hosts the Person server *and* acts as the Invitation client; Fokus hosts the Invitation server *and* acts as the Person client. This is materially more wiring than "one new gRPC service" and was not stated in the spec scope.
  - Sub-decision A1: does Fokus do the email-match + Accept server-side and return role (recommended — keeps Invitation behavior in Fokus, one round trip, atomic), or does Identity fetch the invitation read-only over gRPC and a separate accept call mark it Accepted (two round trips, accept not atomic with the match)?

- **Option B:** Identity calls Fokus's existing **in-process REST** endpoint(s) over `HttpClient` (loopback). This contradicts architecture §154 (only gRPC/integration events) and still requires a *new* accept-by-token endpoint that returns the role and marks Accepted, plus an HTTP client the architecture doesn't otherwise use. Not recommended.

- **Option C (split, async):** Identity validates synchronously via a gRPC read, signs the user in, and marks Accepted via a fire-and-forget integration event afterward. Rejected by AC "the same link cannot be redeemed a second time" — a window exists where the token is still Pending after sign-in, and the role still has to come back synchronously, so a read contract is needed anyway. Not recommended.

**Recommended default:** Option A with sub-decision A1 (server-side validate + email-match + Accept, returning role in one gRPC call). This keeps all Invitation behavior in Fokus (honoring L1), uses the architecture's sanctioned synchronous mechanism, and mirrors the `PersonGrpcService` pattern in reverse. **Note for scope:** confirming A means the spec's "cross-module Person service (new)" expands to **two** new gRPC contracts in opposite directions. Please confirm this is in scope for Pass 4, or whether redemption should be re-scoped.

### Answer
**Build it now — Option A + sub-decision A1** (user, 2026-06-09; escalated as a scope change). Add a gRPC code-first `IInvitationService` contract in `Blocks.Contracts`, **Fokus = server / Identity = client** (opposite direction of `PersonGrpcService`). Fokus performs server-side validate + email-match (case-insensitive) + `Invitation.Accept(now)` and returns the outcome (success + role, or a specific failure reason) in **one atomic round trip**. Pass 4 scope is accepted as **two opposite-direction gRPC contracts** + gRPC client infra (Q15) + User→aggregate-root promotion + migration. Redemption is **not** deferred; the not-implemented stub dies this pass.

---

## Q14: Where does the company-domain seed announcement originate — UserCreated event or bootstrap inline?
**From:** architect
**To:** PO
**Status:** Answered
**Step:** Phase 1 analysis (pre-plan)
**File:** `Identity.API/Auth/AuthConfiguration.cs:99-119`, `Identity.Domain/Users/Events/UserCreated.cs`

**Context:** BR 12 says user provisioning raises `UserCreated`, and "that event drives **only** the company-domain seed announcement." But the seed must be derived **only at first-administrator bootstrap** (BR 9 — "exactly once, at first-administrator bootstrap"; not on later logins, not on ordinary redemption). `UserCreated(int UserId)` carries only the user id — a handler reacting to it would have to re-load the user, re-load the person for the email, and re-check "is this the bootstrap admin (user count was 0 / first active admin)" to decide whether to seed. The bootstrap branch in `HandleTicketReceivedAsync` (lines 99-119) is the only place that already knows "this is the first user."

Two consistent readings:
- **(a)** `UserCreated` is raised on **every** user provisioning (bootstrap + redemption), and a single `IEventHandler<UserCreated>` decides whether to announce the seed by checking bootstrap-ness. Cleaner event model, but the handler needs enough context to identify the bootstrap admin and the email domain, and must publish the integration event from inside the in-process domain-event handler.
- **(b)** `UserCreated` is raised on every provisioning but only drives **non-seed** concerns; the **seed announcement is published inline in the bootstrap branch** (where "first user" is already known), and the event's stated job ("drives only the seed") is really "is reserved for the seed-class side effects, published at bootstrap." This contradicts BR 12's literal wording.

**Question:** Should the company-domain seed integration event be published by an `IEventHandler<UserCreated>` (reacting to the event, option a), gated on bootstrap-ness inside the handler — or published inline in the bootstrap branch (option b)? If (a), what does the handler use to decide "this is the bootstrap admin" — the user's role == Admin AND active-admin-count == 1, or a dedicated flag/event payload?
**Recommended default:** Option (a) — an `IEventHandler<UserCreated>` publishes the `CompanyDomainSeeded`/`FirstAdministratorRegistered` integration event, gated on "this user is the sole active Admin (bootstrap)"; it derives the domain from the person's email using the **same `email.Split('@')[1]` extraction** the Fokus invitation gate uses (`CreateInvitationEndpoint.cs:21`), and skips known public providers. This keeps the bootstrap branch thin and honors BR 12's "the event drives the seed." Please confirm the bootstrap-detection predicate.

### Answer
**Option (a)** (architect default; resolved by team-lead — design-internal, BR-12-aligned, no scope/decision reversal, no user input needed). An `IEventHandler<UserCreated>` publishes the company-domain seed integration event, gated on the bootstrap predicate **`user.Role == Admin AND active-admin-count == 1`** (sole active Admin). Domain derived via the same `email.Split('@')[1]` extraction as `CreateInvitationEndpoint.cs:21`; known public providers skipped (leave unset). Bootstrap branch stays thin.

---

## Q15: gRPC client infrastructure does not exist in this repo — confirm building it is in scope
**From:** architect
**To:** PO
**Status:** Answered
**Step:** Phase 1 analysis (pre-plan)
**File:** `Directory.Packages.props`, `BuildingBlocks/Blocks.AspNetCore`, `Fokus.API/DependencyInjection.cs`, `Identity.*`

**Context:** Both cross-module gRPC seams require a gRPC **client**, which this repo has never wired. Verified facts:
- `Directory.Packages.props` declares only `protobuf-net.Grpc.AspNetCore` (server). The **client** packages (`protobuf-net.Grpc`, `Grpc.Net.Client`/`Grpc.Net.ClientFactory`) are present in the *reference* repo's CPM but **absent** here.
- `Blocks.AspNetCore` in this repo has **no** gRPC client registration helper (the reference's `GrpcClientRegistrationExtensions` / `GrpcServicesOptions` do not exist here).
- The existing gRPC setup is **server-only** (Fokus hosts; the catalog/analytics clients were deferred "with the Reflekt port").
- There is a single shared h2c Kestrel endpoint (`Grpc` → `http://localhost:5001`); both modules' gRPC servers map onto the same in-process Host, and clients dial `localhost:5001` (in-process loopback).

So the gRPC client steps are a **Build** disposition (skill references infra that doesn't exist locally): add the client package(s) to CPM, add a client-registration helper to `Blocks.AspNetCore`, then follow the `create-grpc-contract` Client workflow.

**Question:** Confirm that building the missing gRPC client infrastructure (CPM package additions + a `Blocks.AspNetCore` client-registration helper + the `Grpc` URL config the client reads) is in scope for Pass 4, and that the in-process loopback over `localhost:5001` is the intended transport for both new contracts (no separate ports).
**Recommended default:** Yes — building the client infra is in scope (it is a prerequisite for both Q9's Person upsert and Q13's invitation redemption); reuse the single `localhost:5001` h2c endpoint for both contracts.

### Answer
**Yes — in scope** (resolved by team-lead; it is a forced prerequisite of the already-locked Q9 Person upsert, independent of Q13). Add the gRPC client package(s) to `Directory.Packages.props` (`protobuf-net.Grpc` + `Grpc.Net.ClientFactory`), add a client-registration helper to `Blocks.AspNetCore`, and wire the `Grpc` URL config the client reads. Reuse the single `localhost:5001` h2c endpoint for **both** new contracts — no separate ports.
