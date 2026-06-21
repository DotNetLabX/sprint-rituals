using Microsoft.AspNetCore.SignalR;
using Poker.API.Hubs;
using Poker.Domain.Planning.Events;

namespace Poker.API.Features.Sessions.EstimationEvents;

/// <summary>
/// Broadcasts ItemSkipped when the SM explicitly skips an item (Rule 9).
/// No estimate is recorded; no write-back.
/// </summary>
public sealed class BroadcastItemSkippedHandler(IHubContext<PlanningHub> hubContext)
    : IEventHandler<ItemSkippedEvent>
{
    public Task HandleAsync(ItemSkippedEvent ev, CancellationToken ct)
        => hubContext.Clients.Group($"poker:{ev.SessionId}")
            .SendAsync("ItemSkipped", new
            {
                sessionId = ev.SessionId,
                itemId = ev.ItemId,
            }, ct);
}
