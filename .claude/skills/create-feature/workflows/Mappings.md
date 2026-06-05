# Create Mappings (Mapster)

## Inputs
- Source type (domain entity or command)
- Destination type (response DTO or view model)
- Any custom property mappings needed (non-default names, computed values)

## Outputs
- Entry in `{Svc}.Application/Mappings/{Domain}Mappings.cs` (new `IRegister` class or added config)

## Gate
Proceed only after: source and destination types are defined. Verify an existing `{Domain}Mappings.cs` doesn't already cover this mapping.

## Pattern

**Reference:** `src/Services/{Svc}/{Svc}.Application/Mappings/{Domain}Mappings.cs`

Mapster configs are auto-discovered via `IRegister` interface + assembly scan:

```csharp
public class {Domain}Mappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<{Source}, {Destination}>()
            .Map(dest => dest.{Prop}, src => src.{OtherProp});
    }
}
```

## Registration

**File:** `src/BuildingBlocks/Blocks.Core/Mapster/DependencyInjection.cs`

Auto-discovered — no manual registration:
```csharp
services.AddMapsterConfigsFromAssemblyContaining<GrpcMappings>();
// or: services.AddMapsterConfigsFromCurrentAssembly();
```

## Inline Mapping (No Config Needed)

For simple cases where property names match:
```csharp
var response = entity.Adapt<{ResponseType}>();
var entity = command.Adapt<{Entity}>();
```

## Post-Mapping Actions

Use `AdaptWith<T>()` for mutations after mapping:
```csharp
var dto = entity.AdaptWith<EditorDto>(dest =>
{
    dest.Role = editorRole.EditorRole;
});
```

## Records

For immutable record types, use `MapToConstructor()`:
```csharp
config.NewConfig<Source, DestinationRecord>().MapToConstructor();
```

## Location

Mapping configs in Application project: `{Service}.Application/Mappings/{Domain}Mappings.cs`
