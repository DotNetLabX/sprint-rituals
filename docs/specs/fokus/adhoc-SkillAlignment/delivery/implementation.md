# SkillAlignment — Implementation

## Files Created

### Blocks.Exceptions
- `src/BuildingBlocks/Blocks.Exceptions/Blocks.Exceptions.csproj` — no dependencies class library
- `src/BuildingBlocks/Blocks.Exceptions/HttpException.cs` — abstract base with int StatusCode
- `src/BuildingBlocks/Blocks.Exceptions/BadRequestException.cs` — 400
- `src/BuildingBlocks/Blocks.Exceptions/NotFoundException.cs` — 404
- `src/BuildingBlocks/Blocks.Exceptions/UnauthorizedException.cs` — 401

### Blocks.Core
- `src/BuildingBlocks/Blocks.Core/Blocks.Core.csproj` — references Blocks.Exceptions
- `src/BuildingBlocks/Blocks.Core/Guard.cs` — NotFound, ThrowIfNullOrWhiteSpace, ThrowIfFalse static methods
- `src/BuildingBlocks/Blocks.Core/GuardExtensions.cs` — OrThrowNotFound() extension method

### Blocks.Domain
- `src/BuildingBlocks/Blocks.Domain/Blocks.Domain.csproj` — references Blocks.Core, FastEndpoints package
- `src/BuildingBlocks/Blocks.Domain/Entities/IEntity.cs` — IEntity<TPrimaryKey> interface
- `src/BuildingBlocks/Blocks.Domain/Entities/Entity.cs` — Entity<TPrimaryKey> with Id, equality by ID, IsNew
- `src/BuildingBlocks/Blocks.Domain/Entities/IAggregateRoot.cs` — marker interface with DomainEvents, AddDomainEvent, ClearDomainEvents
- `src/BuildingBlocks/Blocks.Domain/Entities/AggregateRoot.cs` — extends Entity<T>, audit fields, private _domainEvents list; AggregateRoot convenience form = AggregateRoot<int>
- `src/BuildingBlocks/Blocks.Domain/ValueObjects/ValueObject.cs` — abstract with GetEqualityComponents and equality operators
- `src/BuildingBlocks/Blocks.Domain/ValueObjects/StringValueObject.cs` — single string Value
- `src/BuildingBlocks/Blocks.Domain/ValueObjects/SingleValueObject.cs` — single struct Value<T>
- `src/BuildingBlocks/Blocks.Domain/Events/IDomainEvent.cs` — extends FastEndpoints IEvent only (not INotification — Fokus has no MediatR)
- `src/BuildingBlocks/Blocks.Domain/Events/IDomainEventPublisher.cs` — publisher abstraction
- `src/BuildingBlocks/Blocks.Domain/Exceptions/DomainException.cs` — base domain exception

### Blocks.EntityFrameworkCore
- `src/BuildingBlocks/Blocks.EntityFrameworkCore/Blocks.EntityFrameworkCore.csproj` — references Blocks.Domain, Blocks.Exceptions, Microsoft.EntityFrameworkCore
- `src/BuildingBlocks/Blocks.EntityFrameworkCore/ApplicationDbContext.cs` — ApplicationDbContext<TDbContext> base (simplified — no ref cache helpers, incompatible with async)
- `src/BuildingBlocks/Blocks.EntityFrameworkCore/Repositories/RepositoryBase.cs` — generic EF repository: Query, FindByIdAsync, AddAsync, UpsertAsync, DeleteByIdAsync, SaveChangesAsync
- `src/BuildingBlocks/Blocks.EntityFrameworkCore/Extensions/RepositoryExtensions.cs` — FindByIdOrThrowAsync extension
- `src/BuildingBlocks/Blocks.EntityFrameworkCore/EntityConfigurations/EntityConfiguration.cs` — base config with HasKey and HasGeneratedId virtual; two variants: EntityConfiguration<T> (int PK) and EntityConfiguration<T, TKey>
- `src/BuildingBlocks/Blocks.EntityFrameworkCore/EntityConfigurations/AuditedEntityConfiguration.cs` — extends EntityConfiguration, maps audit fields; two variants for int and generic PK; no DefaultDateSql (requires relational EF package not referenced in this package)
- `src/BuildingBlocks/Blocks.EntityFrameworkCore/Interceptors/DispatchDomainEventsInterceptor.cs` — scans ChangeTracker, dispatches events via IDomainEventPublisher after SaveChanges

### Blocks.AspNetCore
- `src/BuildingBlocks/Blocks.AspNetCore/Blocks.AspNetCore.csproj` — references Blocks.Domain, Blocks.Exceptions; uses Microsoft.AspNetCore.App framework reference
- `src/BuildingBlocks/Blocks.AspNetCore/Middlewares/GlobalExceptionMiddleware.cs` — type-switch: ValidationException→400, ArgumentException→400, BadRequestException→400, NotFoundException→404, DomainException→400, UnauthorizedException→401, OperationCanceledException→499, unhandled→500; structured validation error format

### Blocks.FastEndpoints
- `src/BuildingBlocks/Blocks.FastEndpoints/Blocks.FastEndpoints.csproj` — references Blocks.Domain, FastEndpoints package
- `src/BuildingBlocks/Blocks.FastEndpoints/DomainEventPublisher.cs` — implements IDomainEventPublisher via IEvent.PublishAsync(Mode.WaitForAll)

### Fokus Service
- `src/Services/Fokus/CLAUDE.md` — service CLAUDE.md with all axes filled

## Files Modified

### Fokus.Domain
- `src/Services/Fokus/Fokus.Domain/Fokus.Domain.csproj` — added reference to Blocks.Domain
- `src/Services/Fokus/Fokus.Domain/Entities/Sprint.cs` — extends AggregateRoot (int PK); removed explicit Id property (inherited)
- `src/Services/Fokus/Fokus.Domain/Entities/Ticket.cs` — extends AggregateRoot<string>; removed Key property (renamed to inherited Id)
- `src/Services/Fokus/Fokus.Domain/Entities/Developer.cs` — extends Entity<string>; removed AccountId property (renamed to inherited Id)
- `src/Services/Fokus/Fokus.Domain/Entities/StatusTransition.cs` — extends Entity<int>; removed explicit Id (inherited); renamed TicketKey → TicketId
- `src/Services/Fokus/Fokus.Domain/Entities/SprintMembership.cs` — plain class; renamed TicketKey → TicketId
- `src/Services/Fokus/Fokus.Domain/ValueObjects/HealthThresholdConfig.cs` — extends ValueObject; implemented GetEqualityComponents()
- `src/Services/Fokus/Fokus.Domain/ValueObjects/HealthWeightConfig.cs` — extends ValueObject; implemented GetEqualityComponents()

### Fokus.Persistence
- `src/Services/Fokus/Fokus.Persistence/Fokus.Persistence.csproj` — added references to Blocks.EntityFrameworkCore and Blocks.FastEndpoints
- `src/Services/Fokus/Fokus.Persistence/FokusDbContext.cs` — extends ApplicationDbContext<FokusDbContext> instead of DbContext
- `src/Services/Fokus/Fokus.Persistence/Repositories/SprintRepository.cs` — extends RepositoryBase<FokusDbContext, Sprint, int>; removed boilerplate Add/SaveChangesAsync (on base); kept custom UpsertAsync (new keyword) and UpsertMembershipsAsync
- `src/Services/Fokus/Fokus.Persistence/Repositories/TicketRepository.cs` — extends RepositoryBase<FokusDbContext, Ticket, string>; removed boilerplate; renamed Key→Id references; renamed ReplaceTransitionsAsync param ticketKey→ticketId
- `src/Services/Fokus/Fokus.Persistence/Repositories/DeveloperRepository.cs` — extends RepositoryBase<FokusDbContext, Developer, string>; renamed AccountId→Id references; kept custom UpsertAsync (new keyword)
- `src/Services/Fokus/Fokus.Persistence/Configurations/SprintConfiguration.cs` — extends AuditedEntityConfiguration<Sprint>; HasGeneratedId=false (Jira-assigned ID)
- `src/Services/Fokus/Fokus.Persistence/Configurations/TicketConfiguration.cs` — extends AuditedEntityConfiguration<Ticket, string>; HasGeneratedId=false; HasKey now targets Id (was Key)
- `src/Services/Fokus/Fokus.Persistence/Configurations/DeveloperConfiguration.cs` — extends EntityConfiguration<Developer, string>; HasGeneratedId=false; PK now Id (was AccountId)
- `src/Services/Fokus/Fokus.Persistence/Configurations/StatusTransitionConfiguration.cs` — extends EntityConfiguration<StatusTransition>; renamed TicketKey→TicketId in FK and index
- `src/Services/Fokus/Fokus.Persistence/Configurations/SprintMembershipConfiguration.cs` — kept as manual IEntityTypeConfiguration (composite key); renamed TicketKey→TicketId in composite key, FK, and index
- `src/Services/Fokus/Fokus.Persistence/DependencyInjection.cs` — registers IDomainEventPublisher as DomainEventPublisher (scoped), ISaveChangesInterceptor as DispatchDomainEventsInterceptor; wires interceptor into DbContextOptions

### Fokus.API
- `src/Services/Fokus/Fokus.API/Fokus.API.csproj` — added reference to Blocks.AspNetCore
- `src/Services/Fokus/Fokus.API/Program.cs` — registers GlobalExceptionMiddleware before other middleware
- `src/Services/Fokus/Fokus.API/Infrastructure/Jira/Mapping/JiraMapper.cs` — Key→Id in MapTicket; AccountId→Id in MapDeveloper; TicketKey→TicketId in MapMembership and MapStatusTransitions

### Solution
- `src/Fokus.slnx` — added BuildingBlocks folder with 6 new projects

### Migrations
- Deleted old migration `20260505204515_InitialCreate` via `dotnet ef migrations remove`
- Created fresh `20260506192926_InitialCreate` via `dotnet ef migrations add InitialCreate` — includes renamed columns (Ticket.Id, Developer.Id, SprintMembership.TicketId, StatusTransition.TicketId) and new audit columns (CreatedById, CreatedOn, LastModifiedById, LastModifiedOn on Sprint and Ticket)

## Key Decisions

- **IDomainEvent extends IEvent only (not INotification):** Fokus uses FastEndpoints event bus exclusively. The skill shows dual `INotification, IEvent` but the plan explicitly says FastEndpoints-only for Fokus.
- **ApplicationDbContext simplified:** The skill's `GetAllCached`/`GetByIdCached` helpers use `ref` parameters which C# disallows in async methods. Removed those helpers — they are not used by Fokus in v1.
- **AuditedEntityConfiguration no DefaultDateSql:** `HasDefaultValueSql` is a relational EF extension not available in the base `Microsoft.EntityFrameworkCore` package. Since audit fields are nullable and populated by interceptor (when auth is added), no SQL default is configured in v1. Removed the `DefaultDateSql` virtual override.
- **RepositoryBase no TableName property:** `GetTableName()` is also a relational extension. Removed from base — Fokus doesn't use it.
- **UpsertAsync uses `new` keyword:** Domain repos override UpsertAsync with entity-specific field-copy logic (not generic CurrentValues.SetValues). C# requires `new` to suppress CS0108 when intentionally hiding.
- **JiraApiException kept separate:** It uses `System.Net.HttpStatusCode` (not int) and has custom Jira-specific fields. Endpoints handle it with custom logic (401→SendUnauthorizedAsync, others→502). Extending HttpException would require type conversion and lose the JiraMessage field — no benefit.
- **StatusTransition.TicketKey → TicketId:** The plan renames this field. The FK in EF config and the repository query in TicketRepository.ReplaceTransitionsAsync also updated.

## Deviations from Plan

- **Step 3 done before Step 2 (implementation order only):** Blocks.Exceptions was created first since Blocks.Core and Blocks.Domain reference it. The plan lists Step 1 (Core) before Step 3 (Exceptions), but the plan itself states "Blocks.Core references Blocks.Exceptions" — so Exceptions must exist first. No functional deviation.
- **AuditedEntityConfiguration lacks DefaultDateSql:** Plan says "DefaultDateSql virtual returns SQLite-compatible expression". Removed entirely because `HasDefaultValueSql` requires the relational EF package which is not a dependency of `Blocks.EntityFrameworkCore`. Audit fields are nullable — no default needed in v1. When auth is wired, this can be added with the appropriate package reference.
- **ApplicationDbContext has no cache helpers:** Plan says "base with in-memory cache helpers". Removed because the helper pattern uses `ref` parameters which C# prohibits in async methods. Fokus v1 doesn't use in-memory caching.
