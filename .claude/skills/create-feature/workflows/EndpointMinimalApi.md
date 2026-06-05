# Create Endpoint — Minimal APIs

Used by: Minimal API + MediatR services

## Inputs
- Feature name, route path, HTTP method
- Request/response type names
- MediatR command/query type to dispatch to

## Outputs
- `{Svc}.API/Endpoints/{FeatureName}Endpoint.cs`
- Registration entry in `EndpointRegistration.MapAllEndpoints()`

## Gate
Proceed only after: service CLAUDE.md confirms Minimal APIs + MediatR, Handler.md workflow followed first.

Static class with `Map(IEndpointRouteBuilder)` extension method. Dispatches to MediatR handler.

## Pattern

**Reference:** `src/Services/{Svc}/{Svc}.API/Endpoints/{FeatureName}Endpoint.cs`

Create `{FeatureName}Endpoint.cs` in `Endpoints/`:

```csharp
public static class {FeatureName}Endpoint
{
    public static void Map(this IEndpointRouteBuilder app)
    {
        app.Map{Method}("/{domain}/{route}", async ([{FromRoute}] int id, {CommandType} command, ISender sender) =>
        {
            var result = await sender.Send(command with { Id = id });
            return Results.Ok(result);
        })
        .WithTags("{Domain}")
        .WithName("{FeatureName}")
        .Produces<{ResponseType}>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireRoleAuthorization(Role.{RequiredRole});
    }
}
```

## Registration — MANUAL

Add to `EndpointRegistration.cs`:

```csharp
public static class EndpointRegistration
{
    public static IEndpointRouteBuilder MapAllEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        // existing endpoints...
        {FeatureName}Endpoint.Map(api); // ADD THIS LINE

        return app;
    }
}
```

## Handler Location

The endpoint dispatches to a handler in the Application project:
- `{Service}.Application/Features/{Domain}/{FeatureName}/{FeatureName}CommandHandler.cs`

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

Never write `catch → Results.Problem(statusCode)` in an endpoint.

## File Upload Variant

For file uploads, use `[FromForm]` + `.DisableAntiforgery()`:

```csharp
app.MapPost("/{domain}/{entityId}/files",
    async ([FromRoute] int entityId, [FromForm] {UploadCommand} command, ISender sender) => { ... })
    .DisableAntiforgery();
```
