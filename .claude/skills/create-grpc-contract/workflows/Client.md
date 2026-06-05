# Register gRPC Client

## Pattern

**Reference:** `src/BuildingBlocks/Blocks.AspNetCore/Grpc/GrpcClientRegistrationExtensions.cs`

In the consuming service's `DependencyInjection.cs`:

```csharp
var grpcOptions = config.GetSectionByTypeName<GrpcServicesOptions>();
services.AddCodeFirstGrpcClient<I{ServiceName}Service>(grpcOptions, "{ServiceName}");
```

Configuration in `appsettings.json`:

```json
{
  "GrpcServicesOptions": {
    "Retry": {
      "Count": 3,
      "InitialDelayMs": 2
    },
    "Services": {
      "{ServiceName}": {
        "Url": "https://{service}-api:8081",
        "EnableRetry": true
      }
    }
  }
}
```

## Usage in Handlers/Endpoints

Inject the interface directly. **Always pass `CallOptions` with the cancellation token:**

```csharp
public class {FeatureName}Handler(I{ServiceName}Service _{serviceName}Service)
{
    public async Task Handle(..., CancellationToken ct)
    {
        var response = await _{serviceName}Service.{Method}Async(
            new {Request} { Id = id },
            new CallOptions(cancellationToken: ct));
        // use response...
    }
}
```

## Notes

- Client is registered via `AddCodeFirstGrpcClient<T>()` extension from `Blocks.AspNetCore`
- Config-driven service discovery — URLs come from `GrpcServicesOptions.Services` section in appsettings
- No code generation, no .proto files — the shared `[ServiceContract]` interface is the contract
