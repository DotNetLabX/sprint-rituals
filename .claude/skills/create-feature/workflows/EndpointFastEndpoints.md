# Create Endpoint — FastEndpoints

Used by: FastEndpoints services

## Inputs
- Service name and domain area (from plan step)
- Feature name — drives all file names
- Route path and HTTP method
- Request/response type names and properties
- Required role (if auth-gated) or AllowAnonymous

## Outputs
- `{Svc}.API/Features/{Domain}/{FeatureName}/{FeatureName}Endpoint.cs`
- Optionally: `{FeatureName}Endpoint.Configure.cs` (if configuration is complex)

## Gate
Proceed only after: service CLAUDE.md read, endpoint framework confirmed as FastEndpoints, one existing feature in the service reviewed for structure.

Class extending `Endpoint<TRequest, TResponse>`. Handler logic lives directly in `HandleAsync` — no MediatR dispatch.

## Pattern

**Reference:** `src/Services/{Svc}/{Svc}.API/Features/{Domain}/{FeatureName}/{FeatureName}Endpoint.cs`

Create `{FeatureName}Endpoint.cs` in `Features/{Domain}/{FeatureName}/`:

```csharp
[Authorize(Roles = Role.{RequiredRole})]
[Http{Method}("{route}")]
[Tags("{Domain}")]
public class {FeatureName}Endpoint({Dependencies})
    : Endpoint<{CommandType}, {ResponseType}>
{
    public override async Task HandleAsync({CommandType} command, CancellationToken ct)
    {
        // Load aggregate/data
        // Execute domain logic
        // Save changes

        await SendOkAsync(new {ResponseType}(...), ct);
    }
}
```

## Optional: Custom Base Endpoint

If the service defines a custom base endpoint (check service CLAUDE.md), extend it instead of `Endpoint<TReq, TRes>`:

```csharp
public class {FeatureName}Endpoint({Dependencies})
    : BaseEndpoint<{CommandType}, {ResponseType}>
{
    public override async Task HandleAsync({CommandType} command, CancellationToken ct)
    {
        // ...
    }
}
```

## Registration

FastEndpoints auto-discovers endpoints — no manual registration needed.

## Alternative: Configure() partial class split

For complex configuration, split into two partial files:

**`{FeatureName}Endpoint.Configure.cs`:**
```csharp
public partial class {FeatureName}Endpoint
{
    public override void Configure()
    {
        AllowAnonymous(); // or Roles(...)
        Post("{route}");
        Description(x => x.WithSummary("...").WithTags("{Domain}"));
    }
}
```

## Error Handling

**Do not catch exceptions to return HTTP responses.** The `GlobalExceptionMiddleware` handles exception-to-status-code mapping — endpoints must not duplicate that responsibility.

The only acceptable catch in an endpoint is a domain exception where you need to perform a side effect (e.g. logging, cleanup) before re-throwing:

```csharp
catch (SomeDomainException ex)
{
    // side effect only — then re-throw for the middleware
    throw;
}
```

Never write `catch → SendAsync(statusCode)` or `catch → SendUnauthorizedAsync()` in an endpoint.

## Notes

- If the service defines a custom base validator (check service CLAUDE.md), extend it instead of `Validator<T>`
- Use `SendOkAsync()` for all FastEndpoints responses — the pattern used across all services
- Domain events: dispatched automatically via interceptor on SaveChanges, or via `await PublishAsync(new {Event}(...))` for FastEndpoints events

Check the service's CLAUDE.md for which variant to use.
