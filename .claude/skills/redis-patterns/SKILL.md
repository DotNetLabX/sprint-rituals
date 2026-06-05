---
name: redis-patterns
description: Redis.OM persistence — entity model, repository, index creation, seeding, DI wiring. Loaded when working with Redis-backed services.
user-invocable: true
---

# Redis Patterns (Redis.OM)

## When to Use Redis as Primary DB

Use for: read-heavy, low-complexity domain, shallow entity graph, data fits in memory, sub-millisecond reads needed.
Do NOT use for: deep aggregate hierarchies, complex invariants, ACID transactions across entities, complex reporting, data larger than memory.

Typical fit: 1-2 services per system (reference data, lookups, config, lightweight CRUD).

## Entity Base

**File:** `src/BuildingBlocks/Blocks.Redis/Entity.cs`

```csharp
public class Entity
{
    [RedisIdField]
    [Indexed]
    public int Id { get; set; }
}
```

Does NOT inherit from `AggregateRoot` — Redis entities are simpler. No audit fields, no domain events.

## Entity Decoration

Domain entities use Redis.OM attributes:

```csharp
[Document(StorageType = StorageType.Json, Prefixes = new[] { nameof({Entity}) })]
public partial class {Entity} : Entity
{
    [Indexed] public required string Abbreviation { get; set; }
    [Searchable] public required string Name { get; set; }          // full-text search
    [Indexed(Sortable = true)] public required string NormalizedName { get; set; } // case-insensitive queries
    [Searchable] public required string Description { get; set; }
    public required string Code { get; set; }
    public int OwnerId { get; set; }
    [Indexed(JsonPath = "$.Name")] public List<{ChildEntity}> {Children} { get; set; } = new();
    public int ItemCount { get; set; }
}
```

**Attribute guide:**
- `[Indexed]` — equality/range queries
- `[Searchable]` — full-text search
- `[Indexed(Sortable = true)]` — sortable field
- `[Indexed(JsonPath = "$.Name")]` — index nested object property

**Storage types:** `StorageType.Json` (complex objects) vs `StorageType.Hash` (flat objects).

**NormalizedName trick:** Set in property setter for case-insensitive LINQ queries without full-text search overhead.

## Repository<T>

**File:** `src/BuildingBlocks/Blocks.Redis/Repository.cs`

Generic repository, open-generic DI registration:

```csharp
public class Repository<T> where T : Entity
```

Key operations:
- `GetById(int)` / `GetByIdAsync(int)` / `GetByIdOrThrowAsync(int)`
- `Exists(int)` via `AnyAsync`
- `AddAsync(T)` — auto-assigns ID via Redis `INCR` if `Id == 0`, rejects negative IDs
- `UpdateAsync(T)` — standard Redis.OM update
- `ReplaceAsync(T)` — **critical workaround** (see below)
- `DeleteAsync(T)`
- `GenerateNewId()` / `GenerateNewId<TOther>()` — atomic `INCR` on `{TypeName}:Id:Sequence`
- `Collection` — exposes raw `IRedisCollection<T>` for advanced queries

### ReplaceAsync Workaround

**Redis.OM does not properly update nested child collections.** `UpdateAsync` silently fails to persist changes to child lists.

`ReplaceAsync` = delete + re-insert. Use it whenever saving a parent entity with modified child collections:

```csharp
// UpdateAsync does NOT work for children collections
await _repository.ReplaceAsync(entity); // delete + insert
```

Use `UpdateAsync` ONLY for scalar-only changes (e.g., incrementing a counter).

**Known trade-off:** Delete + insert is not atomic. There's a brief window where the entity doesn't exist. Acceptable for low-write-frequency admin operations, risky for high-concurrency writes.

## Extensions

**File:** `src/BuildingBlocks/Blocks.Redis/Extensions.cs`

- `GetByIdOrThrowAsync<T>(collection, id)` — throws `NotFoundException`
- `GenerateNewId<TEntity>(redisDb)` — for use in seeding
- `SetSequenceSeed<TEntity>(redisDb, startValue)` — reset ID counter after seeding
- `SeedFromJson<TEntity>(provider, redisDb)` — reads `Data/Test/{EntityName}.json`, idempotent via `collection.Any()` check

## Multi-Collection Accessor

**File:** `src/Services/{Svc}/{Svc}.Persistence/{Svc}DbContext.cs`

Lightweight typed accessor for read endpoints that query multiple collections:

```csharp
public class {Svc}DbContext
{
    public IRedisCollection<{Entity}> {Entities} => _provider.RedisCollection<{Entity}>();
    public IRedisCollection<{OtherEntity}> {OtherEntities} => _provider.RedisCollection<{OtherEntity}>();
    public RedisConnectionProvider Provider => _provider;
}
```

Use `{Svc}DbContext` for multi-collection reads. Use `Repository<T>` for single-entity writes.

## DI Registration

**File:** `src/Services/{Svc}/{Svc}.Persistence/DependencyInjection.cs`

```csharp
var connectionString = config.GetConnectionString("Database")!;

services.AddSingleton(new RedisConnectionProvider(connectionString));
// Strip redis:// or rediss:// scheme for StackExchange.Redis
var redisConnectionString = connectionString.StartsWith("redis://") || connectionString.StartsWith("rediss://")
    ? connectionString.Substring(connectionString.IndexOf("://") + 3)
    : connectionString;
var redis = ConnectionMultiplexer.Connect(redisConnectionString);
services.AddSingleton<IConnectionMultiplexer>(redis);

services.AddSingleton<{Svc}DbContext>();
services.AddScoped(typeof(Repository<>)); // open-generic — all entity types resolved automatically
```

## Index Creation Middleware

**File:** `src/Services/{Svc}/{Svc}.API/AppBuilderExtensions.cs`

```csharp
public static IApplicationBuilder UseRedis(this IApplicationBuilder app)
{
    using var scope = app.ApplicationServices.CreateScope();
    var provider = scope.ServiceProvider.GetRequiredService<RedisConnectionProvider>();
    provider.Connection.CreateIndex(typeof({Entity}));
    return app;
}
```

Called at startup in `Program.cs`: `app.UseRedis()`. Only index top-level collections — nested documents are NOT indexed separately.

## Seeding

**File:** `src/Services/{Svc}/{Svc}.Persistence/Data/Seed.cs`

```csharp
public static async Task SeedTestData(this IHost host)
{
    using var scope = host.Services.CreateScope();
    var provider = scope.ServiceProvider.GetRequiredService<RedisConnectionProvider>();
    var redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
    await provider.SeedFromJson<{Entity}>(redis.GetDatabase());
    await redis.GetDatabase().SetSequenceSeed<{Entity}>(7);
    await redis.GetDatabase().SetSequenceSeed<{ChildEntity}>(14);
}
```

Always call `SetSequenceSeed` after seeding to advance the ID counter past seeded record IDs.
