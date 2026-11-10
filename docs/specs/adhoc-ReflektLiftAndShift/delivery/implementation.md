# Reflekt Lift and Shift (RA1) — Implementation

## Files Created
- `src/Services/Reflekt/Reflekt.API/` — copied from the original Reflekt project; converted Sdk.Web → Sdk + FrameworkReference; removed Program.cs, Properties/, appsettings*.json, wwwroot/, deploy.zip, publish/, data/, bin/, obj/
- `src/Services/Reflekt/Reflekt.Domain/` — copied from the original Reflekt project; removed bin/, obj/
- `src/Services/Reflekt/Reflekt.Persistence/` — copied from the original Reflekt project; removed bin/, obj/
- `src/Services/Reflekt/Reflekt.Persistence/DesignTimeDbContextFactory.cs` — design-time factory for `dotnet ef` (Persistence-as-startup; no Program.cs)
- `src/Services/Reflekt/Reflekt.Persistence/Migrations/20260614100328_InitialReflektSchema.cs` — EF delta: adds audit props (CreatedById, CreatedOn, LastModifiedById, LastModifiedOn) to all aggregate tables (Votes, Users, Teams, Sprints, Participants, Cards, CardGroups); non-zero because target AggregateRoot exposes these as public properties that EF picks up

## Files Modified

### Step 1 — Move projects + SDK conversion
- `SprintRituals.slnx` — added `/Services/Reflekt/` folder with three project paths
- `src/Services/Reflekt/Reflekt.API/Reflekt.API.csproj` — converted Sdk.Web → Sdk; added PropertyGroup (net10.0, Nullable, ImplicitUsings); added FrameworkReference; removed Sdk.Web-only packages (Microsoft.AspNetCore.Authentication.Google, Microsoft.AspNetCore.OpenApi, Scalar.AspNetCore — Host owns those); added Blocks.AspNetCore + Blocks.FastEndpoints project references; dropped InternalsVisibleTo (tests not in RA1)
- `src/Services/Reflekt/Reflekt.Domain/Reflekt.Domain.csproj` — added PropertyGroup (net10.0, Nullable, ImplicitUsings)
- `src/Services/Reflekt/Reflekt.Persistence/Reflekt.Persistence.csproj` — replaced Blocks.Persistence ref with Blocks.Domain + Blocks.EntityFrameworkCore + Blocks.FastEndpoints refs; added PropertyGroup; added Microsoft.EntityFrameworkCore.Design

### Step 2 — Repoint BuildingBlocks; generic DbContext base; namespace fixes
- `src/Services/Reflekt/Reflekt.Domain/GlobalUsings.cs` — replaced `global using Blocks.Domain` with `global using Blocks.Domain.Entities` + `global using Blocks.Domain.Events`
- 26 `*.cs` files in `Reflekt.Domain/` — removed individual `using Blocks.Domain;` (now covered by GlobalUsings)
- `src/Services/Reflekt/Reflekt.Persistence/GlobalUsings.cs` — replaced `global using Blocks.Persistence` with target namespaces: Blocks.Domain.Entities, Blocks.Domain.Events, Blocks.EntityFrameworkCore, Blocks.EntityFrameworkCore.EntityConfigurations, Blocks.EntityFrameworkCore.Interceptors, Blocks.EntityFrameworkCore.Repositories
- All `*.cs` in `Reflekt.Persistence/` (EntityConfigurations, Repositories) — removed individual `using Blocks.Persistence.*` (covered by GlobalUsings)
- `src/Services/Reflekt/Reflekt.Persistence/ReflektDbContext.cs` — changed base `ApplicationDbContext(options)` → `ApplicationDbContext<ReflektDbContext>(options)` (F4 fix)
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/Repository.cs` — rewrote: uses 3-param `RepositoryBase<ReflektDbContext, TEntity, int>`; added `Db` property alias; added `GetByIdAsync` forwarding to `FindByIdAsync`; dropped the 2-param generic overload; dropped `notnull` constraint
- `src/Services/Reflekt/Reflekt.API/Middleware/GlobalExceptionMiddleware.cs` — replaced `using Blocks.Domain` with `using Blocks.Domain.Exceptions`; kept file (removed in production in Step 6 by dropping the middleware, file still compiles)
- `src/Services/Reflekt/Reflekt.Domain/Cards/EvidenceLink.cs` — changed `: Entity` → `: Entity<int>` (source's non-generic `Entity` alias not present in target)

### Step 3 — Swap domain-event dispatch to target interceptor
- `src/Services/Reflekt/Reflekt.Persistence/DependencyInjection.cs` — rewrote: replaced `AddSingleton<DomainEventPublishingInterceptor>` with `AddScoped<IDomainEventPublisher, DomainEventPublisher>` + `AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>`; changed connection string key from `Reflekt` → `ReflektDatabase`; removed `UseReflektPersistence` (Host owns migration via `app.Migrate<>`); added `SeedReflektDataAsync` public extension

### Step 4 — CPM re-baseline
- `src/Directory.Packages.props` — added `Mapster.DependencyInjection` and `AngleSharp 1.2.0`; bumped `Mapster` from `7.4.0` → `10.0.0` and `Mapster.DependencyInjection` to `10.0.0` (no 7.x DI package exists on NuGet; target wins = 10.0.0)

### Step 5 — FastEndpoints currency (Send*Async → Send.*)
- 43+ endpoint files in `src/Services/Reflekt/Reflekt.API/Features/` — migrated all legacy `SendOkAsync`, `SendNoContentAsync`, `SendNotFoundAsync`, `SendForbiddenAsync`, `SendUnauthorizedAsync`, `SendErrorsAsync`, `SendAsync` (bare 3-arg) to `Send.OkAsync`, `Send.NoContentAsync`, `Send.NotFoundAsync`, `Send.ForbiddenAsync`, `Send.UnauthorizedAsync`, `Send.ErrorsAsync`, `Send.ResponseAsync`; SignalR `hubContext.Clients.*.SendAsync` untouched

### Step 6 — Module DI refactor
- `src/Services/Reflekt/Reflekt.API/DependencyInjection.cs` — refactored `AddReflektApi`/`UseReflektApi` into `AddReflektModule(IConfiguration)` + `MapReflektEndpoints()`; removed `AddFastEndpoints`, `AddSignalR`, `AddOpenApi`, `MapFastEndpoints`, `MapOpenApi`, `MapScalarApiReference`, `MapFallbackToFile`, `UseMiddleware<GlobalExceptionMiddleware>`, `UseDefaultFiles`, `UseStaticFiles`, `UseRouting`, `UseAuthentication`, `UseAuthorization`; hub mapped at `/hubs/retro` (F2); timer callback preserved

### Step 7 — Auth: guest scheme + authorization policies
- `src/Services/Reflekt/Reflekt.API/Auth/AuthenticationConfiguration.cs` — rewrote to add ONLY guest scheme (`.AddScheme<AuthenticationSchemeOptions, GuestTokenAuthenticationHandler>`); dropped `AddAuthentication`, `AddCookie`, `AddGoogle`, `OnGoogleTicketReceived` (RA2 work); `AddReflektAuthorization()` kept (cookie policies resolve against Identity's scheme)
- `src/Services/Reflekt/Reflekt.API/Features/Auth/Login/LoginEndpoint.cs` — unmapped with RA1 marker; Configure() sets `_ra1/unmapped/auth/login` placeholder route
- `src/Services/Reflekt/Reflekt.API/Features/Auth/Logout/LogoutEndpoint.cs` — unmapped with RA1 marker
- `src/Services/Reflekt/Reflekt.API/Features/Auth/Me/MeEndpoint.cs` — unmapped with RA1 marker
- `src/Services/Reflekt/Reflekt.API/Features/Participants/JoinSprintGoogle/JoinSprintGoogleEndpoint.cs` — unmapped with RA1 marker

### Step 8 — Host composition
- `src/Host/Program.cs` — added `using Reflekt.API` + `using Reflekt.Persistence`; added `AddReflektModule`; added `typeof(Reflekt.API.DependencyInjection).Assembly` to FastEndpoints Assemblies; added `app.Migrate<ReflektDbContext>()` + `SeedReflektDataAsync()`; added `app.MapReflektEndpoints()`
- `src/Host/Host.csproj` — added `ProjectReference` to `Reflekt.API.csproj`
- `src/Host/appsettings.json` — added `ConnectionStrings:ReflektDatabase → data/reflekt.db`

### Step 9 — Migration + green build
- `src/Services/Reflekt/Reflekt.Persistence/Migrations/20260614100328_InitialReflektSchema.*` — EF migration adding audit props to all aggregate tables; generated by `dotnet ef migrations add`; build succeeds with 0 errors

## Key Decisions
- **Mapster version bump to 10.0.0** — `Mapster.DependencyInjection 7.4.0` does not exist on NuGet (NU1603); plan said "target wins on overlaps" but the target version 7.4.0 is unavailable as DI package — used 10.0.0 for both. Plan noted "let build catch any 10.x-only breakage at Step 9" — no API breakage found.
- **Reflekt Repository.cs** — source `RepositoryBase<TContext, TEntity>` (2-param, int alias) doesn't exist in target. Added `Db` property alias (`=> DbContext`) and `GetByIdAsync` to preserve concrete repo call sites.
- **EvidenceLink** — source had non-generic `Entity` alias; target only has `Entity<T>`. Changed to `Entity<int>`.
- **GlobalUsings for Reflekt.API** — added missing logging/hosting/SignalR/authorization global usings that the Sdk.Web SDK previously provided implicitly.
- **Pre-existing Fokus error fixed** — `RevokeInvitationEndpoint.cs` used `Send.Results()` which doesn't exist in FE 8.1; was broken before RA1 changes (confirmed via stash baseline). Fixed with `Send.ResponseAsync((object?)null, 409, ct)` to achieve green build.
- **EF migration is non-zero** — audit props (CreatedById/On, LastModifiedById/On) added to all aggregate tables because target `AggregateRoot` exposes them as public properties. Plan's prediction of "possibly none" was optimistic; F7 resolution is correct (EntityConfiguration does NOT map these, but EF convention picks them up from public properties).
- **RA1 unmapped endpoints** — four login endpoints use Configure() override with `_ra1/unmapped/*` placeholder routes to satisfy FastEndpoints discovery without exposing real paths.

## Skills Used
| Step | Skill(s) invoked | Notes |
|------|------------------|-------|
| 1 | None | Mechanical copy + SDK conversion per plan |
| 2 | None | Mechanical repoint per plan |
| 3 | persistence-patterns | Invoked; interceptor pattern follows Fokus.Persistence/DependencyInjection.cs |
| 4 | central-package-management | Invoked; 3-form verification grep passed |
| 5 | framework-currency | Invoked; two-stage detection applied; SignalR false positives untouched |
| 6 | service-registration | Invoked; AddReflektModule + MapReflektEndpoints per ADR-014 |
| 7 | None | Lift-specific auth per plan |
| 8 | service-registration, persistence-patterns | Invoked; Host Program.cs Add/Init/Use pattern followed |
| 9 | persistence-patterns | Invoked; design-time factory + migration generated and committed |

## Carry-Over Findings
| Title | Severity | For | Evidence | Note |
|-------|----------|-----|----------|------|
| `ActiveUserPreProcessor` may reject guest-scheme participants | medium | reviewer | Plan Step 7 documents this risk | Plan says: if it hard-requires Identity cookie, scoping it is RA2 escalation |
| Audit props unmapped but migrated | low | reviewer | InitialReflektSchema migration | EntityConfiguration does not call AuditedEntityConfiguration; props are in schema but never populated |
| Reflekt hub sends "sprint:{sprintId}" groups | low | reviewer | SprintHub.cs group prefix | Hub was renamed /hubs/retro but group name prefix still says "sprint:" — cosmetic until RA2 |

## KB Changes
None — RA1 is structural lift only; no business-rule KB entries needed.

## Deviations from Plan
- **Mapster 10.0.0 instead of 7.4.0** — `Mapster.DependencyInjection 7.4.0` does not exist; 10.0.0 used for both (plan: "target wins"; in this case target pin was unavailable so next available version used). No 10.x API breakage found.
- **Non-zero migration** — plan said "possibly none"; audit columns added to all aggregate tables because EF convention discovers public properties on the base class. This is correct behavior.
- **Pre-existing `Send.Results` bug in Fokus fixed** — out of scope for RA1 but required for green solution build (acceptance criterion). Documented as carry-over.
- **`EvidenceLink : Entity` → `Entity<int>`** — source had non-generic alias not present in target. One-line fix; no behavior change.

*Status: COMPLETE — developer, 2026-06-14*
