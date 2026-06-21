# Suite Scaffolding — Implementation

## Files Created

### Repo root
- `CLAUDE.md` — adapted from Fokus, updated for modular monolith structure
- `SprintRituals.slnx` — solution file with all projects registered

### Host
- `src/Host/Host.csproj` — Web SDK, references Identity.API, Fokus.API, Blocks.AspNetCore
- `src/Host/Program.cs` — full Host composition: AddIdentityModule + AddFokusModule, both migrations, FastEndpoints multi-assembly with ActiveUserPreProcessor, Scalar in dev, SPA fallback
- `src/Host/appsettings.json` — Database + IdentityDatabase connection strings, Google/Jira/Xray config placeholders
- `src/Host/appsettings.Development.json` — dev logging config
- `src/Host/Properties/launchSettings.json` — http profile on port 5000

### Identity.Domain
- `Identity.Domain/Identity.Domain.csproj` — references Blocks.Domain, Microsoft.AspNetCore.Identity
- `Identity.Domain/GlobalUsings.cs` — Blocks.Domain.Entities, Blocks.Domain.Events, Microsoft.AspNetCore.Identity
- `Identity.Domain/Enums/PersonSource.cs` — Google, Jira, Guest enum
- `Identity.Domain/Persons/Person.cs` — aggregate root: FirstName/LastName/Email/PictureUrl/Source/ExternalId/CreatedAt/UserId/User
- `Identity.Domain/Persons/Behaviors/Person.cs` — CreateFromGoogle factory, UpdateFromGoogle
- `Identity.Domain/Users/User.cs` — IdentityUser<int> + manual IAggregateRoot, PersonId FK, RegistrationDate, LastLogin
- `Identity.Domain/Users/Behaviors/User.cs` — CreateFromPerson factory, RecordLogin
- `Identity.Domain/Roles/Role.cs` — IdentityRole<int> + Description
- `Identity.Domain/Events/PersonCreated.cs` — domain event record
- `Identity.Domain/Events/UserCreated.cs` — domain event record

### Identity.Persistence
- `Identity.Persistence/Identity.Persistence.csproj` — references Identity.Domain, Blocks.EntityFrameworkCore, Blocks.FastEndpoints; Microsoft.AspNetCore.Identity.EntityFrameworkCore; Microsoft.EntityFrameworkCore.Sqlite
- `Identity.Persistence/GlobalUsings.cs` — Identity domain, EF Core namespaces
- `Identity.Persistence/IdentityDbContext.cs` — inherits IdentityDbContext<User, Role, int>, adds Persons DbSet
- `Identity.Persistence/Configurations/PersonConfiguration.cs` — required fields, unique email index
- `Identity.Persistence/Configurations/UserConfiguration.cs` — Person-User 1:1 FK per reference pattern, DeleteBehavior.Restrict
- `Identity.Persistence/Repositories/PersonRepository.cs` — GetByEmailAsync, GetByIdAsync, GetByExternalIdAsync
- `Identity.Persistence/Repositories/UserRepository.cs` — GetByIdAsync, GetByEmailAsync, CountAsync
- `Identity.Persistence/DependencyInjection.cs` — AddPersistenceServices: DbContext, AddIdentityCore chain, PersonRepository + UserRepository
- `Identity.Persistence/DesignTimeDbContextFactory.cs` — for ef migrations
- `Identity.Persistence/Migrations/` — InitialCreate migration generated

### Identity.API
- `Identity.API/Identity.API.csproj` — class library SDK with FrameworkReference Microsoft.AspNetCore.App, FastEndpoints, Microsoft.AspNetCore.Authentication.Google
- `Identity.API/GlobalUsings.cs` — domain + persistence namespaces
- `Identity.API/Auth/AuthConfiguration.cs` — Google OAuth + cookie auth, OnTicketReceived handles Person lookup/create, User creation via UserManager, Admin bootstrap, role assignment
- `Identity.API/Auth/ActiveUserPreProcessor.cs` — validates authenticated users exist in Identity DB; returns 401 if not found
- `Identity.API/Features/Auth/Login/LoginEndpoint.cs` — minimal API GET /auth/login, Google challenge
- `Identity.API/Features/Auth/Logout/LogoutEndpoint.cs` — minimal API POST /auth/logout, cookie sign-out
- `Identity.API/Features/Auth/GetMe/GetMeEndpoint.cs` — FastEndpoints GET /api/auth/me, claims-based
- `Identity.API/DependencyInjection.cs` — AddIdentityModule (calls AddApiServices + AddPersistenceServices), MapIdentityEndpoints

### Fokus service (ported)
- `Fokus.API/Fokus.API.csproj` — converted from Web SDK to class library SDK, added FrameworkReference, removed Google Auth/Scalar/OpenAPI packages, added Microsoft.AspNetCore.OpenApi
- `Fokus.API/GlobalUsings.cs` — added Microsoft.AspNetCore.Http (for [Tags]), Microsoft.Extensions.Logging
- `Fokus.API/DependencyInjection.cs` — renamed AddFokusServices → AddFokusModule/AddApiServices/MapFokusEndpoints, removed auth registration
- `Fokus.API/Features/Auth/CreateInvitation/CreateInvitationEndpoint.cs` — removed AppUserRepository dependency; inviterId from claims
- `Fokus.Persistence/DependencyInjection.cs` — renamed AddFokusPersistence → AddPersistenceServices, removed AppUserRepository registration
- `Fokus.Persistence/FokusDbContext.cs` — removed AppUsers DbSet
- `Fokus.Persistence/Configurations/InvitationConfiguration.cs` — removed FK to AppUser; InvitedByUserId is unconstrained int (cross-service reference, no DB FK)
- `Fokus.Persistence/Migrations/` — copied from Fokus source repo; added RemoveAppUsers migration

### Infrastructure
- `Auth/` dir emptied (AuthConfiguration.cs, ActiveUserPreProcessor.cs, AuthMiddleware.cs removed)
- Auth feature folders removed: Login, Logout, GetMe, GetUsers, UpdateRole, UpdateStatus
- Domain: AppUser.cs and Behaviors/AppUser.cs removed
- Persistence: AppUserConfiguration.cs and AppUserRepository.cs removed

### Frontend
- `client/` — copied from Fokus client/
- `client/vite.config.ts` — updated outDir to `../src/Host/wwwroot`; proxy unchanged (same /api and /auth paths)

### Claude setup
- `.claude/` — copied from Fokus (skills, agents, rules)
- `docs/conventions/` — copied from Fokus
- `docs/architecture/` — copied from Fokus
- `docs/proposals/suite-integration-architecture.md` — copied

## Key Decisions

- `Identity.Domain.csproj` references `Microsoft.AspNetCore.Identity` (v2.3.1) — provides `IdentityUser<int>` base class for a non-web class library
- `Identity.API.csproj` and `Fokus.API.csproj` both use `<FrameworkReference Include="Microsoft.AspNetCore.App" />` to access ASP.NET Core HTTP types (`SameSiteMode`, `CookieSecurePolicy`, `[Tags]`) without being Web SDK projects
- `[Tags]` attribute requires `Microsoft.AspNetCore.Http` using — added to GlobalUsings in both service API projects
- `ILogger<>` requires `Microsoft.Extensions.Logging` using — added to Fokus.API GlobalUsings
- `FastEndpoints` multi-assembly scanning uses `AddFastEndpoints(o => o.Assemblies = [...])` on IServiceCollection (v6 API), not `UseFastEndpoints`
- `InvitedByUserId` on Invitation dropped FK constraint — AppUser is in Identity (separate DB), so no DB-level FK is possible; field is kept as unconstrained int reference
- `[Tags]` attribute on GetMeEndpoint removed (class library context) — TagsAttribute now resolved via GlobalUsings in both projects

## Deviations from Plan

- **Step 4:** Created `src/Services/Identity/CLAUDE.md` manually (create-service skill requires it, but no architect-generated file existed for ad-hoc work). Content follows the plan's stated domain model.
- **Step 5:** `[Tags("Auth")]` removed from `GetMeEndpoint` in initial pass since it required framework reference not yet wired — later fixed by adding FrameworkReference and Microsoft.AspNetCore.Http to GlobalUsings.
- **Step 8:** The `CreateInvitationEndpoint` previously checked `AppUserRepository.GetByEmailAsync` to verify no existing user — this check was dropped (no cross-service call to Identity feasible without gRPC/integration events, which are out of scope). Per plan: "replace AppUserRepository usage with claims-based user info."
- **Step 8:** `InvitationConfiguration` FK to AppUser dropped (cross-service DB boundary). Added comment explaining the intentional omission.
- **Step 9:** `AddFastEndpoints()` moved to Host Program.cs (not inside service DI) since Host owns the multi-assembly scanning. Individual services no longer call `AddFastEndpoints()` themselves.

---

## Review Cycle 1 Fixes

### Files Modified

- `Identity.Domain/Identity.Domain.csproj` — removed `Microsoft.AspNetCore.Identity` v2.3.1 package (carried high-severity `System.Security.Cryptography.Xml` vulnerability); User and Role moved to Persistence to resolve this
- `Identity.Domain/GlobalUsings.cs` — removed `global using Microsoft.AspNetCore.Identity;` (no longer needed)
- `Identity.Domain/Events/UserCreated.cs` — changed payload from `User` object to `int UserId` to avoid Domain→Persistence circular reference after User moved out of Domain
- `Identity.Domain/Persons/Person.cs` — removed `User? User` navigation (User moved to Persistence); removed `required` keyword on mutable string properties (incompatible with `private set`); initialized to `string.Empty`; `UserId` now has `private set` modified only via `LinkUser()`
- `Identity.Domain/Persons/Behaviors/Person.cs` — added `LinkUser(int userId)` domain method; `CreateFromGoogle` now normalizes email with `.ToLowerInvariant()`
- `Identity.Persistence/Identity.Persistence.csproj` — added `Microsoft.AspNetCore.Identity.EntityFrameworkCore` (moved from Domain); User and Role now defined here
- `Identity.Persistence/GlobalUsings.cs` — added `Microsoft.AspNetCore.Identity` using; added `Identity.Persistence.Entities.Roles` and `Identity.Persistence.Entities.Users`
- `Identity.Persistence/Configurations/UserConfiguration.cs` — changed `.WithOne(p => p.User)` → `.WithOne()` since Person no longer has User navigation property
- `Identity.Persistence/Repositories/PersonRepository.cs` — removed `Include(p => p.User)` (navigation removed); email lookup uses `.ToLowerInvariant()` to match stored lowercase value
- `Identity.Persistence/Entities/Users/User.cs` — new file: `IdentityUser<int>` + manual `IAggregateRoot` implementation (cannot inherit both `IdentityUser<int>` and `AggregateRoot<T>`)
- `Identity.Persistence/Entities/Users/UserBehaviors.cs` — new file: `CreateFromPerson` factory, `RaiseCreatedEvent`, `RecordLogin`, `Deactivate`, `Reactivate` partial class behaviors
- `Identity.Persistence/Entities/Roles/Role.cs` — new file: `IdentityRole<int>` + optional `Description` property
- `Identity.API/Identity.API.csproj` — added `<ProjectReference>` to `Fokus.Persistence` (deliberate cross-service coupling at auth boundary, architect-approved)
- `Identity.API/GlobalUsings.cs` — added `Fokus.Persistence.Repositories`; updated to use `Identity.Persistence.Entities.Roles/Users` namespaces
- `Identity.API/Auth/AuthConfiguration.cs` — full rewrite: added invitation path (reads `invite_token` from auth properties, validates invitation status/expiry/email, creates user with invitation's role, calls `invitation.Accept()`); added deactivated-user redirect (`/login?error=deactivated`); extracted `SignInWithClaimsAsync` helper; uses `person.LinkUser()` domain method; calls `RaiseCreatedEvent()` after UserManager assigns Id
- `Identity.API/Auth/ActiveUserPreProcessor.cs` — added `IsActive` check returning 403 with "Your account has been deactivated." message; extracted `WriteErrorAsync` helper
- `Identity.API/Features/Auth/GetMe/GetMeEndpoint.cs` — restored `[Tags("Auth")]` attribute
- `Jira.RestApi/RestApiJiraClient.cs` — removed 2 debug `Console.WriteLine` statements

### Key Decisions (Cycle 1)

- **User/Role in Persistence, not Domain:** Moving these out of Domain eliminates the high-severity NuGet vulnerability. `IAggregateRoot` is implemented manually on `User` since it cannot inherit both `IdentityUser<int>` and `AggregateRoot<T>`. Architect decision.
- **Cross-service coupling (Identity.API → Fokus.Persistence):** Identity validates and accepts Fokus invitations during the OAuth ticket exchange. This is the correct boundary for this coupling — auth is the one place that must straddle both services. Documented with comment in `.csproj` and in `AuthConfiguration.cs`. Architect decision.
- **`UserCreated` carries `int UserId`:** After User moved to Persistence, the Domain event could no longer reference the `User` type without creating a Domain→Persistence dependency. Carrying only the primitive ID preserves clean layering.

---

## Review Cycle 2 Fixes

### Files Created

- `Identity.Persistence/Migrations/[timestamp]_AddUserIsActive.cs` — EF migration adding `IsActive` column to `AspNetUsers` table (column was added to `User` entity after `InitialCreate` was generated)

### Files Modified

- `Identity.API/Auth/ActiveUserPreProcessor.cs` — added missing `return` after `WriteErrorAsync` call in the `!user.IsActive` branch (line 33); without it execution continued past the 403 response

---

## Review Cycle 3 Fixes

### Files Modified

- `Identity.Persistence/Migrations/20260517203914_AddUserIsActive.cs` — changed `defaultValue: false` → `defaultValue: true`; using `false` would lock out all existing users when the migration runs against a populated database
- `Identity.Persistence/Configurations/UserConfiguration.cs` — added `builder.Property(u => u.IsActive).HasDefaultValue(true)` so future migrations generate the correct default automatically, matching the C# property initializer
