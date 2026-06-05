# Create Aggregate — EF Core

Used by: EF Core services

## State File: `{Service}.Domain/{AggregateName}/{AggregateName}.cs`

**Reference:** `src/Services/{Svc}/{Svc}.Domain/Entities/{Aggregate}.cs`

```csharp
public partial class {AggregateName} : AggregateRoot
{
    // Required properties
    public required string {Property} { get; init; }

    // Backing collections (private list, public readonly)
    private readonly List<{ChildEntity}> _{items} = new();
    public IReadOnlyList<{ChildEntity}> {Items} => _{items}.AsReadOnly();

    // Navigation properties
    public int {ForeignKeyId} { get; init; }
}
```

## Behavior File: `{Service}.Domain/{AggregateName}/Behaviors/{AggregateName}.cs`

**Reference:** `src/Services/{Svc}/{Svc}.Domain/Behaviors/{Aggregate}.cs`

```csharp
public partial class {AggregateName}
{
    public void {DomainMethod}({Parameters}, TAction action)
    {
        // Validate business rules
        // Mutate state
        AddDomainEvent(new {EventName}(this, action));
    }
}
```

**Convention:** action parameter is preferred last.

## Folder Structure

Two layouts exist. **Aggregate-grouped** (recommended for new aggregates) is used by services with multiple aggregates:

```
{Service}.Domain/
├── {AggregateName}/
│   ├── {AggregateName}.cs             (state)
│   ├── Behaviors/
│   │   └── {AggregateName}.cs         (behavior — partial class)
│   ├── Events/
│   │   └── {EventName}.cs            (domain event records)
│   └── ValueObjects/
│       └── {ValueObjectName}.cs      (value objects)
```

Single-aggregate services may use a **flat layout** with top-level folders (Entities/, Behaviors/, Events/, ValueObjects/) instead of grouping by aggregate.

## EF Configuration: `{Service}.Persistence/EntityConfigurations/{AggregateName}EntityConfiguration.cs`

```csharp
internal class {AggregateName}EntityConfiguration : AuditedEntityConfiguration<{AggregateName}>
{
    // Override if needed:
    // protected override bool HasGeneratedId => false;        // for natural keys
    // protected override string DefaultDateSql => "NOW()..."; // for PostgreSQL
    // protected override bool HasConcurrencyToken => true;    // for optimistic concurrency

    public override void Configure(EntityTypeBuilder<{AggregateName}> builder)
    {
        base.Configure(builder);
        builder.Property(e => e.{Property}).HasMaxLength(MaxLength.C64).IsRequired();
        // OwnsOne for value objects
        builder.OwnsOne(e => e.{ValueObject}, b => { b.Property(v => v.Value).HasMaxLength(MaxLength.C256); });
    }
}
```

## Repository: `{Service}.Persistence/Repositories/{AggregateName}Repository.cs`

Only create if the aggregate needs custom queries beyond the base `Repository<T>`:

```csharp
public class {AggregateName}Repository({DbContext} context) : Repository<{AggregateName}>(context)
{
    public async Task<{AggregateName}> GetWith{Children}Async(int id) =>
        await Query().Include(a => a.{Children}).FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new NotFoundException(nameof({AggregateName}));
}
```

Registration: auto via assembly scan (`AddDerivedTypesOf(typeof(Repository<>))`) or manual `AddScoped<{AggregateName}Repository>()` per repository. Check the service's CLAUDE.md for which approach to use.
