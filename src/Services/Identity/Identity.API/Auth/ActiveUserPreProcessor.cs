using System.Security.Claims;
using System.Text.Json;
using FastEndpoints;
using Identity.Persistence.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.API.Auth;

public class ActiveUserPreProcessor : IGlobalPreProcessor
{
    public async Task PreProcessAsync(IPreProcessorContext context, CancellationToken ct)
    {
        var httpContext = context.HttpContext;

        // Scope the gate off modules whose endpoints authenticate non-User participants (RA2/ADR-016,
        // Q4 mechanism A). Resolve the matched endpoint's declaring assembly and skip when excluded —
        // BEFORE the user lookup, so an authenticated-but-not-a-User Reflekt request is never 401'd.
        var endpointAssemblyName = httpContext.GetEndpoint()?
            .Metadata.GetMetadata<EndpointDefinition>()?
            .EndpointType.Assembly.GetName().Name;
        if (ActiveUserGateScope.IsExcludedModule(endpointAssemblyName))
            return;

        if (httpContext.User.Identity?.IsAuthenticated != true)
            return;

        var userIdStr = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId))
            return;

        var userRepository = httpContext.RequestServices.GetRequiredService<UserRepository>();
        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is not null && user.IsActive)
            return;

        var message = user is null ? "Account not found." : "Your account has been deactivated.";

        httpContext.Response.StatusCode = 401;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            statusCode = 401,
            message
        }), ct);

        await httpContext.Response.CompleteAsync();
        return;
    }
}
