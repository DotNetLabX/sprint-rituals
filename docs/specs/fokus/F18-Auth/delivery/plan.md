# Authentication & Access Control

**Feature Spec:** `docs/features/Auth/spec.md`

## Context

Fokus currently runs with no authentication — all endpoints use `[AllowAnonymous]`, and every page is accessible to anyone who can reach the server. As Fokus expands to serve multiple managers, it needs authentication (who are you?) and authorization (what can you do?) to protect sensitive sprint and developer analytics.

This plan adds Google OAuth with cookie-based sessions, an invitation system for onboarding new users, two roles (Admin/Manager) for controlling who can modify settings, and an IsActive enforcement layer that blocks deactivated users on every request. The frontend gains a login page, an invite acceptance page, route guards, and role-based UI restrictions on Settings.

**Services impacted:** Fokus (single service). Backend adds two domain entities (AppUser, Invitation), extends AppSettings with CompanyDomain, adds Google OAuth middleware, adds 8 new API endpoints, adds an active-user enforcement pre-processor, and removes `[AllowAnonymous]` from all existing endpoints. Frontend adds login page, invite page, auth store, route guards, and Settings read-only mode for Managers.

## Scope

**In scope:**
- AppUser entity (auto-increment int PK) with GoogleId, Email, DisplayName, AvatarUrl, Role, CreatedAt, LastLoginAt, IsActive
- Invitation entity (auto-increment int PK) with state machine (Pending, Accepted, Expired, Revoked)
- CompanyDomain property on AppSettings (captured during bootstrap)
- Google OAuth via ASP.NET Core `AddAuthentication().AddCookie().AddGoogle()`
- Custom post-authentication callback that handles bootstrap, invitation acceptance, and regular login
- 8 new endpoints: GET /api/auth/me, POST /api/auth/logout, GET /api/users, PUT /api/users/{id}/role, PUT /api/users/{id}/status, POST /api/invitations, GET /api/invitations, DELETE /api/invitations/{id}, GET /api/invitations/{token}/validate
- IsActive enforcement pre-processor on every authenticated request
- Remove `[AllowAnonymous]` from all 26 existing endpoints
- Frontend: LoginView, InviteView, auth store, route guards, 401 interceptor, Settings read-only for Managers, User Management section in Settings for Admins
- Cookie security: SameSite=Lax, persistent, JSON content-type for implicit CSRF

**Explicitly out of scope:**
- Automated email delivery (invite links are generated, not emailed)
- SAML / enterprise SSO
- SCIM provisioning
- Fine-grained data visibility (per-team scoping)
- Audit logging
- Multi-tenancy
- Password-based login
- Self-registration

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | domain-patterns | Follow | AppUser entity (Entity\<int\>), Role as string property, static factory for bootstrap creation | |
| 2 | domain-patterns | Follow | Invitation entity (Entity\<int\>), state machine (Pending/Accepted/Expired/Revoked), behavior methods for Accept/Revoke/IsExpired | |
| 3 | persistence-patterns | Follow | AppUserConfiguration, InvitationConfiguration, FokusDbContext DbSets, AppSettings.CompanyDomain column, migration | |
| 4 | (none) | -- | Google OAuth middleware setup, cookie auth config, custom callback handler | Log: no auth-setup skill |
| 5 | create-feature | Follow | POST /api/auth/callback-handler (internal), GET /api/auth/me, POST /api/auth/logout | |
| 6 | create-feature | Follow | GET /api/users, PUT /api/users/{id}/role, PUT /api/users/{id}/status with validators | |
| 7 | create-feature | Follow | POST /api/invitations, GET /api/invitations, DELETE /api/invitations/{id}, GET /api/invitations/{token}/validate | |
| 8 | (none) | -- | IsActive enforcement pre-processor, remove AllowAnonymous from all existing endpoints | Log: no pre-processor skill |
| 9 | create-vue-feature | Follow | Auth types, auth API module, auth store, route definitions | |
| 10 | vue-component-architecture | Follow | LoginView, InviteView (outside AppShell), layout routing in App.vue | |
| 11 | vue-component-architecture, pinia-patterns | Follow | UserManagement section in SettingsView, invitation management, user table | |
| 12 | (none) | -- | Settings read-only mode for Managers, 401 interceptor in API client, route guard | Log: no auth-frontend skill |

## Domain Model Changes

**New entity: AppUser**
- `Entity<int>` (auto-increment PK)
- `GoogleId` (string, unique) -- Google `sub` claim
- `Email` (string, unique) -- case-insensitive, company domain enforced
- `DisplayName` (string) -- from Google profile
- `AvatarUrl` (string, nullable) -- Google profile picture
- `Role` (string, default "Manager") -- "Admin" or "Manager"
- `CreatedAt` (DateTime)
- `LastLoginAt` (DateTime, nullable)
- `IsActive` (bool, default true)
- Static factory: `CreateBootstrapAdmin(googleId, email, displayName, avatarUrl)` -- sets Role="Admin", CreatedAt=now
- Static factory: `CreateFromInvitation(googleId, email, displayName, avatarUrl, role)` -- sets role from invitation
- Behavior: `RecordLogin(displayName, avatarUrl)` -- updates LastLoginAt, syncs display name and avatar from Google

**New entity: Invitation**
- `Entity<int>` (auto-increment PK)
- `Email` (string) -- invited email, company domain enforced
- `Role` (string, default "Manager")
- `InvitedByUserId` (int, FK to AppUser)
- `Token` (string, unique) -- URL-safe random token
- `CreatedAt` (DateTime)
- `ExpiresAt` (DateTime) -- CreatedAt + 7 days
- `AcceptedAt` (DateTime, nullable)
- `Status` (string) -- "Pending", "Accepted", "Expired", "Revoked"
- Behavior: `Accept(DateTime now)` -- sets Status="Accepted", AcceptedAt=now. Guard: must be Pending and not expired.
- Behavior: `Revoke()` -- sets Status="Revoked". Guard: must be Pending.
- Computed: `IsExpired` -- `Status == "Pending" && DateTime.UtcNow > ExpiresAt`
- Static factory: `Create(email, role, invitedByUserId)` -- generates token, sets ExpiresAt = CreatedAt + 7 days

**Modified entity: AppSettings**
- Add `CompanyDomain` (string, nullable) -- captured from first user's email during bootstrap. Not user-editable after set.

## Data Model Changes

**New table: AppUsers**
- `Id` (INTEGER, PK, auto-increment)
- `GoogleId` (TEXT, not null, unique index)
- `Email` (TEXT, not null, unique index)
- `DisplayName` (TEXT, not null, max length 256)
- `AvatarUrl` (TEXT, nullable)
- `Role` (TEXT, not null, default "Manager", max length 32)
- `CreatedAt` (TEXT, not null) -- SQLite stores DateTime as TEXT
- `LastLoginAt` (TEXT, nullable)
- `IsActive` (INTEGER, not null, default 1) -- SQLite bool

**New table: Invitations**
- `Id` (INTEGER, PK, auto-increment)
- `Email` (TEXT, not null, max length 256)
- `Role` (TEXT, not null, default "Manager", max length 32)
- `InvitedByUserId` (INTEGER, not null, FK to AppUsers.Id)
- `Token` (TEXT, not null, unique index, max length 128)
- `CreatedAt` (TEXT, not null)
- `ExpiresAt` (TEXT, not null)
- `AcceptedAt` (TEXT, nullable)
- `Status` (TEXT, not null, default "Pending", max length 32)

**Modified table: AppSettings**
- Add column `CompanyDomain` (TEXT, nullable)

**Indexes:**
- `IX_AppUsers_GoogleId` (unique) -- lookup during login
- `IX_AppUsers_Email` (unique) -- duplicate check during invitation
- `IX_Invitations_Token` (unique) -- lookup during invite acceptance
- `IX_Invitations_Email` -- check for existing pending invitation

## Implementation Steps

### Step 1: Create AppUser domain entity

**What:** Create the AppUser entity with properties, static factories for bootstrap and invitation-based creation, and a behavior method for recording logins.

**Follow:** `domain-patterns`

**Files to create:**
- `src/Services/Fokus/Fokus.Domain/Auth/AppUser.cs` -- Entity definition (properties, partial class data side)
- `src/Services/Fokus/Fokus.Domain/Auth/Behaviors/AppUser.cs` -- Static factories (`CreateBootstrapAdmin`, `CreateFromInvitation`) and `RecordLogin` behavior

**Entity details:**
- Extends `Entity<int>`
- Properties as listed in Domain Model Changes
- `CreateBootstrapAdmin(string googleId, string email, string displayName, string? avatarUrl)` -- returns new AppUser with Role="Admin", CreatedAt=DateTime.UtcNow, IsActive=true
- `CreateFromInvitation(string googleId, string email, string displayName, string? avatarUrl, string role)` -- returns new AppUser with the given role, CreatedAt=DateTime.UtcNow, IsActive=true
- `RecordLogin(string displayName, string? avatarUrl)` -- updates LastLoginAt=DateTime.UtcNow, DisplayName=displayName, AvatarUrl=avatarUrl (Google profile sync per BR-8)

**Pattern reference:** `src/Services/Fokus/Fokus.Domain/Developer/Developer.cs` for Entity\<T\> usage, `src/Services/Fokus/Fokus.Domain/Sprint/Behaviors/Sprint.cs` for partial class behavior split.

**Dependencies:** None.

### Step 2: Create Invitation domain entity

**What:** Create the Invitation entity with state machine behavior (Pending/Accepted/Expired/Revoked), static factory for creation, and guard methods.

**Follow:** `domain-patterns`

**Files to create:**
- `src/Services/Fokus/Fokus.Domain/Auth/Invitation.cs` -- Entity definition (properties)
- `src/Services/Fokus/Fokus.Domain/Auth/Behaviors/Invitation.cs` -- Static factory `Create`, behavior methods `Accept`, `Revoke`, computed `IsExpired`

**Entity details:**
- Extends `Entity<int>`
- Properties as listed in Domain Model Changes
- `Create(string email, string role, int invitedByUserId)` -- generates Token via `Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))` converted to URL-safe format, sets CreatedAt=DateTime.UtcNow, ExpiresAt=CreatedAt+7 days, Status="Pending"
- `Accept(DateTime now)` -- Guard: Status must be "Pending" and now <= ExpiresAt. Sets Status="Accepted", AcceptedAt=now.
- `Revoke()` -- Guard: Status must be "Pending". Sets Status="Revoked".
- `IsExpired` (computed property) -- `Status == "Pending" && DateTime.UtcNow > ExpiresAt`

**Dependencies:** None.

### Step 3: Persistence layer -- EF configs, repositories, migration

**What:** Create EF Core configurations for AppUser and Invitation, add DbSet properties to FokusDbContext, add CompanyDomain to AppSettings, create repositories, and generate a migration.

**Follow:** `persistence-patterns`

**Files to create:**
- `src/Services/Fokus/Fokus.Persistence/Configurations/AppUserConfiguration.cs` -- configure unique indexes on GoogleId and Email, max lengths, default values
- `src/Services/Fokus/Fokus.Persistence/Configurations/InvitationConfiguration.cs` -- configure unique index on Token, index on Email, FK to AppUser (InvitedByUserId), max lengths, default values
- `src/Services/Fokus/Fokus.Persistence/Repositories/AppUserRepository.cs` -- concrete class. Methods: `GetByGoogleIdAsync`, `GetByEmailAsync`, `GetByIdAsync`, `GetAllAsync`, `CountAsync`, `CountActiveAdminsAsync`, `AddAsync`, `SaveChangesAsync`
- `src/Services/Fokus/Fokus.Persistence/Repositories/InvitationRepository.cs` -- concrete class. Methods: `GetByTokenAsync`, `GetPendingByEmailAsync`, `GetAllAsync`, `GetByIdAsync`, `AddAsync`, `SaveChangesAsync`

**Files to modify:**
- `src/Services/Fokus/Fokus.Persistence/FokusDbContext.cs` -- add `DbSet<AppUser> AppUsers` and `DbSet<Invitation> Invitations`
- `src/Services/Fokus/Fokus.Domain/Settings/AppSettings.cs` -- add `CompanyDomain` (string?, nullable, default null) property. Update `CreateDefault()` to include `CompanyDomain = null`.
- `src/Services/Fokus/Fokus.Persistence/Configurations/AppSettingsConfiguration.cs` -- add `CompanyDomain` column config (nullable TEXT)
- `src/Services/Fokus/Fokus.Persistence/DependencyInjection.cs` -- register `AppUserRepository` and `InvitationRepository` as scoped

**Migration:** Generate via `dotnet ef migrations add AddAuth -p src/Services/Fokus/Fokus.Persistence -s src/Services/Fokus/Fokus.API`

**Dependencies:** Steps 1, 2.

### Step 4: Configure Google OAuth and cookie authentication

**What:** Add Google OAuth authentication to the ASP.NET Core pipeline. Configure cookie authentication with SameSite=Lax. Set up the Google OAuth callback to redirect to a custom processing endpoint. Add a `ForbiddenException` to the exception hierarchy for 403 responses.

**No skill -- auth middleware configuration.**

**Files to create:**
- `src/Services/Fokus/Fokus.API/Auth/AuthConfiguration.cs` -- extension method `AddFokusAuth(IServiceCollection, IConfiguration)` that configures:
  - `AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)`
  - `.AddCookie(options => { options.Cookie.SameSite = SameSiteMode.Lax; options.Cookie.HttpOnly = true; options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; options.ExpireTimeSpan = TimeSpan.FromDays(30); options.SlidingExpiration = true; options.LoginPath = "/auth/login"; options.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = 401; return Task.CompletedTask; }; })`
  - `.AddGoogle(options => { options.ClientId = config["Google:ClientId"]; options.ClientSecret = config["Google:ClientSecret"]; options.CallbackPath = "/auth/callback"; options.SaveTokens = false; })` -- scopes default to openid, profile, email
- `src/Services/Fokus/Fokus.API/Auth/AuthMiddleware.cs` -- extension method `UseFokusAuth(WebApplication)` that calls `app.UseAuthentication(); app.UseAuthorization();`
- `src/BuildingBlocks/Blocks.Exceptions/ForbiddenException.cs` -- extends `HttpException`, status code 403

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Fokus.API.csproj` -- add `<PackageReference Include="Microsoft.AspNetCore.Authentication.Google" />` 
- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` -- call `services.AddFokusAuth(configuration)` in `AddFokusServices`
- `src/Services/Fokus/Fokus.API/Program.cs` -- call `app.UseFokusAuth()` before `app.UseFokusMiddleware()` (auth must run before endpoint routing)
- `src/BuildingBlocks/Blocks.AspNetCore/Middlewares/GlobalExceptionMiddleware.cs` -- add `ForbiddenException e => (403, e.Message)` to the `MapStatusCode` switch

**Configuration (User Secrets for dev):**
```
Google:ClientId = (from Google Cloud Console)
Google:ClientSecret = (from Google Cloud Console)
```

**Cookie auth behavior:** When an unauthenticated request hits an endpoint that requires auth, the cookie middleware returns 401 (not a redirect) because we override `OnRedirectToLogin`. The SPA handles 401 by redirecting to the login page client-side.

**Auth flow routes (middleware-handled):**
- `GET /auth/login` -- triggers Google OAuth challenge (implemented in Step 5)
- `GET /auth/callback` -- Google OAuth callback (handled by ASP.NET Core Google middleware, triggers `OnTicketReceived`)

**Dependencies:** Step 3 (repositories needed for callback processing).

### Step 5: Create auth endpoints (me, logout, login trigger, callback processing)

**What:** Create the authentication flow endpoints: a login trigger that initiates the Google OAuth challenge, a callback processor that handles bootstrap/invitation/regular login logic, a `/api/auth/me` endpoint, and a `/api/auth/logout` endpoint.

**Follow:** `create-feature`

**Files to create:**
- `src/Services/Fokus/Fokus.API/Features/Auth/Login/LoginEndpoint.cs` -- `GET /auth/login`. AllowAnonymous. Accepts optional `?invite={token}` query parameter. Issues a Google OAuth challenge, storing the invite token in `AuthenticationProperties.Items["invite_token"]` for the callback to read. If no invite token, issues a plain challenge.
- `src/Services/Fokus/Fokus.API/Features/Auth/Callback/AuthCallbackEndpoint.cs` -- `GET /auth/callback-process`. This is the endpoint that Google redirects to after authentication (set as the redirect URI in the Google OAuth config's `OnTicketReceived` or via `options.Events.OnCreatingTicket`). Alternatively, configure `options.SignInScheme` to the cookie scheme and use an `OnTicketReceived` event to run the business logic.

  **Recommended approach:** Use `GoogleOptions.Events.OnTicketReceived` in `AuthConfiguration.cs` to intercept the Google callback, run the business logic (below), and create the cookie claims. This avoids a separate endpoint and keeps auth flow in middleware where it belongs. The logic:
  1. Extract Google claims: `sub` (GoogleId), `email`, `name` (DisplayName), `picture` (AvatarUrl).
  2. Extract `invite_token` from `context.Properties.Items` if present.
  3. **Invitation path** (invite_token present):
     - Look up invitation by token via `InvitationRepository.GetByTokenAsync`.
     - Validate: exists, Status=="Pending", not expired. If invalid, redirect to `/invite/error?reason={expired|revoked|used|invalid}`.
     - Validate: authenticated email matches invitation email (case-insensitive). If mismatch, redirect to `/invite/error?reason=email-mismatch`.
     - Create AppUser via `AppUser.CreateFromInvitation(...)`.
     - Accept invitation via `invitation.Accept(DateTime.UtcNow)`.
     - Save both via repositories.
  4. **Bootstrap path** (no invite_token, zero AppUsers):
     - `AppUserRepository.CountAsync() == 0`.
     - Create AppUser via `AppUser.CreateBootstrapAdmin(...)`.
     - Extract domain from email (`email.Split('@')[1]`), set `AppSettings.CompanyDomain`.
     - Save via repositories.
  5. **Regular login path** (no invite_token, AppUsers exist):
     - Look up user by GoogleId via `AppUserRepository.GetByGoogleIdAsync`.
     - If not found: redirect to `/login?error=no-account`.
     - If found but not active: redirect to `/login?error=deactivated`.
     - If found and active: call `user.RecordLogin(displayName, avatarUrl)`, save.
  6. **Create claims principal** with: `ClaimTypes.NameIdentifier = user.Id`, `ClaimTypes.Email = user.Email`, `ClaimTypes.Name = user.DisplayName`, `ClaimTypes.Role = user.Role`, custom `"avatar_url" = user.AvatarUrl`.
  7. **Sign in** via `context.HttpContext.SignInAsync(...)` with the cookie scheme.
  8. **Redirect** to `/` (dashboard).

- `src/Services/Fokus/Fokus.API/Features/Auth/GetMe/GetMeEndpoint.cs` -- `GET /api/auth/me`. Requires authentication (default -- no AllowAnonymous). Reads claims from `HttpContext.User`. Returns `{ id, email, displayName, avatarUrl, role }`.
- `src/Services/Fokus/Fokus.API/Features/Auth/GetMe/GetMeQuery.cs` -- response type `MeResponse { int Id, string Email, string DisplayName, string? AvatarUrl, string Role }`.
- `src/Services/Fokus/Fokus.API/Features/Auth/Logout/LogoutEndpoint.cs` -- `POST /api/auth/logout`. Requires authentication. Calls `HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme)`. Returns 204.

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Auth/AuthConfiguration.cs` -- add the `OnTicketReceived` event handler with the business logic above. The handler needs `AppUserRepository`, `InvitationRepository`, `AppSettingsRepository` injected via `context.HttpContext.RequestServices`.

**Dependencies:** Steps 3, 4.

### Step 6: Create user management endpoints

**What:** Create Admin-only endpoints for listing users, changing roles, and toggling active status. Include last-Admin protection guard.

**Follow:** `create-feature`

**Files to create:**
- `src/Services/Fokus/Fokus.API/Features/Auth/GetUsers/GetUsersEndpoint.cs` -- `GET /api/users`. Tags: "Users". Role gate: Admin only (`Roles("Admin")` via endpoint config). Returns list of all AppUsers with id, email, displayName, avatarUrl, role, lastLoginAt, isActive.
- `src/Services/Fokus/Fokus.API/Features/Auth/GetUsers/GetUsersQuery.cs` -- response types.
- `src/Services/Fokus/Fokus.API/Features/Auth/UpdateRole/UpdateRoleEndpoint.cs` -- `PUT /api/users/{id}/role`. Tags: "Users". Admin only. Validates role is "Admin" or "Manager". Enforces last-Admin protection: if demoting from Admin, check `CountActiveAdminsAsync > 1`. Returns updated user.
- `src/Services/Fokus/Fokus.API/Features/Auth/UpdateRole/UpdateRoleCommand.cs` -- request `{ int Id (route), string Role }`, response `UserResponse`, validator.
- `src/Services/Fokus/Fokus.API/Features/Auth/UpdateStatus/UpdateStatusEndpoint.cs` -- `PUT /api/users/{id}/status`. Tags: "Users". Admin only. Enforces last-Admin protection: if deactivating an Admin, check `CountActiveAdminsAsync > 1`. Returns updated user.
- `src/Services/Fokus/Fokus.API/Features/Auth/UpdateStatus/UpdateStatusCommand.cs` -- request `{ int Id (route), bool IsActive }`, response `UserResponse`, validator.

**Shared response type `UserResponse`:** `{ int Id, string Email, string DisplayName, string? AvatarUrl, string Role, DateTime? LastLoginAt, bool IsActive }`

**Validation:**
- UpdateRole: `Role` must be `NotEmpty()` and must be one of "Admin", "Manager".
- UpdateStatus: `IsActive` is required (bool).
- Both: `Id` must be > 0.

**Last-Admin protection (BR-5):**
- Before demoting an Admin to Manager: `appUserRepository.CountActiveAdminsAsync()` must return > 1. If not, throw `BadRequestException("Cannot demote the last active Admin.")`.
- Before deactivating an Admin: same check. If not, throw `BadRequestException("Cannot deactivate the last active Admin.")`.

**Dependencies:** Steps 3, 4.

### Step 7: Create invitation management endpoints

**What:** Create Admin-only endpoints for creating, listing, revoking invitations, and a public endpoint for validating invite tokens.

**Follow:** `create-feature`

**Files to create:**
- `src/Services/Fokus/Fokus.API/Features/Auth/CreateInvitation/CreateInvitationEndpoint.cs` -- `POST /api/invitations`. Tags: "Invitations". Admin only. Validates email belongs to company domain (from AppSettings.CompanyDomain). Checks for existing AppUser or pending Invitation with same email (409). Creates Invitation via factory. Returns `{ id, email, role, inviteLink, expiresAt }` with status 201. The `inviteLink` is computed as `{request.Scheme}://{request.Host}/invite/{token}`.
- `src/Services/Fokus/Fokus.API/Features/Auth/CreateInvitation/CreateInvitationCommand.cs` -- request `{ string Email, string? Role }` (Role defaults to "Manager" if null), response type, validator.
- `src/Services/Fokus/Fokus.API/Features/Auth/GetInvitations/GetInvitationsEndpoint.cs` -- `GET /api/invitations`. Tags: "Invitations". Admin only. Returns list of all invitations with id, email, role, status, createdAt, expiresAt.
- `src/Services/Fokus/Fokus.API/Features/Auth/GetInvitations/GetInvitationsQuery.cs` -- response types.
- `src/Services/Fokus/Fokus.API/Features/Auth/RevokeInvitation/RevokeInvitationEndpoint.cs` -- `DELETE /api/invitations/{id}`. Tags: "Invitations". Admin only. Loads invitation, calls `Revoke()`, saves. Returns 204. 404 if not found.
- `src/Services/Fokus/Fokus.API/Features/Auth/RevokeInvitation/RevokeInvitationCommand.cs` -- request with Id from route.
- `src/Services/Fokus/Fokus.API/Features/Auth/ValidateInvitation/ValidateInvitationEndpoint.cs` -- `GET /api/invitations/{token}/validate`. AllowAnonymous. Looks up invitation by token. Returns `{ valid: true, email }` if Pending and not expired. Returns `{ valid: false, error: "expired"|"revoked"|"used"|"not-found" }` otherwise.
- `src/Services/Fokus/Fokus.API/Features/Auth/ValidateInvitation/ValidateInvitationQuery.cs` -- response types.

**Validation (CreateInvitation):**
- `Email`: `NotEmpty()`, must be valid email format
- `Role` when present: must be one of "Admin", "Manager"

**Dependencies:** Steps 3, 4.

### Step 8: Enforce authentication on all existing endpoints

**What:** Remove `[AllowAnonymous]` from all 26 existing endpoints and add an IsActive enforcement pre-processor that checks the user's active status on every authenticated request.

**No skill -- cross-cutting auth enforcement.**

**Approach:** FastEndpoints supports global endpoint configuration. Instead of editing 26 files to remove `[AllowAnonymous]`, configure the FastEndpoints default to require authentication:

```csharp
app.UseFastEndpoints(c =>
{
    c.Endpoints.Configurator = ep =>
    {
        // All endpoints require authentication by default
        // Individual endpoints opt-out with AllowAnonymous()
    };
});
```

However, FastEndpoints already requires auth by default when ASP.NET Core authentication is configured -- `[AllowAnonymous]` was added explicitly to bypass it. So the actual work is:
1. Remove `[AllowAnonymous]` from all 26 existing endpoint files.
2. Add `[AllowAnonymous]` only to endpoints that must be anonymous: `LoginEndpoint`, `ValidateInvitationEndpoint`.
3. Create an `ActiveUserPreProcessor` that runs on every authenticated request.

**Files to create:**
- `src/Services/Fokus/Fokus.API/Auth/ActiveUserPreProcessor.cs` -- implements `IGlobalPreProcessor`. On every request where `HttpContext.User.Identity?.IsAuthenticated == true`: extract user ID from claims, look up AppUser by ID via `AppUserRepository`, check `IsActive`. If not active, return 401 with message "Your account has been deactivated." Skip for anonymous endpoints.

**Files to modify (remove `[AllowAnonymous]`):**
- `src/Services/Fokus/Fokus.API/Features/Settings/GetSettings/GetSettingsEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveSettings/SaveSettingsEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Settings/GetCycleTimeBoundaries/GetCycleTimeBoundariesEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveCycleTimeBoundaries/SaveCycleTimeBoundariesEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Settings/GetExcludedStatuses/GetExcludedStatusesEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveExcludedStatuses/SaveExcludedStatusesEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Settings/DetectWorkflowStages/DetectWorkflowStagesEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetSprintSummary/GetSprintSummaryEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetDeveloperThroughput/GetDeveloperThroughputEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetScopeChange/GetScopeChangeEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetCarryOver/GetCarryOverEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetBugRatio/GetBugRatioEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetEpicProgress/GetEpicProgressEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetCycleTime/GetCycleTimeEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Sync/GetBoards/GetBoardsEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Sync/GetJiraSprints/GetJiraSprintsEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncSprints/SyncSprintsEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncBacklogSprints/SyncBacklogSprintsEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Sync/GetStatuses/GetStatusesEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Sprints/GetClosedSprints/GetClosedSprintsEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Developers/GetDevelopers/GetDevelopersEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Developers/GetSubTeams/GetSubTeamsEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Developers/GetCapacity/GetCapacityEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Developers/SetCapacity/SetCapacityEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Team/GetTeamRoster/GetTeamRosterEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Team/UpdateTeamConfig/UpdateTeamConfigEndpoint.cs`

**Additionally, add Admin-only role gate to Settings write endpoints:**
- `SaveSettingsEndpoint` -- add `Roles("Admin")` in Configure()
- `SaveCycleTimeBoundariesEndpoint` -- add `Roles("Admin")` in Configure()
- `SaveExcludedStatusesEndpoint` -- add `Roles("Admin")` in Configure()
- `SyncSprintsEndpoint` -- add `Roles("Admin")` in Configure()
- `SyncBacklogSprintsEndpoint` -- add `Roles("Admin")` in Configure()
- `UpdateTeamConfigEndpoint` -- add `Roles("Admin")` in Configure()
- `SetCapacityEndpoint` -- add `Roles("Admin")` in Configure()

**Files to modify (DI registration):**
- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` -- register `ActiveUserPreProcessor` in the FastEndpoints configuration

**Performance:** The `ActiveUserPreProcessor` hits the database on every authenticated request. For a small-scale app like Fokus this is acceptable. If it becomes a bottleneck, cache the active status with a short TTL.

**Dependencies:** Steps 4, 5, 6, 7.

### Step 9: Frontend auth infrastructure (types, API, store, router)

**What:** Add TypeScript types for auth entities, create the auth API module, create the auth Pinia store, update the router with auth-related routes and a navigation guard, and add a 401 interceptor to the API client.

**Follow:** `create-vue-feature`, `pinia-patterns`

**Files to modify:**
- `client/src/types/index.ts` -- add types:
  ```
  AuthUser { id: number, email: string, displayName: string, avatarUrl: string | null, role: 'Admin' | 'Manager' }
  UserEntry { id: number, email: string, displayName: string, avatarUrl: string | null, role: string, lastLoginAt: string | null, isActive: boolean }
  InvitationEntry { id: number, email: string, role: string, status: string, createdAt: string, expiresAt: string }
  CreateInvitationRequest { email: string, role?: string }
  CreateInvitationResponse { id: number, email: string, role: string, inviteLink: string, expiresAt: string }
  InviteValidation { valid: boolean, email?: string, error?: string }
  ```
- `client/src/api/client.ts` -- add 401 interceptor: if response status is 401, redirect to `/login` via `window.location.href = '/login'` (hard redirect to clear SPA state). Do NOT intercept 401 for `/api/auth/me` calls (the me check is used to detect unauthenticated state).
- `client/src/router.ts` -- add routes:
  - `/login` -- `LoginView.vue`, meta: `{ layout: 'blank', public: true }`
  - `/invite/:token` -- `InviteView.vue`, meta: `{ layout: 'blank', public: true }`
  - `/invite/error` -- `InviteErrorView.vue`, meta: `{ layout: 'blank', public: true }`
  - Add `beforeEach` navigation guard: if route is not `public` and auth store has no user, call `authStore.fetchMe()`. If fetch fails (401), redirect to `/login`.

**Files to create:**
- `client/src/api/auth.ts` -- functions:
  - `getMe(): Promise<AuthUser>` -- `GET /api/auth/me`
  - `logout(): Promise<void>` -- `POST /api/auth/logout`
  - `getUsers(): Promise<UserEntry[]>` -- `GET /api/users`
  - `updateUserRole(id: number, role: string): Promise<UserEntry>` -- `PUT /api/users/{id}/role`
  - `updateUserStatus(id: number, isActive: boolean): Promise<UserEntry>` -- `PUT /api/users/{id}/status`
  - `createInvitation(req: CreateInvitationRequest): Promise<CreateInvitationResponse>` -- `POST /api/invitations`
  - `getInvitations(): Promise<InvitationEntry[]>` -- `GET /api/invitations`
  - `revokeInvitation(id: number): Promise<void>` -- `DELETE /api/invitations/{id}`
  - `validateInvitation(token: string): Promise<InviteValidation>` -- `GET /api/invitations/{token}/validate`
- `client/src/stores/authStore.ts` -- Pinia setup store:
  - **State:** `user: AuthUser | null`, `loading: boolean`, `initialized: boolean`
  - **Getters:** `isAuthenticated: boolean`, `isAdmin: boolean`, `displayName: string`
  - **Actions:**
    - `fetchMe()` -- calls `getMe()`, sets user on success, sets user=null on 401 error. Sets initialized=true.
    - `logout()` -- calls `logout()`, sets user=null, redirects to `/login`.

**Dependencies:** Steps 5, 6, 7.

### Step 10: Frontend login and invite pages

**What:** Create the LoginView (outside AppShell, shows "Sign in with Google" button), InviteView (validates token then redirects to Google auth), InviteErrorView (displays error messages), and update App.vue to support a blank layout for auth pages.

**Follow:** `vue-component-architecture`

**Files to create:**
- `client/src/views/LoginView.vue` -- full-page centered layout (no sidebar, no header). Fokus logo, "Sign in with Google" button (links to `/auth/login`), error messages from query params (`?error=no-account` -> "Access denied. You need an invitation to use Fokus.", `?error=deactivated` -> "Your account has been deactivated. Contact your administrator."). Dark theme background consistent with app.
- `client/src/views/InviteView.vue` -- on mount, extracts `token` from route params, calls `validateInvitation(token)`. If valid, shows "You've been invited as {role}" with a "Sign in with Google to accept" button (links to `/auth/login?invite={token}`). If invalid, redirects to `/invite/error?reason={error}`.
- `client/src/views/InviteErrorView.vue` -- reads `reason` from query params. Displays appropriate message: "expired" -> "This invitation has expired.", "revoked" -> "This invitation has been revoked.", "used" -> "This invitation has already been used.", "email-mismatch" -> "Sign in with the email this invitation was sent to.", "invalid" -> "Invalid invitation link."

**Files to modify:**
- `client/src/App.vue` -- change layout to conditionally render AppShell based on route meta:
  ```vue
  <template>
    <template v-if="route.meta.layout === 'blank'">
      <router-view />
    </template>
    <template v-else>
      <AppShell>
        <router-view />
      </AppShell>
    </template>
  </template>
  ```

**Dependencies:** Step 9.

### Step 11: User Management section in Settings (Admin only)

**What:** Add a "User Management" section to SettingsView visible only to Admins. Contains: users table (name, email, role, last login, status with inline controls), pending invitations list, and invite form. Managers see Settings in read-only mode with a visual indicator.

**Follow:** `vue-component-architecture`, `pinia-patterns`

**Files to modify:**
- `client/src/views/SettingsView.vue` -- add at the top of the page:
  1. **Read-only banner for Managers:** If `authStore.user?.role === 'Manager'`, show a banner: "View only -- contact an Admin to make changes." Disable all form inputs and buttons.
  2. **User Management section (Admin only):** Conditionally rendered when `authStore.isAdmin`. Contains:
     - **Invite form:** Email input + role dropdown (Manager default, Admin option) + "Send Invite" button. On submit, calls `createInvitation`. Displays the generated invite link with a "Copy" button.
     - **Pending invitations table:** Shows all pending invitations with email, role, expiry, and a "Revoke" button. Fetches from `getInvitations()` on mount.
     - **Users table:** Shows all users with displayName, email, role dropdown, last login, active toggle. Role change calls `updateUserRole`. Active toggle calls `updateUserStatus`. Last-Admin protection errors displayed inline.
  3. **Sidebar User Management visibility:** The Settings nav item stays visible to all. Within Settings, the User Management section is conditionally rendered for Admin only (spec Flow 7: "User Management section is not visible to Managers").

**Files to modify:**
- `client/src/components/AppSidebar.vue` -- add user avatar and display name at the bottom of the sidebar (from authStore), with a logout button. This provides visual confirmation of who is logged in.

**Dependencies:** Steps 9, 10.

### Step 12: Settings read-only enforcement and 401 handling polish

**What:** Complete the frontend auth integration: enforce read-only mode on all Settings form elements for Managers, ensure 401 interceptor works correctly for all API calls, and add the logout action to the sidebar.

**No skill -- cross-cutting frontend enforcement.**

**Files to modify:**
- `client/src/views/SettingsView.vue` -- wrap all existing `<button>` elements and `<input>`/`<select>` elements with a `:disabled` binding that checks `!authStore.isAdmin`. This makes all settings controls read-only for Managers without restructuring the template. Add a subtle visual overlay or opacity change for the read-only state.
- `client/src/api/client.ts` -- finalize 401 handling: ensure the interceptor does NOT redirect for the initial `/api/auth/me` call (which is expected to 401 when not logged in). Use a flag or check the URL path.
- `client/src/stores/authStore.ts` -- ensure `fetchMe` swallows 401 errors gracefully (sets user=null, no redirect). The router guard handles the redirect.

**Dependencies:** Steps 9, 10, 11.

## Cross-Service Changes

None. Fokus is a single-service system.

## Migration Notes

```bash
# Add migration (from repo root)
dotnet ef migrations add AddAuth -p src/Services/Fokus/Fokus.Persistence -s src/Services/Fokus/Fokus.API

# Apply migration
dotnet ef database update -p src/Services/Fokus/Fokus.Persistence -s src/Services/Fokus/Fokus.API
```

The migration creates two new tables (AppUsers, Invitations) and adds one column (CompanyDomain) to AppSettings. No existing data is affected. No seed data needed -- the first user is created through the bootstrap flow.

**User Secrets setup:**
```bash
dotnet user-secrets set "Google:ClientId" "<value>" --project src/Services/Fokus/Fokus.API
dotnet user-secrets set "Google:ClientSecret" "<value>" --project src/Services/Fokus/Fokus.API
```

## Testing Strategy

**Backend -- Auth flow:**
- Unauthenticated `GET /api/settings` returns 401 (not 200 as before)
- `GET /api/auth/me` with valid cookie returns 200 with user profile
- `GET /api/auth/me` without cookie returns 401
- `POST /api/auth/logout` clears cookie, subsequent `/api/auth/me` returns 401
- Bootstrap: first Google login creates Admin, captures company domain
- Second login from non-invited user gets "Access denied"
- Deactivated user gets 401 on any authenticated endpoint

**Backend -- Invitations:**
- `POST /api/invitations` with valid company-domain email returns 201 with invite link
- `POST /api/invitations` with non-company-domain email returns 400
- `POST /api/invitations` with email that already has an account returns 409
- `POST /api/invitations` with email that has a pending invitation returns 409
- `GET /api/invitations/{token}/validate` with valid pending token returns `{ valid: true, email }`
- `GET /api/invitations/{token}/validate` with expired token returns `{ valid: false, error: "expired" }`
- `DELETE /api/invitations/{id}` revokes and subsequent validate returns "revoked"
- Manager calling `POST /api/invitations` returns 403

**Backend -- User management:**
- `GET /api/users` returns all users (Admin only, Manager gets 403)
- `PUT /api/users/{id}/role` with `{ role: "Admin" }` promotes user
- `PUT /api/users/{id}/role` demoting last Admin returns 400
- `PUT /api/users/{id}/status` deactivating last Admin returns 400
- `PUT /api/users/{id}/status` with `{ isActive: false }` deactivates user

**Frontend:**
- Unauthenticated user sees login page (not dashboard)
- Login page shows "Sign in with Google" button
- After Google auth, user lands on dashboard
- Invite link shows invitation details and "Sign in" button
- Expired/revoked invite links show appropriate error messages
- Settings page: Manager sees read-only controls with indicator banner
- Settings page: Admin sees User Management section
- Admin can create invitation and copy link
- Admin can change user roles and toggle active status
- Last-Admin operations show inline error
- Sidebar shows user avatar and logout button
- 401 on any API call redirects to login page

## Open Questions

None.
