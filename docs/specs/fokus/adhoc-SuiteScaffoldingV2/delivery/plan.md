# Suite Scaffolding V2 — SprintRituals Monolith + Identity (plain DDD)

**Feature Spec:** None — ad-hoc infrastructure, driven by `docs/proposals/suite-integration-architecture.md` (Status: Decided).
**Supersedes:** `docs/specs/adhoc-SuiteScaffolding/` (prior attempt, discarded). This plan keeps that run's proven structure and **bakes in its 3-review-cycle lessons**, but replaces ASP.NET Core Identity with a plain-DDD identity model.

## Context

Fokus (mature, F1–F35) and Reflekt are standalone repos. The decided proposal merges them into one **modular-monolith** Host with a new **Identity** module. The prior scaffold was discarded: it copied Fokus into a fresh repo and chose **full ASP.NET Core Identity**, which caused 3 review cycles (vulnerable `System.Security.Cryptography.Xml` NuGet, can't-inherit-both `IAggregateRoot` gymnastics, IsActive-migration bug). The discarded code was deleted; this repo is now an empty repo shell (only a suite-aware `.gitignore`, one "Initial commit").

This plan rebuilds the scaffold **into that empty repo**, via a disciplined clean copy from the original Fokus project, with a **hard end-to-end verification gate**.

## Locked Decisions (user-confirmed this session)

| # | Decision | Choice |
|---|----------|--------|
| 1 | Repo | Continue in this empty repo (don't re-copy a 3rd time, don't tear up live `fokus`) |
| 2 | Code migration | **Clean copy** from `fokus` + **hard verify gate**. No git-history preservation — `fokus` repo remains the historical record |
| 3 | Scope | **Scaffold only** — Host + Identity + Fokus-as-module, running with Google OAuth. No gRPC, no MassTransit, no Reflekt |
| 4 | Suite name | **SprintRituals** (`SprintRituals.slnx`, modules keep own root namespaces: `Identity.*`, `Fokus.*`) |
| 5 | **Identity model** | **Plain DDD** — `Person : AggregateRoot<int>` + `User : Entity<int>`. **No ASP.NET Core Identity / IdentityUser / IdentityDbContext / UserManager.** Role = string claim. Google-only |
| 6 | Invitation | **Stays in Fokus** (Fokus feature). Redemption-at-login deferred to the infra increment |
| 7 | Pipeline artifacts | Live in `<fokus-repo>/docs/specs/` (the `.claude` pipeline only exists here); code is built in `sprint-rituals` |

**Forward context (non-binding):** the suite is Google-only SSO. Real multi-provider SSO, if ever needed, becomes a separate "platform" application — so this Identity module stays minimal and swappable.

## Scope

**In scope**
- Repo skeleton in `sprint-rituals` + `.claude/` + `docs/` seed
- Host project (single Web SDK deployable)
- Identity module (Person, User, Google OAuth, cookie auth) — **plain DDD**
- Fokus module port (all features; `AppUser` → Identity; **Invitation stays**)
- BuildingBlocks (6) + Modules (Jira, Xray) clean copy
- Frontend (client/) clean copy + Vite retarget
- **Hard end-to-end verify gate**

**Out of scope**
- Reflekt port
- gRPC contracts / `Blocks.Contracts` / PersonGrpcService
- MassTransit / integration events / `SprintSynced`
- **Invitation redemption-at-login** (needs cross-module plumbing → infra increment)
- Frontend routing split (`/retro/*` vs `/dashboard/*`)

## Identity Reference

`<reference-repo>/src/Services/Auth/` — adopt the **DDD structure**, not the membership stack.

| Adopt | Do NOT adopt |
|-------|--------------|
| Person/User/Role 3-folder DDD split | `IdentityUser<int>` / `IdentityDbContext` / `UserManager` (we go plain) |
| Partial-class behavior split (`X.cs` + `Behaviors/X.cs`) | JWT / RefreshToken / `PostConfigureJwtBearerOptions` (we use cookie + Google) |
| Concrete repositories, `Query()` with Includes | Value objects (`EmailAddress`, `HonorificTitle`, `ProfessionalProfile`) — overkill |
| `PersonGrpcService` *pattern* (for the future infra increment, not now) | `IUserCreationInfo` abstraction (no gRPC consumers yet) |

The **authoritative source pattern for Identity is Fokus's own current auth** — it already runs the exact model we want (`AppUser : Entity<int>`, string `Role`, Google + cookie, `[Authorize(Roles="Admin")]`, `ActiveUserPreProcessor`) with zero ASP.NET Core Identity. Identity = that model, split into Person + User.

## Identity Domain Model (plain DDD)

**Person** — `AggregateRoot<int>`, the universal anchor (anyone from any source; may never log in).
```
Person : AggregateRoot<int>
  FirstName   : string
  LastName    : string
  FullName    : string (computed)
  Email       : string (required; stored normalized lowercase; unique, NOCASE)
  PictureUrl  : string?
  Source      : PersonSource (Google | Jira | Guest)
  ExternalId  : string?  (GoogleId / JiraAccountId / GuestToken)
  CreatedAt   : DateTime
  UserId      : int?     (nullable back-reference; set via LinkUser())
```

**User** — `Entity<int>`, a Person who authenticated. **Plain entity, not IdentityUser.**
```
User : Entity<int>
  PersonId         : int (required FK → Person, unique 1:1)
  Role             : string  ("Admin" | "Manager")
  IsActive         : bool = true
  RegistrationDate : DateTime
  LastLogin        : DateTime?
  behaviors: CreateFromPerson(), RecordLogin(), Deactivate(), Reactivate()
```

- Person created first; User second. Person may exist without a User.
- FK on User (`PersonId` required, unique). `Person.UserId?` is the nullable back-ref.
- EF: `HasOne<Person>().WithOne().HasForeignKey<User>(u => u.PersonId).IsRequired().OnDelete(DeleteBehavior.Restrict)`.
- **No `Role` table, no `RoleManager`** — `Role` is a string, emitted as `ClaimTypes.Role` in the cookie (exactly as Fokus does today).
- **Google name mapping (F3):** `FirstName` ← `given_name`, `LastName` ← `family_name`, `FullName` computed (`"First Last"`). If Google omits given/family, fall back to splitting `ClaimTypes.Name` on the first space (remainder → LastName; no space → whole value in FirstName).
- **Frontend parity (F3):** the `/api/auth/me` and `/api/users` responses keep their existing `DisplayName` field — map it to `Person.FullName`. The Vue client must not need changes.

## Project Reference Graph

```
Host ──▶ Identity.API ──▶ Identity.Persistence ──▶ Identity.Domain ──▶ Blocks.Domain
  │           └──▶ Blocks.FastEndpoints, Blocks.AspNetCore
  ├──▶ Fokus.API ──▶ Fokus.Persistence ──▶ Fokus.Domain ──▶ Blocks.Domain
  │        └──▶ Jira.RestApi/Contracts, Xray.GraphQL/Contracts, Blocks.AspNetCore
  └──▶ Blocks.AspNetCore

Fokus.API does NOT reference Identity.* (no cross-module dependency in the scaffold).
```

## Skill Mapping

| Step | Skill | Disposition | Notes |
|------|-------|-------------|-------|
| 1 — Repo skeleton + setup | None | None | Manual: folders, copy `.claude/`+`docs/`, root `CLAUDE.md`, empty `SprintRituals.slnx` |
| 2 — BuildingBlocks + Modules | None | None | Clean copy, add to slnx, isolated build |
| 3 — Host skeleton | `service-registration` | Follow | Host composition + DI pattern. **Host Program.cs orchestration is a skill gap (`compose-host`)** — inline detail provided |
| 4 — Identity skeleton | `create-service` | Follow | 3-project split. **Prereq: architect-authored `Identity/CLAUDE.md` (via `create-service-claude-md`)** |
| 5 — Identity domain | `create-aggregate`, `domain-patterns` | Follow | Person aggregate + plain User entity. **No ASP.NET Core Identity** |
| 6 — Identity persistence | `persistence-patterns` | Follow | Plain `DbContext` (not IdentityDbContext), configs, concrete repos |
| 7 — Identity auth + endpoints | `create-feature`, `authorization-patterns` | Follow | Google OAuth, cookie, pre-processor, login/logout/me + user-mgmt endpoints |
| 8 — Port Fokus module | `service-registration` | Follow | Web SDK → class library; remove AppUser/auth; keep Invitation |
| 9 — Host composition + frontend + VERIFY GATE | `service-registration` | Follow | Multi-assembly FastEndpoints, multi-DbContext migration; hard gate; docs update |

**Skill gap logged:** `compose-host` (multi-module Host Program.cs orchestration). Inline detail given in Steps 3 & 9; log to lessons for the learner.

---

## Cross-Repo Execution Notes (read before implementing)

This pipeline runs from the original Fokus project (where `.claude/` + the plan live), but **all code is built in this repo**. Verified working in this session: writes to sprint-rituals and all Bash/`dotnet`/`npm`/`git` commands execute **without permission prompts** (`settings.local.json` blanket-allows Bash/Write/Edit/Task/Skill). Operating rules:

1. **Always use absolute paths.** The Bash shell cwd **resets to the original Fokus project after every call.** For builds/migrations/tests, pass absolute paths or chain in a single invocation: `cd <repo> && dotnet build src/SprintRituals.slnx`.
2. **The Host run is long-running.** `dotnet run --project src/Host` blocks — launch it with `run_in_background`, then probe it; never run it foreground.
3. **EF migrations** rely on the hardcoded design-time connection string (Step 6); run from sprint-rituals with explicit `-p`/`-s` project paths.
4. **Google credentials are a secret the user must supply.** `Google:ClientId`/`ClientSecret` are NOT in the repo. The auth gate items (3, 3b, and the browser-dependent 4/6/7) **cannot pass unattended** — they need real Google OAuth creds in sprint-rituals user-secrets *and* a human at a browser. An unattended team run can complete **Steps 1–8 + gate items 1, 2, 8** (build, startup, migrations, Scalar shows both modules) and must hand live-login verification back to the user. **Never commit secrets.**
5. The Identity service `CLAUDE.md` (Step 4 prerequisite) is **already authored** at `src/Services/Identity/CLAUDE.md` — do not regenerate it; Step 1's `.claude` seed must not overwrite it.

## Implementation Steps

### Step 1: Repo skeleton + `.claude`/`docs` seed

Create the folder skeleton in this repo: `src/Host/`, `src/BuildingBlocks/`, `src/Modules/`, `src/Services/Identity/`, `src/Services/Fokus/`, `client/`, `docs/`. Seed `.claude/` (agents, rules, conventions, skills), `docs/conventions/`, `docs/architecture/`, and `docs/proposals/suite-integration-architecture.md` from the original Fokus project. Create root `CLAUDE.md` adapted from Fokus's — update the **Repo Structure** section for the modular monolith (Host + modules), keep stack/port/guardrail sections. Create empty `SprintRituals.slnx`.

- **Source:** `<fokus-repo>/.claude/`, `<fokus-repo>/docs/conventions/`, `<fokus-repo>/docs/architecture/`
- **Note:** the `.gitignore` already present in sprint-rituals is correct — do not overwrite it.
- **Note (non-blocking):** `.claude` is also being extracted into a plugin ("Nexus"). For now, copy it so the repo is self-sufficient; migrating to the plugin is a separate track.
- **Accept:** folder skeleton exists; `.claude/` + `docs/` seeded; root `CLAUDE.md` describes the monolith; empty `SprintRituals.slnx` present.
- **Confidence:** high

### Step 2: Copy BuildingBlocks + Modules; verify isolated build

Clean-copy all 6 BuildingBlocks (`Blocks.Core`, `Blocks.Exceptions`, `Blocks.Domain`, `Blocks.EntityFrameworkCore`, `Blocks.AspNetCore`, `Blocks.FastEndpoints`) and both Modules (`Jira/Jira.Contracts`, `Jira/Jira.RestApi`, `Xray/Xray.Contracts`, `Xray/Xray.GraphQL`) from `<fokus-repo>/src/`. Add all to `SprintRituals.slnx`.

- **Source:** `<fokus-repo>/src/BuildingBlocks/`, `<fokus-repo>/src/Modules/`
- **Accept:** `dotnet build` passes (0 `CS` errors) for all 10 copied projects in isolation. Check the full output for `NU1903`/`NU1902` advisories — report any (lesson).
- **Confidence:** high

### Step 3: Host project skeleton

Create `src/Host/Host.csproj` (`Microsoft.NET.Sdk.Web`, net10.0) and a **skeleton** `Program.cs` (middleware pipeline, static files, SPA fallback) with no module registrations yet. Create `appsettings.json` with **two** connection strings — `Database` (→ `data/fokus.db`) and `IdentityDatabase` (→ `data/identity.db`) — plus `Jira`/`Xray`/`Google` config sections (placeholders). Create `appsettings.Development.json` and `Properties/launchSettings.json` (single HTTP profile, port 5000).

- **Skill:** `service-registration` (Host composition section)
- **Inline (skill gap `compose-host`):** Host owns the multi-assembly FastEndpoints registration and both DbContext migrations — wired fully in Step 9. Step 3 is the empty shell.
- **Accept:** `dotnet build src/Host/Host.csproj` passes with no module registrations.
- **Confidence:** medium

### Step 4: Identity service skeleton

Scaffold `src/Services/Identity/` as 3 projects: `Identity.API` (class library — `Microsoft.NET.Sdk` + `<FrameworkReference Include="Microsoft.AspNetCore.App" />`), `Identity.Domain`, `Identity.Persistence`. Correct references per the graph above. `DependencyInjection.cs` stubs in API + Persistence (`AddIdentityModule` no-op). Add to solution; reference `Identity.API` from Host.

- **Skill:** `create-service`
- **Prerequisite (architect-authored):** `src/Services/Identity/CLAUDE.md` — the `create-service` skill hard-errors without it. The architect provides it via `create-service-claude-md` (Identity axes: FastEndpoints, SQLite `identity.db`, plain-DDD Person/User, Google+cookie auth, no gRPC/events). Lesson: include CLAUDE.md creation in the step that needs it.
- **Lesson:** do **not** add forward-referencing `global using` for domain namespaces in `GlobalUsings.cs` until Step 5 creates those types (causes CS0246).
- **Accept:** solution builds; Host calls `AddIdentityModule` (no-op).
- **Confidence:** medium

### Step 5: Identity domain (plain DDD)

Create the Person aggregate (partial split: `Persons/Person.cs` + `Persons/Behaviors/Person.cs` — `CreateFromGoogle`, `UpdateFromGoogle`, `LinkUser`) and the plain `User` entity (partial split: `Users/User.cs` + `Users/Behaviors/User.cs` — `CreateFromPerson`, `RecordLogin`, `Deactivate`, `Reactivate`). Create `Enums/PersonSource.cs`. Domain events `Persons/Events/PersonCreated.cs` and `Users/Events/UserCreated.cs` carry **primitive ids only** (`int PersonId` / `int UserId`) — never the entity (lesson: avoids layering coupling).

Model per the **Identity Domain Model** section above. **No `Microsoft.AspNetCore.Identity` package anywhere.** `Email` stored normalized (`.ToLowerInvariant()`).

- **Skill:** `create-aggregate`, `domain-patterns`
- **Files:** under `src/Services/Identity/Identity.Domain/`
- **Accept:** `Identity.Domain` builds; no ASP.NET Core Identity reference; `User` is `Entity<int>` (not `IdentityUser`).
- **Confidence:** high (mirrors Fokus's proven `AppUser`)

### Step 6: Identity persistence

Create `IdentityDbContext.cs` as a **plain `DbContext`** (NOT `IdentityDbContext<...>`) with `DbSet<Person>` + `DbSet<User>`. Configs: `PersonConfiguration` (required fields; **unique email index with `.UseCollation("NOCASE")`** — lesson) and `UserConfiguration` (1:1 Person FK, `DeleteBehavior.Restrict`; `Role` max-length + default; `IsActive` `HasDefaultValue(true)` — lesson). Concrete `PersonRepository` (`GetByEmailAsync` lowercased, `GetByExternalIdAsync`, `GetByIdAsync`) and `UserRepository` (`GetByIdAsync`, `GetByPersonIdAsync`, `CountAsync`, `CountActiveAdminsAsync`, **`GetAllWithPersonAsync` — F2: `Include(Person)`, ordered by `Person.FullName`; feeds the `GetUsers` list**). Any read whose response carries display fields (Email, name, AvatarUrl) **must eager-load Person** — those fields live on Person, not User. Wire `AddPersistenceServices` (SQLite → `IdentityDatabase` conn str, repos, the standard `DispatchDomainEventsInterceptor`). Add `DesignTimeDbContextFactory` with a **hardcoded** conn str (lesson). Generate `InitialCreate` migration.

- **Skill:** `persistence-patterns`
- **Files:** under `src/Services/Identity/Identity.Persistence/`
- **Accept:** `InitialCreate` migration generates; DbContext instantiates; email index is case-insensitive.
- **Confidence:** high

### Step 7: Identity auth + endpoints

Create `Auth/AuthConfiguration.cs` — Google OAuth + cookie scheme (port Fokus's `AddFokusAuth` 1:1, adapting `AppUser` → Person+User). `OnTicketReceived` handles **two paths for the scaffold**:
1. **Bootstrap** — first User (count 0) → create Person + User with `Role = "Admin"`.
2. **Regular login** — find Person by `ExternalId`(GoogleId)/email → its User → reject if missing (`/login?error=no-account`) or `!IsActive` (`/login?error=deactivated`) → `RecordLogin`.

**Deferred to the infra increment (mark with `// DEFERRED (infra increment)` stubs — do NOT wire cross-module reads):**
- **Invite-token redemption** — the `invite_token` path that validates/accepts a Fokus `Invitation`.
- **CompanyDomain auto-seed (F1)** — Fokus's current bootstrap sets `AppSettings.CompanyDomain` from the first admin's email domain. `AppSettings` lives in `fokus.db` (different module); replicating it now would force `Identity → Fokus` coupling. It is only consumed by invitation validation, which is itself deferred — so defer the seed too. When invitations light up, this moves to the Fokus side (Fokus derives it). Document so it isn't silently lost. Build the claims principal (`NameIdentifier = User.Id`, `Email`, `Name = Person.FullName`, `Role`, `avatar_url`), sign in the cookie.

Create `Auth/ActiveUserPreProcessor.cs` (global pre-processor; looks up Identity `User` by id; **401 for both missing and inactive** — match current Fokus behavior, do NOT use 403 (F6); **`return` immediately after every response write**, lesson). Create endpoints: `Login` (minimal API GET `/auth/login` Google challenge), `Logout` (POST `/auth/logout`), `GetMe` (`GET /api/auth/me`, claims-based), and the **user-management endpoints moved from Fokus**: `GetUsers` (`GET /api/users`), `UpdateRole` (`PUT /api/users/{id}/role`, last-admin protection via `CountActiveAdminsAsync`), `UpdateStatus` (`PUT /api/users/{id}/status`) — all `[Authorize(Roles="Admin")]`, operating on Identity `User`.

- **Companion files (F4):** each ported endpoint carries its sibling command/query file (request/response DTOs + validator) per the vertical-slice pattern — `GetMeQuery.cs` (`MeResponse`), `GetUsersQuery.cs` (`UserResponse`), `UpdateRoleCommand.cs`, `UpdateStatusCommand.cs`. Port them with their endpoints.
- **Person join (F2):** `UserResponse`/`MeResponse` carry `Email`, `DisplayName`, `AvatarUrl` (Person fields) plus `Role`, `IsActive`, `LastLoginAt`, `Id` (User fields). Populate via the repository's `Include(Person)`; `DisplayName` = `Person.FullName`. The `{id}` route param is `User.Id` (write-back target for UpdateRole/UpdateStatus). Wire `AddIdentityModule` → `AddApiServices` (Google+cookie) + `AddPersistenceServices`; `MapIdentityEndpoints` maps the minimal-API routes.

- **Skill:** `create-feature`, `authorization-patterns`
- **Source pattern:** `<fokus-repo>/src/Services/Fokus/Fokus.API/Auth/` and `Features/Auth/{GetMe,GetUsers,UpdateRole,UpdateStatus,Login,Logout}/`
- **Lesson:** `[Tags]` needs `global using Microsoft.AspNetCore.Http;`; `ILogger<>` needs `global using Microsoft.Extensions.Logging;` (class-library context).
- **Accept:** Identity builds end-to-end; Google config compiles; `GetMe` returns claims shape.
- **Confidence:** medium (auth flow is the highest-risk area; port carefully, diff against Fokus original)

### Step 8: Port Fokus as a module

Clean-copy `<fokus-repo>/src/Services/Fokus/` (excluding `Fokus.Tests` unless trivially portable) into `src/Services/Fokus/`. Convert `Fokus.API.csproj` Web SDK → class library + `<FrameworkReference Include="Microsoft.AspNetCore.App" />`; remove the Google/Scalar/OpenAPI packages now owned by Host/Identity; add `Microsoft.AspNetCore.Http` + `Microsoft.Extensions.Logging` to `GlobalUsings.cs` (lessons). Remove `Program.cs` (Host owns it).

**Remove (moved to Identity):** `Auth/AuthConfiguration.cs`, `Auth/AuthMiddleware.cs`, `Auth/ActiveUserPreProcessor.cs`; `Features/Auth/{Login,Logout,GetMe,GetUsers,UpdateRole,UpdateStatus}/`; `Domain/Auth/AppUser.cs` + `Behaviors/AppUser.cs`; `Persistence/Configurations/AppUserConfiguration.cs`, `Persistence/Repositories/AppUserRepository.cs`, `AppUsers` DbSet.

**Keep (Fokus feature):** `Domain/Auth/Invitation.cs` + behaviors; `Features/Auth/{CreateInvitation,GetInvitations,RevokeInvitation,ValidateInvitation}/`; `InvitationRepository`, `InvitationConfiguration`.

**Rewire:** `CreateInvitationEndpoint` reads `inviterId` from the cookie claim (`ClaimTypes.NameIdentifier`) instead of `AppUserRepository` (it's already `[Authorize(Roles="Admin")]`). In `InvitationConfiguration`, **drop the `HasOne<AppUser>()` FK** — `InvitedByUserId` becomes an unconstrained `int` (cross-module reference, no DB FK — lesson; add a comment). Rename `AddFokusServices` → `AddFokusModule`/`AddApiServices`/`MapFokusEndpoints`; remove auth registration from Fokus DI (Host owns auth). Add a `RemoveAppUsers` migration after copying Fokus's existing migrations. Add projects to solution; reference from Host.

- **Skill:** `service-registration`
- **Verification sub-step:** grep the copied Fokus tree for `AppUser` and `AppUserRepository` — every remaining hit must be either removed or rewired to claims. Diff `[Authorize]` attributes against the Fokus original to confirm no access control was dropped (lesson).
- **Accept:** Fokus compiles as a class library; all kept endpoints build; no `AppUser` references remain; Invitation endpoints intact.
- **Confidence:** medium

### Step 9: Host composition + frontend + VERIFY GATE

Wire the final Host `Program.cs`: call `AddIdentityModule` + `AddFokusModule`; register FastEndpoints across **both** assemblies via `services.AddFastEndpoints(o => o.Assemblies = [ ... ])` (v6 IServiceCollection API — lesson) with the global `ActiveUserPreProcessor`; configure cookie + Google auth (owned by Identity's `AddApiServices`); run **both** DbContext migrations on startup (**Identity first**, then Fokus) and ensure the `data/` dir exists; seed Fokus `AppSettings` default row (port Fokus's startup seed); gate Scalar/OpenAPI to `IsDevelopment`; `MapFallbackToFile("index.html")`. Copy `<fokus-repo>/client/` → `sprint-rituals/client/`; set Vite `build.outDir` → `../src/Host/wwwroot`; keep `/api` + `/auth` proxy to the Host port. Update root `CLAUDE.md` (Identity module added; auth now in Identity) and add all projects to `SprintRituals.slnx`.

- **Skill:** `service-registration` (Host composition + multi-assembly + multi-DbContext)

**HARD VERIFY GATE — every item must pass with observed evidence (not assumed):**
1. `dotnet build src/SprintRituals.slnx` → **0 `CS` errors** (report `NU190x` advisories).
2. `dotnet run --project src/Host` starts; **both** `identity.db` + `fokus.db` are created and migrated (startup logs).
3. Google OAuth login (first user) → **Person + User created in `identity.db`**, User `Role = "Admin"`, cookie set.
3b. **Second login (F5), same Google account** → recognised via `ExternalId`/email, `User.LastLogin` updated in `identity.db`, cookie claims match the joined Person+User data (`FullName`, `Email`, `Role`, `avatar_url`). (Regular-login path is higher-risk than bootstrap — it exercises the ExternalId lookup + Person→User navigation.)
4. Frontend at `localhost:5173` (Vite dev proxy) loads; dashboard renders.
5. A Fokus API call (e.g. `GET /api/settings`) succeeds against `fokus.db`.
6. `[Authorize(Roles="Admin")]` enforced — a Manager-claim request to an Admin endpoint gets 403; unauthenticated gets 401.
7. `ActiveUserPreProcessor` — deactivated user → 401.
8. Scalar UI (dev) lists endpoints from **both** modules.
9. Invitation endpoints (`POST /api/invitations`, list, revoke) work for an Admin; redemption-at-login is the documented deferred gap.

- **Accept:** all 9 gate items pass; evidence recorded in `implementation.md`. Any failure → fix before "done."
- **Confidence:** medium (this is the integration crux; the gate is the safety net the prior run lacked)

---

## Migration Notes

- Fresh databases — no data migration from existing `fokus.db`.
- Identity: `dotnet ef migrations add InitialCreate -p src/Services/Identity/Identity.Persistence -s src/Host`
- Fokus: copy existing migrations from source, then `dotnet ef migrations add RemoveAppUsers -p src/Services/Fokus/Fokus.Persistence -s src/Host`
- Host startup: **Identity migrations first**, then Fokus.

## Commit

The repo currently has one "Initial commit" (`.gitignore`). After the verify gate passes, the **user** decides when to commit per the commit protocol — do not auto-commit. No secrets in any commit (`.env`, `appsettings` with real Google/Jira creds, `*.db`).

## Lessons carried in from the discarded run

`[TRACKED]` items baked into the steps above: class-library `FrameworkReference`; `[Tags]`/`ILogger` global usings; FastEndpoints v6 multi-assembly API; no forward-ref GlobalUsings; hardcoded design-time conn str; cross-service FK removal; CLAUDE.md prerequisite; SQLite NOCASE email; `IsActive` default `true`; pre-processor `return`-after-write; invite-token round-trip (deferred, but documented so it isn't silently lost). **The biggest lesson — avoid ASP.NET Core Identity — is now a structural decision (Step 5), not a review fix.**

## Plan Review

**Critic (Mode 2, plan vs proposal + locked decisions):** verdict REVISE → **all findings resolved** (see `review.md`). Confirmed sound: coverage, plain-DDD (zero ASP.NET Core Identity traces), all 11 `[TRACKED]` lessons baked in, ordering, no over-specification.

| Finding | Sev | Resolution |
|---------|-----|-----------|
| F1 CompanyDomain auto-seed lost | HIGH | Step 7 — deferred + documented (only consumed by deferred invitation validation) |
| F2 `GetUsers` repo method + Person join | HIGH | Step 6 — added `GetAllWithPersonAsync`; Step 7 — Person-`Include` note |
| F3 Google name → First/Last + DisplayName parity | MED | Domain model — `given_name`/`family_name` mapping; `DisplayName` = `Person.FullName` |
| F4 Companion DTO/validator files | MED | Step 7 — companion command/query files listed |
| F5 No second-login gate test | MED | Step 9 — added gate item 3b |
| F6 401 vs 403 for deactivated | LOW | Step 7 — 401 for both (match current Fokus) |

**Status: Approved** for implementation.

## Open Questions

None — all decisions resolved this session.
