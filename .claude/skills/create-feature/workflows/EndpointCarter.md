# Create Endpoint — Carter

Used by: Carter + MediatR services

## Inputs
- Feature name, route path, HTTP method
- Request/response type names
- MediatR command/query type to dispatch to

## Outputs
- `{Svc}.API/Endpoints/{Domain}/{FeatureName}Endpoint.cs`

## Gate
Proceed only after: service CLAUDE.md confirms Carter + MediatR, Handler.md workflow followed first.

Class implementing `ICarterModule` with fluent route building. Dispatches to MediatR handler.

## Pattern

**Reference:** `src/Services/{Svc}/{Svc}.API/Endpoints/{Domain}/{FeatureName}Endpoint.cs`

Create `{FeatureName}Endpoint.cs` in `Endpoints/{Domain}/`:

```csharp
public class {FeatureName}Endpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
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

## Handler Location

The endpoint dispatches to a handler in the Application project:
- `{Service}.Application/Features/{Domain}/{FeatureName}/{FeatureName}CommandHandler.cs`

## Registration

Carter auto-discovers modules via `AddCarter()` + `app.MapCarter()` — no manual registration.

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

## Notes

- Route parameters bind via `[FromRoute]`, body via `[FromBody]` or parameter name matching
- Multiple routes can be defined in a single `ICarterModule`
- Group-level authorization: apply `.RequireAuthorization()` on the route
