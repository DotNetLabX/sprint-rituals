using System.Security.Claims;

namespace Reflekt.API.Auth;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// The Identity <c>Person</c> id of the authenticated suite member, read from the <c>person_id</c>
    /// cookie claim (RA2/ADR-016). Reflekt ownership (Team/Sprint) keys on Person id, not the User id.
    /// Throws if the claim is absent — call only on cookie-authenticated suite-member paths.
    /// </summary>
    public static int GetPersonId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue("person_id")
            ?? throw new InvalidOperationException("person_id claim is missing.");
        return int.Parse(value);
    }

    /// <summary>
    /// The Identity <c>Person</c> id if the principal carries a parseable <c>person_id</c> claim
    /// (authenticated suite member), else null (guest — no cookie / no claim). Use on optional-auth
    /// paths such as the join endpoint, where both guests and suite members are valid.
    /// </summary>
    public static int? GetPersonIdOrNull(this ClaimsPrincipal principal)
        => int.TryParse(principal.FindFirstValue("person_id"), out var personId) ? personId : null;
}
