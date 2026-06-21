using Microsoft.AspNetCore.SignalR;
using Poker.API.Hubs;
using Poker.Domain.Planning.Events;

namespace Poker.API.Features.Sessions.EstimationEvents;

/// <summary>
/// Broadcasts ItemSelected to the session group when the SM selects an item to estimate.
/// Mirrors Reflekt's BroadcastCardCreatedHandler pattern (effect-named, co-located with feature).
/// </summary>
public sealed class BroadcastItemSelectedHandler(IHubContext<PlanningHub> hubContext)
    : IEventHandler<ItemSelectedEvent>
{
    public Task HandleAsync(ItemSelectedEvent ev, CancellationToken ct)
        => hubContext.Clients.Group($"poker:{ev.SessionId}")
            .SendAsync("ItemSelected", new
            {
                sessionId = ev.SessionId,
                itemId = ev.ItemId,
                label = ev.Label,
            }, ct);
}
