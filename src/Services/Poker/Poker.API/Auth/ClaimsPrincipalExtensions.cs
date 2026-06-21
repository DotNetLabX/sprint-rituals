using System.Security.Claims;

namespace Poker.API.Auth;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// The Identity Person id from the cookie claim. Throws if absent.
    /// Use on authenticated-SM paths where person_id must be present.
    /// </summary>
    public static int GetPersonId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue("person_id")
            ?? throw new InvalidOperationException("person_id claim is missing.");
        return int.Parse(value);
    }

    /// <summary>
    /// Person id if the principal carries a parseable person_id claim, else null (guest).
    /// Use on join/optional-auth paths where both Google members and guests are valid.
    /// </summary>
    public static int? GetPersonIdOrNull(this ClaimsPrincipal principal)
        => int.TryParse(principal.FindFirstValue("person_id"), out var personId) ? personId : null;
}
