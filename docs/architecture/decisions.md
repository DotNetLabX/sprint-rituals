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
- Cross-aggregate computation → **domain services** in `{Module}.Domain/{Area}/` (e.g. `Fokus.Domain/Analytics/SprintSummaryCalculator.cs`).
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

## Supersession note
`v1.md` remains valid for Fokus domain detail (entity classification, HealthScore formula, indexes). It is **superseded** on: standalone deployment, "no auth", "no gRPC/MassTransit/MediatR" scope lines, thin-domain-as-end-state, and compute-in-endpoints — all overridden by ADR-001/004/005/006/007 above.
