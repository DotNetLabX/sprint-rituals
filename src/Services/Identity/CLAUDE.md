# Identity Service

**Endpoint framework:** FastEndpoints
**Database:** SQLite (identity.db)
**Auth:** Google OAuth + cookie (the suite's only sign-in)
**Port:** N/A — hosted inside the SprintRituals Host (class library, no standalone self-host)

## Purpose

Identity owns the universal **Person** record and the authenticated **User** for the SprintRituals suite. It performs Google OAuth sign-in (cookie-based), issues the role claim, and exposes user-management endpoints. Other modules (Fokus, later Reflekt) reference a Person; Identity is the single source of truth for who a human is across the suite.

**Plain DDD — NOT ASP.NET Core Identity.** No `IdentityUser`, `IdentityDbContext`, `UserManager`, or `IdentityRole`. Google is the identity provider; we store no passwords. Role is a string claim. Rationale: `docs/proposals/suite-integration-architecture.md` and the `adhoc-SuiteScaffoldingV2` plan.

## Domain model

- **Aggregate:** `Person : AggregateRoot<int>` — universal anchor; anyone from any source, may never log in. `FirstName`, `LastName`, `FullName` (computed), `Email?` (nullable; normalized lowercase; unique NOCASE when non-null; Google-sourced always set, Jira-sourced best-effort), `PictureUrl?`, `Source` (PersonSource), `ExternalId?` (Jira AccountId for Jira-sourced), `CreatedAt`, `UserId?` (nullable back-ref via `LinkUser()`). Factories: `CreateFromGoogle`, `CreateFromJira`. Jira-sourced Persons are matched by `ExternalId` + `Source=Jira`; Google-sourced by email.
- **Aggregate:** `User : AggregateRoot<int>` (promoted from Entity — carries `UserCreated` domain event). `PersonId` FK (1:1, unique), `Role` (string: Admin | Manager), `IsActive`, `RegistrationDate`, `LastLogin?`. Behaviors: `CreateFromPerson`, `RecordLogin`, `Deactivate`, `Reactivate`, `RaiseCreated` (call after SaveChangesAsync — not before, so CountActiveAdminsAsync is correct).
- **Domain events:** `UserCreated(int UserId)` — raised via `user.RaiseCreated()` after save. Dispatched by `DispatchDomainEventsInterceptor`. Handler: `AnnounceCompanyDomainOnUserCreatedHandler`.
- **Enum:** `PersonSource` (Google | Jira | Guest).
- **Value objects:** none.
- **Application layer:** None — handler logic lives in endpoints.

## Endpoint pattern

FastEndpoints class extending `Endpoint<TRequest,TResponse>` / `EndpointWithoutRequest<TResponse>`. Handler logic in `HandleAsync`. Validators are sibling `Validator<T>` classes in the same feature folder. Endpoints are auto-discovered by the Host's multi-assembly `AddFastEndpoints`.

## Auth

- Google OAuth (`Microsoft.AspNetCore.Authentication.Google`) + cookie scheme, configured in `Auth/AuthConfiguration.cs` (`AddIdentityAuth`), wired by the Host.
- `OnTicketReceived` handles three paths:
  1. **Invite-token redemption** — validates and redeems the invitation via `IInvitationService` gRPC (Fokus server), provisions a User with the invitation's role; error outcomes redirect to `/invite/error?reason={specific}`. Already-registered active users with a token get normal login (token ignored).
  2. **Bootstrap** — first User in the system becomes `Role = "Admin"`.
  3. **Regular login** — find Person by `ExternalId`/email → its User → reject if missing/inactive → `RecordLogin`.
- `ActiveUserPreProcessor` (global): **401** if the authenticated User is missing or inactive.
- Roles: string claim (`ClaimTypes.Role`). Endpoints gate with `[Authorize(Roles="Admin")]`. Two-layer authorization per the `authorization-patterns` skill.

## Persistence

- `IdentityDbContext`: **plain `DbContext`** with `Person` + `User` DbSets — NOT `IdentityDbContext<>`.
- Concrete repositories, no interfaces: `PersonRepository`, `UserRepository`. Repos wrap `SaveChangesAsync`; endpoints never touch the DbContext directly.
- Email stored lowercased; unique index uses `.UseCollation("NOCASE")`.
- `DesignTimeDbContextFactory` uses a hardcoded connection string.

## gRPC clients / server

**Server:** `PersonGrpcService` (`Identity.API/Grpc/`) implements `IPersonService` (Blocks.Contracts). Provides `UpsertPersonAsync` (Jira-sourced by ExternalId+Source or Google-sourced by email) and `FindPersonByEmailAsync`. Mapped via `MapIdentityGrpcServices()` → `app.MapGrpcService<PersonGrpcService>()` in Host/Program.cs. Endpoint: h2c port 5001 (Kestrel `Grpc`).

**Client:** `IInvitationService` (Blocks.Contracts) — dials Fokus's invitation gRPC server. Registered via `AddCodeFirstGrpcClient<IInvitationService>(grpcOptions, "Invitation")` in Identity DI. Config key: `Grpc:Services:Invitation:Url`. Used exclusively from `AuthConfiguration.HandleTicketReceivedAsync`.

**Contract topology:** Two opposite-direction contracts — Identity is server for Person, client for Invitation; Fokus is server for Invitation, client for Person.

## Integration events

**Published:** `FirstAdministratorRegistered(string CompanyDomain)` — published by `AnnounceCompanyDomainOnUserCreatedHandler` (`IEventHandler<UserCreated>`) when the first Admin user is created. Gated: `Role=="Admin" && active-admin-count==1`; skips known public providers (gmail.com, outlook.com, etc.). Domain derived via `email.Split('@')[1]`.

**Consumed:** None.

## Reference

- Structure pattern: `<reference-repo>\src\Services\Auth\` — adopt the DDD Person/User/Role split + partial-class behaviors; do NOT adopt `IdentityUser`/JWT/value-objects.
- Behavioral pattern: the current Fokus auth (`<fokus-repo>\src\Services\Fokus\Fokus.API\Auth\`) — Identity is that proven model, split into Person + User.
