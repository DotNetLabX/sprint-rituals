# Authentication & Access Control — Implementation

## Files Created

### Domain
- `src/Services/Fokus/Fokus.Domain/Auth/AppUser.cs` — AppUser entity with properties (GoogleId, Email, DisplayName, AvatarUrl, Role, CreatedAt, LastLoginAt, IsActive), extends Entity<int>
- `src/Services/Fokus/Fokus.Domain/Auth/Behaviors/AppUser.cs` — Static factories CreateBootstrapAdmin, CreateFromInvitation; behavior method RecordLogin
- `src/Services/Fokus/Fokus.Domain/Auth/Invitation.cs` — Invitation entity with state machine properties, IsExpired computed property, extends Entity<int>
- `src/Services/Fokus/Fokus.Domain/Auth/Behaviors/Invitation.cs` — Static factory Create (generates URL-safe base64 token), Accept (with guards), Revoke (with guard)

### Persistence
- `src/Services/Fokus/Fokus.Persistence/Configurations/AppUserConfiguration.cs` — EF config with unique indexes on GoogleId and Email, max lengths, auto-increment PK
- `src/Services/Fokus/Fokus.Persistence/Configurations/InvitationConfiguration.cs` — EF config with unique index on Token, index on Email, FK to AppUser with Restrict delete
- `src/Services/Fokus/Fokus.Persistence/Repositories/AppUserRepository.cs` — Concrete repository: GetByGoogleIdAsync, GetByEmailAsync, GetByIdAsync, GetAllAsync, CountAsync, CountActiveAdminsAsync, AddAsync, SaveChangesAsync
- `src/Services/Fokus/Fokus.Persistence/Repositories/InvitationRepository.cs` — Concrete repository: GetByTokenAsync, GetPendingByEmailAsync, GetAllAsync, GetByIdAsync, AddAsync, SaveChangesAsync

### API — Auth infrastructure
- `src/Services/Fokus/Fokus.API/Auth/AuthConfiguration.cs` — AddFokusAuth extension: cookie auth, Google OAuth, OnTicketReceived handler implementing bootstrap/invitation/regular login flows
- `src/Services/Fokus/Fokus.API/Auth/AuthMiddleware.cs` — UseFokusAuth extension calling UseAuthentication + UseAuthorization
- `src/Services/Fokus/Fokus.API/Auth/ActiveUserPreProcessor.cs` — IGlobalPreProcessor that checks IsActive on every authenticated request, returns 401 if deactivated

### API — Auth endpoints
- `src/Services/Fokus/Fokus.API/Features/Auth/Login/LoginEndpoint.cs` — GET /auth/login, AllowAnonymous, issues Google OAuth challenge with optional invite_token in properties
- `src/Services/Fokus/Fokus.API/Features/Auth/GetMe/GetMeEndpoint.cs` — GET /api/auth/me, reads claims from HttpContext.User
- `src/Services/Fokus/Fokus.API/Features/Auth/GetMe/GetMeQuery.cs` — MeResponse type
- `src/Services/Fokus/Fokus.API/Features/Auth/Logout/LogoutEndpoint.cs` — POST /api/auth/logout, signs out cookie, returns 204

### API — User management endpoints
- `src/Services/Fokus/Fokus.API/Features/Auth/GetUsers/GetUsersEndpoint.cs` — GET /api/users, Admin only
- `src/Services/Fokus/Fokus.API/Features/Auth/GetUsers/GetUsersQuery.cs` — UserResponse type
- `src/Services/Fokus/Fokus.API/Features/Auth/UpdateRole/UpdateRoleEndpoint.cs` — PUT /api/users/{id}/role, Admin only, last-Admin protection
- `src/Services/Fokus/Fokus.API/Features/Auth/UpdateRole/UpdateRoleCommand.cs` — UpdateRoleRequest + validator
- `src/Services/Fokus/Fokus.API/Features/Auth/UpdateStatus/UpdateStatusEndpoint.cs` — PUT /api/users/{id}/status, Admin only, last-Admin protection
- `src/Services/Fokus/Fokus.API/Features/Auth/UpdateStatus/UpdateStatusCommand.cs` — UpdateStatusRequest + validator

### API — Invitation endpoints
- `src/Services/Fokus/Fokus.API/Features/Auth/CreateInvitation/CreateInvitationEndpoint.cs` — POST /api/invitations, Admin only, validates company domain, checks for duplicate user/invitation (409)
- `src/Services/Fokus/Fokus.API/Features/Auth/CreateInvitation/CreateInvitationCommand.cs` — request + response + validator
- `src/Services/Fokus/Fokus.API/Features/Auth/GetInvitations/GetInvitationsEndpoint.cs` — GET /api/invitations, Admin only
- `src/Services/Fokus/Fokus.API/Features/Auth/GetInvitations/GetInvitationsQuery.cs` — InvitationResponse type
- `src/Services/Fokus/Fokus.API/Features/Auth/RevokeInvitation/RevokeInvitationEndpoint.cs` — DELETE /api/invitations/{id}, Admin only, calls domain Revoke()
- `src/Services/Fokus/Fokus.API/Features/Auth/RevokeInvitation/RevokeInvitationCommand.cs` — request type
- `src/Services/Fokus/Fokus.API/Features/Auth/ValidateInvitation/ValidateInvitationEndpoint.cs` — GET /api/invitations/{token}/validate, AllowAnonymous
- `src/Services/Fokus/Fokus.API/Features/Auth/ValidateInvitation/ValidateInvitationQuery.cs` — request + response types

### BuildingBlocks
- `src/BuildingBlocks/Blocks.Exceptions/ForbiddenException.cs` — 403 HttpException
- `src/BuildingBlocks/Blocks.Exceptions/ConflictException.cs` — 409 HttpException

### Frontend
- `client/src/api/auth.ts` — getMe, logout, getUsers, updateUserRole, updateUserStatus, createInvitation, getInvitations, revokeInvitation, validateInvitation
- `client/src/stores/authStore.ts` — Pinia setup store: user, loading, initialized, isAuthenticated, isAdmin, displayName, fetchMe, logout
- `client/src/views/LoginView.vue` — Full-page login with Google OAuth button, error message display
- `client/src/views/InviteView.vue` — Validates token on mount, shows invite details or redirects to error page
- `client/src/views/InviteErrorView.vue` — Displays reason-specific error messages for expired/revoked/used/email-mismatch/invalid

## Files Modified

- `src/Services/Fokus/Fokus.Domain/GlobalUsings.cs` — Added `global using Fokus.Domain.Auth`
- `src/Services/Fokus/Fokus.Domain/Settings/AppSettings.cs` — Added CompanyDomain (string?, nullable) property and updated CreateDefault()
- `src/Services/Fokus/Fokus.Persistence/GlobalUsings.cs` — Added `global using Fokus.Domain.Auth`
- `src/Services/Fokus/Fokus.Persistence/FokusDbContext.cs` — Added DbSet<AppUser> and DbSet<Invitation>
- `src/Services/Fokus/Fokus.Persistence/Configurations/AppSettingsConfiguration.cs` — Added CompanyDomain column config (nullable TEXT, max 256)
- `src/Services/Fokus/Fokus.Persistence/DependencyInjection.cs` — Registered AppUserRepository and InvitationRepository as scoped
- `src/Services/Fokus/Fokus.API/GlobalUsings.cs` — Added `global using Fokus.Domain.Auth`
- `src/Services/Fokus/Fokus.API/Fokus.API.csproj` — Added Microsoft.AspNetCore.Authentication.Google v10.0.7
- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` — Added AddFokusAuth, ActiveUserPreProcessor scoped registration, UseFastEndpoints with global pre-processor configurator
- `src/Services/Fokus/Fokus.API/Program.cs` — Added UseFokusAuth() call before UseFokusMiddleware()
- `src/BuildingBlocks/Blocks.AspNetCore/Middlewares/GlobalExceptionMiddleware.cs` — Added ForbiddenException (403) and ConflictException (409) to MapStatusCode switch
- `client/src/types/index.ts` — Added AuthUser, UserEntry, InvitationEntry, CreateInvitationRequest, CreateInvitationResponse, InviteValidation interfaces
- `client/src/api/client.ts` — Added 401 interceptor (hard redirect to /login, except /auth/me); added 204 handling
- `client/src/router.ts` — Added /login, /invite/:token, /invite/error routes with blank layout; added beforeEach navigation guard
- `client/src/App.vue` — Added conditional rendering based on route.meta.layout (blank vs AppShell)
- `client/src/views/SettingsView.vue` — Added authStore import, user management state and functions, Manager read-only banner, User Management section (Admin only) with invite form, pending invitations table, users table with role/status controls; isReadOnly computed; Save Settings disabled for Managers
- `client/src/components/AppSidebar.vue` — Added authStore import, user avatar display and logout button at sidebar bottom
- 26 existing endpoints — Removed `[AllowAnonymous]` attribute
- 7 write endpoints — Added `Configure()` method with `Roles("Admin")`

## EF Core Migration
- Migration `AddAuth` generated at `src/Services/Fokus/Fokus.Persistence/Migrations/` — creates AppUsers and Invitations tables, adds CompanyDomain column to AppSettings

## Key Decisions

- **OnTicketReceived in middleware, not a separate endpoint**: The plan's recommended approach was followed — all bootstrap/invitation/login logic runs inside the Google OAuth `OnTicketReceived` event handler in `AuthConfiguration.cs`, keeping auth flow centralized.
- **ConflictException added**: Plan referenced a 409 response for duplicate invitations. No ConflictException existed in the codebase, so it was created alongside ForbiddenException.
- **IGlobalPreProcessor for ActiveUserPreProcessor**: FastEndpoints 6.x global pre-processors are registered via `ep.PreProcessor<T>(Order.Before)` in the `Endpoints.Configurator`. The pre-processor writes the 401 response directly to `HttpContext.Response` and calls `CompleteAsync()` to short-circuit further processing.
- **Migration used Release configuration**: The dev server was running and locking Debug output DLLs, so the migration was generated using `--configuration Release` to use separate output directories.

## Deviations from Plan

- **Step 8 — FastEndpoints global pre-processor registration**: Plan said to register `ActiveUserPreProcessor` in `DependencyInjection.cs` — done both there (as scoped service) and in `UseFastEndpoints` configurator (required for FastEndpoints to discover it globally).

## Cycle 1 Fixes (reviewer findings)

### [HIGH] Step 12 — Complete read-only enforcement (SettingsView.vue)
Applied `:disabled="isReadOnly"` to every interactive control outside the User Management section:
- Board `<select>` and fallback `<input>`
- Done Statuses: remove `<button>` per item, status `<select>`, Add `<button>`, fallback free-text `<input>` and Add `<button>`
- Excluded From Scope: remove `<button>` per item, status `<select>`, Add `<button>`, Save Excluded Statuses `<button>`
- Workflow Stages: Re-detect `<button>`, per-stage up/down/remove `<button>`s, new stage `<input>`, Add `<button>`, sidelined + `<button>`s
- Cycle Time Boundaries: start `<select>`, end `<select>`, Save Boundaries `<button>`
- Health Thresholds: all 6 `<input>` elements
- Health Weights: all 3 `<input>` elements
- Bug Ratio: bugRatioAlertThreshold, bugRatioConsecutiveSprintCount, defaultSpPerBug `<input>` elements
- Sync: syncBackSprintCount `<input>`, planningWindowDays `<input>`, Sync All `<button>`, Load Sprints `<button>`, from/to sprint `<select>`s, Sync Range `<button>`
Also added `disabled:opacity-50 disabled:cursor-not-allowed` Tailwind classes to all newly-disabled controls for visual consistency.
User Management section left unchanged — already Admin-only gated by `v-if="authStore.isAdmin"`.

### [MEDIUM] Two-transaction invitation acceptance (AuthConfiguration.cs)
Merged the two `SaveChangesAsync` calls into one. `AppUser.CreateFromInvitation` staged via `AddAsync`, `invitation.Accept()` mutates the tracked invitation, then a single `appUserRepository.SaveChangesAsync()` saves both atomically. The `invitationRepository.SaveChangesAsync()` call was removed.

### [LOW] ActiveUserPreProcessor misleading message
Added null check differentiation: `user is null` returns "Account not found.", `!user.IsActive` returns "Your account has been deactivated." The message is now selected before writing the 401 response.
