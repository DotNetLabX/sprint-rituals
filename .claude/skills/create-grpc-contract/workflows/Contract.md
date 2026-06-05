# Create gRPC Contract

Code-first contracts using `protobuf-net.Grpc` — no .proto files.

## Pattern

**Reference:** `src/BuildingBlocks/{ProjectName}.Grpc.Contracts/{ServiceName}/{Entity}Contracts.cs`

Create in `BuildingBlocks/{ProjectName}.Grpc.Contracts/{ServiceName}/`:

### Service Interface

```csharp
[ServiceContract]
public interface I{ServiceName}Service
{
    [OperationContract]
    ValueTask<{Response}> {Method}Async({Request} request, CallContext context = default);
}
```

**Must use `ValueTask<T>` and `CallContext context = default`** — this is what protobuf-net.Grpc expects.

### Request/Response DTOs

```csharp
[ProtoContract]
public class {Request}
{
    [ProtoMember(1)] public int Id { get; set; }
}

[ProtoContract]
public class {Response}
{
    [ProtoMember(1)] public int Id { get; set; }
    [ProtoMember(2)] public string Name { get; set; } = default!;
}
```

## Rules

- `[ProtoMember(n)]` numbers must be unique and sequential per class
- Use `= default!` for non-nullable reference properties
- Request/response classes must have parameterless constructors (protobuf-net requirement)
- Collections: use `List<T>` (not arrays) with `[ProtoMember]`
- Nested types: also need `[ProtoContract]` decoration
- Optional fields: `[ProtoMember(N, IsRequired = false)]`
- Enums: need both `[ProtoContract]` and `[ProtoEnum]` on members
