# Refactor Triage

**Status:** Draft — discovery sweep, pending user list + architecture validation
**Date:** 2026-06-05
**Author:** Architect
**Baseline:** `dotnet build SprintRituals.slnx` → green (0 errors, 6 warnings). 288 `.cs` files. No tests (manual testing accepted — small app).

## Context: the code is mid-migration between two architecture docs

- **`docs/architecture/v1.md`** (May 6, "Current") — Fokus **standalone**: no auth, no MassTransit/gRPC/MediatR, **thin domain by design**, natural keys that **"eliminate mapping layers,"** computed metrics, `AppSettings` "not a domain concept," secrets via Key Vault / User Secrets.
- **`docs/proposals/suite-integration-architecture.md`** (May 16, "Decided") — **modular monolith**: Identity owns Person/User, gRPC + `SprintSynced` integration event, Fokus drops standalone auth.

The codebase is migrating *from* v1.md *toward* the suite doc. Several "violations" below are v1.md by-design choices, not debt. **The root issue is documentation: there is no single current architecture statement, so intentional decisions and real debt are indistinguishable.** Resolving that (consolidate into one current architecture doc) is the prerequisite step.

**Verdict legend:** `by-design` (sanctioned by v1.md — likely false positive) · `needs-decision` (the two docs conflict, or a new decision is required) · `real` (genuine debt regardless of doc) · `done`.

---

## Theme A — Architecture boundaries

| ID | Initial sev | Verdict | Location | Note |
|----|------|---------|----------|------|
| A1 | high | **needs-decision** | `Fokus.Domain` → `Jira.Contracts`; `FromJira` factories on aggregates | v1.md "Sync-from-External" says natural keys **"eliminate mapping layers"** — so domain-owns-ingestion may be intentional. But the suite reference-graph says "Domain references nothing," and `Fokus/CLAUDE.md` claims a `JiraMapper` does this (it doesn't). The two docs conflict; decide which holds, then make code + docs agree. |
| A2 | high | **real** | `Fokus.API/Features/Auth/*`, `Fokus.Domain/Auth/Invitation.cs`, migration `RemoveAppUsers` (today) | Identity extraction is genuinely half-done. This is the central in-flight work, not a static defect. Decide what stays in Fokus vs moves to Identity, finish the cut. |

## Theme B — Domain model

| ID | Initial sev | Verdict | Location | Note |
|----|------|---------|----------|------|
| B1 | high | **real (confirm)** | `Identity.Domain/Persons/Behaviors/Person.cs:30` | `RaiseCreated()` called *after* `SaveChangesAsync`, so `PersonCreated` never dispatches. Identity is new code, not covered by the thin-domain decision — looks like a real bug. Confirm intent. |
| B2 | high | **needs-decision** | `Identity.Domain/Users/Events/UserCreated.cs:5` | Declared, never raised. Could be an intentional not-yet-wired placeholder for the in-flight Identity work, or dead code. Confirm. |
| B3 | high | **by-design** | `SprintRepository.cs:55–63`, `TicketRepository.cs:49–58` | Upsert mutates entity props in the repo. v1.md "Sync-from-External": *"Upsert semantics required on all sync paths"* + thin domain = entities are data containers. This is the sanctioned pattern. Withdrawn as a defect. |
| B4 | medium | **by-design (Fokus) / needs-decision (Identity)** | `Person.cs`, `User.cs`, `Invitation.cs`, `Sprint.cs`, `Developer.cs` | v1.md: *"Thin domain in v1. Entities are data containers."* Fokus anemia is documented intent. Identity (Person/User) anemia is less clearly intentional — confirm whether Identity should encapsulate. |
| B5 | medium | **by-design** | `Fokus.Domain/Settings/AppSettings.cs` | v1.md: AppSettings is *"Application configuration, not a domain concept."* No events intended → not dispatching is fine. Withdrawn. |
| B6 | medium | **needs-decision** | `Identity.Persistence/Configurations/UserConfiguration.cs:22` | DB `HasDefaultValue("Manager")` duplicates factory default. Minor; confirm whether DB-level default is wanted as a safety net or removed to force factory use. |
| B7 | low | **real (latent)** | `Blocks.EntityFrameworkCore/Repositories/RepositoryBase.cs:23–28` | Shallow `CurrentValues.SetValues` will miss private backing fields if any aggregate adopts them later. Low priority, note for when domain thickens. |

## Theme C — Sync & analytics code quality

| ID | Initial sev | Verdict | Location | Note |
|----|------|---------|----------|------|
| C1 | high | **by-design** | `Fokus.API/Features/Analytics/*` (services 600–886 lines) | v1.md "Computed Metrics" + skill `analytics-computation-service` sanction substantial computation services. Size alone isn't debt. Only split one if it's genuinely multi-purpose (verify case-by-case, not a blanket pass). |
| C2 | medium | **partly real** | `Fokus.API/Features/Sync/SprintIssueSyncService.cs` | **Real:** `:84–86` main catch swallows the exception with no logging and returns raw `ex.StackTrace` to callers (inconsistent with the other catches that log); `:55` informational message logged at `LogWarning`. **By-design:** per-ticket upsert in loops — v1.md says dataset is small ("hundreds of tickets"), materialize only if perf degrades. **Minor:** `:178` redundant per-sprint settings refetch (`settings2`). |
| C3 | medium | **by-design** | `Fokus.API/Features/Analytics/` root | Shared "focused operation services" are explicitly allowed. Volume is high but sanctioned. Monitor; not a pass. |

## Theme D — Secret handling

| ID | Initial sev | Verdict | Location | Note |
|----|------|---------|----------|------|
| D1 | medium | **by-design** | `src/Host/appsettings.json:16–26` | v1.md "Secrets": Jira creds come from Key Vault / User Secrets. Committed file has placeholders only — that's the intended setup. Withdrawn (optionally add a one-line README note). |
| D2 | medium | **needs-decision** | `src/Host/Program.cs:82–90` | Xray `ClientSecret` seeded into the SQLite `AppSettings` row in plaintext. v1.md stores runtime settings in AppSettings, so this may be intentional — but a secret at rest in the app DB is worth a conscious call. Confirm. |

## Theme E — Docs & conventions (genuinely real — this is the priority)

| ID | Initial sev | Verdict | Location | Note |
|----|------|---------|----------|------|
| E0 | — | **real** | `docs/architecture/v1.md` vs `docs/proposals/suite-integration-architecture.md` | **No single current architecture statement.** v1.md is stale (standalone era) but marked "Current"; the suite decisions live in a proposal. Consolidate into one authoritative architecture doc capturing the *current* modular-monolith decisions. This unblocks distinguishing debt from intent. |
| E1 | high | **real** | stack-conventions block (CLAUDE.md / `docs/conventions/`) | Describes "Services/ = Microservices," `ApiGateway/` (YARP), `docker-compose` — contradicts the decided modular monolith. Misleads planning agents. Align to the monolith. |
| E2 | medium | **real** | `docs/proposals/suite-integration-architecture.md` | Stale repo tree (`src/Identity` vs `src/Services/Identity`) + stray "Sprint 28 — Sprint Report" appended (~lines 229–267). Clean up. |
| E3 | medium | **real** | repo root / `docs/` | Missing `docs/backlog.md`, `docs/kb/index.md` (referenced by rules but absent), `graphify-out/`. |
| E4 | — | **done** | `SprintRituals.slnx` | Nested-folder bug + duplicate BuildingBlocks folder fixed this session (18/18 projects verified). |

---

## Recalibrated summary

After reading v1.md, the genuinely actionable items shrink to:

- **E0 / E1 / E2 / E3** — documentation: consolidate the architecture doc, align conventions, clean stale docs, create backlog/KB. **Do first** — everything else depends on having a true north.
- **A2** — finish the Identity extraction (the real in-flight migration).
- **B1** — `PersonCreated` dispatch bug (confirm).
- **C2** — sync error-handling/logging (real subset).

**Needs a decision from the user:** A1 (domain↔Jira coupling), B2 (UserCreated), B4-Identity (encapsulate Person/User?), B6, D2.

**Withdrawn as by-design:** B3, B5, C1, C3, D1, and the perf sub-point of C2.

---

## Operating model (user directive — authoritative)

1. **Every finding is a problem class, not the example.** Fix all instances across the codebase, not just the cited file.
2. **Root cause first.** In most cases the skill already shows the right way and was ignored — fix the skill/process, not only the code. Root-cause analysis: `skill-adherence-root-cause.md`.
3. **Decision order when code smells:** skill exists & correct → apply it · skill missing or itself smells → use the **reference project** → then fix/create the skill.
4. **Reference project:** the reference codebase (user said `c:`; the path is on `d:`). **Pending user confirmation of exact variant.** Path to be recorded in the architecture doc.
5. **Always latest framework versions.**

## Root cause (why skills were not followed)

Early features (F1/F4) shipped **before** the skill-enforcement guards existed. Sprint-rituals inherited that pre-guard code. Systemic causes (full detail in `skill-adherence-root-cause.md`):
- Skills were not portable (45/57 carried Reflekt-specific names) → "adapt rather than follow blindly" license.
- Plans had no Skill-Mapping table; "adapt" was a no-justification disposition.
- Architecture doc gave inline-pattern cover (no skill-inventory scan).
- Some skills had non-compilable / broken content.
- No reviewer check on skill mapping.

## User-reported problem classes (authoritative)

| # | Problem class | Blast radius | Skill status | Resolution (decision-order) |
|---|---------------|--------------|--------------|------------------------------|
| 1a | Endpoint + request + response + validator in one file | ~12 endpoints define request inside `*Endpoint.cs` | `create-feature` says **2-file split** (`{F}Endpoint.cs` + `{F}Command.cs`/`Query.cs`) — ignored | Apply `extract-endpoint-types` to all |
| 1b | `Request` suffix instead of `Command`/`Query` | **37 types** | `create-feature`/`extract-endpoint-types` prescribe `Command`/`Query` — ignored | Rename; validator `{F}CommandValidator` |
| 1c | Command fields ≠ entity field names (blocks Mapster) | e.g. `AlertThreshold` vs `BugRatioAlertThreshold` | no explicit mapping skill rule | **Decision:** identical names → Mapster for trivial mapping; non-trivial → factory in aggregate behavior partial class |
| 1d | Flat aggregate, no value objects | `AppSettings` (20+ scalars), others | `domain-patterns` has VO base types but **no "when to use VO"** — gap | **Decision:** group cohesive scalars into VOs / embedded entities (e.g. `BugRatioConfig`, `XrayConfig`, `CycleTimeConfig`). Fix skill to state the rule |
| 1e | Deprecated `SendOkAsync` (not `Send.*`) | **89 calls / 54 files** | no framework-currency rule — gap | **Decision:** modern `Send.*` API; add currency rule |
| 1f | .NET version | confirmed `net10.0` ×18 ✅ | — | No action (already 10) |
| 2 | No Central Package Management | no `Directory.Packages.props` | gap — no skill | **Decision:** introduce CPM; create skill / arch entry |
| 3 | Wiring centralized in `Host/Program.cs` | migration/seed/MassTransit/gRPC inline (`Program.cs:44–115`) | `service-registration` prescribes `Add{Svc}Module`, `app.Migrate<T>()`, layer placement — ignored | Distribute to module DI; thin Program.cs to Add/Init/Use |
| 4 | Too many API service classes = thin domain | ~14 in `Features/Analytics/` | **skill gap** (no domain-service concept); `analytics-computation-service` encourages it | Reference project → define domain-service pattern → fix skill → move logic into domain |
| 5 | Huge endpoint handler + huge-param method | `GetSprintSummaryEndpoint`, `SprintSummaryService.ComputeSummary` | decomposition unspecified | Decompose; driven by #4/#6 resolution |
| 6 | Computations depend on response `Info` records → unusable outside API | analytics services | **SKILL SMELL** in `analytics-computation-service` (records are its return type) | Separate computation (domain) from response shape; fix skill |
| 7 | 8 feature areas in `Features/` reads as 8 "aggregates" | `Features/` has 8 dirs vs 2 domain aggregates | — | Symptom of #4/#6 (logic in API not domain) |
| 8 | Feature-folder "aggregates" ≠ domain aggregates | Sprint, Ticket in domain; 8 feature areas | — | Confirm intended aggregate boundaries during #4 work |

## Skill verdicts

| Skill | Verdict | Note |
|-------|---------|------|
| `create-feature` | correct, **ignored** | 2-file split + Command/Query naming |
| `extract-endpoint-types` | correct (fix-tool) | apply to split fat endpoint files |
| `service-registration` | correct, **ignored** | `Add{Svc}Module`, 3-section Program.cs, `app.Migrate<T>()`, MassTransit in App layer |
| `analytics-computation-service` | **SMELL** | returns response records — computation bound to API shape (#6) |
| `domain-patterns` | **gap** | VO base types but no "when to use VO vs scalar" (#1d) |
| `create-aggregate` | **SMELL** | references non-existent workflow files |
| `cqrs-patterns` | partial | implies aggregate-owned mutation; not reconciled with service classes |
| domain-service (cross-aggregate computation) | **MISSING** | no skill defines it — largest gap (#4/#6) |
| `persistence-patterns` | **SMELL** | non-compilable `ref`-param helpers + `HasDefaultValueSql` |
| central-package-management | **MISSING** | no skill (#2) |
| framework-currency ("always latest", `Send.*`) | **MISSING** | no rule (#1e) |

## Decisions captured (to be promoted into the architecture doc)

- **D1** Always latest framework versions. Modern FastEndpoints `Send.*` API. .NET 10 (already met).
- **D2** Central Package Management (`Directory.Packages.props`).
- **D3** Endpoint = 2 files: `{F}Endpoint.cs` (handler) + `{F}Command.cs`/`{F}Query.cs` (request + response + DTOs + validator).
- **D4** Naming: `{F}Command` (mutations) / `{F}Query` (reads); validator `{F}CommandValidator`. No `Request` suffix.
- **D5** Command↔entity names identical → Mapster (trivial); non-trivial → factory in aggregate behavior partial class.
- **D6** Aggregates use value objects / embedded entities, not flat scalars.
- **D7** Rich domain: computation belongs in the domain (aggregate behavior or domain services), not API service classes returning response records. Minimize API service classes.
- **D8** Wiring distributed to module DI extensions per `service-registration`; `Program.cs` thinned to Add/Init/Use.
- **D9** Decision order: skill → reference project → fix/create skill. Every finding = problem class (fix all instances + root cause).
- **D10** Record the reference-project path in the architecture doc.
- **D11** Verify the enforcement guards (portable skills, mandatory skill-mapping in plans, no unjustified "adapt", reviewer skill check, arch-doc skill-inventory scan) are active in sprint-rituals — not just inherited assumptions.

## Open items (need user input or reference inspection)

- **O1** Confirm exact reference-project path (4 candidates on the owner's machine).
- **O2** Derive the domain-service / computation-placement target pattern from the reference project (fixes `analytics-computation-service` smell + #4/#5/#6).
- **O3** Confirm earlier-triage decisions: A1 (domain→Jira coupling), B4-Identity (encapsulate Person/User?), D2-Xray-secret-at-rest.
