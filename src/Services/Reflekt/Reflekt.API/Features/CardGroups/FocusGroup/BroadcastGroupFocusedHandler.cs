using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Hubs;
using Reflekt.Domain.RetroSessions.Events;

namespace Reflekt.API.Features.CardGroups.FocusGroup;

public sealed class BroadcastGroupFocusedHandler(IHubContext<RetroHub> hubContext)
    : IEventHandler<GroupFocusedEvent>
{
    public Task HandleAsync(GroupFocusedEvent ev, CancellationToken ct)
        => hubContext.Clients.Group($"retro:{ev.RetroSession.Id}")
            .SendAsync("GroupFocused", new
            {
                groupId = ev.GroupId,
                previousGroupId = ev.PreviousGroupId
            }, ct);
}

