using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Hubs;
using Reflekt.Domain.Cards.Events;

namespace Reflekt.API.Features.Cards.DeleteCard;

public sealed class BroadcastCardDeletedHandler(IHubContext<SprintHub> hubContext)
    : IEventHandler<CardDeletedEvent>
{
    public Task HandleAsync(CardDeletedEvent ev, CancellationToken ct)
        => hubContext.Clients.Group($"sprint:{ev.Card.SprintId}")
            .SendAsync("CardDeleted", new { id = ev.Card.Id }, ct);
}
