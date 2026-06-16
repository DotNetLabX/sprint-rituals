# Architecture Decisions

**Status:** Current · **Date:** 2026-06-05
**Authority:** This register is authoritative. Where it conflicts with `v1.md` (Fokus standalone era), **this wins**.
**Source:** Decisions taken during the refactor-triage session. Evidence in `docs/proposals/refactor-triage.md`, `skill-adherence-root-cause.md`, `skill-domain-summary.md`, `reference-patterns-domain.md`, `reference-patterns-structure.md`.

Each decision is a binding contract. Deviating in a plan or implementation is a review failure unless this register is amended first.

---

## ADR-001 — Modular monolith, single Host
The system is a modular monolith. `src/Host` is the only Web SDK / deployable project; every `*.API` is a class library composed into one process. Module boundaries (API/Domain/Persistence per module, DB-per-module, gRPC + integration events across modules) are kept so future service extraction needs only the Host split. (Ratifies `proposals/suite-integration-architecture.md` §1.)

## ADR-002 — Reference project
**`<reference-repo>`** is the canonical pattern source. When the codebase smells and no correct skill covers it, derive the pattern from this repo, then fix/create the skill. (The reference uses .NET, FastEndpoints, MediatR, Mapster, EF Core, MassTransit, Central Package Management.)

## ADR-003 — Decision order when code smells
1. A correct skill exists → **apply the skill**.
2. No skill, or the skill itself smells → **use the reference project (ADR-002)** → then **fix or create the skill**.
Every finding is a **problem class**: fix all instances across the codebase, not only the cited example, and fix the root cause (skill/process), not only the symptom. Enforced by `.claude/rules/engineering-discipline.md`.

## ADR-004 — Light stack retained (no MediatR, no Application layer)
Each module is **API + Domain + Persistence** only. FastEndpoints endpoint classes are the command/query handlers. **No MediatR, no Application project.** (Confirms `v1.md`; chosen over full reference alignment.)

## ADR-005 — Computation lives in the Domain layer
Analytics/derived computation is **domain logic**, not API plumbing.
- Single-aggregate logic → **aggregate behavior methods**.
- Cross-aggregate computation → **domain services** in `{Module}.Domain/{Area}/` (e.g. `Fokus.Domain/Analytics/SprintSummaryAnalyzer.cs`).
- Domain services **return domain types / value objects** — never API response DTOs.
- Endpoints call the domain service and **Mapster-map the domain result to the response DTO** at the boundary.
- **Minimize API service classes.** A service class in `*.API/Features/` is a smell signalling logic that belongs in the domain — justify it or move it. (Resolves problems #4, #5, #6, #7.)

## ADR-006 — No DTO enters the Domain
Domain projects must not reference external/integration/response DTOs. Specifically: **drop `Fokus.Domain → Jira.Contracts`**; remove `FromJira(...)` factories from aggregates; map Jira/Xray DTOs → domain at the API/Persistence boundary via Mapster. Domain methods return domain types; mapping to response DTOs happens in the endpoint. (Resolves problem A1. Reference: *"No DTO enters the domain."*)

## ADR-007 — Rich aggregates with value objects
Aggregates encapsulate state: private/`internal` setters, `internal` constructor, **static factory** for creation, **behavior split into a partial class** (`Entities/{N}.cs` state + `Behaviors/{N}.cs` methods). Group cohesive scalar fields into **value objects / owned types** rather than flat scalars — e.g. `AppSettings` → `BugRatioConfig`, `XrayConfig`, `CycleTimeConfig`, persisted via EF `ComplexProperty`/owned types. Applies to **all** aggregates including Identity `Person`/`User` (resolves B4-Identity). (Resolves problem #1d.)

## ADR-008 — Vertical-slice file layout (FastEndpoints)
Two files per feature:
- `{Feature}Endpoint.cs` — endpoint class + handler only.
- `{Feature}Command.cs` (mutations) / `{Feature}Query.cs` (reads) — request + response + DTOs + validator.
No endpoint+request+response+validator crammed in one file. Fix existing violations with the `extract-endpoint-types` skill. (Resolves problem #1a.)

## ADR-009 — Endpoint type naming
- Request type: **`{Action}{Entity}Command`** (mutations) / **`{Action}{Entity}Query`** (reads). The command/query **record IS the request body**. No `Request` suffix.
- Response: `{Action}{Entity}Response`. Validator: `{Command}Validator`.
- Prefer `record` for commands/queries/responses/DTOs.
(Resolves problem #1b. ~37 `*Request` types to rename.)

## ADR-010 — Mapping convention
Command/query field names **match the entity** so mapping is trivial. Trivial mapping → **Mapster** (`IRegister` configs + `.Adapt<T>()`). Non-trivial mapping → **factory method in the aggregate behavior partial class**. Do not hand-assign properties field-by-field in handlers. (Resolves problem #1c.)

## ADR-011 — FastEndpoints currency
Use the modern **`Send.*` API** (`Send.OkAsync`, `Send.ResponseAsync`, …). **Never `SendOkAsync`** or other legacy `Send*Async` methods (~89 calls across 54 files to migrate). Attribute routing is primary; split `Configure()` into a `*.Configure.cs` partial only for complex cases. (Resolves problem #1e.)

## ADR-012 — Always-latest framework versions
Track the latest stable versions of the core stack. Target framework is **.NET 10** (met). Upgrade FastEndpoints and other core packages to latest as part of the refactor.

## ADR-013 — Central Package Management
Introduce **`Directory.Packages.props`** at `src/` with `ManagePackageVersionsCentrally=true`; declare every version as `<PackageVersion .../>`; `.csproj` files use bare `<PackageReference Include="..."/>` with no `Version`. (Resolves problem #2.)

## ADR-014 — Thin composition root
`Host/Program.cs` is **Add / Init / Use** only. Each module owns its wiring:
- `Add{Module}Module(config)` (composes `AddApiServices` + `AddPersistenceServices`) and `Map{Module}Endpoints()`.
- Migrations via `app.Migrate<TDbContext>()` helper — not inline directory/migrate blocks.
- MassTransit, gRPC server/client registration, and **seeding** move into the owning module's DI, not the Host.
(Resolves problem #3. Pattern: `service-registration` skill + reference.)

## ADR-015 — Identity extraction (in-flight)
Completing the Fokus → Identity extraction (move/retire `Fokus.API/Features/Auth` + `Fokus.Domain/Auth` per the suite decision; finish the `RemoveAppUsers` cut) is tracked refactor work, not a static defect. (Problem A2.)

---

## Reflekt integration ADRs

> Promote `proposals/suite-integration-architecture.md` (Decided, 2026-05-16) into the binding register. Previously only §1 was ratified (ADR-001); the Reflekt-specific decisions below lived only in the proposal and were therefore unenforceable. Port **sequencing** (lift-and-shift first, then refactor/integrate) is a program decision in `docs/backlog.md`, not an ADR.

## ADR-016 — Reflekt joins as a module; Identity owns Person/User
Reflekt is ported in as a module (`src/Services/Reflekt/{API,Domain,Persistence}`, `reflekt.db`), composed by the Host like Identity and Fokus. **Identity owns Person and User** (ADR-001 boundary): Reflekt does **not** keep its own `User` aggregate or its own Google/cookie authentication — it relies on the Identity cookie, exactly as Fokus does. Reflekt **retains** its guest-token authentication for anonymous retro participants. A Reflekt `Participant` carries an **optional FK to Person**; cross-module Person create/lookup uses the existing `IPersonService` gRPC (Reflekt becomes a second client alongside Fokus). Retro access control is set by **ADR-021** (open + SM waiting room); the participant↔Person mapping, including the guest case, by **ADR-022** (no Person for guests). (Promotes §3, §10.)
**Timing (owner-ratified 2026-06-14):** Reflekt becomes the second `IPersonService` gRPC client with `adhoc-RetroWaitingRoom` (when it must *mint* Persons for not-yet-known Google participants), **not** in RA2 (`adhoc-ReflektIdentityUnification`). RA2 only *reads* the Person id from a `person_id` cookie claim — no gRPC client.

## ADR-017 — Fokus owns sprint data; Reflekt references it
Fokus is the source of truth for Jira sprints and all sprint analytics. Reflekt does **not** create or own Jira sprints. A retro references Fokus sprint(s) by ID and never holds sprint metadata as authoritative state. (Promotes §4.)

## ADR-018 — RetroSession aggregate; one retro spans 1..N Fokus sprints
Reflekt's retro aggregate is named **`RetroSession`** — not `Sprint`. A retro is not a Jira sprint, and a single retro **occasionally covers more than one sprint** (when a retro is skipped, the next one covers two). A `RetroSession` therefore references **1..N Fokus sprints**; the common single-sprint case is a one-element set. Reflekt stores a **`FokusSprintSnapshot`** read-projection — one row per referenced Fokus sprint, overwritten on re-sync. The rename from the current `Sprint` aggregate and the 1:N link land in the sprint-ownership pass, not the mechanical lift. (Refines §4; new decision, owner 2026-06-14.)

## ADR-019 — Fokus → Reflekt data flow is one-directional and anonymized
Cross-module data flows **Fokus → Reflekt only**:
- **gRPC on demand** — Reflekt calls Fokus for anonymized sprint metrics (`ISprintAnalyticsService` / `AnonymizedSprintSummary`) when the sprint-context panel loads; cached per retro session.
- **Integration event** — `SprintSynced` (already published by Fokus) is consumed by Reflekt to refresh the `FokusSprintSnapshot` projection.
Privacy is enforced **at the contract**: only anonymized aggregates cross the boundary — no per-developer metrics, no individual ticket assignments. **Reflekt → Fokus is deferred** — no concrete metric Fokus can derive from retro data yet. (Promotes §5, §6, §9.)

## ADR-020 — Frontend: one routing shell, module-isolated SPAs
The client is one routing shell composing module-isolated Vue apps: `/retro/*` (Reflekt), `/dashboard/*` (Fokus), `/settings/*` (shared / Identity). Each module owns its Pinia stores, components, and views; the shared layer is routing + Google-auth state (read from the cookie) + a thin UI kit. The build topology (single Vite app vs. two builds into `Host/wwwroot`) is **deferred** to the frontend-merge pass. (Promotes §8.)

## ADR-021 — Reflekt retro access: open participation, SM waiting room
Retro access is **open but Scrum-Master-gated**, not invitation-gated. Anyone may attempt to join a retro; the SM approves entry via a **waiting room**. Participants already known to Identity (an existing `Person`/`User` for that Google account) skip the room; **guests (no account) and Google users not yet in Identity** wait for SM approval. This replaces Identity's suite-login invitation gate *for Reflekt* — the global `ActiveUserPreProcessor` is **scoped** so it does not 401 Reflekt's retro endpoints (a retro participant is a `Person`-or-guest, not necessarily a suite `User`). **The waiting room is a separate, later feature** (`adhoc-RetroWaitingRoom`, `docs/backlog.md`), not part of RA2; RA2 builds compatible plumbing and leaves the approval step as an unbuilt seam. Conscious departure from proposal §10's zero-friction guest framing. (Resolves RA2 Q1.)

## ADR-022 — Reflekt participant ↔ Person: no Person for guests
A `Person` is created **only for Google-authenticated participants** (Identity mints it at login; Reflekt reads it from the cookie and sets `Participant.PersonId`). **Guests get no `Person`** — a guest is `Participant{GuestToken, DisplayName}` with `PersonId` null. No guest-Person creation, no cross-session guest matching (no IP, no fingerprinting). This **resolves the §3↔§10 tension** in `suite-integration-architecture.md` toward §10's orphan-avoidance rationale: §3 loosely implied a guest Person record; §10 argues against orphan Persons and complex matching. Guests are a free-tier demo edge case — a per-retro `Participant` row suffices; cross-retro/cross-module correlation applies only to real members, who arrive via Google. A guest who later authenticates simply gains their Google `Person`; old guest rows are not retro-linked. (Resolves RA2 Q2; supersedes the deferred-mechanism note in ADR-016.)

---

## Supersession note
`v1.md` remains valid for Fokus domain detail (entity classification, HealthScore formula, indexes). It is **superseded** on: standalone deployment, "no auth", "no gRPC/MassTransit/MediatR" scope lines, thin-domain-as-end-state, and compute-in-endpoints — all overridden by ADR-001/004/005/006/007 above.
