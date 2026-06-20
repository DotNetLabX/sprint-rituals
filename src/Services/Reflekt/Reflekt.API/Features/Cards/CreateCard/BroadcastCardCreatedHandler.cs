using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Hubs;
using Reflekt.Domain.Cards.Events;

namespace Reflekt.API.Features.Cards.CreateCard;

public sealed class BroadcastCardCreatedHandler(IHubContext<RetroHub> hubContext)
    : IEventHandler<CardCreatedEvent>
{
    public Task HandleAsync(CardCreatedEvent ev, CancellationToken ct)
        => hubContext.Clients.Group($"retro:{ev.Card.RetroSessionId}")
            .SendAsync("CardCreated", ev.Card.Adapt<CardResponse>(), ct);
}
