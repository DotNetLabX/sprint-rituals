using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Fokus.API.Hubs;

/// <summary>
/// SignalR hub for sprint-related real-time notifications.
/// Clients subscribe here and receive server-pushed events after sync operations.
/// No client-to-server methods in v1 — server-push only.
/// </summary>
[Authorize]
public class SprintHub : Hub
{
}

/// <summary>
/// Strongly-typed method name constants for SprintHub client calls.
/// </summary>
public static class SprintHubMethods
{
    public const string SprintSynced = "SprintSynced";
}
