# Pass 0 — Foundations

**Feature Spec:** None (ad-hoc refactoring — scope in `docs/backlog.md` "Pass 0 — Foundations"; binding ADRs in `docs/architecture/decisions.md`)

## Context

The codebase is mid-migration from the Fokus-standalone era (`v1.md`) toward the decided modular monolith (`decisions.md`). Four cross-cutting, mechanical defects block clean per-feature work in later passes:

- **No Central Package Management** — every `.csproj` carries inline `Version=` (ADR-013, problem #2).
- **Deprecated FastEndpoints API** — ~115 legacy `Send*Async` calls (`SendOkAsync`, `SendNoContentAsync`, `SendNotFoundAsync`, …) across 53 files; FastEndpoints pinned at 6.0.0 (ADR-011/012, problem #1e).
- **Fat composition root** — `Host/Program.cs` inlines migration, SQLite directory bootstrap, AppSettings/Xray seeding, MassTransit, gRPC server registration, and SignalR mapping that ADR-014 assigns to the owning modules (problem #3).
- **Stale convention text** — `docs/conventions/project-rules.md` still describes "Services/ = Microservices", an `ApiGateway/` (YARP), and `docker-compose`, contradicting ADR-001 (finding E1).

This is a **refactoring** pass: behavior-preserving. The acceptance bar is a green build with the four debts removed — no functional change. Every defect is a **problem class** (ADR-003 / engineering-discipline): fix all instances, not the cited examples.

**Decision order (ADR-003):** correct skill → reference project `<reference-repo>` → fix/create the skill. The reference project has been read; its patterns anchor every step below.

## Scope

**In scope**
1. CPM: `src/Directory.Packages.props`; strip `Version=` from all `<PackageReference>` in all 18 projects.
2. Framework currency: migrate every legacy FastEndpoints `Send*Async` to the modern `Send.*` API; bump FastEndpoints + core packages to latest stable; fix resulting breaking changes.
3. Thin composition root: distribute Host wiring into module DI extensions + an `app.Migrate<TDbContext>()` helper; `Program.cs` ends as Add / Init / Use only.
4. Convention alignment: rewrite the stale stack-conventions block to the modular monolith.

**Explicitly out of scope**
- **Identity extraction** (ADR-015 / Pass 4). The Fokus `Features/Auth/*` and `Fokus.Domain/Auth/*` files are migrated *mechanically* in Step 2 (their legacy `Send*Async` calls are migrated like every other call — that's the problem-class rule) but are **not deleted, moved, or restructured** here. Migrating a call site does not decide a file's fate.
- **Domain enrichment / endpoint-split / naming** (Passes 1–3). No VO extraction, no `*Request`→`Command` rename, no endpoint-file splits in this pass.
- **Skill creation.** No CPM skill or framework-currency skill exists (confirmed gaps — see Skill Mapping). Creating them is Pass 1 (skill repair). This pass derives the patterns from the reference project and logs the gaps to `lessons.md`.
- **RabbitMQ / real transport.** MassTransit stays in-memory; only its registration *location* moves.
- **The deeper `src/Services/Fokus/CLAUDE.md` v1-vs-current drift** (its "no auth / no MassTransit / no gRPC" claims overlap Pass 4) — only the structure/deployment lines E1 names are touched. See Open Questions.

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | (none — CPM) | — | Reference props: `<reference-repo>\src\Directory.Packages.props`. All 18 csproj under `src/`. | Log `central-package-management` skill gap to lessons.md |
| 2 | (none — framework currency) | — | Modern `Send.*` map; FastEndpoints bump; exclude one HttpClient `SendAsync`. Reference uses FastEndpoints 7.0.1. | Log `framework-currency` rule/skill gap to lessons.md |
| 3 | service-registration | Build | `Migrate<TDbContext>(this IHost)` helper does not exist locally → build it in `Blocks.EntityFrameworkCore` (reference: `Blocks.EntityFrameworkCore/Extensions/DbContextExtensions.cs`), then follow the skill. | |
| 4 | service-registration | Follow | Module DI: `AddFokusModule`/`AddIdentityModule` absorb MassTransit, gRPC server, SignalR, seeding; `Map{Module}Endpoints`; thin `Program.cs`. | |
| 5 | (none — docs) | — | Rewrite Repo Structure block in `docs/conventions/project-rules.md` to modular monolith. | |
| 6 | (none — KB) | — | KB has no entry for composition/packaging; verify and note. | |

**Disposition notes**
- Steps 1, 2, 5 are **None** because no skill covers Central Package Management or framework-currency, and convention text is documentation, not a code pattern. Per ADR-003 the pattern is **reference-derived** (the reference project uses CPM + FastEndpoints 7 `Send.*`), and the missing skills are logged for Pass 1. This is justified inline detail, not skipped-skill license.
- Steps 3–4 are **Build then Follow** / **Follow** of `service-registration`, which already prescribes `Add{Module}Module`, `Map{Module}Endpoints`, `app.Migrate<T>()`, and layer placement for MassTransit/gRPC/seeding. Do not restate the skill's structure in the step — invoke it.

## Domain Model Changes

None. This pass touches no aggregate, entity, value object, or domain event. (Behavior-preserving refactor.)

## Data Model Changes

None. No new tables, columns, or migrations. The existing migration *execution* and the AppSettings/Xray seed *relocate* (Steps 3–4) but produce the identical database state.

## Implementation Steps

Run `dotnet build SprintRituals.slnx` after **each** step (known-deviation: build verification batched to end). Windows file-lock MSB errors (MSB3026/27/3492) are not compiler errors — filter with `error CS`.

### Step 1 — Central Package Management (ADR-013)

Introduce CPM across all 18 projects. **Disposition: None** (no skill; pattern from reference `<reference-repo>\src\Directory.Packages.props`).

- **Create** `src/Directory.Packages.props` with `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>` and (recommended, matching reference) `<CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>`. Declare one `<PackageVersion Include="..." Version="..." />` per distinct package, using the versions currently pinned in the csproj files.
- **Edit** every `.csproj` under `src/` (all 18): strip the `Version="..."` attribute from each `<PackageReference Include="..." />`. Preserve all other attributes/child elements (e.g. `Microsoft.EntityFrameworkCore.Design`'s `<PrivateAssets>`/`<IncludeAssets>` block stays).
- **Problem-class sweep:** after editing, grep `PackageReference Include=.*Version=` under `src/` returns **zero** matches. Known current version owners to consolidate: EF Core 10.0.0-preview.4.25258.110, FastEndpoints 6.0.0 (changes in Step 2), MassTransit 8.4.1, protobuf-net.Grpc.AspNetCore 1.1.1, Scalar.AspNetCore 2.4.18, Authentication.Google 10.0.7, FluentValidation 11.11.0, Refit.HttpClientFactory 8.0.0, System.ServiceModel.Primitives 6.2.0, Microsoft.Extensions.Http / Options.DataAnnotations 10.0.0-preview.4.25258.110, MassTransit.Abstractions 8.4.1.
- **Accept:** `dotnet build SprintRituals.slnx` green; no per-project versions remain; a single `<PackageVersion>` per package.
- **Confidence: high** — mechanical, reference-anchored.

### Step 2 — FastEndpoints currency + `Send.*` migration (ADR-011, ADR-012)

Migrate the deprecated API and bump the framework. **Disposition: None** (no framework-currency skill; reference uses FastEndpoints 7.0.1 with `Send.*`).

- **Bump FastEndpoints to the latest stable** in `src/Directory.Packages.props` (`FastEndpoints`, `FastEndpoints.Swagger`, and any FastEndpoints.* packages). Reference project runs 7.0.1 as a known-good floor; use the latest stable ≥ that. Apply to all consumers: `Blocks.FastEndpoints`, `Blocks.Domain`, `Identity.API`, `Fokus.API`, `Host`.
- **Migrate every legacy `Send*Async` call** to the modern `Send.*` form. Canonical map: `SendOkAsync(x, ct)` → `Send.OkAsync(x, ct)`; `SendAsync(x, statusCode, ct)` → `Send.ResponseAsync(...)`; `SendNoContentAsync(ct)` → `Send.NoContentAsync(ct)`; `SendNotFoundAsync(ct)` → `Send.NotFoundAsync(ct)`; `SendErrorsAsync(...)` → `Send.ErrorsAsync(...)`; `SendStringAsync`/`SendRedirectAsync`/`SendForbiddenAsync`/`SendUnauthorizedAsync` → their `Send.*` equivalents. Confirm exact modern names against the FastEndpoints version you land on (the `Send.*` static is the v6+ surface; some helpers were renamed across 6→7 — verify against the installed package, not memory).
- **Problem-class scope:** all ~115 occurrences across 53 files (Fokus `Features/**`, Identity `Features/Auth/**`, including the Fokus `Features/Auth/**` files that Pass 4 will later remove). After migration, grep for legacy names (`\bSend(Ok|NoContent|NotFound|Errors|String|Redirect|Forbidden|Unauthorized|Created)\w*Async\b` as a *method call*, not `Send.`) returns **zero** in `*.cs` under `src/`.
- **DO NOT TOUCH — known false positive:** `src/Modules/Xray/Xray.GraphQL/GraphQLXrayClient.cs:296` `await httpClient.SendAsync(request, ct)` is a **`System.Net.Http.HttpClient`** call, not a FastEndpoints method. Leave it exactly as-is.
- **Fix breaking changes** introduced by the version bump (signature/namespace shifts, `Configure()` or attribute-routing changes, validator base changes). Resolve them in place; do not restructure endpoints (that's Pass 2).
- **Accept:** build green; zero legacy `Send*Async` method calls remain (excluding the HttpClient false positive); all endpoints return the same status codes/payloads as before.
- **Confidence: medium** — the 6→latest bump may surface breaking changes; verify modern method names against the actual installed package and use `diagnose` if a migration error is non-obvious.

### Step 3 — `Migrate<TDbContext>()` helper (ADR-014)

Build the migration helper the thin Program.cs depends on. **Disposition: Build then Follow `service-registration`.**

- **Build** `Migrate<TDbContext>(this IHost app) where TDbContext : DbContext` in `src/BuildingBlocks/Blocks.EntityFrameworkCore/Extensions/DbContextExtensions.cs` (create the file/folder; reference shape: `<reference-repo>\src\BuildingBlocks\Blocks.EntityFrameworkCore\Extensions\DbContextExtensions.cs`). Creates a scope, resolves the context, calls `Database.Migrate()`, returns the host.
- **Preserve the SQLite directory-creation side effect.** Today `Program.cs:44–72` ensures the SQLite data directory exists before `MigrateAsync` (SQLite does not create the parent dir). The helper must reproduce this: derive the `DataSource` path from the connection string and `Directory.CreateDirectory(...)` before migrating. Do **not** drop this — losing it breaks first-run startup. (If you prefer the dir-creation to live in the module's `Add{Module}Persistence`/migrate step instead of the generic helper, that is acceptable as long as the behavior is preserved; the generic helper is the reference default.)
- **Accept:** build green; `app.Migrate<IdentityDbContext>()` and `app.Migrate<FokusDbContext>()` compile and, on a clean machine with no `data/` dir, create both databases exactly as before.
- **Confidence: high** — direct reference port.

### Step 4 — Distribute wiring into modules; thin Program.cs (ADR-014)

Move all per-module wiring out of `Host/Program.cs` into the owning module's DI/endpoint extensions. **Disposition: Follow `service-registration`** (invoke the skill for `Add{Module}Module` / `Map{Module}Endpoints` structure and MassTransit/gRPC/seeding placement). Feature-specific relocations:

- **Fokus module** (`src/Services/Fokus/Fokus.API/DependencyInjection.cs`):
  - Absorb `AddCodeFirstGrpc()` (gRPC server) and the in-memory MassTransit registration (`AddMassTransit(... UsingInMemory ...)`) into `AddFokusModule` — these belong to Fokus (it owns the gRPC services and publishes `SprintSynced`).
  - Absorb the **AppSettings default-row seed** and the **Xray-settings-from-config seed** (currently `Program.cs:75–90`) into Fokus DI/init (a seeding extension invoked from the module, per the skill's seeding-in-owning-module rule). Preserve exact semantics: seed default AppSettings only if none exists; set Xray fields only when config has ClientId+ClientSecret.
  - Keep SignalR: `AddSignalR()` already in `AddFokusModule`; `MapHub<SprintHub>` already in `UseFokusMiddleware`. Move the two `app.MapGrpcService<...>()` calls (`SprintAnalyticsGrpcService`, `SprintCatalogGrpcService`) into a Fokus endpoint-mapping extension (extend `UseFokusMiddleware` or add `MapFokusEndpoints`).
- **Identity module** (`src/Services/Identity/Identity.API/DependencyInjection.cs`): ensure `AddIdentityModule` owns all Identity wiring; move the minimal-API `LoginEndpoint.MapLoginEndpoint(app)` and the global `ActiveUserPreProcessor` configurator registration into an Identity endpoint-mapping extension (`MapIdentityEndpoints`) so the Host does not name Identity internals. The shared `UseFastEndpoints` multi-assembly call stays in the Host's Use section (it is cross-module composition, not per-module wiring).
- **Host `Program.cs`** (`src/Host/Program.cs`) ends as three sections only:
  - **Add:** `AddIdentityModule` + `AddFokusModule` + shared `AddOpenApi` + the multi-assembly `AddFastEndpoints`. No `AddCodeFirstGrpc`/`AddMassTransit`/`AddSignalR` directly in the Host.
  - **Init:** `app.Migrate<IdentityDbContext>()` then `app.Migrate<FokusDbContext>()` (order preserved — Identity first); module seeding invoked here or from module init. No inline scope/directory/migrate/seed block.
  - **Use:** shared pipeline (`GlobalExceptionMiddleware`, static files, auth) + `UseFastEndpoints` (the global `ActiveUserPreProcessor` configurator may stay here or move with Identity, your call — keep it applied) + `app.MapIdentityEndpoints()` + `app.UseFokusMiddleware()`/`MapFokusEndpoints()` + dev-gated OpenAPI/Scalar + `MapFallbackToFile`.
  - **Acceptance for thinness:** `Program.cs` contains **no** `MapGrpcService`, no `AddMassTransit`/`UsingInMemory`, no `Database.MigrateAsync`, no `SqliteConnectionStringBuilder`/`Directory.CreateDirectory`, and no `fokusDb.AppSettings.Add(...)` / Xray seed block. Grep these tokens in `Program.cs` → zero.
- **Behavior parity (must not change):** migration order (Identity → Fokus), the data-dir bootstrap, seed semantics, the `ActiveUserPreProcessor` global pre-processor, the login minimal-API route, the gRPC h2c endpoint, SignalR hub path `/hubs/sprint`, dev-only Scalar, and SPA fallback all behave identically.
- **Accept:** build green; app starts; all endpoints, gRPC services, SignalR, seeding, and migrations behave as before.
- **Confidence: medium** — multi-file relocation; the seeding move and the Identity login/pre-processor relocation are the trickiest. Explore the existing wiring before moving; preserve, don't rewrite.

### Step 5 — Convention alignment to modular monolith (ADR-001, finding E1)

Rewrite the stale stack-conventions text. **Disposition: None** (documentation).

- **Edit** `docs/conventions/project-rules.md` — the **Repo Structure** block (lines ~32–47): replace "Services/ = Microservices", the `ApiGateway/` (YARP) line, and the `docker-compose` line with the decided structure: single `src/Host` Web SDK deployable composing modules; `Services/{Svc}/{Svc}.API|Domain|Persistence` as **class libraries**; `Modules/` for external-system integration; **no API gateway, no docker-compose** in this monolith. Mirror the accurate tree already in the root `CLAUDE.md` "Repo Structure". Keep all other sections of the file unchanged.
- **Problem-class note:** the same stale claim also appears in the architect agent file's inlined stack-conventions block and (partially) in `src/Services/Fokus/CLAUDE.md`. The agent-file copy is a **flow artifact** owned by the learner (`improve-flow`) — out of scope here; flag it (Open Questions). The Fokus CLAUDE.md drift is broader than E1 and overlaps Pass 4 — out of scope here; flag it. The matches in `.claude/skills/system-design`, `create-service*`, `create-module` are legitimate general-purpose scaffolding vocabulary (YARP/microservice as *options*), **not** stale project-convention text — leave them (Pass 1 territory if anything).
- **Accept:** `project-rules.md` Repo Structure describes the modular monolith with no "Microservices/ApiGateway/docker-compose" claims.
- **Confidence: high.**

### Step 6 — KB check (kb-maintenance)

Verify KB consistency. **Disposition: None.**

- Read `docs/kb/index.md`. Pass 0 changes packaging/composition, not business rules — likely **no KB entry** exists for composition root or package management, so no business-rule update is needed. If `index.md` is absent or has no relevant entry, record "no KB impact" in implementation.md and stop. Do **not** invent a new KB entry for infra (per kb-maintenance "What NOT to Capture").
- **Accept:** KB index reviewed; either no change needed (documented) or the single affected entry updated via the Edit tool.
- **Confidence: high.**

## Cross-Service Changes

None. The `SprintSynced` integration event contract, the two gRPC service contracts (`ISprintAnalyticsService`, `ISprintCatalogService`), and their consumers are unchanged — only the *registration location* of the gRPC server and MassTransit bus moves (Step 4). No contract, consumer, or publisher signature changes.

## Migration Notes

No new EF migrations. Existing migrations run unchanged via the new `app.Migrate<T>()` helper (Step 3), in the same order (Identity → Fokus). Seed data (AppSettings default row, Xray-from-config) relocates to the Fokus module with identical semantics (Step 4). On a clean checkout the app must still create `data/identity.db` and `data/fokus.db` on first run.

## Testing Strategy

Manual (no automated suite — project decision). After Step 4, smoke-test:
1. Clean run with no `data/` dir → both DBs created, AppSettings default row seeded, app starts.
2. With Xray config present → Xray settings seeded into AppSettings.
3. One GET and one POST endpoint per module return the same status + payload as before the `Send.*` migration (spot-check Identity `GetMe`, Fokus `GetSettings`, a Fokus analytics endpoint, a Settings POST).
4. gRPC h2c endpoint (port 5001) responds; SignalR `/hubs/sprint` connects; `SprintSynced` still publishes after a sync.
5. Dev-only Scalar UI loads; SPA fallback serves `index.html`.
Per-step gate: `dotnet build SprintRituals.slnx` green after every step.

## KB Impact

Expected **none** — Pass 0 is infrastructure/packaging with no business-rule, formula, or edge-case change. Step 6 verifies this against `docs/kb/index.md`. (KB review is a numbered step, not a trailing note, so the done check catches it.)

## Open Questions

None blocking — Pass 0 is fully specified by the ADRs and the backlog; no user input required.

**Flags for the team-lead (not blockers, route as you see fit):**
1. The **architect agent file** (`.claude/agents/...`) inlines the same stale "Services/ = Microservices" stack-conventions block fixed in Step 5. That's a flow artifact owned by the **learner** (`improve-flow`), not this code/convention pass — recommend queuing a learner pass.
2. `src/Services/Fokus/CLAUDE.md` carries broader v1-standalone drift ("no auth / no MassTransit / no gRPC / thin domain by design") that overlaps **Pass 4** (Identity extraction, ADR-015) and Pass 3 (domain enrichment). Out of scope for Pass 0's E1 structural fix; recommend folding into the relevant later pass rather than a piecemeal edit now.
