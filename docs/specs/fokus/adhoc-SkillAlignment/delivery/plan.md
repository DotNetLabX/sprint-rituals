# Skill Alignment — BuildingBlocks & Entity Refactoring

**Feature Spec:** None

## Context

The Fokus codebase was implemented without following the skills in `.claude/skills/`. Entities are plain classes (no base classes), there are no BuildingBlocks packages, no domain event infrastructure, no global error middleware, and no Guard utilities. The architecture doc v2 identifies 8 referenced skills — this plan builds the missing infrastructure and refactors existing code to follow those skills.

This is a foundational refactoring. Every future feature (F6–F14 analytics, domain events, richer domain behavior) depends on this infrastructure being in place.

## Scope

**In scope:**
- Create all BuildingBlocks packages referenced by applicable skills
- Refactor Fokus.Domain entities to extend skill-defined base classes
- Refactor Fokus.Persistence to use skill-defined repository and configuration bases
- Add domain event dispatch infrastructure (even though v1 raises no events — the interceptor is harmless and ready for later)
- Add global error middleware
- Create service CLAUDE.md
- Fresh migration (pre-production, no data to preserve)

**Out of scope:**
- Adding domain events to existing aggregates (no events needed in v1)
- Adding behavior methods to entities (they remain thin data containers for now)
- Frontend changes
- New features or analytics endpoints
- MediatR, MassTransit, gRPC, auth (not applicable in v1)

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | error-handling | Build | Guard.cs, GuardExtensions.cs in Blocks.Core | |
| 2 | domain-patterns | Build | Entity<T>, AggregateRoot, VOs, IDomainEvent, DomainException in Blocks.Domain | |
| 3 | error-handling | Build | HttpException hierarchy in Blocks.Exceptions | |
| 4 | persistence-patterns | Build | RepositoryBase, EntityConfiguration, AuditedEntityConfiguration, Interceptor in Blocks.EntityFrameworkCore | |
| 5 | error-handling | Build | GlobalExceptionMiddleware in Blocks.AspNetCore | |
| 6 | domain-patterns | Build | DomainEventPublisher (FastEndpoints variant) in Blocks.FastEndpoints | |
| 7 | domain-patterns | Follow | Sprint → AggregateRoot, Ticket → AggregateRoot<string>, Developer → Entity<string>, StatusTransition → Entity<int>, VOs → ValueObject | |
| 8 | persistence-patterns | Follow | FokusDbContext → ApplicationDbContext, repos → RepositoryBase, configs → EntityConfiguration, register interceptor | |
| 9 | error-handling | Follow | Register GlobalExceptionMiddleware, update JiraApiException to extend HttpException | |
| 10 | (none) | — | Drop old migration, create fresh InitialCreate | Log to lessons.md |
| 11 | create-service-claude-md | Follow | Fokus service: FastEndpoints, SQLite, FE event bus, no auth, service index TBD | |
| 12 | (none) | — | dotnet build + verify | |

**Disposition rules:**
- **Follow** — apply the skill pattern. Adapt implementation specifics (e.g., SQLite instead of SQL Server) but never skip the pattern.
- **Build** — skill references infrastructure that doesn't exist locally. Build it first, then follow the skill.
- **None** — no skill covers this step. Full inline detail required. Log the gap.
- **Never "Adapt"** — if you're about to write "adapted for this project's needs," you're skipping the skill. Either Follow (the pattern applies, specifics may differ) or Build (the infrastructure is missing).

## Domain Model Changes

### Entity Renames (base class alignment)

The `domain-patterns` skill defines `Entity<TPrimaryKey>` with an `Id` property. Entities with non-`Id` primary keys must be renamed:

| Entity | Current PK Property | New PK Property | Type | Rationale |
|--------|-------------------|-----------------|------|-----------|
| Sprint | Id (int) | Id (int) | No change | Already matches |
| Ticket | Key (string) | Id (string) | **Rename** | Entity<string>.Id replaces Ticket.Key |
| Developer | AccountId (string) | Id (string) | **Rename** | Entity<string>.Id replaces Developer.AccountId |
| StatusTransition | Id (int) | Id (int) | No change | Already matches |

### Cascade from Renames

| Location | Old Reference | New Reference |
|----------|--------------|---------------|
| SprintMembership.TicketKey | FK → Ticket.Key | SprintMembership.TicketId → Ticket.Id |
| Ticket.AssigneeId | FK → Developer.AccountId | Ticket.AssigneeId → Developer.Id (name stays, target changes) |
| SprintMembership composite PK | (SprintId, TicketKey) | (SprintId, TicketId) |
| All repository queries | `.Key`, `.AccountId` | `.Id` |
| JiraMapper | Maps to `.Key`, `.AccountId` | Maps to `.Id` |
| EF configurations | `.HasKey(t => t.Key)` etc. | `.HasKey(t => t.Id)` |
| Endpoint code | References `.Key`, `.AccountId` | References `.Id` |

### New Audit Fields (from AggregateRoot)

Sprint and Ticket gain nullable audit columns: `CreatedById` (string?), `CreatedOn` (DateTime?), `LastModifiedById` (string?), `LastModifiedOn` (DateTime?). These will be null in v1 (no user auth). The interceptor/middleware populates them when auth is added.

### Entity Classification

| Entity | Base Class | Notes |
|--------|-----------|-------|
| Sprint | AggregateRoot (int PK) | Aggregate root, owns SprintMemberships |
| Ticket | AggregateRoot<string> | Aggregate root, owns StatusTransitions |
| Developer | Entity<string> | Standalone entity, not an aggregate root |
| StatusTransition | Entity<int> | Child entity of Ticket |
| SprintMembership | Plain class | Child entity of Sprint, composite key — does not fit Entity<T> |
| AppSettings | Plain class | Configuration entity, not a DDD concept |

### Value Objects

| VO | Base Class | Notes |
|----|-----------|-------|
| HealthThresholdConfig | ValueObject | Multi-property, uses GetEqualityComponents |
| HealthWeightConfig | ValueObject | Multi-property, uses GetEqualityComponents |

## Data Model Changes

- Ticket table: rename column `Key` → `Id` (PK)
- Developer table: rename column `AccountId` → `Id` (PK)
- SprintMembership table: rename column `TicketKey` → `TicketId` (FK + composite PK)
- Sprint table: add nullable audit columns (CreatedById, CreatedOn, LastModifiedById, LastModifiedOn)
- Ticket table: add nullable audit columns
- Pre-production project → drop and recreate migration (no data to preserve)

## Implementation Steps

### Step 1: Create Blocks.Core

Build `error-handling` skill infrastructure (Guard utilities).

**Create:**
- `src/BuildingBlocks/Blocks.Core/Blocks.Core.csproj` — class library, no external dependencies
- `src/BuildingBlocks/Blocks.Core/Guard.cs` — static guard methods: `NotFound(value)`, `ThrowIfNullOrWhiteSpace(value)`, `ThrowIfFalse(condition, message)`
- `src/BuildingBlocks/Blocks.Core/GuardExtensions.cs` — `OrThrowNotFound()` extension method

Follow `error-handling` skill § Guard Utilities.

### Step 2: Create Blocks.Domain

Build `domain-patterns` skill infrastructure (entity base classes, VOs, events).

**Create:**
- `src/BuildingBlocks/Blocks.Domain/Blocks.Domain.csproj` — references Blocks.Core
- `src/BuildingBlocks/Blocks.Domain/Entities/IEntity.cs` — `IEntity<TPrimaryKey>` interface
- `src/BuildingBlocks/Blocks.Domain/Entities/Entity.cs` — `Entity<TPrimaryKey>` with Id, equality by ID, IsNew
- `src/BuildingBlocks/Blocks.Domain/Entities/IAggregateRoot.cs` — marker interface with `IReadOnlyList<IDomainEvent> DomainEvents`, `AddDomainEvent()`, `ClearDomainEvents()`
- `src/BuildingBlocks/Blocks.Domain/Entities/AggregateRoot.cs` — extends Entity<T>, adds audit fields (CreatedById, CreatedOn, LastModifiedById, LastModifiedOn), private `_domainEvents` list. Convenience form: `AggregateRoot` = `AggregateRoot<int>`
- `src/BuildingBlocks/Blocks.Domain/ValueObjects/ValueObject.cs` — abstract, `GetEqualityComponents()`, equality operators
- `src/BuildingBlocks/Blocks.Domain/ValueObjects/StringValueObject.cs` — single string `Value`, full equality
- `src/BuildingBlocks/Blocks.Domain/ValueObjects/SingleValueObject.cs` — `SingleValueObject<T>` for struct values
- `src/BuildingBlocks/Blocks.Domain/Events/IDomainEvent.cs` — `IDomainEvent` (dual: FastEndpoints `IEvent`)
- `src/BuildingBlocks/Blocks.Domain/Events/IDomainEventPublisher.cs` — publisher abstraction
- `src/BuildingBlocks/Blocks.Domain/Exceptions/DomainException.cs` — base domain exception

Follow `domain-patterns` skill exactly. Since Fokus uses FastEndpoints event bus (not MediatR), `IDomainEvent` extends FastEndpoints `IEvent` only (not `INotification`).

**Dependencies:** Step 1 (Blocks.Core for Guard).

### Step 3: Create Blocks.Exceptions

Build `error-handling` skill infrastructure (HTTP exception hierarchy).

**Create:**
- `src/BuildingBlocks/Blocks.Exceptions/Blocks.Exceptions.csproj` — no dependencies
- `src/BuildingBlocks/Blocks.Exceptions/HttpException.cs` — base with StatusCode
- `src/BuildingBlocks/Blocks.Exceptions/BadRequestException.cs` → 400
- `src/BuildingBlocks/Blocks.Exceptions/NotFoundException.cs` → 404
- `src/BuildingBlocks/Blocks.Exceptions/UnauthorizedException.cs` → 401

Follow `error-handling` skill § Exception Hierarchy. Wire `Guard.NotFound()` in Blocks.Core to throw `NotFoundException` from this package — Blocks.Core references Blocks.Exceptions.

**Dependencies:** None (but Blocks.Core should reference this for Guard.NotFound).

### Step 4: Create Blocks.EntityFrameworkCore

Build `persistence-patterns` skill infrastructure.

**Create:**
- `src/BuildingBlocks/Blocks.EntityFrameworkCore/Blocks.EntityFrameworkCore.csproj` — references Blocks.Domain, Microsoft.EntityFrameworkCore
- `src/BuildingBlocks/Blocks.EntityFrameworkCore/ApplicationDbContext.cs` — `ApplicationDbContext<TDbContext>` base
- `src/BuildingBlocks/Blocks.EntityFrameworkCore/Repositories/RepositoryBase.cs` — `RepositoryBase<TContext, TEntity, TKey>` with Query(), AddAsync(), FindByIdAsync(), UpsertAsync(), SaveChangesAsync(), DeleteByIdAsync()
- `src/BuildingBlocks/Blocks.EntityFrameworkCore/Extensions/RepositoryExtensions.cs` — `FindByIdOrThrowAsync()`
- `src/BuildingBlocks/Blocks.EntityFrameworkCore/EntityConfigurations/EntityConfiguration.cs` — base config with HasKey, SeedFromJsonFile
- `src/BuildingBlocks/Blocks.EntityFrameworkCore/EntityConfigurations/AuditedEntityConfiguration.cs` — extends EntityConfiguration, maps audit fields, HasGeneratedId virtual, DefaultDateSql virtual
- `src/BuildingBlocks/Blocks.EntityFrameworkCore/Interceptors/DispatchDomainEventsInterceptor.cs` — scans ChangeTracker for IAggregateRoot with pending events, publishes via IDomainEventPublisher, clears events

Follow `persistence-patterns` skill. Adapt for SQLite: `DefaultDateSql` override returns SQLite-compatible expression. No `IRepository` interface (project guardrail: no repository interfaces). RepositoryBase constrains on `IEntity<TKey>`.

**Dependencies:** Step 2 (Blocks.Domain), Step 3 (Blocks.Exceptions for NotFoundException in FindByIdOrThrowAsync).

### Step 5: Create Blocks.AspNetCore

Build `error-handling` skill infrastructure (middleware).

**Create:**
- `src/BuildingBlocks/Blocks.AspNetCore/Blocks.AspNetCore.csproj` — references Blocks.Exceptions, Blocks.Domain (for DomainException)
- `src/BuildingBlocks/Blocks.AspNetCore/Middlewares/GlobalExceptionMiddleware.cs` — type-switch mapping per `error-handling` skill (ValidationException→400, ArgumentException→400, BadRequestException→400, NotFoundException→404, DomainException→400, UnauthorizedException→401, OperationCanceledException→499, unhandled→500). Returns JSON `{ StatusCode, Message, TraceId, Details }` with structured validation errors.

Follow `error-handling` skill § Global Exception Middleware.

**Dependencies:** Step 2 (DomainException), Step 3 (Blocks.Exceptions).

### Step 6: Create Blocks.FastEndpoints

Build `domain-patterns` skill infrastructure (event publisher variant).

**Create:**
- `src/BuildingBlocks/Blocks.FastEndpoints/Blocks.FastEndpoints.csproj` — references Blocks.Domain, FastEndpoints package
- `src/BuildingBlocks/Blocks.FastEndpoints/DomainEventPublisher.cs` — implements `IDomainEventPublisher`, dispatches via FastEndpoints `IEvent.PublishAsync(Mode.WaitForAll)`

Follow `domain-patterns` skill § IDomainEventPublisher — Framework Variants (FastEndpoints variant).

**Dependencies:** Step 2 (Blocks.Domain for IDomainEvent, IDomainEventPublisher).

### Step 7: Refactor Fokus.Domain

Follow `domain-patterns` skill.

**Modify:**
- `Fokus.Domain.csproj` — add reference to Blocks.Domain
- `Sprint.cs` — extend `AggregateRoot` (int PK). Remove explicit `Id` property (inherited). Keep backing collection pattern (already correct: `_memberships`).
- `Ticket.cs` — extend `AggregateRoot<string>`. Rename `Key` → inherited `Id`. Keep backing collection (`_statusTransitions`).
- `Developer.cs` — extend `Entity<string>`. Rename `AccountId` → inherited `Id`.
- `StatusTransition.cs` — extend `Entity<int>`. Remove explicit `Id` (inherited).
- `SprintMembership.cs` — rename `TicketKey` → `TicketId`. Keep as plain class (composite key child entity).
- `HealthThresholdConfig.cs` — extend `ValueObject`. Implement `GetEqualityComponents()`.
- `HealthWeightConfig.cs` — extend `ValueObject`. Implement `GetEqualityComponents()`.

**Dependencies:** Steps 1–2 (Blocks.Core + Blocks.Domain exist).

### Step 8: Refactor Fokus.Persistence

Follow `persistence-patterns` skill.

**Modify:**
- `Fokus.Persistence.csproj` — add references to Blocks.EntityFrameworkCore, Blocks.FastEndpoints
- `FokusDbContext.cs` — extend `ApplicationDbContext<FokusDbContext>` instead of `DbContext`
- `Repositories/SprintRepository.cs` — extend `RepositoryBase<FokusDbContext, Sprint, int>`. Keep custom `UpsertAsync` and `UpsertMembershipsAsync`. Remove boilerplate methods now on base (Add, Query, SaveChangesAsync).
- `Repositories/TicketRepository.cs` — extend `RepositoryBase<FokusDbContext, Ticket, string>`. Keep custom methods.
- `Repositories/DeveloperRepository.cs` — extend `RepositoryBase<FokusDbContext, Developer, string>`. Keep custom UpsertAsync.
- `Repositories/AppSettingsRepository.cs` — keep as-is (AppSettings is not an IEntity<T>, doesn't extend base)
- `Configurations/SprintConfiguration.cs` — extend `AuditedEntityConfiguration<Sprint>`. Override `HasGeneratedId` → `false` (Jira-assigned ID, ValueGeneratedNever).
- `Configurations/TicketConfiguration.cs` — extend `AuditedEntityConfiguration<Ticket>`. Override `HasGeneratedId` → `false`. Update `HasKey` from `Key` → `Id`.
- `Configurations/DeveloperConfiguration.cs` — extend `EntityConfiguration<Developer>`. Override `HasGeneratedId` → `false`. Update PK from `AccountId` → `Id`.
- `Configurations/SprintMembershipConfiguration.cs` — keep as manual config (composite key, no base class). Rename `TicketKey` → `TicketId` in composite key and FK.
- `Configurations/StatusTransitionConfiguration.cs` — extend `EntityConfiguration<StatusTransition>`.
- `Configurations/AppSettingsConfiguration.cs` — keep as-is.
- `DependencyInjection.cs` — register `DispatchDomainEventsInterceptor` as `ISaveChangesInterceptor`. Register `DomainEventPublisher` as `IDomainEventPublisher`.

**Dependencies:** Steps 4, 6, 7 (BuildingBlocks exist + domain entities refactored).

### Step 9: Refactor Fokus.API

Follow `error-handling` skill for middleware registration.

**Modify:**
- `Fokus.API.csproj` — add reference to Blocks.AspNetCore
- `Program.cs` — register `GlobalExceptionMiddleware` via `app.UseMiddleware<GlobalExceptionMiddleware>()` (before other middleware)
- `Infrastructure/Jira/JiraApiException.cs` — evaluate whether to make it extend `HttpException` or keep separate (Jira errors already have custom handling in endpoints)
- `Features/Sync/SyncSprints/SyncSprintsEndpoint.cs` — update all `TicketKey`/`Key`/`AccountId` references to `TicketId`/`Id`
- `Features/Sync/SyncBacklog/SyncBacklogEndpoint.cs` — same rename updates
- `Infrastructure/Jira/Mapping/JiraMapper.cs` — update mapped property names (Key→Id, AccountId→Id, TicketKey→TicketId)
- All other endpoints/validators — update any references to renamed properties

**Dependencies:** Steps 5, 7, 8 (middleware exists, domain + persistence refactored).

### Step 10: Fresh Migration

No skill covers this step. Full inline detail:

1. Delete the existing migration files in `Fokus.Persistence/Migrations/`
2. Run: `dotnet ef migrations add InitialCreate -p src/Services/Fokus/Fokus.Persistence -s src/Services/Fokus/Fokus.API`
3. Delete the old SQLite database file (if exists)
4. The migration will create the schema from scratch with: renamed columns (Key→Id, AccountId→Id, TicketKey→TicketId), new audit columns on Sprint and Ticket tables

Pre-production project — no data to preserve. Fresh start is cleaner than column rename migrations.

**Dependencies:** Steps 7–9 (all code changes complete).

### Step 11: Create Service CLAUDE.md

Follow `create-service-claude-md` skill.

**Create:** `src/Services/Fokus/CLAUDE.md`

Service axes:
- **Name:** Fokus
- **Endpoint framework:** FastEndpoints
- **Persistence:** EF Core + SQLite
- **Event dispatch:** FastEndpoints IEvent bus (standard interceptor, not transactional)
- **Domain event publisher:** Blocks.FastEndpoints/DomainEventPublisher
- **Auth:** None (v1)
- **Service index:** TBD (assign when wiring docker-compose)
- **Key patterns:** Sync-from-external (Jira), natural keys, computed metrics (never stored), thin domain (v1)

**Dependencies:** None (can be done anytime).

### Step 12: Verify Build

No skill covers this step.

1. Run `dotnet build` from solution root — must compile with zero errors
2. Run the application: `dotnet run --project src/Services/Fokus/Fokus.API` — must start, apply migration, seed default AppSettings
3. Confirm Scalar UI loads at `/scalar` in development mode
4. If any compilation errors, fix them before reporting done

**Dependencies:** Steps 1–10 complete.

## Migration Notes

```bash
# Delete existing migrations
rm -rf src/Services/Fokus/Fokus.Persistence/Migrations/

# Delete existing SQLite database (if any)
rm -f src/Services/Fokus/Fokus.API/*.db

# Create fresh migration
dotnet ef migrations add InitialCreate -p src/Services/Fokus/Fokus.Persistence -s src/Services/Fokus/Fokus.API

# Apply
dotnet ef database update -p src/Services/Fokus/Fokus.Persistence -s src/Services/Fokus/Fokus.API
```

## Testing Strategy

- **Build verification:** `dotnet build` passes with zero errors and zero warnings related to the refactoring
- **Runtime verification:** Application starts, migration applies, default AppSettings seeded
- **Endpoint verification:** GET /api/settings returns valid response (proves DB + repository + endpoint chain works)
- **Domain event infrastructure:** Interceptor runs on SaveChanges (logs nothing — no events raised). Verified by successful save operations.

## Open Questions

None.
