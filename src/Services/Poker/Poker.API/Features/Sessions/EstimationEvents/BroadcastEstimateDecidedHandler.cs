using Microsoft.AspNetCore.SignalR;
using Poker.API.Hubs;
using Poker.Domain.Planning.Events;

namespace Poker.API.Features.Sessions.EstimationEvents;

/// <summary>
/// Broadcasts EstimateDecided after the SM picks a final value AND (Sprint-bound) Jira write-back confirmed.
/// The item is already marked Decided in the domain at this point (Rule 10 sequencing in PlanningHub).
/// </summary>
public sealed class BroadcastEstimateDecidedHandler(IHubContext<PlanningHub> hubContext)
    : IEventHandler<EstimateDecidedEvent>
{
    public Task HandleAsync(EstimateDecidedEvent ev, CancellationToken ct)
        => hubContext.Clients.Group($"poker:{ev.SessionId}")
            .SendAsync("EstimateDecided", new
            {
                sessionId = ev.SessionId,
                itemId = ev.ItemId,
                finalEstimate = ev.FinalEstimate,
            }, ct);
}
