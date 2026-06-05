---
name: persistence-patterns
description: EF Core persistence — repository pattern, entity configurations, DbContext setup, seed data, interceptors. Loaded when working with database entities or persistence infrastructure.
user-invocable: true
---

# Persistence Patterns (EF Core)

## Repository Pattern (3-Tier)

### Tier 1: Interface + Base
**Files:**
- `src/BuildingBlocks/Blocks.EntityFrameworkCore/Repositories/IRepository.cs`
- `src/BuildingBlocks/Blocks.EntityFrameworkCore/Repositories/Repository.cs`

`IRepository<TEntity, TKey>` — comprehensive interface: CRUD, batch, query, save.
`RepositoryBase<TContext, TEntity, TKey>` — generic EF implementation with smart defaults.

Key interface methods: `Query()` (returns `IQueryable`), `AddAsync()`, `SaveChangesAsync()`, `DeleteByIdAsync()` (raw SQL for required-property entities).
Additional `RepositoryBase` methods (not on `IRepository`): `UpsertAsync()`, `FindByIdAsync()`, `TableName`.

### Tier 2: Service-Level Repository
Each service defines a thin concrete `Repository<TEntity>` binding to its DbContext:
```csharp
public class Repository<TEntity>({Svc}DbContext dbContext)
    : RepositoryBase<{Svc}DbContext, TEntity>(dbContext)
    where TEntity : class, IEntity<int>;
```

### Tier 3: Domain-Specific Repository
Custom queries with eager loading:
```csharp
public class {Entity}Repository({Svc}DbContext dbContext) : Repository<{Entity}>(dbContext)
{
    public override IQueryable<{Entity}> Query()
    {
        return base.Entity
            .Include(e => e.{Navigation})
                .ThenInclude(e => e.{ChildNavigation})
            .Include(e => e.{OtherNavigation});
    }

    public async Task<{Entity}?> GetFullByIdAsync(int id, CancellationToken ct = default)
    {
        return await Query()
            .Include(e => e.{AdditionalNavigation})
            .SingleOrDefaultAsync(e => e.Id == id, ct);
    }
}
```

### Extension: FindByIdOrThrowAsync
**File:** `src/BuildingBlocks/Blocks.EntityFrameworkCore/Extensions/RepositoryExtensions.cs`

Shared extension — do NOT reimplement per service:
```csharp
await repository.FindByIdOrThrowAsync(id);
```

### Repository Registration Variants

| Variant | How | When to use |
|---------|-----|-------------|
| Auto (assembly scan) | `AddDerivedTypesOf(typeof(Repository<>))` | Services with many repositories |
| Manual | `AddScoped<{Entity}Repository>()` per repo | Services with few repositories or explicit control needed |

## Entity Configuration

### Base: EntityConfiguration<T>
**File:** `src/BuildingBlocks/Blocks.EntityFrameworkCore/EntityConfigurations/EntityConfiguration.cs`

- `HasKey(e => e.Id)` + auto-calls `builder.SeedFromJsonFile()`
- `HasGeneratedId` virtual (default `true`) — override to `false` for natural keys

### Audited: AuditedEntityConfiguration<T>
**File:** `src/BuildingBlocks/Blocks.EntityFrameworkCore/EntityConfigurations/AuditedEntityConfiguration.cs`

Extends `EntityConfiguration<T>`. Three opt-in override points:
- `HasGeneratedId` — `ValueGeneratedOnAdd()` vs `ValueGeneratedNever()`
- `DefaultDateSql` — default `"GETUTCDATE()"`, override `"NOW() AT TIME ZONE 'UTC'"` for PostgreSQL. **Requires** `Microsoft.EntityFrameworkCore.Relational` package — `HasDefaultValueSql()` is not in the base EF Core package.
- `HasConcurrencyToken` — default `false`, set `true` for opt-in `RowVersion` shadow property

```csharp
public class {Entity}EntityConfiguration : AuditedEntityConfiguration<{Entity}>
{
    protected override bool HasConcurrencyToken => true;

    public override void Configure(EntityTypeBuilder<{Entity}> builder)
    {
        base.Configure(builder);
        // custom configuration...
    }
}
```

### Value Object Mapping
Two approaches:
- `OwnsOne` (owned entity type): `builder.OwnsOne(p => p.Email, ...)`
- `ComplexProperty` (complex type): `builder.ComplexProperty(a => a.Email, ...)`

## DbContext Setup

**File:** `src/BuildingBlocks/Blocks.EntityFrameworkCore/ApplicationDbContext.cs`

- `ApplicationDbContext<TDbContext>` base with in-memory cache helpers (`GetAllCached`, `GetByIdCached`). **Warning:** these methods use `ref` parameters internally — they cannot be called from `async` methods. Call them from synchronous code paths or extract the cached value before entering an async context.

**Variants:**
- `ApplicationDbContext<T>` — most services
- `IdentityDbContext<User, Role, int>` — services using ASP.NET Identity

**Database engine variants:**
- SQL Server: `UseSqlServer(connectionString)` — default
- PostgreSQL: `UseNpgsql(connectionString)` — opt-in per service
- SQLite: `UseSqlite(connectionString)` — lightweight single-node

## Seed Data (Dual Pattern)

### Master Data (schema-deployed via migrations)
**Files:**
- `src/BuildingBlocks/Blocks.EntityFrameworkCore/Extensions/EntityTypeBuilderExtensions.cs`

Convention: drop a `Data/Master/{EntityName}.json` file → `EntityConfiguration` base auto-calls `builder.SeedFromJsonFile()` → becomes EF `HasData` → included in migrations. Zero code changes needed.

### Test/Dev Data (runtime, behind IsDevelopment guard)
**Files:**
- `src/BuildingBlocks/Blocks.EntityFrameworkCore/Extensions/DbContextExtensions.Seed.cs`
- `src/BuildingBlocks/Blocks.EntityFrameworkCore/Seeding/ManualGenerateIdScope.cs`
- `src/Services/{Svc}/*/Data/Test/Seed.cs` per service

Pattern per service:
```csharp
public static class Seed
{
    public static void SeedTestData(this IServiceProvider services)
    {
        services.SeedTestData<{Svc}DbContext>(context =>
        {
            context.SeedFromJsonFile<{Entity}>();
        });
    }
}
```

- Idempotent: checks `if (context.Set<T>().Any()) return`
- `ManualGenerateIdScope` wraps `SET IDENTITY_INSERT ON/OFF` when JSON has explicit IDs
- JSON file naming: `typeof(T).Name` must match filename exactly

**IMPORTANT:** JSON files must be marked in `.csproj`:
```xml
<ItemGroup>
  <None Update="Data\**\*.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

### Seed Variants

| Variant | Mechanism | When to use |
|---------|-----------|-------------|
| EF JSON | `context.SeedFromJsonFile<T>()` | EF Core services (default) |
| Identity custom | `UserManager.CreateAsync()` (can't bypass Identity) | Services using ASP.NET Identity |
| Redis JSON | `provider.SeedFromJson<T>(redisDb)` | Redis-backed services |

### Wiring in Program.cs
```csharp
app.Migrate<{Svc}DbContext>();
if (app.Environment.IsDevelopment())
    app.Services.SeedTestData();
```

## Interceptors

Registered in `Persistence/DependencyInjection.cs`:
```csharp
services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();
```

See `domain-patterns` skill for interceptor variant details (standard vs transactional).
