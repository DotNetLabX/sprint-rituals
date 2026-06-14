using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Hubs;
using Reflekt.Domain.Cards.Events;

namespace Reflekt.API.Features.Cards.EditCard;

public sealed class BroadcastCardUpdatedHandler(IHubContext<SprintHub> hubContext)
    : IEventHandler<CardUpdatedEvent>
{
    public Task HandleAsync(CardUpdatedEvent ev, CancellationToken ct)
        => hubContext.Clients.Group($"sprint:{ev.Card.SprintId}")
            .SendAsync("CardUpdated", ev.Card.Adapt<CardResponse>(), ct);
}
