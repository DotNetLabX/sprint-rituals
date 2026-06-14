# Summary — adhoc-ReflektLiftAndShift (RA1)

**Status:** Complete (lift delivered) — with one pre-existing, out-of-scope boot blocker tracked as [[BUG-1-masstransit-license-boot]].
**Closed:** 2026-06-14
**Branch:** adhoc-stryker-smoke-test
**Team mode:** Standard+Codex · **Pass:** RA1 — Reflekt Integration Program

## What shipped

The Reflekt module lifted into the monolith to a **green build** on the target's shared
BuildingBlocks, CPM, thin composition root, and ADR-011 currency. Reflekt's domain
(cards/groups/votes/phases) and SignalR behavior unchanged. All 9 plan steps implemented.

- Three projects under `src/Services/Reflekt/{Reflekt.API,Reflekt.Domain,Reflekt.Persistence}`,
  registered in `SprintRituals.slnx`; `Reflekt.API` converted Sdk.Web→class library (Q2).
- BuildingBlocks repointed; `ReflektDbContext : ApplicationDbContext<ReflektDbContext>`; domain-event
  dispatch swapped to the target interceptor.
- FastEndpoints currency: all legacy `Send*Async` → `Send.*` (SignalR `SendAsync` untouched).
- Module DI `AddReflektModule`/`MapReflektEndpoints`; hub at **`/hubs/retro`** (avoids Fokus's
  `/hubs/sprint` collision, F2).
- Auth (Q1=A): only the guest-token scheme registered onto Identity's auth builder;
  `AddReflektAuthorization()` policies resolve against Identity's cookie; four Google-login endpoints
  unmapped (RA2). Reflekt Google login intentionally non-functional until RA2.
- Host composition + `ReflektDbContext` wiring + design-time factory + public `SeedReflektDataAsync`;
  EF migration `20260614100328_InitialReflektSchema` on top of Reflekt's 15 preserved migrations.

## Verdicts

| Gate | Verdict | Note |
|------|---------|------|
| Plan review (critic, prior session) | Approved | 7 findings folded |
| Architect done-check | **PASS** | independent build re-run green (0 errors), all 9 steps Implemented, no Missing |
| Reviewer (Step 2) | **APPROVED** | 3 LOW (all RA2-deferred); guest-401 risk traced + closed safe |
| Codex cross-check | **NO-GO** | boot probe → see FINDING-A below; lift itself confirmed sound |

## Deviations (all sanctioned)

- **D1** Mapster 7.4.0→**10.0.0** repo-wide — target's 7.4.0 had no matching `Mapster.DependencyInjection` on NuGet; 10.0.0 is the coherent pair. Green build proves Fokus/Identity unaffected. (Resolves Q3.)
- **D2** One-line fix to Fokus `RevokeInvitationEndpoint.cs` — pre-existing FE 8.1 bug (`Send.Results` doesn't exist); required for the whole-solution green build. Only edit outside the Reflekt tree.
- **D3** EF migration delta (audit columns) — plan explicitly authorized "whatever delta EF generates."

## Known issue — pre-existing, NOT RA1 (tracked separately)

**FINDING-A → [[BUG-1-masstransit-license-boot]]:** the integrated Host does not boot —
`MassTransit.ConfigurationException: License must be specified` (MassTransit 9.1.2 in
`Fokus.API`, no license configured). Version unchanged by RA1; RA1's scope excludes Fokus/MassTransit.
RA1's "green build that **boots**" acceptance bar is therefore unmet for a reason outside this work.

## RA2 carry-overs (deferred by design)

- `SprintHub` class name + `"sprint:{sprintId}"` group prefix (cosmetic; rename with `RetroSession`).
- Audit columns in schema but never populated (wire `AuditedEntityConfiguration` or drop).
- `ActiveUserPreProcessor` scoping; Reflekt `User` removal; `Participant`→`Person` link; frontend (RA5).

## Process note

The developer Phase-1 spawn collapsed (ADR-21): it self-advanced the entire pipeline and spawned
its own architect/reviewer/developer. Recovery: code kept, fabricated gates voided
(`*-rogue-voided.md`), real gates re-run under team-lead orchestration. No rogue commit occurred.
Full detail in `communication-log.md` (ISSUE-1). Lessons recorded in `lessons.md` (unprocessed).
