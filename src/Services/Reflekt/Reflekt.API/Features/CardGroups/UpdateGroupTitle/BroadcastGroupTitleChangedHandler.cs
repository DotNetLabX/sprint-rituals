using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Hubs;
using Reflekt.Domain.CardGroups.Events;

namespace Reflekt.API.Features.CardGroups.UpdateGroupTitle;

public sealed class BroadcastGroupTitleChangedHandler(
    IHubContext<RetroHub> hubContext)
    : IEventHandler<CardGroupTitleChangedEvent>
{
    public async Task HandleAsync(CardGroupTitleChangedEvent ev, CancellationToken ct)
    {
        await hubContext.Clients.Group($"retro:{ev.Group.RetroSessionId}")
            .SendAsync("GroupTitleChanged", new
            {
                groupId = ev.Group.Id,
                title = ev.Group.Title
            }, ct);
    }
}
