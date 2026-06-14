using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Hubs;
using Reflekt.Domain.CardGroups.Events;

namespace Reflekt.API.Features.CardGroups.AddCardToGroup;

public sealed class BroadcastCardsGroupedHandler(
    IHubContext<SprintHub> hubContext)
    : IEventHandler<CardsGroupedEvent>
{
    public async Task HandleAsync(CardsGroupedEvent ev, CancellationToken ct)
    {
        await hubContext.Clients.Group($"sprint:{ev.Group.SprintId}")
            .SendAsync("CardsGrouped", new
            {
                groupId = ev.Group.Id,
                cardId = ev.CardId,
                sourceGroupId = ev.SourceGroupId
            }, ct);
    }
}
