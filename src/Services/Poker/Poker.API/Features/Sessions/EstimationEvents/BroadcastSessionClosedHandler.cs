using Microsoft.AspNetCore.SignalR;
using Poker.API.Hubs;
using Poker.Domain.Planning.Events;

namespace Poker.API.Features.Sessions.EstimationEvents;

/// <summary>
/// Broadcasts SessionClosed with summary stats when the SM closes the session (Rule 14/17).
/// Clients render the read-only summary view on receipt.
/// </summary>
public sealed class BroadcastSessionClosedHandler(IHubContext<PlanningHub> hubContext)
    : IEventHandler<SessionClosedEvent>
{
    public Task HandleAsync(SessionClosedEvent ev, CancellationToken ct)
        => hubContext.Clients.Group($"poker:{ev.SessionId}")
            .SendAsync("SessionClosed", new
            {
                sessionId = ev.SessionId,
                itemsEstimated = ev.ItemsEstimated,
                itemsSkipped = ev.ItemsSkipped,
                totalStoryPoints = ev.TotalStoryPoints,
            }, ct);
}
