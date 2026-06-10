# Identity Extraction (Pass 4) — Implementation

## Files Created

### Step 1 — User promoted to AggregateRoot
- `src/Services/Identity/Identity.Persistence/Migrations/20260609192647_PromoteUserToAggregate.cs` — EF migration adding 4 audit columns (CreatedById, CreatedOn, LastModifiedById, LastModifiedOn) to the Users table.
- `src/Services/Identity/Identity.Persistence/Migrations/20260609192647_PromoteUserToAggregate.Designer.cs` — EF-generated designer file.

### Step 2 — gRPC contracts
- `src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/RedeemInvitationRequest.cs` — DTO for IInvitationService.RedeemAsync (Token + SigningInEmail).
- `src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/RedeemInvitationResponse.cs` — DTO response + `InvitationRedeemOutcome` enum (Success/NotFound/Used/Revoked/Expired/EmailMismatch).
- `src/BuildingBlocks/Blocks.Contracts/Grpc/IInvitationService.cs` — Fokus=server, Identity=client.
- `src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/UpsertPersonRequest.cs` — DTO for IPersonService.UpsertPersonAsync (ExternalId?, Source, Email?, FirstName, LastName, PictureUrl?).
- `src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/UpsertPersonResponse.cs` — PersonId.
- `src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/FindPersonByEmailRequest.cs` — defined-but-unconsumed lookup DTO.
- `src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/FindPersonByEmailResponse.cs` — lookup response (Found, PersonId, Email?, FirstName?, LastName?).
- `src/BuildingBlocks/Blocks.Contracts/Grpc/IPersonService.cs` — Identity=server, Fokus=client.

### Step 3 — gRPC client infra
- `src/BuildingBlocks/Blocks.AspNetCore/Grpc/GrpcServicesOptions.cs` — config POCO (Dictionary<string, GrpcServiceSettings>).
- `src/BuildingBlocks/Blocks.AspNetCore/Grpc/GrpcClientRegistrationExtensions.cs` — `AddCodeFirstGrpcClient<TClient>` helper using `GrpcChannel.ForAddress(url).CreateGrpcService<TClient>()`. Plain h2c, no TLS cert handler, no Polly retry.

### Step 4 — PersonGrpcService (Identity server)
- `src/Services/Identity/Identity.API/Grpc/PersonGrpcService.cs` — `IPersonService` impl. Jira-sourced matched by ExternalId+Source; Google-sourced by email; creates via `CreateFromJira`/`CreateFromGoogle`; calls `person.RaiseCreated()` post-save on create.

### Step 5 — InvitationGrpcService (Fokus server)
- `src/Services/Fokus/Fokus.API/Grpc/InvitationGrpcService.cs` — `IInvitationService` impl. Pre-checks all failure outcomes before calling `Invitation.Accept(now)` — no exception crosses the gRPC boundary (L1). EmailMismatch is a redeem-only outcome (L2).

### Step 7 — Integration event contract
- `src/BuildingBlocks/Blocks.Contracts/IntegrationEvents/FirstAdministratorRegistered.cs` — plain record `(string CompanyDomain)`, mirrors `SprintSynced` style.

### Step 8 — UserCreated domain event handler
- `src/Services/Identity/Identity.API/Features/Auth/UserCreated/AnnounceCompanyDomainOnUserCreatedHandler.cs` — `IEventHandler<UserCreated>`. Gates on `Role=="Admin" && active-admin-count==1`; derives domain via `email.Split('@')[1]`; skips public providers (gmail.com, outlook.com, hotmail.com, live.com, yahoo.com, icloud.com, me.com, mac.com, googlemail.com); publishes `FirstAdministratorRegistered` via `IPublishEndpoint`.

### Step 9 — FirstAdministratorRegistered consumer
- `src/Services/Fokus/Fokus.API/Features/Settings/Consumers/SeedCompanyDomainOnFirstAdministratorRegisteredConsumer.cs` — sealed `IConsumer<FirstAdministratorRegistered>`. Idempotent: sets `AppSettings.CompanyDomain` only when null. Mutates the loaded instance via `AppSettingsRepository.GetAsync` + `SaveAsync` (L5).

### Step 10 — Person.Email nullable + Jira user fetch
- `src/Services/Identity/Identity.Persistence/Migrations/20260609..._MakePersonEmailOptional.cs` — drops unique index, alters Email to nullable, recreates index with filter `[Email] IS NOT NULL`.
- `src/Services/Fokus/Fokus.Persistence/Migrations/20260609..._AddDeveloperEmail.cs` — adds nullable `Email` column to `Developers` table.

## Files Modified

### Step 1
- `src/Services/Identity/Identity.Domain/Users/User.cs` — base class `Entity<int>` → `AggregateRoot<int>`.
- `src/Services/Identity/Identity.Domain/Users/Behaviors/User.cs` — added `RaiseCreated()` calling `AddDomainEvent(new UserCreated(Id))`.
- `src/Services/Identity/Identity.Domain/Users/Events/UserCreated.cs` — updated doc-comment (event is now live, not a placeholder).
- `src/Services/Identity/Identity.Persistence/Migrations/IdentityDbContextModelSnapshot.cs` — auto-updated by EF (4 audit columns on User).

### Steps 3 / 4
- `src/BuildingBlocks/Blocks.AspNetCore/Blocks.AspNetCore.csproj` — added `protobuf-net.Grpc` + `Grpc.Net.Client` package refs.
- `src/Directory.Packages.props` — added `protobuf-net.Grpc 1.2.2` + `Grpc.Net.Client 2.71.0` (NOT Grpc.Net.ClientFactory).
- `src/Host/appsettings.json` — added `Grpc:Services:{ Invitation: { Url }, Person: { Url } }` client-dial config.
- `src/Services/Identity/Identity.API/Identity.API.csproj` — added MassTransit, protobuf-net.Grpc.AspNetCore, Blocks.Contracts refs.
- `src/Services/Identity/Identity.API/DependencyInjection.cs` — added `AddCodeFirstGrpc()`, `AddCodeFirstGrpcClient<IInvitationService>`, `MapIdentityGrpcServices()` method.
- `src/Host/Program.cs` — added `app.MapIdentityGrpcServices()`.

### Step 5
- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` — added `AddCodeFirstGrpcClient<IPersonService>`, `MapGrpcService<InvitationGrpcService>`, `AddConsumers(...)`.

### Step 6 — Invite redemption at login
- `src/Services/Identity/Identity.API/Auth/AuthConfiguration.cs` — replaced `not-implemented` stub with full redemption flow. Decomposed into private helpers: `HandleInviteRedemptionAsync`, `HandleBootstrapAsync`, `HandleRegularLoginAsync`, `ResolveOrCreatePersonAsync`, `SignInAsync`, `ExtractName`, `OutcomeToReason`. Bootstrap path now calls `user.RaiseCreated()` post-save (L3). Existing-active-user fast-path ignores token and logs in normally (Q5).

### Step 9
- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` — `AddConsumers(typeof(DependencyInjection).Assembly)` added inside existing single `AddMassTransit` (no second bus).

### Step 10
- `src/Services/Identity/Identity.Domain/Persons/Person.cs` — `Email` changed from `string` to `string?`.
- `src/Services/Identity/Identity.Domain/Persons/Behaviors/Person.cs` — added `CreateFromJira` + `UpdateFromJira` factories; `UpdateFromJira` fills email only if currently null.
- `src/Services/Identity/Identity.Persistence/Configurations/PersonConfiguration.cs` — removed `IsRequired()` from Email; unique index scoped to `[Email] IS NOT NULL`.
- `src/Services/Identity/Identity.Persistence/Repositories/PersonRepository.cs` — added `GetByExternalIdAndSourceAsync`.
- `src/Services/Identity/Identity.API/Auth/AuthConfiguration.cs` — email null-coalesced in claims builder.
- `src/Modules/Jira/Jira.Contracts/JiraIssue.cs` — added `EmailAddress` to `JiraUser`.
- `src/Modules/Jira/Jira.Contracts/IJiraClient.cs` — added `GetUserAsync(accountId)`.
- `src/Modules/Jira/Jira.RestApi/IJiraApi.cs` — added Refit `GET /rest/api/3/user` method.
- `src/Modules/Jira/Jira.RestApi/RestApiJiraClient.cs` — implemented best-effort `GetUserAsync`.
- `src/Services/Fokus/Fokus.Domain/Developer/Developer.cs` — added `Email string?`.
- `src/Services/Fokus/Fokus.API/Features/Sync/JiraDomainMapper.cs` — propagated email in `MapDeveloper`.
- `src/Services/Fokus/Fokus.Persistence/Repositories/DeveloperRepository.cs` — propagated email on upsert.
- `src/Services/Fokus/Fokus.Persistence/Configurations/DeveloperConfiguration.cs` — `HasMaxLength(256)` on Email.
- `src/Services/Fokus/Fokus.Persistence/Fokus.Persistence.csproj` — added `Microsoft.EntityFrameworkCore.Design` (PrivateAssets=all).

### Step 11 — Wire Fokus sync → IPersonService
- `src/Services/Fokus/Fokus.API/Features/Sync/SprintIssueSyncService.cs` — injected `IPersonService`; added `UpsertPersonBestEffortAsync` (try/catch, gRPC failure swallowed/logged); called from both `SyncAsync` and `SyncEpicDiscoveryAsync` for each discovered developer. `DisplayName` split into FirstName/LastName by first space.

### Fix cycle 1 — Review findings (H1, H2, M1-M5, L1, L2)
- `src/Services/Fokus/Fokus.Domain/Auth/Invitation.cs` — added `ConcurrencyToken Guid` property (H1 — optimistic concurrency guard against replay).
- `src/Services/Fokus/Fokus.Persistence/Configurations/InvitationConfiguration.cs` — configured `ConcurrencyToken` as `IsConcurrencyToken()` (H1).
- `src/Services/Fokus/Fokus.API/Grpc/InvitationGrpcService.cs` — bumps `ConcurrencyToken` before save; catches `DbUpdateConcurrencyException` → returns `Used` outcome (H1: atomic single-use redemption).
- `src/Services/Fokus/Fokus.Persistence/Migrations/...AddInvitationConcurrencyToken.cs` — Fokus migration adding `ConcurrencyToken` column to `Invitations` table (H1).
- `src/Services/Identity/Identity.API/Auth/AuthConfiguration.cs` — `HandleInviteRedemptionAsync` now covers inactive user: if user exists (active OR inactive), never creates a second User for same PersonId; inactive → `/invite/error?reason=account-inactive` (H2); wraps `RedeemAsync` in `catch (RpcException)` → `/invite/error?reason=error` (M1); return type changed from `Task<bool>` to `Task` — all branches use `return;` (L2).
- `src/Services/Fokus/Fokus.API/Features/Sync/SprintIssueSyncService.cs` — `FetchJiraEmailBestEffortAsync` helper added (wires the previously dead `GetUserAsync` — M2); called when developer email is null; `UpsertPersonBestEffortAsync` moved to AFTER `SaveChangesAsync` — Person reconciliation now only runs after Fokus data is committed (M4); same fix applied to `SyncEpicDiscoveryAsync`.
- `src/Services/Identity/Identity.Persistence/Repositories/PersonRepository.cs` — `GetByExternalIdAndSourceAsync` uses `Enum.TryParse<PersonSource>` before the LINQ query, preventing client-side evaluation (M3); added `global using Identity.Domain.Enums` to `GlobalUsings.cs`.
- `src/Services/Identity/Identity.Persistence/Migrations/20260609194823_MakePersonEmailOptional.cs` — `Down()` replaced with `throw InvalidOperationException` (M5: migration is non-reversible once null-email Persons exist).
- `src/BuildingBlocks/Blocks.AspNetCore/Grpc/GrpcClientRegistrationExtensions.cs` — `AddScoped` → `AddSingleton` for gRPC channel + typed client (L1: channels are thread-safe and designed to be shared; avoids socket leak per DI resolution).

### Step 12 — Documentation
- `src/Services/Identity/CLAUDE.md` — updated domain model (User promoted; Person.Email nullable; ExternalId for Jira); updated Auth section (three login paths); updated gRPC and integration events sections.
- `src/Services/Fokus/CLAUDE.md` — updated purpose, gRPC client/server, integration events, MassTransit topology.
- `CLAUDE.md` (root) — removed "Deferred (infra increment)" bullet; added gRPC contract topology + company-domain seed description.

## Key Decisions
- Used Identity.Persistence as both `-p` and `-s` for the Identity EF migrations because `Identity.API` lacks `Microsoft.EntityFrameworkCore.Design`. Persistence has both the Design package and a `DesignTimeDbContextFactory`.
- `HandleInviteRedemptionAsync` always completes the response (no fall-through return path) — caller returns immediately after awaiting. Return type is `Task` (no bool return value).
- `UpsertPersonBestEffortAsync` runs AFTER Fokus `SaveChangesAsync` — a cross-module write failure never orphans committed Fokus data; the sync result is already durable.
- Concurrency guard for invitation redemption: opted for EF `IsConcurrencyToken` on a `Guid` property rather than a raw `ExecuteUpdateAsync` WHERE clause — EF handles the optimistic locking pattern cleanly in SQLite; `DbUpdateConcurrencyException` maps to `Used` outcome without exposing an exception across the gRPC boundary.
- `MakePersonEmailOptional` declared non-reversible: no backfill strategy exists for NULL-email Persons once Jira-sourced data exists; an unfiltered unique index restore would fail at runtime.
- Single `AddMassTransit` lives in Fokus. Identity publishes via `IPublishEndpoint` resolved from the Host's shared DI container — no second bus registration needed.

## Carry-Over Findings
| Title | Severity | For | Evidence | Note |
|-------|----------|-----|----------|------|

## KB Changes
| Entry | Action | What changed |
|-------|--------|-------------|

## Deviations from Plan
- Plan specified `-s src/Services/Identity/Identity.API` for the migration; switched to `-s src/Services/Identity/Identity.Persistence` because `Identity.API` lacks `Microsoft.EntityFrameworkCore.Design`. Persistence has both the Design package and a `DesignTimeDbContextFactory`.
- Added `Microsoft.EntityFrameworkCore.Design` to `Fokus.Persistence.csproj` (PrivateAssets=all) to enable Fokus migrations with Persistence as startup project — this was a gap in the plan that was necessary in practice.
- Steps executed in order 1→2→3→10→4→5→6→7→8→9→11→12 (Step 10 moved before Step 4 because PersonGrpcService depends on nullable Email and `CreateFromJira` factory).
