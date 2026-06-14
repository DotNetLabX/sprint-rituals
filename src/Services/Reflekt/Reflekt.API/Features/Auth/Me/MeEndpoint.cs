// RA1: unmapped pending Identity unification (RA2, ADR-016).
// /auth/me will be served by Identity.
namespace Reflekt.API.Features.Auth.Me;

public sealed record MeResponse(string Email, string DisplayName, string GivenName);

[Microsoft.AspNetCore.Authorization.AllowAnonymous]
public sealed class MeEndpoint : EndpointWithoutRequest<MeResponse>
{
    public override void Configure()
    {
        // RA1: unmapped pending Identity unification (RA2, ADR-016)
        Get("_ra1/unmapped/auth/me");
        DontThrowIfValidationFails();
    }

    public override Task HandleAsync(CancellationToken ct) => Task.CompletedTask;
}
