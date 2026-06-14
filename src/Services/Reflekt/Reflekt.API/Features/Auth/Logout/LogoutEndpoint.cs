// RA1: unmapped pending Identity unification (RA2, ADR-016).
// Cookie logout is owned by Identity in the monolith.
using Microsoft.AspNetCore.Authorization;

namespace Reflekt.API.Features.Auth.Logout;

[AllowAnonymous]
public sealed class LogoutEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        // RA1: unmapped pending Identity unification (RA2, ADR-016)
        Post("_ra1/unmapped/auth/logout");
        DontThrowIfValidationFails();
    }

    public override Task HandleAsync(CancellationToken ct) => Task.CompletedTask;
}
