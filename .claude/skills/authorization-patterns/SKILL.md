---
name: authorization-patterns
description: Two-layer authorization — role gate + resource gate. Loaded when implementing endpoint authorization or access control.
user-invocable: true
---

# Authorization Patterns

## Two-Layer Authorization Template

### Layer 1 — Role Gate (endpoint-level)

Checks if the user has one of the allowed roles via JWT claims. Applied as a policy on the endpoint.

**Requirement:**
```csharp
public class {Resource}RoleRequirement : IAuthorizationRequirement
{
    public IReadOnlySet<UserRoleType> AllowedRoles { get; }

    public {Resource}RoleRequirement(IEnumerable<string> allowedRoles)
    {
        AllowedRoles = allowedRoles.Select(r => r.ToEnum<UserRoleType>()).ToHashSet();
    }

    public {Resource}RoleRequirement(IEnumerable<UserRoleType> allowedRoles)
    {
        AllowedRoles = allowedRoles.ToHashSet();
    }
}
```

**Registration:**
```csharp
services.AddAuthorization(options =>
{
    options.AddPolicy("{Resource}.{Action}", policy =>
        policy.Requirements.Add(new {Resource}RoleRequirement(new[] { UserRoleType.Editor, UserRoleType.Admin })));
});
```

**Applied to endpoint:** `[Authorize(Policy = "{Resource}.{Action}")]`

### Layer 2 — Resource Gate (resource-level)

Checks if the user has access to the specific resource being requested (e.g., is this user assigned to THIS article?).

**Access checker interface (per-service):**
```csharp
public interface I{Resource}AccessChecker
{
    Task<bool> HasAccessAsync(int? {resource}Id, int? userId, IReadOnlySet<UserRoleType> roles, CancellationToken ct = default);
}
```

**Authorization handler (combines both layers):**
```csharp
public class {Resource}AccessAuthorizationHandler(HttpContextProvider _httpProvider, I{Resource}AccessChecker _{resource}RoleChecker)
    : AuthorizationHandler<{Resource}RoleRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        {Resource}RoleRequirement requirement)
    {
        var userRoles = _httpProvider.GetUserRoles<UserRoleType>()
                            .Where(requirement.AllowedRoles.Contains)
                            .ToHashSet();

        if (userRoles.Count > 0
            && await _{resource}RoleChecker.HasAccessAsync(
                _httpProvider.Get{Resource}Id(), _httpProvider.GetUserId(), userRoles,
                _httpProvider.HttpContext.RequestAborted))
        {
            context.Succeed(requirement);
        }
    }
}
```

## Variants

### Simple (role-only)
Skip Layer 2. Use for admin endpoints, global actions where any user with the role can act.

```csharp
[Authorize(Roles = Role.EditorAdmin)]
```

Or via policy with just the role requirement, no access checker.

### Full (role + resource)
Both layers. Use when users can only access resources they own or are assigned to.

The handler checks role via `{Resource}RoleRequirement` AND queries DB via `I{Resource}AccessChecker` to verify the user has access to the specific resource.

### Tenant-scoped
Extend Layer 2 to include tenant ID in the access check. For multi-tenant apps, the checker verifies both resource access AND tenant membership.

## Source Files

- `src/BuildingBlocks/{ProjectName}.Abstractions/Security/I{Resource}AccessChecker.cs` — access checker interface
- `src/BuildingBlocks/{ProjectName}.Security/{Resource}RoleRequirement.cs`
- `src/BuildingBlocks/{ProjectName}.Security/{Resource}AccessAuthorizationHandler.cs`
- `src/BuildingBlocks/{ProjectName}.Security/Extensions.cs` — `RequireRoleAuthorization()` extension
- `src/BuildingBlocks/{ProjectName}.Security/Role.cs` — role string constants

## Endpoint Authorization by Framework

| Framework | Simple (role-only) | Policy-based |
|-----------|-------------------|--------------|
| FastEndpoints | `[Authorize(Roles = Role.Admin)]` | `[Authorize(Policy = "...")]` |
| Carter | `.RequireRoleAuthorization(Role.Editor, Role.EditorAdmin)` | `.RequireAuthorization("PolicyName")` |
| Minimal APIs | `.RequireRoleAuthorization(Role.Author)` | `.RequireAuthorization("PolicyName")` |
