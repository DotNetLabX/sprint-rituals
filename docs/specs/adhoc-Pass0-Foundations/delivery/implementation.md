# Implementation — adhoc-Pass0-Foundations

**Date:** 2026-06-05
**Developer:** nexus-net:developer
**Plan:** docs/specs/adhoc-Pass0-Foundations/delivery/plan.md

---

## Step 1 — Central Package Management (ADR-013)

**Status:** Complete. Build: green.

**What was done:**
- Created `src/Directory.Packages.props` with `ManagePackageVersionsCentrally=true` and `CentralPackageTransitivePinningEnabled=true`.
- Declared 18 distinct `<PackageVersion>` entries. All packages bumped to latest stable (EF Core 10.0.8, FastEndpoints 8.1.0, MassTransit 9.1.2, etc. — see props file for full list).
- Stripped `Version=` from all `<PackageReference>` entries across all 18 `.csproj` files.
- Post-edit grep: `PackageReference Include=.*Version=` → zero matches under `src/`.

**Files modified:**
- `src/Directory.Packages.props` (created)
- `src/BuildingBlocks/Blocks.Core/Blocks.Core.csproj`
- `src/BuildingBlocks/Blocks.Domain/Blocks.Domain.csproj`
- `src/BuildingBlocks/Blocks.EntityFrameworkCore/Blocks.EntityFrameworkCore.csproj`
- `src/BuildingBlocks/Blocks.FastEndpoints/Blocks.FastEndpoints.csproj`
- `src/BuildingBlocks/Blocks.Contracts/Blocks.Contracts.csproj`
- `src/Modules/Jira/Jira.RestApi/Jira.RestApi.csproj`
- `src/Modules/Xray/Xray.GraphQL/Xray.GraphQL.csproj`
- `src/Services/Identity/Identity.API/Identity.API.csproj`
- `src/Services/Identity/Identity.Persistence/Identity.Persistence.csproj`
- `src/Services/Fokus/Fokus.API/Fokus.API.csproj`
- `src/Services/Fokus/Fokus.Persistence/Fokus.Persistence.csproj`
- `src/Host/Host.csproj`

**Deviations:** None. `Microsoft.EntityFrameworkCore.Relational` was added to the props file in Step 3 when the `Migrate<T>()` helper required it (see Step 3 notes).

---

## Step 2 — FastEndpoints currency + `Send.*` migration (ADR-011, ADR-012)

**Status:** Complete. Build: green.

**What was done:**
- Bumped FastEndpoints to 8.1.0 (latest stable; reference project floor was 7.0.1) via CPM in Step 1.
- Used `sed` to mechanically replace all legacy `Send*Async` call forms across 53 endpoint files:
  - `await SendOkAsync(` → `await Send.OkAsync(`
  - `return SendOkAsync(` → `return Send.OkAsync(`
  - `await SendNoContentAsync(` → `await Send.NoContentAsync(`
  - `await SendNotFoundAsync(` → `await Send.NotFoundAsync(`
  - `await SendErrorsAsync(` → `await Send.ErrorsAsync(`
  - Plus `SendCreatedAtAsync`, `SendStringAsync`, `SendRedirectAsync`, `SendForbiddenAsync`, `SendUnauthorizedAsync` variants (none present in this codebase, treated as zero-match).
- Post-migration grep: zero legacy method-call patterns remain.
- Known false positive confirmed untouched: `src/Modules/Xray/Xray.GraphQL/GraphQLXrayClient.cs:296` `await httpClient.SendAsync(request, ct)` is `System.Net.Http` — left exactly as-is.
- **Breaking change — Refit 11.0.1:** `ApiExceptionBase.Content` no longer exists (moved to `ApiException`). Fixed `src/Modules/Jira/Jira.RestApi/RestApiJiraClient.cs:165` by casting: `(response.Error as ApiException)?.Content`. Behavior preserved — `ParseError` still receives the raw response body string.
- **Missed file — `CreateInvitationEndpoint.cs`:** The initial grep pattern (`await SendOkAsync|await SendNoContentAsync|...`) did not match `await SendAsync(response, 201, ct)` — the bare `SendAsync` with explicit status code. Migrated to `await Send.ResponseAsync(response, 201, ct)`. Post-fix grep for bare `SendAsync` (excluding `httpClient.SendAsync`) confirmed zero remaining.

**Files modified:**
- All 53 `*Endpoint.cs` files under `src/Services/Identity/Identity.API/Features/` and `src/Services/Fokus/Fokus.API/Features/`
- `src/Services/Fokus/Fokus.API/Features/Auth/CreateInvitation/CreateInvitationEndpoint.cs` (bare `SendAsync` → `Send.ResponseAsync`)
- `src/Modules/Jira/Jira.RestApi/RestApiJiraClient.cs` (Refit breaking change fix)

**Deviations:** None for the Send.* migration itself. The Refit `ApiException.Content` fix is a breaking-change resolution required by the version bump (ADR-012 "always-latest framework versions") — not a scope deviation.

---

## Step 3 — `Migrate<TDbContext>()` helper (ADR-014)

**Status:** Complete. Build: green.

**What was done:**
- Created `src/BuildingBlocks/Blocks.EntityFrameworkCore/Extensions/DbContextExtensions.cs`.
- `Migrate<TDbContext>(this IHost host)` creates a DI scope, resolves `TDbContext`, calls `EnsureDataDirectory` (parses `Data Source=` from the connection string using `GetConnectionString()`, derives the parent dir, calls `Directory.CreateDirectory`), then calls `context.Database.Migrate()`. Returns `IHost` for chaining.
- `EnsureDataDirectory` uses standard connection string segment parsing (splits on `;` then `=`) — no `SqliteConnectionStringBuilder` dependency in the building block.
- Added `Microsoft.EntityFrameworkCore.Relational` to `src/Directory.Packages.props` and `Blocks.EntityFrameworkCore.csproj` — required because `GetConnectionString()` and `Database.Migrate()` are relational-layer extensions. This package is an EF Core peer, not a new dependency category.

**Files modified:**
- `src/BuildingBlocks/Blocks.EntityFrameworkCore/Extensions/DbContextExtensions.cs` (created)
- `src/BuildingBlocks/Blocks.EntityFrameworkCore/Blocks.EntityFrameworkCore.csproj`
- `src/Directory.Packages.props` (added `Microsoft.EntityFrameworkCore.Relational`)

**Deviations:** Reference project's `DbContextExtensions.cs` does not include SQLite directory-creation (it uses SQL Server). The plan explicitly requires preserving this side effect; the helper was extended accordingly.

---

## Step 4 — Distribute wiring into modules; thin Program.cs (ADR-014)

**Status:** Complete. Build: green (after fixing two breaking changes — see Step 2 and Step 3 for their root causes; the build failure here was the first point where all three projects compiled together).

**What was done:**

### Fokus.API/DependencyInjection.cs
- Added `AddCodeFirstGrpc()` (gRPC server) to `AddFokusModule`.
- Added `AddMassTransit(x => x.UsingInMemory(...))` to `AddFokusModule`.
- Added `SeedFokusDataAsync(IServiceProvider, IConfiguration)` — async seeding extension: seeds default `AppSettings` row if missing; seeds Xray fields from config if `ClientId`+`ClientSecret` present. Semantics identical to original `Program.cs` block.
- Added `MapFokusEndpoints(WebApplication)` — maps `SprintAnalyticsGrpcService` and `SprintCatalogGrpcService` via `app.MapGrpcService<T>()`.
- Changed `MassTransit.Abstractions` → `MassTransit` in `Fokus.API.csproj` (full package needed for `AddMassTransit`).

### Identity.API/DependencyInjection.cs
- Added `MapIdentityEndpoints(WebApplication)` — calls `LoginEndpoint.MapLoginEndpoint(app)`. Identity owns its own login route mapping; Host no longer names Identity internals.

### Host/Program.cs
- Rewritten to pure Add/Init/Use:
  - **Add:** `AddIdentityModule` + `AddFokusModule` + `AddOpenApi` + `AddFastEndpoints` (multi-assembly). No `AddCodeFirstGrpc`/`AddMassTransit` directly.
  - **Init:** `app.Migrate<IdentityDbContext>()` then `app.Migrate<FokusDbContext>()` (order preserved) + `await app.Services.SeedFokusDataAsync(...)`.
  - **Use:** `GlobalExceptionMiddleware`, static files, auth, `MapIdentityEndpoints()`, `UseFastEndpoints` (with `ActiveUserPreProcessor` configurator), `UseFokusMiddleware()`, `MapFokusEndpoints()`, dev-gated OpenAPI/Scalar, `MapFallbackToFile`.
- Removed imports: `Fokus.API.Grpc`, `Identity.API.Features.Auth.Login`, `MassTransit`, `Microsoft.EntityFrameworkCore`, `ProtoBuf.Grpc.Server`, `Xray.GraphQL`, `Microsoft.Data.Sqlite`.
- Added imports: `Blocks.EntityFrameworkCore.Extensions`.

### Host.csproj
- Removed: `MassTransit`, `protobuf-net.Grpc.AspNetCore`, `Microsoft.AspNetCore.Authentication.Google`, `Microsoft.Data.Sqlite` (all moved to owning modules).
- Added project reference: `Blocks.EntityFrameworkCore` (for `Migrate<T>()`).

**Acceptance check (Program.cs token grep):** Zero matches for `MapGrpcService`, `AddMassTransit`, `UsingInMemory`, `Database.MigrateAsync`, `SqliteConnectionStringBuilder`, `Directory.CreateDirectory`, `AppSettings.Add`, `XrayClientId`.

**Files modified:**
- `src/Services/Fokus/Fokus.API/DependencyInjection.cs`
- `src/Services/Fokus/Fokus.API/Fokus.API.csproj`
- `src/Services/Identity/Identity.API/DependencyInjection.cs`
- `src/Host/Program.cs`
- `src/Host/Host.csproj`

**Deviations:** None. `ActiveUserPreProcessor` configurator stayed in Program.cs `UseFastEndpoints` block — it is cross-module composition (applies globally to all endpoints from all assemblies), not Identity-specific wiring. This matches the plan's "your call — keep it applied" note.

---

## Step 5 — Convention alignment to modular monolith (ADR-001, finding E1)

**Status:** Complete.

**What was done:**
- Replaced the stale "Repo Structure" block in `docs/conventions/project-rules.md` (lines ~32–45). Removed: "Microservices — one folder per service", `ApiGateway/ # YARP reverse proxy`, `docker-compose # Local dev environment`. Replaced with the accurate modular monolith tree: `Host/`, `BuildingBlocks/` with `Blocks.Contracts/`, `Services/{Svc}/{Svc}.API|Domain|Persistence` as class libraries, `Modules/` for external integration, `client/`.
- The closing sentence explicitly states: single deployable, no API gateway, no docker-compose.
- All other sections of the file left unchanged.

**Flags noted (not fixed — out of scope per plan):**
- The architect agent file inlines the same stale "Services/ = Microservices" block — flagged for learner (`improve-flow`).
- `src/Services/Fokus/CLAUDE.md` carries broader v1-standalone drift — flagged for Pass 4.

**Files modified:**
- `docs/conventions/project-rules.md`

**Deviations:** None.

---

## Step 6 — KB check

**Status:** Complete. No KB update needed.

**What was done:**
- Searched `docs/kb/` — directory contains no files; `docs/kb/index.md` does not exist.
- Pass 0 changes packaging, composition root, and framework API — no business rules, formulas, edge cases, or key file paths. Per `kb-maintenance` "What NOT to Capture", infra/packaging does not warrant a KB entry.
- Recorded: no KB impact.

---

## Breaking Changes Fixed (version bumps)

| Package | Old | New | Breaking change | Fix |
|---------|-----|-----|-----------------|-----|
| FastEndpoints | 6.0.0 | 8.1.0 | `Send*Async` → `Send.*` | Mechanical replacement (Step 2) |
| Refit.HttpClientFactory | 8.0.0 | 11.0.1 | `ApiExceptionBase.Content` removed | Cast to `ApiException` (Step 2) |
| MassTransit | 8.4.1 | 9.1.2 | None observed | — |
| EF Core | 10.0.0-preview.4 | 10.0.8 | None observed | — |

---

## Boy Scout Report

**`src/Services/Fokus/Fokus.API/DependencyInjection.cs`:** Fixed stale XML doc comment — `Migrate{TDbContext}` → `app.Migrate<FokusDbContext>()` (the referenced method is on `IHost`, not the class itself).

---

## Lessons to Log

1. **Missing skill: `central-package-management`** — no skill exists for introducing CPM. Pattern derived from `<reference-repo>/src/Directory.Packages.props`. Log gap for Pass 1 skill repair.
2. **Missing skill/rule: `framework-currency`** — no skill covers detecting and migrating deprecated FastEndpoints APIs. Pattern derived from reference project (FastEndpoints 7+ `Send.*`). Log gap for Pass 1.
3. **`Microsoft.EntityFrameworkCore.Relational` required for `Migrate<T>()`** — the base `Microsoft.EntityFrameworkCore` package does not include `GetConnectionString()` or `Database.Migrate()`. Any building-block that calls these must reference the Relational package explicitly.
4. **Refit 11 breaking change: `ApiExceptionBase.Content`** — the `Content` property moved from `ApiExceptionBase` to `ApiException` in Refit 11. Code using `response.Error?.Content` must cast: `(response.Error as ApiException)?.Content`.
5. **FastEndpoints bare `SendAsync(body, statusCode, ct)` is also a legacy call** — the initial grep pattern only matched named variants (`SendOkAsync`, `SendNotFoundAsync`, etc.) and missed the bare `SendAsync(response, 201, ct)` overload. The `framework-currency` skill/rule must include `\bSendAsync\(` (bare, not `httpClient.SendAsync`) in its migration pattern when covering FastEndpoints.

---

## Fix Cycle 1 — Reviewer-approved findings (HIGH-1, MEDIUM-2)

**Date:** 2026-06-05
**Build:** green (`Build succeeded.`)

### HIGH-1 — `FirstAsync()` → `FirstOrDefaultAsync()` + null guard

**File:** `src/Services/Fokus/Fokus.API/DependencyInjection.cs` (~line 78)

Replaced the unconditional `await fokusDb.AppSettings.FirstAsync()` inside the Xray wiring block with `FirstOrDefaultAsync()` and an explicit null guard. If the seed row was not created (e.g., a failed prior migration), the method now returns early instead of throwing `InvalidOperationException`.

```csharp
var settings = await fokusDb.AppSettings.FirstOrDefaultAsync();
if (settings is null) return;   // row was not seeded — skip Xray wiring
```

### MEDIUM-2 — Remove unused `IConfiguration configuration` parameter from `SeedFokusDataAsync`

**Files:**
- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` — removed `IConfiguration configuration` parameter; `XrayOptions` is already resolved via `IOptions<XrayOptions>` from the DI scope.
- `src/Host/Program.cs` — updated call site from `await app.Services.SeedFokusDataAsync(builder.Configuration)` to `await app.Services.SeedFokusDataAsync()`.

Note: `using Microsoft.Extensions.Configuration` was retained — it is still required by `AddFokusModule(this IServiceCollection services, IConfiguration configuration)` on the same class.
