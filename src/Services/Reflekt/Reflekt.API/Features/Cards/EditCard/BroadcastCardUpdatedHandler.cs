using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Hubs;
using Reflekt.Domain.Cards.Events;

namespace Reflekt.API.Features.Cards.EditCard;

public sealed class BroadcastCardUpdatedHandler(IHubContext<RetroHub> hubContext)
    : IEventHandler<CardUpdatedEvent>
{
    public Task HandleAsync(CardUpdatedEvent ev, CancellationToken ct)
        => hubContext.Clients.Group($"retro:{ev.Card.RetroSessionId}")
            .SendAsync("CardUpdated", ev.Card.Adapt<CardResponse>(), ct);
}
