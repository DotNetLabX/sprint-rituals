# Critic — Mode 1 Spec Review (relay scratch file)

**Scope:** `docs/specs/adhoc-Pass4-IdentityExtraction/definition/spec.md` cross-referenced against the evidence base (refactor-triage A2, decisions.md ADR-001/004/015, backlog Pass 4, questions Q1–Q12, communication-log L1/L2) and the actual source on disk.

**NOT a verdict artifact.** This is a team-lead relay fallback for the inline findings, not `review.md`. The spec author (PO) folds these in.

**Verdict summary:** Spec is substantially sound — all 12 answered questions are reflected, no ADR-001 violation, no internal contradiction in the business-rule logic. Findings are 0 BLOCKER, 1 HIGH, 4 MEDIUM, 5 LOW. The HIGH is a traceability gap (the spec never names the sanctioned cross-module mechanism that L2/Q9 lock in as gRPC). None of these block the architect from planning, but the HIGH and the MEDIUMs should be resolved by the PO first to avoid the architect re-deciding locked points.

---

## HIGH

### H1 — Spec never names the cross-module Person mechanism; L2 + Q9 + Identity/CLAUDE.md lock it as gRPC `PersonGrpcService`
- **Where:** spec §Entities "Cross-module Person contract (new)", §API Surface last line, §User Flows Flow 3, Business Rule 11, Acceptance Criterion 9.
- **What's wrong:** The spec consistently calls it "the cross-module Person service / contract" and says only "module-to-module service contract … not part of the browser-facing REST surface." It never says **gRPC**. But:
  - communication-log **L2** explicitly scopes "(c) gRPC **PersonGrpcService**."
  - **Q9** answer and its evidence pointer name `Blocks.Contracts/Grpc` and "`PersonGrpcService`."
  - `Identity/CLAUDE.md` ("gRPC clients / server") states "`PersonGrpcService` arrives in the infra increment (cross-module Person upsert)."
  - CLAUDE.md tech stack: gRPC code-first (protobuf-net.Grpc) is the sanctioned **synchronous** cross-module mechanism; contracts live in `Blocks.Contracts`.
- **Why it matters:** The spec leaves the transport unstated for a decision that is already locked. An architect could legitimately choose a non-gRPC synchronous mechanism and not be contradicting the spec — re-litigating a locked decision. The spec should state the contract is the gRPC code-first `PersonGrpcService` in `Blocks.Contracts`, server impl in `Identity` (this pass reverses the existing Fokus-server/Host-client direction → Identity becomes a gRPC server for Person upsert; Fokus becomes the client). That direction reversal is itself non-trivial and unstated.
- **Fix:** Name the mechanism (gRPC code-first, `Blocks.Contracts`) and state which module hosts the server (Identity) vs client (Fokus) per Flow 3 direction Fokus→Identity.

---

## MEDIUM

### M1 — "Person service" announcement vs. Acceptance Criterion 9 wording conflict on what is proven end-to-end
- **Where:** Business Rule 12 + spec §Entities/User vs. Acceptance Criterion 9.
- **What's wrong:** BR 12 says the User-creation event drives **both** side effects: "the company-domain seed announcement **and person reconciliation**." But person reconciliation (Flow 3 / BR 11) is triggered by **Fokus syncing a Jira developer**, NOT by user creation in Identity. The spec elsewhere (Flow 3, BR 11) correctly says reconciliation is Fokus→Identity on Jira sync — that has nothing to do with the UserCreated event. BR 12 conflates the two: it claims UserCreated drives "person reconciliation," which contradicts BR 11/Flow 3 where reconciliation is a Fokus-initiated upsert unrelated to a user being created.
- **Why it matters:** Internal contradiction. An implementer reading BR 12 would wire person reconciliation onto the UserCreated event; reading BR 11 they would wire it onto Jira sync. These are different triggers. Q11's answer says UserCreated drives "the company-domain seed + cross-module Person sync via the bus" — but the only cross-module *sync* that UserCreated could drive is the seed; the Jira-developer reconciliation is inbound to Identity (Fokus is the caller), so Identity's UserCreated cannot drive it.
- **Fix:** BR 12 should scope UserCreated to the company-domain seed announcement only (the redemption/bootstrap side effect). State separately that Fokus→Identity Person reconciliation is triggered by Jira sync on the Fokus side, not by UserCreated. (If the intent really is that UserCreated also reconciles the redeemed Person somewhere, say where and to which store — but that is not what BR 11/Flow 3 describe.)

### M2 — "Promote User to aggregate root" decision (Q11) is under-stated in the spec body
- **Where:** spec §Entities/User ("promoted so that its creation raises a domain event"); Business Rule 12.
- **What's wrong:** Q11's answer is explicit: "**Promote User to an aggregate root** this pass so its creation event is raised." The codebase confirms this is load-bearing — `UserCreated.cs` doc-comment states "`User` is an `Entity<int>`, not an AggregateRoot, so domain-event dispatch is not yet available … do not add a RaiseCreated call without first promoting User to AggregateRoot." The spec only says User is "promoted so that its creation raises a domain event" — it never states the concrete structural change (Entity → AggregateRoot) that Q11 locked and that the code comment makes a hard prerequisite.
- **Why it matters:** The architect/developer must know the promotion is a type change (`User : Entity<int>` → `User : AggregateRoot<int>`), with the EF/persistence and DomainEvents-dispatch consequences that entails (ADR-007 rich-aggregate shape). Leaving it as soft "promoted" risks the dispatch never wiring (exactly the latent bug class B1/B2 from triage).
- **Fix:** State in §Entities/User that User is promoted from `Entity<int>` to an aggregate root so it carries `DomainEvents` and `UserCreated` actually dispatches via the SaveChanges interceptor.

### M3 — Acceptance Criterion 9 (end-to-end Jira→Identity reconciliation) is untestable as written and may exceed the wired scope
- **Where:** Acceptance Criterion 9: "A developer synced from Jira in Fokus is reconciled into the Identity Person store across the boundary, **demonstrated end-to-end** …"
- **What's wrong:** §Entities "Cross-module Person contract" + Out-of-Scope last bullet say only the operations the in-scope flows need are wired; "operations with no in-scope consumer are defined but left unconsumed." Flow 3 is the upsert. But the acceptance criterion demands an end-to-end demonstration of Jira-developer→Identity reconciliation, which requires the **upsert consumer to actually be wired on a real Jira sync path**. The spec does not state that the Jira-sync code path is modified this pass to call the new contract — it only defines the contract and says "when Fokus syncs a developer … it reconciles." If the upsert is the one wired consumer, say so explicitly; if it is among the "defined but unconsumed," then AC 9 is untestable.
- **Why it matters:** Ambiguity about whether the Fokus sync service is actually edited to invoke the upsert this pass. AC 9's "demonstrated end-to-end" is only testable if it is. The "no automated suite / manual testing" decision (backlog) makes "demonstrated end-to-end" a manual-verification claim — acceptable, but it must name the observable (a Person row with Source=Jira appears in identity.db after a Fokus sync).
- **Fix:** State explicitly that the Jira-developer sync path (Fokus) is wired to call the upsert this pass (it is the one in-scope consumer), and make AC 9 name the observable evidence (identity.db Person row, Source=Jira).

### M4 — Invitation called an "aggregate" throughout; it is `Entity<int>` in code (also affects the L1 quote)
- **Where:** spec §Entities "Invitation (owned by Fokus …)", §Out of Scope first bullet ("Relocating the Invitation **aggregate**"); mirrors comm-log L1 wording.
- **What's wrong:** `Fokus.Domain/Auth/Invitation.cs` declares `public partial class Invitation : Entity<int>` — it is an entity, not an `AggregateRoot<int>`. The spec (and L1) call it an aggregate. It also is not listed as an aggregate in `Fokus/CLAUDE.md` (aggregates there are Sprint + Ticket only).
- **Why it matters:** Low functional impact (it stays in Fokus either way), but the architect plans against domain-model facts. Calling a bare `Entity` an "aggregate" can mislead ADR-007 expectations (rich aggregate, behavior split) — note that Invitation already *has* a behavior partial (`Behaviors/Invitation.cs`) with `Create/Accept/Revoke`, so it is partially aggregate-shaped but not typed as one. Flag for precision.
- **Fix:** Call it the Invitation **entity/concept** (it has behavior methods but is `Entity<int>`), or confirm with the architect whether Pass 4 promotes it — but Pass 4 scope says it only *bridges* to Invitation, so no promotion is intended; just fix the term.

---

## LOW

### L1 — `docs/kb/` is empty; spec's "Open Questions / evidence" implies KB but none exists
- **Where:** Review prompt asked to cross-reference `docs/kb/` for business rules on invitations/identity/company-domain.
- **Finding:** `docs/kb/` contains **zero files** (confirmed via Glob — no `index.md`, nothing). This matches triage finding **E3** ("Missing `docs/kb/index.md` … referenced by rules but absent"). No KB business rule contradicts the spec because there is no KB. Not a spec defect — recorded so the reviewer/architect does not assume a KB exists to validate the company-domain public-provider list against.

### L2 — "Known public provider" list is illustrative, not enumerated/testable
- **Where:** §Entities/Company domain, Flow 2, Business Rule 9, Acceptance Criterion 7 — all say "known public provider (e.g. gmail.com, outlook.com)."
- **Finding:** The exclusion set is never closed. AC 7 ("does not seed when that domain is a known public provider") is only testable for the two examples. For manual testing this is tolerable, but the spec should state where the authoritative list lives (a constant in Identity) and that it is non-exhaustive by design, so a reviewer doesn't fail an un-listed provider.

### L3 — Email-domain extraction edge cases unspecified
- **Where:** §Entities/Company domain ("derived from the bootstrap administrator's email domain"); BR 9.
- **Finding:** The spec doesn't address malformed-email / multi-`@` / subdomain handling for deriving the domain. The existing `CreateInvitationEndpoint` does `email.Split('@')[1]` (last-wins is actually `[1]`, i.e. second segment) for the gate. For consistency the seed derivation should match the existing gate's extraction. Minor; flag so the seed and the gate use identical domain-extraction logic (otherwise a seeded value might never match the gate).

### L4 — API Surface table row 2 has no method/route, only a parenthetical
- **Where:** §API Surface, row "| GET | (Google sign-in entry, token-carrying) | …".
- **Finding:** The actual entry point is the existing Google challenge → `/auth/callback` (`OnTicketReceived` in `AuthConfiguration.cs`), not a documented REST route. The placeholder "(Google sign-in entry, token-carrying)" is vague. Acceptable for an API/redirect-only pass, but the architect will need the concrete callback path; consider citing `/auth/callback` and the `invite_token` auth-property carrier (the stub already reads `context.Properties.Items["invite_token"]`).

### L5 — Redirect destinations are client paths deferred to a follow-up (Q12/Out-of-Scope) but the spec asserts redirects "land on home"
- **Where:** §User Flows Flow 1 steps 7/8, Acceptance Criteria 1/4/6, Out-of-Scope "Frontend invite landing/error pages."
- **Finding:** Out-of-Scope correctly states the `/invite/{token}` landing and `/invite/error` pages are deferred and "the server still redirects to those paths." But ACs 1/6 say the user "lands on home" and AC 4 says the reason is "surfaced … at a dedicated invite-error destination." Since the frontend pages don't exist this pass, "lands on home" / "surfaces the reason" is only verifiable as a **302 to the right URL**, not a rendered page. Consistent with Q12, but the ACs should phrase the observable as the redirect target + reason query param (302 Location), not a rendered landing — otherwise they're untestable this pass. (Low: the intent is clear and Out-of-Scope already carves it out.)

---

## Cross-reference: all 12 answered questions vs. spec body (verification)

| Q | Decision | Reflected in spec? | Note |
|---|----------|--------------------|------|
| Q1 | link → validate → Google carries token → callback redeems | YES | Flow 1 steps 2–6 |
| Q2 | enforce exact email match (case-insensitive), reject mismatch | YES | BR 7, Flow 1 step 5, AC 2 |
| Q3 | redeemed role from invitation; bootstrap still Admin | YES | BR 3, BR 4, §Entities/User |
| Q4 | reuse existing Person, leave source; create only if none | YES | BR 8, §Entities/Person, AC 5 |
| Q5 | dedicated `/invite/error?reason=`; already-registered → ignore token, normal login | YES | BR 1, Flow 1 steps 8–9, AC 4/6 |
| Q6 | seed on bootstrap only, no later reconcile | YES | BR 9 ("not reconciled on later logins"), Flow 2 |
| Q7 | derive from admin email domain; skip public providers | YES | BR 9, §Entities/Company domain, AC 7 |
| Q8 | seed via integration event over in-process transport | YES | BR 10, §Real-time Events, AC (implied by 7/8) |
| Q9 | Fokus→Identity upsert, source=Jira | PARTIAL | direction YES (BR 11/Flow 3); **mechanism (gRPC) NOT named → H1** |
| Q10 | full contract defined, only in-scope consumers wired | YES | §Entities last bullet, Out-of-Scope last bullet |
| Q11 | promote User to **aggregate root**; event drives side effects | PARTIAL | event YES; **"aggregate root" structural change under-stated → M2** |
| Q12 | API/redirect-only; client pages deferred | YES | Out-of-Scope, §API Surface intro |

## Cross-reference: binding ADRs

- **ADR-001 (no cross-module project refs):** Spec **complies** — BR 13 + AC 9 + §Entities intro all assert no direct project reference; both seams use sanctioned mechanisms (integration event + cross-module service). No violation. (H1 is about *naming* the service mechanism, not a violation.)
- **ADR-004 (light stack, no MediatR/Application):** No conflict — spec adds no Application layer; uses FastEndpoints + IEvent bus + integration event, consistent.
- **ADR-007 (rich aggregates):** Relevant to M2/M4 (User promotion, Invitation typing) — spec under-specifies the structural shape.
- **ADR-015 (Identity extraction in-flight):** Spec is the completion of exactly this ADR; traces correctly. The "remove the not-implemented stub" (BR 14, AC final) matches the real stub at `AuthConfiguration.cs:88-95`.

## Evidence grounding (stub pointers in questions.md verified on disk)
- `AuthConfiguration.cs:88-95` — redemption stub redirecting to `/invite/error?reason=not-implemented`: **CONFIRMED** (lines 88–95).
- `AuthConfiguration.cs:116` — CompanyDomain auto-seed is comment-only: **CONFIRMED** (lines 116–118 are a DEFERRED comment block).
- `Identity.Domain/Users/Events/UserCreated.cs` — unraised placeholder, doc-comment requires promoting User to AggregateRoot first: **CONFIRMED**.
