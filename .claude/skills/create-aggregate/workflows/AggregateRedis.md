# Create Aggregate — Redis

Used by: Redis-backed services

Redis entities do NOT use `AggregateRoot`. No audit fields, no domain events from base class.

## Entity File: `{Service}.Domain/{EntityName}/{EntityName}.cs`

**Reference:** `src/Services/{Svc}/{Svc}.Domain/{Aggregate}/{Aggregate}.cs`

```csharp
[Document(StorageType = StorageType.Json, Prefixes = new[] { nameof({EntityName}) })]
public partial class {EntityName} : Entity
{
    [Indexed] public string Name { get; set; }
    [Indexed(Sortable = true)] public string NormalizedName { get; set; } // case-insensitive queries
    [Searchable] public string Description { get; set; } // full-text search

    // Nested child collections
    public List<{ChildEntity}> {Children} { get; set; } = new();
}
```

## Attribute Guide

- `[Indexed]` — equality/range queries
- `[Searchable]` — full-text search
- `[Indexed(Sortable = true)]` — sortable field
- `[Indexed(JsonPath = "$.Name")]` — index nested object property
- `StorageType.Json` for complex objects, `StorageType.Hash` for flat objects

## Child Entity (Nested Document)

```csharp
[Document(StorageType = StorageType.Json)]
public class {ChildEntity} : Entity
{
    [Indexed] public string Name { get; set; }
}
```

Note: `[Indexed(JsonPath = "$.Name")]` goes on the **parent entity's collection property**, not on the child. For example, the parent declares `[Indexed(JsonPath = "$.Name")] public List<{ChildEntity}> {Children} { get; set; }` to index the nested `Name` field.

## Registration

No repository subclass needed — `Repository<T>` is open-generic:
```csharp
services.AddScoped(typeof(Repository<>));
```

Add index creation in `AppBuilderExtensions.UseRedis()`:
```csharp
provider.Connection.CreateIndex(typeof({EntityName}));
```

Only index top-level collections — nested documents stored inside parent JSON are NOT indexed separately.

## Key Differences from EF Core

- No `AggregateRoot` base, no audit fields, no domain event queue
- No migrations — schema is defined by attributes
- `ReplaceAsync` for parent entities with child collections (not `UpdateAsync`)
- ID generation via Redis `INCR` (auto in `AddAsync` if `Id == 0`)
- Domain events: use `IDomainEvent` directly, dispatched via FastEndpoints `IEventHandler`
