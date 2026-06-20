using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Hubs;
using Reflekt.Domain.RetroSessions.Events;

namespace Reflekt.API.Features.CardGroups.ClearFocus;

public sealed class BroadcastGroupFocusClearedHandler(IHubContext<RetroHub> hubContext)
    : IEventHandler<GroupFocusClearedEvent>
{
    public Task HandleAsync(GroupFocusClearedEvent ev, CancellationToken ct)
        => hubContext.Clients.Group($"retro:{ev.RetroSession.Id}")
            .SendAsync("GroupFocusCleared", new
            {
                previousGroupId = ev.PreviousGroupId
            }, ct);
}

