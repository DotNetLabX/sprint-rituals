using Microsoft.AspNetCore.SignalR;
using Poker.API.Hubs;
using Poker.Domain.Planning.Events;

namespace Poker.API.Features.Sessions.EstimationEvents;

/// <summary>
/// Broadcasts VotesRevealed (full tally) to the session group.
/// This is the first point in the flow where vote VALUES are visible to all participants (Rule 7).
/// </summary>
public sealed class BroadcastVotesRevealedHandler(IHubContext<PlanningHub> hubContext)
    : IEventHandler<VotesRevealedEvent>
{
    public Task HandleAsync(VotesRevealedEvent ev, CancellationToken ct)
        => hubContext.Clients.Group($"poker:{ev.SessionId}")
            .SendAsync("VotesRevealed", new
            {
                sessionId = ev.SessionId,
                itemId = ev.ItemId,
                votes = ev.Votes.Select(v => new
                {
                    participantId = v.ParticipantId,
                    displayName = v.DisplayName,
                    value = v.Value,
                }).ToList(),
            }, ct);
}
