# Suite Integration: Scaffolding + Identity Service

**Feature Spec:** None (ad-hoc infrastructure, driven by `docs/proposals/suite-integration-architecture.md`)

## Context

Fokus and Reflekt are standalone public repos. The decided proposal merges them into a private modular monolith suite. This plan covers the first milestone: create the fresh `sprint-rituals` repo, scaffold the structure, build the Identity service, and port Fokus into its service slot.

After this milestone: Host composes Identity + Fokus services, Identity owns Person/User/Google OAuth, Fokus runs with all its current features, frontend works.

## Scope

**In scope:**
- Fresh repo at `this repo\`
- Host project (single deployable, Web SDK)
- Identity service (Person, User via IdentityUser<int>, Google OAuth)
- Fokus service port (all features, Invitations stay in Fokus)
- BuildingBlocks (copied from Fokus, the more mature set)
- Modules (Jira, Xray — copied from Fokus)
- Frontend (copied from Fokus client/)
- Claude Code setup (.claude/ — skills, agents, rules, conventions)

**Out of scope:**
- Reflekt port (future milestone)
- gRPC contracts / PersonGrpcService (future — when cross-service Person upsert is needed)
- Integration events / MassTransit
- Frontend routing split (`/retro/*` vs `/dashboard/*`)

## Reference Project

`<reference-repo>\src\Services\Auth\` — follow these patterns for Identity:

| Pattern | Reference file | What to adopt |
|---------|---------------|---------------|
| Person aggregate | `Auth.Domain/Persons/Person.cs` | Partial class split, aggregate root, FirstName/LastName/Email/PictureUrl |
| User entity | `Auth.Domain/Users/User.cs` | Inherits `IdentityUser<int>`, implements `IAggregateRoot` manually, PersonId FK |
| Person-User 1:1 | `Auth.Persistence/.../UserEntityConfiguration.cs` | FK on User (`PersonId`), `DeleteBehavior.Restrict` |
| IdentityDbContext | `Auth.Persistence/AuthDbContext.cs` | Inherits `IdentityDbContext<User, Role, int>`, adds `Persons` DbSet |
| Repositories | `Auth.Persistence/Repositories/` | Concrete, no interfaces, `Query()` with Includes |
| DI composition | `Auth.API/DependencyInjection.cs` | Layer registration pattern |
| Person creation flow | `Auth.API/Features/.../CreateUserEndpoint.cs` | Look up Person by email → create if missing → create User |

**What NOT to adopt:**
- JWT / RefreshTokens (we use cookie + Google OAuth)
- Complex value objects (EmailAddress, HonorificTitle, ProfessionalProfile)
- `IPersonCreationInfo` abstraction (no gRPC consumers yet)
- MediatR (we use FastEndpoints direct handlers)

## Key Decisions

| Decision | Choice | Why |
|----------|--------|-----|
| Repo | Fresh `this repo\`, private GitHub | Keep Fokus/Reflekt public and independent |
| Service naming | Identity | Owns Persons (universal identity), not just auth |
| Folder structure | `src/Services/Identity/`, `src/Services/Fokus/` | Standard service convention |
| User base class | `IdentityUser<int>` | Proper role management via ASP.NET Core Identity |
| IAggregateRoot on User | Manual implementation | Can't inherit both IdentityUser and AggregateRoot — per reference |
| DbContext base (Identity) | `IdentityDbContext<User, Role, int>` | Gets Identity tables for free |
| SQLite split | `identity.db` + `fokus.db` | Physical service boundary |
| Invitations | Stay in Fokus | Fokus-specific onboarding, not an identity concern |
| CompanyDomain | Stays in Fokus AppSettings | Used by Invitation validation which stays in Fokus |
| Host composition | Each service exposes `Add{X}Module` + `Map{X}Endpoints` | `service-registration` skill pattern |
| Claude Code setup | Copied from Fokus repo | Most mature (.claude/, skills, agents, rules, docs/conventions) |
| Solution name | `SprintRituals.slnx` | Suite identity |

## Domain Model: Identity Service

**Person** (aggregate root) — the universal anchor. Anyone who appears in the system from any source.

```
Person : AggregateRoot<int>
  FirstName    : string (required)
  LastName     : string (required)
  FullName     : string (computed)
  Email        : string (required, unique index on normalized form)
  PictureUrl   : string?
  Source       : PersonSource enum (Google, Jira, Guest)
  ExternalId   : string? (GoogleId, JiraAccountId, GuestToken)
  CreatedAt    : DateTime

  UserId       : int? (nullable back-reference)
  User         : User? (navigation)
```

**User** — a Person who authenticated. Inherits `IdentityUser<int>`, implements `IAggregateRoot` manually.

```
User : IdentityUser<int>, IAggregateRoot
  PersonId         : int (required FK → Person, one-to-one, unique)
  Person           : Person (navigation)
  RegistrationDate : DateTime
  LastLogin        : DateTime?

  // From IdentityUser: Email, NormalizedEmail, UserName, etc.
  // Manual IAggregateRoot: DomainEvents list, AddDomainEvent(), ClearDomainEvents()
```

**Role** — extends `IdentityRole<int>`.

```
Role : IdentityRole<int>
  Description : string
```

**Person-User relationship:**
- Person created first, User second. Person can exist without a User.
- FK on User side (`PersonId` required, unique). Person has nullable `UserId?` + `User?`.
- EF: `HasOne(u => u.Person).WithOne(p => p.User).HasForeignKey<User>(u => u.PersonId).IsRequired().OnDelete(DeleteBehavior.Restrict)`

## Target Structure

```
this repo\
  .claude/                          # Copied from Fokus — skills, agents, rules
  docs/
    architecture/
    conventions/
    kb/
    proposals/                      # suite-integration-architecture.md
    specs/
  src/
    Host/                           # Web SDK — single deployable
      Host.csproj
      Program.cs
      appsettings.json
      appsettings.Development.json
      Properties/launchSettings.json
      wwwroot/
    BuildingBlocks/                  # Copied from Fokus (6 projects)
      Blocks.Core/
      Blocks.Domain/
      Blocks.Exceptions/
      Blocks.EntityFrameworkCore/
      Blocks.AspNetCore/
      Blocks.FastEndpoints/
    Services/
      Identity/                     # New — built from scratch
        Identity.API/
        Identity.Domain/
        Identity.Persistence/
      Fokus/                        # Ported from <fokus-repo>\src\Services\Fokus\
        Fokus.API/                  # Class library, AppUser removed, Invitations kept
        Fokus.Domain/               # AppUser removed, Invitation stays
        Fokus.Persistence/          # AppUsers table removed, Invitations stays
    Modules/                        # Copied from Fokus
      Jira/
      Xray/
  client/                           # Copied from Fokus client/
  SprintRituals.slnx
  CLAUDE.md
  README.md
```

## Project Reference Graph

```
Host ──▶ Identity.API ──▶ Identity.Persistence ──▶ Identity.Domain ──▶ Blocks.Domain
  │           │
  │           ├──▶ Blocks.FastEndpoints
  │           └──▶ Blocks.AspNetCore
  │
  ├──▶ Fokus.API ──▶ Fokus.Persistence ──▶ Fokus.Domain ──▶ Blocks.Domain
  │        │
  │        ├──▶ Jira.RestApi + Jira.Contracts
  │        └──▶ Xray.GraphQL + Xray.Contracts
  │
  └──▶ Blocks.AspNetCore

Fokus.API does NOT reference Identity.API (no cross-service dependency yet).
```

## Skill Mapping

| Step | Skill | Disposition |
|------|-------|-------------|
| 1 — Fresh repo + structure | None | Manual: git init, folder creation, copy .claude/ setup |
| 2 — BuildingBlocks + Modules | None | Copy from Fokus, verify build |
| 3 — Host project | `service-registration` | Use: Host composition section, Program.cs structure |
| 4 — Identity service skeleton | `create-service` | Use: 3-project split (API/Domain/Persistence) |
| 5 — Identity domain | `create-aggregate`, `domain-patterns` | Use: Person aggregate. Manual IAggregateRoot on User per reference. |
| 6 — Identity persistence | `persistence-patterns` | Use: IdentityDbContext, configs, repositories |
| 7 — Identity auth + endpoints | `create-feature`, `authorization-patterns` | Use: Login/Logout/GetMe. Google OAuth config. |
| 8 — Port Fokus service | `service-registration` | Use: layer DI pattern. Remove AppUser, keep Invitations. |
| 9 — Port frontend + wire Host | `service-registration` | Use: Host composition, multi-assembly FastEndpoints |

## Implementation Steps

### Step 1: Fresh repo + structure

Create `this repo\`. Initialize git. Create the folder skeleton (`src/`, `src/Host/`, `src/Services/`, `src/Services/Identity/`, `src/Services/Fokus/`, `src/BuildingBlocks/`, `src/Modules/`, `client/`, `docs/`). Copy `.claude/` from Fokus (agents, skills, rules, conventions). Copy `docs/conventions/`, `docs/architecture/`, `docs/proposals/suite-integration-architecture.md`. Create `CLAUDE.md` (adapt from Fokus — update repo structure section for modular monolith). Create `SprintRituals.slnx` (empty, projects added in subsequent steps).

**Source:** `<fokus-repo>\.claude\`, `<fokus-repo>\docs\conventions\`, `<fokus-repo>\docs\architecture\`
**Accept:** Git repo initialized. Folder skeleton exists. `.claude/` setup in place.

### Step 2: Copy BuildingBlocks + Modules

Copy all 6 BuildingBlocks projects from `<fokus-repo>\src\BuildingBlocks\`. Copy Jira and Xray modules from `<fokus-repo>\src\Modules\`. Add all to `SprintRituals.slnx`. Verify `dotnet build` passes for BuildingBlocks and Modules in isolation.

**Source:** `<fokus-repo>\src\BuildingBlocks\`, `<fokus-repo>\src\Modules\`
**Accept:** `dotnet build` passes for all copied projects.

### Step 3: Create Host project

Create `src/Host/Host.csproj` (Web SDK). Create `Program.cs` following the `service-registration` skill's Host composition pattern — initially just the skeleton (middleware pipeline, static files, SPA fallback). Create `appsettings.json` with both connection strings (`Database` for fokus.db, `IdentityDatabase` for identity.db). Create `appsettings.Development.json` with Google OAuth config placeholder. Create `Properties/launchSettings.json`.

**Files to create:** `Host.csproj`, `Program.cs`, `appsettings.json`, `appsettings.Development.json`, `launchSettings.json`
**Accept:** `dotnet build src/Host/Host.csproj` passes (with empty service registrations).

### Step 4: Create Identity service skeleton

Scaffold `src/Services/Identity/` with 3 projects: `Identity.API`, `Identity.Domain`, `Identity.Persistence`. Correct project references (API → Persistence → Domain, Domain → Blocks.Domain, etc.). `GlobalUsings.cs` per project. `DependencyInjection.cs` stubs in API and Persistence. Add to solution. Reference `Identity.API` from Host.

**Skill:** `create-service`
**Accept:** Solution builds. Host calls `AddIdentityModule` (no-op stub).

### Step 5: Create Identity domain entities

Create Person aggregate with partial class split (data + behaviors). Create User inheriting `IdentityUser<int>` with manual `IAggregateRoot` (DomainEvents list, audit properties, PersonId FK). Create Role extending `IdentityRole<int>`. Create `PersonSource` enum. Create `UserCreated` domain event.

Follow reference: `<reference-repo>\src\Services\Auth\Auth.Domain\`

**Skill:** `create-aggregate`, `domain-patterns`
**Files:** All under `src/Services/Identity/Identity.Domain/`
**Accept:** Identity.Domain builds.

### Step 6: Build Identity persistence layer

Create `IdentityDbContext` inheriting `IdentityDbContext<User, Role, int>` with `DbSet<Person> Persons`. Create `PersonConfiguration` (email unique index, required fields). Create `UserConfiguration` (one-to-one FK per reference pattern). Create `PersonRepository` and `UserRepository` (concrete, with Includes in Query()). Wire `AddPersistenceServices` in DI — register DbContext (SQLite), Identity services (`AddIdentityCore<User>`, `AddRoles<Role>`, `AddEntityFrameworkStores`, `AddSignInManager`), repositories. Generate `InitialCreate` migration.

Follow reference: `<reference-repo>\src\Services\Auth\Auth.Persistence\`

**Skill:** `persistence-patterns`
**Files:** All under `src/Services/Identity/Identity.Persistence/`
**Accept:** Migration generates. DbContext can be instantiated.

### Step 7: Build Identity auth + endpoints

Create `Auth/AuthConfiguration.cs` — Google OAuth + cookie scheme. `OnTicketReceived` handles: bootstrap (create Person + User, assign Admin role), regular login (look up User via UserManager, validate active, update LastLogin). Create `Auth/ActiveUserPreProcessor.cs` — validates user is active via UserRepository.

Create minimal endpoints: Login (minimal API route for Google challenge), Logout, GetMe.

Wire `AddApiServices` — calls `AddIdentityAuth` (Google config, cookie config). Wire `AddIdentityModule` to call both layers. Wire `MapIdentityEndpoints` to map the login route.

Follow reference: `<fokus-repo>\src\Services\Fokus\Fokus.API\Auth\AuthConfiguration.cs` (adapt for Person+User model)

**Skill:** `create-feature`, `authorization-patterns`
**Files:** Under `src/Services/Identity/Identity.API/`
**Accept:** Identity builds end-to-end. Google OAuth config compiles.

### Step 8: Port Fokus service

Copy `<fokus-repo>\src\Services\Fokus\` into `src/Services/Fokus/`. Convert `Fokus.API.csproj` from Web SDK to class library SDK. Remove `Program.cs` (Host owns this). Remove `Auth/AuthConfiguration.cs`, `Auth/AuthMiddleware.cs`, `Auth/ActiveUserPreProcessor.cs`. Remove `Features/Auth/Login/`, `Features/Auth/Logout/`, `Features/Auth/GetMe/`, `Features/Auth/GetUsers/`, `Features/Auth/UpdateRole/`, `Features/Auth/UpdateStatus/`. Keep `Features/Auth/CreateInvitation/`, `Features/Auth/GetInvitations/`, `Features/Auth/ValidateInvitation/`, `Features/Auth/RevokeInvitation/`.

Remove `AppUser.cs` and `Behaviors/AppUser.cs` from Domain (User moved to Identity). Keep `Invitation.cs`. Remove `AppUsers` DbSet, `AppUserConfiguration`, `AppUserRepository` from Persistence. Keep Invitations.

Adapt Invitation endpoints: replace `AppUserRepository` usage with claims-based user info. Rename `AddFokusServices` → `AddFokusModule`. Wire `AddPersistenceServices` and `AddApiServices` per `service-registration` skill. Add to solution. Reference from Host.

**Skill:** `service-registration`
**Source:** `<fokus-repo>\src\Services\Fokus\`
**Accept:** Solution builds. All Fokus endpoints compile.

### Step 9: Port frontend + wire Host composition

Copy `<fokus-repo>\client\` into `sprint-rituals/client/`. Update `vite.config.ts` — set `outDir` to `../src/Host/wwwroot`, proxy to Host port. Remove auth-related API calls that moved to Identity (login/logout/getMe) and point them at the same paths (no URL change needed — same Host serves both).

Wire final Host `Program.cs`: call both `AddIdentityModule` + `AddFokusModule`, configure FastEndpoints multi-assembly scanning (both service assemblies), register `ActiveUserPreProcessor` globally, handle migrations for both DbContexts (Identity first), wire OpenAPI + Scalar in dev, SPA fallback.

**Skill:** `service-registration`
**Accept:** Full end-to-end:
- `dotnet run --project src/Host` starts, creates both .db files
- Frontend at localhost:5173 works (Vite dev proxy)
- Google OAuth login → Person + User created in identity.db
- Dashboard loads, API calls work against fokus.db
- Invitation flow still works
- Scalar UI shows endpoints from both services in dev

## Migration Notes

- Fresh databases — no migration from existing fokus.db
- Identity: `dotnet ef migrations add InitialCreate --project src/Services/Identity/Identity.Persistence --startup-project src/Host`
- Fokus: copy existing migrations from source repo, then add `RemoveAppUsers` migration
- Host startup: Identity migrations first, then Fokus migrations

## Testing Strategy

- Build: `dotnet build src/SprintRituals.slnx` — zero errors
- Auth flow: Google OAuth login → Person + User in identity.db, cookie set
- Bootstrap: first user gets Admin role
- Fokus features: all analytics/sync/settings endpoints work
- Invitations: create/validate/accept works
- Frontend: Vite dev server proxies correctly
- ActiveUserPreProcessor: deactivated user gets 401

## Open Questions

None — all decisions resolved.
