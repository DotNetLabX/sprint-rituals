# Identity Extraction — Finish the Fokus → Identity Cut

**Traces to:** `docs/architecture/decisions.md` ADR-015 (Identity extraction in-flight), ADR-001 (modular monolith), ADR-004 (light stack); `docs/proposals/refactor-triage.md` item A2; `docs/backlog.md` Pass 4.
**Source:** Scratch (refactor program, Pass 4)
**Dependencies:** Passes 0–3c (Done) — CPM, skill repair, slice hygiene, domain enrichment, event-correctness all landed. Person aggregate, User entity, Google OAuth + cookie auth, and the Fokus Invitation entity already exist.
**Status:** Ready
**Plan:** docs/specs/adhoc-Pass4-IdentityExtraction/delivery/plan.md

---

## Purpose

Identity extraction is the central in-flight migration of the refactor program: auth moved out of Fokus into the Identity module, but three cross-module seams were stubbed and deferred rather than finished. This feature completes the cut by delivering the three deferred behaviors — **invitation redemption at login**, **company-domain auto-seed**, and the **cross-module Person service** — so a person can be invited, redeem the invite by signing in with Google, and be provisioned end-to-end without the login flow ever short-circuiting to a "not-implemented" page. The locked scope decision (communication-log L1) keeps the Invitation concept and its four management features in Fokus; the redemption flow therefore must bridge Identity (login) and Fokus (invitation) across the module boundary, never by a direct project reference.

## Entities

This feature does not introduce new aggregates. It extends existing concepts, promotes one (User) to an aggregate root so its creation event can dispatch, and adds one cross-module contract. Cross-module interactions use the two sanctioned mechanisms: a MassTransit integration event for fire-and-forget propagation, and a gRPC code-first cross-module Person service for synchronous exchange (both consistent with the existing `SprintSynced` event and the existing gRPC code-first contracts in `Blocks.Contracts`). No direct project reference crosses the module boundary.

### Invitation (owned by Fokus — stays per L1)
- Existing concept — an entity with behavior methods (create, accept, revoke), not promoted in this pass: an email address, a role (Admin or Manager), the inviter, a single-use token, created/expiry timestamps, an acceptance timestamp, and a status (Pending, Accepted, Revoked).
- This feature exercises the existing **acceptance** behavior: an invitation can only be accepted while Pending and not past expiry; accepting records the acceptance time and moves status to Accepted.
- An invitation is usable for redemption only when Pending, unexpired, not already used, and not revoked.

### Person (owned by Identity)
- Existing aggregate: name, email, optional picture, a source (Google, Jira, …), an external id, and an optional link to a User.
- **Email is optional (nullable) for non-Google sources.** Jira Cloud does not expose a developer's email, so Jira-sourced Persons are identified by their `ExternalId` (the Jira account id, with source = Jira), and email is filled only best-effort when Jira happens to expose it. Google-sourced Persons continue to be identified by email (stored lower-cased, treated case-insensitively).
- During redemption (a Google sign-in), if a Person already exists for the invited email, the feature **links a new User to that existing Person** and leaves its source unchanged; a new Person is created only when none exists.
- The Person aggregate is the single, suite-wide home of person identity: when Fokus syncs a developer from Jira, that person is reconciled into the Identity Person store (source = Jira) via the cross-module Person service, keyed on `ExternalId` rather than email.

### User (owned by Identity)
- Existing concept: link to a Person, a role (string claim), an active flag, registration date, last-login.
- This feature provisions a User for a redeemed invitation, taking its role from the invitation rather than the default.
- **User is promoted from a plain entity (`Entity<int>`) to an aggregate root** this pass, so it carries a domain-events collection and its creation event actually dispatches through the existing save-changes interceptor on the in-process event bus. (This is the locked structural prerequisite — without the promotion the creation event cannot be raised; see ADR-007 rich-aggregate shape.)
- The User-creation event drives **only** the company-domain seed announcement (the bootstrap side effect — see Company domain below). It does **not** trigger the Fokus→Identity Person reconciliation; that is a separate, Fokus-initiated flow (see Cross-module Person contract).

### Company domain (a setting owned by Fokus)
- A single optional company-domain string on Fokus application settings. When set, it gates who can be invited: an invitee's email domain must match it.
- Today it is only ever set by hand. This feature **auto-seeds** it once, at first-administrator bootstrap, from the bootstrap administrator's email domain — unless that domain is a known public provider, in which case it is left unset for manual configuration.
- The domain is extracted from the email the **same way the existing invitation gate extracts it** (the segment after the `@`), so the seeded value is guaranteed to match what the gate later compares against. Comparison is case-insensitive.
- The authoritative public-provider exclusion list lives as a single constant in the Identity module. It is **non-exhaustive by design** — it names the common consumer providers (e.g. gmail.com, outlook.com) and is extended as needed; an unlisted provider simply seeds normally.

### Cross-module Person contract (new)
- The cross-module mechanism is the **gRPC code-first `PersonGrpcService`**, its contract defined in `Blocks.Contracts` alongside the existing gRPC contracts. Direction is **Fokus → Identity**: Fokus upserts a Person (source = Jira) into Identity when it syncs a Jira developer.
- This **reverses the existing gRPC direction** (today Fokus is the server and the Host is the client). This pass makes **Identity the gRPC server** that hosts the Person upsert, and **Fokus the client** that calls it. Standing up an Identity-hosted gRPC server is new, non-trivial work, not a copy of the existing wiring.
- The contract defines the **full upsert and lookup surface**, but only one consumer is wired this pass: the **Fokus Jira-developer sync path is modified to call the upsert** — that is the single in-scope consumer. Lookup and any other defined operations remain unconsumed until a later increment.

## User Flows

```
Flow 1: Invitation redemption at login (the deferred seam being finished)
1. An administrator creates an invitation for a colleague (existing feature, unchanged).
2. The colleague opens the invitation link, which carries the single-use token.
3. The system confirms the invitation is still usable (Pending, unexpired, not used, not revoked).
4. The colleague chooses "Sign in with Google"; the invitation token is carried through the sign-in.
5. On return from Google, the system re-confirms the invitation is usable and checks that the
   signing-in Google email exactly matches the invitation email (case-insensitive).
6. The system provisions the colleague's identity: a User is created with the role from the
   invitation, linked to an existing Person for that email if one exists (otherwise a new Person
   is created), and the invitation is marked Accepted.
7. The colleague is signed in and lands on the application home.
8. If the invitation is unusable (expired / used / revoked / not-found) or the email does not match,
   the colleague is redirected to a dedicated invite-error destination carrying the specific reason
   and is not signed in.
9. If the person clicking the invite link is already a registered, active user, the token is ignored
   and they are simply logged in normally.

Flow 2: First-administrator bootstrap with company-domain auto-seed
1. On the very first sign-in to a fresh system, the first user is provisioned as the administrator
   (existing behavior).
2. As part of that provisioning, the system derives a candidate company domain from the
   administrator's email domain. If it is a known public provider (e.g. gmail.com, outlook.com),
   no seeding happens (the setting stays unset for manual configuration). Otherwise the derived
   domain is announced to Fokus, which seeds its company-domain setting.
3. From then on, invitation creation enforces that company domain (existing enforcement, now
   populated automatically rather than by hand).

Flow 3: Cross-module Person reconciliation (Fokus → Identity)
1. The Fokus Jira-developer sync path (modified this pass) calls the gRPC PersonGrpcService to upsert
   that developer as a Person in Identity (source = Jira). Identity hosts the gRPC server; Fokus is
   the client — a reversal of the existing Fokus-server/Host-client direction.
2. Identity becomes the single suite-wide home of person identity; a later invitation redemption for
   the same email reuses that Person rather than creating a duplicate.
3. No direct project reference crosses the module boundary; the exchange is over the gRPC contract in
   Blocks.Contracts only.
```

## API Surface

The redemption and bootstrap behaviors complete the existing Google sign-in callback flow and its redirects — they add no new browser-facing REST endpoints beyond what already exists. The existing surface this feature touches:

| Method | Route | Auth | Request body | Response | Status codes |
|--------|-------|------|-------------|----------|-------------|
| GET | `/api/invitations/{token}/validate` | Anonymous | — | Validity + invited email, or a reason (not-found, used, revoked, expired) | 200 |
| GET | `/auth/callback` (Google return; the invitation token rides through sign-in state as the `invite_token` auth property) | Anonymous | — | Redirect (302) to home on success, or to an invite-error destination on failure | 302 |
| POST | `/api/invitations` | Admin | Email + optional role | Created invitation + link | 201, 400 (domain mismatch), 409 (duplicate pending) |

Redemption outcomes (the destination surfaced to the user):
- Invitation not usable (expired / used / revoked / not-found) → redirect to a dedicated invite-error destination carrying the specific reason; not signed in.
- Signing-in email does not match the invitation → redirect to a dedicated invite-error destination with an email-mismatch reason; not signed in.
- Already-registered active user clicks an invite link → token ignored; signed in normally; redirected home.
- Success → invitation marked Accepted, user provisioned, signed in, redirected home.

The cross-module Person contract is the gRPC code-first `PersonGrpcService` (Fokus → Identity, Identity-hosted server), defined in `Blocks.Contracts` — not part of the browser-facing REST surface.

## Real-time Events

This feature produces no real-time (browser push) events. It uses one internal **integration event** between modules for the company-domain seed: Identity announces the first administrator's company domain, and Fokus consumes it to seed its company-domain setting. This rides the existing in-process transport and is not browser-facing.

## Business Rules

1. An invitation can be redeemed only while it is Pending, unexpired, not already used, and not revoked. Any other state blocks redemption with a specific reason.
2. Redemption consumes the invitation exactly once: a successful redemption marks the invitation Accepted, and the same token cannot be redeemed again.
3. A redeemed user's role is taken from the invitation (Admin or Manager), not from the default applied to ordinary self-registration.
4. The first user to sign in to a fresh system is provisioned as administrator (existing bootstrap rule, unchanged).
5. The company-domain setting, once seeded, gates invitation creation: an invitee's email domain must match it (existing rule, unchanged) — this feature only changes how the setting first gets populated.
6. Email is matched case-insensitively throughout (consistent with existing Person email handling).
7. The signing-in Google account's email must exactly match the invitation's email (case-insensitive). A mismatch blocks redemption with an email-mismatch reason and does not sign the user in.
8. Redemption reuses an existing Person for the invited email when one exists (linking a new User to it, leaving its source unchanged); a new Person is created only when none exists.
9. The company domain is auto-seeded exactly once, at first-administrator bootstrap, derived from that administrator's email domain — extracted the same way the existing invitation gate extracts it (the segment after the `@`), so the seeded value matches what the gate later compares — unless the domain is a known public provider, in which case the setting is left unset for manual configuration. It is not reconciled on later logins.
10. The company-domain seed is delivered to Fokus by a MassTransit integration event over the in-process transport; Identity never writes to Fokus's settings store directly.
11. Cross-module person identity is reconciled in one direction over the gRPC `PersonGrpcService` (Identity-hosted server, Fokus client — a reversal of the existing Fokus-server/Host-client direction): the Fokus Jira-developer sync path upserts a Person (source = Jira) into Identity when it syncs a developer. Jira-sourced Persons are matched and created by `(Source=Jira, ExternalId)` — the Jira account id — because Jira Cloud does not expose developer email; email is filled best-effort when Jira returns it and is not required for these Persons. Google-sourced Persons are matched by email. Identity is the single suite-wide home of person identity.
12. User provisioning raises a User-creation domain event (now possible because User is an aggregate root — see Entities). That event drives **only** the company-domain seed announcement; the login flow does not perform the seed inline. The Fokus→Identity Person reconciliation (Rule 11) is a separate, Fokus-initiated upsert on the Jira-sync path and is **not** driven by the User-creation event.
13. No cross-module interaction is implemented as a direct project reference from one module to the other; cross-module data flows only via the MassTransit integration event and the gRPC `PersonGrpcService`. (ADR-001, CLAUDE.md "No cross-module project references".)
14. The login flow never leaves a usable invitation in a dead-end "not-implemented" state — the deferred stub is removed, and every redemption path resolves to either a successful sign-in or a specific, user-visible failure reason.

## Acceptance Criteria

- [ ] Opening a valid invitation link and signing in with the matching Google account provisions a user with the invitation's role, sets the auth cookie, and returns a 302 to the home path — with no redirect to the "not-implemented" error path. (Frontend pages are deferred, so the observable is the cookie + the 302 target, not a rendered home page.)
- [ ] Signing in to redeem with a Google account whose email differs from the invitation email does not set an auth cookie and returns a 302 to the invite-error path carrying an email-mismatch reason query parameter.
- [ ] After a successful redemption, the invitation reports as used (validate endpoint returns the "used" reason) and the same link cannot be redeemed a second time.
- [ ] Redeeming an expired, revoked, already-used, or unknown invitation does not set an auth cookie and returns a 302 to the invite-error path with the specific reason query parameter.
- [ ] When a Person already exists for the invited email, redemption links the new user to that existing Person rather than creating a duplicate (one Person row, now linked to a User).
- [ ] An already-registered active user who clicks an invite link is logged in normally — auth cookie set, 302 to home, no invite-error redirect (token ignored).
- [ ] On a fresh system, the first administrator's sign-in seeds the company-domain setting automatically from the admin's email domain — with no manual configuration step — and does not seed when that domain is a known public provider (the setting stays unset).
- [ ] After auto-seed, creating an invitation for an email outside the company domain is rejected (400), and one inside it succeeds (201) — confirming the seeded value is in effect.
- [ ] After a Fokus Jira sync runs, the synced developer appears as a Person row with `Source=Jira` and `ExternalId` = the Jira account id in the Identity database (`identity.db`) — email populated only if Jira returned it — demonstrating the Fokus client → Identity gRPC server upsert end-to-end, with no direct cross-module project reference present in the solution.
- [ ] `dotnet build SprintRituals.slnx` is green; no direct Identity↔Fokus project reference exists; the deferred "not-implemented" redemption stub is gone.

## Out of Scope

- **Relocating the Invitation entity or its four management features into Identity** — locked to stay in Fokus per communication-log decision L1. This feature bridges to them, it does not move them, and it does not promote the Invitation entity to an aggregate root.
- **Multi-tenancy / multiple company domains** — the company-domain setting remains a single optional value; no per-tenant model.
- **Non-Google identity providers** — Google-only OAuth remains the only sign-in path (CLAUDE.md auth section).
- **RabbitMQ / out-of-process transport** — the company-domain integration event rides the existing in-process transport; durable transport is a separate future increment.
- **Invitation email delivery** — invitations are shared by link as today; no email-sending is added.
- **Frontend invite landing/error pages** — this pass is API/redirect-only; the client `/invite/{token}` landing page and invite-error pages are deferred to a follow-up. The server still redirects to those paths so the follow-up can supply the views.
- **Wiring the unused operations of the gRPC `PersonGrpcService`** — the full upsert/lookup surface is defined, but only the Fokus Jira-sync upsert consumer is wired this pass; lookup and any other operations are defined but left unconsumed.

## Open Questions

All resolved. See `docs/specs/adhoc-Pass4-IdentityExtraction/delivery/questions.md` (Q1–Q12, all Answered) for the decisions folded into this spec. Critic Mode 1 findings (`docs/specs/adhoc-Pass4-IdentityExtraction/delivery/spec-critic-findings.md` — 1 HIGH, 4 MEDIUM, 5 LOW) have all been folded in.
