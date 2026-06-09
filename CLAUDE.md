# CLAUDE.md

## Architecture

DDD, Vertical Slice, CQRS, Clean Architecture, Event Driven Design

## Repo Structure

```
src/
  Host/                        # Single Web SDK deployable — composes all modules
  BuildingBlocks/              # Shared libraries
    (app-agnostic)             #   Reusable primitives (no project knowledge)
    Blocks.Contracts/          #   App-specific cross-module contracts: gRPC interfaces + integration event DTOs
  Services/                    # Modules — one folder per domain
    Identity/                  #   Person + User, Google OAuth, cookie auth
      Identity.API/            #   FastEndpoints feature slices, auth config, DI
      Identity.Domain/         #   Person aggregate, User entity, domain events
      Identity.Persistence/    #   IdentityDbContext (plain DbContext), repos, migrations
    Fokus/                     #   Sprint analytics, Jira sync, health metrics
      Fokus.API/               #   FastEndpoints feature slices, DI (class library)
      Fokus.Domain/            #   Aggregates, value objects, domain events
      Fokus.Persistence/       #   FokusDbContext, configs, migrations, repositories
  Modules/                     # External-system integration modules
    Jira/                      #   Jira REST API client + contracts
    Xray/                      #   Xray GraphQL client + contracts
client/                        # Vue 3 SPA — built into src/Host/wwwroot/
docs/
  conventions/                 # Stack-specific coding standards, project rules
  architecture/                # Architecture decisions, v1 spec
  proposals/                   # Proposals including suite-integration-architecture.md
  kb/                          # Knowledge base (business rules, formulas, edge cases)
SprintRituals.slnx             # Solution file
```

**Host** is the only Web SDK project. All other API projects are class libraries with `<FrameworkReference Include="Microsoft.AspNetCore.App" />`.

**No cross-module project references.** Fokus.API does NOT reference Identity.*. Cross-module data (e.g. Person lookup) arrives via gRPC in the next increment.

## Tech Stack

- .NET 10 / ASP.NET Core — solution uses `.slnx` format (XML-based, .NET 10 default)
- **FastEndpoints** — endpoint classes per feature. Host registers endpoints from ALL module assemblies via `services.AddFastEndpoints(o => o.Assemblies = [...])`.
- **FluentValidation** — request validators, auto-discovered and executed by FastEndpoints before the handler runs.
- **FastEndpoints `IEvent` bus** — in-process publish/subscribe for domain events. Aggregates raise events onto a `DomainEvents` list; a `SaveChangesInterceptor` in Persistence publishes them after a successful save. `IEventHandler<T>` implementations live alongside their feature slices.
- Mapster — object mapping
- EF Core — SQLite (file-backed, two databases: `identity.db` + `fokus.db`)
- **Identity:** plain DDD — `Person : AggregateRoot<int>` + `User : Entity<int>`. **No ASP.NET Core Identity / IdentityUser / IdentityDbContext / UserManager.** Google-only OAuth, cookie auth, Role = string claim.
- MassTransit — in-memory transport wired in Host (`AddMassTransit` + `UsingInMemory`); `IPublishEndpoint` injected into sync services; `SprintSynced` integration event published after each sprint sync. RabbitMQ transport is a future increment.
- gRPC code-first (protobuf-net.Grpc) — `ISprintAnalyticsService` + `ISprintCatalogService` contracts in `Blocks.Contracts`; server impls in `Fokus.API/Grpc/`; h2c endpoint on port 5001 (Kestrel `Endpoints:Grpc`); `AddCodeFirstGrpc` wired in Host. gRPC client registration comes with the Reflekt port.
- SignalR — real-time server → browser push
- Google OAuth via ASP.NET Core `AddGoogle` — authentication (cookie auth; email + profile scope only)
- OpenAPI via `Microsoft.AspNetCore.OpenApi` (built into .NET 10) + **Scalar** (`Scalar.AspNetCore`) for the interactive UI. Do not use Swashbuckle. Scalar UI gated to `IsDevelopment`.

## Frontend

- **Vue 3** + **TypeScript** — SPA framework.
- **Pinia** — state management.
- **Tailwind CSS** — styling. No full UI kit.
- Built with **Vite** into `src/Host/wwwroot/` and served as static files.
- Optimistic UI for user actions; reconcile or revert on API response.

## Auth

Auth lives entirely in **Identity**. The Host wires `AddIdentityAuth` (Google + cookie). Fokus has no auth registration — it relies on the cookie set by Identity. The global `ActiveUserPreProcessor` (Identity) validates the cookie on every authenticated request.

**gRPC contract topology (two opposite-direction contracts, both live):**
- `IInvitationService` (Fokus = server, Identity = client) — synchronous invite-token redemption at login. Identity calls Fokus from `AuthConfiguration.OnTicketReceived`.
- `IPersonService` (Identity = server, Fokus = client) — cross-module Person upsert. Fokus calls Identity from `SprintIssueSyncService` for each discovered Jira developer (best-effort).

**Company-domain auto-seed:** `AnnounceCompanyDomainOnUserCreatedHandler` (Identity) publishes `FirstAdministratorRegistered` on the in-process MassTransit bus (gated on first Admin, skips public email providers). `SeedCompanyDomainOnFirstAdministratorRegisteredConsumer` (Fokus) seeds `AppSettings.CompanyDomain` if null (idempotent).

## Databases

| DB | Connection string key | Migration startup order |
|----|----------------------|------------------------|
| `identity.db` | `IdentityDatabase` | 1st |
| `fokus.db` | `Database` | 2nd |

Both databases are created under `data/` relative to the Host working directory.

## Port Convention

All services publish host ports using an `{AA}XY` scheme. See `docs/conventions/project-rules.md` for full table. HTTP is mandatory; HTTPS and gRPC are opt-in.

## Guardrails

See `docs/conventions/project-rules.md` for all architectural guardrails.

**Plain DDD identity model is a locked decision** — never add ASP.NET Core Identity packages (`Microsoft.AspNetCore.Identity.*`) to this solution. If role management grows complex, extend the string-claim model in Identity; don't switch stacks.
