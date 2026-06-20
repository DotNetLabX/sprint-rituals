# Reflekt Refactor Triage

**Status:** Draft — discovery sweep, pending user validation + verification passes
**Date:** 2026-06-20
**Author:** Architect
**Program slug:** `adhoc-ReflektRefactorTriage`
**Baseline:** Reflekt port closed (RA0–RA5, see `docs/backlog.md`). Module at `src/Services/Reflekt/{API,Domain,Persistence}` + `Reflekt.Domain.Tests` / `Reflekt.Persistence.Tests`.

## Purpose

Mirror the Fokus refactoring path (`refactor-triage.md` → `decisions.md` ADRs → numbered Pass pipeline) for the now-lifted Reflekt code. The Fokus triage crystallized the **user-contributed points** as the binding register **ADR-002…015**. This triage's job is to run Reflekt against that *same* register and emit downstream passes **only where the debt is real** — not a ceremonial re-run.

**The gate is the user-point register.** Every ADR below is checked against actual Reflekt code; verdict legend matches the Fokus triage: `by-design` (false positive / already compliant — withdraw) · `needs-decision` · `real` (genuine debt) · `verify` (claimed compliant, spot-check) · `done`.

## Key framing (the fork that shapes this)

RA1 was **explicitly mechanical** ("no semantic change"), and `docs/backlog.md:106` asserts Reflekt is newer code that already follows the **mechanical** ADRs (008/009 slice+naming; 011/013/014 Send.*/CPM/thin-root folded into RA1). That assertion covers the mechanical arm. It does **not** cover the **deep/semantic** ADRs (005 computation-in-domain, 006 no-DTO-in-domain, 007 rich VOs) or event-correctness — those were never verified against Reflekt. So effort concentrates there.

---

## User-point register coverage (ADR-002…015 gate)

| ADR | User point (Fokus triage origin) | Reflekt finding | Verdict |
|-----|----------------------------------|-----------------|---------|
| 002/003 | Reference project + decision-order + problem-class/root-cause | Process directive — governs this triage and every emitted pass | `applies` |
| 004 | Light stack (no MediatR/Application layer) | Reflekt is API+Domain+Persistence only | `done` |
| 005 | Computation in Domain; minimize API service classes (#4/#5/#6/#7) | **`Reports/*` endpoints are 132–133-line handlers** computing report aggregates inline and returning `ReportContracts` response records — computation in API, not a domain service. `TimerService` (1 service class) is a real-time concern, by-design. | **real** |
| 006 | No DTO enters the Domain (A1) | `Reflekt.Domain.csproj` refs only `Blocks.Domain`; **no `*.Contracts`/Jira/Xray usings, no `FromX(...)` factories, no Response/Dto/Info records in Domain.** Clean. | **by-design (withdraw)** |
| 007 | Rich aggregates / value objects (#1d, B4) | Aggregates *are* rich (sealed partial, static factory, behavior split, some VOs — `ColumnColor`, `FokusSprintRef`). **But VO-candidate clusters persist flat:** `RetroSession` has a **column triple** (`Column{1,2,3}Name`/`Column{1,2,3}Color` → `RetroColumn` VO) and a **timer triple** (`Writing/Voting/DiscussingTimerDefault` → `TimerDefaults` VO). **No `ComplexProperty`/`OwnsOne`/`OwnsMany` anywhere in the module.** Scan other aggregates for the same. | **real** |
| 008 | 2-file vertical slice (#1a) | Per module CLAUDE.md + RA1, already 2-file. | `verify` (spot) |
| 009 | `Command`/`Query` naming, no `Request` suffix (#1b) | Reflekt already uses `Command`/`Query`. | `verify` (spot) |
| 010 | Mapping convention — Mapster/factory (#1c) | `Reflekt.API/Mappings` exists; check report/snapshot paths for hand-assignment. | `verify` |
| 011 | FastEndpoints `Send.*` currency (#1e) | Claimed folded into RA1. | `verify` (grep `Send*Async`) |
| 012 | Always-latest frameworks | net10.0 ×N; CPM-managed. | `done` |
| 013 | Central Package Management (#2) | Applied repo-wide in RA1 (bare `PackageReference`). | `done` |
| 014 | Thin composition root (#3) | `AddReflektModule`/`MapReflektEndpoints`; `app.Migrate<T>()`. | `verify` (spot) |
| 015 | Identity extraction (A2) | Landed as RA2. | `done` |

## Event-correctness arm (Pass-5 equivalent)

| ID | Location | Note | Verdict |
|----|----------|------|---------|
| EC1 | `Reflekt.Domain/*/Behaviors/*.cs` (CardGroup, Card, Participant, RetroSession, Team, Vote) | Events raised in behaviors onto the `DomainEvents` list; interceptor publishes post-save. **Verify:** no `Raise()` *after* `SaveChangesAsync` (the Fokus B1 `PersonCreated` class), and every event in each `Events/` folder is actually raised (no declared-unraised dead events, the Fokus B2 class). | `verify` |

## Cleanup / naming arm (Pass-6 equivalent)

| ID | Location | Note | Verdict |
|----|----------|------|---------|
| N1 | `Reflekt.Domain/Sprints/Behaviors`, `Reflekt.Domain/Sprints/Events` | **Empty leftover dirs** from the RA3 `Sprint`→`RetroSession` rename. Dead. | **real (low)** |
| N2 | Child FK columns / SignalR strings | **Resolved — RA5 closed it (verified 2026-06-20).** All child FK columns are `RetroSessionId` (CardGroup, Card, Participant, FokusSprintRef); no `sprint*` method/group/claim strings remain in the hub. The only `SprintId` left is `FokusSprintSnapshot`'s natural Fokus-sprint key — legitimate, in `FokusSprints/` (Fokus ref, not the renamed aggregate). | **by-design (withdraw)** |

---

## Recalibrated summary

The mechanical arm is genuinely light (Reflekt is post-guard code). The actionable, user-point-grounded work is **four candidate passes**, far short of Fokus's Pass0–6:

| Candidate pass | Covers user point (ADR) | Scope sketch |
|----------------|-------------------------|--------------|
| **RR-B — Rich VOs** | ADR-007 (#1d) | **Problem class (ADR-003): group flat cohesive scalar clusters into VOs across all aggregates.** Confirmed instance: `RetroSession` → `RetroColumn` (Name+Color) + `TimerDefaults`, persisted via `ComplexProperty`. Pass also scans Card/Vote/Team/CardGroup/Participant and fixes any cluster found (small aggregates, likely thin yield — but in-scope, not a follow-up). |
| **RR-C — Computation→Domain** | ADR-005 (#4/#5/#6) | Lift `Reports/*` aggregation into a `Reflekt.Domain` report/analytics service returning domain types; Mapster-map to `ReportContracts` at the endpoint. Thin the 132-line handlers. |
| **RR-D — Event-correctness** | Pass-5 arm | Verify raise-ordering + declared-unraised across all `Behaviors`; fix any post-save raises or dead events. |
| **RR-E — Cleanup rider** | Pass-6 arm | N1 only: delete dead `Sprints/` dirs. N2 withdrawn (RA5 closed naming). Trivial — fold into RR-B/RR-D rather than a standalone pass. |

**Withdrawn as by-design / already done:** ADR-004/006/012/013/015 (006 is the headline — no DTO leak in Reflekt's domain). **Spot-verify then likely withdraw:** ADR-008/009/010/011/014.

**Needs a decision from the user:**
- **O1** — Confirm the three-pass scope (RR-B/C/D + RR-E rider), or do you want a ceremonial full mirror including the already-satisfied arms?
- **O2** — ~~N2 naming~~ **Resolved 2026-06-20: RA5 closed it.** No remaining internal `SprintId`/`sprint*` debt.
- **O3** — ~~RR-B reach~~ **Resolved 2026-06-20:** per ADR-003 (every finding is a problem class), RR-B covers the VO problem class across **all** Reflekt aggregates. `RetroSession` is the confirmed instance; the rest are scanned-and-fixed within the pass, not deferred.

## Operating model (inherited, authoritative)

Same as Fokus (`refactor-triage.md` §Operating model / ADR-003): every finding is a **problem class** (fix all instances + root cause, not the cited example); **decision order** skill → reference project (`<reference-repo>`) → fix/create skill; latest framework versions. Each emitted pass runs through the standard pipeline (Standard+Codex, the mode used for every Fokus pass).
