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
    /// <summary>The Reflekt API assembly simple name — the single module RA2 excludes from the gate.</summary>
    public const string ReflektApiAssemblyName = "Reflekt.API";

    /// <summary>
    /// True when an endpoint declared in <paramref name="endpointAssemblyName"/> is excluded from the
    /// global active-user gate. Null (unresolved endpoint) is NOT excluded — the gate still applies.
    /// </summary>
    public static bool IsExcludedModule(string? endpointAssemblyName)
        => endpointAssemblyName == ReflektApiAssemblyName;
}
