# Identity Service

**Endpoint framework:** FastEndpoints
**Database:** SQLite (identity.db)
**Auth:** Google OAuth + cookie (the suite's only sign-in)
**Port:** N/A — hosted inside the SprintRituals Host (class library, no standalone self-host)

## Purpose

Identity owns the universal **Person** record and the authenticated **User** for the SprintRituals suite. It performs Google OAuth sign-in (cookie-based), issues the role claim, and exposes user-management endpoints. Other modules (Fokus, later Reflekt) reference a Person; Identity is the single source of truth for who a human is across the suite.

**Plain DDD — NOT ASP.NET Core Identity.** No `IdentityUser`, `IdentityDbContext`, `UserManager`, or `IdentityRole`. Google is the identity provider; we store no passwords. Role is a string claim. Rationale: `docs/proposals/suite-integration-architecture.md` and the `adhoc-SuiteScaffoldingV2` plan.

## Domain model

- **Aggregate:** `Person : AggregateRoot<int>` — universal anchor; anyone from any source, may never log in. `FirstName`, `LastName`, `FullName` (computed), `Email` (normalized lowercase, unique NOCASE), `PictureUrl?`, `Source` (PersonSource), `ExternalId?`, `CreatedAt`, `UserId?` (nullable back-ref via `LinkUser()`).
- **Entity:** `User : Entity<int>` — a Person who authenticated. `PersonId` FK (1:1, unique), `Role` (string: Admin | Manager), `IsActive`, `RegistrationDate`, `LastLogin?`. Behaviors: `CreateFromPerson`, `RecordLogin`, `Deactivate`, `Reactivate`.
- **Enum:** `PersonSource` (Google | Jira | Guest).
- **Value objects:** none in the scaffold.
- **Application layer:** None — handler logic lives in endpoints.

## Endpoint pattern

FastEndpoints class extending `Endpoint<TRequest,TResponse>` / `EndpointWithoutRequest<TResponse>`. Handler logic in `HandleAsync`. Validators are sibling `Validator<T>` classes in the same feature folder. Endpoints are auto-discovered by the Host's multi-assembly `AddFastEndpoints`.

## Auth

- Google OAuth (`Microsoft.AspNetCore.Authentication.Google`) + cookie scheme, configured in `Auth/AuthConfiguration.cs` (`AddIdentityAuth`), wired by the Host.
- `OnTicketReceived`: **bootstrap** (first User → `Role = "Admin"`) and **regular login** (find Person by `ExternalId`/email → its User → reject if missing/inactive → `RecordLogin`). Invite-token redemption + `CompanyDomain` seed are **DEFERRED** to the infra increment.
- `ActiveUserPreProcessor` (global): **401** if the authenticated User is missing or inactive (`return` immediately after the response write).
- Roles: string claim (`ClaimTypes.Role`). Endpoints gate with `[Authorize(Roles="Admin")]`. Two-layer authorization per the `authorization-patterns` skill.

## Persistence

- `IdentityDbContext`: **plain `DbContext`** with `Person` + `User` DbSets — NOT `IdentityDbContext<>`.
- Concrete repositories, no interfaces: `PersonRepository`, `UserRepository`. Repos wrap `SaveChangesAsync`; endpoints never touch the DbContext directly.
- Email stored lowercased; unique index uses `.UseCollation("NOCASE")`.
- `DesignTimeDbContextFactory` uses a hardcoded connection string.

## gRPC clients / server

None in the scaffold. `PersonGrpcService` arrives in the infra increment (cross-module Person upsert).

## Integration events

**Published:** None (scaffold). **Consumed:** None.

## Reference

- Structure pattern: `<reference-repo>\src\Services\Auth\` — adopt the DDD Person/User/Role split + partial-class behaviors; do NOT adopt `IdentityUser`/JWT/value-objects.
- Behavioral pattern: the current Fokus auth (`<fokus-repo>\src\Services\Fokus\Fokus.API\Auth\`) — Identity is that proven model, split into Person + User.
