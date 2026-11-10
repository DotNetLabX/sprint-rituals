# Suite Scaffolding V2 — Summary

## Status: COMPLETE

Pipeline completed successfully. Code-side scaffold is built, builds clean, migrates, and boots. The only outstanding work is the **live Google-OAuth verification**, which is a plan-sanctioned hand-off to the user (needs real Google credentials + a browser — cannot run unattended).

## What Was Built

A new **SprintRituals** modular-monolith scaffold in this repo: a single Web-SDK **Host** that composes two class-library modules — a new **Identity** module (plain DDD `Person : AggregateRoot<int>` + `User : Entity<int>`, plain `DbContext`, concrete repositories, Google OAuth + cookie, string-claim roles — **no ASP.NET Core Identity**) and **Fokus** ported from the original Fokus project as a module (Web-SDK→class-library, `AppUser` extracted to Identity, **Invitation kept in Fokus**). Clean copy of BuildingBlocks (6) + Jira/Xray modules (Fokus compile deps); the original Fokus project source was never modified.

## Key Outcomes

- **Scope:** 9 plan steps, all Implemented or Deviated-with-valid-reason. ~17 projects in `SprintRituals.slnx`; new Identity module = 3 projects (~25 files); Fokus converted in-place; frontend `vite` outDir retargeted to `Host/wwwroot`.
- **Build:** `dotnet build SprintRituals.slnx` → **Build succeeded, 0 errors**, 4 NU1903 warnings (pre-existing transitive advisory, identical in Fokus source).
- **Migrations:** both `identity.db` (InitialCreate) + `fokus.db` (incl. `RemoveAppUsers`) migrate on startup; AppSettings seed runs.
- **Host boot:** binds `http://localhost:5000`, **55 endpoints** registered across both module assemblies (proof both modules compose).
- **Pipeline:** Architect Step 1 done-check **PASS** (locked decisions verified at code level by grep, not claim). Reviewer Step 2 → REQUEST CHANGES (1 HIGH / 2 MED / 3 LOW) → **1 fix cycle** → Reviewer re-review **APPROVED**.
- **Locked decisions — all honored & code-verified:** plain DDD identity; Invitation-in-Fokus; clean copy (fokus untouched); no secrets committed.

## Deviations from Plan

- 7 developer deviations, all valid (ambiguity fixes, EF-Core-10 collation-on-property, redundant `.IsRequired()` removed, `Path.IsPathRooted` hardening, sequencing merges). See `implementation.md` → Deviations and Key Decisions.
- H1 fix (PersonCreated Id=0) resolved **locally** (Person `RaiseCreated()` raised post-save in the bootstrap path) — no BuildingBlocks divergence from Fokus.

## Notes (before committing)

1. **Two repos have uncommitted work, both awaiting your decision (no auto-commit per the locked decision):**
   - this repo — the entire scaffold (repo still at single "Initial commit").
   - the original Fokus project — pipeline artifacts under `docs/specs/adhoc-SuiteScaffoldingV2/` + the `I12` backlog line (plus pre-existing unrelated changes).
2. **Live-auth verification is yours** — supply `Google:ClientId`/`Google:ClientSecret` via user-secrets (UserSecretsId `sprint-rituals-host-a1b2c3d4`), register redirect `http://localhost:5000/auth/callback`, then run the verify steps in `implementation.md` → "Items 3, 3b, 4, 6, 7 — HANDOFF TO USER". Until creds exist, Scalar UI 400s (Google middleware validates options on every request) — expected.
3. **Cosmetic cleanup:** delete the stray `src/Services/Fokus/Fokus.API/fokus.db` before your first commit (it's gitignored & untracked, so harmless, but tidy).
4. **Infra increment (next, out of this scope):** gRPC PersonGrpcService, MassTransit, Reflekt, invite-token redemption at login, CompanyDomain auto-seed, `UserCreated`/`PersonUpdated` events. Watch the nested-save re-entry note if a `PersonCreated` handler later calls `SaveChangesAsync` on the same DbContext.

Slug: adhoc-SuiteScaffoldingV2
