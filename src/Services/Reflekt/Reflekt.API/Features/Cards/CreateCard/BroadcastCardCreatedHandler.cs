using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Hubs;
using Reflekt.Domain.Cards.Events;

namespace Reflekt.API.Features.Cards.CreateCard;

public sealed class BroadcastCardCreatedHandler(IHubContext<SprintHub> hubContext)
    : IEventHandler<CardCreatedEvent>
{
    public Task HandleAsync(CardCreatedEvent ev, CancellationToken ct)
        => hubContext.Clients.Group($"sprint:{ev.Card.SprintId}")
            .SendAsync("CardCreated", ev.Card.Adapt<CardResponse>(), ct);
}
