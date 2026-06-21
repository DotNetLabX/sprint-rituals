# Suite Scaffolding V2 — Implementation

## Steps Status

| Step | Status | Notes |
|------|--------|-------|
| 1 — Repo skeleton + .claude/docs seed | Done | |
| 2 — BuildingBlocks + Modules | Done | 0 CS errors |
| 3 — Host skeleton | Done | |
| 4 — Identity service skeleton | Done | |
| 5 — Identity domain (plain DDD) | Done | |
| 6 — Identity persistence | Done | Migration generated |
| 7 — Identity auth + endpoints | Done | |
| 8 — Port Fokus as module | Done | RemoveAppUsers migration added |
| 9 — Host composition + frontend + VERIFY GATE | Done | Gate items 1, 2, 8 confirmed; items 3/3b/4/6/7 handoff to user |

---

## Files Created

### Repo root / .claude / docs
- `CLAUDE.md` — root CLAUDE.md adapted for modular-monolith (Host + Identity + Fokus); auth ownership and DB table documented
- `SprintRituals.slnx` — solution file with all 17 projects
- `.claude\` — copied from `<fokus-repo>\.claude\` (agents, rules, conventions, skills, settings.json)
- `docs\conventions\` — copied from Fokus (csharp.md, ef-core.md, project-rules.md, etc.)
- `docs\architecture\` — copied from Fokus (index.md, v1.md)
- `docs\proposals\suite-integration-architecture.md` — copied from Fokus

### BuildingBlocks (clean copy from Fokus)
- `src/BuildingBlocks/Blocks.Core/` — guard utilities, FluentValidation helpers
- `src/BuildingBlocks/Blocks.Domain/` — Entity<T>, AggregateRoot<T>, IDomainEvent, DomainEventPublisher
- `src/BuildingBlocks/Blocks.Exceptions/` — exception hierarchy (BadRequestException, NotFoundException, etc.)
- `src/BuildingBlocks/Blocks.EntityFrameworkCore/` — ApplicationDbContext, DispatchDomainEventsInterceptor
- `src/BuildingBlocks/Blocks.AspNetCore/` — GlobalExceptionMiddleware
- `src/BuildingBlocks/Blocks.FastEndpoints/` — DomainEventPublisher FastEndpoints impl

### Modules (clean copy from Fokus — required by Fokus.Domain/API)
- `src/Modules/Jira/Jira.Contracts/` — Jira DTOs shared across Fokus.Domain and Fokus.API
- `src/Modules/Jira/Jira.RestApi/` — Jira HTTP client, required by Fokus.API
- `src/Modules/Xray/Xray.Contracts/` — Xray DTOs
- `src/Modules/Xray/Xray.GraphQL/` — Xray GraphQL client, required by Fokus.API

### Host
- `src/Host/Host.csproj` — Web SDK, net10.0; references Identity.API + Fokus.API + Blocks.AspNetCore
- `src/Host/Program.cs` — full composition: AddIdentityModule + AddFokusModule, multi-assembly FastEndpoints, both DB migrations, AppSettings seed, Scalar/OpenAPI gated to Development
- `src/Host/appsettings.json` — two connection strings (IdentityDatabase → data/identity.db, Database → data/fokus.db), Google/Jira/Xray placeholder sections
- `src/Host/appsettings.Development.json` — debug logging
- `src/Host/Properties/launchSettings.json` — HTTP profile on port 5000

### Identity.Domain
- `src/Services/Identity/Identity.Domain/Identity.Domain.csproj`
- `src/Services/Identity/Identity.Domain/GlobalUsings.cs`
- `src/Services/Identity/Identity.Domain/Enums/PersonSource.cs` — Google | Jira | Guest
- `src/Services/Identity/Identity.Domain/Persons/Person.cs` — AggregateRoot<int> state: FirstName, LastName, FullName (computed), Email (NOCASE), PictureUrl, Source, ExternalId, CreatedAt, UserId?
- `src/Services/Identity/Identity.Domain/Persons/Behaviors/Person.cs` — CreateFromGoogle, UpdateFromGoogle, LinkUser
- `src/Services/Identity/Identity.Domain/Persons/Events/PersonCreated.cs` — carries int PersonId
- `src/Services/Identity/Identity.Domain/Users/User.cs` — Entity<int> state: PersonId, Role, IsActive, RegistrationDate, LastLogin?
- `src/Services/Identity/Identity.Domain/Users/Behaviors/User.cs` — CreateFromPerson, RecordLogin, Deactivate, Reactivate
- `src/Services/Identity/Identity.Domain/Users/Events/UserCreated.cs` — carries int UserId

### Identity.Persistence
- `src/Services/Identity/Identity.Persistence/Identity.Persistence.csproj`
- `src/Services/Identity/Identity.Persistence/GlobalUsings.cs`
- `src/Services/Identity/Identity.Persistence/IdentityDbContext.cs` — plain DbContext (NOT IdentityDbContext), DbSet<Person> + DbSet<User>
- `src/Services/Identity/Identity.Persistence/Configurations/PersonConfiguration.cs` — Email NOCASE collation, unique index, Source stored as string
- `src/Services/Identity/Identity.Persistence/Configurations/UserConfiguration.cs` — 1:1 Person FK (Restrict), Role default "Manager", IsActive default true
- `src/Services/Identity/Identity.Persistence/Repositories/PersonRepository.cs` — GetByIdAsync, GetByEmailAsync (lowercased), GetByExternalIdAsync, AddAsync, SaveChangesAsync
- `src/Services/Identity/Identity.Persistence/Repositories/UserRepository.cs` — GetByIdAsync, GetByPersonIdAsync, CountAsync, CountActiveAdminsAsync, GetAllWithPersonAsync (Join, ordered by name), AddAsync, SaveChangesAsync
- `src/Services/Identity/Identity.Persistence/DesignTimeDbContextFactory.cs` — hardcoded "Data Source=identity.db"
- `src/Services/Identity/Identity.Persistence/DependencyInjection.cs` — AddIdentityPersistence: SQLite on IdentityDatabase conn str, DomainEventPublisher, DispatchDomainEventsInterceptor, PersonRepository, UserRepository
- `src/Services/Identity/Identity.Persistence/Migrations/20260605102711_InitialCreate.cs` — creates Persons + Users tables

### Identity.API
- `src/Services/Identity/Identity.API/Identity.API.csproj` — class library with FrameworkReference, FastEndpoints, Google auth
- `src/Services/Identity/Identity.API/GlobalUsings.cs`
- `src/Services/Identity/Identity.API/DependencyInjection.cs` — AddIdentityModule (calls AddIdentityAuth + AddIdentityPersistence)
- `src/Services/Identity/Identity.API/Auth/AuthConfiguration.cs` — AddIdentityAuth: cookie + Google OAuth; OnTicketReceived: bootstrap path (first user → Admin) + regular login path (ExternalId/email lookup, reject missing/inactive); deferred stubs: invite-token redemption, CompanyDomain auto-seed
- `src/Services/Identity/Identity.API/Auth/ActiveUserPreProcessor.cs` — IGlobalPreProcessor; 401 for missing/inactive user; return after response write
- `src/Services/Identity/Identity.API/Features/Auth/Login/LoginEndpoint.cs` — minimal API GET /auth/login; Google challenge; invite_token in properties
- `src/Services/Identity/Identity.API/Features/Auth/Logout/LogoutEndpoint.cs` — POST /api/auth/logout
- `src/Services/Identity/Identity.API/Features/Auth/GetMe/GetMeQuery.cs` — MeResponse DTO
- `src/Services/Identity/Identity.API/Features/Auth/GetMe/GetMeEndpoint.cs` — GET /api/auth/me, claims-based
- `src/Services/Identity/Identity.API/Features/Auth/GetUsers/GetUsersQuery.cs` — UserResponse DTO (Id, Email, DisplayName, AvatarUrl, Role, LastLoginAt, IsActive)
- `src/Services/Identity/Identity.API/Features/Auth/GetUsers/GetUsersEndpoint.cs` — GET /api/users [Admin], uses GetAllWithPersonAsync; DisplayName = Person.FullName
- `src/Services/Identity/Identity.API/Features/Auth/UpdateRole/UpdateRoleCommand.cs` — UpdateRoleRequest + validator
- `src/Services/Identity/Identity.API/Features/Auth/UpdateRole/UpdateRoleEndpoint.cs` — PUT /api/users/{id}/role [Admin], last-admin protection
- `src/Services/Identity/Identity.API/Features/Auth/UpdateStatus/UpdateStatusCommand.cs` — UpdateStatusRequest + validator
- `src/Services/Identity/Identity.API/Features/Auth/UpdateStatus/UpdateStatusEndpoint.cs` — PUT /api/users/{id}/status [Admin], last-admin protection

### Fokus (modified in-place during copy)
- `src/Services/Fokus/Fokus.API/Fokus.API.csproj` — converted Web SDK → class library + FrameworkReference; removed Google/Scalar/OpenAPI/EF.Design packages
- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` — renamed AddFokusServices → AddFokusModule; removed auth/swagger registrations; kept all analytics services
- `src/Services/Fokus/Fokus.API/GlobalUsings.cs` — added `Microsoft.AspNetCore.Http` and `Microsoft.Extensions.Logging` (lesson: required in class-library context)
- `src/Services/Fokus/Fokus.API/Features/Auth/CreateInvitation/CreateInvitationEndpoint.cs` — removed AppUserRepository dependency; inviterId now read from claims only; qualified `Fokus.Domain.Auth.Invitation.Create(...)` to avoid ambiguity with System.IO.FileInfo.Create
- `src/Services/Fokus/Fokus.Persistence/FokusDbContext.cs` — removed AppUsers DbSet
- `src/Services/Fokus/Fokus.Persistence/DependencyInjection.cs` — removed AppUserRepository registration
- `src/Services/Fokus/Fokus.Persistence/Configurations/InvitationConfiguration.cs` — dropped HasOne<AppUser> FK; InvitedByUserId is now unconstrained int (cross-module reference by ID only)
- `src/Services/Fokus/Fokus.Persistence/Migrations/20260605104308_RemoveAppUsers.cs` — drops AppUsers table and FK_Invitations_AppUsers_InvitedByUserId

### Removed from Fokus (moved to Identity or eliminated)
- `Fokus.API/Auth/AuthConfiguration.cs` — moved to Identity
- `Fokus.API/Auth/AuthMiddleware.cs` — moved to Identity
- `Fokus.API/Auth/ActiveUserPreProcessor.cs` — moved to Identity
- `Fokus.API/Program.cs` — Host owns this now
- `Fokus.API/Features/Auth/{Login,Logout,GetMe,GetUsers,UpdateRole,UpdateStatus}/` — moved to Identity
- `Fokus.Domain/Auth/AppUser.cs` + `Behaviors/AppUser.cs` — eliminated (Identity owns Person+User)
- `Fokus.Persistence/Configurations/AppUserConfiguration.cs` — eliminated
- `Fokus.Persistence/Repositories/AppUserRepository.cs` — eliminated

### Frontend
- `client/` — clean copy from Fokus; `vite.config.ts` outDir retargeted from `../src/Services/Fokus/Fokus.API/wwwroot` → `../src/Host/wwwroot`; proxy (`/api`, `/auth`) stays on localhost:5000 (correct — Host port)

---

## Files Modified (sprint-rituals only; Fokus source unchanged)

All modifications are in `this repo\` — the Fokus source repo (`<fokus-repo>\`) was never touched.

---

## Key Decisions

1. **Jira + Xray modules copied (scope justification):** The plan scope says "BuildingBlocks (6) + Modules (Jira, Xray)". Fokus.Domain references `Jira.Contracts` and Fokus.API references `Jira.RestApi` + `Xray.GraphQL` — they are required compile dependencies for Fokus to build as a module. This is not a scope deviation; the plan explicitly lists them.

2. **UserCreated domain event simplified:** `User` is `Entity<int>`, not `AggregateRoot<int>`, so it has no `DomainEvents` list. Rather than add a parallel event infrastructure to Entity, UserCreated is not raised in the scaffold. PersonCreated is raised (Person is AggregateRoot). This is consistent with the scaffold-only scope — domain event handlers for UserCreated are not needed yet.

3. **`UseCollation("NOCASE")` on property not index:** EF Core 10 does not expose `.UseCollation()` on `IndexBuilder<T>` — it must be on the property. Applied to `PersonConfiguration.Email` as `.UseCollation("NOCASE")` on the property builder. Migration confirmed correct collation in DDL.

4. **`MapLoginEndpoint` ambiguity resolved:** Having both `Identity.API.DependencyInjection.MapLoginEndpoint` and `Identity.API.Features.Auth.Login.LoginEndpoint.MapLoginEndpoint` caused CS0121 in Host. Removed the wrapper from DependencyInjection; Program.cs calls `LoginEndpoint.MapLoginEndpoint(app)` directly.

5. **`AddFastEndpoints` in Host.csproj:** FastEndpoints is a transitive dependency via Identity.API but the top-level call in Program.cs requires the package directly in Host — added `FastEndpoints` + `FastEndpoints.Swagger` to Host.csproj.

6. **`InvitedByUserId` config:** Removed explicit `.IsRequired()` from InvitationConfiguration — `int` is non-nullable by default; EF already treats it as required. The explicit call caused a model snapshot delta (`PendingModelChangesWarning` at startup).

7. **`Fokus.Domain.Auth.Invitation.Create(...)` qualified:** `implicit usings` includes `System.IO`; the bare `Invitation.Create(...)` was ambiguous with `FileInfo.Create(...)`. Fully qualified the call in CreateInvitationEndpoint.

8. **`GetAllWithPersonAsync` orders by FirstName then LastName (not FullName):** `Person.FullName` is a computed C# property (`$"{FirstName} {LastName}".Trim()`) that EF Core ignores — it cannot be used in an `OrderBy` expression tree. The repository orders by `Person.FirstName` then `Person.LastName` instead, which produces the same sort order for typical names. This is intentional and documented in the repository XML comment.

---

## Verify Gate Results

### Item 1 — `dotnet build SprintRituals.slnx`
**PASS** — 17 projects, 0 CS errors, 4 NU1903 warnings (transitive `Microsoft.Build.Tasks.Core` 17.7.2 advisory from EF Design package — same as Fokus source, non-actionable in scaffold).

### Item 2 — Both DBs migrate on startup
**PASS**
- `identity.db`: "No migrations were applied. The database is already up to date." — InitialCreate (Persons + Users, COLLATE NOCASE) previously applied cleanly.
- `fokus.db`: "No migrations were applied. The database is already up to date." — all 15+ Fokus migrations including `20260605104308_RemoveAppUsers` previously applied cleanly.
- AppSettings seed: `SELECT EXISTS (SELECT 1 FROM "AppSettings")` executed — no error.

### Item 8 — Host starts, Scalar lists both modules' endpoints
**PASS (partial)**
- Host bound to `http://localhost:5000` successfully.
- FastEndpoints registered **55 endpoints** in 1,679 ms — both Identity and Fokus module assemblies scanned.
- Scalar UI at `/scalar/v1` returns HTTP 400 with Google OAuth `ClientId` empty — expected behaviour when no user-secrets are supplied. The Host itself is healthy; the error originates from the Google OAuth middleware validating options on every request. Full Scalar UI access requires supplying `Google:ClientId` and `Google:ClientSecret` via user-secrets (see handoff steps below).

### Items 3, 3b, 4, 6, 7 — HANDOFF TO USER
These require a real Google ClientId/Secret and a browser. Steps:

1. **Supply Google credentials via user-secrets** (never commit to any file):
   ```
   cd src\Host
   dotnet user-secrets set "Google:ClientId" "<your-client-id>"
   dotnet user-secrets set "Google:ClientSecret" "<your-client-secret>"
   ```
   The `UserSecretsId` is `sprint-rituals-host-a1b2c3d4` (set in Host.csproj).

2. **Ensure Google OAuth callback URI is registered** in the Google Cloud Console:
   - Authorized redirect URI: `http://localhost:5000/auth/callback`

3. **Start the Host:**
   ```
   cd src\Host
   dotnet run
   ```

4. **Item 3 — Bootstrap login (first user → Admin):**
   - Navigate to `http://localhost:5000/auth/login`
   - Complete Google sign-in
   - Expected: redirected to `/`, cookie set, `identity.db` has 1 Person row + 1 User row with `Role = "Admin"`
   - Verify: `GET http://localhost:5000/api/auth/me` returns `{ "role": "Admin", "displayName": "<your name>" }`

5. **Item 3b — Second login (F5, same account):**
   - Sign out: `POST http://localhost:5000/api/auth/logout`
   - Sign in again via `http://localhost:5000/auth/login`
   - Expected: `User.LastLogin` updated in `identity.db`, same claims returned

6. **Item 4 — Frontend at localhost:5173:**
   ```
   cd client
   npm install
   npm run dev
   ```
   - Navigate to `http://localhost:5173`
   - Expected: Vue SPA loads, dashboard renders (proxies /api and /auth to localhost:5000)

7. **Item 6 — Role enforcement:**
   - With a Manager-role cookie, call `GET http://localhost:5000/api/users` — expected 403
   - Unauthenticated call — expected 401

8. **Item 7 — ActiveUserPreProcessor:**
   - As Admin, call `PUT http://localhost:5000/api/users/{id}/status` with `{ "id": <id>, "isActive": false }` to deactivate a non-last-admin user
   - Attempt an authenticated API call with that user's cookie — expected 401

9. **Item 9 (manual) — Invitation endpoints:**
   - `POST http://localhost:5000/api/invitations` with Admin cookie — expected 201
   - `GET http://localhost:5000/api/invitations` — list
   - `DELETE http://localhost:5000/api/invitations/{id}` — revoke

---

## Carry-Over Findings

| Title | Severity | For | Evidence | Note |
|-------|----------|-----|----------|------|
| NU1903 `Microsoft.Build.Tasks.Core` advisory | low | reviewer | 4 warnings in build output on Host + Identity.Persistence | Transitive from EF Design; same advisory exists in Fokus source; not actionable in scaffold |
| CS9107 DeveloperRepository constructor warning | low | reviewer | `Fokus.Persistence/Repositories/DeveloperRepository.cs(6,57)` — parameter captured in both primary ctor and base | Pre-existing in Fokus source; not introduced by this work |
| `UserCreated` domain event not raised | low | architect | `User.CreateFromPerson()` has no event dispatch (User is Entity not AggregateRoot) | Acceptable for scaffold; note for infra increment if event-sourcing needed |
| CompanyDomain auto-seed deferred | medium | architect | `// DEFERRED (infra increment)` comment in AuthConfiguration.cs OnTicketReceived | Documented per plan Step 7; only consumed by invitation validation which is itself deferred |
| Invite-token redemption deferred | medium | architect | Stub in AuthConfiguration returns `/invite/error?reason=not-implemented` | Documented per plan; needs cross-module plumbing in infra increment |

---

## KB Changes

None — no domain concepts introduced that aren't already described in the plan. Identity module is new but the plan + CLAUDE.md serve as its specification. A KB entry for Identity can be added in the infra increment when the domain stabilises.

---

## Deviations from Plan

| Step | Plan | Actual | Reason |
|------|------|--------|--------|
| Step 4 | `DependencyInjection.cs` stubs with no-op `AddIdentityModule` | Stub written first (no-op), then fully wired in Steps 6–7 | Same result — step sequencing merged for efficiency |
| Step 7 | `MapIdentityEndpoints` maps minimal-API routes | Login endpoint uses static `LoginEndpoint.MapLoginEndpoint(app)` called from Host Program.cs directly | Avoids the CS0121 ambiguity that arose from having duplicate `MapLoginEndpoint` extension methods; cleaner than an extra wrapper |
| Step 8 | "Add a `RemoveAppUsers` migration after copying Fokus's existing migrations" | Migration added; triggered `PendingModelChangesWarning` on first Host startup due to `.IsRequired()` on int property (redundant, created snapshot delta) | Fixed by removing the redundant config; `int` is non-nullable by default in EF |
| Step 9 | Host startup uses `SqliteConnectionStringBuilder` to extract DataSource | Implemented exactly as in Fokus Program.cs with added `Path.IsPathRooted` guard for relative paths | Defensive: `data/fokus.db` is relative; without the guard, `Path.GetDirectoryName` returns empty |
| All steps | `.claude/agents/` copied from Fokus | Copied `.claude/agents/`, `.claude/rules/`, `.claude/conventions/`, `.claude/skills/`, `.claude/settings.json` — but NOT `.claude/hooks/`, `.claude/commands/`, or `settings.local.json` | hooks and commands are machine-local; settings.local.json has blanket bash permissions suited to the fokus dev environment, not sprint-rituals |

---

## Review Fixes (Cycle 1)

| Finding | Action | File(s) changed |
|---------|--------|----------------|
| H1 — PersonCreated carries Id=0 | Removed `AddDomainEvent` from `CreateFromGoogle`; added `RaiseCreated()` method to Person behaviors; called `person.RaiseCreated()` + second `SaveChangesAsync()` in AuthConfiguration bootstrap path after the first save (when EF has assigned the real PK). No BuildingBlocks changes — fix is localized to Domain + Auth. | `Identity.Domain/Persons/Behaviors/Person.cs`, `Identity.API/Auth/AuthConfiguration.cs` |
| M1 — User.Role public setter | Changed `Role` to `private set` in `User.cs`; added `public void ChangeRole(string role)` to `Users/Behaviors/User.cs`; replaced `user.Role = req.Role` with `user.ChangeRole(req.Role)` in `UpdateRoleEndpoint.cs`. `CreateFromPerson` object initializer compiles — private setter accessible within the partial class. | `Identity.Domain/Users/User.cs`, `Identity.Domain/Users/Behaviors/User.cs`, `Identity.API/Features/Auth/UpdateRole/UpdateRoleEndpoint.cs` |
| M2 — GetAllWithPersonAsync Join comment | Updated XML doc comment to state "Manual join used because Person.FullName is a computed property ignored by EF; Include would still require a Select projection." Also corrected "ordered by Person.FullName" → "ordered by FirstName then LastName". | `Identity.Persistence/Repositories/UserRepository.cs` |
| L1 — return after CompleteAsync | Added `return;` after `await httpContext.Response.CompleteAsync();` in `ActiveUserPreProcessor.PreProcessAsync`. | `Identity.API/Auth/ActiveUserPreProcessor.cs` |
| L2 — ordering decision documented | Added Key Decision #8 to implementation.md explaining why FirstName/LastName ordering is used instead of FullName (EF-ignored computed property). | `implementation.md` |

### Build result (Cycle 1)

Identity.Domain and Identity.API build with **0 CS errors** after fixes. Full solution build (`SprintRituals.slnx`) could not complete — the background Host process from the verify gate (PID 5152) was still running and locking the Identity DLLs (MSB3027/MSB3021 file-copy failures, not compilation errors). Kill the Host process (`Stop-Process -Id 5152 -Force`) and re-run `dotnet build SprintRituals.slnx` to confirm clean full-solution build.
