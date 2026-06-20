using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Hubs;
using Reflekt.Domain.CardGroups.Events;

namespace Reflekt.API.Features.CardGroups.RemoveCardFromGroup;

public sealed class BroadcastCardUngroupedHandler(
    IHubContext<RetroHub> hubContext)
    : IEventHandler<CardUngroupedEvent>
{
    public async Task HandleAsync(CardUngroupedEvent ev, CancellationToken ct)
    {
        await hubContext.Clients.Group($"retro:{ev.SourceGroup.RetroSessionId}")
            .SendAsync("CardUngrouped", new
            {
                cardId = ev.CardId,
                newGroupId = ev.NewGroupId,
                sourceGroupId = ev.SourceGroup.Id
            }, ct);
    }
}
