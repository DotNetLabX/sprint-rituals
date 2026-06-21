using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Poker.Persistence.Repositories;

namespace Poker.API.Auth;

/// <summary>
/// Authenticates Poker guests via a session-scoped token stored in localStorage.
/// Reads from X-Guest-Token header → ?access_token query → Authorization: Bearer.
/// Distinct from Reflekt's GuestTokenAuthenticationHandler (Rule 23 / ADR-016).
/// Full implementation: Step 6.
/// </summary>
public class PokerGuestTokenAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    SessionParticipantRepository participantRepository)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = Request.Headers["X-Guest-Token"].FirstOrDefault()
            ?? Request.Query["access_token"].FirstOrDefault()
            ?? ExtractBearerToken(Request.Headers.Authorization.FirstOrDefault());

        if (token is null)
            return AuthenticateResult.NoResult();

        var participant = await participantRepository.GetByGuestTokenAsync(token, Context.RequestAborted);
        if (participant is null)
            return AuthenticateResult.Fail("Invalid Poker guest token");

        var claims = new[]
        {
            new Claim("participantId", participant.Id.ToString()),
            new Claim("sessionId", participant.SessionId.ToString()),
            new Claim("displayName", participant.DisplayName),
        };
        var identity = new ClaimsIdentity(claims, PokerGuestTokenDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }

    private static string? ExtractBearerToken(string? authorizationHeader)
    {
        if (string.IsNullOrEmpty(authorizationHeader)) return null;
        const string prefix = "Bearer ";
        return authorizationHeader.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? authorizationHeader[prefix.Length..]
            : null;
    }
}
