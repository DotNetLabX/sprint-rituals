# Create Integration Event Contract

## Pattern

**Reference:** `src/BuildingBlocks/{ProjectName}.Integration.Contracts/{Domain}/{EventName}Event.cs`

Create in `BuildingBlocks/{ProjectName}.Integration.Contracts/{Domain}/`:

```csharp
public record {EventName}Event({DtoType} {DtoName});
```

### DTO (included in same file or separate)

```csharp
public record {DtoType}(
    int Id,
    string {Property},
    // ... all data the consumer needs
);
```

## Rules

- Integration events are **records** (immutable)
- Include ALL data the consumer needs — consumers should NOT call back to the publisher service
- Use simple types (int, string, DateTime) — no domain types in contracts
- Event naming: `{What Happened}Event` (past tense) — e.g., `OrderApprovedForFulfillmentEvent`
- DTOs carry the snapshot of state at the time of the event
