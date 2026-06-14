// RA1: unmapped pending Identity unification (RA2, ADR-016).
// This endpoint wrote a Reflekt User from the Google OAuth callback.
// Authentication is now Identity-owned; login endpoints removed from Reflekt's route surface.
// Route placeholder satisfies FastEndpoints discovery without exposing a real path.
using Microsoft.AspNetCore.Authorization;

namespace Reflekt.API.Features.Auth.Login;

[AllowAnonymous]
public sealed class LoginEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        // RA1: unmapped pending Identity unification (RA2, ADR-016)
        Get("_ra1/unmapped/auth/login");
        DontThrowIfValidationFails();
    }

    public override Task HandleAsync(CancellationToken ct) => Task.CompletedTask;
}
