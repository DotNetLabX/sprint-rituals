# Create gRPC Server

## Pattern

**Reference:** `src/Services/{Svc}/{Svc}.API/Features/{Domain}/{ServiceName}GrpcService.cs`

Create in `{Service}.API/Features/{Domain}/{ServiceName}GrpcService.cs`:

```csharp
public class {ServiceName}GrpcService({Dependencies}) : I{ServiceName}Service
{
    public async ValueTask<{Response}> {Method}Async({Request} request, CallContext context = default)
    {
        var entity = await _repository.FindByIdOrThrowAsync(request.Id);
        return entity.Adapt<{Response}>();
    }
}
```

**Must match the contract signature:** `ValueTask<T>`, `CallContext context = default`.

## Registration in Program.cs

```csharp
// In service configuration:
builder.Services.AddCodeFirstGrpc(options =>
{
    options.ResponseCompressionLevel = System.IO.Compression.CompressionLevel.Fastest;
    options.EnableDetailedErrors = true;
});

// In endpoint mapping:
app.MapGrpcService<{ServiceName}GrpcService>();
```

## Notes

- gRPC services are plain classes implementing the contract interface
- Use Mapster for entity → response mapping
- Throw `NotFoundException` for missing entities — middleware maps to gRPC status codes
