using Microsoft.AspNetCore.Authorization;

namespace Reflekt.API.Features.Health;

[AllowAnonymous]
[HttpGet("/health")]
[Tags("Health")]
public sealed class HealthEndpoint : EndpointWithoutRequest
{
    public override Task HandleAsync(CancellationToken ct) => Send.OkAsync(ct);
}
