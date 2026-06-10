# Identity Extraction (Pass 4) — Summary

## Status: COMPLETE

## What Was Built
Finished the in-flight Identity extraction (triage A2 / ADR-015) by delivering the three deferred cross-module seams: **invitation redemption at login** (the "not-implemented" stub is gone), **company-domain auto-seed** from the first administrator, and **cross-module Person reconciliation**. The cut bridges Identity ↔ Fokus using only sanctioned mechanisms — **two opposite-direction gRPC code-first contracts** (`IInvitationService`: Fokus server / Identity client, synchronous atomic redeem; `IPersonService`: Identity server / Fokus client, Jira-developer Person upsert) plus one **MassTransit integration event** (`FirstAdministratorRegistered`) — with **no cross-module project reference** (ADR-001 intact). `User` was promoted `Entity<int>` → `AggregateRoot<int>` so `UserCreated` dispatches; `Person.Email` made optional with Jira-sourced Persons keyed on `ExternalId`. Invitation stays in Fokus (locked L1).

## Key Outcomes
- **Files:** ~53 created/modified across Identity, Fokus, Blocks.Contracts, Blocks.AspNetCore, Jira module; **4 EF migrations** (`PromoteUserToAggregate`, `MakePersonEmailOptional`, `AddDeveloperEmail`, `AddInvitationConcurrencyToken`).
- **Build:** green — `dotnet build SprintRituals.slnx` → 0 errors (4 warnings, incl. pre-existing `NU1903` transitive advisory — see Notes).
- **Review:** Standard+Codex. Critic Mode 1 (spec) + code-grounded Mode 2 (plan). Step-2: nexus reviewer **APPROVED** + Codex **GO** after **1 fix cycle**. Codex's independent cross-check caught 2 HIGH bugs the reviewer missed (redemption replay race; inactive-user duplicate-`User` crash) — both fixed and re-verified by both reviewers.
- **Commits:** `1aaa362` (plan) → `6fcd54f` (implementation) → finalize (this checkpoint).

## Deviations from Plan
- EF migrations scaffolded with `-s *.Persistence` (not `*.API`, which lacks the EF Design tooling); `Microsoft.EntityFrameworkCore.Design` added to `Fokus.Persistence`. Validated as convention-compliant in the Step-1 done-check.
- Step order: Step 10 (nullable `Person.Email` + Jira fetch) executed before Steps 4/5 (PersonGrpcService depends on it) — dependency-driven.
- Plan grew 11 → 12 steps when H1 (Jira-no-email) was resolved at the plan-review stage.
- Developer additionally closed a revoke-vs-redeem race (concurrency token on `Revoke()` too) beyond the requested redeem-path fix.

## Notes
- **H1 — Jira email reality:** Jira Cloud hides developer email by default and the issue payload has none. Resolution: Jira-sourced Persons keyed on `ExternalId` (=AccountId) with `Person.Email` nullable; a best-effort `GET /rest/api/3/user` fetch fills email only when Cloud exposes it. AC-8 observable = a `Source=Jira` Person row with `ExternalId` in `identity.db` after a sync.
- **H2 inactive-user policy (secure default applied):** an inactive/deactivated user who redeems an invite is **not** silently reactivated — redirect to `/invite/error?reason=account-inactive`. Change to reactivation is a future product decision if desired.
- **Follow-up (non-blocking):** `NU1903` high-severity advisory on transitive `System.Security.Cryptography.Xml 10.0.0` (via `System.ServiceModel.Primitives` from the gRPC code-first stack) — pin a patched version in a follow-up.
- **Follow-up (LOW):** active-user redemption path persists `RecordLogin()` via `personRepository.SaveChangesAsync()` (shared `IdentityDbContext`) rather than `userRepository` — cosmetic.
- **Frontend deferred (Q12):** `/invite/{token}` landing + invite-error pages are server-redirect-only this pass.
- **Runtime malfunctions (logged in communication-log):** M1 (PO artifact non-persistence), M2 (PO skipped spec-review offer — remediated via Critic Mode 1), M3 (Codex first cross-check wrote no file). All caught by verify-on-disk discipline; no review or decision lost.
- No automated tests (project decision) — manual testing of the redemption + auto-seed + Jira-reconciliation flows is recommended before release.
