namespace Identity.API.Auth;

/// <summary>
/// Scopes the global <see cref="ActiveUserPreProcessor"/> off modules that authenticate
/// participants who are not Identity <c>User</c>s (RA2/ADR-016, Q4 mechanism A —
/// assembly-of-declaring-endpoint). Reflekt retro endpoints serve guests and (forward-compat
/// with ADR-021's waiting room) authenticated Persons without a User, so the active-user gate
/// must not 401 them. Discrimination is by the declaring endpoint's assembly **simple name**,
/// keeping Identity decoupled from Reflekt (no project reference).
/// </summary>
public static class ActiveUserGateScope
{
    /// <summary>The Reflekt API assembly simple name (RA2/ADR-016 — first excluded module).</summary>
    public const string ReflektApiAssemblyName = "Reflekt.API";

    /// <summary>The Poker API assembly simple name (F6 — second excluded module, same pattern).</summary>
    public const string PokerApiAssemblyName = "Poker.API";

    /// <summary>
    /// Assembly simple names excluded from the global active-user gate.
    /// Modules that authenticate participants who are NOT Identity Users (guests, retro/poker participants)
    /// must be listed here to avoid spurious 401s on guest-accessible endpoints.
    /// Discrimination is by the declaring endpoint's assembly simple name — Identity stays decoupled
    /// from the module projects (no project reference).
    /// </summary>
    private static readonly IReadOnlySet<string> ExcludedModules = new HashSet<string>
    {
        ReflektApiAssemblyName,
        PokerApiAssemblyName,
    };

    /// <summary>
    /// True when an endpoint declared in <paramref name="endpointAssemblyName"/> is excluded from the
    /// global active-user gate. Null (unresolved endpoint) is NOT excluded — the gate still applies.
    /// </summary>
    public static bool IsExcludedModule(string? endpointAssemblyName)
        => endpointAssemblyName is not null && ExcludedModules.Contains(endpointAssemblyName);
}
